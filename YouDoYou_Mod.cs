using System.Collections.Generic;
using System.Reflection;
using System;
using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    public class YouDoYou_Mod : Mod
    {
        YouDoYou_Settings settings;

        public YouDoYou_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<YouDoYou_Settings>();
            Harmony harmony = new Harmony("freemapa.youdoyou");
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("BrawlersCanHuntLong".Translate(), ref settings.brawlersCanHunt, "BrawlersCanHuntShort".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "YouDoYouName".Translate();
        }
    }

    [HarmonyPatch(typeof(PawnTable), MethodType.Constructor, new Type[] { typeof(PawnTableDef), typeof(Func<IEnumerable<Pawn>>), typeof(int), typeof(int) })]
    public class RemoveFreePawns
    {
        static void Postfix(PawnTableDef def, ref Func<IEnumerable<Pawn>> ___pawnsGetter)
        {
            if (def != PawnTableDefOf.Work)
            {
                return;
            }
            Func<IEnumerable<Pawn>> oldPawns = ___pawnsGetter;
            try
            {
                ___pawnsGetter = () =>
                {
                    try
                    {
                        List<Pawn> newPawns = new List<Pawn>();
                        foreach (Pawn pawn in oldPawns())
                        {
                            YouDoYou_MapComponent ydy = pawn.Map.GetComponent<YouDoYou_MapComponent>();
                            string pawnKey = pawn.GetUniqueLoadID();
                            if (ydy != null && ydy.pawnFree != null && ydy.pawnFree.ContainsKey(pawnKey) && ydy.pawnFree[pawnKey])
                            {
                                continue;
                            }
                            newPawns.Add(pawn);
                        }
                        return newPawns;
                    }
                    catch
                    {
                        Logger.Message("You Do You could not remove free pawns from work tab - likely due to a mod conflict");
                        return oldPawns();
                    }
                };
                Logger.Message("You Do You sucessfully patched the work tab");
            }
            catch
            {
                Logger.Message("You Do You failed to patch the work tab");
                ___pawnsGetter = oldPawns;
            }
        }
    }
}

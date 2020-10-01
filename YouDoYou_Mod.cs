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
            Logger.Message("applying patching for YouDoYou");
            try
            {
                harmony.PatchAll(assembly);
            }
            catch
            {
                Logger.Message("could not patch work tab - free pawns may not get removed from the work tab correctly.");
            }
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
            ___pawnsGetter = () =>
            {
                List<Pawn> newPawns = new List<Pawn>();
                foreach (Pawn pawn in oldPawns())
                {
                    YouDoYou_MapComponent ydy = pawn.Map.GetComponent<YouDoYou_MapComponent>();
                    if (ydy != null && ydy.pawnFree.ContainsKey(pawn.GetUniqueLoadID()) && ydy.pawnFree[pawn.GetUniqueLoadID()])
                    {
                        continue;
                    }
                    newPawns.Add(pawn);
                }
                return newPawns;
            };
        }
    }
}

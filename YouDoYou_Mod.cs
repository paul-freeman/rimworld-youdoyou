using System.Collections.Generic;
using System.Reflection;
using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
            harmony.PatchAll(assembly);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("BrawlersCanHuntLong".Translate(), ref settings.brawlersCanHunt, "BrawlersCanHuntShort".Translate());
            listingStandard.CheckboxLabeled("HideWorkTabLong".Translate(), ref settings.hideWorkTab, "HideWorkTabShort".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "YouDoYouName".Translate();
        }
    }

    [HarmonyPatch(typeof(MainButtonsRoot), MethodType.Constructor)]
    public class RemoveWorkTabPatch
    {
        static void Postfix(ref List<MainButtonDef> ___allButtonsInOrder)
        {
            Logger.Message("removing work tab");
            YouDoYou_Settings settings = LoadedModManager.GetMod<YouDoYou_Mod>().GetSettings<YouDoYou_Settings>();
            if (settings.hideWorkTab)
            {
                ___allButtonsInOrder.Remove(DefDatabase<MainButtonDef>.GetNamed("Work"));
            }
        }
    }

    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class DefGenerator_GenerateImpliedDefs_PreResolve
    {
        static void Postfix()
        {
            PawnTableDef workTable = PawnTableDefOf.Work;
            int labelIndex = workTable.columns.IndexOf(DefDatabase<PawnColumnDef>.GetNamed("CopyPasteWorkPriorities"));
            PawnColumnDef enslavedCol = DefDatabase<PawnColumnDef>.GetNamed("Enslaved");
            if (enslavedCol == null)
            {
                Logger.Error("could not find enslaved column");
            }
            workTable.columns.Insert(labelIndex + 1, enslavedCol);
        }
    }
}

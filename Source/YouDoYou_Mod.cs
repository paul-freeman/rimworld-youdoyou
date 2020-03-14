using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    public class YouDoYou_Mod : Mod
    {
        YouDoYou_Settings settings;

        public YouDoYou_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<YouDoYou_Settings>();

            var harmony = new Harmony("freemapa.youdoyou");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("BrawlersCanHuntLong".Translate(), ref settings.brawlersCanHunt, "BrawlersCanHuntShort".Translate());
            listingStandard.CheckboxLabeled("AdaptiveCleaningLong".Translate(), ref settings.adaptiveCleaning, "AdaptiveCleaningShort".Translate());
            listingStandard.CheckboxLabeled("AdaptiveHaulingLong".Translate(), ref settings.adaptiveHauling, "AdaptiveHaulingShort".Translate());
            listingStandard.CheckboxLabeled("DontDisableAnythingLong".Translate(), ref settings.dontDisableAnything, "DontDisableAnythingShort".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "YouDoYouName".Translate();
        }

        public static List<PawnColumnDef> columns;
    }
}

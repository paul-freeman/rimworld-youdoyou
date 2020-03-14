using Verse;

namespace YouDoYou
{
    public class YouDoYou_Settings : ModSettings
    {
        public bool brawlersCanHunt = false;
        public bool adaptiveCleaning = true;
        public bool adaptiveHauling = true;
        public bool dontDisableAnything = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref brawlersCanHunt, "brawlersCanHunt");
            Scribe_Values.Look(ref adaptiveCleaning, "adaptiveCleaning");
            Scribe_Values.Look(ref adaptiveHauling, "adaptiveHauling");
            Scribe_Values.Look(ref dontDisableAnything, "dontDisableAnything");
            base.ExposeData();
        }
    }
}

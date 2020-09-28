using Verse;

namespace YouDoYou
{
    public class YouDoYou_Settings : ModSettings
    {
        public bool brawlersCanHunt = false;
        public bool hideWorkTab = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref brawlersCanHunt, "brawlersCanHunt");
            Scribe_Values.Look(ref hideWorkTab, "hideWorkTab");
            base.ExposeData();
        }
    }
}

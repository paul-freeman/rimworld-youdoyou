using Verse;
using RimWorld;

namespace YouDoYou
{
    public class YouDoYou_PawnColumnWorker_Checkbox : PawnColumnWorker_Checkbox
    {
        protected override bool GetValue(Pawn pawn)
        {
            YouDoYou_MapComponent timeKeeper = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
            return timeKeeper.autoPriorities.TryGetValue(pawn.GetUniqueLoadID(), true);
        }

        protected override void SetValue(Pawn pawn, bool value)
        {
            YouDoYou_MapComponent timeKeeper = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
            timeKeeper.autoPriorities[pawn.GetUniqueLoadID()] = value;
        }
    }
}

using Verse;
using RimWorld;

namespace YouDoYou
{
    public class YouDoYou_PawnColumnWorker_Checkbox : PawnColumnWorker_Checkbox
    {
        protected override bool GetValue(Pawn pawn)
        {
            YouDoYou_MapComponent ydy = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
            string pawnKey = pawn.GetUniqueLoadID();
            if (!ydy.pawnEnslaved.ContainsKey(pawnKey))
            {
                ydy.pawnEnslaved[pawnKey] = false;
            }
            return ydy.pawnEnslaved[pawnKey];
        }

        protected override void SetValue(Pawn pawn, bool value)
        {
            YouDoYou_MapComponent ydy = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>();
            ydy.pawnEnslaved[pawn.GetUniqueLoadID()] = value;
        }
    }
}

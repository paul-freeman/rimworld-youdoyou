using Verse;

namespace YouDoYou
{
    class BestWorker
    {
        private WorkTypeDef workTypeDef;
        private float skill;
        public Pawn Pawn { get; set; }
        public bool Filled { get; set; }


        public BestWorker(WorkTypeDef workTypeDef)
        {
            this.workTypeDef = workTypeDef;
            skill = -1.0f;
            Filled = false;
        }

        public void Update(Pawn pawn, float skill, Priority priority)
        {
            if (pawn != null && pawn.Downed)
                return;
            if (workTypeDef.defName == "Hunting" && pawn != null && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.IsRangedWeapon)
                return;
            if (pawn != null && skill <= this.skill)
                return;

            Pawn = pawn;
            this.skill = skill;
            Filled = Filled || (priority.toInt() > 0);
            return;
        }
    }
}

using Verse;

namespace YouDoYou
{
    class BestWorker
    {
        private Pawn pawn;
        private WorkTypeDef workTypeDef;
        private float skill;
        private float filledCutoff;
        private bool filled;

        public BestWorker(WorkTypeDef workTypeDef, float filledCutoff)
        {
            this.workTypeDef = workTypeDef;
            this.filledCutoff = filledCutoff;
            skill = -1.0f;
            filled = false;
        }

        public void Update(Pawn pawn, float skill, float priority)
        {
            if (workTypeDef.defName == "Hunting" && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.IsRangedWeapon)
            {
                return;
            }
            if (pawn == null || skill > this.skill || priority >= this.filledCutoff)
            {
                this.pawn = pawn;
                this.skill = skill;
                filled = (filled || priority >= this.filledCutoff) ? true : false;
                return;
            }
            return;
        }

        public bool Filled()
        {
            return this.filled;
        }

        public Pawn Get()
        {
            return this.pawn;
        }
    }
}

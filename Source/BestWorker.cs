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
            if (workTypeDef.defName == "Hunting" && pawn != null && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.IsRangedWeapon)
                return;
            bool runTest = pawn == null || skill > this.skill;
            bool fillTest = priority.toInt() > 0;
            Logger.Debug(workTypeDef.defName + ": "
                + pawn.Name.ToStringShort + ": "
                + skill.ToString() + ": "
                + (runTest ? (fillTest ? "filled" : "not filled") : "skipped"));
            if (runTest)
            {
                this.Pawn = pawn;
                this.skill = skill;
                Filled = Filled || fillTest;
                return;
            }
            return;
        }
    }
}

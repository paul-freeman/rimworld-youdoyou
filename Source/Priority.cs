using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    class Priority
    {
        private float value;
        private bool disabled;

        public Priority()
        {
            value = 0f;
            disabled = false;
        }

        public int toInt()
        {
            if (disabled)
                return 0;
            return _toInt();
        }

        private int _toInt()
        {
            if (value >= 0.8)
                return 1;
            if (value >= 0.6)
                return 2;
            if (value >= 0.4)
                return 3;
            if (value >= 0.2)
                return 4;
            return 0;
        }

        public Priority Set(int n)
        {
            switch (n)
            {
                case 1:
                    value = 0.9f;
                    return this;
                case 2:
                    value = 0.7f;
                    return this;
                case 3:
                    value = 0.5f;
                    return this;
                case 4:
                    value = 0.3f;
                    return this;
                default:
                    value = 0.1f;
                    return this;
            }
        }

        public Priority EnableIf(bool cond)
        {
            if (cond)
                disabled = false;
            return this;
        }

        public Priority Enable()
        {
            return EnableIf(true);
        }

        public Priority DisableIf(bool cond)
        {
            if (cond)
                disabled = true;
            return this;
        }

        public Priority Multiply(float n)
        {
            value *= n;
            return this;
        }

        public Priority Step(float step)
        {
            return StepIf(true, step);
        }

        public Priority StepIf(bool cond, float n)
        {
            if (cond)
                value += n * 0.2f;
            return this;
        }
        public bool Enabled()
        {
            return !disabled;
        }

        public bool Disabled()
        {
            return disabled;
        }

        // always do this
        public Priority ConsiderAlwaysDoing(Pawn pawn)
        {
            if (_toInt() == 0)
            {
                Set(4);
                Enable();
            }
            return this;
        }

        // do this if idle
        public Priority ConsiderIdle(Pawn pawn)
        {
            if (_toInt() == 0 && pawn.mindState.IsIdle)
            {
                Set(4);
                Enable();
            }
            return this;
        }

        // consider if pawn is downed
        public Priority ConsiderDowned(Pawn pawn, WorkTypeDef workTypeDef)
        {
            if (!pawn.Downed)
                return this;
            Set(1);
            DisableIf(workTypeDef.defName != "Patient" && workTypeDef.defName != "PatientBedRest");
            return this;
        }

        // raise this two steps if inspired
        public Priority ConsiderInspiration(Pawn pawn, WorkTypeDef workTypeDefA)
        {
            if (!pawn.mindState.inspirationHandler.Inspired)
                return this;
            Inspiration i = pawn.mindState.inspirationHandler.CurState;
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredNonDisabledWorkTypes ?? new List<WorkTypeDef>())
            {
                if (workTypeDefA.defName == workTypeDefB.defName)
                    return Step(2);
            }
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredAnyNonDisabledWorkType ?? new List<WorkTypeDef>())
            {
                if (workTypeDefA.defName == workTypeDefB.defName)
                    return Step(2);
            }
            return this;
        }

        // raise this based on passion
        public Priority ConsiderPassion(Pawn pawn, WorkTypeDef workTypeDef)
        {
            switch (pawn.skills.MaxPassionOfRelevantSkillsFor(workTypeDef))
            {
                case Passion.Major:
                    Step(pawn.needs.mood.CurLevel * 2.5f);
                    return this;
                case Passion.Minor:
                    Step(pawn.needs.mood.CurLevel * 1.25f);
                    if (value > 0.7f)
                        value = 0.7f;
                    return this;
                default:
                    if (value > 0.5f)
                        value = 0.5f;
                    return this;
            }
        }

        // raise this based on downed colonists
        public Priority ConsiderDownedColonists(Pawn pawn, WorkTypeDef workTypeDef, float percentDownedColonists)
        {
            if (workTypeDef.defName != "Doctor")
                return this;
            value += percentDownedColonists;
            return this;
        }

        public Priority ConsiderBuildingImmunity(Pawn pawn, WorkTypeDef workTypeDef)
        {
            try
            {
                if (!pawn.health.hediffSet.HasImmunizableNotImmuneHediff())
                    return this;
                if (workTypeDef.defName == "PatientBedRest")
                    return Step(2);
                if (workTypeDef.defName == "Patient")
                    return this;
                return Step(-1);
            }
            catch
            {
                Logger.Debug("could not consider pawn building immunity");
                return this;
            }
        }

        public Priority ConsiderColonistsNeedingTreatment(Pawn pawn, WorkTypeDef workTypeDef, float percentColonistsNeedingTreatment)
        {
            if (pawn.health.HasHediffsNeedingTend() && workTypeDef.defName != "Patient" && workTypeDef.defName != "PatientBedRest")
                return Step(-1);
            if (workTypeDef.defName == "Doctor")
            {
                value += percentColonistsNeedingTreatment;
                return this;
            }
            return this;
        }

        public Priority AdaptiveCleaning(Map map, Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings)
        {
            if (workTypeDef.defName != "Cleaning" || !settings.adaptiveCleaning)
                return this;

            if (!pawn.Map.areaManager.Home[pawn.Position])
                return Set(4);

            float pawnCleaningDesire = 1.0f - pawn.needs.beauty.CurLevel;
            if (value >= pawnCleaningDesire)
                return this;

            // Our desire to clean is overwhelming!
            // See if we can find something to clean.
            BeautyUtility.FillBeautyRelevantCells(pawn.Position, pawn.Map);
            foreach (IntVec3 c in BeautyUtility.beautyRelevantCells)
                foreach (Thing thing in map.thingGrid.ThingsListAt(c))
                    if (thing.def.category == ThingCategory.Filth)
                    {
                        value = pawnCleaningDesire;
                        return this;
                    }
            return this;
        }

        public Priority ConsiderLowFood(Pawn pawn, WorkTypeDef workTypeDef, int freeColonistsSpawnedCount, float totalHumanEdibleNutrition)
        {
            try
            {
                if (totalHumanEdibleNutrition < 4f * (float)freeColonistsSpawnedCount)
                {
                    if (workTypeDef.defName == "Cooking")
                        return Step(2).Enable();
                    if (workTypeDef.defName == "Hunting" || workTypeDef.defName == "PlantCutting")
                        return Step(1);
                }
                return this;
            }
            catch
            {
                Logger.Debug("Unable to consider low food due to an error");
                return this;
            }
        }

        public Priority AdaptiveHauling(Map map, Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings)
        {
            if (workTypeDef.defName != "Hauling" || !settings.adaptiveHauling)
                return this;

            float pawnHaulingDesire = 1.0f - pawn.needs.beauty.CurInstantLevel;
            if (value >= pawnHaulingDesire)
                return this;

            // Our desire to haul is overwhelming!
            // See if we can find something to haul.
            BeautyUtility.FillBeautyRelevantCells(pawn.Position, pawn.Map);
            foreach (Thing thing in map.listerHaulables?.ThingsPotentiallyNeedingHauling())
                foreach (IntVec3 c in BeautyUtility.beautyRelevantCells)
                    if (c == thing.Position)
                    {
                        value = pawnHaulingDesire;
                        return this;
                    }
            return this;
        }

        public Priority CalcPriority(
            Map map,
            int numPawns,
            Pawn pawn,
            WorkTypeDef workTypeDef,
            bool thingsDeteriorating,
            float percentDownedColonists,
            float percentColonistsNeedingTreatment,
            int freeColonistsSpawnedCount,
            float totalHumanEdibleNutrition,
            YouDoYou_Settings settings)
        {
            float badSkillCutoff = numPawns;
            float goodSkillCutoff = badSkillCutoff + (20f - badSkillCutoff) / 2f;
            float greatSkillCutoff = goodSkillCutoff + (20f - goodSkillCutoff) / 2f;
            float excellentSkillCutoff = greatSkillCutoff + (20f - greatSkillCutoff) / 2f;
            switch (workTypeDef.defName)
            {
                case "Firefighter":
                case "Patient":
                    return this
                        .Set(1)
                        .ConsiderAlwaysDoing(pawn)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "PatientBedRest":
                    return this
                        .Set(3)
                        .Step((1.0f - Mathf.Pow(pawn.health.summaryHealth.SummaryHealthPercent, 4.0f) * 5.0f))
                        .ConsiderAlwaysDoing(pawn)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "BasicWorker":
                    return this
                        .Set(2)
                        .Step(-0.5f)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .ConsiderIdle(pawn)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Hauling":
                case "HaulingUrgent":
                    return this
                        .Set(2)
                        .Step(-0.5f)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .StepIf(!thingsDeteriorating, -1.0f)
                        .AdaptiveHauling(map, pawn, workTypeDef, settings)
                        .ConsiderIdle(pawn)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Cleaning":
                    ExpectationDef e = ExpectationsUtility.CurrentExpectationFor(pawn);
                    return this
                        .Set(2)
                        .Step(e.order - 4)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .AdaptiveCleaning(map, pawn, workTypeDef, settings)
                        .ConsiderIdle(pawn)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                default:
                    if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= excellentSkillCutoff) Set(1);
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= greatSkillCutoff) Set(2);
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= goodSkillCutoff) Set(3);
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= badSkillCutoff) Set(4);
                    else Set(0);

                    return this
                        .ConsiderPassion(pawn, workTypeDef)
                        .ConsiderInspiration(pawn, workTypeDef)
                        .ConsiderDownedColonists(pawn, workTypeDef, percentDownedColonists)
                        .ConsiderIdle(pawn)
                        .DisableIf(workTypeDef.defName == "Hunting" && pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")))
                        .EnableIf(settings.brawlersCanHunt)
                        .DisableIf(workTypeDef.defName == "Hunting" && !WorkGiver_HunterHunt.HasHuntingWeapon(pawn))
                        .EnableIf(settings.dontDisableAnything)
                        .ConsiderLowFood(pawn, workTypeDef, freeColonistsSpawnedCount, totalHumanEdibleNutrition)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;
            }
        }
    }
}

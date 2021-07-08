using System;
using System.Text;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;


namespace YouDoYou
{
    public class Priority : IComparable
    {
        static private readonly int disabledCutoff = 100 / (Pawn_WorkSettings.LowestPriority + 1); // 20 if LowestPriority is 4
        static private readonly int disabledCutoffActiveWorkArea = 100 - disabledCutoff; // 80 if LowestPriority is 4
        static private readonly float onePriorityWidth = (float)disabledCutoffActiveWorkArea / (float)Pawn_WorkSettings.LowestPriority; // ~20 if LowestPriority is 4

        private Pawn pawn;
        private WorkTypeDef workTypeDef;
        private float value;
        private List<string> adjustmentStrings;
        private bool enabled;
        private bool disabled;
        private YouDoYou_MapComponent mapComp;
        private YouDoYou_WorldComponent worldComp;
        const string FIREFIGHTER = "Firefighter";
        const string PATIENT = "Patient";
        const string DOCTOR = "Doctor";
        const string PATIENT_BED_REST = "PatientBedRest";
        const string BASIC_WORKER = "BasicWorker";
        const string WARDEN = "Warden";
        const string HANDLING = "Handling";
        const string COOKING = "Cooking";
        const string HUNTING = "Hunting";
        const string CONSTRUCTION = "Construction";
        const string GROWING = "Growing";
        const string MINING = "Mining";
        const string PLANT_CUTTING = "PlantCutting";
        const string SMITHING = "Smithing";
        const string TAILORING = "Tailoring";
        const string ART = "Art";
        const string CRAFTING = "Crafting";
        const string HAULING = "Hauling";
        const string CLEANING = "Cleaning";
        const string RESEARCHING = "Research";

        // supported mod work types
        const string HAULING_URGENT = "HaulingUrgent";


        public Priority(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, bool freePawn)
        {
            this.pawn = pawn;
            this.workTypeDef = workTypeDef;
            this.adjustmentStrings = new List<string> { };
            this.mapComp = pawn.Map.GetComponent<YouDoYou_MapComponent>();
            this.worldComp = Find.World.GetComponent<YouDoYou_WorldComponent>();
            if (freePawn)
            {
                try
                {
                    this.set(0.2f, "YouDoYouPriorityGlobalDefault".TranslateSimple()).compute();
                }
                catch (System.Exception e)
                {
                    Logger.Message("could not set " + workTypeDef.defName + " priority for pawn: " + pawn.Name + ": " + e.Message);
                    throw;
                }
            }
            else
            {
                int p = pawn.workSettings.GetPriority(workTypeDef);
                if (p == 0)
                {
                    this.set(0.0f, "YouDoYouPriorityNoFreeWill".TranslateSimple());
                }
                else
                {
                    this.set((100f - onePriorityWidth * (p - 1)) / 100f, "YouDoYouPriorityNoFreeWill".TranslateSimple());
                }
            }
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            Priority p = obj as Priority;
            if (p == null)
            {
                return 1;
            }
            return this.value.CompareTo(p.value);
        }

        private Priority compute()
        {
            if (pawn == null)
            {
                Logger.Message("pawn is null");
                return this;
            }
            if (workTypeDef == null)
            {
                Logger.Message("workTypeDef is null");
                return this;
            }
            this.enabled = false;
            this.disabled = false;
            if (this.pawn.GetDisabledWorkTypes(true).Contains(this.workTypeDef))
            {
                return this.neverDo("YouDoYouPriorityPermanentlyDisabled".TranslateSimple());
            }
            switch (this.workTypeDef.defName)
            {
                case FIREFIGHTER:
                    return this
                        .set(0.2f, "YouDoYouPriorityFirefightingDefault".TranslateSimple())
                        .neverDoIf(this.pawn.Downed, "YouDoYouPriorityPawnDowned".TranslateSimple())
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PATIENT:
                    return this
                        .set(0.0f, "YouDoYouPriorityPatientDefault".TranslateSimple())
                        .alwaysDo("YouDoYouPriorityPatientDefault".TranslateSimple())
                        .considerHealth()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case DOCTOR:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PATIENT_BED_REST:
                    return this
                        .set(0.0f, "YouDoYouPriorityBedrestDefault".TranslateSimple())
                        .alwaysDo("YouDoYouPriorityBedrestDefault".TranslateSimple())
                        .considerHealth()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerBored()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case BASIC_WORKER:
                    return this
                        .set(0.5f, "YouDoYouPriorityBasicWorkDefault".TranslateSimple())
                        .considerThoughts()
                        .considerNeedingWarmClothes()
                        .considerHealth()
                        .considerBored()
                        .neverDoIf(this.pawn.Downed, "YouDoYouPriorityPawnDowned".TranslateSimple())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case WARDEN:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HANDLING:
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case COOKING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerFoodPoisoning()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HUNTING:
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerHasHuntingWeapon()
                        .considerBrawlersNotHunting()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CONSTRUCTION:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case GROWING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case MINING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PLANT_CUTTING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerPlantsBlighted()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case SMITHING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case TAILORING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case ART:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CRAFTING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HAULING:
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CLEANING:
                    return this
                        .considerBeautyExpectations()
                        .considerIsAnyoneElseDoing()
                        .considerThoughts()
                        .considerOwnRoom()
                        .considerFoodPoisoning()
                        .considerHealth()
                        .considerBored()
                        .neverDoIf(notInHomeArea(this.pawn), "YouDoYouPriorityNotInHomeArea".TranslateSimple())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case RESEARCHING:
                    return this
                        .considerRelevantSkills()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HAULING_URGENT:
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                default:
                    Logger.Message("no custom priorities set for " + this.workTypeDef.defName);
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;
            }
        }

        public void ApplyPriorityToGame()
        {
            pawn.workSettings.SetPriority(workTypeDef, this.ToGamePriority());
        }

        public string GetTip()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(workTypeDef.description);
            if (!this.disabled)
            {
                int p = this.ToGamePriority();
                string str = string.Format("Priority{0}", p).TranslateSimple();
                string text = str.Colorize(WidgetsWork.ColorOfPriority(p));
                stringBuilder.AppendLine(text);
                stringBuilder.AppendLine("------------------------------");
            }
            foreach (string adj in this.adjustmentStrings)
            {
                stringBuilder.AppendLine(adj);
            }
            return stringBuilder.ToString();
        }

        public int ToGamePriority()
        {
            int valueInt = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(this.value * 100), 0, 100);
            if (valueInt <= disabledCutoff)
            {
                if (this.enabled)
                {
                    return Pawn_WorkSettings.LowestPriority;
                }
                return 0;
            }
            if (this.disabled)
            {
                return 0;
            }
            int invertedValueRange = disabledCutoffActiveWorkArea - (valueInt - disabledCutoff); // 0-80 if LowestPriority is 4
            int gamePriorityValue = UnityEngine.Mathf.FloorToInt((float)invertedValueRange / onePriorityWidth) + 1;
            if (gamePriorityValue > Pawn_WorkSettings.LowestPriority || gamePriorityValue < 1)
            {
                Logger.Error("calculated an invalid game priority value of " + gamePriorityValue.ToString());
                gamePriorityValue = UnityEngine.Mathf.Clamp(gamePriorityValue, 1, Pawn_WorkSettings.LowestPriority);
            }

            return gamePriorityValue;
        }

        private Priority set(float x, string s)
        {
            this.value = UnityEngine.Mathf.Clamp01(x);
            if (Prefs.DevMode)
            {
                this.adjustmentStrings.Add("-- reset --");
                this.adjustmentStrings.Add(string.Format("{0} ({1})", this.value.ToStringPercent(), s));
            }
            else
            {
                this.adjustmentStrings = new List<string> { string.Format("{0} ({1})", this.value.ToStringPercent(), s) };
            }
            return this;
        }

        private Priority add(float x, string s)
        {
            if (disabled)
            {
                return this;
            }
            float newValue = UnityEngine.Mathf.Clamp01(value + x);
            if (newValue > value)
            {
                adjustmentStrings.Add(string.Format("+{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            else if (newValue < value)
            {
                adjustmentStrings.Add(string.Format("{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            else if (newValue == value && Prefs.DevMode)
            {
                adjustmentStrings.Add(string.Format("+{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            return this;
        }

        private Priority multiply(float x, string s)
        {
            if (disabled)
            {
                return this;
            }
            float newValue = UnityEngine.Mathf.Clamp01(value * x);
            return add(newValue - value, s);
        }

        private bool isDisabled()
        {
            return this.disabled;
        }

        private Priority alwaysDoIf(bool cond, string s)
        {
            if (!cond || this.enabled)
            {
                return this;
            }
            if (Prefs.DevMode || this.disabled || this.ToGamePriority() == 0)
            {
                string text = string.Format("{0} ({1})", "YouDoYouPriorityEnabled".TranslateSimple(), s);
                this.adjustmentStrings.Add(text);
            }
            this.enabled = true;
            this.disabled = false;
            return this;
        }

        private Priority alwaysDo(string s)
        {
            return this.alwaysDoIf(true, s);
        }

        private Priority neverDoIf(bool cond, string s)
        {
            if (!cond || this.disabled)
            {
                return this;
            }
            if (Prefs.DevMode || this.enabled || this.ToGamePriority() >= 0)
            {
                string text = string.Format("{0} ({1})", "YouDoYouPriorityDisabled".TranslateSimple(), s);
                this.adjustmentStrings.Add(text);
            }
            this.disabled = true;
            this.enabled = false;
            return this;
        }

        private Priority neverDo(string s)
        {
            return this.neverDoIf(true, s);
        }

        private Priority considerInspiration()
        {
            if (!this.pawn.mindState.inspirationHandler.Inspired)
                return this;
            Inspiration i = this.pawn.mindState.inspirationHandler.CurState;
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredNonDisabledWorkTypes ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                    return add(0.4f, "YouDoYouPriorityInspired".TranslateSimple());
            }
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredAnyNonDisabledWorkType ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                    return add(0.4f, "YouDoYouPriorityInspired".TranslateSimple());
            }
            return this;
        }

        private Priority considerThoughts()
        {
            List<Thought> thoughts = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            foreach (Thought thought in thoughts)
            {
                if (thought.def.defName == "NeedFood")
                {
                    if (workTypeDef.defName == COOKING)
                    {
                        return add(-0.01f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".TranslateSimple());
                    }
                    if (workTypeDef.defName == HUNTING || workTypeDef.defName == PLANT_CUTTING)
                    {
                        return add(-0.005f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".TranslateSimple());
                    }
                    return add(0.005f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerNeedingWarmClothes()
        {
            if (this.workTypeDef.defName == TAILORING && this.mapComp.NeedWarmClothes)
            {
                return add(0.2f, "YouDoYouPriorityNeedWarmClothes".TranslateSimple());
            }
            return this;
        }

        private Priority considerColonistLeftUnburied()
        {
            if (this.mapComp.AlertColonistLeftUnburied && (this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT))
            {
                return add(0.4f, "AlertColonistLeftUnburied".TranslateSimple());
            }
            return this;
        }

        private Priority considerBored()
        {
            return this.alwaysDoIf(pawn.mindState.IsIdle, "YouDoYouPriorityBored".TranslateSimple());
        }

        private Priority considerHasHuntingWeapon()
        {
            if (!YouDoYou_Settings.ConsiderHasHuntingWeapon)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != HUNTING)
                {
                    return this;
                }
                return neverDoIf(!WorkGiver_HunterHunt.HasHuntingWeapon(pawn), "YouDoYouPriorityNoHuntingWeapon".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Logger.Error(pawn.Name + " could not consider has hunting weapon to adjust " + workTypeDef.defName);
                Logger.Message(err.ToString());
                Logger.Message("this consideration will be disabled in the mod settings to avoid future errors");
                YouDoYou_Settings.ConsiderHasHuntingWeapon = false;
                return this;
            }
        }

        private Priority considerBrawlersNotHunting()
        {
            if (!YouDoYou_Settings.ConsiderBrawlersNotHunting)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != HUNTING)
                {
                    return this;
                }
                return neverDoIf(this.pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")), "YouDoYouPriorityBrawler".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Logger.Error(pawn.Name + " could not consider brawlers can hunt to adjust " + workTypeDef.defName);
                Logger.Message(err.ToString());
                Logger.Message("this consideration will be disabled in the mod settings to avoid future errors");
                YouDoYou_Settings.ConsiderBrawlersNotHunting = false;
                return this;
            }
        }

        private Priority considerCompletingTask()
        {
            if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn.CurJob.workGiverDef.workType == workTypeDef)
            {
                return this

                    // pawns should not stop doing the work they are currently
                    // doing
                    .alwaysDo("YouDoYouPriorityCurrentlyDoing".TranslateSimple())

                    // pawns prefer the work they are current doing
                    .multiply(1.8f, "YouDoYouPriorityCurrentlyDoing".TranslateSimple())

                    ;
            }
            return this;
        }

        private Priority considerMovementSpeed()
        {
            try
            {
                if (YouDoYou_Settings.ConsiderMovementSpeed == 0.0f)
                {
                    return this;
                }
                return this.multiply(
                    (
                        YouDoYou_Settings.ConsiderMovementSpeed
                            * 0.25f
                            * this.pawn.GetStatValue(StatDefOf.MoveSpeed, true)
                    ),
                    "YouDoYouPriorityMovementSpeed".TranslateSimple()
                );
            }
            catch (System.Exception err)
            {
                Logger.Message(pawn.Name + " could not consider movement speed to adjust " + workTypeDef.defName);
                Logger.Message(err.ToString());
                Logger.Message("this consideration will be disabled in the mod settings to avoid future errors");
                YouDoYou_Settings.ConsiderMovementSpeed = 0.0f;
                return this;
            }
        }

        private Priority considerCarryingCapacity()
        {
            var _baseCarryingCapacity = 75.0f;
            if (workTypeDef.defName != HAULING && workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            float _carryingCapacity = this.pawn.GetStatValue(StatDefOf.CarryingCapacity, true);
            if (_carryingCapacity >= _baseCarryingCapacity)
            {
                return this;
            }
            return this.multiply(_carryingCapacity / _baseCarryingCapacity, "YouDoYouPriorityCarryingCapacity".TranslateSimple());
        }

        private Priority considerPassion()
        {
            var relevantSkills = workTypeDef.relevantSkills;

            for (int i = 0; i < relevantSkills.Count; i++)
            {
                float x;
                switch (pawn.skills.GetSkill(relevantSkills[i]).passion)
                {
                    case Passion.None:
                        continue;
                    case Passion.Major:
                        x = pawn.needs.mood.CurLevel * 0.5f / relevantSkills.Count;
                        add(x, "YouDoYouPriorityMajorPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Passion.Minor:
                        x = pawn.needs.mood.CurLevel * 0.25f / relevantSkills.Count;
                        add(x, "YouDoYouPriorityMinorPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    default:
                        considerInterest(pawn, relevantSkills[i], relevantSkills.Count, workTypeDef);
                        continue;
                }
            }
            return this;
        }

        private Priority considerInterest(Pawn pawn, SkillDef skillDef, int skillCount, WorkTypeDef workTypeDef)
        {
            if (!YouDoYou_WorldComponent.HasInterestsFramework())
            {
                return this;
            }
            SkillRecord skillRecord = pawn.skills.GetSkill(skillDef);
            float x;
            string interest;
            try
            {
                interest = YouDoYou_WorldComponent.InterestsStrings[(int)skillRecord.passion];

            }
            catch (System.Exception)
            {
                Logger.Message("could not find interest for index " + ((int)skillRecord.passion).ToString());
                return this;
            }
            switch (interest)
            {
                case "DMinorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.25f / skillCount;
                    return add(x, "YouDoYouPriorityMinorAversionTo".TranslateSimple() + " " + skillDef.skillLabel);
                case "DMajorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.5f / skillCount;
                    return add(x, "YouDoYouPriorityMajorAversionTo".TranslateSimple() + " " + skillDef.skillLabel);
                case "DCompulsion":
                    List<Thought> allThoughts = new List<Thought>();
                    pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
                    foreach (var thought in allThoughts)
                    {
                        if (thought.def.defName == "CompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive itch":
                                    x = 0.2f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveItch".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive need":
                                    x = 0.4f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveNeed".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive obsession":
                                    x = 0.6f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveObsession".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Logger.Debug("could not read compulsion label");
                                    return this;
                            }
                        }
                        if (thought.def.defName == "NeuroticCompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive itch":
                                    x = 0.3f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveItch".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive demand":
                                    x = 0.6f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveDemand".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive withdrawal":
                                    x = 0.9f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveWithdrawl".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Logger.Debug("could not read compulsion label");
                                    return this;
                            }
                        }
                        if (thought.def.defName == "VeryNeuroticCompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive yearning":
                                    x = 0.4f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveYearning".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive tantrum":
                                    x = 0.8f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveTantrum".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive hysteria":
                                    x = 1.2f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveHysteria".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Logger.Debug("could not read compulsion label");
                                    return this;
                            }
                        }
                    }
                    return this;
                case "DInvigorating":
                    x = 0.1f / skillCount;
                    return add(x, "YouDoYouPriorityInvigorating".TranslateSimple() + " " + skillDef.skillLabel);
                case "DInspiring":
                    return this;
                case "DStagnant":
                    return this;
                case "DForgetful":
                    return this;
                case "DVocalHatred":
                    return this;
                case "DNaturalGenius":
                    return this;
                case "DBored":
                    if (pawn.mindState.IsIdle)
                    {
                        return this;
                    }
                    return neverDo("YouDoYouPriorityBoredBy".TranslateSimple() + " " + skillDef.skillLabel);
                case "DAllergic":
                    foreach (var hediff in pawn.health.hediffSet.GetHediffs<Hediff>())
                    {
                        if (hediff.def.defName == "DAllergicReaction")
                        {
                            switch (hediff.CurStage.label)
                            {
                                case "initial":
                                    x = -0.2f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionInitial".TranslateSimple() + " " + skillDef.skillLabel);
                                case "itching":
                                    x = -0.5f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionItching".TranslateSimple() + " " + skillDef.skillLabel);
                                case "sneezing":
                                    x = -0.8f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionSneezing".TranslateSimple() + " " + skillDef.skillLabel);
                                case "swelling":
                                    x = -1.1f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionSwelling".TranslateSimple() + " " + skillDef.skillLabel);
                                case "anaphylaxis":
                                    return neverDo("YouDoYouPriorityReactionAnaphylaxis".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    break;
                            }
                        }
                        x = 0.1f / skillCount;
                        return add(x, "YouDoYouPriorityNoReaction".TranslateSimple() + " " + skillDef.skillLabel);
                    }
                    return this;
                default:
                    Logger.Debug("did not recognize interest: " + skillRecord.passion.ToString());
                    return this;
            }
        }

        private Priority considerDownedColonists()
        {
            if (pawn.Downed)
            {
                if (workTypeDef.defName == "Patient" || workTypeDef.defName == PATIENT_BED_REST)
                {
                    return alwaysDo("YouDoYouPriorityPawnDowned".TranslateSimple()).set(1.0f, "YouDoYouPriorityPawnDowned".TranslateSimple());
                }
                return neverDo("YouDoYouPriorityPawnDowned".TranslateSimple());
            }
            if (workTypeDef.defName == DOCTOR)
            {
                return add(mapComp.PercentPawnsDowned, "YouDoYouPriorityOtherPawnsDowned".TranslateSimple());
            }
            return this;
        }

        private Priority considerColonyPolicy()
        {
            try
            {
                this.add(YouDoYou_Settings.globalWorkAdjustments[this.workTypeDef.defName], "YouDoYouPriorityColonyPolicy".TranslateSimple());
            }
            catch (System.Exception)
            {
                YouDoYou_Settings.globalWorkAdjustments[this.workTypeDef.defName] = 0.0f;
            }
            return this;
        }

        private Priority considerRefueling()
        {
            if (workTypeDef.defName != HAULING && workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            if (mapComp.RefuelNeededNow)
            {
                return this.add(0.25f, "YouDoYouPriorityRefueling".TranslateSimple());
            }
            if (mapComp.RefuelNeeded)
            {
                return this.add(0.10f, "YouDoYouPriorityRefueling".TranslateSimple());
            }
            return this;
        }

        private Priority considerFire()
        {
            if (mapComp.HomeFire)
            {
                if (workTypeDef.defName != FIREFIGHTER)
                {
                    return add(-0.2f, "YouDoYouPriorityFireInHomeArea".TranslateSimple());
                }
                return set(1.0f, "YouDoYouPriorityFireInHomeArea".TranslateSimple());
            }
            if (mapComp.MapFires > 0 && workTypeDef.defName == FIREFIGHTER)
            {
                return add(Mathf.Clamp01(mapComp.MapFires * 0.01f), "YouDoYouPriorityFireOnMap".TranslateSimple());
            }
            return this;
        }

        private Priority considerBuildingImmunity()
        {
            try
            {
                if (!pawn.health.hediffSet.HasImmunizableNotImmuneHediff())
                    return this;
                if (workTypeDef.defName == PATIENT_BED_REST)
                    return add(0.4f, "YouDoYouPriorityBuildingImmunity".TranslateSimple());
                if (workTypeDef.defName == "Patient")
                    return this;
                return add(-0.2f, "YouDoYouPriorityBuildingImmunity".TranslateSimple());
            }
            catch
            {
                Logger.Debug("could not consider pawn building immunity");
                return this;
            }
        }

        private Priority considerColonistsNeedingTreatment()
        {
            // this pawn is the one who needs treatment
            if (pawn.health.HasHediffsNeedingTend())
            {
                if (workTypeDef.defName == PATIENT || workTypeDef.defName == PATIENT_BED_REST)
                {
                    // patient and bed rest are activated and set to 100%
                    return this
                        .alwaysDo("YouDoYouPriorityNeedTreatment".TranslateSimple())
                        .set(1.0f, "YouDoYouPriorityNeedTreatment".TranslateSimple())
                        ;
                }
                if (workTypeDef.defName == DOCTOR && pawn.playerSettings.selfTend)
                {
                    // this pawn can self tend, so activate doctor skill and set
                    // to 100%
                    return this
                        .alwaysDo("YouDoYouPriorityNeedTreatmentSelfTend".TranslateSimple())
                        .set(1.0f, "YouDoYouPriorityNeedTreatmentSelfTend".TranslateSimple())
                        ;
                }
                return neverDo("YouDoYouPriorityNeedTreatment".TranslateSimple());
            }

            // some other pawn needs treatment - increase doctor priority
            if (workTypeDef.defName == DOCTOR)
            {
                // increase the doctor priority by the percentage of pawns
                // needing treatment
                //
                // so if 25% of the colony is injured, doctoring for all
                // non-injured pawns will increase by 25%
                return add(mapComp.PercentPawnsNeedingTreatment, "YouDoYouPriorityOthersNeedTreatment".TranslateSimple());
            }
            return this;
        }

        private Priority considerHealth()
        {
            if (this.workTypeDef.defName == PATIENT || this.workTypeDef.defName == PATIENT_BED_REST)
            {
                return add(1 - Mathf.Pow(this.pawn.health.summaryHealth.SummaryHealthPercent, 7.0f), "YouDoYouPriorityHealth".TranslateSimple());
            }
            return multiply(this.pawn.health.summaryHealth.SummaryHealthPercent, "YouDoYouPriorityHealth".TranslateSimple());
        }

        private Priority considerFoodPoisoning()
        {
            if (YouDoYou_Settings.ConsiderFoodPoisoning == 0.0f)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != CLEANING && this.workTypeDef.defName != COOKING)
                {
                    return this;
                }

                var adjustment = 0.0f;
                var found = false;
                foreach (Region region in pawn.GetRoom().Regions)
                {
                    foreach (Thing thing in region.ListerThings.AllThings)
                    {
                        Building building = thing as Building;
                        if (building == null)
                        {
                            continue;
                        }
                        if (building.Faction != Faction.OfPlayer)
                        {
                            continue;
                        }
                        if (building.def.building.isMealSource)
                        {
                            adjustment =
                                (
                                    YouDoYou_Settings.ConsiderFoodPoisoning
                                    * 20.0f
                                    * pawn.GetRoom().GetStat(RoomStatDefOf.FoodPoisonChance)
                                );
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (this.workTypeDef.defName == CLEANING)
                {
                    return add(adjustment, "YouDoYouPriorityFilthyCookingArea".TranslateSimple());
                }
                if (this.workTypeDef.defName == COOKING)
                {
                    return add(-adjustment, "YouDoYouPriorityFilthyCookingArea".TranslateSimple());
                }
                return this;
            }
            catch (System.Exception err)
            {
                Logger.Error(pawn.Name + " could not consider food poisoning to adjust " + workTypeDef.defName);
                Logger.Message(err.ToString());
                Logger.Message("this consideration will be disabled in the mod settings to avoid future errors");
                YouDoYou_Settings.ConsiderFoodPoisoning = 0.0f;
                return this;
            }
        }

        private Priority considerOwnRoom()
        {
            if (YouDoYou_Settings.ConsiderOwnRoom == 0.0f)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != CLEANING)
                {
                    return this;
                }
                var room = pawn.GetRoom();
                var isPawnsRoom = false;
                foreach (Pawn owner in room.Owners)
                {
                    if (pawn == owner)
                    {
                        isPawnsRoom = true;
                        break;
                    }
                }
                if (!isPawnsRoom)
                {
                    return this;
                }
                return multiply(YouDoYou_Settings.ConsiderOwnRoom * 2.0f, "YouDoYouPriorityOwnRoom".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Logger.Message(pawn.Name + " could not consider being in own room to adjust " + workTypeDef.defName);
                Logger.Message(err.ToString());
                Logger.Message("this consideration will be disabled in the mod settings to avoid future errors");
                YouDoYou_Settings.ConsiderOwnRoom = 0.0f;
                return this;
            }
        }

        private Priority considerIsAnyoneElseDoing()
        {
            float pawnSkill = this.pawn.skills.AverageOfRelevantSkillsFor(this.workTypeDef);
            foreach (Pawn other in this.pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == this.pawn)
                {
                    continue;
                }
                if (!other.Awake() || other.Downed || other.Dead)
                {
                    continue;
                }
                if (other.workSettings.GetPriority(this.workTypeDef) != 0)
                {
                    return this; // someone else is doing
                }
            }
            return this.alwaysDo("YouDoYouPriorityNoOneElseDoing".TranslateSimple());
        }

        private Priority considerInjuredPets()
        {
            if (workTypeDef.defName == DOCTOR)
            {
                int n = mapComp.NumPawns;
                if (n == 0)
                {
                    return this;
                }
                float numPetsNeedingTreatment = mapComp.NumPetsNeedingTreatment;
                return add(UnityEngine.Mathf.Clamp01(numPetsNeedingTreatment / ((float)n)) * 0.5f, "YouDoYouPriorityPetsInjured".TranslateSimple());
            }
            return this;
        }

        private Priority considerLowFood()
        {
            if (this.mapComp.TotalFood < 4f * (float)this.mapComp.NumPawns)
            {
                if (this.workTypeDef.defName == COOKING)
                {
                    return this.add(0.4f, "YouDoYouPriorityLowFood".TranslateSimple());
                }
                if (this.workTypeDef.defName == HUNTING || this.workTypeDef.defName == PLANT_CUTTING)
                {
                    return this.add(0.2f, "YouDoYouPriorityLowFood".TranslateSimple());
                }
                if ((this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT)
                    && this.pawn.Map.GetComponent<YouDoYou_MapComponent>().ThingsDeteriorating)
                {
                    return this.add(0.15f, "YouDoYouPriorityLowFood".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerAteRawFood()
        {
            if (this.workTypeDef.defName != "Cooking")
            {
                return this;
            }

            List<Thought> allThoughts = new List<Thought>();
            this.pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
            for (int i = 0; i < allThoughts.Count; i++)
            {
                Thought thought = allThoughts[i];
                if (thought.def.defName == "AteRawFood")
                {
                    if (0.6f > value)
                    {
                        return this.set(0.6f, "YouDoYouPriorityAteRawFood".TranslateSimple());
                    }
                }
            }
            return this;
        }

        private Priority considerThingsDeteriorating()
        {
            if (this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT)
            {
                if (this.pawn.Map.GetComponent<YouDoYou_MapComponent>().ThingsDeteriorating)
                {
                    return this.add(0.2f, "YouDoYouPriorityThingsDeteriorating".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerPlantsBlighted()
        {
            if (this.workTypeDef.defName != PLANT_CUTTING)
            {
                return this;
            }
            if (this.mapComp.PlantsBlighted)
            {
                return this.add(0.4f, "YouDoYouPriorityBlight".TranslateSimple());
            }
            return this;
        }

        private Priority considerBeautyExpectations()
        {
            if (this.workTypeDef.defName != CLEANING && this.workTypeDef.defName != HAULING && this.workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            try
            {
                float e = expectationGrid[ExpectationsUtility.CurrentExpectationFor(this.pawn).defName][this.pawn.needs.beauty.CurCategory];
                if (e < 0.2f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsExceeded".TranslateSimple());
                }
                if (e < 0.4f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsMet".TranslateSimple());
                }
                if (e < 0.6f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsUnmet".TranslateSimple());
                }
                if (e < 0.8f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsLetDown".TranslateSimple());
                }
                return this.set(e, "YouDoYouPriorityExpectionsIgnored".TranslateSimple());
            }
            catch
            {
                return this.set(0.3f, "YouDoYouPriorityBeautyDefault".TranslateSimple());
            }
        }

        private Priority considerRelevantSkills()
        {
            float _badSkillCutoff = Mathf.Min(3f, this.mapComp.NumPawns);
            float _goodSkillCutoff = _badSkillCutoff + (20f - _badSkillCutoff) / 2f;
            float _greatSkillCutoff = _goodSkillCutoff + (20f - _goodSkillCutoff) / 2f;
            float _excellentSkillCutoff = _greatSkillCutoff + (20f - _greatSkillCutoff) / 2f;

            float _avg = this.pawn.skills.AverageOfRelevantSkillsFor(this.workTypeDef);
            if (_avg >= _excellentSkillCutoff)
            {
                return this.set(0.9f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _greatSkillCutoff)
            {
                return this.set(0.7f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _goodSkillCutoff)
            {
                return this.set(0.5f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _badSkillCutoff)
            {
                return this.set(0.3f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".TranslateSimple(), _avg));
            }
            return this.set(0.1f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".TranslateSimple(), _avg));
        }

        private bool notInHomeArea(Pawn pawn)
        {
            return !this.pawn.Map.areaManager.Home[pawn.Position];
        }

        private static Dictionary<string, Dictionary<BeautyCategory, float>> expectationGrid =
            new Dictionary<string, Dictionary<BeautyCategory, float>>
            {
                {
                    "ExtremelyLow", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.3f },
                            { BeautyCategory.VeryUgly, 0.2f },
                            { BeautyCategory.Ugly, 0.1f },
                            { BeautyCategory.Neutral, 0.0f },
                            { BeautyCategory.Pretty, 0.0f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "VeryLow", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.5f },
                            { BeautyCategory.VeryUgly, 0.3f },
                            { BeautyCategory.Ugly, 0.2f },
                            { BeautyCategory.Neutral, 0.1f },
                            { BeautyCategory.Pretty, 0.0f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "Low", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.7f },
                            { BeautyCategory.VeryUgly, 0.5f },
                            { BeautyCategory.Ugly, 0.3f },
                            { BeautyCategory.Neutral, 0.2f },
                            { BeautyCategory.Pretty, 0.1f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "Moderate", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.8f },
                            { BeautyCategory.VeryUgly, 0.7f },
                            { BeautyCategory.Ugly, 0.5f },
                            { BeautyCategory.Neutral, 0.3f },
                            { BeautyCategory.Pretty, 0.2f },
                            { BeautyCategory.VeryPretty, 0.1f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "High", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.9f },
                            { BeautyCategory.VeryUgly, 0.8f },
                            { BeautyCategory.Ugly, 0.7f },
                            { BeautyCategory.Neutral, 0.5f },
                            { BeautyCategory.Pretty, 0.3f },
                            { BeautyCategory.VeryPretty, 0.2f },
                            { BeautyCategory.Beautiful, 0.1f },
                        }
                },
                {
                    "SkyHigh", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 0.9f },
                            { BeautyCategory.Ugly, 0.8f },
                            { BeautyCategory.Neutral, 0.7f },
                            { BeautyCategory.Pretty, 0.5f },
                            { BeautyCategory.VeryPretty, 0.3f },
                            { BeautyCategory.Beautiful, 0.2f },
                        }
                },
                {
                    "Noble", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 1.0f },
                            { BeautyCategory.Ugly, 0.9f },
                            { BeautyCategory.Neutral, 0.8f },
                            { BeautyCategory.Pretty, 0.7f },
                            { BeautyCategory.VeryPretty, 0.5f },
                            { BeautyCategory.Beautiful, 0.3f },
                        }
                },
                {
                    "Royal", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 1.0f },
                            { BeautyCategory.Ugly, 1.0f },
                            { BeautyCategory.Neutral, 0.9f },
                            { BeautyCategory.Pretty, 0.8f },
                            { BeautyCategory.VeryPretty, 0.7f },
                            { BeautyCategory.Beautiful, 0.5f },
                        }
                },
            };
    }
}

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
        private readonly string patientBedRest = "PatientBedRest";
        private readonly string cleaning = "Cleaning";
        private readonly string hauling = "Hauling";
        private readonly string haulingUrgent = "HaulingUrgent";
        private readonly string hunting = "Hunting";
        private readonly string basicWorker = "BasicWorker";
        private readonly string plantcutting = "PlantCutting";


        public Priority(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, bool freePawn)
        {
            this.pawn = pawn;
            this.workTypeDef = workTypeDef;
            this.adjustmentStrings = new List<string> { };
            this.mapComp = pawn.Map.GetComponent<YouDoYou_MapComponent>();
            this.worldComp = Find.World.GetComponent<YouDoYou_WorldComponent>();
            if (freePawn)
            {
                this.set(0.2f, "YouDoYouPriorityGlobalDefault".Translate()).compute(settings);
            }
            else
            {
                int p = pawn.workSettings.GetPriority(workTypeDef);
                if (p == 0)
                {
                    this.set(0.0f, "YouDoYouPriorityNoFreeWill".Translate());
                }
                else
                {
                    this.set((100f - onePriorityWidth * (p - 1)) / 100f, "YouDoYouPriorityNoFreeWill".Translate());
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
            return this.value.CompareTo(p.value);
        }

        private Priority compute(YouDoYou_Settings settings)
        {
            if (pawn == null)
            {
                Logger.Error("pawn is null");
                return this;
            }
            if (workTypeDef == null)
            {
                Logger.Error("workTypeDef is null");
                return this;
            }
            if (settings == null)
            {
                Logger.Error("settings is null");
                return this;
            }
            this.enabled = false;
            this.disabled = false;
            if (this.pawn.GetDisabledWorkTypes(true).Contains(this.workTypeDef))
            {
                return this.neverDo("YouDoYouPriorityPermanentlyDisabled".Translate());
            }
            float x;
            switch (this.workTypeDef.defName)
            {
                case "Firefighter":
                    return this
                        .set(0.2f, "YouDoYouPriorityFirefightingDefault".Translate())
                        .considerMovementSpeed()
                        .neverDoIf(this.pawn.Downed, "YouDoYouPriorityPawnDowned".Translate())
                        .considerFire(this.pawn, this.workTypeDef)
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        ;

                case "Patient":
                    return this
                        .set(0.0f, "YouDoYouPriorityPatientDefault".Translate())
                        .alwaysDo("YouDoYouPriorityPatientDefault".Translate())
                        .considerMovementSpeed()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        ;

                case "PatientBedRest":
                    x = 1 - Mathf.Pow(this.pawn.health.summaryHealth.SummaryHealthPercent, 7.0f);
                    return this
                        .set(0.0f, "YouDoYouPriorityBedrestDefault".Translate())
                        .alwaysDo("YouDoYouPriorityBedrestDefault".Translate())
                        .add(x, "YouDoYouPriorityHealth".Translate())
                        .considerMovementSpeed()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .alwaysDoIf(this.pawn.mindState.IsIdle, "YouDoYouPriorityBored".Translate())
                        .considerDownedColonists()
                        ;

                case "BasicWorker":
                    x = this.pawn.health.summaryHealth.SummaryHealthPercent;
                    return this
                        .set(0.5f, "YouDoYouPriorityBasicWorkDefault".Translate())
                        .considerMovementSpeed()
                        .considerThoughts()
                        .considerNeedingWarmClothes()
                        .multiply(x, "YouDoYouPriorityHealth".Translate())
                        .alwaysDoIf(this.pawn.mindState.IsIdle, "YouDoYouPriorityBored".Translate())
                        .neverDoIf(this.pawn.Downed, "YouDoYouPriorityPawnDowned".Translate())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        ;

                case "Hauling":
                case "HaulingUrgent":
                    x = this.pawn.health.summaryHealth.SummaryHealthPercent;
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerThoughts()
                        .considerNeedingWarmClothes()
                        .multiply(x, "YouDoYouPriorityHealth".Translate())
                        .considerThingsDeteriorating()
                        .alwaysDoIf(this.pawn.mindState.IsIdle, "YouDoYouPriorityBored".Translate())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        ;

                case "Cleaning":
                    x = this.pawn.health.summaryHealth.SummaryHealthPercent;
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerThoughts()
                        .considerNeedingWarmClothes()
                        .multiply(x, "YouDoYouPriorityHealth".Translate())
                        .alwaysDoIf(this.pawn.mindState.IsIdle, "YouDoYouPriorityBored".Translate())
                        .neverDoIf(notInHomeArea(this.pawn), "YouDoYouPriorityNotInHomeArea".Translate())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        ;

                default:
                    return this
                        .considerRelevantSkills()
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerAteRawFood()
                        .considerPlantsBlighted()
                        .considerBored()
                        .considerHunting(settings.brawlersCanHunt)
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
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
                string str = string.Format("Priority{0}", p).Translate();
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
                string text = string.Format("{0} ({1})", "YouDoYouPriorityEnabled".Translate(), s);
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
                string text = string.Format("{0} ({1})", "YouDoYouPriorityDisabled".Translate(), s);
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

        // raise this two steps if inspired
        private Priority considerInspiration()
        {
            if (!this.pawn.mindState.inspirationHandler.Inspired)
                return this;
            Inspiration i = this.pawn.mindState.inspirationHandler.CurState;
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredNonDisabledWorkTypes ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                    return add(0.4f, "YouDoYouPriorityInspired".Translate());
            }
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredAnyNonDisabledWorkType ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                    return add(0.4f, "YouDoYouPriorityInspired".Translate());
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
                    if (workTypeDef.defName == "Cooking")
                    {
                        return add(-0.01f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".Translate());
                    }
                    if (workTypeDef.defName == hunting || workTypeDef.defName == "PlantCutting")
                    {
                        return add(-0.005f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".Translate());
                    }
                    return add(0.005f * thought.CurStage.baseMoodEffect, "YouDoYouPriorityHungerLevel".Translate());
                }
            }
            return this;
        }

        private Priority considerNeedingWarmClothes()
        {
            if (this.workTypeDef.defName == "Tailoring" && this.worldComp.AlertNeedWarmClothes != null)
            {
                return add(0.2f, "YouDoYouPriorityNeedWarmClothes".Translate());
            }
            return this;
        }

        private Priority considerBored()
        {
            return this.alwaysDoIf(pawn.mindState.IsIdle, "YouDoYouPriorityBored".Translate());
        }


        private Priority considerHunting(bool brawlersCanHunt)
        {
            if (this.workTypeDef.defName != hunting)
            {
                return this;
            }
            bool isBrawler = this.pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")) && !brawlersCanHunt;
            return this
                .neverDoIf(isBrawler, "YouDoYouPriorityBrawler".Translate())
                .neverDoIf(!WorkGiver_HunterHunt.HasHuntingWeapon(pawn), "YouDoYouPriorityNoHuntingWeapon".Translate());
        }

        private Priority considerCompletingTask()
        {
            if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn.CurJob.workGiverDef.workType == workTypeDef)
            {
                return alwaysDo("YouDoYouPriorityCurrentlyDoing".Translate());
            }
            return this;
        }

        private Priority considerMovementSpeed()
        {
            if (workTypeDef.defName == "Patient")
            {
                return this;
            }
            if (workTypeDef.defName == patientBedRest)
            {
                return this;
            }
            float movementSpeed = this.pawn.GetStatValue(StatDefOf.MoveSpeed, true);
            if (workTypeDef.defName == hauling || workTypeDef.defName == haulingUrgent)
            {
                return this.multiply(movementSpeed / 4f, "YouDoYouPriorityMovementSpeed".Translate());
            }
            if (workTypeDef.defName == hunting)
            {
                return this.multiply(movementSpeed / 4f, "YouDoYouPriorityMovementSpeed".Translate());
            }
            if (workTypeDef.defName == basicWorker)
            {
                return this.multiply(movementSpeed / 4f, "YouDoYouPriorityMovementSpeed".Translate());
            }
            return this;
        }

        // raise this based on passion
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
                        add(x, "YouDoYouPriorityMajorPassionFor".Translate() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Passion.Minor:
                        x = pawn.needs.mood.CurLevel * 0.25f / relevantSkills.Count;
                        add(x, "YouDoYouPriorityMinorPassionFor".Translate() + " " + relevantSkills[i].skillLabel);
                        continue;
                    default:
                        considerInterest(pawn, relevantSkills[i], relevantSkills.Count, workTypeDef);
                        continue;
                }
            }
            return this;
        }

        // raise this based on interests (from Interests mod)
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
                Logger.Error("could not find interest for index " + ((int)skillRecord.passion).ToString());
                return this;
            }
            switch (interest)
            {
                case "DMinorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.25f / skillCount;
                    return add(x, "YouDoYouPriorityMinorAversionTo".Translate() + " " + skillDef.skillLabel);
                case "DMajorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.5f / skillCount;
                    return add(x, "YouDoYouPriorityMajorAversionTo".Translate() + " " + skillDef.skillLabel);
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
                                    return add(x, "YouDoYouPriorityCompulsiveItch".Translate() + " " + skillDef.skillLabel);
                                case "compulsive need":
                                    x = 0.4f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveNeed".Translate() + " " + skillDef.skillLabel);
                                case "compulsive obsession":
                                    x = 0.6f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveObsession".Translate() + " " + skillDef.skillLabel);
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
                                    return add(x, "YouDoYouPriorityCompulsiveItch".Translate() + " " + skillDef.skillLabel);
                                case "compulsive demand":
                                    x = 0.6f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveDemand".Translate() + " " + skillDef.skillLabel);
                                case "compulsive withdrawal":
                                    x = 0.9f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveWithdrawl".Translate() + " " + skillDef.skillLabel);
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
                                    return add(x, "YouDoYouPriorityCompulsiveYearning".Translate() + " " + skillDef.skillLabel);
                                case "compulsive tantrum":
                                    x = 0.8f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveTantrum".Translate() + " " + skillDef.skillLabel);
                                case "compulsive hysteria":
                                    x = 1.2f / skillCount;
                                    return add(x, "YouDoYouPriorityCompulsiveHysteria".Translate() + " " + skillDef.skillLabel);
                                default:
                                    Logger.Debug("could not read compulsion label");
                                    return this;
                            }
                        }
                    }
                    return this;
                case "DInvigorating":
                    x = 0.1f / skillCount;
                    return add(x, "YouDoYouPriorityInvigorating".Translate() + " " + skillDef.skillLabel);
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
                    return neverDo("YouDoYouPriorityBoredBy".Translate() + " " + skillDef.skillLabel);
                case "DAllergic":
                    foreach (var hediff in pawn.health.hediffSet.GetHediffs<Hediff>())
                    {
                        if (hediff.def.defName == "DAllergicReaction")
                        {
                            switch (hediff.CurStage.label)
                            {
                                case "initial":
                                    x = -0.2f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionInitial".Translate() + " " + skillDef.skillLabel);
                                case "itching":
                                    x = -0.5f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionItching".Translate() + " " + skillDef.skillLabel);
                                case "sneezing":
                                    x = -0.8f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionSneezing".Translate() + " " + skillDef.skillLabel);
                                case "swelling":
                                    x = -1.1f / skillCount;
                                    return add(x, "YouDoYouPriorityReactionSwelling".Translate() + " " + skillDef.skillLabel);
                                case "anaphylaxis":
                                    return neverDo("YouDoYouPriorityReactionAnaphylaxis".Translate() + " " + skillDef.skillLabel);
                                default:
                                    break;
                            }
                        }
                        x = 0.1f / skillCount;
                        return add(x, "YouDoYouPriorityNoReaction".Translate() + " " + skillDef.skillLabel);
                    }
                    return this;
                default:
                    Logger.Debug("did not recognize interest: " + skillRecord.passion.ToString());
                    return this;
            }
        }

        // raise this based on downed colonists
        private Priority considerDownedColonists()
        {
            if (pawn.Downed)
            {
                if (workTypeDef.defName == "Patient" || workTypeDef.defName == patientBedRest)
                {
                    return alwaysDo("YouDoYouPriorityPawnDowned".Translate()).set(1.0f, "YouDoYouPriorityPawnDowned".Translate());
                }
                return neverDo("YouDoYouPriorityPawnDowned".Translate());
            }
            if (workTypeDef.defName == "Doctor")
            {
                return add(mapComp.PercentPawnsDowned, "YouDoYouPriorityOtherPawnsDowned".Translate());
            }
            return this;
        }

        private Priority considerFire(Pawn pawn, WorkTypeDef workTypeDef)
        {
            List<Thing> list = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            int fires = 0;
            for (int j = 0; j < list.Count; j++)
            {
                fires++;
                Thing thing = list[j];
                if (pawn.Map.areaManager.Home[thing.Position] && !thing.Position.Fogged(thing.Map))
                {
                    if (workTypeDef.defName != "Firefighter")
                    {
                        return add(-0.2f, "YouDoYouPriorityFireInHomeArea".Translate());
                    }
                    return set(1.0f, "YouDoYouPriorityFireInHomeArea".Translate());
                }
            }
            if (fires > 0)
            {
                return add(fires * 0.01f, "YouDoYouPriorityFireOnMap".Translate());
            }
            return alwaysDo("YouDoYouPriorityFirefightingDefault".Translate());
        }

        private Priority considerBuildingImmunity()
        {
            try
            {
                if (!pawn.health.hediffSet.HasImmunizableNotImmuneHediff())
                    return this;
                if (workTypeDef.defName == patientBedRest)
                    return add(0.4f, "YouDoYouPriorityBuildingImmunity".Translate());
                if (workTypeDef.defName == "Patient")
                    return this;
                return add(-0.2f, "YouDoYouPriorityBuildingImmunity".Translate());
            }
            catch
            {
                Logger.Debug("could not consider pawn building immunity");
                return this;
            }
        }

        private Priority considerColonistsNeedingTreatment()
        {
            if (pawn.health.HasHediffsNeedingTend())
            {
                if (workTypeDef.defName == "Patient" || workTypeDef.defName == patientBedRest)
                {
                    return alwaysDo("YouDoYouPriorityNeedTreatment".Translate()).set(1.0f, "YouDoYouPriorityNeedTreatment".Translate());
                }
                return neverDo("YouDoYouPriorityNeedTreatment".Translate());
            }
            if (workTypeDef.defName == "Doctor")
            {
                return add(mapComp.PercentPawnsNeedingTreatment, "YouDoYouPriorityOthersNeedTreatment".Translate());
            }
            return this;
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
                if (other.workSettings.GetPriority(this.workTypeDef) != 0)
                {
                    return this; // someone else is doing
                }
            }
            return this.alwaysDo("YouDoYouPriorityNoOneElseDoing".Translate());
        }

        private Priority considerInjuredPets()
        {
            if (workTypeDef.defName == "Doctor")
            {
                int n = mapComp.NumPawns;
                if (n == 0)
                {
                    return this;
                }
                float numPetsNeedingTreatment = mapComp.NumPetsNeedingTreatment;
                return add(UnityEngine.Mathf.Clamp01(numPetsNeedingTreatment / ((float)n)) * 0.5f, "YouDoYouPriorityPetsInjured".Translate());
            }
            return this;
        }

        private Priority considerLowFood()
        {
            if (this.mapComp.TotalFood < 4f * (float)this.mapComp.NumPawns)
            {
                if (this.workTypeDef.defName == "Cooking")
                {
                    return this.add(0.4f, "YouDoYouPriorityLowFood".Translate());
                }
                if (this.workTypeDef.defName == hunting || this.workTypeDef.defName == "PlantCutting")
                {
                    return this.add(0.2f, "YouDoYouPriorityLowFood".Translate());
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
                        return this.set(0.6f, "YouDoYouPriorityAteRawFood".Translate());
                    }
                }
            }
            return this;
        }

        private Priority considerThingsDeteriorating()
        {
            if (this.pawn.Map.GetComponent<YouDoYou_MapComponent>().ThingsDeteriorating)
            {
                return this.add(0.2f, "YouDoYouPriorityThingsDeteriorating".Translate());
            }
            return this;
        }

        private Priority considerPlantsBlighted()
        {
            if (this.workTypeDef.defName != plantcutting)
            {
                return this;
            }
            if (this.pawn.Map.GetComponent<YouDoYou_MapComponent>().PlantsBlighted)
            {
                return this.add(0.4f, "YouDoYouPriorityBlight".Translate());
            }
            return this;
        }

        private Priority considerBeautyExpectations()
        {
            if (this.workTypeDef.defName != cleaning && this.workTypeDef.defName != hauling && this.workTypeDef.defName != haulingUrgent)
            {
                return this;
            }
            try
            {
                float e = expectationGrid[ExpectationsUtility.CurrentExpectationFor(this.pawn).defName][this.pawn.needs.beauty.CurCategory];
                if (e < 0.2f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsExceeded".Translate());
                }
                if (e < 0.4f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsMet".Translate());
                }
                if (e < 0.6f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsUnmet".Translate());
                }
                if (e < 0.8f)
                {
                    return this.set(e, "YouDoYouPriorityExpectionsLetDown".Translate());
                }
                return this.set(e, "YouDoYouPriorityExpectionsIgnored".Translate());
            }
            catch
            {
                return this.set(0.3f, "YouDoYouPriorityBeautyDefault".Translate());
            }
        }

        private Priority considerRelevantSkills()
        {
            float badSkillCutoff = Mathf.Min(3f, this.mapComp.NumPawns);
            float goodSkillCutoff = badSkillCutoff + (20f - badSkillCutoff) / 2f;
            float greatSkillCutoff = goodSkillCutoff + (20f - goodSkillCutoff) / 2f;
            float excellentSkillCutoff = greatSkillCutoff + (20f - greatSkillCutoff) / 2f;

            float avg = this.pawn.skills.AverageOfRelevantSkillsFor(this.workTypeDef);
            if (avg >= excellentSkillCutoff)
            {
                return this.set(0.9f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".Translate(), avg));
            }
            if (avg >= greatSkillCutoff)
            {
                return this.set(0.7f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".Translate(), avg));
            }
            if (avg >= goodSkillCutoff)
            {
                return this.set(0.5f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".Translate(), avg));
            }
            if (avg >= badSkillCutoff)
            {
                return this.set(0.3f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".Translate(), avg));
            }
            return this.set(0.1f, string.Format("{0} {1:f0}", "YouDoYouPrioritySkillLevel".Translate(), avg));
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

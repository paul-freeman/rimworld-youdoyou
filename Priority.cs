using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace YouDoYou
{
    class Priority
    {
        private float value;

        public Priority()
        {
            value = 0f;
        }

        public int toInt()
        {
            if (value >= 0.8) return 1;
            if (value >= 0.6) return 2;
            if (value >= 0.4) return 3;
            if (value >= 0.2) return 4;
            return 0;
        }

        public Priority Set(float v)
        {
            value = v;
            return this;
        }

        public Priority Add(float n)
        {
            value = UnityEngine.Mathf.Clamp(value + n, 0, 1);
            return this;
        }

        public Priority Multiply(float n)
        {
            value = UnityEngine.Mathf.Clamp(value * n, 0, 1);
            return this;
        }

        public bool Enabled()
        {
            return value >= 0.2f;
        }

        public bool Disabled()
        {
            return value < 0.2f;
        }

        public Priority AlwaysDoIf(bool cond)
        {
            if (Disabled() && cond)
            {
                value = 0.2f;
            }
            return this;
        }

        public Priority AlwaysDo()
        {
            return AlwaysDoIf(true);
        }

        public Priority NeverDoIf(bool cond)
        {
            if (cond && value >= 0.2f)
                value = 0.1f;
            return this;
        }

        public Priority NeverDo()
        {
            return NeverDoIf(true);
        }

        // consider if pawn is downed
        public Priority ConsiderDowned(Pawn pawn, WorkTypeDef workTypeDef)
        {
            if (!pawn.Downed)
                return this;
            Set(1);
            NeverDoIf(workTypeDef.defName != "Patient" && workTypeDef.defName != "PatientBedRest");
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
                    return Add(0.4f);
            }
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredAnyNonDisabledWorkType ?? new List<WorkTypeDef>())
            {
                if (workTypeDefA.defName == workTypeDefB.defName)
                    return Add(0.4f);
            }
            return this;
        }

        // raise this based on passion
        public Priority ConsiderPassion(Pawn pawn, WorkTypeDef workTypeDef)
        {
            var relevantSkills = workTypeDef.relevantSkills;

            for (int i = 0; i < relevantSkills.Count; i++)
            {
                switch (pawn.skills.GetSkill(relevantSkills[i]).passion)
                {
                    case Passion.None:
                        continue;
                    case Passion.Major:
                        Add(pawn.needs.mood.CurLevel * 0.5f / relevantSkills.Count);
                        continue;
                    case Passion.Minor:
                        Add(pawn.needs.mood.CurLevel * 0.25f / relevantSkills.Count);
                        continue;
                    default:
                        if (hasInterestsFramework())
                        {
                            ConsiderInterest(pawn, pawn.skills.GetSkill(relevantSkills[i]), relevantSkills.Count, workTypeDef);
                        }
                        continue;
                }
            }
            return this;
        }

        static public List<string> interestStrings = null;
        static public bool checkedForInterestsMod = false;

        // raise this based on interests (from Interests mod)
        public Priority ConsiderInterest(Pawn pawn, SkillRecord skillRecord, int skillCount, WorkTypeDef workTypeDef)
        {
            switch (interestStrings[(int)skillRecord.passion])
            {
                case "DMinorAversion":
                    return Add((1.0f - pawn.needs.mood.CurLevel) * -0.25f / skillCount);
                case "DMajorAversion":
                    return Add((1.0f - pawn.needs.mood.CurLevel) * -0.5f / skillCount);
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
                                    return Add(0.2f / skillCount);
                                case "compulsive need":
                                    return Add(0.4f / skillCount);
                                case "compulsive obsession":
                                    return Add(0.6f / skillCount);
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
                                    return Add(0.3f / skillCount);
                                case "compulsive demand":
                                    return Add(0.6f / skillCount);
                                case "compulsive withdrawal":
                                    return Add(0.9f / skillCount);
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
                                    return Add(0.4f / skillCount);
                                case "compulsive tantrum":
                                    return Add(0.8f / skillCount);
                                case "compulsive hysteria":
                                    return Add(1.2f / skillCount);
                                default:
                                    Logger.Debug("could not read compulsion label");
                                    return this;
                            }
                        }
                    }
                    return this;
                case "DInvigorating":
                    return Add(0.1f / skillCount);
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
                    if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn.CurJob.workGiverDef.workType == workTypeDef)
                    {
                        return Set(0.2f / skillCount);
                    }
                    return this;
                case "DAllergic":
                    foreach (var hediff in pawn.health.hediffSet.GetHediffs<Hediff>())
                    {
                        if (hediff.def.defName == "DAllergicReaction")
                        {
                            switch (hediff.CurStage.label)
                            {
                                case "initial":
                                    return Add(-0.2f / skillCount);
                                case "itching":
                                    return Add(-0.5f / skillCount);
                                case "sneezing":
                                    return Add(-0.8f / skillCount);
                                case "swelling":
                                    return Add(-1.1f / skillCount);
                                case "anaphylaxis":
                                    return NeverDo();
                                default:
                                    break;
                            }
                        }
                        return Add(0.1f / skillCount);
                    }
                    return this;
                default:
                    Logger.Debug("did not recognize interest: " + skillRecord.passion.ToString());
                    return this;
            }
        }

        private bool hasInterestsFramework()
        {
            if (checkedForInterestsMod)
            {
                return (interestStrings != null);
            }

            checkedForInterestsMod = true;
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "[D] Interests Framework"))
            {
                Logger.Message("YouDoYou: found [D] Interests Framework - will attempt to play nice");
                var interestsBaseT = AccessTools.TypeByName("DInterests.InterestBase");
                if (interestsBaseT == null)
                {
                    Logger.Error("did not find interestsBase");
                    return false;
                }
                Logger.Message("found interestsBase");

                var interestList = AccessTools.Field(interestsBaseT, "interestList").GetValue(interestsBaseT);
                if (interestList == null)
                {
                    Logger.Error("did not find interest list");
                    return false;
                }
                Logger.Message("found interest list");

                var interestListT = AccessTools.TypeByName("DInterests.InterestList");
                if (interestListT == null)
                {
                    Logger.Error("could not find interest list type");
                    return false;
                }
                Logger.Message("found interest list type");

                var countMethod = AccessTools.Method(interestListT.BaseType, "get_Count", null);
                if (countMethod == null)
                {
                    Logger.Error("could not find count method");
                    return false;
                }
                Logger.Message("found count method");

                var count = countMethod.Invoke(interestList, null);
                if (count == null)
                {
                    Logger.Error("could not get count");
                    return false;
                }
                Logger.Message("found count");

                var interestDefT = AccessTools.TypeByName("DInterests.InterestDef");
                if (interestDefT == null)
                {
                    Logger.Error("could not find interest def type");
                    return false;
                }
                Logger.Message("found interest def type");

                var getItem = AccessTools.Method(interestListT.BaseType, "get_Item");
                if (getItem == null)
                {
                    Logger.Error("coud not find get item method");
                    return false;
                }
                Logger.Message("found get item method");

                var defNameField = AccessTools.Field(interestDefT, "defName");
                if (defNameField == null)
                {
                    Logger.Error("could not get defName field");
                    return false;
                }
                Logger.Message("found defName field");

                interestStrings = new List<string> { };
                for (int i = 0; i < (int)count; i++)
                {
                    var interestDef = getItem.Invoke(interestList, new object[] { i });
                    if (interestDef == null)
                    {
                        Logger.Error("could not find interest def");
                        return false;
                    }
                    Logger.Message("found interest def");
                    var defName = defNameField.GetValue(interestDef);
                    if (defName == null)
                    {
                        Logger.Error("could not get defname");
                        return false;
                    }
                    Logger.Message("adding defName " + (string)defName);
                    interestStrings.Add((string)defName);
                }
                return true;
            }
            return false;
        }

        // raise this based on downed colonists
        public Priority ConsiderDownedColonists(Pawn pawn, WorkTypeDef workTypeDef, float percentDownedColonists)
        {
            if (workTypeDef.defName != "Doctor")
                return this;
            Add(percentDownedColonists);
            return this;
        }

        public Priority ConsiderBuildingImmunity(Pawn pawn, WorkTypeDef workTypeDef)
        {
            try
            {
                if (!pawn.health.hediffSet.HasImmunizableNotImmuneHediff())
                    return this;
                if (workTypeDef.defName == "PatientBedRest")
                    return Add(0.4f);
                if (workTypeDef.defName == "Patient")
                    return this;
                return Add(-0.2f);
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
                return Add(-0.2f);
            if (workTypeDef.defName == "Doctor")
            {
                value += percentColonistsNeedingTreatment;
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
                        return Add(0.4f);
                    if (workTypeDef.defName == "Hunting" || workTypeDef.defName == "PlantCutting")
                        return Add(0.2f);
                }
                return this;
            }
            catch
            {
                Logger.Debug("Unable to consider low food due to an error");
                return this;
            }
        }

        public Priority ConsiderAteRawFood(Pawn pawn, WorkTypeDef workTypeDef)
        {
            try
            {
                if (workTypeDef.defName != "Cooking")
                    return this;

                List<Thought> allThoughts = new List<Thought>();
                pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
                for (int i = 0; i < allThoughts.Count; i++)
                {
                    Thought thought = allThoughts[i];
                    if (thought.def.defName == "AteRawFood")
                    {
                        return Set(UnityEngine.Mathf.Max(0.6f, this.value));
                    }
                }
                return this;
            }
            catch
            {
                Logger.Debug("Unable to consider eating raw food due to an error");
                return this;
            }
        }

        public Priority ConsiderThingsDeteriorating(bool thingsDeteriorating)
        {
            if (thingsDeteriorating)
                return Add(0.2f);
            return this;
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

        public Priority SetExpectations(Pawn pawn)
        {
            try
            {
                Set(expectationGrid[ExpectationsUtility.CurrentExpectationFor(pawn).defName][pawn.needs.beauty.CurCategory]);
            }
            catch
            {
                Set(0.3f);
            }
            return this;
        }

        public Priority SetRelevantSkills(Pawn pawn, WorkTypeDef workTypeDef, int numPawns)
        {
            float badSkillCutoff = numPawns;
            float goodSkillCutoff = badSkillCutoff + (20f - badSkillCutoff) / 2f;
            float greatSkillCutoff = goodSkillCutoff + (20f - goodSkillCutoff) / 2f;
            float excellentSkillCutoff = greatSkillCutoff + (20f - greatSkillCutoff) / 2f;

            if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= excellentSkillCutoff) return Set(0.9f);
            if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= greatSkillCutoff) return Set(0.7f);
            if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= goodSkillCutoff) return Set(0.5f);
            if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= badSkillCutoff) return Set(0.3f);
            return Set(0.1f);
        }

        private bool NotInHomeArea(Pawn pawn)
        {
            return !pawn.Map.areaManager.Home[pawn.Position];
        }


        public Priority CalcPriority(
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
            switch (workTypeDef.defName)
            {
                case "Firefighter":
                    return this
                        .Set(1.0f)
                        .NeverDoIf(pawn.Downed)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Patient":
                    return this
                        .Set(1.0f)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "PatientBedRest":
                    return this
                        .Set(1.0f - Mathf.Pow(pawn.health.summaryHealth.SummaryHealthPercent, 7.0f))
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "BasicWorker":
                    return this
                        .Set(0.5f)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .AlwaysDoIf(pawn.mindState.IsIdle)
                        .NeverDoIf(pawn.Downed)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Hauling":
                case "HaulingUrgent":
                    return this
                        .SetExpectations(pawn)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .ConsiderThingsDeteriorating(thingsDeteriorating)
                        .AlwaysDoIf(pawn.mindState.IsIdle)
                        .NeverDoIf(pawn.Downed)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Cleaning":
                    return this
                        .SetExpectations(pawn)
                        .Multiply(pawn.health.summaryHealth.SummaryHealthPercent)
                        .AlwaysDoIf(pawn.mindState.IsIdle)
                        .NeverDoIf(pawn.Downed)
                        .NeverDoIf(NotInHomeArea(pawn))
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                case "Hunting":
                    return this
                        .SetRelevantSkills(pawn, workTypeDef, numPawns)
                        .ConsiderPassion(pawn, workTypeDef)
                        .ConsiderInspiration(pawn, workTypeDef)
                        .ConsiderDownedColonists(pawn, workTypeDef, percentDownedColonists)
                        .ConsiderLowFood(pawn, workTypeDef, freeColonistsSpawnedCount, totalHumanEdibleNutrition)
                        .AlwaysDoIf(pawn.mindState.IsIdle)
                        .NeverDoIf(pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")) && !settings.brawlersCanHunt)
                        .NeverDoIf(!WorkGiver_HunterHunt.HasHuntingWeapon(pawn))
                        .NeverDoIf(pawn.Downed)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;

                default:
                    return this
                        .SetRelevantSkills(pawn, workTypeDef, numPawns)
                        .ConsiderPassion(pawn, workTypeDef)
                        .ConsiderInspiration(pawn, workTypeDef)
                        .ConsiderDownedColonists(pawn, workTypeDef, percentDownedColonists)
                        .AlwaysDoIf(pawn.mindState.IsIdle)
                        .ConsiderLowFood(pawn, workTypeDef, freeColonistsSpawnedCount, totalHumanEdibleNutrition)
                        .ConsiderAteRawFood(pawn, workTypeDef)
                        .ConsiderDowned(pawn, workTypeDef)
                        .ConsiderBuildingImmunity(pawn, workTypeDef)
                        .ConsiderColonistsNeedingTreatment(pawn, workTypeDef, percentColonistsNeedingTreatment)
                        ;
            }
        }
    }
}

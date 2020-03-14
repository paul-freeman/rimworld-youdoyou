using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;


namespace YouDoYou
{
    public class YouDoYou_MapComponent : MapComponent
    {
        private const float step = 0.2f;
        private const float priority_one = 4 * step;
        private const float priority_two = 3 * step;
        private const float priority_three = 2 * step;
        private const float priority_four = 1 * step;
        private const float priority_disabled = 0;
        public Dictionary<string, bool> autoPriorities = new Dictionary<string, bool>();

        public YouDoYou_MapComponent(Map map) : base(map) { }
        private int counter = 0;
        private int frequency = 300;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (++counter == frequency)
            {
                YouDoYou_Settings settings = LoadedModManager.GetMod<YouDoYou_Mod>().GetSettings<YouDoYou_Settings>();
                Dictionary<string, bool> autoPriorities = Find.CurrentMap.GetComponent<YouDoYou_MapComponent>().autoPriorities;

                // update priorities
                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    BestWorker best = new BestWorker(workTypeDef, 0.2f);
                    foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (!autoPriorities[pawn.GetUniqueLoadID()])
                        {
                            int forcedPriority = pawn.workSettings.GetPriority(workTypeDef);
                            switch (forcedPriority)
                            {
                                case 1:
                                    best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), 0.8f);
                                    break;
                                case 2:
                                    best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), 0.6f);
                                    break;
                                case 3:
                                    best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), 0.4f);
                                    break;
                                case 4:
                                    best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), 0.2f);
                                    break;
                                default:
                                    best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), 0.0f);
                                    break;
                            }
                            continue;
                        }

                        if (pawn.WorkTypeIsDisabled(workTypeDef))
                            continue;

                        if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn.CurJob.workGiverDef.workType == workTypeDef)
                            // Don't change the priority if this type of work is currently being done.
                            continue;

                        float priority = computeBasePriority(pawn, workTypeDef);

                        priority = cleanNeedModification(pawn, workTypeDef, settings, priority);

                        priority = haulNeedModification(pawn, workTypeDef, settings, priority);

                        priority = avoidDisable(pawn, workTypeDef, settings, priority);

                        priority = disableBrawlerHunting(pawn, workTypeDef, settings, priority);

                        priority = disableNonRanged(pawn, workTypeDef, priority);

                        // make sure it's in range
                        priority = Mathf.Clamp(priority, 0, 1);

                        if (Current.Game.playSettings.useWorkPriorities)
                        {
                            if (0.8 <= priority)
                                pawn.workSettings.SetPriority(workTypeDef, 1);
                            else if (0.6 <= priority)
                                pawn.workSettings.SetPriority(workTypeDef, 2);
                            else if (0.4 <= priority)
                                pawn.workSettings.SetPriority(workTypeDef, 3);
                            else if (0.2 <= priority)
                                pawn.workSettings.SetPriority(workTypeDef, 4);
                            else
                                pawn.workSettings.SetPriority(workTypeDef, 0);
                        }
                        else
                        {
                            if (0.2 <= priority)
                                pawn.workSettings.SetPriority(workTypeDef, 3);
                            else
                                pawn.workSettings.SetPriority(workTypeDef, 0);
                        }
                        best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), priority);
                    }
                        if (!best.Filled() && best.Get() != null)
                        {
                            best.Get().workSettings.SetPriority(workTypeDef, 4);
                        }
                        
                }
                counter = 0;
            }
        }

        private float computeBasePriority(Pawn pawn, WorkTypeDef workTypeDef)
        {
            const float excellentSkillCutoff = 18.0f;
            const float greatSkillCutoff = 15.0f;
            const float goodSkillCutoff = 11.0f;
            const float badSkillCutoff = 3.0f;
            float priority;
            switch (workTypeDef.defName)
            {
                case "Firefighter":
                case "Patient":
                    priority = priority_one;
                    break;
                case "PatientBedRest":
                    priority = priority_three + (1 - Mathf.Pow(pawn.health.summaryHealth.SummaryHealthPercent, 4.0f));
                    break;
                case "BasicWorker":
                case "Hauling":
                    priority = (priority_two - 0.5f * step) * pawn.health.summaryHealth.SummaryHealthPercent;
                    break;
                case "Cleaning":
                    ExpectationDef e = ExpectationsUtility.CurrentExpectationFor(pawn);
                    priority = (priority_two + (e.order - 4) * step) * pawn.health.summaryHealth.SummaryHealthPercent;
                    break;
                case "Hunting":
                default:
                    if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= excellentSkillCutoff)
                    {
                        priority = priority_one;
                    }
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= greatSkillCutoff)
                    {
                        priority = priority_two;
                    }
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= goodSkillCutoff)
                    {
                        priority = priority_three;
                    }
                    else if (pawn.skills.AverageOfRelevantSkillsFor(workTypeDef) >= badSkillCutoff)
                    {
                        priority = priority_four;
                    }
                    else
                    {
                        priority = priority_disabled;
                    }
                    priority += calculatePawnMoodOnPassion(pawn, workTypeDef);
                    break;
            }
            return priority;
        }

        // In some cases, we don't want to disable skill because then they
        // cannot be manually queued by the player
        private float avoidDisable(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, float priority)
        {
            if (pawn.mindState.IsIdle || settings.dontDisableAnything)
            {
                return Mathf.Clamp(priority, 0.2f, 1);
            }
            return priority;
        }

        // Passion in an work type area increases the priority to do that work.
        // But the increase is smaller when pawns are in a bad mood.
        private float calculatePawnMoodOnPassion(Pawn pawn, WorkTypeDef workTypeDef)
        {
            Passion passion = pawn.skills.MaxPassionOfRelevantSkillsFor(workTypeDef);
            return (float)passion * pawn.needs.mood.CurLevel * step * 1.25f;
        }

        // Brawlers don't like having a ranged weapon, so they won't hunt.
        // Sucks if they have a passion for it, though.
        private float disableBrawlerHunting(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, float priority)
        {
            if (workTypeDef.defName == "Hunting" && !settings.brawlersCanHunt && pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")))
            {
                return 0.0f;
            }
            return priority;
        }

        // If the pawn doesn't have a ranged weapon they can't hunt,
        // but they will complain if they really want to hunt.
        private float disableNonRanged(Pawn pawn, WorkTypeDef workTypeDef, float priority)
        {
            if (workTypeDef.defName == "Hunting" && !WorkGiver_HunterHunt.HasHuntingWeapon(pawn) && priority < priority_one)
            {
                return 0.0f;
            }
            return priority;
        }

        // Adaptive cleaning will increase desire to clean if beauty is low.
        // But this is kept low when outside of the home area since pawns
        // cannot clean out there anyway.
        private float cleanNeedModification(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, float priority)
        {
            try
            {
                if (workTypeDef.defName != "Cleaning" || !settings.adaptiveCleaning)
                {
                    return priority;
                }
                if (!pawn.Map.areaManager.Home[pawn.Position])
                {
                    // Don't worry if our non-home area is not beautiful
                    return priority_four;
                }
                float pawnCleaningDesire = 1.0f - pawn.needs.beauty.CurInstantLevel;
                if (priority >= pawnCleaningDesire)
                {
                    return priority;
                }
                // Our desire to clean is overwhelming!
                // See if we can find something to clean.
                BeautyUtility.FillBeautyRelevantCells(pawn.Position, pawn.Map);
                ExpectationDef e = ExpectationsUtility.CurrentExpectationFor(pawn);
                foreach (IntVec3 c in BeautyUtility.beautyRelevantCells)
                {
                    foreach (Thing thing in map.thingGrid.ThingsListAt(c))
                    {
                        if (thing.def.category == ThingCategory.Filth)
                        {
                            return pawnCleaningDesire;
                        }
                    }
                }
                return priority;
            }
            catch
            {
                Logger.Error("caught error in adaptive cleaning");
                return priority;
            }
        }

        // Similar to adaptive cleaning, adaptive hauling will increase
        // desire to haul if beauty is low. This occurs both inside and
        // outside the home area, but only if there are hauling jobs
        // nearby.
        private float haulNeedModification(Pawn pawn, WorkTypeDef workTypeDef, YouDoYou_Settings settings, float priority)
        {
            try
            {
                if (workTypeDef.defName == "Hauling" && settings.adaptiveHauling)
                {
                    float pawnHaulingDesire = 1.0f - pawn.needs.beauty.CurInstantLevel;
                    if (priority < pawnHaulingDesire)
                    {
                        // Our desire to haul is overwhelming!
                        // See if we can find something to haul.
                        BeautyUtility.FillBeautyRelevantCells(pawn.Position, pawn.Map);
                        foreach (Thing thing in map.listerHaulables?.ThingsPotentiallyNeedingHauling())
                        {
                            foreach (IntVec3 c in BeautyUtility.beautyRelevantCells)
                            {
                                if (c == thing.Position)
                                {
                                    return pawnHaulingDesire;
                                }
                            }
                        }
                    }
                }
                return priority;
            }
            catch
            {
                Logger.Error("caught error in adaptive hauling");
                return priority;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref autoPriorities, "AutoPriorities", LookMode.Value, LookMode.Value);
        }
    }
}
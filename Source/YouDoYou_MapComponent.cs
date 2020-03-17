using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;


namespace YouDoYou
{
    public class YouDoYou_MapComponent : MapComponent
    {
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

                bool thingsDeteriorating = false;
                foreach (Thing thing in map.listerHaulables.ThingsPotentiallyNeedingHauling())
                    if (thing.def.CanEverDeteriorate)
                    {
                        thingsDeteriorating = true;
                        break;
                    }

                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    int numPawns = map.mapPawns.FreeColonistsSpawned.Count;
                    BestWorker best = new BestWorker(workTypeDef);
                    foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                    {
                        Priority priority = new Priority();
                        if (!autoPriorities.TryGetValue(pawn.GetUniqueLoadID(), true))
                        {
                            priority.Set(pawn.workSettings.GetPriority(workTypeDef));
                            best.Update(null, -1.0f, priority);
                            continue;
                        }

                        if (pawn.WorkTypeIsDisabled(workTypeDef))
                            continue;

                        if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn?.CurJob?.workGiverDef?.workType == workTypeDef)
                        {
                            // Don't change the priority if this type of work is currently being done.
                            priority.Set(pawn.workSettings.GetPriority(workTypeDef));
                            best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), priority);
                            continue;
                        }

                        priority.CalcPriority(map, numPawns, pawn, workTypeDef, thingsDeteriorating, settings);

                        if (Current.Game.playSettings.useWorkPriorities)
                            pawn.workSettings.SetPriority(workTypeDef, priority.toInt());
                        else if (priority.Disabled())
                            pawn.workSettings.SetPriority(workTypeDef, 0);
                        else
                            pawn.workSettings.SetPriority(workTypeDef, 3);
                        best.Update(pawn, pawn.skills.AverageOfRelevantSkillsFor(workTypeDef), priority);
                    }
                    if (!best.Filled && best.Pawn != null)
                        best.Pawn.workSettings.SetPriority(workTypeDef, 4);
                }
                counter = 0;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref autoPriorities, "AutoPriorities", LookMode.Value, LookMode.Value);
        }
    }
}
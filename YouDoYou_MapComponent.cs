using System.Collections.Generic;
using System.Reflection;
using Verse;
using System.Linq;
using RimWorld;
using HarmonyLib;

namespace YouDoYou
{
    public class YouDoYou_MapComponent : MapComponent
    {
        /// <summary>
        /// The free will status of pawns on the map.
        /// </summary>
        public Dictionary<string, System.Boolean> pawnFree = new Dictionary<string, System.Boolean>();

        /// <summary>
        /// Cached priorities of pawns on the map.
        /// </summary>
        private Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>> priorities;

        /// <summary>
        /// The number of free colonists spawned on the map.
        /// </summary>
        public int NumPawns
        {
            get
            {
                if (numPawns == 0)
                {
                    numPawns = this.map.mapPawns.FreeColonistsSpawnedCount;
                    return numPawns;
                }
                return numPawns;
            }
        }
        private int numPawns;

        /// <summary>
        /// The percentage of pawns on this map needing some sort of medical
        /// treatment.
        ///
        /// The value should always be between 0 and 1.
        /// </summary>
        public float PercentPawnsNeedingTreatment { get { return percentPawnsNeedingTreatment; } }
        private float percentPawnsNeedingTreatment;

        /// <summary>
        /// The percentage of pets on this map needing some sort of medical
        /// treatment.
        ///
        /// The value should always be between 0 and 1.
        /// </summary>
        /// <value></value>
        public int NumPetsNeedingTreatment { get { return numPetsNeedingTreatment; } }
        private int numPetsNeedingTreatment;

        public float PercentPawnsDowned { get { return percentPawnsDowned; } }
        private float percentPawnsDowned;
        public bool ThingsDeteriorating { get { return thingsDeteriorating; } }
        private bool thingsDeteriorating;
        public int MapFires { get { return mapFires; } }
        private int mapFires;
        public bool HomeFire { get { return homeFire; } }
        private bool homeFire;
        public bool RefuelNeededNow { get { return refuelNeededNow; } }
        private bool refuelNeededNow;
        public bool RefuelNeeded { get { return refuelNeeded; } }
        private bool refuelNeeded;
        public bool PlantsBlighted { get { return plantsBlighted; } }
        private bool plantsBlighted;
        public float TotalFood { get { return totalFood; } }
        private float totalFood;

        /// <summary>
        /// The field containing all the active alerts in the game.
        /// </summary>
        private readonly FieldInfo activeAlertsField;

        /// <summary>
        /// Indicates that pawns need warm clothes for cold weather.
        /// </summary>
        public bool NeedWarmClothes { get { return needWarmClothes; } }
        /// <summary>
        /// Indicates that pawns need warm clothes for cold weather.
        /// </summary>
        private bool needWarmClothes;

        /// <summary>
        /// Indicates that pawns need burial.
        /// </summary>
        public bool AlertColonistLeftUnburied { get { return alertColonistLeftUnburied; } }
        /// <summary>
        /// Indicates that pawns need burial.
        /// </summary>
        private bool alertColonistLeftUnburied;

        /// <summary>
        /// The map tick counter for YouDoYou.
        /// </summary>
        private int counter = 0;

        /// <summary>
        /// The number of rest ticks between cache updates.
        /// </summary>
        private const int restTicks = 300;

        public YouDoYou_MapComponent(Map map) : base(map)
        {
            this.pawnFree = new Dictionary<string, System.Boolean> { };
            this.priorities = new Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>> { };
            this.activeAlertsField = AccessTools.Field(typeof(AlertsReadout), "AllAlerts");
            if (this.activeAlertsField == null)
            {
                Logger.Error("could not find activeAlerts field");
            }
        }

        /// <summary>
        /// The work performed by YouDoYou during each map tick.
        ///
        /// Basically, the goal here is to spread the work out over a number of
        /// map ticks and then stop for a bit.
        ///
        /// So we do a few prep ticks to pull environmental values and then one
        /// tick for each pawn. Finally, we rest for some amount of ticks.
        /// </summary>
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            const int NUM_PREP_CASES = 7;
            counter++;
            if (counter > NUM_PREP_CASES + numPawns + restTicks)
            {
                counter = 0;
            }
            switch (counter)
            {
                case 0:
                    checkColonyHealth();
                    return;
                case 1:
                    checkThingsDeteriorating();
                    return;
                case 2:
                    checkBlight();
                    return;
                case 3:
                    checkColonyFoodLevel();
                    return;
                case 4:
                    checkMapFire();
                    return;
                case 5:
                    checkRefuelNeeded();
                    return;
                case 6:
                    checkActiveAlerts();
                    return;
                case NUM_PREP_CASES:
                default:
                    break;
            }
            int adjustedCounter = counter - NUM_PREP_CASES;
            if (adjustedCounter < NumPawns)
            {
                try
                {
                    SetPriorities(adjustedCounter);
                }
                catch
                {
                    Logger.Message("could not set priorities for pawn number " + adjustedCounter);
                }
                return;
            }
        }

        public Dictionary<WorkTypeDef, Priority> GetPriorities(Pawn pawn)
        {
            Dictionary<WorkTypeDef, Priority> pawnPriorities;
            if (this.priorities.TryGetValue(pawn, out pawnPriorities))
            {
                return pawnPriorities;
            }
            // fallback to generating the priorities
            this.checkColonyHealth();
            this.checkThingsDeteriorating();
            this.checkColonyFoodLevel();
            for (int i = 0; i < this.map.mapPawns.FreeColonistsSpawnedCount; i++)
            {
                if (pawn == this.map.mapPawns.FreeColonistsSpawned[i])
                {
                    try
                    {
                        this.SetPriorities(i);
                    }
                    catch
                    {
                        Logger.Message("could not set priorities for pawn number " + i);
                    }
                }
            }
            // now retry
            if (this.priorities.TryGetValue(pawn, out pawnPriorities))
            {
                return pawnPriorities;
            }
            // the pawn might not be on this map
            return new Dictionary<WorkTypeDef, Priority>();
        }

        private void SetPriorities(int n)
        {
            if (n < 0 || n >= map.mapPawns.FreeColonistsSpawnedCount)
            {
                Logger.Message(string.Format("could not find pawn {0}: only {1} free colonists spawned on this map", n, map.mapPawns.FreeColonistsSpawnedCount));
                return;
            }
            Pawn pawn = map.mapPawns.FreeColonistsSpawned[n];
            string pawnKey = pawn.GetUniqueLoadID();
            try
            {
                if (pawnFree == null)
                {
                    pawnFree = new Dictionary<string, bool>();
                }
                if (!pawnFree.ContainsKey(pawnKey))
                {
                    pawnFree[pawnKey] = true;
                }
                Logger.Debug("setting priorities for " + pawn.Name);
                Dictionary<WorkTypeDef, Priority> pawnPriorities = new Dictionary<WorkTypeDef, Priority>();
                YouDoYou_Settings settings = YouDoYou_WorldComponent.Settings;
                if (settings == null)
                {
                    Logger.Message("could not find YouDoYou settings");
                    return;
                }
                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    bool isFree = pawnFree[pawnKey];
                    pawnPriorities[workTypeDef] = new Priority(pawn, workTypeDef, settings, isFree);
                    if (isFree)
                    {
                        try
                        {
                            pawnPriorities[workTypeDef].ApplyPriorityToGame();
                        }
                        catch
                        {
                            Logger.Error(string.Format("could not set priority for pawn: {0} ({1})", pawn.Name, workTypeDef.defName));
                            // marking them as not free
                            pawnFree[pawnKey] = false;
                        }
                    }
                }
                if (priorities == null)
                {
                    priorities = new Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>>();
                }
                // cache the priorities until the next update
                priorities[pawn] = pawnPriorities;
            }
            catch
            {
                Logger.Error("could not set priorities for pawn: " + pawn.Name);
                // marking them as not free
                Logger.Message("marking " + pawn.Name + " as not having free will");
                pawnFree[pawnKey] = false;
            }
        }

        private void checkColonyHealth()
        {
            numPetsNeedingTreatment =
                (from p in map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                 where p.RaceProps.Animal && p.health.HasHediffsNeedingTend()
                 select p).Count();
            numPawns = map.mapPawns.FreeColonistsSpawnedCount;
            percentPawnsDowned = 0.0f;
            percentPawnsNeedingTreatment = 0.0f;
            float colonistWeight = 1.0f / numPawns;
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Downed)
                {
                    percentPawnsDowned += colonistWeight;
                }
                if (pawn.health.HasHediffsNeedingTend())
                {
                    percentPawnsNeedingTreatment += colonistWeight;
                }
            }
        }

        private void checkThingsDeteriorating()
        {
            thingsDeteriorating = false;
            foreach (Thing thing in map.listerHaulables.ThingsPotentiallyNeedingHauling())
            {
                if (SteadyEnvironmentEffects.FinalDeteriorationRate(thing) != 0)
                {
                    thingsDeteriorating = true;
                    return;
                }
            }
        }

        private void checkBlight()
        {
            plantsBlighted = false;
            foreach (Thing thing in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Plant))
            {
                Plant plant = (Plant)thing;
                if (plant != null && plant.Blighted)
                {
                    plantsBlighted = true;
                    return;
                }
            }
        }

        private void checkColonyFoodLevel()
        {
            totalFood = map.resourceCounter.TotalHumanEdibleNutrition;
        }

        private void checkMapFire()
        {
            List<Thing> list = this.map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            mapFires = list.Count;
            homeFire = false;
            for (int j = 0; j < list.Count; j++)
            {
                mapFires++;
                Thing thing = list[j];
                if (this.map.areaManager.Home[thing.Position] && !thing.Position.Fogged(thing.Map))
                {
                    homeFire = true;
                    return;
                }
            }
        }

        private void checkRefuelNeeded()
        {
            refuelNeeded = false;
            refuelNeededNow = false;
            List<Thing> list = this.map.listerThings.ThingsInGroup(ThingRequestGroup.Refuelable);
            foreach (Thing thing in list)
            {
                CompRefuelable refuel = thing.TryGetComp<CompRefuelable>();
                if (refuel == null)
                {
                    continue;
                }
                if (!refuel.HasFuel)
                {
                    refuelNeeded = true;
                    refuelNeededNow = true;
                    return;
                }
                if (!refuel.IsFull)
                {
                    refuelNeeded = true;
                    continue;
                }
            }
        }

        public void checkActiveAlerts()
        {
            try
            {
                UIRoot_Play ui = Find.UIRoot as UIRoot_Play;
                if (ui == null)
                {
                    return;
                }
                // unset all the alerts
                this.needWarmClothes = false;
                this.alertColonistLeftUnburied = false;
                // check current alerts
                foreach (Alert alert in (List<Alert>)activeAlertsField.GetValue(ui.alerts))
                {
                    if (!alert.Active)
                    {
                        continue;
                    }
                    switch (alert)
                    {
                        case Alert_NeedWarmClothes a:
                            this.needWarmClothes = true;
                            break;
                        case Alert_ColonistLeftUnburied a:
                            if (this.map.mapPawns.AnyFreeColonistSpawned)
                            {
                                List<Thing> list = this.map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Corpse));
                                for (int i = 0; i < list.Count; i++)
                                {
                                    Corpse corpse = (Corpse)list[i];
                                    if (Alert_ColonistLeftUnburied.IsCorpseOfColonist(corpse))
                                    {
                                        this.alertColonistLeftUnburied = true;
                                        break;
                                    }
                                }
                                if (this.alertColonistLeftUnburied)
                                {
                                    break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch
            {
                Logger.Error("could not check active alerts");
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawnFree, "PawnFree", LookMode.Value, LookMode.Value);
        }
    }
}

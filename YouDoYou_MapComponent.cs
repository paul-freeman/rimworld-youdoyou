using System.Collections.Generic;
using Verse;
using System.Linq;
using RimWorld;
using RimWorld.Planet;


namespace YouDoYou
{
    public class YouDoYou_MapComponent : MapComponent
    {
        public YouDoYou_MapComponent(Map map) : base(map)
        {
            this.pawnFree = new Dictionary<string, System.Boolean> { };
            this.priorities = new Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>> { };
        }

        public Dictionary<string, System.Boolean> pawnFree = new Dictionary<string, System.Boolean>();
        private Dictionary<Pawn, Dictionary<WorkTypeDef, Priority>> priorities;

        public int NumPawns
        {
            get
            {
                if (numPawns == 0)
                {
                    numPawns = map.mapPawns.FreeColonistsSpawnedCount;
                    return numPawns;
                }
                return numPawns;
            }
        }
        private int numPawns;
        public float PercentPawnsNeedingTreatment { get { return percentPawnsNeedingTreatment; } }
        private float percentPawnsNeedingTreatment;
        public int NumPetsNeedingTreatment { get { return numPetsNeedingTreatment; } }
        private int numPetsNeedingTreatment;
        public float PercentPawnsDowned { get { return percentPawnsDowned; } }
        private float percentPawnsDowned;
        public bool ThingsDeteriorating { get { return thingsDeteriorating; } }
        private bool thingsDeteriorating;
        public bool PlantsBlighted { get { return plantsBlighted; } }
        private bool plantsBlighted;
        public float TotalFood { get { return totalFood; } }
        private float totalFood;

        private int counter = 0;
        private const int restTicks = 300;

        // Basically the goal here is to spread the work out over a number of
        // map ticks and then stop for a bit.
        //
        // So we do a few prep ticks to pull environmental values and then one
        // tick for each pawn. Finally, we rest for some amount of ticks.
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int numPrepCases = 4;
            counter++;
            if (counter > numPrepCases + numPawns + restTicks)
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
                default:
                    break;
            }
            int adjustedCounter = counter - numPrepCases;
            if (adjustedCounter < NumPawns)
            {
                SetPriorities(adjustedCounter);
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
                    this.SetPriorities(i);
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
                return;
            }
            Pawn pawn = map.mapPawns.FreeColonistsSpawned[n];
            string pawnKey = pawn.GetUniqueLoadID();
            if (!pawnFree.ContainsKey(pawnKey))
            {
                pawnFree[pawnKey] = true;
            }
            Logger.Debug("setting priorities for " + pawn.Name);
            Dictionary<WorkTypeDef, Priority> pawnPriorities = new Dictionary<WorkTypeDef, Priority>();
            YouDoYou_Settings settings = YouDoYou_WorldComponent.Settings;
            foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                bool isFree = pawnFree[pawnKey];
                pawnPriorities[workTypeDef] = new Priority(pawn, workTypeDef, settings, isFree);
                if (isFree)
                {
                    pawnPriorities[workTypeDef].ApplyPriorityToGame();
                }
            }
            // cache the priorities until the next update
            priorities[pawn] = pawnPriorities;
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawnFree, "PawnFree", LookMode.Value, LookMode.Value);
        }
    }
}

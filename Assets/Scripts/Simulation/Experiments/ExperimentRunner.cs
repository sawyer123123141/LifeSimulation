using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct ExperimentResult
    {
        public ExperimentResult(
            string scenarioId,
            int worldSeed,
            long completedTicks,
            SimulationStatistics finalStatistics,
            ulong finalStateHash,
            bool eventOverflowed,
            bool populationCapReached,
            int leftFoodTargetDecisions = 0,
            int rightFoodTargetDecisions = 0,
            float totalFoodTargetDistance = 0f,
            int foodTargetDecisionCount = 0,
            int leftLocalPopulationTicks = 0,
            int rightLocalPopulationTicks = 0)
        {
            ScenarioId = scenarioId;
            WorldSeed = worldSeed;
            CompletedTicks = completedTicks;
            FinalStatistics = finalStatistics;
            FinalStateHash = finalStateHash;
            EventOverflowed = eventOverflowed;
            PopulationCapReached = populationCapReached;
            LeftFoodTargetDecisions = leftFoodTargetDecisions;
            RightFoodTargetDecisions = rightFoodTargetDecisions;
            TotalFoodTargetDistance = totalFoodTargetDistance;
            FoodTargetDecisionCount = foodTargetDecisionCount;
            LeftLocalPopulationTicks = leftLocalPopulationTicks;
            RightLocalPopulationTicks = rightLocalPopulationTicks;
        }

        public string ScenarioId { get; }
        public int WorldSeed { get; }
        public long CompletedTicks { get; }
        public SimulationStatistics FinalStatistics { get; }
        public ulong FinalStateHash { get; }
        public bool EventOverflowed { get; }
        public bool PopulationCapReached { get; }
        public int LeftFoodTargetDecisions { get; }
        public int RightFoodTargetDecisions { get; }
        public float TotalFoodTargetDistance { get; }
        public int FoodTargetDecisionCount { get; }
        public int LeftLocalPopulationTicks { get; }
        public int RightLocalPopulationTicks { get; }
        public float MeanFoodTargetDistance => FoodTargetDecisionCount == 0 ? 0f : TotalFoodTargetDistance / FoodTargetDecisionCount;
    }

    public static class ExperimentRunner
    {
        public static ExperimentResult Run(SimulationConfig config, SimulationScenario scenario, int ticks)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks));
            }

            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);
            bool eventOverflowed = false;
            bool populationCapReached = false;
            int leftFoodTargetDecisions = 0;
            int rightFoodTargetDecisions = 0;
            float totalFoodTargetDistance = 0f;
            int foodTargetDecisionCount = 0;
            int leftLocalPopulationTicks = 0;
            int rightLocalPopulationTicks = 0;
            for (int index = 0; index < ticks; index++)
            {
                world.Step(config.FixedDeltaTime);
                CountFoodTargetDecisions(world, ref leftFoodTargetDecisions, ref rightFoodTargetDecisions, ref totalFoodTargetDistance, ref foodTargetDecisionCount);
                CountLocalPopulation(world, ref leftLocalPopulationTicks, ref rightLocalPopulationTicks);
                eventOverflowed |= world.Events.Overflowed;
                populationCapReached |= world.CreatureCount >= config.MaximumPopulation;
                world.Events.Clear();
            }

            return new ExperimentResult(
                scenario.Id,
                config.WorldSeed,
                world.CurrentTick,
                // The end-of-run truth, not the cached cadence sample: a run whose tick count is
                // not a multiple of the statistics interval would otherwise report state up to a
                // full interval old, and a zero-tick run would report the pre-scenario world.
                world.CaptureStatistics(),
                world.ComputeStateHash(),
                eventOverflowed,
                populationCapReached,
                leftFoodTargetDecisions,
                rightFoodTargetDecisions,
                totalFoodTargetDistance,
                foodTargetDecisionCount,
                leftLocalPopulationTicks,
                rightLocalPopulationTicks);
        }

        private static void CountFoodTargetDecisions(SimulationWorld world, ref int leftCount, ref int rightCount, ref float totalDistance, ref int targetCount)
        {
            for (int creatureIndex = 0; creatureIndex < world.CreatureCount; creatureIndex++)
            {
                CreatureDecision decision = world.GetCreatureDecisionAt(creatureIndex);
                if (decision.DecisionTick != world.CurrentTick
                    || (decision.Action != CreatureAction.SeekFood && decision.Action != CreatureAction.Eat)
                    || (uint)decision.TargetResourceIndex >= (uint)world.Resources.Count)
                {
                    continue;
                }

                ResourceState target = world.Resources.GetAt(decision.TargetResourceIndex);
                if (target.Kind == ResourceKind.Food)
                {
                    MovementState movement = world.GetCreatureMovementAt(creatureIndex);
                    totalDistance += SimVector2.Distance(movement.PreviousPosition, target.Position);
                    targetCount++;
                    if (target.Position.X < 0f) leftCount++;
                    else rightCount++;
                }
            }
        }

        private static void CountLocalPopulation(SimulationWorld world, ref int leftCount, ref int rightCount)
        {
            for (int creatureIndex = 0; creatureIndex < world.CreatureCount; creatureIndex++)
            {
                if (world.GetCreatureMovementAt(creatureIndex).Position.X < 0f)
                {
                    leftCount++;
                }
                else
                {
                    rightCount++;
                }
            }
        }
    }
}

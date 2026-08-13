using System;
using LifeSimulation.Simulation.Core;

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
            bool eventOverflowed)
        {
            ScenarioId = scenarioId;
            WorldSeed = worldSeed;
            CompletedTicks = completedTicks;
            FinalStatistics = finalStatistics;
            FinalStateHash = finalStateHash;
            EventOverflowed = eventOverflowed;
        }

        public string ScenarioId { get; }
        public int WorldSeed { get; }
        public long CompletedTicks { get; }
        public SimulationStatistics FinalStatistics { get; }
        public ulong FinalStateHash { get; }
        public bool EventOverflowed { get; }
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
            for (int index = 0; index < ticks; index++)
            {
                world.Step(config.FixedDeltaTime);
                eventOverflowed |= world.Events.Overflowed;
                world.Events.Clear();
            }

            return new ExperimentResult(
                scenario.Id,
                config.WorldSeed,
                world.CurrentTick,
                world.Statistics,
                world.ComputeStateHash(),
                eventOverflowed);
        }
    }
}

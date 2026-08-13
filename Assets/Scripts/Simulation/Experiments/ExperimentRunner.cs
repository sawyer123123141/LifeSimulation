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
            ulong finalStateHash)
        {
            ScenarioId = scenarioId;
            WorldSeed = worldSeed;
            CompletedTicks = completedTicks;
            FinalStatistics = finalStatistics;
            FinalStateHash = finalStateHash;
        }

        public string ScenarioId { get; }
        public int WorldSeed { get; }
        public long CompletedTicks { get; }
        public SimulationStatistics FinalStatistics { get; }
        public ulong FinalStateHash { get; }
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
            for (int index = 0; index < ticks; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            return new ExperimentResult(
                scenario.Id,
                config.WorldSeed,
                world.CurrentTick,
                world.Statistics,
                world.ComputeStateHash());
        }
    }
}

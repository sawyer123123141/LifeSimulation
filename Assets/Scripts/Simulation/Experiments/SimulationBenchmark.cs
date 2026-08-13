using System;
using System.Diagnostics;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct SimulationBenchmarkResult
    {
        public SimulationBenchmarkResult(int measuredTicks, int finalPopulation, double totalMilliseconds)
        {
            MeasuredTicks = measuredTicks;
            FinalPopulation = finalPopulation;
            TotalMilliseconds = totalMilliseconds;
        }

        public int MeasuredTicks { get; }
        public int FinalPopulation { get; }
        public double TotalMilliseconds { get; }
        public double AverageStepMilliseconds => MeasuredTicks == 0 ? 0d : TotalMilliseconds / MeasuredTicks;
    }

    public static class SimulationBenchmark
    {
        public static SimulationBenchmarkResult Run(
            SimulationConfig config,
            SimulationScenario scenario,
            int warmupTicks,
            int measuredTicks)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (warmupTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(warmupTicks));
            }

            if (measuredTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(measuredTicks));
            }

            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);
            for (int index = 0; index < warmupTicks; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            var stopwatch = Stopwatch.StartNew();
            for (int index = 0; index < measuredTicks; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            stopwatch.Stop();
            return new SimulationBenchmarkResult(measuredTicks, world.CreatureCount, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

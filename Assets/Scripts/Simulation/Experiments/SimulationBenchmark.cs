using System;
using System.Diagnostics;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct SimulationBenchmarkResult
    {
        public SimulationBenchmarkResult(
            int measuredTicks,
            int finalPopulation,
            double totalMilliseconds,
            double p95StepMilliseconds)
        {
            MeasuredTicks = measuredTicks;
            FinalPopulation = finalPopulation;
            TotalMilliseconds = totalMilliseconds;
            P95StepMilliseconds = p95StepMilliseconds;
        }

        public int MeasuredTicks { get; }
        public int FinalPopulation { get; }
        public double TotalMilliseconds { get; }
        public double P95StepMilliseconds { get; }
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

            var stepMilliseconds = new double[measuredTicks];
            var stopwatch = Stopwatch.StartNew();
            for (int index = 0; index < measuredTicks; index++)
            {
                long startTimestamp = Stopwatch.GetTimestamp();
                world.Step(config.FixedDeltaTime);
                stepMilliseconds[index] = (Stopwatch.GetTimestamp() - startTimestamp)
                    * 1000d
                    / Stopwatch.Frequency;
            }

            stopwatch.Stop();
            return new SimulationBenchmarkResult(
                measuredTicks,
                world.CreatureCount,
                stopwatch.Elapsed.TotalMilliseconds,
                CalculateP95(stepMilliseconds));
        }

        private static double CalculateP95(double[] samples)
        {
            if (samples.Length == 0)
            {
                return 0d;
            }

            Array.Sort(samples);
            int percentileIndex = Math.Max(0, (int)Math.Ceiling(samples.Length * 0.95d) - 1);
            return samples[percentileIndex];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// Why temperature tolerance is the strongest selection in the model.
    ///
    /// <para>The handoff proposed testing the terrain join on against off, on the reasoning that the
    /// join is what introduced a real temperature field. <b>That hypothesis is refuted by the code
    /// before any run happens.</b> Creature thermoregulation reads
    /// <c>TemperatureField.Sample</c> - a fixed spatial sine, <c>20 + 8*sin(0.18x + 0.11y)</c> - in
    /// both the decision path (<c>ThermoregulationSystem</c>) and the health path
    /// (<c>SimulationWorld.Ticking.cs:151</c>). The join builds an <c>EnvironmentField</c>, which
    /// feeds plants. It cannot reach this gene at all, so both arms of that test are identical.</para>
    ///
    /// <para>The arithmetic explanation is a <b>saturating</b> one. Tolerance in degrees is
    /// <c>2 + 8*gene</c> (<c>GenomePhenotype.cs:422</c>) and stress is
    /// <c>max(0, |T - 20| - tolerance)</c>. The field can deviate by at most 8 degrees, so a gene of
    /// <b>0.75 covers the entire world</b> and every point above that buys nothing. The only cost of
    /// carrying it is <c>0.06*gene</c> in the maintenance multiplier against a midpoint total of
    /// about 1.54 - roughly 1% of upkeep for the 0.27 of gene at issue. Enormous benefit, negligible
    /// price, hard ceiling.</para>
    ///
    /// <para>So the prediction, written before the measurement: the mean climbs steeply and then
    /// <b>flattens near the saturation point</b> implied by the deviations creatures actually
    /// experience, rather than climbing linearly for the whole run. This measures both halves - the
    /// trajectory, and the realised deviation distribution that sets where the ceiling is.</para>
    /// </summary>
    internal static class Thermal
    {
        private const int Checkpoints = 12;

        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            var tolerance = new List<double>[Checkpoints + 1];
            var control = new List<double>[Checkpoints + 1];
            for (int slot = 0; slot <= Checkpoints; slot++)
            {
                tolerance[slot] = new List<double>();
                control[slot] = new List<double>();
            }

            var deviations = new List<double>();
            int extinct = 0;

            for (int index = 0; index < seedCount; index++)
            {
                if (!RunOne(Program.FirstSeed + index, ticks, configure, scenario, tolerance, control, deviations)) extinct++;
            }

            Console.WriteLine();
            Console.WriteLine("thermal trajectory - " + seedCount + " seeds, " + ticks + " ticks, "
                + extinct + " extinct (excluded from every row)");
            Console.WriteLine("  tick | temperature_tolerance | neutral_marker | n");
            for (int slot = 0; slot <= Checkpoints; slot++)
            {
                long tick = (long)ticks * slot / Checkpoints;
                Console.WriteLine("  " + tick.ToString().PadLeft(5)
                    + " | " + Mean(tolerance[slot]).ToString("0.0000").PadLeft(21)
                    + " | " + Mean(control[slot]).ToString("0.0000").PadLeft(14)
                    + " | " + tolerance[slot].Count);
            }

            // Between-world spread at the end, which is where the placeholder and a real field differ
            // most: a sine applies the same pressure to every world, while terrain gives one arena a
            // temperate continent and another a cold one.
            var endpoints = new List<double>(tolerance[Checkpoints]);
            endpoints.Sort();
            Console.WriteLine();
            Console.WriteLine("endpoint across worlds: min " + Format(endpoints, 0d)
                + "  p25 " + Format(endpoints, 0.25d)
                + "  median " + Format(endpoints, 0.5d)
                + "  p75 " + Format(endpoints, 0.75d)
                + "  max " + Format(endpoints, 1d)
                + "  sd " + StandardDeviation(tolerance[Checkpoints]).ToString("0.0000"));

            deviations.Sort();
            Console.WriteLine();
            Console.WriteLine("realised |T - 20| at occupied positions, " + deviations.Count + " samples");
            Console.WriteLine("  mean " + Mean(deviations).ToString("0.000")
                + "  p50 " + Percentile(deviations, 0.50).ToString("0.000")
                + "  p90 " + Percentile(deviations, 0.90).ToString("0.000")
                + "  p99 " + Percentile(deviations, 0.99).ToString("0.000")
                + "  max " + (deviations.Count == 0 ? double.NaN : deviations[deviations.Count - 1]).ToString("0.000"));
            Console.WriteLine("  gene that covers p99: " + ((Percentile(deviations, 0.99) - 2d) / 8d).ToString("0.000")
                + "   gene that covers max: " + ((deviations.Count == 0 ? double.NaN : deviations[deviations.Count - 1] - 2d) / 8d).ToString("0.000"));
        }

        /// <summary>One run. Returns false if the world went extinct, in which case it contributes nothing.</summary>
        private static bool RunOne(
            int seed,
            int ticks,
            Func<int, SimulationConfig> configure,
            SimulationScenario scenario,
            List<double>[] tolerance,
            List<double>[] control,
            List<double> deviations)
        {
            SimulationConfig config = configure(seed);
            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);

            // Statistics are rebuilt every BaseFrequencyHz / StatisticsHz ticks; reading them before
            // that returns a zeroed struct. Same warm-up the drift measurement uses.
            int warmup = Math.Max(2, config.Schedule.BaseFrequencyHz / config.Schedule.StatisticsHz);

            var toleranceRun = new double[Checkpoints + 1];
            var controlRun = new double[Checkpoints + 1];
            var deviationRun = new List<double>();

            for (int tick = 0; tick <= ticks; tick++)
            {
                if (tick == warmup)
                {
                    toleranceRun[0] = world.Statistics.MeanTemperatureToleranceGene;
                    controlRun[0] = world.Statistics.MeanNeutralMarkerGene;
                }

                int slot = (int)((long)tick * Checkpoints / ticks);
                if (slot > 0 && tick == (long)ticks * slot / Checkpoints)
                {
                    toleranceRun[slot] = world.Statistics.MeanTemperatureToleranceGene;
                    controlRun[slot] = world.Statistics.MeanNeutralMarkerGene;
                    SampleDeviations(world, deviationRun);
                }

                if (tick < ticks)
                {
                    world.Step(config.FixedDeltaTime);
                    world.Events.Clear();
                }
            }

            if (world.Statistics.Population == 0) return false;

            for (int slot = 0; slot <= Checkpoints; slot++)
            {
                tolerance[slot].Add(toleranceRun[slot]);
                control[slot].Add(controlRun[slot]);
            }

            deviations.AddRange(deviationRun);
            return true;
        }

        /// <summary>How far from comfortable the ground under each living creature actually is.</summary>
        private static void SampleDeviations(SimulationWorld world, List<double> into)
        {
            for (int index = 0; index < world.CreatureCount; index++)
            {
                MovementState movement = world.GetCreatureMovementAt(index);
                // The world's own climate, not TemperatureField - otherwise the --terrain-temperature
                // arm would report deviations from a field its creatures are not living in.
                into.Add(Math.Abs(world.Climate.Celsius(movement.Position, world.CurrentTick) - 20f));
            }
        }

        private static string Format(List<double> sorted, double fraction)
        {
            return Percentile(sorted, fraction).ToString("0.0000");
        }

        private static double StandardDeviation(List<double> values)
        {
            if (values.Count < 2) return double.NaN;
            double mean = values.Average();
            double total = 0d;
            foreach (double value in values) total += (value - mean) * (value - mean);
            return Math.Sqrt(total / (values.Count - 1));
        }

        private static double Mean(List<double> values)
        {
            return values.Count == 0 ? double.NaN : values.Average();
        }

        private static double Percentile(List<double> sorted, double fraction)
        {
            if (sorted.Count == 0) return double.NaN;
            int rank = (int)Math.Round(fraction * (sorted.Count - 1));
            return sorted[Math.Min(sorted.Count - 1, Math.Max(0, rank))];
        }
    }
}

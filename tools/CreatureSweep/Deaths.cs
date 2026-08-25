using System;
using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// Which selective channel is even open — asked before building an instrument to measure it.
    ///
    /// <para><c>UrgencyExponent</c> is under the most reproducible selection in the model, nine
    /// conditions out of nine negative at |t| up to 19.4, and there are two live explanations that
    /// both predict exactly that sign:</para>
    ///
    /// <list type="number">
    /// <item><b>The reproduction gates.</b> <c>CanSeekMate</c> needs energy, hydration and health all
    /// at 80% and <c>CanReproduce</c> needs all three at 70%, so a sluggish eater can sit below the
    /// threshold and never breed. Selection through <i>fertility</i>.</item>
    /// <item><b>Starvation.</b> A creature that waits until it is nearly empty may simply not reach
    /// food in time. Selection through <i>survival</i>.</item>
    /// </list>
    ///
    /// <para>The gate test costs a configuration value, a hash version bump and three guard updates.
    /// <b>This costs one run</b>, and it can retire hypothesis 2 outright: if almost nobody dies of
    /// starvation or dehydration, the survival channel cannot be carrying a t of 19. Mean energy and
    /// hydration answer the other half — a population hovering at the gate is what the first
    /// explanation looks like from outside.</para>
    /// </summary>
    internal static class Deaths
    {
        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            var totals = new long[7];
            var energy = new List<double>();
            var hydration = new List<double>();
            long population = 0;
            int extinct = 0;

            for (int index = 0; index < seedCount; index++)
            {
                SimulationConfig config = configure(Program.FirstSeed + index);
                var world = new SimulationWorld(config);
                scenario.ApplyTo(world);
                for (int tick = 0; tick < ticks; tick++)
                {
                    world.Step(config.FixedDeltaTime);
                    world.Events.Clear();
                }

                SimulationStatistics statistics = world.Statistics;
                if (statistics.Population == 0)
                {
                    extinct++;
                    continue;
                }

                totals[(int)DeathCause.Starvation] += statistics.StarvationDeathCount;
                totals[(int)DeathCause.Dehydration] += statistics.DehydrationDeathCount;
                totals[(int)DeathCause.Age] += statistics.AgeDeathCount;
                totals[(int)DeathCause.Health] += statistics.HealthDeathCount;
                totals[(int)DeathCause.Predation] += statistics.PredationDeathCount;
                energy.Add(statistics.MeanEnergyFraction);
                hydration.Add(statistics.MeanHydrationFraction);
                population += statistics.Population;
            }

            int counted = seedCount - extinct;
            long named = totals.Sum();
            Console.WriteLine();
            Console.WriteLine("death causes over " + counted + " surviving runs of " + seedCount
                + " (" + named + " deaths attributed)");
            Write("starvation", totals[(int)DeathCause.Starvation], named);
            Write("dehydration", totals[(int)DeathCause.Dehydration], named);
            Write("age", totals[(int)DeathCause.Age], named);
            Write("health", totals[(int)DeathCause.Health], named);
            Write("predation", totals[(int)DeathCause.Predation], named);

            Console.WriteLine();
            Console.WriteLine("the gates sit at 0.70 (breed) and 0.80 (seek a mate) on all three needs");
            Console.WriteLine("  mean energy fraction    " + Mean(energy).ToString("0.0000"));
            Console.WriteLine("  mean hydration fraction " + Mean(hydration).ToString("0.0000"));
            Console.WriteLine("  mean final population   " + (counted == 0 ? 0d : (double)population / counted).ToString("0.0"));
        }

        private static void Write(string name, long count, long total)
        {
            string share = total == 0 ? "n/a" : (100d * count / total).ToString("0.0") + "%";
            Console.WriteLine("  " + name.PadRight(12) + count.ToString().PadLeft(8) + "   " + share.PadLeft(6));
        }

        private static double Mean(List<double> values)
        {
            return values.Count == 0 ? double.NaN : values.Average();
        }
    }
}

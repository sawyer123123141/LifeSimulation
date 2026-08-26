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
            float gate = configure(Program.FirstSeed).ReproductionNeedFraction;
            var totals = new long[7];
            var energy = new List<double>();
            var hydration = new List<double>();
            var health = new List<double>();
            var sterile = new List<double>();
            var populations = new List<double>();
            int extinct = 0;

            // Counted over EVERY run, extinct ones included, and reported separately below.
            //
            // The block above deliberately conditions on survivors, because a mean energy taken over
            // a dead world is meaningless. But when a whole arm dies - which is what the predation
            // profile does - conditioning on survivors reports zero deaths of zero causes over zero
            // runs, and the instrument says nothing at all about a result it fully measured. That
            // happened on 2026-08-26 and this exists because of it.
            var allTotals = new long[7];
            var allBirths = new List<double>();
            var allAttacks = new List<double>();
            var allCombatDamage = new List<double>();
            var defenseLiving = new List<double>();
            var defenseDead = new List<double>();
            var defensePredated = new List<double>();

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
                allTotals[(int)DeathCause.Starvation] += statistics.StarvationDeathCount;
                allTotals[(int)DeathCause.Dehydration] += statistics.DehydrationDeathCount;
                allTotals[(int)DeathCause.Age] += statistics.AgeDeathCount;
                allTotals[(int)DeathCause.Health] += statistics.HealthDeathCount;
                allTotals[(int)DeathCause.Predation] += statistics.PredationDeathCount;
                allBirths.Add(statistics.BirthCount);
                allAttacks.Add(statistics.AttackHitCount);
                allCombatDamage.Add(statistics.CumulativeCombatDamage);
                if (statistics.Population > 0) defenseLiving.Add(statistics.MeanDefenseGene);
                if (statistics.MeanDefenseAtDeath > 0f) defenseDead.Add(statistics.MeanDefenseAtDeath);
                if (statistics.MeanDefenseAtPredationDeath > 0f) defensePredated.Add(statistics.MeanDefenseAtPredationDeath);
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
                health.Add(HealthFraction(world, gate + SimulationConfig.MateSeekingNeedMargin, out double sterileShare));
                sterile.Add(sterileShare);
                populations.Add(statistics.Population);
            }

            long allNamed = allTotals.Sum();
            Console.WriteLine();
            Console.WriteLine("death causes over ALL " + seedCount + " runs, extinct included ("
                + allNamed + " deaths attributed)");
            Write("starvation", allTotals[(int)DeathCause.Starvation], allNamed);
            Write("dehydration", allTotals[(int)DeathCause.Dehydration], allNamed);
            Write("age", allTotals[(int)DeathCause.Age], allNamed);
            Write("health", allTotals[(int)DeathCause.Health], allNamed);
            Write("predation", allTotals[(int)DeathCause.Predation], allNamed);
            // Births separate deaths that ended a working population from a founder cohort that never
            // reproduced at all - the recorded predation failure mode is "zero births", which a death
            // mix alone cannot distinguish from a population that lived and then collapsed.
            Console.WriteLine("  births per run          " + Mean(allBirths).ToString("0.0")
                + "   attack hits per run " + Mean(allAttacks).ToString("0.0"));
            // Health removed by combat, which is the quantity the mortality-versus-fertility question
            // turns on and which nothing reported until 2026-08-26.
            Console.WriteLine("  combat damage per run   " + Mean(allCombatDamage).ToString("0.0"));
            // Defense of the living against defense of the dead. If death is concentrated on the
            // low-defense tail these separate; if it is random in defense they do not.
            Console.WriteLine("  defense: living " + Mean(defenseLiving).ToString("0.0000")
                + "   all dead " + Mean(defenseDead).ToString("0.0000")
                + "   predated " + Mean(defensePredated).ToString("0.0000")
                + "   (runs contributing: " + defenseDead.Count + " / " + defensePredated.Count + ")");

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
            Console.WriteLine("the gates sit at " + gate.ToString("0.00") + " (breed) and "
                + (gate + SimulationConfig.MateSeekingNeedMargin).ToString("0.00")
                + " (seek a mate) on all three needs");
            Console.WriteLine("  mean energy fraction    " + Mean(energy).ToString("0.0000"));
            Console.WriteLine("  mean hydration fraction " + Mean(hydration).ToString("0.0000"));
            // Health never regenerates - five subtractions in NeedsSystem and no addition anywhere -
            // so this is a one-way ratchet, and it is one of the THREE conditions on the gate above.
            // A creature that loses a fifth of its health is permanently unable to seek a mate.
            Console.WriteLine("  mean health fraction    " + Mean(health).ToString("0.0000"));
            Console.WriteLine("  below the health gate   " + (100d * Mean(sterile)).ToString("0.0") + "%  <- cannot seek a mate, ever");
            // Population SPREAD is what separates a habitat from a ceiling. A carrying capacity
            // produces a distribution; a cap produces a constant. Eleven committed corpora and 4,080
            // runs have a population column with zero variance, which is why this is printed.
            var sortedPopulations = new List<double>(populations);
            sortedPopulations.Sort();
            Console.WriteLine("  final population        mean " + Mean(populations).ToString("0.0")
                + "  min " + Percentile(sortedPopulations, 0d).ToString("0")
                + "  median " + Percentile(sortedPopulations, 0.5d).ToString("0")
                + "  max " + Percentile(sortedPopulations, 1d).ToString("0")
                + "  sd " + StandardDeviation(populations).ToString("0.00"));
        }

        /// <summary>
        /// Mean health as a fraction of capacity, and the share of the living that are under the
        /// mate-seeking gate on health alone.
        ///
        /// <para><b>Health never regenerates.</b> <c>NeedsSystem</c> subtracts from it in five places
        /// and nothing anywhere adds to it, so it is a one-way ratchet from birth - and it is one of
        /// the three conditions on the gate. A creature that loses a fifth of its health is not
        /// injured, it is <b>permanently sterile</b> for the rest of its life.</para>
        /// </summary>
        private static double HealthFraction(SimulationWorld world, float seekGate, out double sterileShare)
        {
            double total = 0d;
            int under = 0;
            int counted = 0;
            for (int index = 0; index < world.CreatureCount; index++)
            {
                float capacity = world.Creatures.GetPhenotypeAt(index).HealthCapacity;
                if (capacity <= 0f) continue;
                float fraction = world.GetCreatureNeedsAt(index).Health / capacity;
                total += fraction;
                if (fraction < seekGate) under++;
                counted++;
            }

            sterileShare = counted == 0 ? double.NaN : (double)under / counted;
            return counted == 0 ? double.NaN : total / counted;
        }

        private static void Write(string name, long count, long total)
        {
            string share = total == 0 ? "n/a" : (100d * count / total).ToString("0.0") + "%";
            Console.WriteLine("  " + name.PadRight(12) + count.ToString().PadLeft(8) + "   " + share.PadLeft(6));
        }

        private static double Percentile(List<double> sorted, double fraction)
        {
            if (sorted.Count == 0) return double.NaN;
            int rank = (int)Math.Round(fraction * (sorted.Count - 1));
            return sorted[Math.Min(sorted.Count - 1, Math.Max(0, rank))];
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// Realised energy intake against the diet gene — the measurement that says WHY the digestion
    /// trade-off does not reach fitness.
    ///
    /// <para><b>What is already established.</b> `DietSpecialization` is neutral in every
    /// configuration measured: drift t of +0.59 / -0.21 / +0.52 / +1.56 against a control that moves
    /// as much, and a near-uniform distribution that drifts to fixation independently per world, even
    /// where a third of deaths are starvation
    /// (`docs/experiments/p3-digestion-strategies-2026-08-30.md`). The source says it should not be
    /// neutral: `PlantFoodYieldMultiplier` falls from 1.0 to 0.7 across the gene's range.</para>
    ///
    /// <para><b>The hypothesis this tests, and it is only a hypothesis.</b> The penalty is on yield
    /// per unit eaten, and patches sit at capacity, so a creature with poor plant yield may simply
    /// eat more and arrive at the same energy. If that is what happens, plant intake per feeding tick
    /// falls with diet while time spent eating rises and total intake stays flat — and the trade-off
    /// is real in the source and cancelled by behaviour. If instead total intake falls with diet and
    /// the gene is still neutral, the cost is real and something else is absorbing it.</para>
    ///
    /// <para>An outside observer, in the manner of <c>CreatureActionHistory</c>: it reads the world
    /// and never writes to it, so it cannot change a tick or a hash.</para>
    /// </summary>
    internal static class Intake
    {
        private const int BinCount = 5;

        private static double Correlation(double[] first, double[] second)
        {
            int count = first.Length;
            if (count < 2) return 0d;

            double firstMean = first.Average();
            double secondMean = second.Average();
            double covariance = 0d;
            double firstVariance = 0d;
            double secondVariance = 0d;
            for (int index = 0; index < count; index++)
            {
                double a = first[index] - firstMean;
                double b = second[index] - secondMean;
                covariance += a * b;
                firstVariance += a * a;
                secondVariance += b * b;
            }

            double denominator = Math.Sqrt(firstVariance * secondVariance);
            return denominator <= 0d ? 0d : covariance / denominator;
        }

        private sealed class Tally
        {
            public float Diet;
            public double PlantGain;
            public double MeatGain;
            public long PlantTicks;
            public long MeatTicks;
            public long AliveTicks;
            public float LastEnergy;
            public bool Seen;
            public int Offspring;
        }

        private static void CreditParent(Dictionary<long, Tally> tallies, long seedKey, CreatureId parent)
        {
            if (parent.Value <= 0L) return;
            if (tallies.TryGetValue(seedKey | (uint)parent.Value, out Tally tally)) tally.Offspring++;
        }

        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            var tallies = new Dictionary<long, Tally>();
            int extinct = 0;
            int surviving = 0;

            for (int index = 0; index < seedCount; index++)
            {
                SimulationConfig config = configure(Program.FirstSeed + index);
                var world = new SimulationWorld(config);
                scenario.ApplyTo(world);

                // Keyed by seed and creature so ids from different worlds cannot collide.
                long seedKey = (long)(Program.FirstSeed + index) << 32;

                for (int tick = 0; tick < ticks; tick++)
                {
                    world.Step(config.FixedDeltaTime);

                    // Births, before the buffer is cleared. Offspring count is the fitness measure
                    // the intake table was missing: energy taken in only matters if it becomes
                    // descendants.
                    for (int eventIndex = 0; eventIndex < world.Events.Count; eventIndex++)
                    {
                        SimulationEvent simulationEvent = world.Events.GetAt(eventIndex);
                        if (simulationEvent.Kind != SimulationEventKind.Birth) continue;

                        CreditParent(tallies, seedKey, simulationEvent.FirstRelated);
                        CreditParent(tallies, seedKey, simulationEvent.SecondRelated);
                    }

                    world.Events.Clear();

                    for (int creature = 0; creature < world.CreatureCount; creature++)
                    {
                        long key = seedKey | (uint)world.GetCreatureIdAt(creature).Value;
                        if (!tallies.TryGetValue(key, out Tally tally))
                        {
                            tally = new Tally { Diet = world.Creatures.GetGenomeAt(creature).DietSpecialization };
                            tallies[key] = tally;
                        }

                        float energy = world.GetCreatureNeedsAt(creature).Energy;
                        CreatureAction action = world.GetCreatureDecisionAt(creature).Action;

                        if (tally.Seen)
                        {
                            // Energy rises only through ingestion; everything else drains it. A
                            // positive delta while the action is Eat or FeedCarcass is the intake
                            // for that tick, attributed to what it was eating.
                            float delta = energy - tally.LastEnergy;
                            if (delta > 0f)
                            {
                                if (action == CreatureAction.Eat) tally.PlantGain += delta;
                                else if (action == CreatureAction.FeedCarcass) tally.MeatGain += delta;
                            }
                        }

                        if (action == CreatureAction.Eat) tally.PlantTicks++;
                        else if (action == CreatureAction.FeedCarcass) tally.MeatTicks++;

                        tally.AliveTicks++;
                        tally.LastEnergy = energy;
                        tally.Seen = true;
                    }
                }

                if (world.CreatureCount == 0) extinct++;
                else surviving++;
            }

            Tally[] measured = tallies.Values.Where(tally => tally.AliveTicks > 200L).ToArray();
            Console.WriteLine();
            Console.WriteLine($"realised intake against the diet gene, {surviving} surviving runs of {seedCount}");
            Console.WriteLine($"{measured.Length} creatures that lived more than 200 ticks");
            if (measured.Length == 0)
            {
                Console.WriteLine("  nothing lived long enough to measure");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("| diet | creatures | plant energy/1k ticks | meat energy/1k ticks | TOTAL/1k | plant ticks % | meat ticks % | plant energy per eating tick | lifetime intake | OFFSPRING | ticks alive |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (int bin = 0; bin < BinCount; bin++)
            {
                double low = bin / (double)BinCount;
                double high = (bin + 1) / (double)BinCount;
                Tally[] group = measured
                    .Where(tally => tally.Diet >= low && (tally.Diet < high || (bin == BinCount - 1 && tally.Diet <= 1f)))
                    .ToArray();
                if (group.Length == 0)
                {
                    Console.WriteLine($"| {low:0.0}-{high:0.0} | 0 | - | - | - | - | - | - |");
                    continue;
                }

                double plantRate = group.Average(tally => tally.PlantGain / tally.AliveTicks * 1000d);
                double meatRate = group.Average(tally => tally.MeatGain / tally.AliveTicks * 1000d);
                double plantShare = group.Average(tally => tally.PlantTicks / (double)tally.AliveTicks) * 100d;
                double meatShare = group.Average(tally => tally.MeatTicks / (double)tally.AliveTicks) * 100d;
                double perEatingTick = group.Where(tally => tally.PlantTicks > 0).Select(tally => tally.PlantGain / tally.PlantTicks).DefaultIfEmpty(0d).Average();

                double lifetimeIntake = group.Average(tally => tally.PlantGain + tally.MeatGain);
                double offspring = group.Average(tally => (double)tally.Offspring);
                double aliveTicks = group.Average(tally => (double)tally.AliveTicks);
                Console.WriteLine($"| {low:0.0}-{high:0.0} | {group.Length} | {plantRate:0.000} | {meatRate:0.000}"
                    + $" | {plantRate + meatRate:0.000} | {plantShare:0.00} | {meatShare:0.000} | {perEatingTick:0.0000}"
                    + $" | {lifetimeIntake:0.0} | {offspring:0.000} | {aliveTicks:0} |");
            }

            Console.WriteLine();
            Console.WriteLine($"  meat is {measured.Sum(t => t.MeatGain) * 100d / Math.Max(1d, measured.Sum(t => t.PlantGain + t.MeatGain)):0.00}% of all energy ingested");

            // DOES INTAKE BECOME FITNESS? The diet table shows a 12% spread in intake that selects
            // nothing. Either intake does not predict offspring at all - in which case no energy
            // trade-off can ever select - or it does, and something specific to diet cancels it.
            double[] intakes = measured.Select(tally => tally.PlantGain + tally.MeatGain).ToArray();
            double[] offspringCounts = measured.Select(tally => (double)tally.Offspring).ToArray();
            double[] lifetimes = measured.Select(tally => (double)tally.AliveTicks).ToArray();
            Console.WriteLine();
            Console.WriteLine("  does intake become fitness?");
            Console.WriteLine($"    corr(lifetime intake, offspring)   {Correlation(intakes, offspringCounts):0.000}");
            Console.WriteLine($"    corr(ticks alive,     offspring)   {Correlation(lifetimes, offspringCounts):0.000}");
            Console.WriteLine($"    corr(lifetime intake, ticks alive) {Correlation(intakes, lifetimes):0.000}");

            // Intake per tick alive strips out "lived longer, therefore ate more", which is the
            // obvious confound in the first correlation.
            double[] intakeRates = measured.Select(tally => (tally.PlantGain + tally.MeatGain) / tally.AliveTicks).ToArray();
            Console.WriteLine($"    corr(intake RATE,     offspring)   {Correlation(intakeRates, offspringCounts):0.000}");
            Console.WriteLine($"    mean offspring {offspringCounts.Average():0.000}, of {measured.Length} creatures; {offspringCounts.Count(value => value > 0d) * 100d / measured.Length:0.0}% left any");
            Console.WriteLine($"  extinct runs: {extinct}");
        }
    }
}

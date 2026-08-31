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
            Console.WriteLine("| diet | creatures | plant energy/1k ticks | meat energy/1k ticks | TOTAL/1k | plant ticks % | meat ticks % | plant energy per eating tick |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|");
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

                Console.WriteLine($"| {low:0.0}-{high:0.0} | {group.Length} | {plantRate:0.000} | {meatRate:0.000}"
                    + $" | {plantRate + meatRate:0.000} | {plantShare:0.00} | {meatShare:0.000} | {perEatingTick:0.0000} |");
            }

            Console.WriteLine();
            Console.WriteLine($"  meat is {measured.Sum(t => t.MeatGain) * 100d / Math.Max(1d, measured.Sum(t => t.PlantGain + t.MeatGain)):0.00}% of all energy ingested");
            Console.WriteLine($"  extinct runs: {extinct}");
        }
    }
}

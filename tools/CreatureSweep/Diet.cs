using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// The DISTRIBUTION of <c>DietSpecialization</c>, because a mean cannot show two modes.
    ///
    /// <para><b>Why this exists.</b> P3's exit gate is "at least two distinct trait strategies
    /// persist because they exploit different conditions". On 2026-08-30 that was tested with the
    /// drift table, which reports a <i>mean</i> per gene, and the answer came back t +1.56 against a
    /// control of +1.62 - unselected. But a population half of which sits at 0.2 and half at 0.8 has
    /// a mean of 0.5 and no drift, and would be reported as nothing happening while being exactly the
    /// result the gate asks for. **The instrument could not have seen the thing it was pointed at.**
    /// This closes that gap before any mechanism is changed on the strength of the negative.</para>
    ///
    /// <para>The number that matters most is the share above
    /// <c>PredationSystem.MinimumHuntingDiet</c> = 0.58, because below it a creature cannot hunt at
    /// all: it is the fraction of the population that is even capable of being a carnivore.</para>
    /// </summary>
    internal static class Diet
    {
        /// <summary>Matches <c>PredationSystem.MinimumHuntingDiet</c>. Repeated because that constant is private.</summary>
        private const float MinimumHuntingDiet = .58f;

        private const int BinCount = 10;

        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            var bins = new long[BinCount];
            var all = new List<double>();
            var perRunHunterShare = new List<double>();
            var hunterOtherTraits = new List<double[]>();
            var grazerOtherTraits = new List<double[]>();
            long total = 0;
            int extinct = 0;
            int surviving = 0;

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

                if (world.CreatureCount == 0)
                {
                    extinct++;
                    continue;
                }

                surviving++;
                int hunters = 0;
                for (int creature = 0; creature < world.CreatureCount; creature++)
                {
                    Genome genome = world.Creatures.GetGenomeAt(creature);
                    float diet = genome.DietSpecialization;
                    all.Add(diet);
                    total++;

                    int bin = (int)(diet * BinCount);
                    if (bin >= BinCount) bin = BinCount - 1;
                    if (bin < 0) bin = 0;
                    bins[bin]++;

                    // If two strategies exist, the ones capable of hunting should differ in more than
                    // diet - a carnivore that is no more aggressive and no better armed than a grazer
                    // is a number, not a strategy.
                    double[] traits =
                    {
                        genome.Attack, genome.Aggression, genome.Defense,
                        genome.MovementSpeed, genome.BodySize, genome.VisionRange,
                    };

                    if (diet >= MinimumHuntingDiet)
                    {
                        hunters++;
                        hunterOtherTraits.Add(traits);
                    }
                    else
                    {
                        grazerOtherTraits.Add(traits);
                    }
                }

                perRunHunterShare.Add(world.CreatureCount == 0 ? 0d : hunters / (double)world.CreatureCount);
            }

            Console.WriteLine();
            Console.WriteLine($"diet_specialization distribution over {surviving} surviving runs of {seedCount} ({total} creatures)");
            if (total == 0)
            {
                Console.WriteLine("  every run went extinct - nothing to describe");
                return;
            }

            double mean = all.Average();
            double sd = Math.Sqrt(all.Sum(value => (value - mean) * (value - mean)) / all.Count);
            Console.WriteLine($"  mean {mean:0.000}   sd {sd:0.000}   min {all.Min():0.000}   max {all.Max():0.000}");
            Console.WriteLine();

            long peak = bins.Max();
            for (int bin = 0; bin < BinCount; bin++)
            {
                double low = bin / (double)BinCount;
                double high = (bin + 1) / (double)BinCount;
                int barLength = peak == 0 ? 0 : (int)Math.Round(50d * bins[bin] / peak);
                string marker = low <= MinimumHuntingDiet && MinimumHuntingDiet < high ? "  <- hunting threshold 0.58" : string.Empty;
                Console.WriteLine($"  {low:0.0}-{high:0.0} {new string('#', barLength).PadRight(50)} {bins[bin],6} ({bins[bin] * 100d / total,5:0.0}%){marker}");
            }

            Console.WriteLine();
            double hunterShare = all.Count(value => value >= MinimumHuntingDiet) / (double)total;
            Console.WriteLine($"  able to hunt (diet >= {MinimumHuntingDiet:0.00}): {hunterShare * 100d:0.0}% of all creatures");
            if (perRunHunterShare.Count > 0)
            {
                Console.WriteLine($"  per-run hunter share: mean {perRunHunterShare.Average() * 100d:0.0}%"
                    + $"  min {perRunHunterShare.Min() * 100d:0.0}%  max {perRunHunterShare.Max() * 100d:0.0}%");
            }

            // Bimodality, stated plainly rather than with a test nobody will re-derive: a population
            // with two strategies has a DIP between two peaks. If the middle bins are the fullest,
            // there is one strategy and it is a generalist.
            long middle = bins[4] + bins[5];
            long ends = bins[0] + bins[1] + bins[8] + bins[9];
            Console.WriteLine($"  middle two bins {middle * 100d / total:0.0}%  vs  outer four bins {ends * 100d / total:0.0}%"
                + (middle > ends ? "   -> UNIMODAL, centred: one generalist strategy" : "   -> mass at the ends: look for two strategies"));

            if (hunterOtherTraits.Count > 0 && grazerOtherTraits.Count > 0)
            {
                string[] names = { "attack", "aggression", "defense", "movement_speed", "body_size", "vision_range" };
                Console.WriteLine();
                Console.WriteLine("  do the hunt-capable differ in anything else?");
                for (int trait = 0; trait < names.Length; trait++)
                {
                    double hunter = hunterOtherTraits.Average(values => values[trait]);
                    double grazer = grazerOtherTraits.Average(values => values[trait]);
                    Console.WriteLine($"    {names[trait],-15} hunt-capable {hunter:0.000}   rest {grazer:0.000}   difference {hunter - grazer:+0.000;-0.000; 0.000}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"  extinct runs: {extinct}");
        }
    }
}

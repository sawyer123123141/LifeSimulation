using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// What does making creatures pay for climbing do to them?
    ///
    /// <para>Paired on the seed: the same world, the same founders, the same everything, run twice
    /// with <c>slopeMovementCostEnabled</c> the only difference. Both arms have the terrain join on,
    /// because without elevation the flag is inert by construction and the comparison would be of a
    /// flag against itself.</para>
    ///
    /// <para><b>What would count as an effect.</b> Fewer survivors, or lower energy, or a shift in a
    /// gene that plausibly responds to the cost of moving - movement speed, metabolic pace, travel
    /// sensitivity, vision range. <c>NeutralMarker</c> is carried as a control: it does nothing, so
    /// if it moves as much as the others, what is being measured is drift.</para>
    ///
    /// <para><b>Multiple comparisons.</b> Fourteen columns are reported. At |t| = 2 roughly one in
    /// twenty crosses by chance, so one or two significant cells in fourteen is noise, and the
    /// control column is there to say so out loud.</para>
    /// </summary>
    internal static class Program
    {
        /// <summary>Matches tools/PlantSweep exactly, so the two corpora are comparable.</summary>
        private const int Ticks = 12000;
        private const int Founders = 12;
        private const int FirstSeed = 42;
        private const int MaximumPopulation = 48;

        private static int _seedCount = 120;

        /// <summary>
        /// The focused arm: seeds chosen for having a hill, and population headroom to die into.
        ///
        /// <para>The first sweep's null carried two limitations it could not overcome. Half the
        /// arenas were flat, so half the pairs were byte-identical; and 96 of 120 pairs finished at
        /// the population cap, so a survival effect smaller than the headroom could not appear. This
        /// mode removes both. The cap is <b>not</b> the plant corpus's 48, deliberately - which means
        /// results here are not comparable with that corpus and are not meant to be.</para>
        /// </summary>
        private static bool _focused;

        /// <summary>
        /// Population ceiling for the focused arm, overridable per run.
        ///
        /// <para>200 was the first value and it overshot: raising the cap from the plant corpus's 48
        /// gave survival room to move and also let populations boom and crash, so 33 of 60 pairs died
        /// in <i>both</i> arms and the metric that had been saturated became one that was mostly
        /// zero. It is an argument rather than a constant so a run's condition is chosen and recorded
        /// rather than edited into the source.</para>
        /// </summary>
        private static int _focusedPopulationCap = 200;
        private const double MinimumClimbMetres = 5d;

        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--relief")
            {
                ReportRelief();
                return;
            }

            if (args.Length > 0 && args[0] == "--focused")
            {
                _focused = true;
                if (args.Length > 1 && int.TryParse(args[1], out int focusedSeeds)) _seedCount = focusedSeeds;
                if (args.Length > 2 && int.TryParse(args[2], out int cap)) _focusedPopulationCap = cap;
            }
            else if (args.Length > 0 && int.TryParse(args[0], out int seeds))
            {
                _seedCount = seeds;
            }

            int[] chosen;
            if (_focused)
            {
                Console.Error.WriteLine("selecting seeds with at least " + MinimumClimbMetres + " m of climb");
                chosen = Relief.WithRelief(FirstSeed, _seedCount, MinimumClimbMetres);
                Console.Error.WriteLine("  " + chosen.Length + " seeds, highest " + chosen[chosen.Length - 1]);
            }
            else
            {
                chosen = new int[_seedCount];
                for (int index = 0; index < _seedCount; index++) chosen[index] = FirstSeed + index;
            }

            var runs = new List<RunSpec>();
            foreach (bool slope in new[] { false, true })
            {
                foreach (int seed in chosen) runs.Add(new RunSpec(slope, seed));
            }

            Console.Error.WriteLine(runs.Count + " runs of " + Ticks + " ticks");
            var results = new ConcurrentBag<RunResult>();
            int done = 0;
            Parallel.ForEach(runs, spec =>
            {
                results.Add(Execute(spec));
                int count = Interlocked.Increment(ref done);
                if (count % 20 == 0) Console.Error.WriteLine("  " + count + "/" + runs.Count);
            });

            RunResult[] ordered = results
                .OrderBy(result => result.Slope).ThenBy(result => result.Seed)
                .ToArray();

            WriteCsv(ordered);
            Report(ordered);
        }

        private readonly struct RunSpec
        {
            public RunSpec(bool slope, int seed)
            {
                Slope = slope;
                Seed = seed;
            }

            public bool Slope { get; }
            public int Seed { get; }
        }

        private sealed class RunResult
        {
            public bool Slope;
            public int Seed;
            public ulong Hash;
            public int Population;
            public bool Extinct;
            public double Energy;
            public double OccupiedElevation;
            public double OccupiedSlope;
            public double[] Genes;
            public double[] Founder;
        }

        /// <summary>
        /// The full-ecosystem configuration with the terrain join on, and the slope cost as the only
        /// arm. Every other flag is held where <c>CreatePrototype4Defaults</c> and the plant sweep
        /// put it, so a difference between arms is attributable to the one flag that moved.
        /// </summary>
        private static SimulationConfig CreateConfig(int worldSeed, bool slope)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, Founders);
            return new SimulationConfig(
                worldSeed,
                Founders,
                defaults.Schedule,
                _focused ? _focusedPopulationCap : MaximumPopulation,
                FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: slope);
        }

        private static readonly string[] GeneNames =
        {
            "body_size", "movement_speed", "metabolic_pace", "vision_range", "water_efficiency",
            "food_efficiency", "temperature_tolerance", "fertility_investment", "lifespan_tendency",
            "urgency_exponent", "travel_sensitivity", "risk_aversion", "neutral_marker",
        };

        private static double[] Genes(SimulationStatistics statistics)
        {
            return new double[]
            {
                statistics.MeanBodySizeGene, statistics.MeanMovementSpeedGene,
                statistics.MeanMetabolicPaceGene, statistics.MeanVisionRangeGene,
                statistics.MeanWaterEfficiencyGene, statistics.MeanFoodEfficiencyGene,
                statistics.MeanTemperatureToleranceGene, statistics.MeanFertilityInvestmentGene,
                statistics.MeanLifespanTendencyGene, statistics.MeanUrgencyExponentGene,
                statistics.MeanTravelSensitivityGene, statistics.MeanRiskAversionGene,
                statistics.MeanNeutralMarkerGene,
            };
        }

        private static RunResult Execute(RunSpec spec)
        {
            SimulationConfig config = CreateConfig(spec.Seed, spec.Slope);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            // Founder means, taken once statistics have actually been sampled.
            //
            // Statistics are rebuilt every BaseFrequencyHz / StatisticsHz ticks, not every tick, so
            // reading them after a single step returns a default-valued struct - every gene zero.
            // Measured against that, all thirteen genes "drifted" by +0.49 at t = 30, control
            // included, which is the population mean rather than any movement. One second of warm-up
            // costs nothing against 12,000 ticks and gives a real baseline.
            int warmup = Math.Max(2, config.Schedule.BaseFrequencyHz / config.Schedule.StatisticsHz);
            for (int tick = 0; tick < warmup; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            double[] founder = Genes(world.Statistics);

            for (int tick = warmup; tick < Ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            SimulationStatistics statistics = world.Statistics;
            Relief.Occupancy(world, out double elevation, out double slope);
            return new RunResult
            {
                Slope = spec.Slope,
                Seed = spec.Seed,
                Hash = world.ComputeBehaviorHash(),
                Population = statistics.Population,
                Extinct = statistics.Population == 0,
                Energy = statistics.MeanEnergyFraction,
                OccupiedElevation = elevation,
                OccupiedSlope = slope,
                Genes = Genes(statistics),
                Founder = founder,
            };
        }

        private static void WriteCsv(RunResult[] results)
        {
            var builder = new StringBuilder();
            builder.Append(ExperimentManifest.Describe(
                CodeRevision(),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                CreateConfig(FirstSeed, slope: true),
                FirstSeed,
                _seedCount,
                Ticks));
            builder.AppendLine();

            builder.Append("arm,seed,hash,population,extinct,energy,occupied_elevation,occupied_slope");
            foreach (string name in GeneNames) builder.Append(",").Append(name);
            builder.AppendLine();

            foreach (RunResult result in results)
            {
                builder.Append(result.Slope ? "slope-on" : "slope-off").Append(",")
                    .Append(result.Seed).Append(",")
                    .Append(result.Hash).Append(",")
                    .Append(result.Population).Append(",")
                    .Append(result.Extinct ? 1 : 0).Append(",")
                    .Append(Format(result.Energy)).Append(",")
                    .Append(Format(result.OccupiedElevation)).Append(",")
                    .Append(Format(result.OccupiedSlope));
                foreach (double gene in result.Genes) builder.Append(",").Append(Format(gene));
                builder.AppendLine();
            }

            string path = Path.Combine(
                "docs", "experiments",
                _focused
                    ? "p6-slope-cost-focused-cap" + _focusedPopulationCap + "-2026-08-24.csv"
                    : "p6-slope-cost-2026-08-24.csv");
            File.WriteAllText(path, builder.ToString());
            Console.Error.WriteLine("wrote " + path);
        }

        /// <summary>
        /// The commit the corpus was produced at. Read from git rather than typed, because a
        /// provenance line somebody has to remember to update is a provenance line that is wrong.
        /// </summary>
        private static string CodeRevision()
        {
            try
            {
                var start = new System.Diagnostics.ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                using System.Diagnostics.Process process = System.Diagnostics.Process.Start(start);
                string revision = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return string.IsNullOrWhiteSpace(revision) ? "unknown" : revision;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static void Report(RunResult[] results)
        {
            RunResult[] on = results.Where(result => result.Slope).OrderBy(result => result.Seed).ToArray();
            RunResult[] off = results.Where(result => !result.Slope).OrderBy(result => result.Seed).ToArray();

            Console.WriteLine("paired, slope-on minus slope-off, " + on.Length + " seeds"
                + (_focused ? ", population cap " + _focusedPopulationCap : string.Empty));
            Console.WriteLine();
            Console.WriteLine("column                  mean      t      n>0");
            Console.WriteLine(Summarise("population", on, off, result => result.Population));
            Console.WriteLine(Summarise("energy", on, off, result => result.Energy));
            Console.WriteLine(Summarise("occupied_elevation", on, off, result => result.OccupiedElevation));
            Console.WriteLine(Summarise("occupied_slope", on, off, result => result.OccupiedSlope));
            for (int gene = 0; gene < GeneNames.Length; gene++)
            {
                int index = gene;
                Console.WriteLine(Summarise(GeneNames[index], on, off, result => result.Genes[index]));
            }

            ReportDrift(off);

            Console.WriteLine();
            Console.WriteLine("extinct: slope-on " + on.Count(result => result.Extinct)
                + ", slope-off " + off.Count(result => result.Extinct));
            Console.WriteLine();
            Console.WriteLine("Fourteen columns at |t| = 2: expect roughly one to cross by chance.");
            Console.WriteLine("neutral_marker is the control - it responds to nothing.");
        }

        /// <summary>
        /// Whether the genes moved <b>at all</b>, measured against where the founders started.
        ///
        /// <para>The paired arm-against-arm table cannot answer this. A trait under strong selection
        /// in both arms cancels exactly, so "the flag moved nothing" and "nothing is happening" look
        /// identical there. This asks the other question: over one run, did the population drift away
        /// from its founders, and did it do so further than <c>NeutralMarker</c> - a gene that is
        /// carried, inherited and mutated exactly like the others and affects nothing at all.</para>
        ///
        /// <para><b>The control is the whole test.</b> Every gene drifts: finite populations lose
        /// variance to chance, and the founders are not the survivors. Selection is the claim that a
        /// gene drifted <i>further than a gene with no consequences did</i>.</para>
        /// </summary>
        private static void ReportDrift(RunResult[] arm)
        {
            Console.WriteLine();
            Console.WriteLine("drift from founders, baseline arm, " + arm.Length + " runs");
            Console.WriteLine();
            // The founder value is printed because a bounded gene that starts away from the middle
            // moves toward it under symmetric mutation alone. Without this column, regression to the
            // centre and selection are the same picture.
            Console.WriteLine("gene                  founder     mean       t     |mean| vs control");

            double control = Math.Abs(MeanDrift(arm, GeneNames.Length - 1));
            for (int gene = 0; gene < GeneNames.Length; gene++)
            {
                var deltas = new List<double>();
                foreach (RunResult result in arm)
                {
                    double delta = result.Genes[gene] - result.Founder[gene];
                    if (!double.IsNaN(delta)) deltas.Add(delta);
                }

                if (deltas.Count < 2) continue;

                double mean = deltas.Average();
                double variance = deltas.Sum(value => (value - mean) * (value - mean)) / (deltas.Count - 1);
                double error = Math.Sqrt(variance / deltas.Count);
                double t = error <= 0d ? 0d : mean / error;
                double ratio = control <= 0d ? double.NaN : Math.Abs(mean) / control;

                var founders = new List<double>();
                foreach (RunResult result in arm)
                {
                    if (!double.IsNaN(result.Founder[gene])) founders.Add(result.Founder[gene]);
                }

                Console.WriteLine(
                    GeneNames[gene].PadRight(19)
                    + Format(founders.Count == 0 ? double.NaN : founders.Average()).PadLeft(9)
                    + Format(mean).PadLeft(9)
                    + Format(t).PadLeft(8)
                    + ratio.ToString("0.00").PadLeft(10)
                    + (gene == GeneNames.Length - 1 ? "   <- control" : string.Empty));
            }
        }

        private static double MeanDrift(RunResult[] arm, int gene)
        {
            var deltas = new List<double>();
            foreach (RunResult result in arm)
            {
                double delta = result.Genes[gene] - result.Founder[gene];
                if (!double.IsNaN(delta)) deltas.Add(delta);
            }

            return deltas.Count == 0 ? 0d : deltas.Average();
        }

        private static string Summarise(
            string label, RunResult[] on, RunResult[] off, Func<RunResult, double> select)
        {
            var differences = new List<double>();
            for (int index = 0; index < on.Length && index < off.Length; index++)
            {
                if (on[index].Seed != off[index].Seed) continue;

                double difference = select(on[index]) - select(off[index]);
                if (!double.IsNaN(difference)) differences.Add(difference);
            }

            if (differences.Count < 2) return label.PadRight(22) + "  (too few pairs)";

            double mean = differences.Average();
            double variance = differences.Sum(value => (value - mean) * (value - mean)) / (differences.Count - 1);
            double error = Math.Sqrt(variance / differences.Count);
            double t = error <= 0d ? 0d : mean / error;

            return label.PadRight(22)
                + Format(mean).PadLeft(9)
                + Format(t).PadLeft(8)
                + differences.Count(value => value > 0d).ToString(CultureInfo.InvariantCulture).PadLeft(6)
                + "/" + differences.Count;
        }

        /// <summary>
        /// What the ground under each arena looks like, seed by seed.
        ///
        /// <para><b>Without this the sweep's null is unreadable.</b> "Charging for climbs changes
        /// nothing" could mean creatures are indifferent to real hills, or that there are no hills to
        /// be indifferent to - opposite findings the trait table cannot tell apart. The same question
        /// sank the terrain join's first result.</para>
        /// </summary>
        private static void ReportRelief()
        {
            Console.WriteLine("seed   climb_per_25m");
            foreach (int seed in new[] { 42, 55, 71, 100, 120, 161 })
            {
                Console.WriteLine(
                    seed.ToString().PadRight(7) + Format(Relief.ClimbPerTraverse(seed)).PadLeft(14));
            }
        }


        private static string Format(double value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }
    }
}

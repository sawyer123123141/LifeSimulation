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

        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--relief")
            {
                ReportRelief();
                return;
            }

            if (args.Length > 0 && int.TryParse(args[0], out int seeds)) _seedCount = seeds;

            var runs = new List<RunSpec>();
            foreach (bool slope in new[] { false, true })
            {
                for (int seed = FirstSeed; seed < FirstSeed + _seedCount; seed++)
                {
                    runs.Add(new RunSpec(slope, seed));
                }
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
            public double[] Genes;
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
                MaximumPopulation,
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

            for (int tick = 0; tick < Ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            SimulationStatistics statistics = world.Statistics;
            return new RunResult
            {
                Slope = spec.Slope,
                Seed = spec.Seed,
                Hash = world.ComputeBehaviorHash(),
                Population = statistics.Population,
                Extinct = statistics.Population == 0,
                Energy = statistics.MeanEnergyFraction,
                Genes = Genes(statistics),
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

            builder.Append("arm,seed,hash,population,extinct,energy");
            foreach (string name in GeneNames) builder.Append(",").Append(name);
            builder.AppendLine();

            foreach (RunResult result in results)
            {
                builder.Append(result.Slope ? "slope-on" : "slope-off").Append(",")
                    .Append(result.Seed).Append(",")
                    .Append(result.Hash).Append(",")
                    .Append(result.Population).Append(",")
                    .Append(result.Extinct ? 1 : 0).Append(",")
                    .Append(Format(result.Energy));
                foreach (double gene in result.Genes) builder.Append(",").Append(Format(gene));
                builder.AppendLine();
            }

            string path = Path.Combine("docs", "experiments", "p6-slope-cost-2026-08-24.csv");
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

            Console.WriteLine("paired, slope-on minus slope-off, " + on.Length + " seeds");
            Console.WriteLine();
            Console.WriteLine("column                  mean      t      n>0");
            Console.WriteLine(Summarise("population", on, off, result => result.Population));
            Console.WriteLine(Summarise("energy", on, off, result => result.Energy));
            for (int gene = 0; gene < GeneNames.Length; gene++)
            {
                int index = gene;
                Console.WriteLine(Summarise(GeneNames[index], on, off, result => result.Genes[index]));
            }

            Console.WriteLine();
            Console.WriteLine("extinct: slope-on " + on.Count(result => result.Extinct)
                + ", slope-off " + off.Count(result => result.Extinct));
            Console.WriteLine();
            Console.WriteLine("Fourteen columns at |t| = 2: expect roughly one to cross by chance.");
            Console.WriteLine("neutral_marker is the control - it responds to nothing.");
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
        /// What the ground under the arena actually looks like.
        ///
        /// <para><b>Without this the sweep's null is unreadable.</b> "Charging for climbs changes
        /// nothing" could mean creatures are indifferent to real hills, or it could mean there are no
        /// hills to be indifferent to - opposite findings that the trait table cannot tell apart. The
        /// same question sank the terrain join's first result, and the answer there was that the
        /// arena is 0.1 radian across and climate is continental.</para>
        ///
        /// <para>Reported per seed: the spread of elevation over the arena in metres, and the climb a
        /// creature accumulates crossing it - which is what the cost is actually charged on.</para>
        /// </summary>
        private static void ReportRelief()
        {
            const int Steps = 41;
            const float Half = 25f;

            Console.WriteLine("seed   range_m     sd_m   climb_per_25m");
            foreach (int seed in new[] { 42, 55, 71, 100, 120, 161 })
            {
                EnvironmentField field = EnvironmentField.CreateTerrainDriven(seed);
                var heights = new double[Steps, Steps];
                double lowest = double.MaxValue;
                double highest = double.MinValue;
                double total = 0d;

                for (int row = 0; row < Steps; row++)
                {
                    for (int column = 0; column < Steps; column++)
                    {
                        float x = -Half + (2f * Half * column / (Steps - 1));
                        float y = -Half + (2f * Half * row / (Steps - 1));
                        double metres = field.Sample(new SimVector2(x, y)).Elevation
                            * PlanetTerrain.MetresPerElevationUnit;
                        heights[row, column] = metres;
                        if (metres < lowest) lowest = metres;
                        if (metres > highest) highest = metres;
                        total += metres;
                    }
                }

                double mean = total / (Steps * Steps);
                double variance = 0d;
                double climb = 0d;
                for (int row = 0; row < Steps; row++)
                {
                    for (int column = 0; column < Steps; column++)
                    {
                        double difference = heights[row, column] - mean;
                        variance += difference * difference;

                        // Uphill only, along one row: the same thing the cost charges for.
                        if (column > 0)
                        {
                            double step = heights[row, column] - heights[row, column - 1];
                            if (step > 0d) climb += step;
                        }
                    }
                }

                Console.WriteLine(
                    seed.ToString().PadRight(7)
                    + Format(highest - lowest).PadLeft(8)
                    + Format(Math.Sqrt(variance / (Steps * Steps))).PadLeft(9)
                    + Format(climb / Steps).PadLeft(15));
            }
        }

        private static string Format(double value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }
    }
}

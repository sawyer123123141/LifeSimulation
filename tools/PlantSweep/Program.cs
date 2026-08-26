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

namespace LifeSimulation.Tools.PlantSweep
{
    /// <summary>
    /// Measures plant trait selection under the flat environment field and under the terrain-driven
    /// one, on otherwise identical runs.
    ///
    /// <para>Both arms are run here rather than comparing terrain against the recorded corpus. The
    /// recorded numbers came from a probe that was never committed, so a difference against them
    /// could be the terrain join or could be the harness. A difference between two arms of THIS
    /// sweep can only be the flag.</para>
    /// </summary>
    internal static class Program
    {
        private const int Ticks = 12000;
        private const int Founders = 12;
        private const int FirstSeed = 42;

        /// <summary>
        /// The recorded condition, and not the configuration default. <c>SimulationConfig</c>
        /// defaults to 1,000, which is not a cap this scenario ever reaches - the recorded corpus
        /// was measured at 48, where the cap binds and grazing pressure is part of the ecology.
        /// </summary>
        private const int MaximumPopulation = 48;

        /// <summary>
        /// The cap actually used, and whether the population is allowed to limit itself instead.
        ///
        /// <para><b>Why these exist.</b> Every plant result on record was measured with the herbivore
        /// population <b>pinned</b> at the cap - 4,080 runs across eleven corpora
        /// (<c>p4-cap-pinning-audit-2026-08-22.md</c>) - which is the scope qualification the whole
        /// plant corpus carries. <c>gradedFertilityEnabled</c> makes the population settle below a cap
        /// it can reach, with real variance
        /// (<c>p6-graded-fertility-closes-the-cap-debt-2026-08-24.md</c>), so the qualification can
        /// finally be tested rather than restated.</para>
        ///
        /// <para>Defaults reproduce the recorded condition exactly.</para>
        /// </summary>
        private static int _maximumPopulation = MaximumPopulation;

        private static bool _gradedFertility;

        private static float _brakeStrength = SimulationConfig.DefaultGradedFertilityStrength;

        private static int _seedCount = 120;

        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--fields")
            {
                ReportFields();
                return;
            }

            foreach (string argument in args)
            {
                if (argument == "--graded-fertility") _gradedFertility = true;
                if (argument.StartsWith("--brake=")
                    && float.TryParse(argument.Substring("--brake=".Length), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float brake))
                {
                    _gradedFertility = true;
                    _brakeStrength = brake;
                }
                if (argument.StartsWith("--cap=") && int.TryParse(argument.Substring("--cap=".Length), out int cap))
                {
                    _maximumPopulation = cap;
                }
            }

            if (args.Length > 0 && int.TryParse(args[0], out int seeds)) _seedCount = seeds;

            var runs = new List<RunSpec>();
            foreach (bool terrain in new[] { false, true })
            {
                foreach (bool contest in new[] { false, true })
                {
                    for (int seed = FirstSeed; seed < FirstSeed + _seedCount; seed++)
                    {
                        runs.Add(new RunSpec(terrain, contest, seed));
                    }
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
                .OrderBy(result => result.Terrain).ThenBy(result => result.Contest).ThenBy(result => result.Seed)
                .ToArray();

            WriteCsv(ordered);
            Report(ordered);
        }

        private readonly struct RunSpec
        {
            public RunSpec(bool terrain, bool contest, int seed)
            {
                Terrain = terrain;
                Contest = contest;
                Seed = seed;
            }

            public bool Terrain { get; }
            public bool Contest { get; }
            public int Seed { get; }
        }

        private sealed class RunResult
        {
            public bool Terrain;
            public bool Contest;
            public int Seed;
            public ulong Hash;
            public int Population;
            public bool Extinct;
            public bool Frozen;
            public double Occupancy;
            public int PlantBirths;
            public int HighestPlantGeneration;
            public double[] Founder;
            public double[] Final;
        }

        /// <summary>
        /// The full-ecosystem configuration, with the establishment contest and the terrain join
        /// made into arms. Every other flag is held at the value
        /// <c>SimulationConfig.CreateFullEcosystemDefaults</c> uses, so an arm difference is
        /// attributable to the one flag that moved.
        /// </summary>
        private static SimulationConfig CreateConfig(int worldSeed, bool contest, bool terrain)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, Founders);
            return new SimulationConfig(
                worldSeed,
                Founders,
                defaults.Schedule,
                _maximumPopulation,
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
                plantEstablishmentContestEnabled: contest,
                plantInvaderEstablishmentContestEnabled: contest,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: terrain,
                gradedFertilityEnabled: _gradedFertility,
                gradedFertilityStrength: _brakeStrength);
        }

        /// <summary>
        /// Founder genomes that differ from each other in every trait, rotated so no two traits are
        /// correlated across sites. Uniform founders leave nothing for selection to act on and the
        /// run measures drift.
        /// </summary>
        private static PlantGenome FounderGenome(int site)
        {
            int count = PlantGenome.TraitCount;
            var traits = new float[count];
            for (int trait = 0; trait < count; trait++)
            {
                int step = (trait + site) % count;
                traits[trait] = 0.30f + (0.40f * step / (count - 1));
            }

            return PlantGenome.FromTraits(traits);
        }

        private static RunResult Execute(RunSpec spec)
        {
            SimulationConfig config = CreateConfig(spec.Seed, spec.Contest, spec.Terrain);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            int count = PlantGenome.TraitCount;
            var founderSum = new double[count];
            int founderCount = 0;
            for (int index = 0; index < world.Plants.Count; index++)
            {
                PlantPatchState patch = world.Plants.GetAt(index);
                if (patch.Biomass <= 0f) continue;

                PlantGenome genome = FounderGenome(founderCount);
                world.Plants.SetGenomeAndLineage(index, genome, patch.Lineage);
                for (int trait = 0; trait < count; trait++) founderSum[trait] += genome.GetTrait(trait);
                founderCount++;
            }

            for (int tick = 0; tick < Ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            var finalSum = new double[count];
            int live = 0;
            for (int index = 0; index < world.Plants.Count; index++)
            {
                PlantPatchState patch = world.Plants.GetAt(index);
                if (patch.Biomass <= 0f) continue;

                for (int trait = 0; trait < count; trait++) finalSum[trait] += patch.Genome.GetTrait(trait);
                live++;
            }

            SimulationStatistics statistics = world.CaptureStatistics();
            return new RunResult
            {
                Terrain = spec.Terrain,
                Contest = spec.Contest,
                Seed = spec.Seed,
                Hash = world.ComputeStateHash(),
                Population = statistics.Population,
                Extinct = statistics.Population == 0,
                Frozen = statistics.HighestPlantGeneration == 0,
                Occupancy = world.PlantSites.Count == 0 ? 0d : live / (double)world.PlantSites.Count,
                PlantBirths = statistics.PlantBirthCount,
                HighestPlantGeneration = statistics.HighestPlantGeneration,
                Founder = founderSum.Select(sum => founderCount == 0 ? 0d : sum / founderCount).ToArray(),
                Final = finalSum.Select(sum => live == 0 ? double.NaN : sum / live).ToArray(),
            };
        }

        // ---- output ------------------------------------------------------------------------

        private static void WriteCsv(RunResult[] results)
        {
            var builder = new StringBuilder();
            builder.Append("terrain,arm,seed,hash,occupancy,population,extinct,frozen,plant_births,highest_plant_generation");
            for (int trait = 0; trait < PlantGenome.TraitCount; trait++)
            {
                string name = PlantGenome.TraitName(trait).ToLowerInvariant().Replace(' ', '_');
                builder.Append(",").Append(name).Append("_founder,")
                    .Append(name).Append("_final,")
                    .Append(name).Append("_delta");
            }

            builder.AppendLine();

            foreach (RunResult result in results)
            {
                builder.Append(result.Terrain ? "terrain" : "flat").Append(",")
                    .Append(result.Contest ? "contest-on" : "contest-off").Append(",")
                    .Append(result.Seed).Append(",")
                    .Append(result.Hash).Append(",")
                    .Append(Format(result.Occupancy)).Append(",")
                    .Append(result.Population).Append(",")
                    .Append(result.Extinct ? 1 : 0).Append(",")
                    .Append(result.Frozen ? 1 : 0).Append(",")
                    .Append(result.PlantBirths).Append(",")
                    .Append(result.HighestPlantGeneration);
                for (int trait = 0; trait < PlantGenome.TraitCount; trait++)
                {
                    builder.Append(",").Append(Format(result.Founder[trait]))
                        .Append(",").Append(Format(result.Final[trait]))
                        .Append(",").Append(Format(result.Final[trait] - result.Founder[trait]));
                }

                builder.AppendLine();
            }

            // The filename encodes the configuration, and deliberately never reproduces the name of a
            // committed corpus.
            //
            // This used to be the hardcoded string "p4-terrain-local-band-2026-08-23.csv", so EVERY
            // run of this tool overwrote a recorded 480-row experimental result with whatever had
            // just been run - and on 2026-08-24 one did, replacing it with 160 rows from a 40-seed
            // tuning sweep. It was caught by `git status` and restored. A corpus is the evidence for
            // a written conclusion; a tool that silently rewrites one is a tool that can invalidate
            // the record without anybody noticing.
            string configuration = "cap" + _maximumPopulation
                + (_gradedFertility ? "-brake" + _brakeStrength.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "")
                + "-" + _seedCount + "seeds";
            string path = Path.Combine("docs", "experiments", "p6-plant-" + configuration + "-2026-08-24.csv");
            File.WriteAllText(path, builder.ToString());
            Console.Error.WriteLine("wrote " + path);
        }

        private static string Format(double value)
        {
            return double.IsNaN(value) ? "" : value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static void Report(RunResult[] results)
        {
            foreach (bool contest in new[] { false, true })
            {
                foreach (bool terrain in new[] { false, true })
                {
                    RunResult[] set = results.Where(result => result.Contest == contest && result.Terrain == terrain).ToArray();
                    Console.WriteLine();
                    Console.WriteLine("== " + (contest ? "contest-on" : "contest-off") + " / "
                        + (terrain ? "terrain-driven" : "flat") + " (" + set.Length + " seeds)");
                    Console.WriteLine("   extinct " + set.Count(result => result.Extinct) + "/" + set.Length
                        + "   frozen " + set.Count(result => result.Frozen) + "/" + set.Length
                        + "   occupancy " + set.Average(result => result.Occupancy).ToString("0.000", CultureInfo.InvariantCulture)
                        + "   population " + set.Average(result => (double)result.Population).ToString("0.0", CultureInfo.InvariantCulture));
                    for (int trait = 0; trait < PlantGenome.TraitCount; trait++)
                    {
                        double[] deltas = set.Where(result => !double.IsNaN(result.Final[trait]))
                            .Select(result => result.Final[trait] - result.Founder[trait]).ToArray();
                        Console.WriteLine("   " + PlantGenome.TraitName(trait).PadRight(22) + Summarise(deltas));
                    }
                }
            }

            // Paired on-off: the comparison the establishment claim is actually about.
            foreach (bool terrain in new[] { false, true })
            {
                Console.WriteLine();
                Console.WriteLine("== establishment contest, paired on-off, " + (terrain ? "terrain-driven" : "flat"));
                for (int trait = 0; trait < PlantGenome.TraitCount; trait++)
                {
                    Console.WriteLine("   " + PlantGenome.TraitName(trait).PadRight(22)
                        + Summarise(Paired(results, on => on.Terrain == terrain && on.Contest,
                            (candidate, on) => candidate.Terrain == terrain && !candidate.Contest && candidate.Seed == on.Seed, trait)));
                }
            }

            // The join itself: terrain-on minus terrain-off, paired by seed.
            foreach (bool contest in new[] { false, true })
            {
                Console.WriteLine();
                Console.WriteLine("== the join, paired terrain minus flat, " + (contest ? "contest-on" : "contest-off"));
                for (int trait = 0; trait < PlantGenome.TraitCount; trait++)
                {
                    Console.WriteLine("   " + PlantGenome.TraitName(trait).PadRight(22)
                        + Summarise(Paired(results, on => on.Terrain && on.Contest == contest,
                            (candidate, on) => !candidate.Terrain && candidate.Contest == contest && candidate.Seed == on.Seed, trait)));
                }
            }
        }

        private static double[] Paired(
            RunResult[] results, Func<RunResult, bool> selectLeft,
            Func<RunResult, RunResult, bool> matchRight, int trait)
        {
            var differences = new List<double>();
            foreach (RunResult left in results.Where(selectLeft))
            {
                RunResult right = results.FirstOrDefault(candidate => matchRight(candidate, left));
                if (right == null) continue;
                if (double.IsNaN(left.Final[trait]) || double.IsNaN(right.Final[trait])) continue;

                differences.Add(left.Final[trait] - right.Final[trait]);
            }

            return differences.ToArray();
        }

        /// <summary>
        /// What the two fields actually look like over the arena the creatures live in.
        ///
        /// <para>Without this, "the join moves no plant conclusion" is unreadable: it could mean the
        /// terrain field is a genuinely different landscape that selection is indifferent to, or it
        /// could mean the field barely varies and the flag is nearly a no-op. Those are opposite
        /// findings and the trait table cannot tell them apart.</para>
        /// </summary>
        private static void ReportFields()
        {
            const int Steps = 41;
            const float Half = 25f;
            foreach (int seed in new[] { 42, 71, 161 })
            {
                EnvironmentField flat = EnvironmentField.CreateProcedural(seed, elevationEnabled: true);
                EnvironmentField terrain = EnvironmentField.CreateTerrainDriven(seed);
                var flatSamples = new List<EnvironmentSample>();
                var terrainSamples = new List<EnvironmentSample>();
                for (int row = 0; row < Steps; row++)
                {
                    for (int column = 0; column < Steps; column++)
                    {
                        float x = -Half + (2f * Half * column / (Steps - 1));
                        float y = -Half + (2f * Half * row / (Steps - 1));
                        var position = new SimVector2(x, y);
                        flatSamples.Add(flat.Sample(position));
                        terrainSamples.Add(terrain.Sample(position));
                    }
                }

                Console.WriteLine();
                Console.WriteLine("== seed " + seed + ", " + (Steps * Steps) + " positions across the arena");
                Describe("moisture    flat", flatSamples.Select(sample => (double)sample.Moisture).ToArray());
                Describe("moisture terrain", terrainSamples.Select(sample => (double)sample.Moisture).ToArray());
                Describe("fertility   flat", flatSamples.Select(sample => (double)sample.Fertility).ToArray());
                Describe("fertility terrain", terrainSamples.Select(sample => (double)sample.Fertility).ToArray());
                Describe("temperature flat", flatSamples.Select(sample => (double)sample.Temperature).ToArray());
                Describe("temperature terrain", terrainSamples.Select(sample => (double)sample.Temperature).ToArray());
            }
        }

        private static void Describe(string label, double[] values)
        {
            double mean = values.Average();
            double deviation = Math.Sqrt(values.Sum(value => (value - mean) * (value - mean)) / values.Length);
            Console.WriteLine("   " + label.PadRight(20)
                + " mean " + mean.ToString("0.000", CultureInfo.InvariantCulture)
                + "  sd " + deviation.ToString("0.000", CultureInfo.InvariantCulture)
                + "  range " + values.Min().ToString("0.000", CultureInfo.InvariantCulture)
                + " to " + values.Max().ToString("0.000", CultureInfo.InvariantCulture));
        }

        /// <summary>Mean, one-sample t against zero, and the sign count.</summary>
        private static string Summarise(double[] values)
        {
            if (values.Length < 2) return "insufficient";

            double mean = values.Average();
            double variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Length - 1);
            double error = Math.Sqrt(variance / values.Length);
            double t = error <= 0d ? 0d : mean / error;
            int up = values.Count(value => value > 0d);
            return mean.ToString("+0.0000;-0.0000", CultureInfo.InvariantCulture).PadLeft(9)
                + "  t " + t.ToString("+0.00;-0.00", CultureInfo.InvariantCulture).PadLeft(7)
                + "  " + up.ToString().PadLeft(3) + "/" + values.Length + " up";
        }
    }
}

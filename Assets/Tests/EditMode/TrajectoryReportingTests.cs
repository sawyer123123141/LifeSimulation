using System;
using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Tools;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The reporting layer, which decides what every experiment record in this project says.
    ///
    /// <para><b>Why these exist.</b> Two reporting defects were found by reading console output
    /// rather than by any test. The five-number summary of final population was computed over
    /// surviving worlds only, so a cell that lost four of twenty-four worlds reported a minimum of 32
    /// and no zeros - the more of it died, the healthier its distribution read. And there were no
    /// per-seed rows at all, so which seeds survived, how their plant communities differed, and how
    /// many worlds touched the population cap were unanswerable from a committed artefact.</para>
    ///
    /// <para>Nothing here touches simulation behaviour, and two tests prove that.</para>
    /// </summary>
    public sealed class TrajectoryReportingTests
    {
        private const int Seed = 42;
        private const int RecordedTicks = 24000;
        private const int ExtensionTicks = 72000;

        /// <summary>Long enough for the trajectory to fill and short enough to run in a test.</summary>
        private const int ShortTicks = 900;

        // ---- sample spacing ------------------------------------------------------------------

        [Test]
        public void TheDefaultIsNineSamplesSoEveryRecordedArtefactReproduces()
        {
            Assert.That(Trajectory.DefaultSampleCount, Is.EqualTo(9));
            Assert.That(new Trajectory(RecordedTicks).SampleCount, Is.EqualTo(9),
                "an omitted sample count must still be nine, or every recorded artefact stops reproducing");
        }

        [Test]
        public void HoldingRecordedSpacingGivesTwentySevenSamplesAtTheExtensionLength()
        {
            // 24,000 / 9 = 2,666.67 ticks per sample. The graded-seeding arm's entire crash occupies
            // one sample at that spacing, so a longer run holding sample COUNT averages over the very
            // thing being measured.
            Assert.That(Trajectory.SampleCountForRecordedSpacing(RecordedTicks), Is.EqualTo(9));
            Assert.That(Trajectory.SampleCountForRecordedSpacing(ExtensionTicks), Is.EqualTo(27));

            double recorded = RecordedTicks / (double)Trajectory.SampleCountForRecordedSpacing(RecordedTicks);
            double extension = ExtensionTicks / (double)Trajectory.SampleCountForRecordedSpacing(ExtensionTicks);
            Assert.That(extension, Is.EqualTo(recorded).Within(1e-9),
                "the extension must sample at the spacing the recorded corpus was written against");
        }

        [Test]
        public void TheFinalBoundaryLandsExactlyOnTheFinalTickAtEverySampleCount()
        {
            // A last boundary that misses the final tick makes the end-of-run row disagree with every
            // other number the sweep prints.
            foreach (int sampleCount in new[] { 1, 7, 9, 27, 100 })
            {
                var trajectory = new Trajectory(ExtensionTicks, sampleCount);
                Assert.That(trajectory.BoundaryAt(sampleCount - 1), Is.EqualTo(ExtensionTicks),
                    "sample count " + sampleCount + " does not close on the final tick");
                Assert.That(trajectory.BoundaryAt(0), Is.GreaterThan(0));
            }
        }

        [Test]
        public void BoundariesAreStrictlyIncreasingAndEvenlySpaced()
        {
            var trajectory = new Trajectory(ExtensionTicks, 27);
            int previous = 0;
            for (int index = 0; index < trajectory.SampleCount; index++)
            {
                int boundary = trajectory.BoundaryAt(index);
                Assert.That(boundary, Is.GreaterThan(previous));
                Assert.That(boundary - previous, Is.EqualTo(ExtensionTicks / 27).Within(1),
                    "spacing drifted at sample " + index);
                previous = boundary;
            }
        }

        [Test]
        public void EverySampleIsTakenAndTheLastRowIsTheFinalTick()
        {
            var trajectory = new Trajectory(ShortTicks, 9, "unit", Seed);
            RunWorld(trajectory, ShortTicks);

            string[] rows = DataRows(Trajectory.ToCsv(new[] { trajectory }));
            Assert.That(rows.Length, Is.EqualTo(9), "one row per sample");
            Assert.That(Field(rows[rows.Length - 1], "tick"), Is.EqualTo(ShortTicks.ToString()));
        }

        [Test]
        public void ReportRefusesToMixSampleCounts()
        {
            // Two runs sampled differently do not share columns and their thirds do not line up.
            var mixed = new List<Trajectory> { new Trajectory(ShortTicks, 9), new Trajectory(ShortTicks, 3) };
            Assert.That(() => Trajectory.Report(mixed, ShortTicks), Throws.ArgumentException);
        }

        // ---- per-seed identity ---------------------------------------------------------------

        [Test]
        public void EveryRowCarriesItsOwnArmAndSeed()
        {
            var control = new Trajectory(ShortTicks, 9, "control", 42);
            var arm = new Trajectory(ShortTicks, 9, "graded-seeding", 43);
            RunWorld(control, ShortTicks, 42);
            RunWorld(arm, ShortTicks, 43);

            string[] rows = DataRows(Trajectory.ToCsv(new[] { control, arm }));
            Assert.That(rows.Count(row => Field(row, "arm") == "control"), Is.EqualTo(9));
            Assert.That(rows.Count(row => Field(row, "arm") == "graded-seeding"), Is.EqualTo(9));
            Assert.That(rows.Where(row => Field(row, "seed") == "42").All(row => Field(row, "arm") == "control"));
            Assert.That(rows.Where(row => Field(row, "seed") == "43").All(row => Field(row, "arm") == "graded-seeding"));
        }

        [Test]
        public void TwoSeedsInOneFileAreJoinableOnSeedAndTick()
        {
            var control = new Trajectory(ShortTicks, 9, "control", 42);
            var arm = new Trajectory(ShortTicks, 9, "graded-seeding", 42);
            RunWorld(control, ShortTicks, 42);
            RunWorld(arm, ShortTicks, 42);

            string[] rows = DataRows(Trajectory.ToCsv(new[] { control, arm }));
            var ticksPerArm = rows.GroupBy(row => Field(row, "arm"))
                .ToDictionary(group => group.Key, group => group.Select(row => Field(row, "tick")).ToArray());

            Assert.That(ticksPerArm["control"], Is.EqualTo(ticksPerArm["graded-seeding"]),
                "a paired analysis joins on seed and tick, so both arms must land on the same grid");
        }

        // ---- extinction inclusion ------------------------------------------------------------

        [Test]
        public void AllWorldDistributionsIncludeExtinctWorldsAsZero()
        {
            List<Trajectory> runs = OneSurvivorAndOneCasualty();

            double[] allWorlds = Trajectory.FinalPopulationsAllWorlds(runs);
            Assert.That(allWorlds.Length, Is.EqualTo(runs.Count), "an all-world distribution drops nobody");
            Assert.That(allWorlds.Count(value => value == 0d), Is.EqualTo(1), "the dead are zeros, not absences");

            double[] survivors = Trajectory.FinalPopulationsSurvivorsOnly(runs);
            Assert.That(survivors.Length, Is.EqualTo(runs.Count - 1));
            Assert.That(survivors.Any(value => value == 0d), Is.False);
        }

        [Test]
        public void TheTwoDistributionsDisagreeAndThatIsThePoint()
        {
            List<Trajectory> runs = OneSurvivorAndOneCasualty();

            string allWorlds = Trajectory.FiveNumberSummary(Trajectory.FinalPopulationsAllWorlds(runs));
            string survivors = Trajectory.FiveNumberSummary(Trajectory.FinalPopulationsSurvivorsOnly(runs));

            Assert.That(allWorlds, Does.Contain("n 2").And.Contain("min 0.0"));
            Assert.That(survivors, Does.Contain("n 1"));
            Assert.That(allWorlds, Is.Not.EqualTo(survivors),
                "if these ever agree the labelling has stopped meaning anything");
        }

        [Test]
        public void ExtinctAtEndMarksTheRunNotTheRow()
        {
            // A run alive at its first sample and dead at its last is flagged on every row, so a
            // distribution can include the dead without re-deriving which they were.
            Trajectory casualty = OneSurvivorAndOneCasualty()[1];
            string[] rows = DataRows(Trajectory.ToCsv(new[] { casualty }));

            Assert.That(rows.All(row => Field(row, "extinct_at_end") == "1"));
            Assert.That(int.Parse(Field(rows[0], "population")), Is.GreaterThan(0),
                "the first sample was alive; the flag describes the run's ending, not this row");
            Assert.That(int.Parse(Field(rows[rows.Length - 1], "population")), Is.EqualTo(0));
        }

        [Test]
        public void TheSummaryReportsQuartilesAndCountsEveryValue()
        {
            string summary = Trajectory.FiveNumberSummary(new[] { 0d, 0d, 10d, 20d, 30d, 40d, 50d, 500d });
            Assert.That(summary, Does.Contain("n 8"));
            Assert.That(summary, Does.Contain("min 0.0").And.Contain("max 500.0"));
            Assert.That(summary, Does.Contain("q1 ").And.Contain("median ").And.Contain("q3 "));
        }

        // ---- plant columns -------------------------------------------------------------------

        [Test]
        public void PlantColumnsArePresentAndPopulatedFromTheWorld()
        {
            var trajectory = new Trajectory(ShortTicks, 9, "unit", Seed);
            RunWorld(trajectory, ShortTicks);

            string csv = Trajectory.ToCsv(new[] { trajectory });
            foreach (string column in new[]
            {
                "plant_patches", "seed_eligible_patches", "plant_biomass",
                "plant_growth_cumulative", "plant_offtake_cumulative", "plant_mortality_cumulative",
            })
            {
                Assert.That(Header(csv), Does.Contain(column), "missing column " + column);
            }

            string[] rows = DataRows(csv);
            Assert.That(Value(rows[0], "plant_patches"), Is.GreaterThan(0d),
                "the calibration scenario seeds patches, so this cannot be zero");
            Assert.That(Value(rows[0], "plant_biomass"), Is.GreaterThan(0d));
            Assert.That(Value(rows[0], "seed_eligible_patches"),
                Is.LessThanOrEqualTo(Value(rows[0], "plant_patches")),
                "seed-eligible patches are a subset of live patches");
        }

        [Test]
        public void CumulativePlantColumnsNeverGoBackwards()
        {
            var trajectory = new Trajectory(ShortTicks, 9, "unit", Seed);
            RunWorld(trajectory, ShortTicks);
            string[] rows = DataRows(Trajectory.ToCsv(new[] { trajectory }));

            foreach (string column in new[]
            {
                "plant_growth_cumulative", "plant_offtake_cumulative", "plant_mortality_cumulative",
                "starvation_deaths", "age_deaths",
            })
            {
                for (int index = 1; index < rows.Length; index++)
                {
                    Assert.That(Value(rows[index], column), Is.GreaterThanOrEqualTo(Value(rows[index - 1], column)),
                        column + " fell between samples, so it is not cumulative");
                }
            }
        }

        [Test]
        public void CapContactAndPeakAreRecorded()
        {
            var trajectory = new Trajectory(ShortTicks, 9, "unit", Seed);
            RunWorld(trajectory, ShortTicks);

            string csv = Trajectory.ToCsv(new[] { trajectory });
            Assert.That(Header(csv), Does.Contain("at_cap").And.Contain("peak_population"));

            string[] rows = DataRows(csv);
            double peak = rows.Max(row => Value(row, "population"));
            Assert.That(Value(rows[0], "peak_population"), Is.EqualTo(peak),
                "peak_population is the run's peak across samples, not the row's own population");
            Assert.That(trajectory.PeakPopulation, Is.EqualTo(peak));
            Assert.That(trajectory.CapContactSamples, Is.EqualTo(rows.Count(row => Field(row, "at_cap") == "1")));
        }

        // ---- the simulation is untouched -------------------------------------------------------

        [Test]
        public void ObservingAWorldDoesNotChangeIt()
        {
            // The whole reporting layer is worthless if it perturbs what it measures. Same seed, same
            // ticks, one run sampled at every boundary and one not sampled at all.
            SimulationConfig config = Config(Seed);
            var observed = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(observed);
            var trajectory = new Trajectory(ShortTicks, 27, "unit", Seed);
            for (int tick = 0; tick < ShortTicks; tick++)
            {
                observed.Step(config.FixedDeltaTime);
                trajectory.Observe(tick, observed);
            }

            var unobserved = new SimulationWorld(Config(Seed));
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(unobserved);
            for (int tick = 0; tick < ShortTicks; tick++) unobserved.Step(config.FixedDeltaTime);

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(unobserved.ComputeStateHash()),
                "sampling the world changed the world");
            Assert.That(observed.ComputeBehaviorHash(), Is.EqualTo(unobserved.ComputeBehaviorHash()));
        }

        [Test]
        public void TheRecordedBehaviorHashIsTheWorldsOwnHashAtThatSample()
        {
            SimulationConfig config = Config(Seed);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            var trajectory = new Trajectory(ShortTicks, 9, "unit", Seed);
            for (int tick = 0; tick < ShortTicks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                trajectory.Observe(tick, world);
            }

            string[] rows = DataRows(Trajectory.ToCsv(new[] { trajectory }));
            Assert.That(Field(rows[rows.Length - 1], "behavior_hash"),
                Is.EqualTo(world.ComputeBehaviorHash().ToString()),
                "the last row's hash must be the world's own hash at the final tick, or determinism is unprovable from the file");
        }

        // ---- fixtures --------------------------------------------------------------------------

        private static SimulationConfig Config(int worldSeed)
        {
            return new SimulationConfig(
                worldSeed: worldSeed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                plantMortalityEnabled: true,
                plantSiteCompetitionEnabled: true);
        }

        private static void RunWorld(Trajectory trajectory, int ticks, int worldSeed = Seed, bool withResources = true)
        {
            SimulationConfig config = Config(worldSeed);
            var world = new SimulationWorld(config);
            if (withResources) Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                trajectory.Observe(tick, world);
            }
        }

        /// <summary>
        /// One world with food and water and one with neither. The second dies of thirst, which is
        /// how this fixture produces a genuine extinct run rather than a hand-written zero.
        /// </summary>
        private static List<Trajectory> OneSurvivorAndOneCasualty()
        {
            const int ticks = 12000;
            var survivor = new Trajectory(ticks, 9, "with-resources", Seed);
            RunWorld(survivor, ticks);

            var casualty = new Trajectory(ticks, 9, "no-resources", Seed);
            RunWorld(casualty, ticks, Seed, withResources: false);

            Assert.That(survivor.FinalPopulation, Is.GreaterThan(0d), "fixture: the fed world should live");
            Assert.That(casualty.FinalPopulation, Is.EqualTo(0d), "fixture: the unfed world should die");
            return new List<Trajectory> { survivor, casualty };
        }

        // ---- csv helpers -------------------------------------------------------------------------

        private static string Header(string csv)
        {
            return csv.Split('\n')[0].Trim();
        }

        private static string[] DataRows(string csv)
        {
            return csv.Split('\n')
                .Skip(1)
                .Select(row => row.Trim())
                .Where(row => row.Length > 0)
                .ToArray();
        }

        private static string Field(string row, string column)
        {
            return row.Split(',')[ColumnIndex(column)];
        }

        private static double Value(string row, string column)
        {
            string text = Field(row, column);
            return text.Length == 0 ? double.NaN : double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int ColumnIndex(string column)
        {
            string[] columns = Header(Trajectory.ToCsv(Array.Empty<Trajectory>())).Split(',');
            int index = Array.IndexOf(columns, column);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "no such column: " + column);
            return index;
        }
    }
}

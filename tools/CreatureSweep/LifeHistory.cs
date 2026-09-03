using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// The life-history sweep: lifetime ingestion measured at the allocation site, joined to the
    /// recorded pedigree, restricted to a genotype-independent cohort, and reported per world before
    /// it is reported across worlds.
    ///
    /// <para>It supersedes <c>--intake</c>, which infers a flow from a state variable and therefore
    /// measures that flow minus everything else that touched the variable on the same tick.
    /// <c>Intake.cs</c> is deliberately kept so the number being retracted stays reproducible.</para>
    ///
    /// <para>A thin driver only: every rule, statistic and diagnostic it prints lives in
    /// <c>Assets/Scripts/Simulation/Analysis</c>, which the headless test project compiles.</para>
    /// </summary>
    internal static class LifeHistory
    {
        private const int BinCount = 5;
        private const int MinimumCohortSizePerWorld = 20;
        private const int BootstrapResampleCount = 2_000;

        /// <summary>
        /// The share of reproduction ticks that must be cap-blocked before a reproductive-skew reading
        /// is called unsafe. Explicit, because a default would become a fact nobody chose.
        /// </summary>
        private const float UnsafeCapBlockedFraction = 0.25f;

        /// <summary>
        /// Last birth tick of the "expansion" window. It is the complete-life cohort of a
        /// 12,000-tick run (12,000 - 5,400), chosen so the early window here is exactly the cohort
        /// the 12,000-tick measurement reported on and the two are directly comparable.
        /// </summary>
        private const long ExpansionWindowEndTick = 6_600;

        private sealed class CohortMeasurements
        {
            public int Count;
            public double[] Diet = Array.Empty<double>();

            /// <summary>
            /// The inert control channel. <c>NeutralMarker</c> is read by zero behaviour code and is
            /// pinned dead by <c>LivenessTests</c> under the widest available configuration, so any
            /// relationship it shows against a fitness quantity is structure rather than biology -
            /// most plausibly family-level, since relatives share both a drifted marker value and a
            /// foraging neighbourhood.
            /// </summary>
            public double[] NeutralMarker = Array.Empty<double>();
            public double[] GrossPerThousandTicks = Array.Empty<double>();
            public double[] LifetimeGross = Array.Empty<double>();
            public double[] Offspring = Array.Empty<double>();
            public double[] OffspringToAdulthood = Array.Empty<double>();
            public double[] LifetimeProxyGross = Array.Empty<double>();
            public double[] LifetimeStaleGross = Array.Empty<double>();
        }

        private sealed class WorldOutcome
        {
            public int Seed;
            public int Population;
            public bool LedgerComplete;
            public int CohortSize;
            public int ExcludedByHorizon;
            public int ExcludedWithoutGenome;
            public int AdulthoodCohortSize;
            public double[] Diet = Array.Empty<double>();

            /// <summary>
            /// The inert control channel. <c>NeutralMarker</c> is read by zero behaviour code and is
            /// pinned dead by <c>LivenessTests</c> under the widest available configuration, so any
            /// relationship it shows against a fitness quantity is structure rather than biology -
            /// most plausibly family-level, since relatives share both a drifted marker value and a
            /// foraging neighbourhood.
            /// </summary>
            public double[] NeutralMarker = Array.Empty<double>();
            public double[] GrossPerThousandTicks = Array.Empty<double>();
            public double[] Offspring = Array.Empty<double>();
            public double[] OffspringToAdulthood = Array.Empty<double>();
            public double[] LifetimeGross = Array.Empty<double>();
            public double[] LifetimeProxyGross = Array.Empty<double>();
            public double[] LifetimeStaleGross = Array.Empty<double>();
            public CohortMeasurements Early = new CohortMeasurements();
            public CohortMeasurements Late = new CohortMeasurements();
            public double PlantGross;
            public double PlantStored;
            public double PlantSurplus;
            public double CarcassGross;
            public double CarcassSurplus;
            public double StaleShare;
            public double ProxyGross;
            public ReproductionBottleneck Bottleneck;
            public PopulationCapDiagnostic Cap;
        }

        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            var outcomes = new List<WorldOutcome>();
            for (int index = 0; index < seedCount; index++)
            {
                outcomes.Add(RunOne(Program.FirstSeed + index, ticks, configure, scenario));
            }

            Console.WriteLine();
            Console.WriteLine($"life history, {outcomes.Count} worlds of {ticks} ticks");
            PrintHorizons(outcomes[0], ticks);
            PrintCohorts(outcomes);
            PrintIngestionByDietBin(outcomes);
            PrintFitnessByDietBin(outcomes);
            PrintByBirthWindow(outcomes);
            PrintBottleneck(outcomes);
            PrintCap(outcomes);
            PrintRelationships(outcomes);
            PrintProxyComparison(outcomes);
            VerifyTheInstrumentDidNotPerturbTheSubject(configure, scenario);
        }

        private static WorldOutcome RunOne(int seed, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            SimulationConfig config = configure(seed);
            var world = new SimulationWorld(config) { Recorder = new IngestionRecorder() };
            scenario.ApplyTo(world);

            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, world.Creatures);
            ledger.Observe(world);

            var bottleneck = new ReproductionBottleneck(config.ReproductionNeedFraction);
            var cap = new PopulationCapDiagnostic(config.ReproductionNeedFraction, UnsafeCapBlockedFraction);
            var proxy = new EnergyDeltaProxy();

            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);

                // The world never clears its own 1,024-entry buffer, so the host must drain it every
                // tick or the ledger goes permanently incomplete.
                ledger.RecordCompleteBatch(world.Events, world.CurrentTick);
                world.Events.Clear();

                ledger.Observe(world);
                bottleneck.SampleWorld(world);
                cap.SampleWorld(world);
                proxy.Observe(world);
            }

            var outcome = new WorldOutcome
            {
                Seed = seed,
                Population = world.CreatureCount,
                LedgerComplete = ledger.IsComplete,
                Bottleneck = bottleneck,
                Cap = cap,
                PlantGross = world.Recorder.TotalGrossEnergy(ResourceKind.Food),
                PlantStored = world.Recorder.TotalStoredEnergy(ResourceKind.Food),
                PlantSurplus = world.Recorder.TotalSurplusEnergy(ResourceKind.Food),
                CarcassGross = world.Recorder.TotalGrossEnergy(ResourceKind.Carcass),
                CarcassSurplus = world.Recorder.TotalSurplusEnergy(ResourceKind.Carcass),
                ProxyGross = proxy.TotalPositiveDeltaEnergy(ResourceKind.Food) + proxy.TotalPositiveDeltaEnergy(ResourceKind.Carcass),
            };

            double totalGross = outcome.PlantGross + outcome.CarcassGross;
            outcome.StaleShare = totalGross <= 0d
                ? 0d
                : (world.Recorder.TotalStaleActionGrossEnergy(ResourceKind.Food) + world.Recorder.TotalStaleActionGrossEnergy(ResourceKind.Carcass)) / totalGross;

            if (!ledger.IsComplete) return outcome;

            FitnessCohort completeLife = FitnessCohort.Select(ledger, config.Schedule, ticks, FitnessCohortKind.CompleteLife);
            FitnessCohort toAdulthood = FitnessCohort.Select(ledger, config.Schedule, ticks, FitnessCohortKind.OffspringToAdulthood);
            IngestionLedger joined = IngestionLedger.Join(ledger, world.Recorder, completeLife);

            outcome.CohortSize = completeLife.Count;
            outcome.ExcludedByHorizon = completeLife.ExcludedByHorizon;
            outcome.ExcludedWithoutGenome = completeLife.ExcludedWithoutGenome;
            outcome.AdulthoodCohortSize = toAdulthood.Count;

            long adultAgeTicks = FitnessCohort.AdultAgeTicks(config.Schedule);
            CohortMeasurements pooled = Measure(joined, ledger, world, proxy, ticks, adultAgeTicks);

            outcome.Diet = pooled.Diet;
            outcome.GrossPerThousandTicks = pooled.GrossPerThousandTicks;
            outcome.LifetimeGross = pooled.LifetimeGross;
            outcome.Offspring = pooled.Offspring;
            outcome.OffspringToAdulthood = pooled.OffspringToAdulthood;
            outcome.LifetimeProxyGross = pooled.LifetimeProxyGross;
            outcome.LifetimeStaleGross = pooled.LifetimeStaleGross;

            // The same genotype-independent horizon, split on birth tick alone, so the expansion
            // phase and everything after it can be read separately instead of averaged together.
            FitnessCohort early = FitnessCohort.Select(
                ledger, config.Schedule, ticks, FitnessCohortKind.CompleteLife,
                excludeFounders: false, birthWindowStartTick: 0, birthWindowEndTick: ExpansionWindowEndTick);
            FitnessCohort late = FitnessCohort.Select(
                ledger, config.Schedule, ticks, FitnessCohortKind.CompleteLife,
                excludeFounders: false, birthWindowStartTick: ExpansionWindowEndTick + 1, birthWindowEndTick: long.MaxValue);

            outcome.Early = Measure(IngestionLedger.Join(ledger, world.Recorder, early), ledger, world, proxy, ticks, adultAgeTicks);
            outcome.Late = Measure(IngestionLedger.Join(ledger, world.Recorder, late), ledger, world, proxy, ticks, adultAgeTicks);
            return outcome;
        }

        private static CohortMeasurements Measure(
            IngestionLedger joined,
            LifeHistoryLedger ledger,
            SimulationWorld world,
            EnergyDeltaProxy proxy,
            int ticks,
            long adultAgeTicks)
        {
            var measurements = new CohortMeasurements
            {
                Count = joined.Count,
                Diet = new double[joined.Count],
                NeutralMarker = new double[joined.Count],
                GrossPerThousandTicks = new double[joined.Count],
                LifetimeGross = new double[joined.Count],
                Offspring = new double[joined.Count],
                OffspringToAdulthood = new double[joined.Count],
                LifetimeProxyGross = new double[joined.Count],
                LifetimeStaleGross = new double[joined.Count],
            };

            for (int index = 0; index < joined.Count; index++)
            {
                LifeHistoryRecord life = joined.GetLifeAt(index);
                double lifespan = Math.Max(1d, LifespanTicksOf(life, ticks));
                double gross = joined.GrossEnergyAt(index, ResourceKind.Food) + joined.GrossEnergyAt(index, ResourceKind.Carcass);

                measurements.Diet[index] = life.Genome.DietSpecialization;
                measurements.NeutralMarker[index] = life.Genome.NeutralMarker;
                measurements.LifetimeGross[index] = gross;
                measurements.GrossPerThousandTicks[index] = gross / lifespan * 1_000d;
                measurements.Offspring[index] = life.OffspringCredited;
                measurements.OffspringToAdulthood[index] = CountOffspringReachingAdulthood(ledger, life.CreatureId, ticks, adultAgeTicks);
                measurements.LifetimeProxyGross[index] = proxy.PositiveDeltaEnergy(life.CreatureId, ResourceKind.Food)
                    + proxy.PositiveDeltaEnergy(life.CreatureId, ResourceKind.Carcass);
                measurements.LifetimeStaleGross[index] = world.Recorder.StaleActionGrossEnergy(life.CreatureId, ResourceKind.Food)
                    + world.Recorder.StaleActionGrossEnergy(life.CreatureId, ResourceKind.Carcass);
            }

            return measurements;
        }

        private static long LifespanTicksOf(LifeHistoryRecord life, int ticks)
        {
            return life.IsAlive ? ticks - life.BirthTick : life.DeathTick - life.BirthTick;
        }

        private static int CountOffspringReachingAdulthood(LifeHistoryLedger ledger, CreatureId parent, int ticks, long adultAgeTicks)
        {
            int reached = 0;
            int children = ledger.OffspringCredited(parent);
            for (int index = 0; index < children; index++)
            {
                if (!ledger.TryGet(ledger.OffspringAt(parent, index), out LifeHistoryRecord child)) continue;
                if (LifespanTicksOf(child, ticks) >= adultAgeTicks) reached++;
            }

            return reached;
        }

        private static void PrintHorizons(WorldOutcome first, int ticks)
        {
            SimulationSchedule schedule = SimulationConfig.CreatePrototype4Defaults(first.Seed, 1).Schedule;
            long lifespan = FitnessCohort.MaximumLifespanTicks(schedule);
            long adult = FitnessCohort.AdultAgeTicks(schedule);
            Console.WriteLine();
            Console.WriteLine($"censoring horizons, derived from the phenotype map: adult age {adult} ticks, longest reachable life {lifespan} ticks");
            Console.WriteLine($"  complete-life cohort admits births in [0, {ticks - lifespan}] - {(ticks - lifespan) / (double)ticks:0.0%} of the run");
            Console.WriteLine($"  offspring-to-adulthood cohort admits births in [0, {ticks - lifespan - adult}]");
        }

        private static void PrintCohorts(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine("| seed | population | ledger complete | complete-life cohort | excluded by horizon | excluded: no genome | to-adulthood cohort |");
            Console.WriteLine("|---|---|---|---|---|---|---|");
            foreach (WorldOutcome outcome in outcomes)
            {
                Console.WriteLine($"| {outcome.Seed} | {outcome.Population} | {(outcome.LedgerComplete ? "yes" : "NO")} | {outcome.CohortSize} | {outcome.ExcludedByHorizon} | {outcome.ExcludedWithoutGenome} | {outcome.AdulthoodCohortSize} |");
            }

            int incomplete = outcomes.Count(outcome => !outcome.LedgerComplete);
            if (incomplete > 0)
            {
                Console.WriteLine($"**{incomplete} worlds have an incomplete pedigree and are excluded from every statistic below.**");
            }
        }

        private static void PrintIngestionByDietBin(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine("gross ingestion by diet bin, pooled over cohort members (per-world figures are below)");
            Console.WriteLine("| diet | creatures | gross/1k ticks | lifetime gross |");
            Console.WriteLine("|---|---|---|---|");
            for (int bin = 0; bin < BinCount; bin++)
            {
                var rates = new List<double>();
                var lifetimes = new List<double>();
                foreach (WorldOutcome outcome in outcomes)
                {
                    for (int index = 0; index < outcome.Diet.Length; index++)
                    {
                        if (BinOf(outcome.Diet[index]) != bin) continue;
                        rates.Add(outcome.GrossPerThousandTicks[index]);
                        lifetimes.Add(outcome.LifetimeGross[index]);
                    }
                }

                Console.WriteLine(rates.Count == 0
                    ? $"| {Low(bin)}-{High(bin)} | 0 | - | - |"
                    : $"| {Low(bin)}-{High(bin)} | {rates.Count} | {rates.Average():0.00} | {lifetimes.Average():0.0} |");
            }

            double gross = outcomes.Sum(outcome => outcome.PlantGross + outcome.CarcassGross);
            double stored = outcomes.Sum(outcome => outcome.PlantStored);
            double surplus = outcomes.Sum(outcome => outcome.PlantSurplus + outcome.CarcassSurplus);
            Console.WriteLine();
            Console.WriteLine($"whole-run totals: gross {gross:0.0}, stored (plant) {stored:0.0}, surplus lost to the capacity clamp {surplus:0.0} = {(gross <= 0d ? 0d : surplus / gross):0.00%} of gross");
        }

        private static void PrintFitnessByDietBin(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine("fitness by diet bin, complete-life cohort only");
            Console.WriteLine("| diet | creatures | offspring | offspring surviving to adulthood |");
            Console.WriteLine("|---|---|---|---|");
            for (int bin = 0; bin < BinCount; bin++)
            {
                var offspring = new List<double>();
                var survived = new List<double>();
                foreach (WorldOutcome outcome in outcomes)
                {
                    for (int index = 0; index < outcome.Diet.Length; index++)
                    {
                        if (BinOf(outcome.Diet[index]) != bin) continue;
                        offspring.Add(outcome.Offspring[index]);
                        survived.Add(outcome.OffspringToAdulthood[index]);
                    }
                }

                Console.WriteLine(offspring.Count == 0
                    ? $"| {Low(bin)}-{High(bin)} | 0 | - | - |"
                    : $"| {Low(bin)}-{High(bin)} | {offspring.Count} | {offspring.Average():0.000} | {survived.Average():0.000} |");
            }

            Console.WriteLine("both parents are credited for the same birth, so replacement is about two, not one");
        }

        /// <summary>
        /// The pooled tables above average the expansion phase together with everything after it.
        /// Run length and which phase the cohort samples are otherwise confounded by construction, so
        /// this splits the same cohort on birth tick alone - the genotype-independent rule from
        /// <see cref="FitnessCohort"/> - and reports the two windows side by side.
        /// </summary>
        private static void PrintByBirthWindow(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine($"SPLIT BY BIRTH WINDOW: births in [0, {ExpansionWindowEndTick}] against births after");
            Console.WriteLine("the same complete-life horizon applies to both; only the birth tick differs");

            int earlyTotal = outcomes.Sum(outcome => outcome.Early.Count);
            int lateTotal = outcomes.Sum(outcome => outcome.Late.Count);
            Console.WriteLine($"cohort sizes: early {earlyTotal}, late {lateTotal}");

            Console.WriteLine();
            Console.WriteLine("| diet | early: creatures | early: gross/1k | early: offspring | late: creatures | late: gross/1k | late: offspring |");
            Console.WriteLine("|---|---|---|---|---|---|---|");
            for (int bin = 0; bin < BinCount; bin++)
            {
                var earlyRates = new List<double>();
                var earlyOffspring = new List<double>();
                var lateRates = new List<double>();
                var lateOffspring = new List<double>();
                foreach (WorldOutcome outcome in outcomes)
                {
                    CollectBin(outcome.Early, bin, earlyRates, earlyOffspring);
                    CollectBin(outcome.Late, bin, lateRates, lateOffspring);
                }

                Console.WriteLine($"| {Low(bin)}-{High(bin)} "
                    + $"| {earlyRates.Count} | {Mean(earlyRates)} | {Mean(earlyOffspring)} "
                    + $"| {lateRates.Count} | {Mean(lateRates)} | {Mean(lateOffspring)} |");
            }

            PrintWindowRelationship(outcomes, "early window", outcome => outcome.Early);
            PrintWindowRelationship(outcomes, "late window", outcome => outcome.Late);
            PrintUShapeCounts(outcomes);
        }

        /// <summary>
        /// The statistic that matches the shape of the density-dependent claim. A correlation measures
        /// a monotone trend and a valley has none, so a coin-flip sign count discriminates a U-shape
        /// from noise not at all. This counts worlds directly: does the 0.6-0.8 bin mean sit below
        /// both end bins? Run for the inert marker as well, so the count has its own null.
        /// </summary>
        private static void PrintUShapeCounts(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine("U-SHAPE COUNT: worlds where the 0.6-0.8 bin mean falls below BOTH the 0.0-0.2 and 0.8-1.0 bin means");
            Console.WriteLine($"judged against the repository own committed threshold, PairedEvolutionCriterion.MinimumDirectionConsistency = {PairedEvolutionCriterion.MinimumDirectionConsistency:0.00}");
            Console.WriteLine();
            Console.WriteLine("| window | predictor | worlds with a valley | of | fraction | verdict |");
            Console.WriteLine("|---|---|---|---|---|---|");
            PrintOneUShapeCount(outcomes, "early", "diet", outcome => outcome.Early.Diet, outcome => outcome.Early.GrossPerThousandTicks);
            PrintOneUShapeCount(outcomes, "early", "neutral marker (control)", outcome => outcome.Early.NeutralMarker, outcome => outcome.Early.GrossPerThousandTicks);
            PrintOneUShapeCount(outcomes, "late", "diet", outcome => outcome.Late.Diet, outcome => outcome.Late.GrossPerThousandTicks);
            PrintOneUShapeCount(outcomes, "late", "neutral marker (control)", outcome => outcome.Late.NeutralMarker, outcome => outcome.Late.GrossPerThousandTicks);
        }

        private static void PrintOneUShapeCount(
            List<WorldOutcome> outcomes,
            string window,
            string predictorName,
            Func<WorldOutcome, double[]> predictor,
            Func<WorldOutcome, double[]> response)
        {
            int valleyWorlds = 0;
            int judged = 0;
            int skippedForEmptyBins = 0;

            foreach (WorldOutcome outcome in outcomes)
            {
                if (!outcome.LedgerComplete) continue;
                PerWorldRelationship world = PerWorldRelationship.ForWorld(outcome.Seed, predictor(outcome), response(outcome), BinCount);
                if (world.CohortSize < MinimumCohortSizePerWorld) continue;

                double low = world.BinMean(0);
                double middle = world.BinMean(3);
                double high = world.BinMean(BinCount - 1);
                if (double.IsNaN(low) || double.IsNaN(middle) || double.IsNaN(high))
                {
                    skippedForEmptyBins++;
                    continue;
                }

                judged++;
                if (middle < low && middle < high) valleyWorlds++;
            }

            if (judged == 0)
            {
                Console.WriteLine($"| {window} | {predictorName} | - | 0 | - | no world judgeable |");
                return;
            }

            double fraction = valleyWorlds / (double)judged;
            string verdict = fraction >= PairedEvolutionCriterion.MinimumDirectionConsistency ? "**PASSES**" : "fails";
            Console.WriteLine($"| {window} | {predictorName} | {valleyWorlds} | {judged} | {fraction:0.000} | {verdict}"
                + (skippedForEmptyBins > 0 ? $" ({skippedForEmptyBins} worlds unjudgeable, an end bin was empty)" : string.Empty)
                + " |");
        }

        private static void CollectBin(CohortMeasurements measurements, int bin, List<double> rates, List<double> offspring)
        {
            for (int index = 0; index < measurements.Diet.Length; index++)
            {
                if (BinOf(measurements.Diet[index]) != bin) continue;
                rates.Add(measurements.GrossPerThousandTicks[index]);
                offspring.Add(measurements.Offspring[index]);
            }
        }

        private static void PrintWindowRelationship(List<WorldOutcome> outcomes, string label, Func<WorldOutcome, CohortMeasurements> select)
        {
            var intakeWorlds = new List<PerWorldRelationship>();
            var offspringWorlds = new List<PerWorldRelationship>();
            foreach (WorldOutcome outcome in outcomes)
            {
                if (!outcome.LedgerComplete) continue;
                CohortMeasurements measurements = select(outcome);
                intakeWorlds.Add(PerWorldRelationship.ForWorld(outcome.Seed, measurements.Diet, measurements.GrossPerThousandTicks, BinCount));
                offspringWorlds.Add(PerWorldRelationship.ForWorld(outcome.Seed, measurements.Diet, measurements.Offspring, BinCount));
            }

            var neutralIntakeWorlds = new List<PerWorldRelationship>();
            var neutralOffspringWorlds = new List<PerWorldRelationship>();
            foreach (WorldOutcome outcome in outcomes)
            {
                if (!outcome.LedgerComplete) continue;
                CohortMeasurements measurements = select(outcome);
                neutralIntakeWorlds.Add(PerWorldRelationship.ForWorld(outcome.Seed, measurements.NeutralMarker, measurements.GrossPerThousandTicks, BinCount));
                neutralOffspringWorlds.Add(PerWorldRelationship.ForWorld(outcome.Seed, measurements.NeutralMarker, measurements.Offspring, BinCount));
            }

            Console.WriteLine();
            PrintSignCounts(label + ", diet    versus gross ingestion rate", intakeWorlds);
            PrintSignCounts(label + ", NEUTRAL versus gross ingestion rate", neutralIntakeWorlds);
            PrintSignCounts(label + ", diet    versus offspring", offspringWorlds);
            PrintSignCounts(label + ", NEUTRAL versus offspring", neutralOffspringWorlds);
        }

        private static void PrintSignCounts(string label, List<PerWorldRelationship> worlds)
        {
            if (worlds.Count == 0)
            {
                Console.WriteLine($"{label}: no world has a usable cohort");
                return;
            }

            PerWorldRelationshipSummary summary = PerWorldRelationshipSummary.Across(
                worlds,
                MinimumCohortSizePerWorld,
                BootstrapResampleCount,
                bootstrapSeed: Program.FirstSeed);

            // Direction consistency is the repository committed acceptance criterion. An interval
            // excluding zero is not one, and at these widths some will exclude it by chance.
            int majority = Math.Max(summary.PositiveWorldCount, summary.NegativeWorldCount);
            double consistency = summary.IncludedWorldCount == 0 ? 0d : majority / (double)summary.IncludedWorldCount;
            string verdict = consistency >= PairedEvolutionCriterion.MinimumDirectionConsistency ? "**PASSES 0.75**" : "fails 0.75";

            Console.WriteLine($"{label}: mean per-world r {summary.MeanCorrelation:+0.000;-0.000}, "
                + $"**{summary.PositiveWorldCount} positive / {summary.NegativeWorldCount} negative** of {summary.IncludedWorldCount}, "
                + $"direction consistency {consistency:0.000} {verdict}, "
                + $"95% interval [{summary.CorrelationInterval.LowerBound:+0.000;-0.000}, {summary.CorrelationInterval.UpperBound:+0.000;-0.000}]"
                + (summary.ExcludedForSmallCohortCount > 0 ? $", {summary.ExcludedForSmallCohortCount} worlds excluded for a cohort below {MinimumCohortSizePerWorld}" : string.Empty));
        }

        private static string Mean(List<double> values)
        {
            return values.Count == 0 ? "-" : values.Average().ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static void PrintBottleneck(List<WorldOutcome> outcomes)
        {
            long adultSamples = outcomes.Sum(outcome => outcome.Bottleneck.AdultSamples);
            long blocked = outcomes.Sum(outcome => outcome.Bottleneck.BlockedSamples);
            Console.WriteLine();
            Console.WriteLine($"which need is binding, sampled on reproduction ticks over {adultSamples} adult creature-samples");
            Console.WriteLine("| need | lowest of the three | below the gate | lowest, among blocked |");
            Console.WriteLine("|---|---|---|---|");
            foreach (ReproductiveNeed need in new[] { ReproductiveNeed.Energy, ReproductiveNeed.Hydration, ReproductiveNeed.Health })
            {
                long minimum = outcomes.Sum(outcome => outcome.Bottleneck.MinimumNeedCount(need));
                long blocking = outcomes.Sum(outcome => outcome.Bottleneck.BlockingNeedCount(need));
                long blockedMinimum = outcomes.Sum(outcome => outcome.Bottleneck.BlockedMinimumNeedCount(need));
                Console.WriteLine($"| {need} | {Share(minimum, adultSamples)} | {Share(blocking, adultSamples)} | {Share(blockedMinimum, blocked)} |");
            }

            long cooldown = outcomes.Sum(outcome => outcome.Bottleneck.CooldownBlockedSamples);
            long mateSeeking = outcomes.Sum(outcome => outcome.Bottleneck.MateSeekingBlockedSamples);
            Console.WriteLine($"blocked at all: {Share(blocked, adultSamples)}; of those, a running cooldown: {Share(cooldown, blocked)} (not a need)");
            Console.WriteLine($"passed the breeding gate but not the mate-seeking gate: {Share(mateSeeking, adultSamples)}");
        }

        private static void PrintCap(List<WorldOutcome> outcomes)
        {
            long samples = outcomes.Sum(outcome => outcome.Cap.ReproductionTickSamples);
            long saturated = outcomes.Sum(outcome => outcome.Cap.SaturatedSamples);
            long capBlocked = outcomes.Sum(outcome => outcome.Cap.CapBlockedSamples);
            long mateLimited = outcomes.Sum(outcome => outcome.Cap.MateLimitedSamples);
            int unsafeWorlds = outcomes.Count(outcome => outcome.Cap.ReproductiveSkewInterpretationIsUnsafe);

            Console.WriteLine();
            Console.WriteLine($"population cap: saturated on {Share(saturated, samples)} of reproduction ticks, cap-blocked on {Share(capBlocked, samples)}, mate-limited below the cap on {Share(mateLimited, samples)}");
            Console.WriteLine(unsafeWorlds > 0
                ? $"**ReproductiveSkewInterpretationIsUnsafe in {unsafeWorlds} of {outcomes.Count} worlds** at a threshold of {UnsafeCapBlockedFraction:0.00}. Any statement about reproductive skew is confounded by the cap and by the CreatureId-ordered scheduler beneath it."
                : $"reproductive-skew interpretation is safe in all {outcomes.Count} worlds at a threshold of {UnsafeCapBlockedFraction:0.00}");
        }

        private static void PrintRelationships(List<WorldOutcome> outcomes)
        {
            PrintOneRelationship(outcomes, "diet versus gross ingestion rate", outcome => outcome.Diet, outcome => outcome.GrossPerThousandTicks);
            PrintOneRelationship(outcomes, "diet versus offspring", outcome => outcome.Diet, outcome => outcome.Offspring);
            PrintOneRelationship(outcomes, "diet versus offspring surviving to adulthood", outcome => outcome.Diet, outcome => outcome.OffspringToAdulthood);
            PrintOneRelationship(outcomes, "lifetime gross ingestion versus offspring", outcome => outcome.LifetimeGross, outcome => outcome.Offspring);
        }

        private static void PrintOneRelationship(
            List<WorldOutcome> outcomes,
            string label,
            Func<WorldOutcome, double[]> predictor,
            Func<WorldOutcome, double[]> response)
        {
            var worlds = new List<PerWorldRelationship>();
            foreach (WorldOutcome outcome in outcomes)
            {
                if (!outcome.LedgerComplete) continue;
                worlds.Add(PerWorldRelationship.ForWorld(outcome.Seed, predictor(outcome), response(outcome), BinCount));
            }

            if (worlds.Count == 0)
            {
                Console.WriteLine($"{label}: no world has a usable cohort");
                return;
            }

            PerWorldRelationshipSummary summary = PerWorldRelationshipSummary.Across(
                worlds,
                MinimumCohortSizePerWorld,
                BootstrapResampleCount,
                bootstrapSeed: Program.FirstSeed);

            Console.WriteLine();
            Console.WriteLine($"{label}: mean per-world r {summary.MeanCorrelation:+0.000;-0.000}, "
                + $"{summary.PositiveWorldCount} worlds positive / {summary.NegativeWorldCount} negative of {summary.IncludedWorldCount}, "
                + $"95% interval [{summary.CorrelationInterval.LowerBound:+0.000;-0.000}, {summary.CorrelationInterval.UpperBound:+0.000;-0.000}]"
                + (summary.ExcludedForSmallCohortCount > 0 ? $", {summary.ExcludedForSmallCohortCount} worlds excluded for a cohort below {MinimumCohortSizePerWorld}" : string.Empty));
            Console.WriteLine($"  pooled r {summary.PooledCorrelation:+0.000;-0.000} over {summary.PooledObservationCount} creatures - **PSEUDO-REPLICATED**, reported only beside the per-world values");
            Console.Write("  per-world r:");
            foreach (PerWorldRelationship world in worlds)
            {
                if (world.CohortSize < MinimumCohortSizePerWorld) continue;
                Console.Write($" {world.WorldSeed}:{world.Correlation:+0.00;-0.00}");
            }

            Console.WriteLine();
        }

        private static void PrintProxyComparison(List<WorldOutcome> outcomes)
        {
            double measured = outcomes.Sum(outcome => outcome.PlantGross + outcome.CarcassGross);
            double estimated = outcomes.Sum(outcome => outcome.ProxyGross);
            double staleShare = outcomes.Count == 0 ? 0d : outcomes.Average(outcome => outcome.StaleShare);

            Console.WriteLine();
            Console.WriteLine($"retired delta proxy: measured gross {measured:0.0} against the proxy's {estimated:0.0}, ratio {(estimated <= 0d ? 0d : measured / estimated):0.000}");
            Console.WriteLine($"ingestion taken under a stale Seek action: {staleShare:0.00%} of gross energy, invisible to the proxy by construction");

            // By diet bin, because a diet-DEPENDENT erasure rate is the mechanism by which the old
            // instrument could have manufactured a valley that is not there. A flat ratio leaves the
            // valley's origin unexplained, which the record must then say rather than assert.
            Console.WriteLine();
            Console.WriteLine("erasure by diet bin, cohort members only");
            Console.WriteLine("| diet | creatures | measured gross | proxy gross | ratio | stale share |");
            Console.WriteLine("|---|---|---|---|---|---|");
            for (int bin = 0; bin < BinCount; bin++)
            {
                int members = 0;
                double measuredBin = 0d;
                double proxyBin = 0d;
                double staleBin = 0d;
                foreach (WorldOutcome outcome in outcomes)
                {
                    for (int index = 0; index < outcome.Diet.Length; index++)
                    {
                        if (BinOf(outcome.Diet[index]) != bin) continue;
                        members++;
                        measuredBin += outcome.LifetimeGross[index];
                        proxyBin += outcome.LifetimeProxyGross[index];
                        staleBin += outcome.LifetimeStaleGross[index];
                    }
                }

                Console.WriteLine(members == 0
                    ? $"| {Low(bin)}-{High(bin)} | 0 | - | - | - | - |"
                    : $"| {Low(bin)}-{High(bin)} | {members} | {measuredBin:0.0} | {proxyBin:0.0} | {(proxyBin <= 0d ? 0d : measuredBin / proxyBin):0.000} | {(measuredBin <= 0d ? 0d : staleBin / measuredBin):0.00%} |");
            }
        }

        private static void VerifyTheInstrumentDidNotPerturbTheSubject(Func<int, SimulationConfig> configure, SimulationScenario scenario)
        {
            // A sweep that perturbs its own subject is worse than no sweep, so the mode checks itself
            // rather than relying on the EditMode suite having been run.
            SimulationConfig config = configure(Program.FirstSeed);
            var detached = new SimulationWorld(config);
            scenario.ApplyTo(detached);
            var attached = new SimulationWorld(config) { Recorder = new IngestionRecorder() };
            scenario.ApplyTo(attached);

            for (int tick = 0; tick < 2_000; tick++)
            {
                detached.Step(config.FixedDeltaTime);
                attached.Step(config.FixedDeltaTime);
                detached.Events.Clear();
                attached.Events.Clear();
            }

            bool identical = detached.ComputeStateHash() == attached.ComputeStateHash();
            Console.WriteLine();
            Console.WriteLine(identical
                ? "instrument check: 2,000 ticks with the recorder attached and detached give an identical state hash"
                : "**INSTRUMENT CHECK FAILED: the recorder changed the run. Every number above is void.**");
        }

        private static int BinOf(double diet)
        {
            int bin = (int)(diet * BinCount);
            if (bin < 0) return 0;
            return bin >= BinCount ? BinCount - 1 : bin;
        }

        private static string Low(int bin) => (bin / (double)BinCount).ToString("0.0", CultureInfo.InvariantCulture);

        private static string High(int bin) => ((bin + 1) / (double)BinCount).ToString("0.0", CultureInfo.InvariantCulture);

        private static string Share(long count, long total)
        {
            return total <= 0 ? "-" : (count / (double)total).ToString("0.0%", CultureInfo.InvariantCulture);
        }
    }
}

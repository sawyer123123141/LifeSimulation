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
            public double[] GrossPerThousandTicks = Array.Empty<double>();
            public double[] Offspring = Array.Empty<double>();
            public double[] OffspringToAdulthood = Array.Empty<double>();
            public double[] LifetimeGross = Array.Empty<double>();
            public double[] LifetimeProxyGross = Array.Empty<double>();
            public double[] LifetimeStaleGross = Array.Empty<double>();
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
            var diet = new double[joined.Count];
            var grossRate = new double[joined.Count];
            var lifetimeGross = new double[joined.Count];
            var offspring = new double[joined.Count];
            var offspringToAdulthood = new double[joined.Count];
            var proxyGross = new double[joined.Count];
            var staleGross = new double[joined.Count];

            for (int index = 0; index < joined.Count; index++)
            {
                LifeHistoryRecord life = joined.GetLifeAt(index);
                double lifespan = Math.Max(1d, LifespanTicksOf(life, ticks));
                double gross = joined.GrossEnergyAt(index, ResourceKind.Food) + joined.GrossEnergyAt(index, ResourceKind.Carcass);

                diet[index] = life.Genome.DietSpecialization;
                lifetimeGross[index] = gross;
                grossRate[index] = gross / lifespan * 1_000d;
                offspring[index] = life.OffspringCredited;
                offspringToAdulthood[index] = CountOffspringReachingAdulthood(ledger, life.CreatureId, ticks, adultAgeTicks);
                proxyGross[index] = proxy.PositiveDeltaEnergy(life.CreatureId, ResourceKind.Food)
                    + proxy.PositiveDeltaEnergy(life.CreatureId, ResourceKind.Carcass);
                staleGross[index] = world.Recorder.StaleActionGrossEnergy(life.CreatureId, ResourceKind.Food)
                    + world.Recorder.StaleActionGrossEnergy(life.CreatureId, ResourceKind.Carcass);
            }

            outcome.Diet = diet;
            outcome.GrossPerThousandTicks = grossRate;
            outcome.LifetimeGross = lifetimeGross;
            outcome.Offspring = offspring;
            outcome.OffspringToAdulthood = offspringToAdulthood;
            outcome.LifetimeProxyGross = proxyGross;
            outcome.LifetimeStaleGross = staleGross;
            return outcome;
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

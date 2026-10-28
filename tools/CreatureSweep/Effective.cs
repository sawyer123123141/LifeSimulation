using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// How strong is genetic drift relative to census population size?
    ///
    /// <para><b>Why this exists.</b> `2026-08-30-what-finished-means-design.md` section 8 lists
    /// effective population size as a **current diagnostic**, not a future concern, and marks it
    /// MISSING. The reasoning there: a neutral gene has been measured drifting to fixation
    /// independently per world, and that observation is what makes Ne worth measuring - while absent
    /// clustering and a weak trait-to-fitness signal are **not** evidence of small Ne by themselves.
    /// The spec is explicit that these observations <i>motivate</i> the measurement and do not
    /// establish the answer. This tool measures it; it does not assume it.</para>
    ///
    /// <para><b>No definitive Ne is produced, deliberately.</b> Effective size has several related
    /// definitions - inbreeding, variance, coalescent - which do not coincide under the overlapping
    /// generations and age structure this simulation has. A single variance-in-offspring equation
    /// assumes discrete generations and would return a number that looks authoritative and is not.
    /// The spec asks for "a useful diagnostic of the strength of drift relative to census population
    /// size, not false precision", so this reports the demographic inputs, a clearly-labelled ratio
    /// proxy, and an independent realised-drift measurement, and lets them be compared.</para>
    ///
    /// <para><b>Two routes, as specified.</b>
    /// <b>Route A, demographic:</b> lifetime reproductive success and its variance over the
    /// complete-life cohort, with generation length measured rather than assumed.
    /// <b>Route B, realised drift:</b> the spread of <c>NeutralMarker</c> <i>among replicate worlds</i>
    /// over time. The marker is read by no behaviour system and is pinned inert by
    /// <c>LivenessTests</c>, so its divergence across identically-configured worlds is drift and
    /// mutation alone.</para>
    ///
    /// <para><b>Route B's two cautions, both required by the spec.</b> The marker is a
    /// <b>continuous trait, not a biallelic locus</b>, so this is variance among replicate means and
    /// not allele-frequency variance. And <b>mutational input is not separable here</b>: the
    /// inheritance model adds Gaussian noise every birth and no configuration knob disables it, so
    /// the measured spread is drift <i>plus</i> mutation and is an upper bound on drift alone. Both
    /// are stated again in the output so a reader of the console artefact cannot miss them.</para>
    ///
    /// <para><b>Reported as a trajectory, not a single figure.</b> Every cell in this project is
    /// recorded as collapsing (`p6-regime-b-triage-2026-09-05.md`), and this project has already
    /// published one reading taken at the peak of a boom. A single end-of-run Ne would be the same
    /// mistake. The snapshots show what drift does through the boom and the crash.</para>
    /// </summary>
    internal static class Effective
    {
        private const int DefaultSnapshots = 12;

        private sealed class Snapshot
        {
            public long Tick;
            public int Census;
            public double MarkerMean;
            public double MarkerVarianceWithin;

            /// <summary>
            /// A gene under strong, reproducible selection in this cell - `defense` reaches t +12.7
            /// here. Carried beside the inert marker as an internal contrast: if selection is
            /// overcoming drift, identically-configured worlds should converge on the same defense
            /// value while the neutral marker diverges freely.
            /// </summary>
            public double DefenseMean;
            public double DefenseVarianceWithin;
        }

        private sealed class WorldOutcome
        {
            public int Seed;
            public bool LedgerComplete;
            public int FinalCensus;
            public bool Extinct;
            public List<Snapshot> Snapshots = new List<Snapshot>();

            // Route A, over the complete-life cohort.
            public int CohortSize;
            public double MeanOffspring;
            public double VarianceOffspring;
            public double ShareBreeding;
            public double MeanGenerationTicks;
            public int GenerationSamples;
        }

        public static void Report(int seedCount, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario, int snapshots)
        {
            int snapshotCount = snapshots > 1 ? snapshots : DefaultSnapshots;
            long interval = Math.Max(1L, ticks / snapshotCount);

            var outcomes = new List<WorldOutcome>();
            for (int index = 0; index < seedCount; index++)
            {
                outcomes.Add(RunOne(Program.FirstSeed + index, ticks, configure, scenario, interval));
            }

            PrintHeader(seedCount, ticks, interval, outcomes);
            PrintRouteA(outcomes);
            PrintRouteB(outcomes, interval);
            PrintComparison(outcomes);
        }

        private static WorldOutcome RunOne(int seed, int ticks, Func<int, SimulationConfig> configure, SimulationScenario scenario, long interval)
        {
            SimulationConfig config = configure(seed);
            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);

            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, world.Creatures);
            ledger.Observe(world);

            var outcome = new WorldOutcome { Seed = seed };
            TakeSnapshot(outcome, world);

            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);

                // Every tick, or the pedigree goes permanently incomplete - same contract as
                // LifeHistory.
                ledger.RecordCompleteBatch(world.Events, world.CurrentTick);
                world.Events.Clear();
                ledger.Observe(world);

                if (world.CurrentTick % interval == 0) TakeSnapshot(outcome, world);
            }

            outcome.FinalCensus = world.CreatureCount;
            outcome.Extinct = world.CreatureCount == 0;
            outcome.LedgerComplete = ledger.IsComplete;
            MeasureRouteA(outcome, ledger, config, ticks);
            return outcome;
        }

        private static void TakeSnapshot(WorldOutcome outcome, SimulationWorld world)
        {
            int count = world.CreatureCount;
            var markers = new List<double>(count);
            var defense = new List<double>(count);
            for (int index = 0; index < count; index++)
            {
                Genome genome = world.Creatures.GetGenomeAt(index);
                markers.Add(genome.NeutralMarker);
                defense.Add(genome.Defense);
            }

            MeanAndVariance(markers, out double markerMean, out double markerWithin);
            MeanAndVariance(defense, out double defenseMean, out double defenseWithin);

            outcome.Snapshots.Add(new Snapshot
            {
                Tick = world.CurrentTick,
                Census = count,
                MarkerMean = markerMean,
                MarkerVarianceWithin = markerWithin,
                DefenseMean = defenseMean,
                DefenseVarianceWithin = defenseWithin,
            });
        }

        /// <summary>
        /// Lifetime reproductive success over the complete-life cohort, plus generation length.
        ///
        /// <para>The cohort is <see cref="FitnessCohortKind.CompleteLife"/> - the same censoring
        /// contract <c>--life-history</c> uses - because an individual still alive at the end of the
        /// run has an unfinished offspring count, and including it would bias the variance downward
        /// exactly where the population is largest.</para>
        /// </summary>
        private static void MeanAndVariance(List<double> values, out double mean, out double variance)
        {
            mean = values.Count == 0 ? 0d : values.Average();
            double centred = mean;
            variance = values.Count < 2
                ? 0d
                : values.Sum(value => (value - centred) * (value - centred)) / (values.Count - 1);
        }

        private static void MeasureRouteA(WorldOutcome outcome, LifeHistoryLedger ledger, SimulationConfig config, int ticks)
        {
            FitnessCohort cohort = FitnessCohort.Select(ledger, config.Schedule, ticks, FitnessCohortKind.CompleteLife);
            var offspring = new List<double>(cohort.Count);
            var generationGaps = new List<double>();

            for (int index = 0; index < cohort.Count; index++)
            {
                if (!ledger.TryGet(cohort.GetIdAt(index), out LifeHistoryRecord record)) continue;
                offspring.Add(record.OffspringCredited);
            }

            // Generation length, measured rather than assumed: the mean age of a parent when a
            // child is born. Needed because every per-generation quantity below would otherwise be
            // quoted in ticks, and ticks are not generations in an age-structured population.
            for (int index = 0; index < ledger.PedigreeCount; index++)
            {
                if (!ledger.TryGet(ledger.GetPedigreeIdAt(index), out LifeHistoryRecord child)) continue;
                if (child.BirthTick <= 0) continue;

                foreach (CreatureId parentId in new[] { child.FirstParent, child.SecondParent })
                {
                    if (parentId.Value <= 0) continue;
                    if (!ledger.TryGet(parentId, out LifeHistoryRecord parent)) continue;
                    long age = child.BirthTick - parent.BirthTick;
                    if (age > 0) generationGaps.Add(age);
                }
            }

            outcome.CohortSize = offspring.Count;
            if (offspring.Count > 0)
            {
                outcome.MeanOffspring = offspring.Average();
                outcome.VarianceOffspring = offspring.Count < 2
                    ? 0d
                    : offspring.Sum(value => (value - outcome.MeanOffspring) * (value - outcome.MeanOffspring)) / (offspring.Count - 1);
                outcome.ShareBreeding = offspring.Count(value => value > 0d) / (double)offspring.Count;
            }

            outcome.GenerationSamples = generationGaps.Count;
            outcome.MeanGenerationTicks = generationGaps.Count == 0 ? 0d : generationGaps.Average();
        }

        private static void PrintHeader(int seedCount, int ticks, long interval, List<WorldOutcome> outcomes)
        {
            int complete = outcomes.Count(outcome => outcome.LedgerComplete);
            int extinct = outcomes.Count(outcome => outcome.Extinct);
            Console.WriteLine();
            Console.WriteLine($"effective population size diagnostic, {outcomes.Count} worlds of {ticks} ticks, snapshot every {interval}");
            Console.WriteLine($"  pedigree complete in {complete} of {seedCount} worlds; {extinct} extinct at the end");
            Console.WriteLine("  NO definitive Ne is produced. Effective size has several non-coinciding definitions under");
            Console.WriteLine("  overlapping generations; this is a diagnostic of drift strength relative to census size.");
            if (complete < seedCount)
            {
                Console.WriteLine("  WORLDS WITH AN INCOMPLETE PEDIGREE ARE EXCLUDED FROM ROUTE A BELOW.");
            }
        }

        private static void PrintRouteA(List<WorldOutcome> outcomes)
        {
            WorldOutcome[] usable = outcomes.Where(outcome => outcome.LedgerComplete && outcome.CohortSize > 1).ToArray();
            Console.WriteLine();
            Console.WriteLine("ROUTE A - demographic, from lifetime reproductive success over the complete-life cohort");
            if (usable.Length == 0)
            {
                Console.WriteLine("  no world has a complete pedigree and a usable cohort - route A cannot be computed");
                return;
            }

            Console.WriteLine("| seed | cohort | final N | mean k | var k | var/mean | share breeding | generation (ticks) | Ne/N proxy |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|---|");
            foreach (WorldOutcome outcome in usable.OrderBy(outcome => outcome.Seed))
            {
                Console.WriteLine(string.Join(" | ",
                    $"| {outcome.Seed}",
                    outcome.CohortSize.ToString(CultureInfo.InvariantCulture),
                    outcome.FinalCensus.ToString(CultureInfo.InvariantCulture),
                    outcome.MeanOffspring.ToString("0.000", CultureInfo.InvariantCulture),
                    outcome.VarianceOffspring.ToString("0.000", CultureInfo.InvariantCulture),
                    (outcome.MeanOffspring <= 0d ? 0d : outcome.VarianceOffspring / outcome.MeanOffspring).ToString("0.000", CultureInfo.InvariantCulture),
                    outcome.ShareBreeding.ToString("0.000", CultureInfo.InvariantCulture),
                    outcome.MeanGenerationTicks.ToString("0", CultureInfo.InvariantCulture),
                    VarianceRatioProxy(outcome).ToString("0.000", CultureInfo.InvariantCulture) + " |"));
            }

            Console.WriteLine();
            Console.WriteLine($"  pooled: mean k {usable.Average(o => o.MeanOffspring):0.000}"
                + $", var k {usable.Average(o => o.VarianceOffspring):0.000}"
                + $", share breeding {usable.Average(o => o.ShareBreeding):0.000}"
                + $", generation {usable.Average(o => o.MeanGenerationTicks):0} ticks"
                + $", Ne/N proxy {usable.Average(VarianceRatioProxy):0.000}");
            Console.WriteLine("  The Ne/N proxy is k / (k - 1 + var k / k), the variance-effective ratio in its general");
            Console.WriteLine("  form. It assumes constant size and discrete generations, and THIS POPULATION IS");
            Console.WriteLine("  NEITHER, so read it as an index of reproductive evenness rather than as Ne/N. A value");
            Console.WriteLine("  near 1 means reproduction is spread evenly enough that skew is not depressing Ne.");
            Console.WriteLine("  Both parents are credited for each birth, so mean k near 2 is replacement.");
        }

        /// <summary>
        /// The variance-effective ratio in its GENERAL form, k / (k - 1 + Vk/k).
        ///
        /// <para><b>Corrected after the first run.</b> The first version used 4 / (Vk/k + 2), which
        /// is the special case at replacement (mean k = 2). Mean k here is near 4 - both parents are
        /// credited for each birth and the population is growing - and that version returned
        /// <b>1.400</b>, a ratio above one, which is not a possible value for Ne/N and was a bug in
        /// the instrument rather than a finding about the population.</para>
        ///
        /// <para>Still a proxy, and still assumes constant size and discrete generations, neither of
        /// which holds here. Read it as an index of reproductive evenness.</para>
        /// </summary>
        private static double VarianceRatioProxy(WorldOutcome outcome)
        {
            double k = outcome.MeanOffspring;
            if (k <= 0d) return 0d;
            double denominator = k - 1d + (outcome.VarianceOffspring / k);
            return denominator <= 0d ? 0d : k / denominator;
        }

        private static void PrintRouteB(List<WorldOutcome> outcomes, long interval)
        {
            Console.WriteLine();
            Console.WriteLine("ROUTE B - realised drift, from the spread of NeutralMarker AMONG replicate worlds");
            Console.WriteLine("  CAUTION 1: the marker is a continuous trait, not a biallelic locus. This is variance");
            Console.WriteLine("             among replicate population means, not allele-frequency variance.");
            Console.WriteLine("  CAUTION 2: the inheritance model adds Gaussian noise at every birth and no configuration");
            Console.WriteLine("             knob disables it, so the spread below is drift PLUS mutation - an upper bound");
            Console.WriteLine("             on drift alone.");
            Console.WriteLine();

            Console.WriteLine("  The STRUCTURE column is among / (among + within): the share of all variation in a");
            Console.WriteLine("  gene that sits BETWEEN worlds rather than inside them. Mutation inflates both parts, so");
            Console.WriteLine("  a structure share that climbs is drift pulling worlds apart faster than mutation fills");
            Console.WriteLine("  them in; a flat share is consistent with mutation driving both.");
            Console.WriteLine("  DEFENSE is carried as the contrast - it reaches t +12.7 in this cell, so if selection");
            Console.WriteLine("  beats drift its worlds should stay together while the inert marker's drift apart.");
            Console.WriteLine();

            int maximum = outcomes.Max(outcome => outcome.Snapshots.Count);
            Console.WriteLine("| tick | worlds | mean census | marker among | marker within | marker STRUCTURE | defense among | defense within | defense STRUCTURE |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|---|");
            for (int step = 0; step < maximum; step++)
            {
                Snapshot[] live = outcomes
                    .Where(outcome => step < outcome.Snapshots.Count)
                    .Select(outcome => outcome.Snapshots[step])
                    .Where(snapshot => snapshot.Census > 0)
                    .ToArray();
                if (live.Length == 0) continue;

                double markerAmong = Among(live.Select(snapshot => snapshot.MarkerMean));
                double markerWithin = live.Average(snapshot => snapshot.MarkerVarianceWithin);
                double defenseAmong = Among(live.Select(snapshot => snapshot.DefenseMean));
                double defenseWithin = live.Average(snapshot => snapshot.DefenseVarianceWithin);

                Console.WriteLine($"| {live[0].Tick} | {live.Length} | {live.Average(s => s.Census):0.0}"
                    + $" | {markerAmong:0.000000} | {markerWithin:0.000000} | {Structure(markerAmong, markerWithin):0.000}"
                    + $" | {defenseAmong:0.000000} | {defenseWithin:0.000000} | {Structure(defenseAmong, defenseWithin):0.000} |");
            }
        }

        private static double Among(IEnumerable<double> means)
        {
            double[] values = means.ToArray();
            if (values.Length < 2) return 0d;
            double grand = values.Average();
            return values.Sum(value => (value - grand) * (value - grand)) / (values.Length - 1);
        }

        private static double Structure(double among, double within)
        {
            double total = among + within;
            return total <= 0d ? 0d : among / total;
        }

        private static void PrintComparison(List<WorldOutcome> outcomes)
        {
            Console.WriteLine();
            Console.WriteLine("WHAT TO READ FROM THIS");
            Console.WriteLine("  Route A says how unequally reproduction is distributed. A var/mean well above 1 means a");
            Console.WriteLine("  minority of individuals produce most offspring, which shrinks effective size below census");
            Console.WriteLine("  size regardless of how many animals are alive.");
            Console.WriteLine("  Route B says how far identically-configured worlds actually drifted apart. If the");
            Console.WriteLine("  among-world variance grows while census size is large, drift is strong at high census");
            Console.WriteLine("  size, and census size is not protecting against it.");
            Console.WriteLine("  The two are independent. Agreement is evidence; disagreement means at least one");
            Console.WriteLine("  assumption behind route A does not hold here, which is itself worth knowing.");
            Console.WriteLine("  NEITHER establishes that Ne is small. That was the error the frozen spec corrects.");
        }
    }
}

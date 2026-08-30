using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Behavior;

namespace LifeSimulation.Tools.HistoryProbe
{
    /// <summary>
    /// What the P5 history panel reports while watching `Y`.
    ///
    /// <para><b>Why this exists.</b> Every P5 test feeds the analysis synthetic ancestry - which is
    /// the right way to test clustering logic and says nothing about whether a real population ever
    /// produces a split worth showing. The panel is on screen at all times, 520 x 340 pixels of it,
    /// and nobody had ever recorded what it says.</para>
    ///
    /// <para>The world is the shipped `Y` - the four-way split layout, at `Y`'s own configuration -
    /// so this describes the panel the user is looking at, not a neighbouring world.</para>
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Overridable so the same probe can ask whether divergence is limited by TIME or by the
        /// mechanism. If maximum pairwise distance and the spatial correlation both plateau while
        /// generations keep accumulating, more world will not help either.
        /// </summary>
        private static int _ticks = 12000;
        private const int FirstSeed = 42;

        private static int _seedCount = 6;

        /// <summary>
        /// Thresholds to cluster the final population at. The panel runs at 0.25; the rest say
        /// whether that is the number that is wrong or whether nothing would separate this
        /// population.
        /// </summary>
        private static readonly float[] SweepThresholds = { .05f, .10f, .15f, .25f, .40f };

        private sealed class RunResult
        {
            public int Seed;
            public int Population;
            public int Observations;
            public int DisplayEvents;
            public int NotableEvents;
            public bool Overflowed;
            public bool AncestryComplete;
            public string StatusText;
            public Dictionary<ClusterHistoryEventKind, int> KindCounts = new Dictionary<ClusterHistoryEventKind, int>();
            public int MaximumClusterCount;
            public int FinalClusterCount;
            public float MaximumPairwiseDistance;
            public float MeanPairwiseDistance;
            public int[] ClustersByThreshold;
            public int HighestGeneration;
            public double SpatialGeneticCorrelation;
            public float NearPairDistance;
            public float FarPairDistance;
            public int NearPairCount;
            public int FarPairCount;
        }

        private static int Main(string[] arguments)
        {
            foreach (string argument in arguments)
            {
                if (argument.StartsWith("--seeds=", StringComparison.Ordinal))
                {
                    _seedCount = int.Parse(argument.Substring(8), CultureInfo.InvariantCulture);
                }
                else if (argument.StartsWith("--ticks=", StringComparison.Ordinal))
                {
                    _ticks = int.Parse(argument.Substring(8), CultureInfo.InvariantCulture);
                }
            }

            Console.WriteLine($"P5 history in the shipped Y, seeds {FirstSeed}..{FirstSeed + _seedCount - 1}, {_ticks} ticks");
            Console.WriteLine($"observation cadence {P5HistoryPanelSession.ObservationIntervalTicks} ticks, genetic threshold {P5HistoryPanelSession.GeneticThreshold}");
            Console.WriteLine();

            var results = new RunResult[_seedCount];
            Parallel.For(0, _seedCount, index => { results[index] = Execute(FirstSeed + index); });

            Console.WriteLine("| seed | population | observations | events | notable | clusters (max/final) | ancestry complete | overflowed |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|");
            foreach (RunResult result in results)
            {
                Console.WriteLine($"| {result.Seed} | {result.Population} | {result.Observations} | {result.DisplayEvents}"
                    + $" | {result.NotableEvents} | {result.MaximumClusterCount}/{result.FinalClusterCount}"
                    + $" | {result.AncestryComplete} | {result.Overflowed} |");
            }

            Console.WriteLine();
            Console.WriteLine("event kinds, summed over seeds");
            var totals = new Dictionary<ClusterHistoryEventKind, int>();
            foreach (RunResult result in results)
            {
                foreach (KeyValuePair<ClusterHistoryEventKind, int> pair in result.KindCounts)
                {
                    totals.TryGetValue(pair.Key, out int running);
                    totals[pair.Key] = running + pair.Value;
                }
            }

            if (totals.Count == 0)
            {
                Console.WriteLine("  NONE. The panel has nothing to report in this world.");
            }
            else
            {
                foreach (KeyValuePair<ClusterHistoryEventKind, int> pair in totals.OrderByDescending(pair => pair.Value))
                {
                    Console.WriteLine($"  {pair.Key}: {pair.Value}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("is there structure to find? final-population pairwise genetic distance, and clusters by threshold");
            Console.WriteLine("| seed | mean distance | max distance | " + string.Join(" | ", SweepThresholds.Select(t => "t=" + t.ToString("0.00", CultureInfo.InvariantCulture))) + " |");
            Console.WriteLine("|---|---|---|" + string.Concat(SweepThresholds.Select(_ => "---|")));
            foreach (RunResult result in results)
            {
                Console.WriteLine($"| {result.Seed} | {result.MeanPairwiseDistance.ToString("0.000", CultureInfo.InvariantCulture)}"
                    + $" | {result.MaximumPairwiseDistance.ToString("0.000", CultureInfo.InvariantCulture)} | "
                    + string.Join(" | ", result.ClustersByThreshold) + " |");
            }

            Console.WriteLine();
            Console.WriteLine("does space structure genes? (the premise under P6 partitioning as a speciation route)");
            Console.WriteLine("| seed | generations | corr(distance apart, genetic distance) | near pairs <5u | far pairs >20u | far - near |");
            Console.WriteLine("|---|---|---|---|---|---|");
            foreach (RunResult result in results)
            {
                Console.WriteLine($"| {result.Seed} | {result.HighestGeneration}"
                    + $" | {result.SpatialGeneticCorrelation.ToString("0.000", CultureInfo.InvariantCulture)}"
                    + $" | {result.NearPairDistance.ToString("0.000", CultureInfo.InvariantCulture)} (n={result.NearPairCount})"
                    + $" | {result.FarPairDistance.ToString("0.000", CultureInfo.InvariantCulture)} (n={result.FarPairCount})"
                    + $" | {(result.FarPairDistance - result.NearPairDistance).ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture)} |");
            }

            Console.WriteLine();
            Console.WriteLine("status text the panel would show at the end of each run");
            foreach (RunResult result in results)
            {
                Console.WriteLine($"  {result.Seed}: {result.StatusText}");
            }

            return 0;
        }

        /// <summary>`Y`'s configuration, as the presenter builds it. Kept in step by hand, like tools/SitePilot.</summary>
        private static SimulationConfig CreateConfig(int seed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: seed, initialPopulation: 4);
            return new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 96,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
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
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: true,
                terrainDrivenTemperatureEnabled: true,
                healthRecoveryEnabled: true,
                wanderHomeHysteresisEnabled: true,
                feedInPlaceEnabled: true);
        }

        private static double Correlation(List<double> first, List<double> second)
        {
            int count = first.Count;
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

        private static RunResult Execute(int seed)
        {
            SimulationConfig config = CreateConfig(seed);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate
                .SplitSites("p6-defense-calibration-split4", parts: 4, spread: 6f)
                .ApplyTo(world);

            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);
            var result = new RunResult { Seed = seed };

            for (int tick = 0; tick < _ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);

                // The presenter advances the session every frame, so this does too. Anything else
                // would be measuring a cadence the panel does not run at.
                session.Advance(world);
                world.Events.Clear();
            }

            result.Population = world.CreatureCount;
            result.Observations = session.ObservationCount;
            result.DisplayEvents = session.DisplayEventCount;
            result.NotableEvents = session.NotableEventCount;
            result.Overflowed = session.OutputOverflowed;
            result.AncestryComplete = session.AncestryIsComplete;
            result.StatusText = session.StatusText;

            // Is there anything for a threshold to FIND? "No splits" has two very different causes -
            // a threshold set too high over a structured population, or a population with no
            // structure in it at all - and only the spread of pairwise distances separates them.
            PopulationGenomeSnapshot snapshot = PopulationGenomeSnapshot.Capture(world.CurrentTick, world.Creatures);
            double distanceSum = 0d;
            int pairCount = 0;
            for (int first = 0; first < snapshot.Count; first++)
            {
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    float distance = GeneticDistance.Between(snapshot.GetGenomeAt(first), snapshot.GetGenomeAt(second));
                    distanceSum += distance;
                    pairCount++;
                    if (distance > result.MaximumPairwiseDistance) result.MaximumPairwiseDistance = distance;
                }
            }

            result.MeanPairwiseDistance = pairCount == 0 ? 0f : (float)(distanceSum / pairCount);
            result.ClustersByThreshold = SweepThresholds
                .Select(threshold => GeneticClusters.From(snapshot, threshold).Count)
                .ToArray();
            result.HighestGeneration = world.CaptureStatistics().HighestGeneration;

            // DOES SPACE STRUCTURE GENES HERE?
            //
            // This is the premise underneath P6 world partitioning as a route to speciation: that
            // separating a population geographically makes it diverge genetically. Before building a
            // phase on it, ask whether the effect exists at all at the scale that already exists. If
            // animals living 20 units apart are no more distant genetically than neighbours, then
            // space does not structure genes in this ecology and more space will not either.
            //
            // Pearson correlation over every pair, plus the blunter near/far means, because a
            // correlation near zero and a correlation of 0.3 look the same in one number if the
            // spread is wide.
            var spatial = new List<double>();
            var genetic = new List<double>();
            double nearSum = 0d;
            double farSum = 0d;
            int nearCount = 0;
            int farCount = 0;
            for (int first = 0; first < snapshot.Count; first++)
            {
                if (!world.TryGetCreatureIndex(snapshot.GetIdAt(first), out int firstIndex)) continue;
                SimVector2 firstPosition = world.GetCreatureMovementAt(firstIndex).Position;
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    if (!world.TryGetCreatureIndex(snapshot.GetIdAt(second), out int secondIndex)) continue;
                    SimVector2 secondPosition = world.GetCreatureMovementAt(secondIndex).Position;

                    double apart = SimVector2.Distance(firstPosition, secondPosition);
                    double unlike = GeneticDistance.Between(snapshot.GetGenomeAt(first), snapshot.GetGenomeAt(second));
                    spatial.Add(apart);
                    genetic.Add(unlike);

                    if (apart < 5d)
                    {
                        nearSum += unlike;
                        nearCount++;
                    }
                    else if (apart > 20d)
                    {
                        farSum += unlike;
                        farCount++;
                    }
                }
            }

            result.SpatialGeneticCorrelation = Correlation(spatial, genetic);
            result.NearPairCount = nearCount;
            result.FarPairCount = farCount;
            result.NearPairDistance = nearCount == 0 ? 0f : (float)(nearSum / nearCount);
            result.FarPairDistance = farCount == 0 ? 0f : (float)(farSum / farCount);

            for (int index = 0; index < session.DisplayEventCount; index++)
            {
                ClusterHistoryEvent historyEvent = session.GetEventAt(index);
                result.KindCounts.TryGetValue(historyEvent.Kind, out int running);
                result.KindCounts[historyEvent.Kind] = running + 1;

                // How many clusters the analysis was tracking at each observation, which is the
                // "useful separation" half of the P5 exit gate.
                if (historyEvent.CurrentTrackCount > result.MaximumClusterCount)
                {
                    result.MaximumClusterCount = historyEvent.CurrentTrackCount;
                }

                result.FinalClusterCount = historyEvent.CurrentTrackCount;
            }

            return result;
        }
    }
}

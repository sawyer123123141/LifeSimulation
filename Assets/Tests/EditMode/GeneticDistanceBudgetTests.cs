using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// P5 clustering is O(n^2) in pair comparisons, and each comparison allocated two trait arrays.
    /// Measured before changing anything: a constant 240 bytes per pair, which is 187 KB per
    /// observation at 40 creatures, 4.8 MB at 200, and 120 MB and 126 ms at 1,000. The snapshot is
    /// not sampled at any of those sizes, so the cost is real rather than an artefact of a cap.
    ///
    /// <para>These tests pin both halves of the fix: the result must not change, and the allocation
    /// must scale with the population rather than with the number of pairs.</para>
    /// </summary>
    public sealed class GeneticDistanceBudgetTests
    {
        [Test]
        public void ClusteringAgreesWithPairwiseGeneticDistance()
        {
            PopulationGenomeSnapshot snapshot = BuildSnapshot(60);
            const float threshold = .25f;

            GeneticClusters clusters = GeneticClusters.From(snapshot, threshold);

            // Every pair the reference distance calls close must share a cluster, and clustering is
            // transitive, so a pair in the same cluster must be linked by some chain of close pairs.
            for (int first = 0; first < snapshot.Count; first++)
            {
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    bool close = GeneticDistance.Between(snapshot.GetGenomeAt(first), snapshot.GetGenomeAt(second)) <= threshold;
                    if (close)
                    {
                        Assert.That(clusters.GetClusterIndexAt(first), Is.EqualTo(clusters.GetClusterIndexAt(second)),
                            $"creatures {first} and {second} are within the threshold but landed in different clusters");
                    }
                }
            }
        }

        [Test]
        public void ClusteringAllocationScalesWithPopulationNotWithPairs()
        {
            PopulationGenomeSnapshot small = BuildSnapshot(50);
            PopulationGenomeSnapshot large = BuildSnapshot(200);

            // Warm up: first call JITs and touches statics, which would land in the measurement.
            GeneticClusters.From(small, .25f);
            GeneticClusters.From(large, .25f);

            long smallBytes = MeasureAllocation(small);
            long largeBytes = MeasureAllocation(large);

            // Pairs grow 16x from 50 to 200 creatures (1,225 to 19,900). Allocation must not.
            // Before the fix this ratio was ~16; a linear implementation lands near 4.
            double ratio = largeBytes / (double)Math.Max(1, smallBytes);
            Assert.That(ratio, Is.LessThan(8.0),
                $"allocation grew {ratio:F1}x for a 16x increase in pairs ({smallBytes} to {largeBytes} bytes) - it is still per-pair");
            Assert.That(largeBytes, Is.LessThan(1_000_000),
                $"clustering 200 creatures allocated {largeBytes} bytes; it was 4,777,720 before the fix");
        }

        private static long MeasureAllocation(PopulationGenomeSnapshot snapshot)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            GeneticClusters.From(snapshot, .25f);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static PopulationGenomeSnapshot BuildSnapshot(int population)
        {
            var config = new SimulationConfig(
                worldSeed: 42,
                initialPopulation: population,
                schedule: SimulationConfig.CreatePrototype1Defaults(42, 4).Schedule,
                maximumPopulation: Math.Max(population, 1000));
            var world = new SimulationWorld(config);
            return PopulationGenomeSnapshot.Capture(world.CurrentTick, world.Creatures);
        }
    }
}

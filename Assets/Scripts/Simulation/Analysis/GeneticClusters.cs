using System;

using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Analysis-only connected genetic groups; callers must state their threshold explicitly.</summary>
    public sealed class GeneticClusters
    {
        private readonly int[] _clusterIndices;

        private GeneticClusters(int count, int[] clusterIndices)
        {
            Count = count;
            _clusterIndices = clusterIndices;
        }

        public int Count { get; }

        public static GeneticClusters From(PopulationGenomeSnapshot snapshot, float threshold)
        {
            if (threshold < 0f || threshold > 1f) throw new ArgumentOutOfRangeException(nameof(threshold));
            var parents = new int[snapshot.Count];
            for (int index = 0; index < parents.Length; index++) parents[index] = index;
            // Traits are flattened ONCE per snapshot rather than rebuilt for every comparison.
            // Clustering is O(n^2) in pairs, and the previous form allocated two trait arrays per
            // pair: a measured 240 bytes each time, which is 4.8 MB at 200 creatures and 120 MB and
            // 126 ms at 1,000. This makes the allocation scale with the population instead.
            var traits = new float[snapshot.Count * Genome.TraitCount];
            for (int index = 0; index < snapshot.Count; index++)
            {
                snapshot.GetGenomeAt(index).WriteTraits(traits, index * Genome.TraitCount);
            }

            for (int first = 0; first < snapshot.Count; first++)
            {
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    if (GeneticDistance.Between(traits, first * Genome.TraitCount, second * Genome.TraitCount, Genome.TraitCount) <= threshold)
                    {
                        Union(parents, first, second);
                    }
                }
            }
            var clusterIndices = new int[snapshot.Count];
            int count = 0;
            for (int index = 0; index < parents.Length; index++)
            {
                int root = Find(parents, index);
                int clusterIndex = 0;
                while (clusterIndex < index && Find(parents, clusterIndex) != root) clusterIndex++;
                if (clusterIndex == index) count++;
                clusterIndices[index] = clusterIndex;
            }

            return new GeneticClusters(count, clusterIndices);
        }

        public int GetClusterIndexAt(int sampleIndex)
        {
            if ((uint)sampleIndex >= (uint)_clusterIndices.Length) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            return _clusterIndices[sampleIndex];
        }

        private static int Find(int[] parents, int index)
        {
            while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }
        private static void Union(int[] parents, int first, int second)
        {
            first = Find(parents, first); second = Find(parents, second);
            if (first != second) parents[second] = first;
        }
    }
}

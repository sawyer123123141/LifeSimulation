using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Analysis-only connected genetic groups; callers must state their threshold explicitly.</summary>
    public sealed class GeneticClusters
    {
        private GeneticClusters(int count) { Count = count; }
        public int Count { get; }

        public static GeneticClusters From(PopulationGenomeSnapshot snapshot, float threshold)
        {
            if (threshold < 0f || threshold > 1f) throw new ArgumentOutOfRangeException(nameof(threshold));
            var parents = new int[snapshot.Count];
            for (int index = 0; index < parents.Length; index++) parents[index] = index;
            for (int first = 0; first < snapshot.Count; first++)
            {
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    if (GeneticDistance.Between(snapshot.GetGenomeAt(first), snapshot.GetGenomeAt(second)) <= threshold) Union(parents, first, second);
                }
            }
            int count = 0;
            for (int index = 0; index < parents.Length; index++) if (Find(parents, index) == index) count++;
            return new GeneticClusters(count);
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

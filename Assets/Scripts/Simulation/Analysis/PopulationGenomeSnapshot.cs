using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Host-triggered immutable population sample for P5 analysis.</summary>
    public sealed class PopulationGenomeSnapshot
    {
        private readonly CreatureId[] _ids;
        private readonly Genome[] _genomes;

        private PopulationGenomeSnapshot(long tick, CreatureId[] ids, Genome[] genomes, bool isSampled, int sourcePopulationCount, int sampleLimit)
        {
            Tick = tick;
            _ids = ids;
            _genomes = genomes;
            IsSampled = isSampled;
            SourcePopulationCount = sourcePopulationCount;
            SampleLimit = sampleLimit;
        }

        public long Tick { get; }
        public int Count => _ids.Length;
        public bool IsSampled { get; }
        public int SourcePopulationCount { get; }
        public int SampleLimit { get; }

        public static PopulationGenomeSnapshot Capture(long tick, CreatureStore creatures)
        {
            var ids = new CreatureId[creatures.Count];
            var genomes = new Genome[creatures.Count];
            for (int index = 0; index < creatures.Count; index++)
            {
                ids[index] = creatures.GetIdAt(index);
                genomes[index] = creatures.GetGenomeAt(index);
            }
            return new PopulationGenomeSnapshot(tick, ids, genomes, isSampled: false, sourcePopulationCount: creatures.Count, sampleLimit: 0);
        }

        public static PopulationGenomeSnapshot CaptureSample(long tick, CreatureStore creatures, int maximumCount)
        {
            if (maximumCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCount));
            int sampleCount = Math.Min(creatures.Count, maximumCount);
            var sourceIndices = new int[creatures.Count];
            for (int index = 0; index < sourceIndices.Length; index++) sourceIndices[index] = index;
            SortByCreatureId(creatures, sourceIndices);

            var ids = new CreatureId[sampleCount];
            var genomes = new Genome[sampleCount];
            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                int sortedIndex = sampleCount == 1 ? 0 : (sampleIndex * (creatures.Count - 1)) / (sampleCount - 1);
                int sourceIndex = sourceIndices[sortedIndex];
                ids[sampleIndex] = creatures.GetIdAt(sourceIndex);
                genomes[sampleIndex] = creatures.GetGenomeAt(sourceIndex);
            }

            return new PopulationGenomeSnapshot(tick, ids, genomes, isSampled: true, sourcePopulationCount: creatures.Count, sampleLimit: maximumCount);
        }

        public CreatureId GetIdAt(int index) => _ids[index];
        public Genome GetGenomeAt(int index) => _genomes[index];

        private static void SortByCreatureId(CreatureStore creatures, int[] sourceIndices)
        {
            for (int first = 0; first < sourceIndices.Length; first++)
            {
                int smallest = first;
                for (int candidate = first + 1; candidate < sourceIndices.Length; candidate++)
                {
                    if (creatures.GetIdAt(sourceIndices[candidate]).Value < creatures.GetIdAt(sourceIndices[smallest]).Value)
                    {
                        smallest = candidate;
                    }
                }

                int sourceIndex = sourceIndices[first];
                sourceIndices[first] = sourceIndices[smallest];
                sourceIndices[smallest] = sourceIndex;
            }
        }
    }
}

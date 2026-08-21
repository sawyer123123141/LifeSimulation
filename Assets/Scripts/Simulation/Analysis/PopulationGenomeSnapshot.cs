using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Host-triggered immutable population sample for P5 analysis.</summary>
    public sealed class PopulationGenomeSnapshot
    {
        private readonly CreatureId[] _ids;
        private readonly Genome[] _genomes;

        private PopulationGenomeSnapshot(long tick, CreatureId[] ids, Genome[] genomes)
        {
            Tick = tick;
            _ids = ids;
            _genomes = genomes;
        }

        public long Tick { get; }
        public int Count => _ids.Length;

        public static PopulationGenomeSnapshot Capture(long tick, CreatureStore creatures)
        {
            var ids = new CreatureId[creatures.Count];
            var genomes = new Genome[creatures.Count];
            for (int index = 0; index < creatures.Count; index++)
            {
                ids[index] = creatures.GetIdAt(index);
                genomes[index] = creatures.GetGenomeAt(index);
            }
            return new PopulationGenomeSnapshot(tick, ids, genomes);
        }

        public CreatureId GetIdAt(int index) => _ids[index];
        public Genome GetGenomeAt(int index) => _genomes[index];
    }
}

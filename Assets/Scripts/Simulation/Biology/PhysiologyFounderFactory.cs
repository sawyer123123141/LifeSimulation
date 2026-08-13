using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Biology
{
    public static class PhysiologyFounderFactory
    {
        public static Genome Create(int worldSeed, long founderOrdinal)
        {
            return new Genome(
                CoreTrait(worldSeed, founderOrdinal, 0),
                CoreTrait(worldSeed, founderOrdinal, 1),
                CoreTrait(worldSeed, founderOrdinal, 2),
                CoreTrait(worldSeed, founderOrdinal, 3),
                CoreTrait(worldSeed, founderOrdinal, 4),
                CoreTrait(worldSeed, founderOrdinal, 5),
                memoryCapacity: Trait(worldSeed, founderOrdinal, 6),
                memoryRetention: Trait(worldSeed, founderOrdinal, 7),
                learningRate: Trait(worldSeed, founderOrdinal, 8),
                exploration: Trait(worldSeed, founderOrdinal, 9),
                temperatureTolerance: Trait(worldSeed, founderOrdinal, 10),
                fertilityInvestment: Trait(worldSeed, founderOrdinal, 11),
                lifespanTendency: Trait(worldSeed, founderOrdinal, 12));
        }

        private static float Trait(int worldSeed, long founderOrdinal, int traitIndex)
        {
            return DeterministicRandom.Float01(
                worldSeed,
                RandomDomain.FounderGenome,
                founderOrdinal,
                traitIndex,
                2,
                24 + traitIndex);
        }

        private static float CoreTrait(int worldSeed, long founderOrdinal, int traitIndex)
        {
            return 0.5f + (DeterministicRandom.Gaussian(
                worldSeed,
                RandomDomain.FounderGenome,
                founderOrdinal,
                traitIndex,
                2,
                traitIndex * 2) * 0.12f);
        }
    }
}

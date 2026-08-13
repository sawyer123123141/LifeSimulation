using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Biology
{
    public static class PredationFounderFactory
    {
        public static Genome Create(int worldSeed, long founderOrdinal)
        {
            Genome baseline = Genome.Neutral;
            return new Genome(
                baseline.BodySize,
                baseline.MovementSpeed,
                baseline.MetabolicPace,
                baseline.VisionRange,
                baseline.WaterEfficiency,
                baseline.FoodEfficiency,
                Trait(worldSeed, founderOrdinal, 0),
                Trait(worldSeed, founderOrdinal, 1),
                Trait(worldSeed, founderOrdinal, 2),
                Trait(worldSeed, founderOrdinal, 3),
                Trait(worldSeed, founderOrdinal, 4),
                Trait(worldSeed, founderOrdinal, 5),
                Trait(worldSeed, founderOrdinal, 6),
                Trait(worldSeed, founderOrdinal, 7),
                Trait(worldSeed, founderOrdinal, 8),
                Trait(worldSeed, founderOrdinal, 9),
                Trait(worldSeed, founderOrdinal, 10));
        }

        private static float Trait(int worldSeed, long founderOrdinal, int traitIndex)
        {
            return DeterministicRandom.Float01(
                worldSeed,
                RandomDomain.FounderGenome,
                founderOrdinal,
                traitIndex,
                1,
                12 + traitIndex);
        }
    }
}

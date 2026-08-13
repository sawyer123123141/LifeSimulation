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
                attack: Trait(worldSeed, founderOrdinal, 0),
                defense: Trait(worldSeed, founderOrdinal, 1),
                maneuverability: Trait(worldSeed, founderOrdinal, 2),
                fear: Trait(worldSeed, founderOrdinal, 3),
                aggression: Trait(worldSeed, founderOrdinal, 4),
                dietSpecialization: Trait(worldSeed, founderOrdinal, 5));
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

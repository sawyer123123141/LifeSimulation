using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Biology
{
    public static class GenomeInheritance
    {
        public static Genome CreateChild(
            Genome firstParent,
            Genome secondParent,
            int worldSeed,
            long birthOrdinal,
            float mutationStandardDeviation)
        {
            if (mutationStandardDeviation < 0f
                || float.IsNaN(mutationStandardDeviation)
                || float.IsInfinity(mutationStandardDeviation))
            {
                throw new ArgumentOutOfRangeException(nameof(mutationStandardDeviation));
            }

            return new Genome(
                InheritTrait(firstParent.BodySize, secondParent.BodySize, worldSeed, birthOrdinal, 0, mutationStandardDeviation),
                InheritTrait(firstParent.MovementSpeed, secondParent.MovementSpeed, worldSeed, birthOrdinal, 1, mutationStandardDeviation),
                InheritTrait(firstParent.MetabolicPace, secondParent.MetabolicPace, worldSeed, birthOrdinal, 2, mutationStandardDeviation),
                InheritTrait(firstParent.VisionRange, secondParent.VisionRange, worldSeed, birthOrdinal, 3, mutationStandardDeviation),
                InheritTrait(firstParent.WaterEfficiency, secondParent.WaterEfficiency, worldSeed, birthOrdinal, 4, mutationStandardDeviation),
                InheritTrait(firstParent.FoodEfficiency, secondParent.FoodEfficiency, worldSeed, birthOrdinal, 5, mutationStandardDeviation),
                InheritTrait(firstParent.Attack, secondParent.Attack, worldSeed, birthOrdinal, 6, mutationStandardDeviation),
                InheritTrait(firstParent.Defense, secondParent.Defense, worldSeed, birthOrdinal, 7, mutationStandardDeviation),
                InheritTrait(firstParent.Maneuverability, secondParent.Maneuverability, worldSeed, birthOrdinal, 8, mutationStandardDeviation),
                InheritTrait(firstParent.Fear, secondParent.Fear, worldSeed, birthOrdinal, 9, mutationStandardDeviation),
                InheritTrait(firstParent.Aggression, secondParent.Aggression, worldSeed, birthOrdinal, 10, mutationStandardDeviation),
                InheritTrait(firstParent.DietSpecialization, secondParent.DietSpecialization, worldSeed, birthOrdinal, 11, mutationStandardDeviation));
        }

        private static float InheritTrait(
            float firstParentTrait,
            float secondParentTrait,
            int worldSeed,
            long birthOrdinal,
            int traitIndex,
            float mutationStandardDeviation)
        {
            float crossover = DeterministicRandom.Float01(
                worldSeed,
                RandomDomain.Crossover,
                birthOrdinal,
                traitIndex,
                0,
                0);
            float inheritedTrait = crossover < 0.5f ? firstParentTrait : secondParentTrait;
            float mutation = DeterministicRandom.Gaussian(
                worldSeed,
                RandomDomain.Mutation,
                birthOrdinal,
                traitIndex,
                0,
                0) * mutationStandardDeviation;
            return inheritedTrait + mutation;
        }
    }
}

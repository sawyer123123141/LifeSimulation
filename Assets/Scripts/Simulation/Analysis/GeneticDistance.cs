using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Analysis-only normalized genetic distance. Simulation systems must not read this result.</summary>
    public static class GeneticDistance
    {
        public static float Between(Genome first, Genome second)
        {
            return RootMeanSquare(first.ToTraits(), second.ToTraits());
        }

        public static float Between(PlantGenome first, PlantGenome second)
        {
            return RootMeanSquare(first.ToTraits(), second.ToTraits());
        }

        /// <summary>
        /// Distance between two genomes already flattened into one shared trait buffer. Allocates
        /// nothing, which is what lets an O(n^2) clustering pass allocate O(n).
        /// </summary>
        public static float Between(float[] traits, int firstOffset, int secondOffset, int traitCount)
        {
            if (traits == null) throw new ArgumentNullException(nameof(traits));
            if (traitCount <= 0) throw new ArgumentOutOfRangeException(nameof(traitCount));
            if (firstOffset < 0 || firstOffset + traitCount > traits.Length) throw new ArgumentOutOfRangeException(nameof(firstOffset));
            if (secondOffset < 0 || secondOffset + traitCount > traits.Length) throw new ArgumentOutOfRangeException(nameof(secondOffset));

            float sumSquared = 0f;
            for (int index = 0; index < traitCount; index++)
            {
                float difference = traits[firstOffset + index] - traits[secondOffset + index];
                sumSquared += difference * difference;
            }

            return (float)Math.Sqrt(sumSquared / traitCount);
        }

        private static float RootMeanSquare(float[] first, float[] second)
        {
            float sumSquared = 0f;
            for (int index = 0; index < first.Length; index++)
            {
                float difference = first[index] - second[index];
                sumSquared += difference * difference;
            }

            return (float)Math.Sqrt(sumSquared / first.Length);
        }
    }
}

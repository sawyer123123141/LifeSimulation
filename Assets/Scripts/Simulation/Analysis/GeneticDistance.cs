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

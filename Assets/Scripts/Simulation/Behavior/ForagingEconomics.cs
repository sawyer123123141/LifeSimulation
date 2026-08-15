using System;
using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Simulation.Behavior
{
    public static class ForagingEconomics
    {
        public static float ExpectedGain(float remainingAmount, Phenotype phenotype, float nutritionMultiplier, float handlingSeconds)
        {
            if (remainingAmount < 0f || float.IsNaN(remainingAmount) || float.IsInfinity(remainingAmount))
            {
                throw new ArgumentOutOfRangeException(nameof(remainingAmount));
            }

            if (handlingSeconds < 0f || float.IsNaN(handlingSeconds) || float.IsInfinity(handlingSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(handlingSeconds));
            }

            float amountEatable = phenotype.IngestionRate * handlingSeconds;
            float amountTaken = Math.Min(remainingAmount, amountEatable);

            return amountTaken * phenotype.PlantFoodYieldMultiplier * nutritionMultiplier;
        }
    }
}

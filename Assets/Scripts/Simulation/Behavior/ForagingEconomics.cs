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

        public static float TravelEnergy(float distance, Phenotype phenotype)
        {
            if (distance < 0f || float.IsNaN(distance) || float.IsInfinity(distance))
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            return distance * phenotype.BodyMass * 0.5f;
        }
    }
}

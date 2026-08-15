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

        public static float PatchScore(
            float urgency,
            float remainingAmount,
            float distance,
            Phenotype phenotype,
            float nutritionMultiplier,
            float handlingSeconds,
            float referenceGain)
        {
            if (urgency < 0f || urgency > 1f || float.IsNaN(urgency) || float.IsInfinity(urgency))
            {
                throw new ArgumentOutOfRangeException(nameof(urgency));
            }

            if (float.IsNaN(nutritionMultiplier) || float.IsInfinity(nutritionMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(nutritionMultiplier));
            }

            if (referenceGain <= 0f || float.IsNaN(referenceGain) || float.IsInfinity(referenceGain))
            {
                throw new ArgumentOutOfRangeException(nameof(referenceGain));
            }

            float expectedGain = ExpectedGain(remainingAmount, phenotype, nutritionMultiplier, handlingSeconds);
            float travelEnergy = TravelEnergy(distance, phenotype);
            float netGain = expectedGain - travelEnergy;

            return Clamp01(urgency * netGain / referenceGain);
        }

        public static float CommitmentBonus(
            float secondsInCurrentAction,
            float persistence,
            float commitmentStrength,
            float commitmentHalfLifeSeconds)
        {
            if (secondsInCurrentAction < 0f || float.IsNaN(secondsInCurrentAction) || float.IsInfinity(secondsInCurrentAction))
            {
                throw new ArgumentOutOfRangeException(nameof(secondsInCurrentAction));
            }

            if (persistence < 0f || float.IsNaN(persistence) || float.IsInfinity(persistence))
            {
                throw new ArgumentOutOfRangeException(nameof(persistence));
            }

            if (commitmentStrength < 0f || float.IsNaN(commitmentStrength) || float.IsInfinity(commitmentStrength))
            {
                throw new ArgumentOutOfRangeException(nameof(commitmentStrength));
            }

            if (commitmentHalfLifeSeconds <= 0f || float.IsNaN(commitmentHalfLifeSeconds) || float.IsInfinity(commitmentHalfLifeSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(commitmentHalfLifeSeconds));
            }

            float decay = (float)Math.Pow(0.5, secondsInCurrentAction / commitmentHalfLifeSeconds);

            return commitmentStrength * persistence * decay;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}

using System;

namespace LifeSimulation.Simulation.Biology
{
    public struct CreatureNeeds
    {
        public float Energy;
        public float Hydration;
        public float Rest;
        public float Health;
        public float Age;

        public static CreatureNeeds Full(Phenotype phenotype)
        {
            return new CreatureNeeds
            {
                Energy = phenotype.EnergyCapacity,
                Hydration = phenotype.HydrationCapacity,
                Rest = 100f,
                Health = phenotype.HealthCapacity,
                Age = 0f,
            };
        }
    }

    public static class NeedsSystem
    {
        private const float FoodEnergyPerUnit = 20f;
        private const float WaterHydrationPerUnit = 20f;
        public const float RestCapacity = 100f;
        private const float RestRecoveryPerSecond = 5f;
        private const float RestExhaustionHealthCostPerSecond = 3f;

        public static void Tick(ref CreatureNeeds needs, Phenotype phenotype, float deltaTime, float movementDistance, bool restBehaviorEnabled = false, bool isResting = false)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (movementDistance < 0f || float.IsNaN(movementDistance) || float.IsInfinity(movementDistance))
            {
                throw new ArgumentOutOfRangeException(nameof(movementDistance));
            }

            float energyCost = (phenotype.BasalEnergyCostMultiplier * deltaTime)
                + (movementDistance * phenotype.BodyMass * 0.5f);
            float hydrationCost = phenotype.BodyMass
                * phenotype.DigestionRate
                * phenotype.WaterLossMultiplier
                * 0.75f
                * deltaTime;

            needs.Energy = Math.Max(0f, needs.Energy - energyCost);
            needs.Hydration = Math.Max(0f, needs.Hydration - hydrationCost);
            if (restBehaviorEnabled && isResting)
            {
                needs.Rest = Math.Min(RestCapacity, needs.Rest + (RestRecoveryPerSecond * deltaTime));
            }
            else
            {
                needs.Rest = Math.Max(0f, needs.Rest - (0.1f * phenotype.CognitionRestCostMultiplier * deltaTime));
            }
            needs.Age += deltaTime;

            if (needs.Energy <= 0f)
            {
                needs.Health = Math.Max(0f, needs.Health - (4f * deltaTime));
            }

            if (needs.Hydration <= 0f)
            {
                needs.Health = Math.Max(0f, needs.Health - (5f * deltaTime));
            }

            if (restBehaviorEnabled && needs.Rest <= 0f)
            {
                needs.Health = Math.Max(0f, needs.Health - (RestExhaustionHealthCostPerSecond * deltaTime));
            }
        }

        public static void ApplyTemperatureStress(ref CreatureNeeds needs, Phenotype phenotype, float temperature, float deltaTime)
        {
            float deviation = Math.Max(0f, Math.Abs(temperature - 20f) - phenotype.TemperatureTolerance);
            needs.Health = Math.Max(0f, needs.Health - (deviation * 0.35f * deltaTime));
        }

        public static void ConsumeFood(ref CreatureNeeds needs, Phenotype phenotype, float amount)
        {
            ValidateResourceAmount(amount);
            needs.Energy = Math.Min(phenotype.EnergyCapacity, needs.Energy + (amount * FoodEnergyPerUnit * phenotype.FoodYield));
        }

        public static void DrinkWater(ref CreatureNeeds needs, Phenotype phenotype, float amount)
        {
            ValidateResourceAmount(amount);
            needs.Hydration = Math.Min(phenotype.HydrationCapacity, needs.Hydration + (amount * WaterHydrationPerUnit));
        }

        private static void ValidateResourceAmount(float amount)
        {
            if (amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
        }
    }
}

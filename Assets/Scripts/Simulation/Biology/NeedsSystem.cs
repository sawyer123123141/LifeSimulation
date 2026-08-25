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

        /// <summary>Fraction of health capacity restored per second while well fed and watered.</summary>
        public const float HealthRecoveryFractionPerSecond = 0.005f;

        /// <summary>How full a creature must be, on energy and hydration, to heal at all.</summary>
        public const float HealthRecoveryNeedFraction = 0.5f;

        /// <summary>
        /// Healing, which did not exist.
        ///
        /// <para><b>The defect this closes.</b> Health was written once at birth and subtracted from
        /// in five places, with no addition anywhere in the simulation - a one-way ratchet. And health
        /// is one of the three conditions on the mate-seeking gate, so a creature that lost a fifth of
        /// its health was not injured, it was <b>permanently sterile for the rest of its life</b>.
        /// See <c>docs/experiments/p6-nothing-starves-2026-08-24.md</c>.</para>
        ///
        /// <para><b>Measured before being called a crisis:</b> mean health sits at 0.9861 and only
        /// 0.8% of the living are under the gate, because the population evolves thermal tolerance
        /// until nothing damages it. So this is a latent trap rather than an active one - and it is
        /// the likeliest reason thermal tolerance is the fiercest selection in the model, since when
        /// damage is permanent the only winning move is never to be damaged.</para>
        ///
        /// <para><b>Deliberately conditional.</b> Healing requires being over half full on energy and
        /// hydration, so it is paid for rather than free, and the rate is roughly half the peak
        /// thermal damage rate - a creature standing in a hot band still loses ground, and only
        /// recovers once it leaves.</para>
        /// </summary>
        /// <param name="metabolicScale">
        /// <c>0.7 + 0.8 * MetabolicPace</c> when metabolic healing is on, otherwise 1.
        ///
        /// <para>This is the honest benefit for a gene that had none. <c>MetabolicPace</c> raises the
        /// energy and water drains by that exact factor and buys nothing, and the ingestion attempt
        /// failed because ingestion is a <b>shared</b> channel - every competitor eating faster
        /// cancels it. Healing is private: nobody else can consume it. It also feeds the
        /// mate-seeking gate, which is where fitness is actually decided.</para>
        /// </param>
        public static void RecoverHealth(ref CreatureNeeds needs, Phenotype phenotype, float deltaTime, float metabolicScale = 1f)
        {
            if (needs.Energy < phenotype.EnergyCapacity * HealthRecoveryNeedFraction) return;
            if (needs.Hydration < phenotype.HydrationCapacity * HealthRecoveryNeedFraction) return;

            needs.Health = Math.Min(
                phenotype.HealthCapacity,
                needs.Health + (phenotype.HealthCapacity * HealthRecoveryFractionPerSecond * metabolicScale * deltaTime));
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

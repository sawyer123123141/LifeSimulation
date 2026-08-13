using System;

namespace LifeSimulation.Simulation.Biology
{
    public readonly struct Genome
    {
        public Genome(
            float bodySize,
            float movementSpeed,
            float metabolicPace,
            float visionRange,
            float waterEfficiency,
            float foodEfficiency)
        {
            BodySize = Clamp01(bodySize);
            MovementSpeed = Clamp01(movementSpeed);
            MetabolicPace = Clamp01(metabolicPace);
            VisionRange = Clamp01(visionRange);
            WaterEfficiency = Clamp01(waterEfficiency);
            FoodEfficiency = Clamp01(foodEfficiency);
        }

        public static Genome Neutral => new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);

        public float BodySize { get; }
        public float MovementSpeed { get; }
        public float MetabolicPace { get; }
        public float VisionRange { get; }
        public float WaterEfficiency { get; }
        public float FoodEfficiency { get; }

        public Genome WithBodySize(float value)
        {
            return new Genome(value, MovementSpeed, MetabolicPace, VisionRange, WaterEfficiency, FoodEfficiency);
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }

    public readonly struct Phenotype
    {
        private Phenotype(
            float bodyMass,
            float energyCapacity,
            float hydrationCapacity,
            float healthCapacity,
            float maximumSpeed,
            float visionRange,
            float foodYield,
            float ingestionRate,
            float digestionRate,
            float waterLossMultiplier,
            float basalEnergyCostMultiplier)
        {
            BodyMass = bodyMass;
            EnergyCapacity = energyCapacity;
            HydrationCapacity = hydrationCapacity;
            HealthCapacity = healthCapacity;
            MaximumSpeed = maximumSpeed;
            VisionRange = visionRange;
            FoodYield = foodYield;
            IngestionRate = ingestionRate;
            DigestionRate = digestionRate;
            WaterLossMultiplier = waterLossMultiplier;
            BasalEnergyCostMultiplier = basalEnergyCostMultiplier;
        }

        public float BodyMass { get; }
        public float EnergyCapacity { get; }
        public float HydrationCapacity { get; }
        public float HealthCapacity { get; }
        public float MaximumSpeed { get; }
        public float VisionRange { get; }
        public float FoodYield { get; }
        public float IngestionRate { get; }
        public float DigestionRate { get; }
        public float WaterLossMultiplier { get; }
        public float BasalEnergyCostMultiplier { get; }

        public static Phenotype FromGenome(Genome genome)
        {
            float bodyMass = 0.6f * (float)Math.Pow(4d, genome.BodySize);
            float maintenance = 1f
                + (0.08f * genome.MovementSpeed)
                + (0.05f * genome.VisionRange)
                + (0.07f * genome.WaterEfficiency)
                + (0.04f * genome.FoodEfficiency);

            return new Phenotype(
                bodyMass,
                bodyMass * 100f,
                (float)Math.Pow(bodyMass, 0.8d) * 50f,
                (float)Math.Pow(bodyMass, 0.67d) * 100f,
                1f + (3f * genome.MovementSpeed),
                4f + (12f * genome.VisionRange),
                0.75f + (0.5f * genome.FoodEfficiency),
                1.25f - (0.5f * genome.FoodEfficiency),
                0.7f + (0.8f * genome.MetabolicPace),
                1f - (0.55f * genome.WaterEfficiency),
                (float)Math.Pow(bodyMass, 0.75d) * (0.7f + (0.8f * genome.MetabolicPace)) * maintenance);
        }
    }
}

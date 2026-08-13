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
            float foodEfficiency,
            float attack = 0f,
            float defense = 0f,
            float maneuverability = 0f,
            float fear = 0f,
            float aggression = 0f,
            float dietSpecialization = 0f,
            float memoryCapacity = 0f,
            float memoryRetention = 0f,
            float learningRate = 0f,
            float exploration = 0f,
            float temperatureTolerance = 0f)
        {
            BodySize = Clamp01(bodySize);
            MovementSpeed = Clamp01(movementSpeed);
            MetabolicPace = Clamp01(metabolicPace);
            VisionRange = Clamp01(visionRange);
            WaterEfficiency = Clamp01(waterEfficiency);
            FoodEfficiency = Clamp01(foodEfficiency);
            Attack = Clamp01(attack);
            Defense = Clamp01(defense);
            Maneuverability = Clamp01(maneuverability);
            Fear = Clamp01(fear);
            Aggression = Clamp01(aggression);
            DietSpecialization = Clamp01(dietSpecialization);
            MemoryCapacity = Clamp01(memoryCapacity);
            MemoryRetention = Clamp01(memoryRetention);
            LearningRate = Clamp01(learningRate);
            Exploration = Clamp01(exploration);
            TemperatureTolerance = Clamp01(temperatureTolerance);
        }

        public static Genome Neutral => new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);

        public float BodySize { get; }
        public float MovementSpeed { get; }
        public float MetabolicPace { get; }
        public float VisionRange { get; }
        public float WaterEfficiency { get; }
        public float FoodEfficiency { get; }
        public float Attack { get; }
        public float Defense { get; }
        public float Maneuverability { get; }
        public float Fear { get; }
        public float Aggression { get; }
        public float DietSpecialization { get; }
        public float MemoryCapacity { get; }
        public float MemoryRetention { get; }
        public float LearningRate { get; }
        public float Exploration { get; }
        public float TemperatureTolerance { get; }

        public Genome WithBodySize(float value)
        {
            return new Genome(
                value,
                MovementSpeed,
                MetabolicPace,
                VisionRange,
                WaterEfficiency,
                FoodEfficiency,
                Attack,
                Defense,
                Maneuverability,
                Fear,
                Aggression,
                DietSpecialization,
                MemoryCapacity,
                MemoryRetention,
                LearningRate,
                Exploration,
                TemperatureTolerance);
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
            float basalEnergyCostMultiplier,
            float attackPower,
            float defense,
            float maneuverability,
            float fearResponse,
            float aggression,
            float plantFoodYieldMultiplier,
            float meatYieldMultiplier,
            float memoryConfidenceDecayPerSecond,
            float cognitionRestCostMultiplier,
            float temperatureTolerance)
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
            AttackPower = attackPower;
            Defense = defense;
            Maneuverability = maneuverability;
            FearResponse = fearResponse;
            Aggression = aggression;
            PlantFoodYieldMultiplier = plantFoodYieldMultiplier;
            MeatYieldMultiplier = meatYieldMultiplier;
            MemoryConfidenceDecayPerSecond = memoryConfidenceDecayPerSecond;
            CognitionRestCostMultiplier = cognitionRestCostMultiplier;
            TemperatureTolerance = temperatureTolerance;
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
        public float AttackPower { get; }
        public float Defense { get; }
        public float Maneuverability { get; }
        public float FearResponse { get; }
        public float Aggression { get; }
        public float PlantFoodYieldMultiplier { get; }
        public float MeatYieldMultiplier { get; }
        public float MemoryConfidenceDecayPerSecond { get; }
        public float CognitionRestCostMultiplier { get; }
        public float TemperatureTolerance { get; }

        public static Phenotype FromGenome(Genome genome)
        {
            float bodyMass = 0.6f * (float)Math.Pow(4d, genome.BodySize);
            float maintenance = 1f
                + (0.08f * genome.MovementSpeed)
                + (0.05f * genome.VisionRange)
                + (0.07f * genome.WaterEfficiency)
                + (0.04f * genome.FoodEfficiency)
                + (0.10f * genome.Attack)
                + (0.10f * genome.Defense)
                + (0.08f * genome.Maneuverability)
                + (0.03f * genome.Fear)
                + (0.04f * genome.Aggression)
                + (0.04f * genome.DietSpecialization)
                + (0.08f * genome.MemoryCapacity)
                + (0.05f * genome.MemoryRetention)
                + (0.04f * genome.LearningRate)
                + (0.02f * genome.Exploration)
                + (0.06f * genome.TemperatureTolerance);

            return new Phenotype(
                bodyMass,
                bodyMass * 100f,
                (float)Math.Pow(bodyMass, 0.8d) * 50f,
                (float)Math.Pow(bodyMass, 0.67d) * 100f,
                1f + (3f * genome.MovementSpeed),
                4f + (12f * genome.VisionRange),
                0.75f + (0.65f * genome.FoodEfficiency),
                1.25f - (0.3f * genome.FoodEfficiency),
                0.7f + (0.8f * genome.MetabolicPace),
                1f - (0.55f * genome.WaterEfficiency),
                (float)Math.Pow(bodyMass, 0.75d) * (0.7f + (0.8f * genome.MetabolicPace)) * maintenance,
                genome.Attack * (0.75f + (0.5f * bodyMass)),
                genome.Defense * (0.75f + (0.5f * bodyMass)),
                1f + (2f * genome.Maneuverability),
                genome.Fear,
                genome.Aggression,
                1f - (0.3f * genome.DietSpecialization),
                0.5f + genome.DietSpecialization,
                0.12f - (0.10f * genome.MemoryRetention),
                1f + (0.6f * genome.MemoryCapacity) + (0.25f * genome.LearningRate),
                2f + (8f * genome.TemperatureTolerance));
        }
    }
}

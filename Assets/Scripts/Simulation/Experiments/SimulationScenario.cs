using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct ResourceDefinition
    {
        public ResourceDefinition(
            ResourceKind kind,
            SimVector2 position,
            float interactionRadius,
            float initialAmount,
            float capacity,
            float regenerationPerSecond,
            bool isActive = true,
            float nutritionMultiplier = 1f)
        {
            Kind = kind;
            Position = position;
            InteractionRadius = interactionRadius;
            InitialAmount = initialAmount;
            Capacity = capacity;
            RegenerationPerSecond = regenerationPerSecond;
            IsActive = isActive;
            NutritionMultiplier = nutritionMultiplier;
        }

        public ResourceKind Kind { get; }
        public SimVector2 Position { get; }
        public float InteractionRadius { get; }
        public float InitialAmount { get; }
        public float Capacity { get; }
        public float RegenerationPerSecond { get; }
        public bool IsActive { get; }
        public float NutritionMultiplier { get; }

        public ResourceId AddTo(ResourceStore resources, float populationScale)
        {
            if (populationScale <= 0f || float.IsNaN(populationScale) || float.IsInfinity(populationScale))
            {
                throw new ArgumentOutOfRangeException(nameof(populationScale));
            }

            ResourceId id = resources.Add(
                Kind,
                Position,
                InteractionRadius,
                InitialAmount * populationScale,
                Capacity * populationScale,
                RegenerationPerSecond * populationScale,
                NutritionMultiplier);
            if (!IsActive)
            {
                resources.SetActive(id, false);
            }

            return id;
        }
    }

    public sealed class SimulationScenario
    {
        private readonly ResourceDefinition[] _resources;
        private readonly SimVector2? _founderPlacement;

        public SimulationScenario(string id, ResourceDefinition[] resources, SimVector2? founderPlacement = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Scenario identifier is required.", nameof(id));
            }

            Id = id;
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _founderPlacement = founderPlacement;
        }

        public string Id { get; }
        public int ResourceCount => _resources.Length;

        public void ApplyTo(SimulationWorld world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.Resources.Count != 0)
            {
                throw new InvalidOperationException("A scenario can only be applied to a world with no existing resources.");
            }

            if (_founderPlacement.HasValue)
            {
                SimVector2 placement = _founderPlacement.Value;
                for (int index = 0; index < world.CreatureCount; index++)
                {
                    world.SetCreaturePosition(world.GetCreatureIdAt(index), placement);
                }
            }

            float populationScale = Math.Max(1f, world.Config.InitialPopulation / 4f);
            for (int index = 0; index < _resources.Length; index++)
            {
                ResourceDefinition definition = _resources[index];
                ResourceId resourceId = definition.AddTo(world.Resources, populationScale);
                if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food)
                {
                    float capacity = definition.Capacity * populationScale;
                    float biomass = definition.InitialAmount * populationScale;
                    float growthRate = capacity <= 0f ? 0f : definition.RegenerationPerSecond / capacity;
                    world.AddPlantPatch(resourceId, definition.Position, biomass, capacity, growthRate, waterDemand: 0f, nutrition: definition.NutritionMultiplier, defense: 0f);
                }
            }
        }
    }

    public static class Prototype1Scenarios
    {
        public static SimulationScenario Baseline { get; } = new SimulationScenario(
            "baseline",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.75f, waterAmount: 12f, waterRegeneration: 0.75f));

        public static SimulationScenario Drought { get; } = new SimulationScenario(
            "drought",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.75f, waterAmount: 12f, waterRegeneration: 0.25f));

        public static SimulationScenario FoodScarcity { get; } = new SimulationScenario(
            "food-scarcity",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.25f, waterAmount: 12f, waterRegeneration: 0.75f));

        private static ResourceDefinition[] CreateResources(
            float foodAmount,
            float foodRegeneration,
            float waterAmount,
            float waterRegeneration)
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, waterAmount, waterAmount, waterRegeneration),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, waterAmount, waterAmount, waterRegeneration),
            };
        }
    }

    public static class Prototype3Scenarios
    {
        public static SimulationScenario PlantNutritionPoor { get; } = CreateNutritionScenario("p3-plant-nutrition-poor", 0.5f);
        public static SimulationScenario PlantNutritionRich { get; } = CreateNutritionScenario("p3-plant-nutrition-rich", 1.5f);

        private static SimulationScenario CreateNutritionScenario(string id, float plantNutrition)
        {
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: plantNutrition),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: plantNutrition),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, 12f, 12f, 0.75f),
            });
        }
    }

    public static class Prototype4Scenarios
    {
        public static SimulationScenario PlantBackedBaseline { get; } = new SimulationScenario(
            "p4-plant-backed-baseline",
            new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, 12f, 12f, 0.75f),
            });
    }

    public static class DecisionPolicyScenarios
    {
        public static SimulationScenario NearAdequateFood { get; } = CreateTravelScenario("policy-near-adequate", nearFoodAmount: 10f, farFoodAmount: 3f);
        public static SimulationScenario FarRichFood { get; } = CreateTravelScenario("policy-far-rich", nearFoodAmount: 3f, farFoodAmount: 10f);

        private static SimulationScenario CreateTravelScenario(string id, float nearFoodAmount, float farFoodAmount)
        {
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-4f, 0f), 1.5f, nearFoodAmount, nearFoodAmount, 0.35f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, 0f), 1.5f, farFoodAmount, farFoodAmount, 0.75f, nutritionMultiplier: 1.5f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-4f, -5f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(18f, -5f), 1.5f, 12f, 12f, 0.75f),
            }, founderPlacement: new SimVector2(0f, 0f));
        }
    }
}

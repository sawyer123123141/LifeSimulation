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
            bool isActive = true)
        {
            Kind = kind;
            Position = position;
            InteractionRadius = interactionRadius;
            InitialAmount = initialAmount;
            Capacity = capacity;
            RegenerationPerSecond = regenerationPerSecond;
            IsActive = isActive;
        }

        public ResourceKind Kind { get; }
        public SimVector2 Position { get; }
        public float InteractionRadius { get; }
        public float InitialAmount { get; }
        public float Capacity { get; }
        public float RegenerationPerSecond { get; }
        public bool IsActive { get; }

        public void AddTo(ResourceStore resources, float populationScale)
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
                RegenerationPerSecond * populationScale);
            if (!IsActive)
            {
                resources.SetActive(id, false);
            }
        }
    }

    public sealed class SimulationScenario
    {
        private readonly ResourceDefinition[] _resources;

        public SimulationScenario(string id, ResourceDefinition[] resources)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Scenario identifier is required.", nameof(id));
            }

            Id = id;
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
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

            float populationScale = Math.Max(1f, world.Config.InitialPopulation / 4f);
            for (int index = 0; index < _resources.Length; index++)
            {
                _resources[index].AddTo(world.Resources, populationScale);
            }
        }
    }

    public static class Prototype1Scenarios
    {
        public static SimulationScenario Baseline { get; } = new SimulationScenario(
            "baseline",
            CreateResources(foodAmount: 12f, foodRegeneration: 1f, waterAmount: 12f, waterRegeneration: 1f));

        public static SimulationScenario Drought { get; } = new SimulationScenario(
            "drought",
            CreateResources(foodAmount: 12f, foodRegeneration: 1f, waterAmount: 12f, waterRegeneration: 0.25f));

        public static SimulationScenario FoodScarcity { get; } = new SimulationScenario(
            "food-scarcity",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.25f, waterAmount: 12f, waterRegeneration: 1f));

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
}

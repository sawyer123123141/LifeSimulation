using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public readonly struct PlantPatchId
    {
        public PlantPatchId(int value) { Value = value; }
        public int Value { get; }
    }

    public readonly struct PlantPatchState
    {
        public PlantPatchState(PlantPatchId id, ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float waterDemand, float nutrition, float defense)
        {
            Id = id;
            FoodResourceId = foodResourceId;
            Position = position;
            Biomass = biomass;
            Capacity = capacity;
            GrowthRate = growthRate;
            WaterDemand = waterDemand;
            Nutrition = nutrition;
            Defense = defense;
        }

        public PlantPatchId Id { get; }
        public ResourceId FoodResourceId { get; }
        public SimVector2 Position { get; }
        public float Biomass { get; }
        public float Capacity { get; }
        public float GrowthRate { get; }
        public float WaterDemand { get; }
        public float Nutrition { get; }
        public float Defense { get; }
        public bool IsDormant => Biomass <= 0f;
    }
}

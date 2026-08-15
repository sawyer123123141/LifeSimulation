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
        public PlantPatchState(PlantPatchId id, ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense, PlantGenome genome, PlantLineage lineage, float age, float seedReserve, float reproductionCooldownRemaining)
        {
            Id = id;
            FoodResourceId = foodResourceId;
            Position = position;
            Biomass = biomass;
            Capacity = capacity;
            GrowthRate = growthRate;
            Nutrition = nutrition;
            Defense = defense;
            Genome = genome;
            Lineage = lineage;
            Age = age;
            SeedReserve = seedReserve;
            ReproductionCooldownRemaining = reproductionCooldownRemaining;
        }

        public PlantPatchId Id { get; }
        public ResourceId FoodResourceId { get; }
        public SimVector2 Position { get; }
        public float Biomass { get; }
        public float Capacity { get; }
        public float GrowthRate { get; }
        public float Nutrition { get; }
        public float Defense { get; }
        public PlantGenome Genome { get; }
        public PlantLineage Lineage { get; }
        public float Age { get; }
        public float SeedReserve { get; }
        public float ReproductionCooldownRemaining { get; }
        public bool IsDormant => Biomass <= 0f;
    }

    public readonly struct PlantLineage
    {
        public PlantLineage(PlantPatchId lineageId, PlantPatchId parentId, int generation)
        {
            LineageId = lineageId;
            ParentId = parentId;
            Generation = generation;
        }

        public PlantPatchId LineageId { get; }
        public PlantPatchId ParentId { get; }
        public int Generation { get; }
    }
}

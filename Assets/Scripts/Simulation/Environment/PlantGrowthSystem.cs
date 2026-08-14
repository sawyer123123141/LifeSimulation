using System;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantGrowthSystem
    {
        public static float Step(PlantPatchStore patches, EnvironmentField field, float deltaTime)
        {
            float addedBiomass = 0f;
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                if (patch.Biomass <= 0f || patch.Biomass >= patch.Capacity) continue;
                EnvironmentSample sample = field.Sample(patch.Position);
                float limit = Math.Max(0f, Math.Min(sample.Moisture, Math.Min(sample.Fertility, sample.Temperature)));
                float growth = patch.GrowthRate * patch.Biomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
                float next = Math.Min(patch.Capacity, patch.Biomass + growth);
                patches.SetBiomass(index, next);
                addedBiomass += next - patch.Biomass;
            }

            return addedBiomass;
        }

        public static void ProjectFoodResources(PlantPatchStore patches, ResourceStore resources)
        {
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                resources.SetFoodProjection(patch.FoodResourceId, patch.Biomass, patch.Nutrition);
            }
        }
    }
}

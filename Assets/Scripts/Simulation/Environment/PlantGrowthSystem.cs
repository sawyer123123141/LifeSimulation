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
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                float moistureAdaptation = sample.Moisture <= 0f
                    ? 0f
                    : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * patch.Genome.WaterEfficiency + .3f * patch.Genome.MoistureTolerance)));
                float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(sample.Fertility, sample.Temperature)));
                float growth = patch.GrowthRate * phenotype.GrowthRateMultiplier * patch.Biomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
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
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                resources.SetFoodProjection(patch.FoodResourceId, patch.Biomass, patch.Nutrition * phenotype.NutritionMultiplier);
            }
        }
    }
}

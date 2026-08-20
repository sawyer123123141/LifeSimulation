using System;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantGrowthSystem
    {
        private const float SproutFloorFraction = 0.01f;

        public static float Step(
            PlantPatchStore patches,
            EnvironmentField field,
            float deltaTime,
            bool temperatureAdaptationEnabled = false,
            bool fertilityAdaptationEnabled = false)
        {
            float addedBiomass = 0f;
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                if (patch.Biomass >= patch.Capacity) continue;
                EnvironmentSample sample = field.Sample(patch.Position);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome, fertilityAdaptationEnabled);
                float moistureAdaptation = sample.Moisture <= 0f
                    ? 0f
                    : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * patch.Genome.WaterEfficiency + .3f * patch.Genome.MoistureTolerance)));

                // Temperature mirrors the moisture pattern above when enabled. Without it,
                // sample.Temperature is a raw limit that no gene can improve against, so
                // TemperatureTolerance pays a -.10f growth penalty in PlantPhenotype and can never
                // earn it back - a pure cost under every environment
                // (docs/experiments/plant-gene-liveness-2026-08-18.md).
                //
                // Moisture splits its adaptation between two genes (.7 WaterEfficiency +
                // .3 MoistureTolerance); temperature has only the one, so it carries full weight.
                //
                // Note this is inert until the environment actually varies in temperature:
                // EnvironmentField returns Temperature = 1 on every production path today, and at
                // 1 the expression collapses to 1, exactly the raw value. The flag is therefore
                // listed in LivenessTests.KnownInertFlags until terrain fields land.
                float temperatureLimit = sample.Temperature;
                if (temperatureAdaptationEnabled)
                {
                    temperatureLimit = sample.Temperature <= 0f
                        ? 0f
                        : Math.Min(1f, sample.Temperature + ((1f - sample.Temperature) * patch.Genome.TemperatureTolerance));
                }

                // Fertility mirrors the same pattern, and it is the channel that mattered most:
                // measured over 120 seeds at plant-reachable positions, fertility was the binding
                // minimum for 82-90% of them, because it was the only channel no gene could answer
                // while both adaptation terms lift THEIR channel out of contention for the Min.
                // That is why neither tolerance gene showed a selection response
                // (docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md).
                //
                // Like temperature, this is inert while the environment is flat: fertility is
                // pinned at 1 unless ProceduralEnvironmentFieldsEnabled is set, and at 1 the
                // expression collapses to the raw value. Unlike temperature, the flag is still
                // live under a flat field because it also gates NutrientUptake's growth charge.
                float fertilityLimit = sample.Fertility;
                if (fertilityAdaptationEnabled)
                {
                    fertilityLimit = sample.Fertility <= 0f
                        ? 0f
                        : Math.Min(1f, sample.Fertility + ((1f - sample.Fertility) * patch.Genome.NutrientUptake));
                }

                float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(fertilityLimit, temperatureLimit)));
                float sproutBiomass = patch.Biomass + (SproutFloorFraction * patch.Capacity);
                float growth = patch.GrowthRate * phenotype.GrowthRateMultiplier * sproutBiomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
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
                resources.SetFoodProjection(patch.FoodResourceId, patch.Biomass, patch.Nutrition * phenotype.NutritionMultiplier, phenotype.Defense);
            }
        }
    }
}

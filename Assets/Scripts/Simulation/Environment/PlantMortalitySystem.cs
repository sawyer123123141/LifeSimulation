using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantMortalitySystem
    {
        /// <summary>
        /// Ages every patch and removes those past their lifespan, releasing each dead patch's
        /// resource site back to the dispersal pool. Returns the total biomass removed, which the
        /// caller accumulates so the plant biomass conservation residual stays balanced.
        /// </summary>
        public static float Step(PlantPatchStore patches, ResourceStore resources, float deltaTime)
        {
            float removedBiomass = 0f;

            // Iterate backward: RemoveAt swaps the last element into the vacated slot, so a
            // forward loop would skip whatever got moved down into the current index.
            for (int index = patches.Count - 1; index >= 0; index--)
            {
                patches.AdvanceAge(index, deltaTime);
                PlantPatchState patch = patches.GetAt(index);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                if (patch.Age < phenotype.LifespanSeconds) continue;

                removedBiomass += patch.Biomass;
                resources.SetFoodProjection(patch.FoodResourceId, 0f, 1f, 0f);
                resources.SetActive(patch.FoodResourceId, false);
                patches.RemoveAt(index);
            }

            return removedBiomass;
        }
    }
}

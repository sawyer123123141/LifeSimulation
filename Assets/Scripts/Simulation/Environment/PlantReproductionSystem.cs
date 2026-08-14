using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantReproductionSystem
    {
        private const float MaturityFraction = .75f;
        private const float MutationStandardDeviation = .03f;
        private const int SiteAttempts = 4;

        public static int Step(PlantPatchStore patches, ResourceStore resources, int worldSeed, long tick, ref long seedOrdinal)
        {
            int parentCount = patches.Count;
            int births = 0;
            for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
            {
                PlantPatchState parent = patches.GetAt(parentIndex);
                if (parent.Biomass < parent.Capacity * MaturityFraction) continue;
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(parent.Genome);
                float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction;
                int siteIndex = FindSite(resources, parent, worldSeed, tick, seedOrdinal, phenotype.DispersalRange);
                if (siteIndex < 0) continue;

                ResourceState site = resources.GetAt(siteIndex);
                float transferred = patches.ConsumeAt(parentIndex, seedBiomass);
                if (transferred <= 0f) continue;
                PlantGenome childGenome = PlantGenome.CloneMutated(parent.Genome, worldSeed, seedOrdinal++, MutationStandardDeviation);
                int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.WaterDemand, parent.Nutrition, parent.Defense);
                PlantPatchState child = patches.GetAt(childIndex);
                patches.SetGenomeAndLineage(childIndex, childGenome, new PlantLineage(child.Id, parent.Id, parent.Lineage.Generation + 1));
                resources.SetActiveAt(siteIndex, true);
                births++;
            }

            return births;
        }

        private static int FindSite(ResourceStore resources, PlantPatchState parent, int seed, long tick, long ordinal, float range)
        {
            for (int attempt = 0; attempt < SiteAttempts; attempt++)
            {
                int index = (int)(DeterministicRandom.Float01(seed, RandomDomain.PlantDispersal, tick, parent.Id.Value, ordinal, attempt) * resources.Count);
                ResourceState candidate = resources.GetAt(index);
                if (candidate.Kind != ResourceKind.Food || candidate.IsActive || SimVector2.Distance(parent.Position, candidate.Position) > range) continue;
                return index;
            }
            return -1;
        }
    }
}

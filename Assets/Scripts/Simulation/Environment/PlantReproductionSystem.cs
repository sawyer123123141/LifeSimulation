using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantReproductionSystem
    {
        private const float MaturityFraction = .75f;
        private const float MutationStandardDeviation = .03f;
        private const int SiteAttempts = 4;
        private const float ReproductionCooldownSeconds = 20f;

        public static int Step(PlantPatchStore patches, ResourceStore resources, PlantSiteRegistry sites, int worldSeed, long tick, float deltaTime, ref long seedOrdinal)
        {
            int parentCount = patches.Count;
            int births = 0;
            for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
            {
                PlantPatchState parent = patches.GetAt(parentIndex);
                if (parent.ReproductionCooldownRemaining > 0f)
                {
                    float remaining = Math.Max(0f, parent.ReproductionCooldownRemaining - deltaTime);
                    patches.SetReproductionCooldown(parentIndex, remaining);
                    if (remaining > 0f) continue;
                }
                if (parent.Biomass < parent.Capacity * MaturityFraction) continue;
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(parent.Genome);
                float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction;
                int siteIndex = FindSite(resources, sites, parent, worldSeed, tick, seedOrdinal, phenotype.DispersalRange);
                if (siteIndex < 0) continue;

                ResourceState site = resources.GetAt(siteIndex);
                float transferred = patches.ConsumeAt(parentIndex, seedBiomass);
                if (transferred <= 0f) continue;
                PlantGenome childGenome = PlantGenome.CloneMutated(parent.Genome, worldSeed, seedOrdinal++, MutationStandardDeviation);
                int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.Nutrition, parent.Defense);
                PlantPatchState child = patches.GetAt(childIndex);
                patches.SetGenomeAndLineage(childIndex, childGenome, new PlantLineage(child.Id, parent.Id, parent.Lineage.Generation + 1));
                resources.SetActiveAt(siteIndex, true);
                patches.SetReproductionCooldown(parentIndex, ReproductionCooldownSeconds);
                births++;
            }

            return births;
        }

        public static float EstablishmentSuccessProbability(float distance, float dispersalRange)
        {
            float range = Math.Max(.01f, dispersalRange);
            float normalizedDistance = Math.Min(1f, Math.Max(0f, distance / range));
            return 1f - normalizedDistance;
        }

        private static int FindSite(ResourceStore resources, PlantSiteRegistry sites, PlantPatchState parent, int seed, long tick, long ordinal, float range)
        {
            if (sites.Count == 0) return -1;

            for (int attempt = 0; attempt < SiteAttempts; attempt++)
            {
                int slot = (int)(DeterministicRandom.Float01(seed, RandomDomain.PlantDispersal, tick, parent.Id.Value, ordinal, attempt) * sites.Count);
                int index = sites.GetResourceIndexAt(slot);
                ResourceState candidate = resources.GetAt(index);
                if (candidate.Kind != ResourceKind.Food || candidate.IsActive) continue;

                float distance = SimVector2.Distance(parent.Position, candidate.Position);
                if (distance > range) continue;

                float establishmentRoll = DeterministicRandom.Float01(seed, RandomDomain.PlantEstablishment, tick, parent.Id.Value, ordinal, attempt);
                if (establishmentRoll > EstablishmentSuccessProbability(distance, range)) continue;

                return index;
            }
            return -1;
        }
    }
}

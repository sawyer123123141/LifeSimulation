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
        private const float VulnerabilityFraction = .25f;

        /// <param name="establishmentContestEnabled">
        /// Makes the takeover of a vulnerable seedling a contest its <c>SeedlingResilience</c> can
        /// win, instead of an unconditional replacement. Measured on 2026-08-20, that replacement
        /// destroys 34% of every patch ever born inside a median two seconds and accounts for
        /// 51.9% of the variance in per-patch lifetime offspring, with no gene correlating with
        /// the outcome above |r| = 0.10 - the largest non-heritable term in plant fitness.
        /// docs/experiments/p4-where-plant-fitness-is-decided-2026-08-20.md
        /// </param>
        public static int Step(PlantPatchStore patches, ResourceStore resources, PlantSiteRegistry sites, int worldSeed, long tick, float deltaTime, ref long seedOrdinal, bool competitionEnabled = false, bool establishmentContestEnabled = false)
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
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(parent.Genome, fertilityAdaptationEnabled: false, establishmentContestEnabled);
                float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction;
                int siteIndex = FindSite(resources, sites, patches, parent, worldSeed, tick, seedOrdinal, phenotype.DispersalRange, competitionEnabled, establishmentContestEnabled);
                if (siteIndex < 0) continue;

                ResourceState site = resources.GetAt(siteIndex);
                float transferred = patches.ConsumeAt(parentIndex, seedBiomass);
                if (transferred <= 0f) continue;
                PlantGenome childGenome = PlantGenome.CloneMutated(parent.Genome, worldSeed, seedOrdinal++, MutationStandardDeviation);

                if (site.IsActive)
                {
                    int occupantIndex = patches.FindIndex(site.Id);
                    if (occupantIndex < 0) continue;
                    PlantPatchState occupant = patches.GetAt(occupantIndex);
                    float takenOverBiomass = Math.Min(site.Capacity, transferred + occupant.Biomass);
                    var takeoverLineage = new PlantLineage(occupant.Id, parent.Id, parent.Lineage.Generation + 1);
                    patches.ReplaceAt(occupantIndex, childGenome, takeoverLineage, takenOverBiomass, parent.GrowthRate, parent.Nutrition, parent.Defense);
                }
                else
                {
                    int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.Nutrition, parent.Defense);
                    PlantPatchState child = patches.GetAt(childIndex);
                    patches.SetGenomeAndLineage(childIndex, childGenome, new PlantLineage(child.Id, parent.Id, parent.Lineage.Generation + 1));
                }

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

        private static int FindSite(ResourceStore resources, PlantSiteRegistry sites, PlantPatchStore patches, PlantPatchState parent, int seed, long tick, long ordinal, float range, bool competitionEnabled, bool establishmentContestEnabled)
        {
            if (sites.Count == 0) return -1;

            for (int attempt = 0; attempt < SiteAttempts; attempt++)
            {
                int slot = (int)(DeterministicRandom.Float01(seed, RandomDomain.PlantDispersal, tick, parent.Id.Value, ordinal, attempt) * sites.Count);
                int index = sites.GetResourceIndexAt(slot);
                ResourceState candidate = resources.GetAt(index);
                if (candidate.Kind != ResourceKind.Food) continue;

                if (candidate.IsActive)
                {
                    if (!competitionEnabled) continue;
                    if (candidate.Id.Equals(parent.FoodResourceId)) continue;
                    int occupantIndex = patches.FindIndex(candidate.Id);
                    if (occupantIndex < 0) continue;
                    PlantPatchState occupant = patches.GetAt(occupantIndex);
                    if (occupant.Capacity <= 0f) continue;
                    if (occupant.Biomass / occupant.Capacity >= VulnerabilityFraction) continue;

                    // The incumbent seedling gets to defend itself. Drawn on its own random
                    // domain rather than reusing PlantEstablishment, so the two rolls stay
                    // independent and the contest cannot correlate with the distance roll below.
                    if (establishmentContestEnabled)
                    {
                        float contestRoll = DeterministicRandom.Float01(seed, RandomDomain.PlantEstablishmentContest, tick, parent.Id.Value, ordinal, attempt);
                        if (contestRoll < occupant.Genome.SeedlingResilience) continue;
                    }
                }

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

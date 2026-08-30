using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlantSiteGeneratorTests
    {
        private static List<GeneratedPlantSite> Generate(int seed, float threshold, float budget = 432f, float spacing = 5f)
        {
            return PlantSiteGenerator.Generate(
                seed,
                EnvironmentField.CreateProcedural(seed, elevationEnabled: true),
                arenaHalfWidth: 25f,
                spacing: spacing,
                jitterFraction: .35f,
                fertilityThreshold: threshold,
                capacityBudget: budget);
        }

        [Test]
        public void GeneratedSitesAreDeterministicInTheWorldSeed()
        {
            List<GeneratedPlantSite> first = Generate(42, .45f);
            List<GeneratedPlantSite> second = Generate(42, .45f);

            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].Position.X, Is.EqualTo(first[index].Position.X));
                Assert.That(second[index].Position.Y, Is.EqualTo(first[index].Position.Y));
                Assert.That(second[index].Capacity, Is.EqualTo(first[index].Capacity));
            }
        }

        [Test]
        public void DifferentSeedsProduceDifferentPlacement()
        {
            List<GeneratedPlantSite> first = Generate(42, .45f);
            List<GeneratedPlantSite> second = Generate(43, .45f);

            bool identical = first.Count == second.Count
                && first.Zip(second, (left, right) => left.Position.X == right.Position.X && left.Position.Y == right.Position.Y).All(same => same);
            Assert.That(identical, Is.False);
        }

        /// <summary>
        /// The budget is the whole reason a threshold change is a placement change and not a food
        /// change. If it did not hold, lowering the threshold would make the world richer and the
        /// measured difference would be about how much food there is.
        /// </summary>
        [Test]
        public void TotalCapacityEqualsTheBudgetWhateverTheThreshold()
        {
            foreach (float threshold in new[] { .20f, .45f, .70f })
            {
                List<GeneratedPlantSite> sites = Generate(42, threshold);
                if (sites.Count == 0) continue;
                Assert.That(sites.Sum(site => site.Capacity), Is.EqualTo(432f).Within(.5f), $"threshold {threshold}");
            }
        }

        [Test]
        public void RaisingTheThresholdKeepsFewerAndMoreFertileSites()
        {
            List<GeneratedPlantSite> permissive = Generate(42, .30f);
            List<GeneratedPlantSite> strict = Generate(42, .60f);

            Assert.That(strict.Count, Is.LessThan(permissive.Count));
            Assert.That(strict.All(site => site.Fertility >= .60f));
        }

        [Test]
        public void EverySiteSitsInsideTheArena()
        {
            foreach (GeneratedPlantSite site in Generate(42, .45f))
            {
                Assert.That(site.Position.X, Is.InRange(-25f, 25f));
                Assert.That(site.Position.Y, Is.InRange(-25f, 25f));
            }
        }

        [Test]
        public void MoreFertileGroundGetsMoreCapacity()
        {
            List<GeneratedPlantSite> sites = Generate(42, .45f).OrderBy(site => site.Fertility).ToList();

            Assert.That(sites.Count, Is.GreaterThan(1));
            Assert.That(sites.Last().Capacity, Is.GreaterThan(sites.First().Capacity));
        }

        [Test]
        public void SplitSitesAtOnePartIsTheSameLayout()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            SimulationScenario split = source.SplitSites("split-1", parts: 1, spread: 6f);

            Assert.That(split.ComputeLayoutFingerprint(), Is.EqualTo(source.ComputeLayoutFingerprint()));
            Assert.That(split.FounderPlacement, Is.EqualTo(source.FounderPlacement));
        }

        [Test]
        public void SplitSitesKeepsAPartOnTheFounderPlacement()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            SimulationScenario split = source.SplitSites("split-4", parts: 4, spread: 6f);
            var world = new SimulationWorld(CreateConfig(generatedSites: false));
            split.ApplyTo(world);

            SimVector2 placement = source.FounderPlacement.Value;
            bool siteOnPlacement = false;
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind != ResourceKind.Food) continue;
                if (SimVector2.Distance(resource.Position, placement) <= 0f) siteOnPlacement = true;
            }

            Assert.That(siteOnPlacement, Is.True, "founders would start on empty ground");
        }

        [Test]
        public void SplitSitesConservesTotalFoodCapacity()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            float before = TotalFoodCapacity(source, generatedSites: false);
            float after = TotalFoodCapacity(source.SplitSites("split-4", parts: 4, spread: 6f), generatedSites: false);

            Assert.That(after, Is.EqualTo(before).Within(.5f));
        }

        [Test]
        public void GeneratedPlacementReplacesTheDormantSitesAndKeepsTheirCapacity()
        {
            SimulationScenario scenario = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            var authored = new SimulationWorld(CreateConfig(generatedSites: false));
            scenario.ApplyTo(authored);
            var generated = new SimulationWorld(CreateConfig(generatedSites: true));
            scenario.ApplyTo(generated);

            Assert.That(FoodSiteCount(generated), Is.GreaterThan(FoodSiteCount(authored)));
            Assert.That(TotalFoodCapacity(generated), Is.EqualTo(TotalFoodCapacity(authored)).Within(1f));
            Assert.That(generated.Plants.Count, Is.EqualTo(authored.Plants.Count), "the founder plants are unchanged");
        }

        /// <summary>
        /// The registry holds resource INDICES, and generated placement is the first thing that adds
        /// resources outside the definition loop. A slot pointing at the wrong resource would send
        /// seeds to a water site and fail silently.
        /// </summary>
        [Test]
        public void EveryRegisteredSlotPointsAtADormantFoodResource()
        {
            var world = new SimulationWorld(CreateConfig(generatedSites: true));
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            Assert.That(world.PlantSites.Count, Is.GreaterThan(0));
            for (int slot = 0; slot < world.PlantSites.Count; slot++)
            {
                ResourceState resource = world.Resources.GetAt(world.PlantSites.GetResourceIndexAt(slot));
                Assert.That(resource.Kind, Is.EqualTo(ResourceKind.Food), $"slot {slot}");
            }
        }

        [Test]
        public void GeneratedPlacementIsInertWhileTheFlagIsOff()
        {
            var world = new SimulationWorld(CreateConfig(generatedSites: false));
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            Assert.That(FoodSiteCount(world), Is.EqualTo(24));
        }

        private static int FoodSiteCount(SimulationWorld world)
        {
            int count = 0;
            for (int index = 0; index < world.Resources.Count; index++)
            {
                if (world.Resources.GetAt(index).Kind == ResourceKind.Food) count++;
            }

            return count;
        }

        private static float TotalFoodCapacity(SimulationWorld world)
        {
            float total = 0f;
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind == ResourceKind.Food) total += resource.Capacity;
            }

            return total;
        }

        private static float TotalFoodCapacity(SimulationScenario scenario, bool generatedSites)
        {
            var world = new SimulationWorld(CreateConfig(generatedSites));
            scenario.ApplyTo(world);
            return TotalFoodCapacity(world);
        }

        private static SimulationConfig CreateConfig(bool generatedSites)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            return new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 96,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                elevationFieldEnabled: true,
                generatedPlantSitesEnabled: generatedSites);
        }
    }
}

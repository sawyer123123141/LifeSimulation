using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class HomeRangeAffinityTests
    {
        [Test]
        public void EnabledAffinityPrefersTheEqualFoodCandidateNearerTheHomeRangeCentre()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 2);
            ResourceId fartherFromHome = AddResource(resources, ResourceKind.Food, new SimVector2(-2f, 0f));
            ResourceId nearerHome = AddResource(resources, ResourceKind.Food, new SimVector2(2f, 0f));
            var foodCandidates = new ResourceCandidateBuffer();
            foodCandidates.Consider(new ResourceObservation(fartherFromHome, resourceIndex: 0, distance: 2f, remainingAmount: 10f));
            foodCandidates.Consider(new ResourceObservation(nearerHome, resourceIndex: 1, distance: 2f, remainingAmount: 10f));
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(2f, 0f),
                Familiarity = 1f,
            };

            CreatureDecision disabled = Decide(
                needs, genome, phenotype, resources, out _, foodCandidates, homeRange: homeRange);
            CreatureDecision enabled = Decide(
                needs, genome, phenotype, resources, out _, foodCandidates,
                homeRange: homeRange, homeRangeAffinityEnabled: true);

            Assert.That(disabled.TargetResourceIndex, Is.EqualTo(0));
            Assert.That(enabled.TargetResourceIndex, Is.EqualTo(1));
            Assert.That(enabled.Score, Is.GreaterThan(disabled.Score));
        }

        [Test]
        public void ActiveThreatLeavesAffinityOutOfFoodAndFleeScores()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 1);
            ResourceId foodId = AddResource(resources, ResourceKind.Food, new SimVector2(1f, 0f));
            var foodCandidates = new ResourceCandidateBuffer();
            foodCandidates.Consider(new ResourceObservation(foodId, resourceIndex: 0, distance: 1f, remainingAmount: 10f));
            var threat = new CreatureObservation(new CreatureId(2), creatureIndex: 1, distance: 1f);
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(1f, 0f),
                Familiarity = 1f,
            };

            CreatureDecision disabled = Decide(
                needs, genome, phenotype, resources, out DecisionDiagnostics disabledDiagnostics,
                foodCandidates, threat, threatIntensity: 10f, predationEnabled: true, homeRange: homeRange);
            CreatureDecision enabled = Decide(
                needs, genome, phenotype, resources, out DecisionDiagnostics enabledDiagnostics,
                foodCandidates, threat, threatIntensity: 10f, predationEnabled: true,
                homeRange: homeRange, homeRangeAffinityEnabled: true);

            Assert.That(disabled.Action, Is.EqualTo(CreatureAction.Flee));
            Assert.That(enabled.Action, Is.EqualTo(CreatureAction.Flee));
            Assert.That(enabled.Score, Is.EqualTo(disabled.Score));
            Assert.That(enabledDiagnostics.FleeScore, Is.EqualTo(disabledDiagnostics.FleeScore));
            Assert.That(enabledDiagnostics.FoodScore, Is.EqualTo(disabledDiagnostics.FoodScore));
        }

        [Test]
        public void MatingIntentReceivesNoAffinityBonus()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Age = ReproductionSystem.AdultAgeSeconds;
            CreatureNeeds mateNeeds = CreatureNeeds.Full(phenotype);
            mateNeeds.Age = ReproductionSystem.AdultAgeSeconds;
            var resources = new ResourceStore(initialCapacity: 0);
            var mate = new CreatureObservation(new CreatureId(2), creatureIndex: 1, distance: 1f);
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(1f, 0f),
                Familiarity = 1f,
            };

            CreatureDecision disabled = DecideMate(needs, genome, phenotype, resources, mate, mateNeeds, homeRange, false);
            CreatureDecision enabled = DecideMate(needs, genome, phenotype, resources, mate, mateNeeds, homeRange, true);

            Assert.That(disabled.Action, Is.EqualTo(CreatureAction.SeekMate));
            Assert.That(enabled.Action, Is.EqualTo(CreatureAction.SeekMate));
            Assert.That(enabled.Score, Is.EqualTo(disabled.Score));
        }

        [Test]
        public void NoResourceFallbackReceivesNoAffinityBonus()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(5f, 5f),
                Familiarity = 1f,
            };

            CreatureDecision decision = Decide(
                needs, genome, phenotype, new ResourceStore(initialCapacity: 0), out _,
                homeRange: homeRange, homeRangeAffinityEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Wander));
            Assert.That(decision.Score, Is.EqualTo(0f));
        }

        [Test]
        public void UnavailableFoodCandidateReceivesNoAffinityBonus()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 1);
            ResourceId foodId = AddResource(resources, ResourceKind.Food, new SimVector2(1f, 0f));
            resources.SetActive(foodId, false);
            var foodCandidates = new ResourceCandidateBuffer();
            foodCandidates.Consider(new ResourceObservation(foodId, resourceIndex: 0, distance: 1f, remainingAmount: 10f));
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(1f, 0f),
                Familiarity = 1f,
            };

            Decide(needs, genome, phenotype, resources, out DecisionDiagnostics disabledDiagnostics, foodCandidates, homeRange: homeRange);
            Decide(
                needs, genome, phenotype, resources, out DecisionDiagnostics enabledDiagnostics,
                foodCandidates, homeRange: homeRange, homeRangeAffinityEnabled: true);

            Assert.That(enabledDiagnostics.FoodScore, Is.EqualTo(disabledDiagnostics.FoodScore));
        }

        [TestCase(ResourceKind.Food, CreatureAction.Eat)]
        [TestCase(ResourceKind.Water, CreatureAction.Drink)]
        public void SuccessfulOrdinaryResourceUseRecordsHomeRangeOnlyWhenEnabled(ResourceKind resourceKind, CreatureAction action)
        {
            var enabled = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: true));
            var disabled = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: false));
            var successPosition = new SimVector2(4f, -4f);
            PrepareResourceSuccess(enabled, resourceKind, action, successPosition);
            PrepareResourceSuccess(disabled, resourceKind, action, successPosition);

            enabled.Step(enabled.Config.FixedDeltaTime);
            disabled.Step(disabled.Config.FixedDeltaTime);

            AssertHomeRange(enabled.Creatures.GetHomeRangeRefAt(0), 1f, -1f, 0.25f);
            AssertBlank(disabled.Creatures.GetHomeRangeRefAt(0));
        }

        [Test]
        public void SuccessfulReproductionRecordsBothParentsButNotTheChildOnlyWhenEnabled()
        {
            var enabled = new SimulationWorld(CreateConfig(
                homeRangeAffinityEnabled: true,
                initialPopulation: 2,
                maximumPopulation: 3,
                reproductionHz: 20));
            var disabled = new SimulationWorld(CreateConfig(
                homeRangeAffinityEnabled: false,
                initialPopulation: 2,
                maximumPopulation: 3,
                reproductionHz: 20));
            PrepareReadyParents(enabled);
            PrepareReadyParents(disabled);

            enabled.Step(enabled.Config.FixedDeltaTime);
            disabled.Step(disabled.Config.FixedDeltaTime);

            Assert.That(enabled.CreatureCount, Is.EqualTo(3));
            Assert.That(disabled.CreatureCount, Is.EqualTo(3));
            AssertHomeRange(enabled.Creatures.GetHomeRangeRefAt(0), 0.5f, 0.5f, 0.25f);
            AssertHomeRange(enabled.Creatures.GetHomeRangeRefAt(1), 0.75f, 0.5f, 0.25f);
            AssertBlank(enabled.Creatures.GetHomeRangeRefAt(2));
            AssertBlank(disabled.Creatures.GetHomeRangeRefAt(0));
            AssertBlank(disabled.Creatures.GetHomeRangeRefAt(1));
            AssertBlank(disabled.Creatures.GetHomeRangeRefAt(2));
        }

        [Test]
        public void NeedsTickDecaysFamiliarityOnlyWhenEnabledWithoutMovingTheCentre()
        {
            var enabled = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: true, needsHz: 20));
            var disabled = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: false, needsHz: 20));
            enabled.Creatures.GetHomeRangeRefAt(0) = new HomeRangeState
            {
                Centre = new SimVector2(3f, -2f),
                Familiarity = 0.5f,
            };
            disabled.Creatures.GetHomeRangeRefAt(0) = new HomeRangeState
            {
                Centre = new SimVector2(3f, -2f),
                Familiarity = 0.5f,
            };

            enabled.Step(enabled.Config.FixedDeltaTime);
            disabled.Step(disabled.Config.FixedDeltaTime);

            AssertHomeRange(enabled.Creatures.GetHomeRangeRefAt(0), 3f, -2f, 0.4995f);
            AssertHomeRange(disabled.Creatures.GetHomeRangeRefAt(0), 3f, -2f, 0.5f);
        }

        [Test]
        public void DisabledPairedWorldsIgnoreHomeRangeStateAndRemainByteIdentical()
        {
            var baseline = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: false));
            var dirtyHomeRange = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: false));
            dirtyHomeRange.Creatures.GetHomeRangeRefAt(0) = new HomeRangeState
            {
                Centre = new SimVector2(12f, -9f),
                Familiarity = 1f,
            };

            Assert.That(dirtyHomeRange.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
            for (int tick = 0; tick < 5; tick++)
            {
                baseline.Step(baseline.Config.FixedDeltaTime);
                dirtyHomeRange.Step(dirtyHomeRange.Config.FixedDeltaTime);
                Assert.That(dirtyHomeRange.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
            }
        }

        [Test]
        public void EnabledStateHashIncludesHomeRangeState()
        {
            var baseline = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: true));
            var dirtyHomeRange = new SimulationWorld(CreateConfig(homeRangeAffinityEnabled: true));
            dirtyHomeRange.Creatures.GetHomeRangeRefAt(0) = new HomeRangeState
            {
                Centre = new SimVector2(12f, -9f),
                Familiarity = 1f,
            };

            Assert.That(dirtyHomeRange.ComputeStateHash(), Is.Not.EqualTo(baseline.ComputeStateHash()));
        }

        [Test]
        public void RecordSuccessClampsFamiliarityFromPointNineToOne()
        {
            var homeRange = new HomeRangeState
            {
                Centre = new SimVector2(0f, 0f),
                Familiarity = 0.9f,
            };

            HomeRangeSystem.RecordSuccess(ref homeRange, new SimVector2(1f, 0f));

            Assert.That(homeRange.Familiarity, Is.EqualTo(1f));
        }

        [Test]
        public void ReplacementClearsADirtyVacatedHomeRangeSlot()
        {
            var store = new CreatureStore(initialCapacity: 2);
            store.Add();
            CreatureId removed = store.Add();
            store.GetHomeRangeRefAt(1) = new HomeRangeState
            {
                Centre = new SimVector2(8f, -3f),
                Familiarity = 1f,
            };

            Assert.That(store.Remove(removed), Is.True);
            CreatureId replacement = store.Add();
            Assert.That(store.TryGetIndex(replacement, out int replacementIndex), Is.True);

            AssertBlank(store.GetHomeRangeRefAt(replacementIndex));
        }

        private static CreatureDecision Decide(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            out DecisionDiagnostics diagnostics,
            ResourceCandidateBuffer foodCandidates = default,
            CreatureObservation threat = default,
            float threatIntensity = 0f,
            bool predationEnabled = false,
            HomeRangeState homeRange = default,
            bool homeRangeAffinityEnabled = false)
        {
            return DecisionSystem.DecideIntentUtilityV1(
                needs, genome, phenotype, resources, new SimVector2(0f, 0f), foodCandidates, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threat,
                threatIntensity: threatIntensity, otherPhenotype: phenotype, predationEnabled: predationEnabled,
                physiologyEnabled: false, tick: 1, diagnostics: out diagnostics,
                homeRange: homeRange, homeRangeAffinityEnabled: homeRangeAffinityEnabled);
        }

        private static CreatureDecision DecideMate(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            HomeRangeState homeRange,
            bool homeRangeAffinityEnabled)
        {
            return DecisionSystem.DecideIntentUtilityV1(
                needs, genome, phenotype, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
                reproduction: default, mate: mate, mateNeeds: mateNeeds, matePhenotype: phenotype,
                mateReproduction: default, reproductionEnabled: true, tick: 1, diagnostics: out _,
                homeRange: homeRange, homeRangeAffinityEnabled: homeRangeAffinityEnabled);
        }

        private static ResourceId AddResource(ResourceStore resources, ResourceKind kind, SimVector2 position)
        {
            return resources.Add(
                kind,
                position,
                interactionRadius: 1f,
                initialAmount: 10f,
                capacity: 10f,
                regenerationPerSecond: 0f);
        }

        private static SimulationConfig CreateConfig(
            bool homeRangeAffinityEnabled,
            int initialPopulation = 1,
            int maximumPopulation = 1,
            int needsHz = 1,
            int reproductionHz = 1)
        {
            return new SimulationConfig(
                worldSeed: 701,
                initialPopulation: initialPopulation,
                schedule: new SimulationSchedule(
                    baseFrequencyHz: 20,
                    movementHz: 20,
                    perceptionHz: 1,
                    needsHz: needsHz,
                    decisionsHz: 1,
                    resourcesHz: 1,
                    reproductionHz: reproductionHz,
                    statisticsHz: 1),
                maximumPopulation: maximumPopulation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                homeRangeAffinityEnabled: homeRangeAffinityEnabled);
        }

        private static void PrepareResourceSuccess(
            SimulationWorld world,
            ResourceKind resourceKind,
            CreatureAction action,
            SimVector2 position)
        {
            world.SetCreaturePosition(world.GetCreatureIdAt(0), position);
            AddResource(world.Resources, resourceKind, position);
            world.Creatures.SetDecisionAt(0, new CreatureDecision(action, targetResourceIndex: 0, score: 1f));
        }

        private static void PrepareReadyParents(SimulationWorld world)
        {
            var positions = new[]
            {
                new SimVector2(2f, 2f),
                new SimVector2(3f, 2f),
            };
            for (int index = 0; index < 2; index++)
            {
                world.SetCreaturePosition(world.GetCreatureIdAt(index), positions[index]);
                world.Creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
                world.Creatures.SetDecisionAt(index, new CreatureDecision(CreatureAction.Rest, -1, 0f));
            }
        }

        private static void AssertHomeRange(HomeRangeState state, float centreX, float centreY, float familiarity)
        {
            Assert.That(state.Centre.X, Is.EqualTo(centreX));
            Assert.That(state.Centre.Y, Is.EqualTo(centreY));
            Assert.That(state.Familiarity, Is.EqualTo(familiarity));
        }

        private static void AssertBlank(HomeRangeState state)
        {
            AssertHomeRange(state, 0f, 0f, 0f);
        }
    }
}

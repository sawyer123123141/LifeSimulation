using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class CoreSimulationTests
    {
        [Test]
        public void PrototypeDefaultsProduceAValidSchedule()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 100);

            Assert.That(() => config.Validate(), Throws.Nothing);
            Assert.That(config.Schedule.BaseFrequencyHz, Is.EqualTo(20));
            Assert.That(config.DecisionPolicyVersion, Is.EqualTo(DecisionPolicyVersion.Legacy));
        }

        [Test]
        public void Prototype4ScenarioProjectsPlantCohortsIntoStableFoodResources()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 0);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.PlantBackedBaseline.ApplyTo(world);

            Assert.That(world.Plants.Count, Is.EqualTo(2));
            Assert.That(world.Resources.Count, Is.EqualTo(12));
            Assert.That(world.Resources.GetAt(4).IsActive, Is.False);
            Assert.That(world.Resources.GetAt(0).Amount, Is.EqualTo(world.Plants.GetAt(0).Biomass));

            for (int index = 0; index < config.Schedule.BaseFrequencyHz; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Resources.GetAt(0).Id, Is.EqualTo(world.Plants.GetAt(0).FoodResourceId));
            Assert.That(world.Resources.GetAt(0).Amount, Is.EqualTo(world.Plants.GetAt(0).Biomass));
            Assert.That(world.Statistics.PlantBiomassResidual, Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void PredationFounderProfileSeedsUnlabeledPredationVariation()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 2);
            var config = new SimulationConfig(
                42,
                2,
                defaults.Schedule,
                maximumPopulation: 1000,
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            Assert.That(world.Creatures.GetGenomeAt(0).Attack, Is.InRange(0f, 1f));
            Assert.That(world.Creatures.GetGenomeAt(0).DietSpecialization, Is.InRange(0f, 1f));
            Assert.That(world.Creatures.GetGenomeAt(0).Attack, Is.Not.EqualTo(0f));
            Assert.That(world.Creatures.GetGenomeAt(0).MemoryCapacity, Is.EqualTo(0f));
            Assert.That(world.Creatures.GetGenomeAt(0).TemperatureTolerance, Is.EqualTo(0f));
            Assert.That(world.Creatures.GetGenomeAt(0).FertilityInvestment, Is.EqualTo(0f));
        }

        [Test]
        public void PredationStatisticsExposeThePopulationTraitMeansUsedByControlExperiments()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 4);
            var world = new SimulationWorld(new SimulationConfig(
                42,
                4,
                defaults.Schedule,
                founderProfile: FounderProfile.PredationVariation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1));
            world.Spawn(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));

            for (int index = 0; index < 20; index++)
            {
                world.Step(world.Config.FixedDeltaTime);
            }

            Assert.That(world.Statistics.MeanAttackGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanDefenseGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanAggressionGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanDietSpecializationGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.ViableHunterCount, Is.GreaterThan(0));
            Assert.That(world.Statistics.ViableHunterCount + world.Statistics.NonHunterCount, Is.EqualTo(world.Statistics.Population));
        }

        [Test]
        public void PhysiologyFounderProfileSeedsVariationWithoutPredatorTraits()
        {
            var world = new SimulationWorld(SimulationConfig.CreatePrototype3Defaults(42, 2));

            Assert.That(world.Creatures.GetGenomeAt(0).TemperatureTolerance, Is.InRange(0f, 1f));
            Assert.That(world.Creatures.GetGenomeAt(0).FertilityInvestment, Is.InRange(0f, 1f));
            Assert.That(world.Creatures.GetGenomeAt(0).Attack, Is.EqualTo(0f));
        }

        [Test]
        public void Prototype2FounderProfileSeedsCognitionVariationWithoutPredationTraits()
        {
            var world = new SimulationWorld(SimulationConfig.CreatePrototype2Defaults(42, 2));

            Genome genome = world.Creatures.GetGenomeAt(0);
            Assert.That(genome.MemoryCapacity, Is.GreaterThan(0f));
            Assert.That(genome.MemoryRetention, Is.GreaterThan(0f));
            Assert.That(genome.LearningRate, Is.GreaterThan(0f));
            Assert.That(genome.Exploration, Is.GreaterThan(0f));
            Assert.That(genome.Attack, Is.EqualTo(0f));
        }

        [Test]
        public void CognitionStatisticsExposeThePopulationTraitMeansUsedByP2Experiments()
        {
            var world = new SimulationWorld(SimulationConfig.CreatePrototype2Defaults(42, 4));
            for (int index = 0; index < 20; index++) world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.Statistics.MeanMemoryCapacityGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanMemoryRetentionGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanLearningRateGene, Is.GreaterThan(0f));
            Assert.That(world.Statistics.MeanExplorationGene, Is.GreaterThan(0f));
        }

        [Test]
        public void ScheduleRejectsFrequenciesThatDoNotDivideBaseFrequency()
        {
            var schedule = new SimulationSchedule(20, 20, 3, 2, 2, 1, 1, 1);
            var config = new SimulationConfig(42, 100, schedule);

            Assert.That(() => config.Validate(), Throws.ArgumentException);
        }

        [Test]
        public void ConfigurationRejectsNegativeFounderPopulation()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, -1);

            Assert.That(() => config.Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SimVectorDistanceUsesBothGroundPlaneAxes()
        {
            var origin = new SimVector2(1f, 2f);
            var point = new SimVector2(4f, 6f);

            Assert.That(SimVector2.Distance(origin, point), Is.EqualTo(5f));
        }

        [Test]
        public void KeyedDrawDoesNotDependOnCallOrder()
        {
            float first = DeterministicRandom.Float01(42, RandomDomain.Mutation, 10, 7, 9, 2);

            _ = DeterministicRandom.Float01(42, RandomDomain.Wander, 99, 3, 0, 0);
            float repeated = DeterministicRandom.Float01(42, RandomDomain.Mutation, 10, 7, 9, 2);

            Assert.That(repeated, Is.EqualTo(first));
        }

        [Test]
        public void DecisionTraceRecordsLegacyWinnerScoresAndSwitchesForItsSampledCreature()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn();
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 10f, 10f, 0f);
            world.Resources.Add(ResourceKind.Water, new SimVector2(2f, 0f), 1f, 10f, 10f, 0f);
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;
            world.EnableDecisionTrace(creature, capacity: 4);

            for (int tick = 0; tick < 40; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.DecisionTrace.Count, Is.EqualTo(4));
            DecisionTraceEntry entry = world.DecisionTrace.GetAt(0);
            Assert.That(entry.Winner.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(entry.FoodScore, Is.GreaterThan(entry.WaterScore));
            Assert.That(entry.Switched, Is.True);
            Assert.That(world.DecisionTrace.GetAt(1).InvalidationReason, Is.EqualTo(DecisionInvalidationReason.ExecutionTransition));
            for (int index = 0; index < world.DecisionTrace.Count; index++)
            {
                DecisionTraceEntry trace = world.DecisionTrace.GetAt(index);
                TestContext.WriteLine($"tick={trace.Tick}; previous={trace.Previous.Action}/{trace.Previous.TargetResourceIndex}; winner={trace.Winner.Action}/{trace.Winner.TargetResourceIndex}; food={trace.FoodScore:F3}; water={trace.WaterScore:F3}; switched={trace.Switched}; reason={trace.InvalidationReason}");
            }
        }

        [Test]
        public void GaussianDrawIsFiniteAndRepeatableForAKey()
        {
            float first = DeterministicRandom.Gaussian(42, RandomDomain.Mutation, 10, 7, 9, 4);
            float repeated = DeterministicRandom.Gaussian(42, RandomDomain.Mutation, 10, 7, 9, 4);

            Assert.That(float.IsNaN(first) || float.IsInfinity(first), Is.False);
            Assert.That(repeated, Is.EqualTo(first));
        }

        [Test]
        public void SwapBackRemovalPreservesMovedCreatureLookup()
        {
            var store = new CreatureStore(initialCapacity: 3);
            CreatureId first = store.Add();
            _ = store.Add();
            CreatureId last = store.Add();

            Assert.That(store.Remove(first), Is.True);

            Assert.That(store.TryGetIndex(last, out int movedIndex), Is.True);
            Assert.That(movedIndex, Is.EqualTo(0));
            Assert.That(store.Count, Is.EqualTo(2));
        }

        [Test]
        public void CreatureIdsAreNotReusedAfterRemoval()
        {
            var store = new CreatureStore(initialCapacity: 1);
            CreatureId removed = store.Add();
            Assert.That(store.Remove(removed), Is.True);

            CreatureId replacement = store.Add();

            Assert.That(replacement.Value, Is.GreaterThan(removed.Value));
        }

        [Test]
        public void ChildLineageRecordsBothParentsAndTheNextGeneration()
        {
            var store = new CreatureStore(initialCapacity: 3);
            CreatureId firstParent = store.Add();
            CreatureId secondParent = store.Add();
            CreatureId child = store.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);

            Assert.That(store.TryGetIndex(child, out int childIndex), Is.True);
            CreatureLineage lineage = store.GetLineageAt(childIndex);
            Assert.That(lineage.FirstParent, Is.EqualTo(firstParent));
            Assert.That(lineage.SecondParent, Is.EqualTo(secondParent));
            Assert.That(lineage.Generation, Is.EqualTo(1));
            Assert.That(lineage.LineageId, Is.EqualTo(child));
        }

        [Test]
        public void SwapBackRemovalKeepsBiologyAlignedWithTheMovedCreature()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId first = store.Add(new Genome(0f, 0f, 0f, 0f, 0f, 0f), new SimVector2(-1f, 0f));
            CreatureId moved = store.Add(new Genome(1f, 1f, 1f, 1f, 1f, 1f), new SimVector2(3f, 4f));
            store.SetDecisionAt(1, new CreatureDecision(CreatureAction.SeekWater, targetResourceIndex: 3, score: 0.9f));

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);
            Assert.That(store.GetGenomeAt(movedIndex).BodySize, Is.EqualTo(1f));
            Assert.That(store.GetPhenotypeAt(movedIndex).EnergyCapacity, Is.GreaterThan(100f));
            Assert.That(store.GetNeedsAt(movedIndex).Energy, Is.EqualTo(store.GetPhenotypeAt(movedIndex).EnergyCapacity));
            Assert.That(store.GetMovementAt(movedIndex).Position.X, Is.EqualTo(3f));
            Assert.That(store.GetMovementAt(movedIndex).Position.Y, Is.EqualTo(4f));
            Assert.That(store.GetDecisionAt(movedIndex).Action, Is.EqualTo(CreatureAction.SeekWater));
            Assert.That(store.GetDecisionAt(movedIndex).TargetResourceIndex, Is.EqualTo(3));
        }

        [Test]
        public void SwapBackRemovalKeepsTheP1CombatSidecarAligned()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId first = store.Add();
            CreatureId moved = store.Add();
            ref CombatState combat = ref store.GetCombatRefAt(1);
            combat.WoundSeverity = 0.35f;
            combat.AttackRecoveryRemaining = 2f;

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);
            Assert.That(store.GetCombatRefAt(movedIndex).WoundSeverity, Is.EqualTo(0.35f));
            Assert.That(store.GetCombatRefAt(movedIndex).AttackRecoveryRemaining, Is.EqualTo(2f));
        }

        [Test]
        public void SwapBackRemovalKeepsTheP2MemorySidecarAligned()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId first = store.Add();
            CreatureId moved = store.Add();
            ref MemoryState memory = ref store.GetMemoryRefAt(1);
            memory.FoodPosition = new SimVector2(5f, -3f);
            memory.FoodConfidence = 0.8f;

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);
            Assert.That(store.GetMemoryRefAt(movedIndex).FoodPosition.X, Is.EqualTo(5f));
            Assert.That(store.GetMemoryRefAt(movedIndex).FoodPosition.Y, Is.EqualTo(-3f));
            Assert.That(store.GetMemoryRefAt(movedIndex).FoodConfidence, Is.EqualTo(0.8f));
        }

        [Test]
        public void WorldAppliesRequestedDeathsAtTheEndOfItsFixedStep()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            CreatureId creature = world.GetCreatureIdAt(0);

            world.RequestDeath(creature, DeathCause.Debug);
            world.Step(config.FixedDeltaTime);

            Assert.That(world.CreatureCount, Is.EqualTo(0));
            Assert.That(world.TryGetCreatureIndex(creature, out _), Is.False);
        }

        [Test]
        public void PredationDeathLeavesEdibleCarcassBiomassAtTheVictimPosition()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(new SimulationConfig(
                42,
                0,
                defaults.Schedule,
                founderProfile: FounderProfile.PredationVariation));
            CreatureId victim = world.Spawn(Genome.Neutral);
            world.SetCreaturePosition(victim, new SimVector2(3f, -2f));

            world.RequestDeath(victim, DeathCause.Predation);
            world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.CreatureCount, Is.EqualTo(0));
            Assert.That(world.Resources.Count, Is.EqualTo(1));
            ResourceState carcass = world.Resources.GetAt(0);
            Assert.That(carcass.Kind, Is.EqualTo(ResourceKind.Carcass));
            Assert.That(carcass.Position.X, Is.EqualTo(3f));
            Assert.That(carcass.Position.Y, Is.EqualTo(-2f));
            Assert.That(carcass.Amount, Is.GreaterThan(0f));
        }

        [Test]
        public void HungryMeatAdaptedCreatureCanSelectAndConsumeAVisibleCarcass()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(new SimulationConfig(
                42,
                0,
                defaults.Schedule,
                founderProfile: FounderProfile.PredationVariation));
            CreatureId creature = world.Spawn(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;
            world.Resources.Add(ResourceKind.Carcass, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f);

            for (int index = 0; index < 10; index++) world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.GetCreatureDecisionAt(0).Action, Is.EqualTo(CreatureAction.FeedCarcass));
            Assert.That(world.GetCreatureNeedsAt(0).Energy, Is.GreaterThan(0f));
            for (int index = 0; index < 10; index++) world.Step(world.Config.FixedDeltaTime);
            Assert.That(world.Statistics.CumulativeCarcassConsumed, Is.GreaterThan(0f));
        }

        [Test]
        public void WorldRejectsVariableStepDeltas()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);

            Assert.That(
                () => world.Step(config.FixedDeltaTime * 0.5f),
                Throws.ArgumentException);
        }

        [Test]
        public void WorldSpawnsCreatureWithStableLookup()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);

            CreatureId creature = world.Spawn();

            Assert.That(world.CreatureCount, Is.EqualTo(1));
            Assert.That(world.TryGetCreatureIndex(creature, out int index), Is.True);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void WorldTicksNeedsOnlyAtTheConfiguredFrequency()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            CreatureNeeds before = world.GetCreatureNeedsAt(0);

            for (int index = 0; index < 9; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.GetCreatureNeedsAt(0).Energy, Is.EqualTo(before.Energy));

            world.Step(config.FixedDeltaTime);

            CreatureNeeds after = world.GetCreatureNeedsAt(0);
            Assert.That(after.Energy, Is.LessThan(before.Energy));
            Assert.That(after.Age, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void WorldRegeneratesResourcesOnlyAtTheConfiguredFrequency()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            world.Resources.Add(
                ResourceKind.Water,
                new SimVector2(0f, 0f),
                interactionRadius: 1f,
                initialAmount: 0f,
                capacity: 10f,
                regenerationPerSecond: 2f);

            for (int index = 0; index < 19; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Resources.GetAt(0).Amount, Is.EqualTo(0f));

            world.Step(config.FixedDeltaTime);

            Assert.That(world.Resources.GetAt(0).Amount, Is.EqualTo(2f));
        }

        [Test]
        public void WorldPlacesFoundersDeterministicallyAndMovesThemAtTheBaseFrequency()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);
            MovementState initial = first.GetCreatureMovementAt(0);

            first.Step(config.FixedDeltaTime);
            second.Step(config.FixedDeltaTime);

            MovementState after = first.GetCreatureMovementAt(0);
            Assert.That(initial.Position.X, Is.EqualTo(second.GetCreatureMovementAt(0).PreviousPosition.X));
            Assert.That(initial.Position.Y, Is.EqualTo(second.GetCreatureMovementAt(0).PreviousPosition.Y));
            Assert.That(after.Position.X, Is.EqualTo(second.GetCreatureMovementAt(0).Position.X));
            Assert.That(after.Position.Y, Is.EqualTo(second.GetCreatureMovementAt(0).Position.Y));
            Assert.That(SimVector2.Distance(after.Position, initial.Position), Is.GreaterThan(0f));
        }

        [Test]
        public void WorldRefreshesCreatureDecisionsAtTheConfiguredFrequency()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);

            for (int index = 0; index < 9; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.GetCreatureDecisionAt(0).DecisionTick, Is.EqualTo(-1));

            world.Step(config.FixedDeltaTime);

            Assert.That(world.GetCreatureDecisionAt(0).DecisionTick, Is.EqualTo(10));
        }

        [Test]
        public void WorldRetainsDiagnosticsFromEachCreatureDecision()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn();
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Hydration = 0f;
            world.Resources.Add(ResourceKind.Water, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f);

            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            DecisionDiagnostics diagnostics = world.GetCreatureDecisionDiagnosticsAt(0);
            Assert.That(diagnostics.WaterVisible, Is.True);
            Assert.That(diagnostics.WaterScore, Is.GreaterThan(0f));
        }

        [Test]
        public void CognitionEnabledWorldStoresSeenResourcesAndLetsUnrefreshedMemoryDecay()
        {
            var world = new SimulationWorld(SimulationConfig.CreatePrototype2Defaults(42, 0));
            CreatureId creature = world.Spawn();
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            ResourceId food = world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f);

            for (int index = 0; index < 10; index++) world.Step(world.Config.FixedDeltaTime);

            MemoryState observed = world.GetCreatureMemoryAt(0);
            Assert.That(observed.FoodConfidence, Is.EqualTo(0.94f).Within(0.0001f));
            Assert.That(observed.FoodPosition.X, Is.EqualTo(0f));
            world.Resources.SetActive(food, false);

            for (int index = 0; index < 10; index++) world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.GetCreatureMemoryAt(0).FoodConfidence, Is.LessThan(1f));
            Assert.That(world.GetCreatureMemoryAt(0).FoodAge, Is.GreaterThan(0f));
        }

        [Test]
        public void VisibleFoodBindsRememberedSeekIntentToAnExecutableEatAction()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 1);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 80f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f);
            ref MemoryState memory = ref world.Creatures.GetMemoryRefAt(0);
            memory.FoodPosition = new SimVector2(0f, 0f);
            memory.FoodConfidence = 1f;
            memory.FoodOutcomeValue = 1f;
            memory.FoodExperienceCount = 1;

            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            CreatureDecision decision = world.GetCreatureDecisionAt(0);
            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Eat));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(0));
            Assert.That(world.Resources.GetAt(0).Amount, Is.LessThan(10f));
        }

        [Test]
        public void CognitionEnabledWandererStaysNearItsRememberedPairedResourceHome()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 1);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 100f, 100f, 0f);
            world.Resources.Add(ResourceKind.Water, new SimVector2(0f, 0f), 2f, 100f, 100f, 0f);
            ref MemoryState memory = ref world.Creatures.GetMemoryRefAt(0);
            memory.FoodPosition = new SimVector2(0f, 0f);
            memory.FoodConfidence = 1f;
            memory.WaterPosition = new SimVector2(0f, 0f);
            memory.WaterConfidence = 1f;

            for (int index = 0; index < 200; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            MovementState movement = world.GetCreatureMovementAt(0);
            Assert.That(SimVector2.Distance(movement.Position, new SimVector2(0f, 0f)), Is.LessThan(6f));
        }

        [Test]
        public void HungryCreatureConsumesFoodAfterReachingItsSelectedResource()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            world.Spawn();
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;
            ref MovementState movement = ref world.Creatures.GetMovementRefAt(0);
            movement = new MovementState(new SimVector2(0f, 0f));
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 5f, 10f, 10f, 0f);

            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.GetCreatureDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));
            Assert.That(world.GetCreatureNeedsAt(0).Energy, Is.GreaterThan(0f));
            Assert.That(world.Resources.GetAt(0).Amount, Is.LessThan(10f));
        }

        [Test]
        public void HungryCreatureAtFoodPatchReportsEatingRatherThanSeeking()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn();
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 5f, 10f, 10f, 0f);

            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.GetCreatureDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));
        }

        [Test]
        public void CreatureWithNoHealthDiesAtTheEndOfTheScheduledNeedsStep()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;
            needs.Hydration = 0f;
            needs.Health = 1f;

            for (int index = 0; index < 9; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(1));

            world.Step(config.FixedDeltaTime);

            Assert.That(world.CreatureCount, Is.EqualTo(0));
        }

        [Test]
        public void StatisticsReportDehydrationAsTheCauseWhenBothCoreNeedsAreEmpty()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;
            needs.Hydration = 0f;
            needs.Health = 1f;

            for (int index = 0; index < 20; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Statistics.DeathCount, Is.EqualTo(1));
            Assert.That(world.Statistics.DehydrationDeathCount, Is.EqualTo(1));
            Assert.That(world.Statistics.StarvationDeathCount, Is.EqualTo(0));
        }

        [Test]
        public void TwoReadyNearbyParentsCreateOneDeterministicChildOnReproductionTick()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.SetCreaturePosition(first, new SimVector2(0f, 0f));
            world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
            world.Creatures.GetNeedsRefAt(0).Age = 20f;
            world.Creatures.GetNeedsRefAt(1).Age = 20f;

            for (int index = 0; index < 20; index++)
            {
                world.SetCreaturePosition(first, new SimVector2(0f, 0f));
                world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(3));
            CreatureLineage lineage = world.Creatures.GetLineageAt(2);
            Assert.That(lineage.FirstParent, Is.EqualTo(first));
            Assert.That(lineage.SecondParent, Is.EqualTo(second));
            Assert.That(lineage.Generation, Is.EqualTo(1));
        }

        [Test]
        public void UtilityPolicyHealthyAdultsSeekAndMeetBeforePairwiseReproduction()
        {
            SimulationConfig legacy = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var config = new SimulationConfig(
                legacy.WorldSeed,
                0,
                legacy.Schedule,
                legacy.MaximumPopulation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.SetCreaturePosition(first, new SimVector2(0f, 0f));
            world.SetCreaturePosition(second, new SimVector2(1.5f, 0f));
            world.Creatures.GetNeedsRefAt(0).Age = 21f;
            world.Creatures.GetNeedsRefAt(1).Age = 21f;

            for (int index = 0; index < 20; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(3));
            Assert.That(world.Events.Count, Is.EqualTo(1));
            Assert.That(world.Events.GetAt(0).Kind, Is.EqualTo(SimulationEventKind.Birth));
        }

        [Test]
        public void SeparateReadyPairsEachProduceOneChildOnTheSameReproductionTick()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            CreatureId third = world.Spawn();
            CreatureId fourth = world.Spawn();
            CreatureId[] founders = { first, second, third, fourth };

            for (int index = 0; index < founders.Length; index++)
            {
                world.Creatures.GetNeedsRefAt(index).Age = 20f;
            }

            for (int tick = 0; tick < 20; tick++)
            {
                for (int index = 0; index < founders.Length; index++)
                {
                    world.SetCreaturePosition(founders[index], new SimVector2(index * 0.4f, 0f));
                }

                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(6));
        }

        [Test]
        public void NewbornParentsCannotReproduceBeforeMaturity()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.SetCreaturePosition(first, new SimVector2(0f, 0f));
            world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));

            for (int index = 0; index < 20; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(2));
        }

        [Test]
        public void SuccessfulParentsRespectTheirReproductionCooldown()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.Creatures.GetNeedsRefAt(0).Age = 20f;
            world.Creatures.GetNeedsRefAt(1).Age = 20f;

            for (int index = 0; index < 20; index++)
            {
                world.SetCreaturePosition(first, new SimVector2(0f, 0f));
                world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(3));

            for (int index = 0; index < 20; index++)
            {
                world.SetCreaturePosition(first, new SimVector2(0f, 0f));
                world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(3));
        }

        [Test]
        public void WorldPublishesPopulationStatisticsAtTheConfiguredFrequency()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 2);
            var world = new SimulationWorld(config);

            for (int index = 0; index < 19; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Statistics.Tick, Is.EqualTo(0));

            world.Step(config.FixedDeltaTime);

            Assert.That(world.Statistics.Tick, Is.EqualTo(20));
            Assert.That(world.Statistics.Population, Is.GreaterThanOrEqualTo(2));
            float founderMean = (world.Creatures.GetGenomeAt(0).BodySize + world.Creatures.GetGenomeAt(1).BodySize) * 0.5f;
            Assert.That(world.Statistics.MeanBodySizeGene, Is.EqualTo(founderMean).Within(0.001f));
            float founderWaterMean = (world.Creatures.GetGenomeAt(0).WaterEfficiency + world.Creatures.GetGenomeAt(1).WaterEfficiency) * 0.5f;
            Assert.That(world.Statistics.MeanWaterEfficiencyGene, Is.EqualTo(founderWaterMean).Within(0.001f));
        }

        [Test]
        public void EqualWorldsProduceTheSameStateHashAfterEqualSteps()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 3);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);

            first.Step(config.FixedDeltaTime);
            second.Step(config.FixedDeltaTime);

            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void StateHashChangesWhenAuthoritativeCreaturePositionChanges()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);
            ref MovementState movement = ref second.Creatures.GetMovementRefAt(0);
            movement.Position = new SimVector2(12f, -3f);

            Assert.That(second.ComputeStateHash(), Is.Not.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void StateHashChangesWhenReproductionCooldownChanges()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);
            second.Creatures.GetReproductionRefAt(0).CooldownRemaining = 5f;

            Assert.That(second.ComputeStateHash(), Is.Not.EqualTo(first.ComputeStateHash()));
        }

        [Test]
        public void EventBufferRetainsEventsInOrderAndReportsOverflow()
        {
            var events = new SimulationEventBuffer(capacity: 1);
            var birth = new SimulationEvent(
                tick: 10,
                kind: SimulationEventKind.Birth,
                subject: new CreatureId(3),
                firstRelated: new CreatureId(1),
                secondRelated: new CreatureId(2),
                deathCause: DeathCause.Debug);

            Assert.That(events.TryWrite(birth), Is.True);
            Assert.That(events.TryWrite(birth), Is.False);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Overflowed, Is.True);
            Assert.That(events.GetAt(0).Subject, Is.EqualTo(new CreatureId(3)));
        }

        [Test]
        public void WorldEmitsBirthAndAppliedDeathEvents()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.Creatures.GetNeedsRefAt(0).Age = 20f;
            world.Creatures.GetNeedsRefAt(1).Age = 20f;

            for (int index = 0; index < 20; index++)
            {
                world.SetCreaturePosition(first, new SimVector2(0f, 0f));
                world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Events.Count, Is.EqualTo(1));
            SimulationEvent birth = world.Events.GetAt(0);
            Assert.That(birth.Kind, Is.EqualTo(SimulationEventKind.Birth));
            Assert.That(birth.FirstRelated, Is.EqualTo(first));
            Assert.That(birth.SecondRelated, Is.EqualTo(second));
            Assert.That(world.Statistics.BirthCount, Is.EqualTo(1));
            Assert.That(world.Statistics.DeathCount, Is.EqualTo(0));

            world.Events.Clear();
            world.RequestDeath(birth.Subject, DeathCause.Debug);
            world.Step(config.FixedDeltaTime);

            Assert.That(world.Events.Count, Is.EqualTo(1));
            SimulationEvent death = world.Events.GetAt(0);
            Assert.That(death.Kind, Is.EqualTo(SimulationEventKind.Death));
            Assert.That(death.Subject, Is.EqualTo(birth.Subject));
            Assert.That(death.DeathCause, Is.EqualTo(DeathCause.Debug));

            for (int index = 0; index < 19; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Statistics.DeathCount, Is.EqualTo(1));
        }

        [Test]
        public void PopulationSafetyLimitPreventsAdditionalBirths()
        {
            var schedule = new SimulationSchedule(20, 20, 4, 2, 2, 1, 1, 1);
            var config = new SimulationConfig(42, initialPopulation: 0, schedule, maximumPopulation: 2);
            var world = new SimulationWorld(config);
            CreatureId first = world.Spawn();
            CreatureId second = world.Spawn();
            world.Creatures.GetNeedsRefAt(0).Age = 20f;
            world.Creatures.GetNeedsRefAt(1).Age = 20f;

            for (int index = 0; index < 20; index++)
            {
                world.SetCreaturePosition(first, new SimVector2(0f, 0f));
                world.SetCreaturePosition(second, new SimVector2(0.5f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(2));
            Assert.That(world.Statistics.BirthCount, Is.EqualTo(0));
        }

        [Test]
        public void SameSeedCreatesMatchingButVariedFounderGenomes()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 4);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);

            Assert.That(second.Creatures.GetGenomeAt(0).BodySize, Is.EqualTo(first.Creatures.GetGenomeAt(0).BodySize));
            Assert.That(second.Creatures.GetGenomeAt(3).WaterEfficiency, Is.EqualTo(first.Creatures.GetGenomeAt(3).WaterEfficiency));
            Assert.That(first.Creatures.GetGenomeAt(0).BodySize, Is.Not.EqualTo(first.Creatures.GetGenomeAt(1).BodySize));
        }
    }
}

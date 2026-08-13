using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
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

            Assert.That(world.GetCreatureDecisionAt(0).Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(world.GetCreatureNeedsAt(0).Energy, Is.GreaterThan(0f));
            Assert.That(world.Resources.GetAt(0).Amount, Is.LessThan(10f));
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
        public void TwoReadyNearbyParentsCreateOneDeterministicChildOnReproductionTick()
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

            Assert.That(world.CreatureCount, Is.EqualTo(3));
            CreatureLineage lineage = world.Creatures.GetLineageAt(2);
            Assert.That(lineage.FirstParent, Is.EqualTo(first));
            Assert.That(lineage.SecondParent, Is.EqualTo(second));
            Assert.That(lineage.Generation, Is.EqualTo(1));
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
            Assert.That(world.Statistics.MeanBodySizeGene, Is.EqualTo(0.5f).Within(0.001f));
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
    }
}

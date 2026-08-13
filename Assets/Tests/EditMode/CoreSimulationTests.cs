using System;
using LifeSimulation.Simulation.Biology;
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
        public void SwapBackRemovalKeepsBiologyAlignedWithTheMovedCreature()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId first = store.Add(new Genome(0f, 0f, 0f, 0f, 0f, 0f));
            CreatureId moved = store.Add(new Genome(1f, 1f, 1f, 1f, 1f, 1f));

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);
            Assert.That(store.GetGenomeAt(movedIndex).BodySize, Is.EqualTo(1f));
            Assert.That(store.GetPhenotypeAt(movedIndex).EnergyCapacity, Is.GreaterThan(100f));
            Assert.That(store.GetNeedsAt(movedIndex).Energy, Is.EqualTo(store.GetPhenotypeAt(movedIndex).EnergyCapacity));
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
        public void EqualWorldsProduceTheSameStateHashAfterEqualSteps()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 3);
            var first = new SimulationWorld(config);
            var second = new SimulationWorld(config);

            first.Step(config.FixedDeltaTime);
            second.Step(config.FixedDeltaTime);

            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));
        }
    }
}

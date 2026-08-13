using System;
using LifeSimulation.Simulation.Core;
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
    }
}

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
    }
}

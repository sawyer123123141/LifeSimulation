using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class SimulationConfigTests
    {
        [Test]
        public void PredationEconomicsEnabledDefaultsToFalseAndCanBeSetToTrue()
        {
            var schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var defaultConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule);
            var enabledConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule, predationEconomicsEnabled: true);

            Assert.That(defaultConfig.PredationEconomicsEnabled, Is.False);
            Assert.That(enabledConfig.PredationEconomicsEnabled, Is.True);
        }
    }
}

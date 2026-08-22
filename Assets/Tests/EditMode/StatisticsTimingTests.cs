using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Statistics are a reporting surface, and every experiment in docs/experiments reads them.
    /// A sample that is taken before the tick's deaths are committed reports a population that
    /// includes creatures the same tick already killed, and a death count that excludes them.
    /// </summary>
    public sealed class StatisticsTimingTests
    {
        private const int StatisticsIntervalTicks = 20;

        [Test]
        public void ADeathOnAStatisticsTickIsVisibleInThatTicksSample()
        {
            SimulationWorld world = CreateWorld();
            StepTo(world, StatisticsIntervalTicks - 1);
            int populationBefore = world.CreatureCount;
            world.Creatures.GetNeedsRefAt(0).Health = 0f;

            world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.CurrentTick, Is.EqualTo(StatisticsIntervalTicks));
            Assert.That(world.CreatureCount, Is.EqualTo(populationBefore - 1), "the creature must actually have died on this tick");
            Assert.That(world.Statistics.Tick, Is.EqualTo(StatisticsIntervalTicks));
            Assert.That(world.Statistics.Population, Is.EqualTo(world.CreatureCount),
                "the sample must not count a creature this tick already removed");
            Assert.That(world.Statistics.DeathCount, Is.EqualTo(1),
                "the sample must include the death that happened on the tick it reports");
        }

        [Test]
        public void ExtinctionOnAStatisticsTickReportsAnEmptyPopulation()
        {
            SimulationWorld world = CreateWorld();
            StepTo(world, StatisticsIntervalTicks - 1);
            int populationBefore = world.CreatureCount;
            for (int index = 0; index < world.CreatureCount; index++)
            {
                world.Creatures.GetNeedsRefAt(index).Health = 0f;
            }

            world.Step(world.Config.FixedDeltaTime);

            Assert.That(world.CreatureCount, Is.EqualTo(0));
            Assert.That(world.Statistics.Population, Is.EqualTo(0), "an extinction must not be reported as a surviving population");
            Assert.That(world.Statistics.DeathCount, Is.EqualTo(populationBefore));
        }

        [Test]
        public void CaptureStatisticsReportsTheCurrentTickWhenTheRunEndsOffCadence()
        {
            SimulationWorld world = CreateWorld();
            StepTo(world, StatisticsIntervalTicks + 5);

            Assert.That(world.Statistics.Tick, Is.EqualTo(StatisticsIntervalTicks),
                "the cached sample is only refreshed on cadence ticks");

            SimulationStatistics current = world.CaptureStatistics();

            Assert.That(current.Tick, Is.EqualTo(world.CurrentTick));
            Assert.That(current.Population, Is.EqualTo(world.CreatureCount));
        }

        [Test]
        public void CaptureStatisticsAtTickZeroReflectsAnAppliedScenario()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ObservationStable.ApplyTo(world);

            Assert.That(world.Statistics.AvailableFood, Is.EqualTo(0f),
                "the constructor-time sample predates the scenario and is stale by construction");

            SimulationStatistics current = world.CaptureStatistics();

            Assert.That(current.Tick, Is.EqualTo(0));
            Assert.That(current.AvailableFood, Is.GreaterThan(0f), "the scenario's food must be visible without stepping first");
            Assert.That(current.Population, Is.EqualTo(world.CreatureCount));
        }

        [Test]
        public void ExperimentRunnerReportsTheEndOfRunStateRatherThanTheLastCadenceSample()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            ExperimentResult result = ExperimentRunner.Run(config, Prototype4Scenarios.ObservationStable, ticks: StatisticsIntervalTicks + 5);

            Assert.That(result.CompletedTicks, Is.EqualTo(StatisticsIntervalTicks + 5));
            Assert.That(result.FinalStatistics.Tick, Is.EqualTo(result.CompletedTicks),
                "an experiment's reported statistics must describe the tick the run actually ended on");
        }

        [Test]
        public void CaptureStatisticsRefusesToSampleWhileADeathIsQueued()
        {
            SimulationWorld world = CreateWorld();
            StepTo(world, 5);
            world.RequestDeath(world.GetCreatureIdAt(0), DeathCause.Starvation);

            Assert.That(
                () => world.CaptureStatistics(),
                Throws.InstanceOf<System.InvalidOperationException>(),
                "sampling between RequestDeath and the next Step would report the pending state this method exists to exclude");
        }

        [Test]
        public void CaptureStatisticsSucceedsOnceTheQueuedDeathIsCommitted()
        {
            SimulationWorld world = CreateWorld();
            StepTo(world, 5);
            int populationBefore = world.CreatureCount;
            world.RequestDeath(world.GetCreatureIdAt(0), DeathCause.Starvation);

            world.Step(world.Config.FixedDeltaTime);
            SimulationStatistics current = world.CaptureStatistics();

            Assert.That(world.CreatureCount, Is.EqualTo(populationBefore - 1));
            Assert.That(current.Population, Is.EqualTo(world.CreatureCount));
            Assert.That(current.DeathCount, Is.EqualTo(1));
        }

        private static SimulationWorld CreateWorld()
        {
            return new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4));
        }

        private static void StepTo(SimulationWorld world, long tick)
        {
            while (world.CurrentTick < tick)
            {
                world.Step(world.Config.FixedDeltaTime);
                world.Events.Clear();
            }
        }
    }
}

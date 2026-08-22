using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class P5HistoryPanelSessionTests
    {
        [Test]
        public void AdvanceRecordsEventsBeforeTheHostBufferIsCleared()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);
            world.Step(world.Config.FixedDeltaTime);
            Assert.That(world.Events.TryWrite(new SimulationEvent(
                world.CurrentTick,
                SimulationEventKind.Birth,
                new CreatureId(1000),
                default,
                default,
                DeathCause.None)), Is.True);

            session.Advance(world);

            Assert.That(world.Events.Count, Is.EqualTo(1));
            Assert.That(session.AncestryCompleteThroughTick, Is.EqualTo(world.CurrentTick));
        }

        [Test]
        public void ObservationCadenceCapturesAFullPopulationAtTheDeclaredTick()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);

            StepAndAdvance(world, session, P5HistoryPanelSession.ObservationIntervalTicks);

            Assert.That(session.ObservationCount, Is.EqualTo(1));
            Assert.That(session.LastObservationWasSampled, Is.False);
            Assert.That(session.NextObservationTick, Is.EqualTo(P5HistoryPanelSession.ObservationIntervalTicks * 2));
        }

        [Test]
        public void FreshSessionCannotCarryTracksOrDisplayedEventsAcrossAReset()
        {
            SimulationWorld firstWorld = CreateWorld();
            P5HistoryPanelSession first = P5HistoryPanelSession.CreateForWorld(firstWorld);
            StepAndAdvance(firstWorld, first, P5HistoryPanelSession.ObservationIntervalTicks * 2);

            SimulationWorld resetWorld = CreateWorld();
            P5HistoryPanelSession reset = P5HistoryPanelSession.CreateForWorld(resetWorld);

            Assert.That(reset.DisplayEventCount, Is.EqualTo(0));
            Assert.That(reset.ObservationCount, Is.EqualTo(0));
            Assert.That(reset.StatusText, Does.Not.Contain("species"));
        }

        [Test]
        public void SessionAnalysisDoesNotChangeTheSimulationHash()
        {
            SimulationWorld baseline = CreateWorld();
            SimulationWorld observed = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(observed);

            for (int step = 0; step < P5HistoryPanelSession.ObservationIntervalTicks * 2; step++)
            {
                baseline.Step(baseline.Config.FixedDeltaTime);
                observed.Step(observed.Config.FixedDeltaTime);
                session.Advance(observed);
                baseline.Events.Clear();
                observed.Events.Clear();
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
        }

        private static SimulationWorld CreateWorld()
        {
            return new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 73, initialPopulation: 4));
        }

        private static void StepAndAdvance(SimulationWorld world, P5HistoryPanelSession session, int count)
        {
            for (int step = 0; step < count; step++)
            {
                world.Step(world.Config.FixedDeltaTime);
                session.Advance(world);
                world.Events.Clear();
            }
        }
    }
}

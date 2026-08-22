using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class P5HistoryPanelSessionTests
    {
        [Test]
        public void AdvanceRecordsOverflowedHostEventsAsIncompleteAncestryEvidence()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);
            int writtenEventCount = OverflowHostEvents(world);

            session.Advance(world);

            Assert.That(world.Events.Overflowed, Is.True);
            Assert.That(world.Events.Count, Is.EqualTo(writtenEventCount));
            Assert.That(session.AncestryCompleteThroughTick, Is.EqualTo(-1));
            Assert.That(session.AncestryIsComplete, Is.False);
            Assert.That(session.StatusText, Does.Contain("incomplete"));
        }

        [Test]
        public void SessionStatusMakesOutputOverflowVisibleWithoutInventingHistory()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world, outputCapacity: 1);

            AdvanceAtNextObservationWithIncompleteAncestry(world, session);
            AdvanceAtNextObservationWithIncompleteAncestry(world, session);

            Assert.That(session.OutputOverflowed, Is.True);
            Assert.That(session.StatusText, Does.Contain("dropped"));
            Assert.That(session.StatusText, Does.Not.Contain("species"));
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
        public void MissedObservationCadenceIsVisibleInsteadOfCreatingAnOutOfCadenceSnapshot()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);

            for (int step = 0; step < P5HistoryPanelSession.ObservationIntervalTicks + 1; step++)
            {
                world.Step(world.Config.FixedDeltaTime);
            }

            session.Advance(world);

            Assert.That(session.ObservationCount, Is.EqualTo(0));
            Assert.That(session.NextObservationTick, Is.EqualTo(P5HistoryPanelSession.ObservationIntervalTicks * 2));
            Assert.That(session.StatusText, Does.Contain("missed"));
        }

        [Test]
        public void FreshSessionCannotCarryTracksOrDisplayedEventsAcrossAReset()
        {
            SimulationWorld firstWorld = CreateWorld();
            P5HistoryPanelSession first = P5HistoryPanelSession.CreateForWorld(firstWorld);
            OverflowHostEvents(firstWorld);
            first.Advance(firstWorld);
            firstWorld.Events.Clear();
            StepAndAdvance(firstWorld, first, P5HistoryPanelSession.ObservationIntervalTicks * 2);
            Assert.That(first.DisplayEventCount, Is.GreaterThan(0));

            SimulationWorld resetWorld = CreateWorld();
            P5HistoryPanelSession reset = P5HistoryPanelSession.CreateForWorld(resetWorld);

            Assert.That(reset.DisplayEventCount, Is.EqualTo(0));
            Assert.That(reset.ObservationCount, Is.EqualTo(0));
            Assert.That(reset.StatusText, Does.Not.Contain("species"));
        }

        [Test]
        public void RoutineConfirmedContinuityIsHiddenFromThePanelButKeptInTheHistory()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);

            StepAndAdvance(world, session, P5HistoryPanelSession.ObservationIntervalTicks * 6);

            int routineCount = 0;
            for (int index = 0; index < session.DisplayEventCount; index++)
            {
                if (P5HistoryPanelSession.IsRoutineContinuity(session.GetEventAt(index)))
                {
                    routineCount++;
                }
            }

            Assert.That(routineCount, Is.GreaterThan(0), "the run must produce routine continuity for the filter to be exercised");
            Assert.That(session.HiddenRoutineContinuityCount, Is.EqualTo(routineCount));
            Assert.That(session.NotableEventCount, Is.EqualTo(session.DisplayEventCount - routineCount));
            for (int index = 0; index < session.NotableEventCount; index++)
            {
                Assert.That(P5HistoryPanelSession.IsRoutineContinuity(session.GetNotableEventAt(index)), Is.False);
            }
        }

        [Test]
        public void NotableRecordsKeepTheirHistoryOrderAndUnresolvedContinuityStaysVisible()
        {
            SimulationWorld world = CreateWorld();
            P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);
            AdvanceAtNextObservationWithIncompleteAncestry(world, session);
            StepAndAdvance(world, session, P5HistoryPanelSession.ObservationIntervalTicks * 5);

            Assert.That(session.NotableEventCount, Is.GreaterThan(0));
            long previousTick = long.MinValue;
            for (int index = 0; index < session.NotableEventCount; index++)
            {
                ClusterHistoryEvent notable = session.GetNotableEventAt(index);
                Assert.That(notable.FirstObservedTick, Is.GreaterThanOrEqualTo(previousTick));
                previousTick = notable.FirstObservedTick;
                Assert.That(
                    notable.Kind != ClusterHistoryEventKind.Continuity
                        || notable.Status != ClusterHistoryEventStatus.Confirmed,
                    Is.True);
            }
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

        private static void AdvanceAtNextObservationWithIncompleteAncestry(SimulationWorld world, P5HistoryPanelSession session)
        {
            int stepsBeforeObservation = (int)(session.NextObservationTick - world.CurrentTick - 1);
            StepAndAdvance(world, session, stepsBeforeObservation);
            world.Step(world.Config.FixedDeltaTime);
            OverflowHostEvents(world);
            session.Advance(world);
            world.Events.Clear();
        }

        private static int OverflowHostEvents(SimulationWorld world)
        {
            int eventIndex = 0;
            while (world.Events.TryWrite(new SimulationEvent(
                world.CurrentTick,
                SimulationEventKind.Birth,
                new CreatureId(1000 + eventIndex),
                default,
                default,
                DeathCause.None)))
            {
                eventIndex++;
            }

            return eventIndex;
        }
    }
}

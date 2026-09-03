using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class LifeHistoryLedgerTests
    {
        [Test]
        public void FoundersRecordedAtTickZeroCarryTheirGenomeAndNoParents()
        {
            var creatures = new CreatureStore(2);
            CreatureId founder = creatures.Add(new Genome(.25f, .5f, .5f, .5f, .5f, .5f));
            var ledger = new LifeHistoryLedger();

            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            Assert.That(ledger.TryGet(founder, out LifeHistoryRecord record), Is.True);
            Assert.That(record.BirthTick, Is.EqualTo(0));
            Assert.That(record.FirstParent, Is.EqualTo(default(CreatureId)));
            Assert.That(record.SecondParent, Is.EqualTo(default(CreatureId)));
            Assert.That(record.HasGenome, Is.True);
            Assert.That(record.Genome.BodySize, Is.EqualTo(.25f));
        }

        [Test]
        public void ACreatureBornDuringTheRunHasItsGenomeCapturedOnTheFirstObservationAfterItsBirth()
        {
            var creatures = new CreatureStore(2);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);

            CreatureId child = creatures.Add(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: .75f));
            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(40, SimulationEventKind.Birth, child, new CreatureId(101), new CreatureId(102), DeathCause.None));
            ledger.RecordCompleteBatch(events, 40);

            Assert.That(ledger.TryGet(child, out LifeHistoryRecord beforeObservation), Is.True);
            Assert.That(beforeObservation.HasGenome, Is.False);

            ledger.Observe(creatures);

            Assert.That(ledger.TryGet(child, out LifeHistoryRecord afterObservation), Is.True);
            Assert.That(afterObservation.BirthTick, Is.EqualTo(40));
            Assert.That(afterObservation.HasGenome, Is.True);
            Assert.That(afterObservation.Genome.LifespanTendency, Is.EqualTo(.75f));
        }

        [Test]
        public void ADeadCreatureKeepsTheGenomeCapturedWhileItWasAlive()
        {
            var creatures = new CreatureStore(2);
            CreatureId founder = creatures.Add(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, defense: .8f));
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            creatures.Remove(founder);
            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(120, SimulationEventKind.Death, founder, default, default, DeathCause.Starvation));
            ledger.RecordCompleteBatch(events, 120);
            ledger.Observe(creatures);

            Assert.That(ledger.TryGet(founder, out LifeHistoryRecord record), Is.True);
            Assert.That(record.DeathTick, Is.EqualTo(120));
            Assert.That(record.DeathCause, Is.EqualTo(DeathCause.Starvation));
            Assert.That(record.IsAlive, Is.False);
            Assert.That(record.HasGenome, Is.True);
            Assert.That(record.Genome.Defense, Is.EqualTo(.8f));
        }

        [Test]
        public void TheStillAliveSentinelIsAZeroDeathTickWithNoCauseAndNothingElse()
        {
            // AncestryHistory writes deathTick 0 / DeathCause.None for a creature that has not died,
            // so "died at tick 0" is not representable. Deaths are only ever drained from events, and
            // no death event can precede the first Step, which makes the ambiguity unreachable rather
            // than merely unlikely. This test documents the sentinel; it does not make it safe.
            var creatures = new CreatureStore(2);
            CreatureId founder = creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            Assert.That(ledger.TryGet(founder, out LifeHistoryRecord alive), Is.True);
            Assert.That(alive.DeathTick, Is.EqualTo(0));
            Assert.That(alive.DeathCause, Is.EqualTo(DeathCause.None));
            Assert.That(alive.IsAlive, Is.True);

            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(0, SimulationEventKind.Death, founder, default, default, DeathCause.Age));
            ledger.RecordCompleteBatch(events, 0);

            Assert.That(ledger.TryGet(founder, out LifeHistoryRecord killedAtTickZero), Is.True);
            Assert.That(killedAtTickZero.IsAlive, Is.False, "A non-None cause is what distinguishes a death at tick 0 from the sentinel.");
        }

        [Test]
        public void OffspringCreditedIsReadFromTheRecordedPedigreeRatherThanACounterOfItsOwn()
        {
            var creatures = new CreatureStore(4);
            CreatureId firstParent = creatures.Add(Genome.Neutral);
            CreatureId secondParent = creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(30, SimulationEventKind.Birth, new CreatureId(90), firstParent, secondParent, DeathCause.None));
            events.TryWrite(new SimulationEvent(30, SimulationEventKind.Birth, new CreatureId(91), firstParent, secondParent, DeathCause.None));
            ledger.RecordCompleteBatch(events, 30);

            // Both parents are credited for the same birth. That is correct for this monoecious
            // model and is why population-mean offspring sits near two at replacement.
            Assert.That(ledger.OffspringCredited(firstParent), Is.EqualTo(2));
            Assert.That(ledger.OffspringCredited(secondParent), Is.EqualTo(2));
            Assert.That(ledger.TryGet(firstParent, out LifeHistoryRecord record), Is.True);
            Assert.That(record.OffspringCredited, Is.EqualTo(2));
        }

        [Test]
        public void ObservationOrderIsStableAndDoesNotDependOnDictionaryIteration()
        {
            var creatures = new CreatureStore(4);
            CreatureId first = creatures.Add(Genome.Neutral);
            CreatureId second = creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);
            ledger.Observe(creatures);

            Assert.That(ledger.Count, Is.EqualTo(2));
            Assert.That(ledger.GetIdAt(0), Is.EqualTo(first));
            Assert.That(ledger.GetIdAt(1), Is.EqualTo(second));
        }

        [Test]
        public void AnOverflowedBatchMakesTheLedgerPermanentlyIncomplete()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);

            var overflowed = new SimulationEventBuffer(1);
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(50), new CreatureId(1), default, DeathCause.None));
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(51), new CreatureId(1), default, DeathCause.None));
            Assert.That(overflowed.Overflowed, Is.True);

            ledger.RecordCompleteBatch(overflowed, 10);
            Assert.That(ledger.IsComplete, Is.False);

            var clean = new SimulationEventBuffer(4);
            ledger.RecordCompleteBatch(clean, 20);
            Assert.That(ledger.IsComplete, Is.False, "Overflow is permanent; a later clean batch cannot restore completeness.");
        }

        [Test]
        public void CompletenessMirrorsTheUnderlyingAncestryWatermark()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();

            Assert.That(ledger.IsComplete, Is.False);
            Assert.That(ledger.CompleteThroughTick, Is.EqualTo(-1));

            ledger.RecordFounders(0, creatures);
            ledger.RecordCompleteBatch(new SimulationEventBuffer(4), 60);

            Assert.That(ledger.IsComplete, Is.True);
            Assert.That(ledger.CompleteThroughTick, Is.EqualTo(60));
        }

        [Test]
        public void EventBatchesBeforeFoundersAreRefused()
        {
            var ledger = new LifeHistoryLedger();

            Assert.Throws<InvalidOperationException>(() => ledger.RecordCompleteBatch(new SimulationEventBuffer(4), 10));
        }
    }
}

using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class IngestionLedgerTests
    {
        private static SimulationSchedule Schedule => SimulationConfig.CreatePrototype4Defaults(worldSeed: 1, initialPopulation: 1).Schedule;

        [Test]
        public void TheJoinCarriesLifetimeGrossStoredAndSurplusForEveryCohortMember()
        {
            var creatures = new CreatureStore(2);
            CreatureId founder = creatures.Add(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, dietSpecialization: .9f));
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);
            ledger.RecordCompleteBatch(new SimulationEventBuffer(1), 36_000);

            var recorder = new IngestionRecorder();
            recorder.Record(founder, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 2f, grossEnergy: 10f, storedEnergy: 6f);
            recorder.Record(founder, ResourceKind.Food, underStaleSeekAction: true, resourceAmount: 1f, grossEnergy: 5f, storedEnergy: 5f);
            recorder.Record(founder, ResourceKind.Carcass, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: 8f, storedEnergy: 2f);

            FitnessCohort cohort = FitnessCohort.Select(ledger, Schedule, 36_000, FitnessCohortKind.CompleteLife);
            IngestionLedger joined = IngestionLedger.Join(ledger, recorder, cohort);

            Assert.That(joined.Count, Is.EqualTo(1));
            Assert.That(joined.GetIdAt(0), Is.EqualTo(founder));
            Assert.That(joined.GetLifeAt(0).Genome.DietSpecialization, Is.EqualTo(.9f));
            Assert.That(joined.GrossEnergyAt(0, ResourceKind.Food), Is.EqualTo(15d).Within(1e-6));
            Assert.That(joined.StoredEnergyAt(0, ResourceKind.Food), Is.EqualTo(11d).Within(1e-6));
            Assert.That(joined.SurplusEnergyAt(0, ResourceKind.Food), Is.EqualTo(4d).Within(1e-6));
            Assert.That(joined.ResourceAmountAt(0, ResourceKind.Food), Is.EqualTo(3d).Within(1e-6));
            Assert.That(joined.FeedingTicksAt(0, ResourceKind.Food), Is.EqualTo(2));
            Assert.That(joined.GrossEnergyAt(0, ResourceKind.Carcass), Is.EqualTo(8d).Within(1e-6));
            Assert.That(joined.SurplusEnergyAt(0, ResourceKind.Carcass), Is.EqualTo(6d).Within(1e-6));
        }

        [Test]
        public void CreaturesOutsideTheCohortAreNotInTheJoinEvenWhenTheRecorderSawThemEat()
        {
            var creatures = new CreatureStore(4);
            CreatureId founder = creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            CreatureId lateBorn = creatures.Add(Genome.Neutral);
            var events = new SimulationEventBuffer(2);
            events.TryWrite(new SimulationEvent(35_000, SimulationEventKind.Birth, lateBorn, founder, default, DeathCause.None));
            ledger.RecordCompleteBatch(events, 35_000);
            ledger.Observe(creatures);
            ledger.RecordCompleteBatch(new SimulationEventBuffer(1), 36_000);

            var recorder = new IngestionRecorder();
            recorder.Record(founder, ResourceKind.Food, false, 1f, 10f, 10f);
            recorder.Record(lateBorn, ResourceKind.Food, false, 1f, 90f, 90f);

            FitnessCohort cohort = FitnessCohort.Select(ledger, Schedule, 36_000, FitnessCohortKind.CompleteLife);
            IngestionLedger joined = IngestionLedger.Join(ledger, recorder, cohort);

            Assert.That(joined.Count, Is.EqualTo(1));
            Assert.That(joined.GetIdAt(0), Is.EqualTo(founder));
            Assert.That(joined.TotalGrossEnergy(ResourceKind.Food), Is.EqualTo(10d).Within(1e-6), "The right-censored creature's ingestion must not leak into the total.");
        }

        [Test]
        public void SurplusAndStaleActionSharesAreReportedAsFractionsOfGross()
        {
            var creatures = new CreatureStore(2);
            CreatureId founder = creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);
            ledger.RecordCompleteBatch(new SimulationEventBuffer(1), 36_000);

            var recorder = new IngestionRecorder();
            recorder.Record(founder, ResourceKind.Food, underStaleSeekAction: true, resourceAmount: 1f, grossEnergy: 40f, storedEnergy: 40f);
            recorder.Record(founder, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: 60f, storedEnergy: 35f);

            FitnessCohort cohort = FitnessCohort.Select(ledger, Schedule, 36_000, FitnessCohortKind.CompleteLife);
            IngestionLedger joined = IngestionLedger.Join(ledger, recorder, cohort);

            Assert.That(joined.SurplusFraction(ResourceKind.Food), Is.EqualTo(.25d).Within(1e-6));
            Assert.That(joined.StaleActionGrossEnergyFraction(ResourceKind.Food), Is.EqualTo(.4d).Within(1e-6));
        }

        [Test]
        public void AnIncompleteLedgerIsRefusedRatherThanReportedOn()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            var complete = new LifeHistoryLedger();
            complete.RecordFounders(0, creatures);
            complete.Observe(creatures);
            complete.RecordCompleteBatch(new SimulationEventBuffer(1), 36_000);
            FitnessCohort cohort = FitnessCohort.Select(complete, Schedule, 36_000, FitnessCohortKind.CompleteLife);

            var incomplete = new LifeHistoryLedger();
            incomplete.RecordFounders(0, creatures);
            var overflowed = new SimulationEventBuffer(1);
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(60), new CreatureId(1), default, DeathCause.None));
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(61), new CreatureId(1), default, DeathCause.None));
            incomplete.RecordCompleteBatch(overflowed, 10);

            Assert.Throws<InvalidOperationException>(() => IngestionLedger.Join(incomplete, new IngestionRecorder(), cohort));
        }

        [Test]
        public void TheDeltaProxyReproducesTheRetiredInstrumentsAttributionRule()
        {
            var proxy = new EnergyDeltaProxy();
            var creatureId = new CreatureId(2);

            // Only a positive delta counts, and only while the action already reads Eat.
            proxy.ObserveCreature(creatureId, CreatureAction.Eat, energy: 10f);
            proxy.ObserveCreature(creatureId, CreatureAction.Eat, energy: 14f);
            proxy.ObserveCreature(creatureId, CreatureAction.Eat, energy: 12f);
            proxy.ObserveCreature(creatureId, CreatureAction.SeekFood, energy: 18f);

            Assert.That(proxy.PositiveDeltaEnergy(creatureId, ResourceKind.Food), Is.EqualTo(4d).Within(1e-6));
            Assert.That(proxy.ActionTicks(creatureId, ResourceKind.Food), Is.EqualTo(3), "The first observation has no predecessor but is still an Eat tick.");
            Assert.That(proxy.TotalPositiveDeltaEnergy(ResourceKind.Food), Is.EqualTo(4d).Within(1e-6));
        }

        [Test]
        public void TheProxyMissesIngestionTheRecorderSeesOverARealRun()
        {
            // The evidence for the Task 10 retraction: the retired instrument's number against a
            // measurement taken at the allocation site, in the same run.
            SimulationConfig config = SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 51, initialPopulation: 12);
            var world = new SimulationWorld(config) { Recorder = new IngestionRecorder() };
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            var proxy = new EnergyDeltaProxy();

            for (int step = 0; step < 3_000; step++)
            {
                world.Step(config.FixedDeltaTime);
                proxy.Observe(world);
                world.Events.Clear();
            }

            double measured = world.Recorder.TotalGrossEnergy(ResourceKind.Food);
            double estimated = proxy.TotalPositiveDeltaEnergy(ResourceKind.Food);

            Assert.That(measured, Is.GreaterThan(0d));
            Assert.That(estimated, Is.GreaterThan(0d));
            Assert.That(measured, Is.GreaterThan(estimated), "Drain-tick and stale-action erasure can only lose ingestion, never invent it.");
            Assert.That(world.Recorder.TotalStaleActionGrossEnergy(ResourceKind.Food), Is.GreaterThan(0d), "Ingestion under a stale Seek action is exactly what the proxy cannot see.");
        }
    }
}

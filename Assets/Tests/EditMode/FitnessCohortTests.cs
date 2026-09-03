using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class FitnessCohortTests
    {
        private static SimulationSchedule Schedule => SimulationConfig.CreatePrototype4Defaults(worldSeed: 1, initialPopulation: 1).Schedule;

        [Test]
        public void TheHorizonsAreDerivedFromTheSourceRatherThanWrittenAsLiterals()
        {
            SimulationSchedule schedule = Schedule;

            // Written this way so that changing AdultAgeSeconds, the lifespan expression in
            // GenomePhenotype, or the base tick rate fails here rather than silently shifting every
            // cohort in the milestone.
            long expectedAdult = (long)(ReproductionSystem.AdultAgeSeconds * schedule.BaseFrequencyHz);
            long expectedMaximum = (long)(Phenotype.FromGenome(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 1f)).MaximumAgeSeconds * schedule.BaseFrequencyHz);

            Assert.That(FitnessCohort.AdultAgeTicks(schedule), Is.EqualTo(expectedAdult));
            Assert.That(FitnessCohort.MaximumLifespanTicks(schedule), Is.EqualTo(expectedMaximum));
            Assert.That(FitnessCohort.AdultAgeTicks(schedule), Is.EqualTo(400));
            Assert.That(FitnessCohort.MaximumLifespanTicks(schedule), Is.EqualTo(5400));
        }

        [Test]
        public void TheLifespanHorizonIsTheMaximumOverEveryReachableGenotypeNotOverTheObservedOnes()
        {
            SimulationSchedule schedule = Schedule;
            long horizon = FitnessCohort.MaximumLifespanTicks(schedule);

            for (int step = 0; step <= 10; step++)
            {
                float tendency = step / 10f;
                float maximumAge = Phenotype.FromGenome(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: tendency)).MaximumAgeSeconds;
                Assert.That((long)(maximumAge * schedule.BaseFrequencyHz), Is.LessThanOrEqualTo(horizon));
            }
        }

        [Test]
        public void TwoGenotypesAtOppositeLifespanExtremesAreAdmittedOrExcludedTogether()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            long admittedBirth = endTick - FitnessCohort.MaximumLifespanTicks(schedule);

            var shortLived = new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 0f);
            var longLived = new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 1f);

            LifeHistoryLedger admitted = LedgerWith(endTick, (admittedBirth, shortLived), (admittedBirth, longLived));
            FitnessCohort admittedCohort = FitnessCohort.Select(admitted, schedule, endTick, FitnessCohortKind.CompleteLife);
            Assert.That(admittedCohort.Count, Is.EqualTo(2));

            LifeHistoryLedger excluded = LedgerWith(endTick, (admittedBirth + 1, shortLived), (admittedBirth + 1, longLived));
            FitnessCohort excludedCohort = FitnessCohort.Select(excluded, schedule, endTick, FitnessCohortKind.CompleteLife);
            Assert.That(excludedCohort.Count, Is.EqualTo(0));
            Assert.That(excludedCohort.ExcludedByHorizon, Is.EqualTo(2));
        }

        [Test]
        public void ACreatureBornExactlyOnTheHorizonIsAdmittedAndOneTickLaterIsNot()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            long horizonBirth = endTick - FitnessCohort.MaximumLifespanTicks(schedule);
            LifeHistoryLedger ledger = LedgerWith(endTick, (horizonBirth, Genome.Neutral), (horizonBirth + 1, Genome.Neutral));

            FitnessCohort cohort = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife);

            Assert.That(cohort.Count, Is.EqualTo(1));
            Assert.That(cohort.Contains(ledger.GetIdAt(0)), Is.True);
            Assert.That(cohort.Contains(ledger.GetIdAt(1)), Is.False);
        }

        [Test]
        public void TheOffspringToAdulthoodCohortIsAStrictSubsetOfTheCompleteLifeCohort()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            long completeLifeHorizon = endTick - FitnessCohort.MaximumLifespanTicks(schedule);
            long offspringHorizon = endTick - FitnessCohort.MaximumLifespanTicks(schedule) - FitnessCohort.AdultAgeTicks(schedule);
            Assert.That(offspringHorizon, Is.EqualTo(endTick - 5_800));

            LifeHistoryLedger ledger = LedgerWith(endTick, (offspringHorizon, Genome.Neutral), (completeLifeHorizon, Genome.Neutral));

            FitnessCohort completeLife = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife);
            FitnessCohort toAdulthood = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.OffspringToAdulthood);

            Assert.That(completeLife.Count, Is.EqualTo(2));
            Assert.That(toAdulthood.Count, Is.EqualTo(1));
            for (int index = 0; index < toAdulthood.Count; index++)
            {
                Assert.That(completeLife.Contains(toAdulthood.GetIdAt(index)), Is.True);
            }
        }

        [Test]
        public void ACohortBuiltFromAnIncompleteLedgerThrowsRatherThanReturningAPartialSet()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            var overflowed = new SimulationEventBuffer(1);
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(70), new CreatureId(1), default, DeathCause.None));
            overflowed.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(71), new CreatureId(1), default, DeathCause.None));
            ledger.RecordCompleteBatch(overflowed, 10);

            Assert.That(ledger.IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => FitnessCohort.Select(ledger, Schedule, 36_000, FitnessCohortKind.CompleteLife));
        }

        [Test]
        public void FoundersCanBeExcludedOnParentlessnessWhichIsGenotypeIndependent()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            var creatures = new CreatureStore(4);
            creatures.Add(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 0f));
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            CreatureId born = creatures.Add(new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 1f));
            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(500, SimulationEventKind.Birth, born, new CreatureId(1), default, DeathCause.None));
            ledger.RecordCompleteBatch(events, 500);
            ledger.Observe(creatures);

            FitnessCohort withFounders = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife);
            FitnessCohort withoutFounders = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife, excludeFounders: true);

            Assert.That(withFounders.Count, Is.EqualTo(2));
            Assert.That(withoutFounders.Count, Is.EqualTo(1));
            Assert.That(withoutFounders.ExcludedAsFounder, Is.EqualTo(1));
            Assert.That(withoutFounders.GetIdAt(0), Is.EqualTo(born));
        }

        [Test]
        public void ABirthWindowSplitsTheCohortWithoutBeingAppliedByDefault()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            LifeHistoryLedger ledger = LedgerWith(endTick, (100, Genome.Neutral), (9_000, Genome.Neutral));

            FitnessCohort everything = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife);
            FitnessCohort expansionPhase = FitnessCohort.Select(
                ledger,
                schedule,
                endTick,
                FitnessCohortKind.CompleteLife,
                excludeFounders: false,
                birthWindowStartTick: 0,
                birthWindowEndTick: 5_000);

            Assert.That(everything.Count, Is.EqualTo(2), "The expansion phase is reported, never filtered out by default.");
            Assert.That(everything.ExcludedByBirthWindow, Is.EqualTo(0));
            Assert.That(expansionPhase.Count, Is.EqualTo(1));
            Assert.That(expansionPhase.ExcludedByBirthWindow, Is.EqualTo(1));
        }

        [Test]
        public void ACreatureWhoseGenomeWasNeverObservedIsCountedOutRatherThanSilentlyDropped()
        {
            SimulationSchedule schedule = Schedule;
            long endTick = 36_000;
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);
            ledger.Observe(creatures);

            var events = new SimulationEventBuffer(4);
            events.TryWrite(new SimulationEvent(600, SimulationEventKind.Birth, new CreatureId(88), new CreatureId(1), default, DeathCause.None));
            events.TryWrite(new SimulationEvent(700, SimulationEventKind.Death, new CreatureId(88), default, default, DeathCause.Starvation));
            ledger.RecordCompleteBatch(events, 700);

            FitnessCohort cohort = FitnessCohort.Select(ledger, schedule, endTick, FitnessCohortKind.CompleteLife);

            Assert.That(cohort.Count, Is.EqualTo(1));
            Assert.That(cohort.ExcludedWithoutGenome, Is.EqualTo(1));
        }

        private static LifeHistoryLedger LedgerWith(long endTick, params (long BirthTick, Genome Genome)[] births)
        {
            var creatures = new CreatureStore(births.Length + 1);
            var ledger = new LifeHistoryLedger();
            ledger.RecordFounders(0, creatures);

            long previousTick = 0;
            for (int index = 0; index < births.Length; index++)
            {
                (long birthTick, Genome genome) = births[index];
                if (birthTick < previousTick) throw new ArgumentException("Births must be supplied in nondecreasing tick order.", nameof(births));
                previousTick = birthTick;

                CreatureId id = creatures.Add(genome);
                var events = new SimulationEventBuffer(2);
                events.TryWrite(new SimulationEvent(birthTick, SimulationEventKind.Birth, id, new CreatureId(900), new CreatureId(901), DeathCause.None));
                ledger.RecordCompleteBatch(events, birthTick);
                ledger.Observe(creatures);
            }

            ledger.RecordCompleteBatch(new SimulationEventBuffer(1), endTick);
            return ledger;
        }
    }
}

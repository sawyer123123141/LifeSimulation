using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusterHistoryTests
    {
        [Test]
        public void EventBufferIsBoundedAndHostDrained()
        {
            Assert.That(() => new ClusterHistoryEventBuffer(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            var events = new ClusterHistoryEventBuffer(1);

            bool firstWriteSucceeded = events.TryWrite(default);
            bool secondWriteSucceeded = events.TryWrite(default);

            Assert.That(events.Capacity, Is.EqualTo(1));
            Assert.That(firstWriteSucceeded, Is.True);
            Assert.That(secondWriteSucceeded, Is.False);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Overflowed, Is.True);
            Assert.That(() => events.GetAt(1), Throws.TypeOf<ArgumentOutOfRangeException>());

            events.Clear();

            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(events.Overflowed, Is.False);
        }

        [Test]
        public void HistoryRejectsRepeatedAndDecreasingTicksBeforeChangingState()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(8);
            var history = new GeneticClusterHistory(Policy(), events);
            history.Record(Observe(10, creatures), ancestry);

            Assert.That(() => history.Record(Observe(10, creatures), ancestry), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => history.Record(Observe(9, creatures), ancestry), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(events.Count, Is.EqualTo(0));
        }

        [Test]
        public void HistoryRejectsChangedThresholdBeforeRelationWork()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 20);
            var history = new GeneticClusterHistory(Policy(), new ClusterHistoryEventBuffer(8));
            history.Record(Observe(10, creatures, threshold: .05f), ancestry);

            Assert.That(
                () => history.Record(Observe(20, creatures, threshold: .06f), ancestry),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void HistoryRejectsChangedSamplingModeOrSampleLimit()
        {
            var creatures = CreateCreatures(Genome.Neutral, Genome.Neutral);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 30);
            var fullHistory = new GeneticClusterHistory(Policy(), new ClusterHistoryEventBuffer(8));
            fullHistory.Record(Observe(10, creatures), ancestry);

            Assert.That(
                () => fullHistory.Record(ObserveSample(20, creatures, maximumCount: 2), ancestry),
                Throws.TypeOf<ArgumentException>());

            var sampledHistory = new GeneticClusterHistory(Policy(), new ClusterHistoryEventBuffer(8));
            sampledHistory.Record(ObserveSample(10, creatures, maximumCount: 4), ancestry);

            Assert.That(
                () => sampledHistory.Record(ObserveSample(20, creatures, maximumCount: 3), ancestry),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void HistoryRejectsChangedPolicyBeforeRelationWork()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 20);
            ClusterHistoryPolicy policy = Policy();
            var history = new GeneticClusterHistory(policy, new ClusterHistoryEventBuffer(8));
            history.Record(Observe(10, creatures), ancestry);
            var changedPolicy = new ClusterHistoryPolicy(1, .5f, 1, .5f, 2, 2, 2);

            Assert.That(
                () => history.Record(Observe(20, creatures), ancestry, changedPolicy),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void HistoryRejectsChangedAncestrySourceBeforeConfirmingPendingCandidate()
        {
            var creatures = CreateCreatures(Genome.Neutral, Genome.Neutral, Genome.Neutral, Genome.Neutral);
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            CreatureId thirdParent = creatures.GetIdAt(2);
            CreatureId fourthParent = creatures.GetIdAt(3);
            AncestryHistory originalAncestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(32);
            var history = new GeneticClusterHistory(Policy(requiredSuccessorObservations: 1), events);
            history.Record(Observe(10, creatures), originalAncestry);

            CreatureId firstChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId secondChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId thirdChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            CreatureId fourthChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            Remove(creatures, firstParent, secondParent, thirdParent, fourthParent);
            Advance(
                originalAncestry,
                20,
                Birth(15, firstChild, firstParent),
                Birth(15, secondChild, secondParent),
                Birth(15, thirdChild, thirdParent),
                Birth(15, fourthChild, fourthParent));
            history.Record(Observe(20, creatures), originalAncestry);
            int candidateEventCount = events.Count;

            var rerootedAncestry = new AncestryHistory();
            rerootedAncestry.RecordFounders(0, creatures);
            Advance(rerootedAncestry, throughTick: 30);
            GeneticClusterObservation persistenceObservation = Observe(30, creatures);

            Assert.That(
                () => history.Record(persistenceObservation, rerootedAncestry),
                Throws.TypeOf<ArgumentException>());
            Assert.That(events.Count, Is.EqualTo(candidateEventCount));
            Assert.That(Count(events, ClusterHistoryEventKind.Continuity), Is.EqualTo(0));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedSplit), Is.EqualTo(0));

            Advance(originalAncestry, throughTick: 30);
            history.Record(persistenceObservation, originalAncestry);

            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedSplit), Is.EqualTo(1));
        }

        [Test]
        public void MissingObservedAncestryEmitsIncompleteEvidenceWithoutClassification()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            creatures.Add(Genome.Neutral);
            Advance(ancestry, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(8);
            var history = new GeneticClusterHistory(Policy(), events);

            history.Record(Observe(10, creatures), ancestry);

            ClusterHistoryEvent incomplete = Find(events, ClusterHistoryEventKind.IncompleteEvidence);
            Assert.That(incomplete.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(incomplete.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.ObservedCreatureMissing));
            Assert.That(Count(events, ClusterHistoryEventKind.Continuity), Is.EqualTo(0));
        }

        [Test]
        public void ExclusiveSplitIsCandidateUntilEveryChildTrackPersists()
        {
            var creatures = CreateCreatures(Genome.Neutral, Genome.Neutral, Genome.Neutral, Genome.Neutral);
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            CreatureId thirdParent = creatures.GetIdAt(2);
            CreatureId fourthParent = creatures.GetIdAt(3);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(32);
            var history = new GeneticClusterHistory(Policy(requiredSuccessorObservations: 1), events);
            history.Record(Observe(10, creatures), ancestry);

            CreatureId firstChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId secondChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId thirdChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            CreatureId fourthChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            Remove(creatures, firstParent, secondParent, thirdParent, fourthParent);
            Advance(
                ancestry,
                20,
                Birth(15, firstChild, firstParent),
                Birth(15, secondChild, secondParent),
                Birth(15, thirdChild, thirdParent),
                Birth(15, fourthChild, fourthParent));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent candidate = Find(events, ClusterHistoryEventKind.CandidateSplit);
            Assert.That(candidate.Status, Is.EqualTo(ClusterHistoryEventStatus.Candidate));
            Assert.That(candidate.PreviousTrackCount, Is.EqualTo(1));
            Assert.That(candidate.CurrentTrackCount, Is.EqualTo(2));
            Assert.That(candidate.RelationCount, Is.EqualTo(2));
            Assert.That(candidate.GetRelationAt(0).IsStrong, Is.True);
            Assert.That(candidate.GetRelationAt(1).IsStrong, Is.True);
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedSplit), Is.EqualTo(0));

            Advance(ancestry, throughTick: 30);
            history.Record(Observe(30, creatures), ancestry);

            ClusterHistoryEvent confirmed = Find(events, ClusterHistoryEventKind.ConfirmedSplit);
            Assert.That(confirmed.Status, Is.EqualTo(ClusterHistoryEventStatus.Confirmed));
            Assert.That(confirmed.ConfirmationObservationCount, Is.EqualTo(1));
            Assert.That(confirmed.CurrentTrackCount, Is.EqualTo(2));
            Assert.That(confirmed.GetCurrentTrackIdAt(0), Is.EqualTo(candidate.GetCurrentTrackIdAt(0)));
            Assert.That(confirmed.GetCurrentTrackIdAt(1), Is.EqualTo(candidate.GetCurrentTrackIdAt(1)));
        }

        [Test]
        public void SplitCandidateBecomesUnresolvedWhenAnyChildFailsToPersist()
        {
            var creatures = CreateCreatures(Genome.Neutral, Genome.Neutral, Genome.Neutral, Genome.Neutral);
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            CreatureId thirdParent = creatures.GetIdAt(2);
            CreatureId fourthParent = creatures.GetIdAt(3);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(32);
            var history = new GeneticClusterHistory(Policy(requiredSuccessorObservations: 2), events);
            history.Record(Observe(10, creatures), ancestry);

            CreatureId firstChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId secondChild = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId thirdChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            CreatureId fourthChild = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            Remove(creatures, firstParent, secondParent, thirdParent, fourthParent);
            Advance(
                ancestry,
                20,
                Birth(15, firstChild, firstParent),
                Birth(15, secondChild, secondParent),
                Birth(15, thirdChild, thirdParent),
                Birth(15, fourthChild, fourthParent));
            history.Record(Observe(20, creatures), ancestry);

            Remove(creatures, thirdChild, fourthChild);
            Advance(ancestry, throughTick: 30);
            history.Record(Observe(30, creatures), ancestry);

            ClusterHistoryEvent unresolved = Find(events, ClusterHistoryEventKind.UnresolvedCandidate);
            Assert.That(unresolved.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(unresolved.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.CandidateDidNotPersist));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedSplit), Is.EqualTo(0));
        }

        [Test]
        public void ExclusiveMergeIsCandidateUntilItsSuccessorTrackPersists()
        {
            var creatures = CreateCreatures(
                Genome.Neutral.WithBodySize(.2f),
                Genome.Neutral.WithBodySize(.2f),
                Genome.Neutral.WithBodySize(.8f),
                Genome.Neutral.WithBodySize(.8f));
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            CreatureId thirdParent = creatures.GetIdAt(2);
            CreatureId fourthParent = creatures.GetIdAt(3);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(32);
            var history = new GeneticClusterHistory(Policy(requiredSuccessorObservations: 1), events);
            history.Record(Observe(10, creatures), ancestry);

            CreatureId firstChild = creatures.Add(Genome.Neutral);
            CreatureId secondChild = creatures.Add(Genome.Neutral);
            CreatureId thirdChild = creatures.Add(Genome.Neutral);
            CreatureId fourthChild = creatures.Add(Genome.Neutral);
            Remove(creatures, firstParent, secondParent, thirdParent, fourthParent);
            Advance(
                ancestry,
                20,
                Birth(15, firstChild, firstParent),
                Birth(15, secondChild, secondParent),
                Birth(15, thirdChild, thirdParent),
                Birth(15, fourthChild, fourthParent));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent candidate = Find(events, ClusterHistoryEventKind.CandidateMerge);
            Assert.That(candidate.Status, Is.EqualTo(ClusterHistoryEventStatus.Candidate));
            Assert.That(candidate.PreviousTrackCount, Is.EqualTo(2));
            Assert.That(candidate.CurrentTrackCount, Is.EqualTo(1));
            Assert.That(candidate.RelationCount, Is.EqualTo(2));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedMerge), Is.EqualTo(0));

            Advance(ancestry, throughTick: 30);
            history.Record(Observe(30, creatures), ancestry);

            ClusterHistoryEvent confirmed = Find(events, ClusterHistoryEventKind.ConfirmedMerge);
            Assert.That(confirmed.Status, Is.EqualTo(ClusterHistoryEventStatus.Confirmed));
            Assert.That(confirmed.ConfirmationObservationCount, Is.EqualTo(1));
            Assert.That(confirmed.GetCurrentTrackIdAt(0), Is.EqualTo(candidate.GetCurrentTrackIdAt(0)));
        }

        [Test]
        public void ManyToManyStrongComponentIsOnlyAmbiguousReorganisation()
        {
            var creatures = CreateCreatures(
                Genome.Neutral.WithBodySize(.2f),
                Genome.Neutral.WithBodySize(.2f),
                Genome.Neutral.WithBodySize(.8f),
                Genome.Neutral.WithBodySize(.8f));
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            CreatureId thirdParent = creatures.GetIdAt(2);
            CreatureId fourthParent = creatures.GetIdAt(3);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(32);
            var history = new GeneticClusterHistory(Policy(), events);
            history.Record(Observe(10, creatures), ancestry);

            CreatureId firstChild = creatures.Add(Genome.Neutral.WithBodySize(.3f));
            CreatureId secondChild = creatures.Add(Genome.Neutral.WithBodySize(.7f));
            CreatureId thirdChild = creatures.Add(Genome.Neutral.WithBodySize(.3f));
            CreatureId fourthChild = creatures.Add(Genome.Neutral.WithBodySize(.7f));
            Remove(creatures, firstParent, secondParent, thirdParent, fourthParent);
            Advance(
                ancestry,
                20,
                Birth(15, firstChild, firstParent),
                Birth(15, secondChild, secondParent),
                Birth(15, thirdChild, thirdParent),
                Birth(15, fourthChild, fourthParent));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent ambiguous = Find(events, ClusterHistoryEventKind.AmbiguousReorganisation);
            Assert.That(ambiguous.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(ambiguous.PreviousTrackCount, Is.EqualTo(2));
            Assert.That(ambiguous.CurrentTrackCount, Is.EqualTo(2));
            Assert.That(ambiguous.RelationCount, Is.EqualTo(4));
            Assert.That(Count(events, ClusterHistoryEventKind.CandidateSplit), Is.EqualTo(0));
            Assert.That(Count(events, ClusterHistoryEventKind.CandidateMerge), Is.EqualTo(0));
        }

        [Test]
        public void ClusterWithoutStrongPredecessorIsAnUnresolvedArrival()
        {
            var creatures = CreateCreatures(Genome.Neutral.WithBodySize(.2f));
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(16);
            var history = new GeneticClusterHistory(Policy(), events);
            history.Record(Observe(10, creatures), ancestry);
            CreatureId arrival = creatures.Add(Genome.Neutral.WithBodySize(.8f));
            Advance(ancestry, 20, Birth(15, arrival, default));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent unresolvedArrival = Find(events, ClusterHistoryEventKind.UnresolvedArrival);
            Assert.That(unresolvedArrival.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(unresolvedArrival.PreviousTrackCount, Is.EqualTo(0));
            Assert.That(unresolvedArrival.CurrentTrackCount, Is.EqualTo(1));
        }

        [Test]
        public void SampledDisappearanceNeverBecomesLineageExtinction()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            CreatureId disappearing = creatures.GetIdAt(0);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(16);
            var history = new GeneticClusterHistory(Policy(requiredAbsentObservations: 2), events);
            history.Record(ObserveSample(10, creatures, maximumCount: 1), ancestry);
            creatures.Remove(disappearing);
            Advance(ancestry, 20, Death(15, disappearing));
            history.Record(ObserveSample(20, creatures, maximumCount: 1), ancestry);
            Advance(ancestry, throughTick: 30);

            history.Record(ObserveSample(30, creatures, maximumCount: 1), ancestry);

            ClusterHistoryEvent unresolved = Find(events, ClusterHistoryEventKind.UnresolvedDisappearance);
            Assert.That(unresolved.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(unresolved.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.SampledObservation));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedLineageExtinction), Is.EqualTo(0));
        }

        [Test]
        public void FullCompleteAbsenceIsPendingUntilTheRequiredWindowThenConfirmsExtinction()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            CreatureId disappearing = creatures.GetIdAt(0);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(16);
            var history = new GeneticClusterHistory(Policy(requiredAbsentObservations: 2), events);
            history.Record(Observe(10, creatures), ancestry);
            creatures.Remove(disappearing);
            Advance(ancestry, 20, Death(15, disappearing));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent pending = Find(events, ClusterHistoryEventKind.PendingDisappearance);
            Assert.That(pending.Status, Is.EqualTo(ClusterHistoryEventStatus.Candidate));
            Assert.That(pending.ConsecutiveAbsentObservationCount, Is.EqualTo(1));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedLineageExtinction), Is.EqualTo(0));

            Advance(ancestry, throughTick: 30);
            history.Record(Observe(30, creatures), ancestry);

            ClusterHistoryEvent extinction = Find(events, ClusterHistoryEventKind.ConfirmedLineageExtinction);
            Assert.That(extinction.Status, Is.EqualTo(ClusterHistoryEventStatus.Confirmed));
            Assert.That(extinction.EventHistoryIsComplete, Is.True);
            Assert.That(extinction.IsSampled, Is.False);
            Assert.That(extinction.ConsecutiveAbsentObservationCount, Is.EqualTo(2));
            Assert.That(extinction.LivingDescendantCount, Is.EqualTo(0));
            Assert.That(extinction.FirstObservedTick, Is.EqualTo(10));
            Assert.That(extinction.LastObservedTick, Is.EqualTo(30));
        }

        [Test]
        public void LivingWeakDescendantMakesDisappearanceUnresolvedInsteadOfExtinct()
        {
            var creatures = CreateCreatures(Genome.Neutral, Genome.Neutral);
            CreatureId firstParent = creatures.GetIdAt(0);
            CreatureId secondParent = creatures.GetIdAt(1);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(16);
            var strictPreviousSupport = new ClusterHistoryPolicy(1, 1f, 2, 1f, 3, 1, 1);
            var history = new GeneticClusterHistory(strictPreviousSupport, events);
            history.Record(Observe(10, creatures), ancestry);
            CreatureId child = creatures.Add(Genome.Neutral);
            Remove(creatures, firstParent, secondParent);
            Advance(ancestry, 20, Birth(15, child, firstParent));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent unresolved = Find(events, ClusterHistoryEventKind.UnresolvedDisappearance);
            Assert.That(unresolved.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(unresolved.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.LivingDescendant));
            Assert.That(unresolved.LivingDescendantCount, Is.EqualTo(1));
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedLineageExtinction), Is.EqualTo(0));
        }

        [Test]
        public void MissingAncestorOfDisappearedMemberBlocksExtinctionWhenCurrentPopulationIsEmpty()
        {
            var creatures = new CreatureStore(1);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId child = creatures.Add(Genome.Neutral);
            Advance(ancestry, 10, Birth(5, child, new CreatureId(999)));
            var events = new ClusterHistoryEventBuffer(16);
            var history = new GeneticClusterHistory(Policy(requiredAbsentObservations: 1), events);
            history.Record(Observe(10, creatures), ancestry);
            creatures.Remove(child);
            Advance(ancestry, 20, Death(15, child));

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent unresolved = Find(events, ClusterHistoryEventKind.UnresolvedDisappearance);
            Assert.That(unresolved.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete));
            Assert.That(unresolved.AncestryCoverageIsComplete, Is.False);
            Assert.That(Count(events, ClusterHistoryEventKind.ConfirmedLineageExtinction), Is.EqualTo(0));
        }

        [Test]
        public void OverflowedAncestryEmitsUnresolvedIncompleteEvidence()
        {
            var creatures = CreateCreatures(Genome.Neutral);
            AncestryHistory ancestry = CreateCompleteAncestry(creatures, throughTick: 10);
            var events = new ClusterHistoryEventBuffer(16);
            var history = new GeneticClusterHistory(Policy(), events);
            history.Record(Observe(10, creatures), ancestry);
            var overflowedEvents = new SimulationEventBuffer(1);
            overflowedEvents.TryWrite(Birth(15, new CreatureId(100), default));
            overflowedEvents.TryWrite(Birth(15, new CreatureId(101), default));
            ancestry.RecordCompleteBatch(overflowedEvents, throughTick: 20);

            history.Record(Observe(20, creatures), ancestry);

            ClusterHistoryEvent incomplete = Find(events, ClusterHistoryEventKind.IncompleteEvidence);
            Assert.That(incomplete.Status, Is.EqualTo(ClusterHistoryEventStatus.Unresolved));
            Assert.That(incomplete.UnresolvedReason, Is.EqualTo(ClusterHistoryUnresolvedReason.AncestryIncomplete));
            Assert.That(incomplete.EventHistoryIsComplete, Is.False);
            Assert.That(Count(events, ClusterHistoryEventKind.Continuity), Is.EqualTo(0));
        }

        private static CreatureStore CreateCreatures(params Genome[] genomes)
        {
            var creatures = new CreatureStore(Math.Max(1, genomes.Length));
            for (int index = 0; index < genomes.Length; index++) creatures.Add(genomes[index]);
            return creatures;
        }

        private static GeneticClusterObservation Observe(long tick, CreatureStore creatures, float threshold = .05f)
        {
            return GeneticClusterObservation.Create(PopulationGenomeSnapshot.Capture(tick, creatures), threshold);
        }

        private static GeneticClusterObservation ObserveSample(long tick, CreatureStore creatures, int maximumCount, float threshold = .05f)
        {
            return GeneticClusterObservation.Create(PopulationGenomeSnapshot.CaptureSample(tick, creatures, maximumCount), threshold);
        }

        private static AncestryHistory CreateCompleteAncestry(CreatureStore creatures, long throughTick)
        {
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            Advance(ancestry, throughTick);
            return ancestry;
        }

        private static void Advance(AncestryHistory ancestry, long throughTick, params SimulationEvent[] simulationEvents)
        {
            var buffer = new SimulationEventBuffer(Math.Max(1, simulationEvents.Length));
            for (int index = 0; index < simulationEvents.Length; index++)
            {
                Assert.That(buffer.TryWrite(simulationEvents[index]), Is.True);
            }
            ancestry.RecordCompleteBatch(buffer, throughTick);
        }

        private static SimulationEvent Birth(long tick, CreatureId child, CreatureId parent)
        {
            return new SimulationEvent(tick, SimulationEventKind.Birth, child, parent, default, DeathCause.None);
        }

        private static SimulationEvent Death(long tick, CreatureId creatureId)
        {
            return new SimulationEvent(tick, SimulationEventKind.Death, creatureId, default, default, DeathCause.Age);
        }

        private static void Remove(CreatureStore creatures, params CreatureId[] creatureIds)
        {
            for (int index = 0; index < creatureIds.Length; index++)
            {
                Assert.That(creatures.Remove(creatureIds[index]), Is.True);
            }
        }

        private static ClusterHistoryPolicy Policy(int requiredSuccessorObservations = 1, int requiredAbsentObservations = 2)
        {
            return new ClusterHistoryPolicy(
                minimumSupportedCurrentMembers: 1,
                minimumCurrentSupportFraction: .5f,
                minimumSupportingPreviousMembers: 1,
                minimumPreviousSupportFraction: .5f,
                maximumAncestorGenerations: 3,
                requiredSuccessorObservations,
                requiredAbsentObservations);
        }

        private static ClusterHistoryEvent Find(ClusterHistoryEventBuffer events, ClusterHistoryEventKind kind)
        {
            for (int index = 0; index < events.Count; index++)
            {
                ClusterHistoryEvent historyEvent = events.GetAt(index);
                if (historyEvent.Kind == kind) return historyEvent;
            }

            Assert.Fail($"Expected a {kind} event.");
            return default;
        }

        private static int Count(ClusterHistoryEventBuffer events, ClusterHistoryEventKind kind)
        {
            int count = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (events.GetAt(index).Kind == kind) count++;
            }
            return count;
        }
    }
}

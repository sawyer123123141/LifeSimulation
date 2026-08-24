#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// Turning a transition into events: what is emitted, what is held back as pending evidence, and
    /// what is eventually written as unresolved.
    ///
    /// <para>Separated from the state machine in the main file because this half is about the
    /// <i>output</i> - once a transition has been classified, everything here decides how it is
    /// reported. The two were one 1324-line file.</para>
    /// </summary>
    public sealed partial class GeneticClusterHistory
    {
        private void EmitStrongComponentEvents(
            int componentCount,
            int[] previousComponents,
            int[] currentComponents,
            int[] componentPreviousCounts,
            int[] componentCurrentCounts,
            GeneticClusterObservation currentObservation,
            int[] currentClusterOrdinals,
            long[] currentTrackIds,
            GeneticClusterRelation[] relations)
        {
            for (int component = 0; component < componentCount; component++)
            {
                int[] previousOrdinals = Select(_previousClusterOrdinals, previousComponents, component);
                long[] previousTracks = Select(_previousTrackIds, previousComponents, component);
                int[] currentOrdinals = Select(currentClusterOrdinals, currentComponents, component);
                long[] currentTracks = Select(currentTrackIds, currentComponents, component);
                GeneticClusterRelation[] componentRelations = SelectRelations(
                    relations,
                    currentClusterOrdinals.Length,
                    previousComponents,
                    currentComponents,
                    component);
                int previousCount = componentPreviousCounts[component];
                int currentCount = componentCurrentCounts[component];

                if (previousCount == 1 && currentCount == 1)
                {
                    WriteEvent(
                        ClusterHistoryEventKind.Continuity,
                        ClusterHistoryEventStatus.Confirmed,
                        ClusterHistoryUnresolvedReason.None,
                        _previousObservation!.Tick,
                        currentObservation.Tick,
                        currentObservation,
                        eventHistoryIsComplete: true,
                        ancestryCoverageIsComplete: true,
                        confirmationObservationCount: 0,
                        requiredObservationCount: 0,
                        consecutiveAbsentObservationCount: 0,
                        livingDescendantCount: 0,
                        previousOrdinals,
                        previousTracks,
                        currentOrdinals,
                        currentTracks,
                        componentRelations);
                    continue;
                }

                if (previousCount == 1)
                {
                    WriteEvent(
                        ClusterHistoryEventKind.CandidateSplit,
                        ClusterHistoryEventStatus.Candidate,
                        ClusterHistoryUnresolvedReason.None,
                        _previousObservation!.Tick,
                        currentObservation.Tick,
                        currentObservation,
                        eventHistoryIsComplete: true,
                        ancestryCoverageIsComplete: true,
                        confirmationObservationCount: 0,
                        requiredObservationCount: _policy.RequiredSuccessorObservations,
                        consecutiveAbsentObservationCount: 0,
                        livingDescendantCount: 0,
                        previousOrdinals,
                        previousTracks,
                        currentOrdinals,
                        currentTracks,
                        componentRelations);
                    _pendingConfirmations.Add(new PendingConfirmation(
                        ClusterHistoryEventKind.CandidateSplit,
                        _previousObservation!.Tick,
                        currentObservation.Tick,
                        previousOrdinals,
                        previousTracks,
                        currentOrdinals,
                        currentTracks,
                        componentRelations));
                    continue;
                }

                if (currentCount == 1)
                {
                    WriteEvent(
                        ClusterHistoryEventKind.CandidateMerge,
                        ClusterHistoryEventStatus.Candidate,
                        ClusterHistoryUnresolvedReason.None,
                        _previousObservation!.Tick,
                        currentObservation.Tick,
                        currentObservation,
                        eventHistoryIsComplete: true,
                        ancestryCoverageIsComplete: true,
                        confirmationObservationCount: 0,
                        requiredObservationCount: _policy.RequiredSuccessorObservations,
                        consecutiveAbsentObservationCount: 0,
                        livingDescendantCount: 0,
                        previousOrdinals,
                        previousTracks,
                        currentOrdinals,
                        currentTracks,
                        componentRelations);
                    _pendingConfirmations.Add(new PendingConfirmation(
                        ClusterHistoryEventKind.CandidateMerge,
                        _previousObservation!.Tick,
                        currentObservation.Tick,
                        previousOrdinals,
                        previousTracks,
                        currentOrdinals,
                        currentTracks,
                        componentRelations));
                    continue;
                }

                WriteEvent(
                    ClusterHistoryEventKind.AmbiguousReorganisation,
                    ClusterHistoryEventStatus.Unresolved,
                    ClusterHistoryUnresolvedReason.AmbiguousStrongRelations,
                    _previousObservation!.Tick,
                    currentObservation.Tick,
                    currentObservation,
                    eventHistoryIsComplete: true,
                    ancestryCoverageIsComplete: true,
                    confirmationObservationCount: 0,
                    requiredObservationCount: 0,
                    consecutiveAbsentObservationCount: 0,
                    livingDescendantCount: 0,
                    previousOrdinals,
                    previousTracks,
                    currentOrdinals,
                    currentTracks,
                    componentRelations);
            }
        }

        private void AdvancePendingConfirmations(
            GeneticClusterObservation currentObservation,
            int[] currentClusterOrdinals,
            long[] currentTrackIds,
            GeneticClusterRelation[] relations,
            int[] currentStrongCounts,
            int[] previousStrongCounts,
            int[] previousComponents,
            int[] componentPreviousCounts,
            int[] componentCurrentCounts)
        {
            for (int candidateIndex = 0; candidateIndex < _pendingConfirmations.Count; candidateIndex++)
            {
                PendingConfirmation candidate = _pendingConfirmations[candidateIndex];
                bool persisted = true;
                for (int watchedIndex = 0; watchedIndex < candidate.CurrentTrackIds.Length; watchedIndex++)
                {
                    long watchedTrackId = candidate.CurrentTrackIds[watchedIndex];
                    int previousIndex = IndexOf(watchedTrackId, _previousTrackIds);
                    if (previousIndex < 0 || previousStrongCounts[previousIndex] != 1)
                    {
                        persisted = false;
                        break;
                    }

                    int currentIndex = FindOnlyStrongCurrent(relations, currentClusterOrdinals.Length, previousIndex);
                    int component = previousComponents[previousIndex];
                    if (currentIndex < 0
                        || currentStrongCounts[currentIndex] != 1
                        || component < 0
                        || componentPreviousCounts[component] != 1
                        || componentCurrentCounts[component] != 1
                        || currentTrackIds[currentIndex] != watchedTrackId)
                    {
                        persisted = false;
                        break;
                    }
                }

                if (!persisted)
                {
                    WriteUnresolvedCandidate(candidate, currentObservation, ClusterHistoryUnresolvedReason.CandidateDidNotPersist);
                    _pendingConfirmations.RemoveAt(candidateIndex--);
                    continue;
                }

                candidate.SuccessfulObservationCount++;
                if (candidate.SuccessfulObservationCount < _policy.RequiredSuccessorObservations) continue;

                ClusterHistoryEventKind confirmedKind = candidate.Kind == ClusterHistoryEventKind.CandidateSplit
                    ? ClusterHistoryEventKind.ConfirmedSplit
                    : ClusterHistoryEventKind.ConfirmedMerge;
                WriteEvent(
                    confirmedKind,
                    ClusterHistoryEventStatus.Confirmed,
                    ClusterHistoryUnresolvedReason.None,
                    candidate.FirstObservedTick,
                    currentObservation.Tick,
                    currentObservation,
                    eventHistoryIsComplete: true,
                    ancestryCoverageIsComplete: true,
                    candidate.SuccessfulObservationCount,
                    _policy.RequiredSuccessorObservations,
                    consecutiveAbsentObservationCount: 0,
                    livingDescendantCount: 0,
                    candidate.PreviousClusterOrdinals,
                    candidate.PreviousTrackIds,
                    candidate.CurrentClusterOrdinals,
                    candidate.CurrentTrackIds,
                    candidate.Relations);
                _pendingConfirmations.RemoveAt(candidateIndex--);
            }
        }

        private void EmitNewDisappearances(
            int[] previousStrongCounts,
            GeneticClusterObservation currentObservation,
            int[] currentClusterOrdinals,
            AncestryHistory ancestry,
            GeneticClusterRelation[] relations,
            int currentCount)
        {
            for (int previousIndex = 0; previousIndex < previousStrongCounts.Length; previousIndex++)
            {
                if (previousStrongCounts[previousIndex] != 0) continue;
                int[] previousOrdinals = { _previousClusterOrdinals[previousIndex] };
                long[] previousTracks = { _previousTrackIds[previousIndex] };
                GeneticClusterRelation[] disappearanceRelations = SelectPreviousRelations(relations, currentCount, previousIndex);
                WriteEvent(
                    ClusterHistoryEventKind.PendingDisappearance,
                    ClusterHistoryEventStatus.Candidate,
                    ClusterHistoryUnresolvedReason.None,
                    _previousObservation!.Tick,
                    currentObservation.Tick,
                    currentObservation,
                    eventHistoryIsComplete: true,
                    ancestryCoverageIsComplete: true,
                    confirmationObservationCount: 0,
                    requiredObservationCount: _policy.RequiredAbsentObservations,
                    consecutiveAbsentObservationCount: 1,
                    livingDescendantCount: 0,
                    previousOrdinals,
                    previousTracks,
                    EmptyOrdinals,
                    EmptyTracks,
                    disappearanceRelations);

                var pending = new PendingDisappearance(
                    _previousObservation!,
                    _previousClusterOrdinals[previousIndex],
                    _previousTrackIds[previousIndex],
                    absentObservationCount: 1);
                if (_policy.RequiredAbsentObservations == 1)
                {
                    ResolveDisappearance(pending, currentObservation, ancestry, disappearanceRelations);
                }
                else
                {
                    _pendingDisappearances.Add(pending);
                }
            }
        }

        private void AdvancePendingDisappearances(GeneticClusterObservation currentObservation, AncestryHistory ancestry)
        {
            int[] currentOrdinals = GetClusterOrdinals(currentObservation);
            for (int pendingIndex = 0; pendingIndex < _pendingDisappearances.Count; pendingIndex++)
            {
                PendingDisappearance pending = _pendingDisappearances[pendingIndex];
                int[] previousOrdinal = { pending.LastClusterOrdinal };
                GeneticClusterRelation[] relations = CreateRelations(
                    pending.LastObservation,
                    previousOrdinal,
                    currentObservation,
                    currentOrdinals,
                    ancestry);
                if (!AllRelationsHaveCompleteCoverage(relations))
                {
                    WriteUnresolvedDisappearance(
                        pending,
                        currentObservation,
                        ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete,
                        livingDescendantCount: 0,
                        ancestryCoverageIsComplete: false,
                        relations);
                    _pendingDisappearances.RemoveAt(pendingIndex--);
                    continue;
                }

                bool hasStrongDescendant = false;
                for (int relationIndex = 0; relationIndex < relations.Length; relationIndex++)
                {
                    if (relations[relationIndex].IsStrong) hasStrongDescendant = true;
                }
                if (hasStrongDescendant)
                {
                    WriteUnresolvedDisappearance(
                        pending,
                        currentObservation,
                        ClusterHistoryUnresolvedReason.StrongDescendantAfterGap,
                        livingDescendantCount: 0,
                        ancestryCoverageIsComplete: true,
                        relations);
                    _pendingDisappearances.RemoveAt(pendingIndex--);
                    continue;
                }

                pending.AbsentObservationCount++;
                if (pending.AbsentObservationCount < _policy.RequiredAbsentObservations) continue;
                ResolveDisappearance(pending, currentObservation, ancestry, relations);
                _pendingDisappearances.RemoveAt(pendingIndex--);
            }
        }

        private void ResolveDisappearance(
            PendingDisappearance pending,
            GeneticClusterObservation currentObservation,
            AncestryHistory ancestry,
            GeneticClusterRelation[] relations)
        {
            if (currentObservation.IsSampled)
            {
                WriteUnresolvedDisappearance(
                    pending,
                    currentObservation,
                    ClusterHistoryUnresolvedReason.SampledObservation,
                    livingDescendantCount: 0,
                    ancestryCoverageIsComplete: true,
                    relations);
                return;
            }

            int livingDescendantCount = CountLivingDescendants(
                pending.LastObservation,
                pending.LastClusterOrdinal,
                currentObservation,
                ancestry,
                out bool ancestryCoverageIsComplete);
            if (!ancestryCoverageIsComplete)
            {
                WriteUnresolvedDisappearance(
                    pending,
                    currentObservation,
                    ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete,
                    livingDescendantCount,
                    ancestryCoverageIsComplete: false,
                    relations);
                return;
            }
            if (livingDescendantCount > 0)
            {
                WriteUnresolvedDisappearance(
                    pending,
                    currentObservation,
                    ClusterHistoryUnresolvedReason.LivingDescendant,
                    livingDescendantCount,
                    ancestryCoverageIsComplete: true,
                    relations);
                return;
            }

            int[] previousOrdinals = { pending.LastClusterOrdinal };
            long[] previousTracks = { pending.TrackId };
            WriteEvent(
                ClusterHistoryEventKind.ConfirmedLineageExtinction,
                ClusterHistoryEventStatus.Confirmed,
                ClusterHistoryUnresolvedReason.None,
                pending.LastObservation.Tick,
                currentObservation.Tick,
                currentObservation,
                eventHistoryIsComplete: true,
                ancestryCoverageIsComplete: true,
                confirmationObservationCount: 0,
                requiredObservationCount: _policy.RequiredAbsentObservations,
                pending.AbsentObservationCount,
                livingDescendantCount: 0,
                previousOrdinals,
                previousTracks,
                EmptyOrdinals,
                EmptyTracks,
                relations);
        }

        private void EmitUnresolvedArrivals(
            int[] currentStrongCounts,
            GeneticClusterObservation currentObservation,
            int[] currentClusterOrdinals,
            long[] currentTrackIds,
            GeneticClusterRelation[] relations,
            int currentCount)
        {
            for (int currentIndex = 0; currentIndex < currentStrongCounts.Length; currentIndex++)
            {
                if (currentStrongCounts[currentIndex] != 0) continue;
                int[] currentOrdinals = { currentClusterOrdinals[currentIndex] };
                long[] currentTracks = { currentTrackIds[currentIndex] };
                WriteEvent(
                    ClusterHistoryEventKind.UnresolvedArrival,
                    ClusterHistoryEventStatus.Unresolved,
                    ClusterHistoryUnresolvedReason.NoStrongPredecessor,
                    _previousObservation!.Tick,
                    currentObservation.Tick,
                    currentObservation,
                    eventHistoryIsComplete: true,
                    ancestryCoverageIsComplete: true,
                    confirmationObservationCount: 0,
                    requiredObservationCount: 0,
                    consecutiveAbsentObservationCount: 0,
                    livingDescendantCount: 0,
                    EmptyOrdinals,
                    EmptyTracks,
                    currentOrdinals,
                    currentTracks,
                    SelectCurrentRelations(relations, currentCount, currentIndex));
            }
        }

        private void FailPendingEvidence(
            GeneticClusterObservation currentObservation,
            ClusterHistoryUnresolvedReason reason)
        {
            for (int index = 0; index < _pendingConfirmations.Count; index++)
            {
                WriteUnresolvedCandidate(_pendingConfirmations[index], currentObservation, reason);
            }
            _pendingConfirmations.Clear();

            for (int index = 0; index < _pendingDisappearances.Count; index++)
            {
                WriteUnresolvedDisappearance(
                    _pendingDisappearances[index],
                    currentObservation,
                    reason,
                    livingDescendantCount: 0,
                    ancestryCoverageIsComplete: false,
                    EmptyRelations);
            }
            _pendingDisappearances.Clear();
        }

        private void WriteUnresolvedCandidate(
            PendingConfirmation candidate,
            GeneticClusterObservation currentObservation,
            ClusterHistoryUnresolvedReason reason)
        {
            WriteEvent(
                ClusterHistoryEventKind.UnresolvedCandidate,
                ClusterHistoryEventStatus.Unresolved,
                reason,
                candidate.FirstObservedTick,
                currentObservation.Tick,
                currentObservation,
                eventHistoryIsComplete: reason != ClusterHistoryUnresolvedReason.AncestryIncomplete
                    && reason != ClusterHistoryUnresolvedReason.AncestryNotRecordedThroughObservation
                    && reason != ClusterHistoryUnresolvedReason.ObservedCreatureMissing,
                ancestryCoverageIsComplete: reason != ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete,
                candidate.SuccessfulObservationCount,
                _policy.RequiredSuccessorObservations,
                consecutiveAbsentObservationCount: 0,
                livingDescendantCount: 0,
                candidate.PreviousClusterOrdinals,
                candidate.PreviousTrackIds,
                candidate.CurrentClusterOrdinals,
                candidate.CurrentTrackIds,
                candidate.Relations);
        }

        private void WriteUnresolvedDisappearance(
            PendingDisappearance pending,
            GeneticClusterObservation currentObservation,
            ClusterHistoryUnresolvedReason reason,
            int livingDescendantCount,
            bool ancestryCoverageIsComplete,
            GeneticClusterRelation[] relations)
        {
            int[] previousOrdinals = { pending.LastClusterOrdinal };
            long[] previousTracks = { pending.TrackId };
            WriteEvent(
                ClusterHistoryEventKind.UnresolvedDisappearance,
                ClusterHistoryEventStatus.Unresolved,
                reason,
                pending.LastObservation.Tick,
                currentObservation.Tick,
                currentObservation,
                eventHistoryIsComplete: reason != ClusterHistoryUnresolvedReason.AncestryIncomplete
                    && reason != ClusterHistoryUnresolvedReason.AncestryNotRecordedThroughObservation
                    && reason != ClusterHistoryUnresolvedReason.ObservedCreatureMissing,
                ancestryCoverageIsComplete,
                confirmationObservationCount: 0,
                requiredObservationCount: _policy.RequiredAbsentObservations,
                pending.AbsentObservationCount,
                livingDescendantCount,
                previousOrdinals,
                previousTracks,
                EmptyOrdinals,
                EmptyTracks,
                relations);
        }

        private void WriteEvent(
            ClusterHistoryEventKind kind,
            ClusterHistoryEventStatus status,
            ClusterHistoryUnresolvedReason unresolvedReason,
            long firstObservedTick,
            long lastObservedTick,
            GeneticClusterObservation observation,
            bool eventHistoryIsComplete,
            bool ancestryCoverageIsComplete,
            int confirmationObservationCount,
            int requiredObservationCount,
            int consecutiveAbsentObservationCount,
            int livingDescendantCount,
            int[] previousClusterOrdinals,
            long[] previousTrackIds,
            int[] currentClusterOrdinals,
            long[] currentTrackIds,
            GeneticClusterRelation[] relations)
        {
            _events.TryWrite(new ClusterHistoryEvent(
                kind,
                status,
                unresolvedReason,
                firstObservedTick,
                lastObservedTick,
                observation.Threshold,
                observation.IsSampled,
                observation.SourcePopulationCount,
                observation.SampleLimit,
                eventHistoryIsComplete,
                ancestryCoverageIsComplete,
                _policy,
                confirmationObservationCount,
                requiredObservationCount,
                consecutiveAbsentObservationCount,
                livingDescendantCount,
                previousClusterOrdinals,
                previousTrackIds,
                currentClusterOrdinals,
                currentTrackIds,
                relations));
        }
    }
}

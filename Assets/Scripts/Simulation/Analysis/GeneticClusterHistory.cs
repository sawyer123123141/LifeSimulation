using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Bounded external analysis of ancestry-supported cluster continuity and disappearance.</summary>
    public sealed class GeneticClusterHistory
    {
        private static readonly int[] EmptyOrdinals = Array.Empty<int>();
        private static readonly long[] EmptyTracks = Array.Empty<long>();
        private static readonly GeneticClusterRelation[] EmptyRelations = Array.Empty<GeneticClusterRelation>();

        private readonly ClusterHistoryPolicy _policy;
        private readonly ClusterHistoryEventBuffer _events;
        private readonly List<PendingConfirmation> _pendingConfirmations = new List<PendingConfirmation>();
        private readonly List<PendingDisappearance> _pendingDisappearances = new List<PendingDisappearance>();
        private AncestryHistory? _ancestry;
        private GeneticClusterObservation? _previousObservation;
        private int[] _previousClusterOrdinals = EmptyOrdinals;
        private long[] _previousTrackIds = EmptyTracks;
        private bool _previousCanSupportRelations;
        private bool _segmentInitialized;
        private long _lastTick;
        private float _threshold;
        private bool _isSampled;
        private int _sampleLimit;
        private long _nextTrackId = 1;

        public GeneticClusterHistory(ClusterHistoryPolicy policy, ClusterHistoryEventBuffer events)
        {
            ValidatePolicy(policy);
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _policy = policy;
        }

        public ClusterHistoryPolicy Policy => _policy;
        public ClusterHistoryEventBuffer Events => _events;
        public int RetainedCandidateCount => _pendingConfirmations.Count;
        public int RetainedDisappearanceCount => _pendingDisappearances.Count;

        public void Record(GeneticClusterObservation observation, AncestryHistory ancestry)
        {
            RecordCore(observation, ancestry);
        }

        public void Record(GeneticClusterObservation observation, AncestryHistory ancestry, ClusterHistoryPolicy policy)
        {
            ValidatePolicy(policy);
            if (policy != _policy) throw new ArgumentException("The policy must remain identical within one history segment.", nameof(policy));
            RecordCore(observation, ancestry);
        }

        private void RecordCore(GeneticClusterObservation observation, AncestryHistory ancestry)
        {
            if (observation == null) throw new ArgumentNullException(nameof(observation));
            if (ancestry == null) throw new ArgumentNullException(nameof(ancestry));
            ValidateSegment(observation);
            if (_ancestry == null)
            {
                _ancestry = ancestry;
            }
            else if (!ReferenceEquals(_ancestry, ancestry))
            {
                throw new ArgumentException("The ancestry source must remain identical within one history segment.", nameof(ancestry));
            }

            int[] currentClusterOrdinals = GetClusterOrdinals(observation);
            ClusterHistoryUnresolvedReason evidenceReason = GetObservationEvidenceReason(observation, ancestry);
            bool evidenceIsComplete = evidenceReason == ClusterHistoryUnresolvedReason.None;

            if (!_segmentInitialized)
            {
                StartSegment(observation);
                long[] initialTrackIds = AllocateTracks(currentClusterOrdinals.Length);
                if (!evidenceIsComplete)
                {
                    WriteEvent(
                        ClusterHistoryEventKind.IncompleteEvidence,
                        ClusterHistoryEventStatus.Unresolved,
                        evidenceReason,
                        observation.Tick,
                        observation.Tick,
                        observation,
                        eventHistoryIsComplete: false,
                        ancestryCoverageIsComplete: false,
                        confirmationObservationCount: 0,
                        requiredObservationCount: 0,
                        consecutiveAbsentObservationCount: 0,
                        livingDescendantCount: 0,
                        EmptyOrdinals,
                        EmptyTracks,
                        currentClusterOrdinals,
                        initialTrackIds,
                        EmptyRelations);
                }

                StorePrevious(observation, currentClusterOrdinals, initialTrackIds, evidenceIsComplete);
                return;
            }

            _lastTick = observation.Tick;
            if (!evidenceIsComplete)
            {
                FailPendingEvidence(observation, evidenceReason);
                long[] incompleteTrackIds = AllocateTracks(currentClusterOrdinals.Length);
                WriteEvent(
                    ClusterHistoryEventKind.IncompleteEvidence,
                    ClusterHistoryEventStatus.Unresolved,
                    evidenceReason,
                    observation.Tick,
                    observation.Tick,
                    observation,
                    eventHistoryIsComplete: false,
                    ancestryCoverageIsComplete: false,
                    confirmationObservationCount: 0,
                    requiredObservationCount: 0,
                    consecutiveAbsentObservationCount: 0,
                    livingDescendantCount: 0,
                    _previousClusterOrdinals,
                    _previousTrackIds,
                    currentClusterOrdinals,
                    incompleteTrackIds,
                    EmptyRelations);
                StorePrevious(observation, currentClusterOrdinals, incompleteTrackIds, canSupportRelations: false);
                return;
            }

            if (!_previousCanSupportRelations || _previousObservation == null)
            {
                long[] baselineTrackIds = AllocateTracks(currentClusterOrdinals.Length);
                StorePrevious(observation, currentClusterOrdinals, baselineTrackIds, canSupportRelations: true);
                return;
            }

            ProcessTransition(observation, currentClusterOrdinals, ancestry);
        }

        private void ProcessTransition(
            GeneticClusterObservation currentObservation,
            int[] currentClusterOrdinals,
            AncestryHistory ancestry)
        {
            GeneticClusterObservation previousObservation = _previousObservation!;
            int previousCount = _previousClusterOrdinals.Length;
            int currentCount = currentClusterOrdinals.Length;
            GeneticClusterRelation[] relations = CreateRelations(
                previousObservation,
                _previousClusterOrdinals,
                currentObservation,
                currentClusterOrdinals,
                ancestry);

            if (!AllRelationsHaveCompleteCoverage(relations))
            {
                FailPendingEvidence(currentObservation, ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete);
                long[] unresolvedTrackIds = AllocateTracks(currentCount);
                WriteEvent(
                    ClusterHistoryEventKind.IncompleteEvidence,
                    ClusterHistoryEventStatus.Unresolved,
                    ClusterHistoryUnresolvedReason.AncestryCoverageIncomplete,
                    previousObservation.Tick,
                    currentObservation.Tick,
                    currentObservation,
                    eventHistoryIsComplete: true,
                    ancestryCoverageIsComplete: false,
                    confirmationObservationCount: 0,
                    requiredObservationCount: 0,
                    consecutiveAbsentObservationCount: 0,
                    livingDescendantCount: 0,
                    _previousClusterOrdinals,
                    _previousTrackIds,
                    currentClusterOrdinals,
                    unresolvedTrackIds,
                    relations);
                StorePrevious(currentObservation, currentClusterOrdinals, unresolvedTrackIds, canSupportRelations: false);
                return;
            }

            int[] previousStrongCounts = new int[previousCount];
            int[] currentStrongCounts = new int[currentCount];
            CountStrongRelations(relations, currentCount, previousStrongCounts, currentStrongCounts);
            int[] previousComponents = CreateUnassignedArray(previousCount);
            int[] currentComponents = CreateUnassignedArray(currentCount);
            int[] componentPreviousCounts = new int[Math.Max(1, previousCount)];
            int[] componentCurrentCounts = new int[Math.Max(1, previousCount)];
            int componentCount = BuildStrongComponents(
                relations,
                currentCount,
                previousStrongCounts,
                previousComponents,
                currentComponents,
                componentPreviousCounts,
                componentCurrentCounts);

            long[] currentTrackIds = AllocateCurrentTracks(
                relations,
                currentCount,
                currentStrongCounts,
                previousComponents,
                componentPreviousCounts,
                componentCurrentCounts);

            AdvancePendingConfirmations(
                currentObservation,
                currentClusterOrdinals,
                currentTrackIds,
                relations,
                currentStrongCounts,
                previousStrongCounts,
                previousComponents,
                componentPreviousCounts,
                componentCurrentCounts);
            AdvancePendingDisappearances(currentObservation, ancestry);

            EmitStrongComponentEvents(
                componentCount,
                previousComponents,
                currentComponents,
                componentPreviousCounts,
                componentCurrentCounts,
                currentObservation,
                currentClusterOrdinals,
                currentTrackIds,
                relations);
            EmitNewDisappearances(
                previousStrongCounts,
                currentObservation,
                currentClusterOrdinals,
                ancestry,
                relations,
                currentCount);
            EmitUnresolvedArrivals(
                currentStrongCounts,
                currentObservation,
                currentClusterOrdinals,
                currentTrackIds,
                relations,
                currentCount);

            StorePrevious(currentObservation, currentClusterOrdinals, currentTrackIds, canSupportRelations: true);
        }

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

        private long[] AllocateCurrentTracks(
            GeneticClusterRelation[] relations,
            int currentCount,
            int[] currentStrongCounts,
            int[] previousComponents,
            int[] componentPreviousCounts,
            int[] componentCurrentCounts)
        {
            var currentTrackIds = new long[currentCount];
            for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
            {
                if (currentStrongCounts[currentIndex] == 1)
                {
                    int previousIndex = FindOnlyStrongPrevious(relations, currentCount, currentIndex);
                    int component = previousIndex < 0 ? -1 : previousComponents[previousIndex];
                    if (component >= 0
                        && componentPreviousCounts[component] == 1
                        && componentCurrentCounts[component] == 1)
                    {
                        currentTrackIds[currentIndex] = _previousTrackIds[previousIndex];
                        continue;
                    }
                }

                currentTrackIds[currentIndex] = _nextTrackId++;
            }
            return currentTrackIds;
        }

        private long[] AllocateTracks(int count)
        {
            var trackIds = new long[count];
            for (int index = 0; index < count; index++) trackIds[index] = _nextTrackId++;
            return trackIds;
        }

        private void ValidateSegment(GeneticClusterObservation observation)
        {
            if (observation.Tick < 0) throw new ArgumentOutOfRangeException(nameof(observation));
            if (!_segmentInitialized) return;
            if (observation.Tick <= _lastTick) throw new ArgumentOutOfRangeException(nameof(observation), "Observation ticks must be strictly increasing.");
            if (observation.Threshold != _threshold) throw new ArgumentException("The threshold must remain identical within one history segment.", nameof(observation));
            if (observation.IsSampled != _isSampled) throw new ArgumentException("Full and sampled observations cannot share one history segment.", nameof(observation));
            if (observation.SampleLimit != _sampleLimit) throw new ArgumentException("The sample limit must remain identical within one history segment.", nameof(observation));
        }

        private void StartSegment(GeneticClusterObservation observation)
        {
            _segmentInitialized = true;
            _lastTick = observation.Tick;
            _threshold = observation.Threshold;
            _isSampled = observation.IsSampled;
            _sampleLimit = observation.SampleLimit;
        }

        private void StorePrevious(
            GeneticClusterObservation observation,
            int[] clusterOrdinals,
            long[] trackIds,
            bool canSupportRelations)
        {
            _previousObservation = observation;
            _previousClusterOrdinals = clusterOrdinals;
            _previousTrackIds = trackIds;
            _previousCanSupportRelations = canSupportRelations;
        }

        private static ClusterHistoryUnresolvedReason GetObservationEvidenceReason(
            GeneticClusterObservation observation,
            AncestryHistory ancestry)
        {
            if (!ancestry.IsComplete) return ClusterHistoryUnresolvedReason.AncestryIncomplete;
            if (ancestry.CompleteThroughTick < observation.Tick)
            {
                return ClusterHistoryUnresolvedReason.AncestryNotRecordedThroughObservation;
            }
            for (int index = 0; index < observation.Snapshot.Count; index++)
            {
                if (!ancestry.TryGet(observation.Snapshot.GetIdAt(index), out _))
                {
                    return ClusterHistoryUnresolvedReason.ObservedCreatureMissing;
                }
            }
            return ClusterHistoryUnresolvedReason.None;
        }

        private static int[] GetClusterOrdinals(GeneticClusterObservation observation)
        {
            var ordinals = new int[observation.ClusterCount];
            int count = 0;
            for (int sampleIndex = 0; sampleIndex < observation.Snapshot.Count; sampleIndex++)
            {
                int ordinal = observation.GetClusterIndexAt(sampleIndex);
                bool alreadyRecorded = false;
                for (int index = 0; index < count; index++)
                {
                    if (ordinals[index] == ordinal) alreadyRecorded = true;
                }
                if (!alreadyRecorded) ordinals[count++] = ordinal;
            }
            return ordinals;
        }

        private GeneticClusterRelation[] CreateRelations(
            GeneticClusterObservation previousObservation,
            int[] previousOrdinals,
            GeneticClusterObservation currentObservation,
            int[] currentOrdinals,
            AncestryHistory ancestry)
        {
            var relations = new GeneticClusterRelation[previousOrdinals.Length * currentOrdinals.Length];
            for (int previousIndex = 0; previousIndex < previousOrdinals.Length; previousIndex++)
            {
                for (int currentIndex = 0; currentIndex < currentOrdinals.Length; currentIndex++)
                {
                    relations[(previousIndex * currentOrdinals.Length) + currentIndex] = GeneticClusterRelation.Create(
                        previousObservation,
                        previousOrdinals[previousIndex],
                        currentObservation,
                        currentOrdinals[currentIndex],
                        ancestry,
                        _policy);
                }
            }
            return relations;
        }

        private static void CountStrongRelations(
            GeneticClusterRelation[] relations,
            int currentCount,
            int[] previousStrongCounts,
            int[] currentStrongCounts)
        {
            for (int previousIndex = 0; previousIndex < previousStrongCounts.Length; previousIndex++)
            {
                for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
                {
                    if (!relations[(previousIndex * currentCount) + currentIndex].IsStrong) continue;
                    previousStrongCounts[previousIndex]++;
                    currentStrongCounts[currentIndex]++;
                }
            }
        }

        private static int BuildStrongComponents(
            GeneticClusterRelation[] relations,
            int currentCount,
            int[] previousStrongCounts,
            int[] previousComponents,
            int[] currentComponents,
            int[] componentPreviousCounts,
            int[] componentCurrentCounts)
        {
            int componentCount = 0;
            for (int seedPrevious = 0; seedPrevious < previousStrongCounts.Length; seedPrevious++)
            {
                if (previousStrongCounts[seedPrevious] == 0 || previousComponents[seedPrevious] >= 0) continue;
                int component = componentCount++;
                previousComponents[seedPrevious] = component;
                bool changed;
                do
                {
                    changed = false;
                    for (int previousIndex = 0; previousIndex < previousStrongCounts.Length; previousIndex++)
                    {
                        if (previousComponents[previousIndex] != component) continue;
                        for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
                        {
                            if (!relations[(previousIndex * currentCount) + currentIndex].IsStrong
                                || currentComponents[currentIndex] == component) continue;
                            currentComponents[currentIndex] = component;
                            changed = true;
                        }
                    }
                    for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
                    {
                        if (currentComponents[currentIndex] != component) continue;
                        for (int previousIndex = 0; previousIndex < previousStrongCounts.Length; previousIndex++)
                        {
                            if (!relations[(previousIndex * currentCount) + currentIndex].IsStrong
                                || previousComponents[previousIndex] == component) continue;
                            previousComponents[previousIndex] = component;
                            changed = true;
                        }
                    }
                } while (changed);
            }

            for (int index = 0; index < previousComponents.Length; index++)
            {
                if (previousComponents[index] >= 0) componentPreviousCounts[previousComponents[index]]++;
            }
            for (int index = 0; index < currentComponents.Length; index++)
            {
                if (currentComponents[index] >= 0) componentCurrentCounts[currentComponents[index]]++;
            }
            return componentCount;
        }

        private static int CountLivingDescendants(
            GeneticClusterObservation lastObservation,
            int lastClusterOrdinal,
            GeneticClusterObservation currentObservation,
            AncestryHistory ancestry,
            out bool ancestryCoverageIsComplete)
        {
            CreatureId[] lastMemberIds = GetClusterMemberIds(lastObservation, lastClusterOrdinal);
            ancestryCoverageIsComplete = true;
            CreatureId[] noTargetIds = Array.Empty<CreatureId>();
            for (int index = 0; index < lastMemberIds.Length; index++)
            {
                if (!ancestry.TryGet(lastMemberIds[index], out _))
                {
                    ancestryCoverageIsComplete = false;
                    continue;
                }

                HasRecordedAncestor(lastMemberIds[index], noTargetIds, ancestry, ref ancestryCoverageIsComplete);
            }

            int livingDescendantCount = 0;
            for (int currentIndex = 0; currentIndex < currentObservation.Snapshot.Count; currentIndex++)
            {
                CreatureId currentId = currentObservation.Snapshot.GetIdAt(currentIndex);
                if (Contains(currentId, lastMemberIds)
                    || HasRecordedAncestor(currentId, lastMemberIds, ancestry, ref ancestryCoverageIsComplete))
                {
                    livingDescendantCount++;
                }
            }
            return livingDescendantCount;
        }

        private static bool HasRecordedAncestor(
            CreatureId creatureId,
            CreatureId[] targetIds,
            AncestryHistory ancestry,
            ref bool ancestryCoverageIsComplete)
        {
            var visitedIds = new CreatureId[8];
            int visitedCount = 1;
            visitedIds[0] = creatureId;
            var activeIds = new CreatureId[8];
            var activeNextParents = new byte[8];
            int activeCount = 1;
            activeIds[0] = creatureId;

            while (activeCount > 0)
            {
                int activeIndex = activeCount - 1;
                CreatureId activeId = activeIds[activeIndex];
                if (Contains(activeId, targetIds)) return true;
                if (!ancestry.TryGet(activeId, out AncestryRecord record))
                {
                    ancestryCoverageIsComplete = false;
                    activeCount--;
                    continue;
                }

                CreatureId parentId;
                if (activeNextParents[activeIndex] == 0)
                {
                    activeNextParents[activeIndex] = 1;
                    parentId = record.FirstParent;
                }
                else if (activeNextParents[activeIndex] == 1)
                {
                    activeNextParents[activeIndex] = 2;
                    parentId = record.SecondParent;
                }
                else
                {
                    activeCount--;
                    continue;
                }

                if (parentId.Value == 0) continue;
                if (Contains(parentId, activeIds, activeCount))
                {
                    ancestryCoverageIsComplete = false;
                    continue;
                }
                if (Contains(parentId, visitedIds, visitedCount)) continue;

                EnsureCapacity(ref visitedIds, visitedCount + 1);
                visitedIds[visitedCount++] = parentId;
                EnsureCapacity(ref activeIds, ref activeNextParents, activeCount + 1);
                activeIds[activeCount] = parentId;
                activeNextParents[activeCount] = 0;
                activeCount++;
            }

            return false;
        }

        private static CreatureId[] GetClusterMemberIds(GeneticClusterObservation observation, int clusterOrdinal)
        {
            int count = 0;
            for (int index = 0; index < observation.Snapshot.Count; index++)
            {
                if (observation.GetClusterIndexAt(index) == clusterOrdinal) count++;
            }
            var ids = new CreatureId[count];
            int memberIndex = 0;
            for (int index = 0; index < observation.Snapshot.Count; index++)
            {
                if (observation.GetClusterIndexAt(index) == clusterOrdinal)
                {
                    ids[memberIndex++] = observation.Snapshot.GetIdAt(index);
                }
            }
            return ids;
        }

        private static int[] CreateUnassignedArray(int count)
        {
            var values = new int[count];
            for (int index = 0; index < count; index++) values[index] = -1;
            return values;
        }

        private static int FindOnlyStrongCurrent(GeneticClusterRelation[] relations, int currentCount, int previousIndex)
        {
            for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
            {
                if (relations[(previousIndex * currentCount) + currentIndex].IsStrong) return currentIndex;
            }
            return -1;
        }

        private static int FindOnlyStrongPrevious(GeneticClusterRelation[] relations, int currentCount, int currentIndex)
        {
            int previousCount = currentCount == 0 ? 0 : relations.Length / currentCount;
            for (int previousIndex = 0; previousIndex < previousCount; previousIndex++)
            {
                if (relations[(previousIndex * currentCount) + currentIndex].IsStrong) return previousIndex;
            }
            return -1;
        }

        private static int IndexOf(long value, long[] values)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == value) return index;
            }
            return -1;
        }

        private static bool AllRelationsHaveCompleteCoverage(GeneticClusterRelation[] relations)
        {
            for (int index = 0; index < relations.Length; index++)
            {
                if (!relations[index].AncestryCoverageIsComplete) return false;
            }
            return true;
        }

        private static bool Contains(CreatureId value, CreatureId[] values)
        {
            return Contains(value, values, values.Length);
        }

        private static bool Contains(CreatureId value, CreatureId[] values, int count)
        {
            for (int index = 0; index < count; index++)
            {
                if (values[index].Equals(value)) return true;
            }
            return false;
        }

        private static int[] Select(int[] values, int[] components, int component)
        {
            int count = 0;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == component) count++;
            }
            var selected = new int[count];
            int selectedIndex = 0;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == component) selected[selectedIndex++] = values[index];
            }
            return selected;
        }

        private static long[] Select(long[] values, int[] components, int component)
        {
            int count = 0;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == component) count++;
            }
            var selected = new long[count];
            int selectedIndex = 0;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] == component) selected[selectedIndex++] = values[index];
            }
            return selected;
        }

        private static GeneticClusterRelation[] SelectRelations(
            GeneticClusterRelation[] relations,
            int currentCount,
            int[] previousComponents,
            int[] currentComponents,
            int component)
        {
            int relationCount = 0;
            for (int previousIndex = 0; previousIndex < previousComponents.Length; previousIndex++)
            {
                if (previousComponents[previousIndex] != component) continue;
                for (int currentIndex = 0; currentIndex < currentComponents.Length; currentIndex++)
                {
                    if (currentComponents[currentIndex] == component) relationCount++;
                }
            }
            var selected = new GeneticClusterRelation[relationCount];
            int selectedIndex = 0;
            for (int previousIndex = 0; previousIndex < previousComponents.Length; previousIndex++)
            {
                if (previousComponents[previousIndex] != component) continue;
                for (int currentIndex = 0; currentIndex < currentComponents.Length; currentIndex++)
                {
                    if (currentComponents[currentIndex] != component) continue;
                    selected[selectedIndex++] = relations[(previousIndex * currentCount) + currentIndex];
                }
            }
            return selected;
        }

        private static GeneticClusterRelation[] SelectPreviousRelations(
            GeneticClusterRelation[] relations,
            int currentCount,
            int previousIndex)
        {
            var selected = new GeneticClusterRelation[currentCount];
            for (int currentIndex = 0; currentIndex < currentCount; currentIndex++)
            {
                selected[currentIndex] = relations[(previousIndex * currentCount) + currentIndex];
            }
            return selected;
        }

        private static GeneticClusterRelation[] SelectCurrentRelations(
            GeneticClusterRelation[] relations,
            int currentCount,
            int currentIndex)
        {
            int previousCount = currentCount == 0 ? 0 : relations.Length / currentCount;
            var selected = new GeneticClusterRelation[previousCount];
            for (int previousIndex = 0; previousIndex < previousCount; previousIndex++)
            {
                selected[previousIndex] = relations[(previousIndex * currentCount) + currentIndex];
            }
            return selected;
        }

        private static void EnsureCapacity(ref CreatureId[] values, int required)
        {
            if (required <= values.Length) return;
            Array.Resize(ref values, Math.Max(required, values.Length * 2));
        }

        private static void EnsureCapacity(ref CreatureId[] values, ref byte[] nextParents, int required)
        {
            if (required <= values.Length) return;
            int nextCapacity = Math.Max(required, values.Length * 2);
            Array.Resize(ref values, nextCapacity);
            Array.Resize(ref nextParents, nextCapacity);
        }

        private static void ValidatePolicy(ClusterHistoryPolicy policy)
        {
            if (policy.MinimumSupportedCurrentMembers <= 0
                || !(policy.MinimumCurrentSupportFraction > 0f && policy.MinimumCurrentSupportFraction <= 1f)
                || policy.MinimumSupportingPreviousMembers <= 0
                || !(policy.MinimumPreviousSupportFraction > 0f && policy.MinimumPreviousSupportFraction <= 1f)
                || policy.MaximumAncestorGenerations <= 0
                || policy.RequiredSuccessorObservations <= 0
                || policy.RequiredAbsentObservations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        private sealed class PendingConfirmation
        {
            public PendingConfirmation(
                ClusterHistoryEventKind kind,
                long firstObservedTick,
                long candidateTick,
                int[] previousClusterOrdinals,
                long[] previousTrackIds,
                int[] currentClusterOrdinals,
                long[] currentTrackIds,
                GeneticClusterRelation[] relations)
            {
                Kind = kind;
                FirstObservedTick = firstObservedTick;
                CandidateTick = candidateTick;
                PreviousClusterOrdinals = previousClusterOrdinals;
                PreviousTrackIds = previousTrackIds;
                CurrentClusterOrdinals = currentClusterOrdinals;
                CurrentTrackIds = currentTrackIds;
                Relations = relations;
            }

            public ClusterHistoryEventKind Kind { get; }
            public long FirstObservedTick { get; }
            public long CandidateTick { get; }
            public int[] PreviousClusterOrdinals { get; }
            public long[] PreviousTrackIds { get; }
            public int[] CurrentClusterOrdinals { get; }
            public long[] CurrentTrackIds { get; }
            public GeneticClusterRelation[] Relations { get; }
            public int SuccessfulObservationCount { get; set; }
        }

        private sealed class PendingDisappearance
        {
            public PendingDisappearance(
                GeneticClusterObservation lastObservation,
                int lastClusterOrdinal,
                long trackId,
                int absentObservationCount)
            {
                LastObservation = lastObservation;
                LastClusterOrdinal = lastClusterOrdinal;
                TrackId = trackId;
                AbsentObservationCount = absentObservationCount;
            }

            public GeneticClusterObservation LastObservation { get; }
            public int LastClusterOrdinal { get; }
            public long TrackId { get; }
            public int AbsentObservationCount { get; set; }
        }
    }
}

#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Bounded external analysis of ancestry-supported cluster continuity and disappearance.</summary>
    public sealed partial class GeneticClusterHistory
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
    }
}

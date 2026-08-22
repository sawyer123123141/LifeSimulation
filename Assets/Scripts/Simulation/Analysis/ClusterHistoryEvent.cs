using System;

namespace LifeSimulation.Simulation.Analysis
{
    public enum ClusterHistoryEventKind : byte
    {
        Continuity = 0,
        CandidateSplit = 1,
        ConfirmedSplit = 2,
        CandidateMerge = 3,
        ConfirmedMerge = 4,
        AmbiguousReorganisation = 5,
        UnresolvedArrival = 6,
        PendingDisappearance = 7,
        UnresolvedCandidate = 8,
        UnresolvedDisappearance = 9,
        IncompleteEvidence = 10,
        ConfirmedLineageExtinction = 11
    }

    public enum ClusterHistoryEventStatus : byte
    {
        Candidate = 0,
        Confirmed = 1,
        Unresolved = 2
    }

    public enum ClusterHistoryUnresolvedReason : byte
    {
        None = 0,
        AncestryIncomplete = 1,
        AncestryNotRecordedThroughObservation = 2,
        ObservedCreatureMissing = 3,
        AncestryCoverageIncomplete = 4,
        CandidateDidNotPersist = 5,
        AmbiguousStrongRelations = 6,
        NoStrongPredecessor = 7,
        SampledObservation = 8,
        LivingDescendant = 9,
        StrongDescendantAfterGap = 10
    }

    /// <summary>Immutable host-facing evidence for one ancestry-aware cluster-history result.</summary>
    public readonly struct ClusterHistoryEvent
    {
        private readonly int[] _previousClusterOrdinals;
        private readonly long[] _previousTrackIds;
        private readonly int[] _currentClusterOrdinals;
        private readonly long[] _currentTrackIds;
        private readonly GeneticClusterRelation[] _relations;

        internal ClusterHistoryEvent(
            ClusterHistoryEventKind kind,
            ClusterHistoryEventStatus status,
            ClusterHistoryUnresolvedReason unresolvedReason,
            long firstObservedTick,
            long lastObservedTick,
            float threshold,
            bool isSampled,
            int sourcePopulationCount,
            int sampleLimit,
            bool eventHistoryIsComplete,
            bool ancestryCoverageIsComplete,
            ClusterHistoryPolicy policy,
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
            Kind = kind;
            Status = status;
            UnresolvedReason = unresolvedReason;
            FirstObservedTick = firstObservedTick;
            LastObservedTick = lastObservedTick;
            Threshold = threshold;
            IsSampled = isSampled;
            SourcePopulationCount = sourcePopulationCount;
            SampleLimit = sampleLimit;
            EventHistoryIsComplete = eventHistoryIsComplete;
            AncestryCoverageIsComplete = ancestryCoverageIsComplete;
            Policy = policy;
            ConfirmationObservationCount = confirmationObservationCount;
            RequiredObservationCount = requiredObservationCount;
            ConsecutiveAbsentObservationCount = consecutiveAbsentObservationCount;
            LivingDescendantCount = livingDescendantCount;
            _previousClusterOrdinals = previousClusterOrdinals;
            _previousTrackIds = previousTrackIds;
            _currentClusterOrdinals = currentClusterOrdinals;
            _currentTrackIds = currentTrackIds;
            _relations = relations;
        }

        public ClusterHistoryEventKind Kind { get; }
        public ClusterHistoryEventStatus Status { get; }
        public ClusterHistoryUnresolvedReason UnresolvedReason { get; }
        public long FirstObservedTick { get; }
        public long LastObservedTick { get; }
        public float Threshold { get; }
        public bool IsSampled { get; }
        public int SourcePopulationCount { get; }
        public int SampleLimit { get; }
        public bool EventHistoryIsComplete { get; }
        public bool AncestryCoverageIsComplete { get; }
        public ClusterHistoryPolicy Policy { get; }
        public int ConfirmationObservationCount { get; }
        public int RequiredObservationCount { get; }
        public int ConsecutiveAbsentObservationCount { get; }
        public int LivingDescendantCount { get; }
        public int PreviousTrackCount => _previousTrackIds == null ? 0 : _previousTrackIds.Length;
        public int CurrentTrackCount => _currentTrackIds == null ? 0 : _currentTrackIds.Length;
        public int RelationCount => _relations == null ? 0 : _relations.Length;

        public int GetPreviousClusterOrdinalAt(int index)
        {
            if ((uint)index >= (uint)PreviousTrackCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _previousClusterOrdinals[index];
        }

        public long GetPreviousTrackIdAt(int index)
        {
            if ((uint)index >= (uint)PreviousTrackCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _previousTrackIds[index];
        }

        public int GetCurrentClusterOrdinalAt(int index)
        {
            if ((uint)index >= (uint)CurrentTrackCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _currentClusterOrdinals[index];
        }

        public long GetCurrentTrackIdAt(int index)
        {
            if ((uint)index >= (uint)CurrentTrackCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _currentTrackIds[index];
        }

        public GeneticClusterRelation GetRelationAt(int index)
        {
            if ((uint)index >= (uint)RelationCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _relations[index];
        }
    }
}

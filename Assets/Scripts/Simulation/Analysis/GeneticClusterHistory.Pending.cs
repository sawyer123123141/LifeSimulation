#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>The evidence a decision is waiting on: candidates and disappearances in flight.</summary>
    public sealed partial class GeneticClusterHistory
    {
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

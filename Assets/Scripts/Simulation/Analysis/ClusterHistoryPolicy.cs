using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Explicit conservative thresholds for ancestry-aware cluster-history analysis.</summary>
    public readonly struct ClusterHistoryPolicy : IEquatable<ClusterHistoryPolicy>
    {
        public ClusterHistoryPolicy(
            int minimumSupportedCurrentMembers,
            float minimumCurrentSupportFraction,
            int minimumSupportingPreviousMembers,
            float minimumPreviousSupportFraction,
            int maximumAncestorGenerations,
            int requiredSuccessorObservations,
            int requiredAbsentObservations)
        {
            if (minimumSupportedCurrentMembers <= 0) throw new ArgumentOutOfRangeException(nameof(minimumSupportedCurrentMembers));
            if (!(minimumCurrentSupportFraction > 0f && minimumCurrentSupportFraction <= 1f)) throw new ArgumentOutOfRangeException(nameof(minimumCurrentSupportFraction));
            if (minimumSupportingPreviousMembers <= 0) throw new ArgumentOutOfRangeException(nameof(minimumSupportingPreviousMembers));
            if (!(minimumPreviousSupportFraction > 0f && minimumPreviousSupportFraction <= 1f)) throw new ArgumentOutOfRangeException(nameof(minimumPreviousSupportFraction));
            if (maximumAncestorGenerations <= 0) throw new ArgumentOutOfRangeException(nameof(maximumAncestorGenerations));
            if (requiredSuccessorObservations <= 0) throw new ArgumentOutOfRangeException(nameof(requiredSuccessorObservations));
            if (requiredAbsentObservations <= 0) throw new ArgumentOutOfRangeException(nameof(requiredAbsentObservations));

            MinimumSupportedCurrentMembers = minimumSupportedCurrentMembers;
            MinimumCurrentSupportFraction = minimumCurrentSupportFraction;
            MinimumSupportingPreviousMembers = minimumSupportingPreviousMembers;
            MinimumPreviousSupportFraction = minimumPreviousSupportFraction;
            MaximumAncestorGenerations = maximumAncestorGenerations;
            RequiredSuccessorObservations = requiredSuccessorObservations;
            RequiredAbsentObservations = requiredAbsentObservations;
        }

        public int MinimumSupportedCurrentMembers { get; }
        public float MinimumCurrentSupportFraction { get; }
        public int MinimumSupportingPreviousMembers { get; }
        public float MinimumPreviousSupportFraction { get; }
        public int MaximumAncestorGenerations { get; }
        public int RequiredSuccessorObservations { get; }
        public int RequiredAbsentObservations { get; }

        public bool Equals(ClusterHistoryPolicy other)
        {
            return MinimumSupportedCurrentMembers == other.MinimumSupportedCurrentMembers
                && MinimumCurrentSupportFraction == other.MinimumCurrentSupportFraction
                && MinimumSupportingPreviousMembers == other.MinimumSupportingPreviousMembers
                && MinimumPreviousSupportFraction == other.MinimumPreviousSupportFraction
                && MaximumAncestorGenerations == other.MaximumAncestorGenerations
                && RequiredSuccessorObservations == other.RequiredSuccessorObservations
                && RequiredAbsentObservations == other.RequiredAbsentObservations;
        }

        public override bool Equals(object? obj) => obj is ClusterHistoryPolicy other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MinimumSupportedCurrentMembers;
                hashCode = (hashCode * 397) ^ MinimumCurrentSupportFraction.GetHashCode();
                hashCode = (hashCode * 397) ^ MinimumSupportingPreviousMembers;
                hashCode = (hashCode * 397) ^ MinimumPreviousSupportFraction.GetHashCode();
                hashCode = (hashCode * 397) ^ MaximumAncestorGenerations;
                hashCode = (hashCode * 397) ^ RequiredSuccessorObservations;
                return (hashCode * 397) ^ RequiredAbsentObservations;
            }
        }

        public static bool operator ==(ClusterHistoryPolicy first, ClusterHistoryPolicy second) => first.Equals(second);
        public static bool operator !=(ClusterHistoryPolicy first, ClusterHistoryPolicy second) => !first.Equals(second);
    }
}

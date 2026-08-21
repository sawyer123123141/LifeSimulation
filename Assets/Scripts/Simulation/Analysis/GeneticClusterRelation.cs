using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Immutable ancestry evidence between one previous and one current observation-local cluster.</summary>
    public sealed class GeneticClusterRelation
    {
        private GeneticClusterRelation(
            int previousClusterOrdinal,
            int currentClusterOrdinal,
            int previousClusterMemberCount,
            int currentClusterMemberCount,
            int directSupportCount,
            int descendantOnlySupportCount,
            int supportedCurrentMemberCount,
            float currentSupportFraction,
            int supportingPreviousMemberCount,
            float previousSupportFraction,
            int minimumSupportingAncestorDepth,
            int maximumSupportingAncestorDepth,
            bool ancestryCoverageIsComplete,
            bool isStrong)
        {
            PreviousClusterOrdinal = previousClusterOrdinal;
            CurrentClusterOrdinal = currentClusterOrdinal;
            PreviousClusterMemberCount = previousClusterMemberCount;
            CurrentClusterMemberCount = currentClusterMemberCount;
            DirectSupportCount = directSupportCount;
            DescendantOnlySupportCount = descendantOnlySupportCount;
            SupportedCurrentMemberCount = supportedCurrentMemberCount;
            CurrentSupportFraction = currentSupportFraction;
            SupportingPreviousMemberCount = supportingPreviousMemberCount;
            PreviousSupportFraction = previousSupportFraction;
            MinimumSupportingAncestorDepth = minimumSupportingAncestorDepth;
            MaximumSupportingAncestorDepth = maximumSupportingAncestorDepth;
            AncestryCoverageIsComplete = ancestryCoverageIsComplete;
            IsStrong = isStrong;
        }

        public int PreviousClusterOrdinal { get; }
        public int CurrentClusterOrdinal { get; }
        public int PreviousClusterMemberCount { get; }
        public int CurrentClusterMemberCount { get; }
        public int DirectSupportCount { get; }
        public int DescendantOnlySupportCount { get; }
        public int SupportedCurrentMemberCount { get; }
        public float CurrentSupportFraction { get; }
        public int SupportingPreviousMemberCount { get; }
        public float PreviousSupportFraction { get; }
        public int MinimumSupportingAncestorDepth { get; }
        public int MaximumSupportingAncestorDepth { get; }
        public bool AncestryCoverageIsComplete { get; }
        public bool IsStrong { get; }

        public static GeneticClusterRelation Create(
            GeneticClusterObservation previousObservation,
            int previousClusterOrdinal,
            GeneticClusterObservation currentObservation,
            int currentClusterOrdinal,
            AncestryHistory ancestry,
            ClusterHistoryPolicy policy)
        {
            if (previousObservation == null) throw new ArgumentNullException(nameof(previousObservation));
            if (currentObservation == null) throw new ArgumentNullException(nameof(currentObservation));
            if (ancestry == null) throw new ArgumentNullException(nameof(ancestry));
            ValidatePolicy(policy);

            int previousClusterMemberCount = CountMembers(previousObservation, previousClusterOrdinal);
            if (previousClusterMemberCount == 0) throw new ArgumentOutOfRangeException(nameof(previousClusterOrdinal));
            int currentClusterMemberCount = CountMembers(currentObservation, currentClusterOrdinal);
            if (currentClusterMemberCount == 0) throw new ArgumentOutOfRangeException(nameof(currentClusterOrdinal));

            var previousMemberIds = new CreatureId[previousClusterMemberCount];
            int previousMemberIndex = 0;
            for (int sampleIndex = 0; sampleIndex < previousObservation.Snapshot.Count; sampleIndex++)
            {
                if (previousObservation.GetClusterIndexAt(sampleIndex) != previousClusterOrdinal) continue;
                previousMemberIds[previousMemberIndex++] = previousObservation.Snapshot.GetIdAt(sampleIndex);
            }

            var previousMemberSupportsCurrent = new bool[previousClusterMemberCount];
            var visitedIds = new CreatureId[8];
            var visitedDepths = new int[8];
            var activeIds = new CreatureId[8];
            var activeDepths = new int[8];
            var activeNextParents = new byte[8];
            int directSupportCount = 0;
            int descendantOnlySupportCount = 0;
            int supportedCurrentMemberCount = 0;
            int minimumSupportingAncestorDepth = -1;
            int maximumSupportingAncestorDepth = -1;
            bool ancestryCoverageIsComplete = true;

            for (int sampleIndex = 0; sampleIndex < currentObservation.Snapshot.Count; sampleIndex++)
            {
                if (currentObservation.GetClusterIndexAt(sampleIndex) != currentClusterOrdinal) continue;

                CreatureId currentMemberId = currentObservation.Snapshot.GetIdAt(sampleIndex);
                int visitedCount = 1;
                visitedIds[0] = currentMemberId;
                visitedDepths[0] = 0;
                int activeCount = 1;
                activeIds[0] = currentMemberId;
                activeDepths[0] = 0;
                activeNextParents[0] = 0;
                bool hasDirectSupport = false;
                bool hasAnySupport = false;

                while (activeCount > 0)
                {
                    int activeIndex = activeCount - 1;
                    int depth = activeDepths[activeIndex];
                    if (depth >= policy.MaximumAncestorGenerations)
                    {
                        activeCount--;
                        continue;
                    }
                    if (!ancestry.TryGet(activeIds[activeIndex], out AncestryRecord record))
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

                    int parentDepth = depth + 1;
                    int visitedIndex = IndexOf(parentId, visitedIds, visitedCount);
                    if (visitedIndex >= 0 && visitedDepths[visitedIndex] <= parentDepth) continue;
                    if (visitedIndex >= 0)
                    {
                        visitedDepths[visitedIndex] = parentDepth;
                    }
                    else
                    {
                        EnsureCapacity(ref visitedIds, ref visitedDepths, visitedCount + 1);
                        visitedIds[visitedCount] = parentId;
                        visitedDepths[visitedCount] = parentDepth;
                        visitedCount++;
                    }

                    EnsureCapacity(ref activeIds, ref activeDepths, ref activeNextParents, activeCount + 1);
                    activeIds[activeCount] = parentId;
                    activeDepths[activeCount] = parentDepth;
                    activeNextParents[activeCount] = 0;
                    activeCount++;
                }

                for (int visitedIndex = 0; visitedIndex < visitedCount; visitedIndex++)
                {
                    CreatureId traversedId = visitedIds[visitedIndex];
                    int depth = visitedDepths[visitedIndex];
                    for (int candidateIndex = 0; candidateIndex < previousMemberIds.Length; candidateIndex++)
                    {
                        if (!traversedId.Equals(previousMemberIds[candidateIndex])) continue;
                        previousMemberSupportsCurrent[candidateIndex] = true;
                        hasAnySupport = true;
                        if (depth == 0) hasDirectSupport = true;
                        if (minimumSupportingAncestorDepth < 0 || depth < minimumSupportingAncestorDepth)
                        {
                            minimumSupportingAncestorDepth = depth;
                        }
                        if (depth > maximumSupportingAncestorDepth)
                        {
                            maximumSupportingAncestorDepth = depth;
                        }
                    }
                }

                if (!hasAnySupport) continue;
                supportedCurrentMemberCount++;
                if (hasDirectSupport)
                {
                    directSupportCount++;
                }
                else
                {
                    descendantOnlySupportCount++;
                }
            }

            int supportingPreviousMemberCount = 0;
            for (int index = 0; index < previousMemberSupportsCurrent.Length; index++)
            {
                if (previousMemberSupportsCurrent[index]) supportingPreviousMemberCount++;
            }

            float currentSupportFraction = (float)supportedCurrentMemberCount / currentClusterMemberCount;
            float previousSupportFraction = (float)supportingPreviousMemberCount / previousClusterMemberCount;
            bool isStrong = ancestryCoverageIsComplete
                && supportedCurrentMemberCount >= policy.MinimumSupportedCurrentMembers
                && currentSupportFraction >= policy.MinimumCurrentSupportFraction
                && supportingPreviousMemberCount >= policy.MinimumSupportingPreviousMembers
                && previousSupportFraction >= policy.MinimumPreviousSupportFraction;

            return new GeneticClusterRelation(
                previousClusterOrdinal,
                currentClusterOrdinal,
                previousClusterMemberCount,
                currentClusterMemberCount,
                directSupportCount,
                descendantOnlySupportCount,
                supportedCurrentMemberCount,
                currentSupportFraction,
                supportingPreviousMemberCount,
                previousSupportFraction,
                minimumSupportingAncestorDepth,
                maximumSupportingAncestorDepth,
                ancestryCoverageIsComplete,
                isStrong);
        }

        private static int CountMembers(GeneticClusterObservation observation, int clusterOrdinal)
        {
            int count = 0;
            for (int sampleIndex = 0; sampleIndex < observation.Snapshot.Count; sampleIndex++)
            {
                if (observation.GetClusterIndexAt(sampleIndex) == clusterOrdinal) count++;
            }
            return count;
        }

        private static bool Contains(CreatureId candidate, CreatureId[] traversalIds, int traversalCount)
        {
            return IndexOf(candidate, traversalIds, traversalCount) >= 0;
        }

        private static int IndexOf(CreatureId candidate, CreatureId[] traversalIds, int traversalCount)
        {
            for (int index = 0; index < traversalCount; index++)
            {
                if (candidate.Equals(traversalIds[index])) return index;
            }
            return -1;
        }

        private static void EnsureCapacity(
            ref CreatureId[] traversalIds,
            ref int[] traversalDepths,
            int required)
        {
            if (required <= traversalIds.Length) return;
            int nextCapacity = Math.Max(required, traversalIds.Length * 2);
            Array.Resize(ref traversalIds, nextCapacity);
            Array.Resize(ref traversalDepths, nextCapacity);
        }

        private static void EnsureCapacity(
            ref CreatureId[] traversalIds,
            ref int[] traversalDepths,
            ref byte[] traversalNextParents,
            int required)
        {
            if (required <= traversalIds.Length) return;
            int nextCapacity = Math.Max(required, traversalIds.Length * 2);
            Array.Resize(ref traversalIds, nextCapacity);
            Array.Resize(ref traversalDepths, nextCapacity);
            Array.Resize(ref traversalNextParents, nextCapacity);
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
    }
}

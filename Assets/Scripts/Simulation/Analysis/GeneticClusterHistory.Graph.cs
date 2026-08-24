#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// The pure part: relation graphs, strong components, ancestry queries and array helpers.
    ///
    /// <para>Every method here is static and reads no field. That is the reason this is the seam -
    /// nothing in this file can affect a history's state, so it can be read on its own.</para>
    /// </summary>
    public sealed partial class GeneticClusterHistory
    {
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
    }
}

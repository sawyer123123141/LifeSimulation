using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;

namespace LifeSimulation.Simulation.Behavior
{
    public readonly struct ResourceObservation
    {
        public ResourceObservation(ResourceId resourceId, int resourceIndex, float distance, float remainingAmount = 0f)
        {
            ResourceId = resourceId;
            ResourceIndex = resourceIndex;
            Distance = distance;
            RemainingAmount = remainingAmount;
            IsValid = true;
        }

        public ResourceId ResourceId { get; }
        public int ResourceIndex { get; }
        public float Distance { get; }
        public float RemainingAmount { get; }
        public bool IsValid { get; }
    }

    public readonly struct CreatureObservation
    {
        public CreatureObservation(CreatureId creatureId, int creatureIndex, float distance)
        {
            CreatureId = creatureId;
            CreatureIndex = creatureIndex;
            Distance = distance;
            IsValid = true;
        }

        public CreatureId CreatureId { get; }
        public int CreatureIndex { get; }
        public float Distance { get; }
        public bool IsValid { get; }
    }

    public struct ResourceCandidateBuffer
    {
        public const int Capacity = 4;

        private ResourceObservation _candidate0;
        private ResourceObservation _candidate1;
        private ResourceObservation _candidate2;
        private ResourceObservation _candidate3;
        private int _count;

        public int Count => _count;

        public ResourceObservation GetAt(int index)
        {
            switch (index)
            {
                case 0: return _candidate0;
                case 1: return _candidate1;
                case 2: return _candidate2;
                default: return _candidate3;
            }
        }

        public void Consider(ResourceObservation candidate)
        {
            int insertionIndex = _count;
            for (int index = 0; index < _count; index++)
            {
                if (IsBefore(candidate, GetAt(index)))
                {
                    insertionIndex = index;
                    break;
                }
            }

            if (insertionIndex >= Capacity)
            {
                return;
            }

            int lastIndex = _count < Capacity ? _count : Capacity - 1;
            for (int index = lastIndex; index > insertionIndex; index--)
            {
                SetAt(index, GetAt(index - 1));
            }

            SetAt(insertionIndex, candidate);
            if (_count < Capacity)
            {
                _count++;
            }
        }

        private static bool IsBefore(ResourceObservation left, ResourceObservation right)
        {
            return left.Distance < right.Distance
                || (Math.Abs(left.Distance - right.Distance) <= 0.00001f && left.ResourceId.Value < right.ResourceId.Value);
        }

        private void SetAt(int index, ResourceObservation candidate)
        {
            switch (index)
            {
                case 0: _candidate0 = candidate; break;
                case 1: _candidate1 = candidate; break;
                case 2: _candidate2 = candidate; break;
                default: _candidate3 = candidate; break;
            }
        }
    }

    public struct CreatureCandidateBuffer
    {
        public const int Capacity = 4;

        private CreatureObservation _candidate0;
        private CreatureObservation _candidate1;
        private CreatureObservation _candidate2;
        private CreatureObservation _candidate3;
        private int _count;

        public int Count => _count;

        public CreatureObservation GetAt(int index)
        {
            switch (index)
            {
                case 0: return _candidate0;
                case 1: return _candidate1;
                case 2: return _candidate2;
                default: return _candidate3;
            }
        }

        public void Consider(CreatureObservation candidate)
        {
            int insertionIndex = _count;
            for (int index = 0; index < _count; index++)
            {
                if (IsBefore(candidate, GetAt(index)))
                {
                    insertionIndex = index;
                    break;
                }
            }

            if (insertionIndex >= Capacity)
            {
                return;
            }

            int lastIndex = _count < Capacity ? _count : Capacity - 1;
            for (int index = lastIndex; index > insertionIndex; index--)
            {
                SetAt(index, GetAt(index - 1));
            }

            SetAt(insertionIndex, candidate);
            if (_count < Capacity)
            {
                _count++;
            }
        }

        private static bool IsBefore(CreatureObservation left, CreatureObservation right)
        {
            return left.Distance < right.Distance
                || (Math.Abs(left.Distance - right.Distance) <= 0.00001f && left.CreatureId.Value < right.CreatureId.Value);
        }

        private void SetAt(int index, CreatureObservation candidate)
        {
            switch (index)
            {
                case 0: _candidate0 = candidate; break;
                case 1: _candidate1 = candidate; break;
                case 2: _candidate2 = candidate; break;
                default: _candidate3 = candidate; break;
            }
        }
    }

    public static class PerceptionSystem
    {
        public static void FindAvailableResources(
            ResourceStore resources,
            UniformGrid resourceGrid,
            SimVector2 origin,
            float visionRange,
            ResourceKind kind,
            ref ResourceCandidateBuffer candidates)
        {
            int minimumColumn = resourceGrid.GetColumn(origin.X - visionRange);
            int maximumColumn = resourceGrid.GetColumn(origin.X + visionRange);
            int minimumRow = resourceGrid.GetRow(origin.Y - visionRange);
            int maximumRow = resourceGrid.GetRow(origin.Y + visionRange);

            for (int row = minimumRow; row <= maximumRow; row++)
            {
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    int cellIndex = resourceGrid.GetCellIndex(column, row);
                    for (int occupant = resourceGrid.GetCellStart(cellIndex); occupant < resourceGrid.GetCellEnd(cellIndex); occupant++)
                    {
                        int resourceIndex = resourceGrid.GetOccupantIndexAt(occupant);
                        ResourceState candidate = resources.GetAt(resourceIndex);
                        if (!candidate.IsActive || candidate.Amount <= 0f || candidate.Kind != kind)
                        {
                            continue;
                        }

                        float distance = SimVector2.Distance(origin, candidate.Position);
                        if (distance <= visionRange)
                        {
                            candidates.Consider(new ResourceObservation(candidate.Id, resourceIndex, distance, candidate.Amount));
                        }
                    }
                }
            }
        }

        public static CreatureObservation FindNearestOtherCreature(
            CreatureStore creatures,
            UniformGrid creatureGrid,
            SimVector2 origin,
            float visionRange,
            CreatureId excludedCreatureId)
        {
            if (creatures == null)
            {
                throw new ArgumentNullException(nameof(creatures));
            }

            if (creatureGrid == null)
            {
                throw new ArgumentNullException(nameof(creatureGrid));
            }

            if (visionRange < 0f || float.IsNaN(visionRange) || float.IsInfinity(visionRange))
            {
                throw new ArgumentOutOfRangeException(nameof(visionRange));
            }

            int minimumColumn = creatureGrid.GetColumn(origin.X - visionRange);
            int maximumColumn = creatureGrid.GetColumn(origin.X + visionRange);
            int minimumRow = creatureGrid.GetRow(origin.Y - visionRange);
            int maximumRow = creatureGrid.GetRow(origin.Y + visionRange);
            float bestDistance = float.MaxValue;
            CreatureObservation best = default;

            for (int row = minimumRow; row <= maximumRow; row++)
            {
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    int cellIndex = creatureGrid.GetCellIndex(column, row);
                    for (int occupant = creatureGrid.GetCellStart(cellIndex); occupant < creatureGrid.GetCellEnd(cellIndex); occupant++)
                    {
                        int creatureIndex = creatureGrid.GetOccupantIndexAt(occupant);
                        CreatureId candidateId = creatures.GetIdAt(creatureIndex);
                        if (candidateId.Equals(excludedCreatureId))
                        {
                            continue;
                        }

                        float distance = SimVector2.Distance(origin, creatures.GetMovementAt(creatureIndex).Position);
                        if (distance > visionRange)
                        {
                            continue;
                        }

                        if (!best.IsValid
                            || distance < bestDistance
                            || (Math.Abs(distance - bestDistance) <= 0.00001f && candidateId.Value < best.CreatureId.Value))
                        {
                            best = new CreatureObservation(candidateId, creatureIndex, distance);
                            bestDistance = distance;
                        }
                    }
                }
            }

            return best;
        }

        public static void FindOtherCreatures(
            CreatureStore creatures,
            UniformGrid creatureGrid,
            SimVector2 origin,
            float visionRange,
            CreatureId excludedCreatureId,
            ref CreatureCandidateBuffer candidates)
        {
            if (creatures == null)
            {
                throw new ArgumentNullException(nameof(creatures));
            }

            if (creatureGrid == null)
            {
                throw new ArgumentNullException(nameof(creatureGrid));
            }

            if (visionRange < 0f || float.IsNaN(visionRange) || float.IsInfinity(visionRange))
            {
                throw new ArgumentOutOfRangeException(nameof(visionRange));
            }

            int minimumColumn = creatureGrid.GetColumn(origin.X - visionRange);
            int maximumColumn = creatureGrid.GetColumn(origin.X + visionRange);
            int minimumRow = creatureGrid.GetRow(origin.Y - visionRange);
            int maximumRow = creatureGrid.GetRow(origin.Y + visionRange);

            for (int row = minimumRow; row <= maximumRow; row++)
            {
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    int cellIndex = creatureGrid.GetCellIndex(column, row);
                    for (int occupant = creatureGrid.GetCellStart(cellIndex); occupant < creatureGrid.GetCellEnd(cellIndex); occupant++)
                    {
                        int creatureIndex = creatureGrid.GetOccupantIndexAt(occupant);
                        CreatureId candidateId = creatures.GetIdAt(creatureIndex);
                        if (candidateId.Equals(excludedCreatureId))
                        {
                            continue;
                        }

                        float distance = SimVector2.Distance(origin, creatures.GetMovementAt(creatureIndex).Position);
                        if (distance > visionRange)
                        {
                            continue;
                        }

                        candidates.Consider(new CreatureObservation(candidateId, creatureIndex, distance));
                    }
                }
            }
        }

        public static ResourceObservation FindNearestAvailableResource(
            ResourceStore resources,
            UniformGrid resourceGrid,
            SimVector2 origin,
            float visionRange,
            ResourceKind kind)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (resourceGrid == null)
            {
                throw new ArgumentNullException(nameof(resourceGrid));
            }

            if (visionRange < 0f || float.IsNaN(visionRange) || float.IsInfinity(visionRange))
            {
                throw new ArgumentOutOfRangeException(nameof(visionRange));
            }

            var candidates = new ResourceCandidateBuffer();
            FindAvailableResources(resources, resourceGrid, origin, visionRange, kind, ref candidates);
            return candidates.Count > 0 ? candidates.GetAt(0) : default;
        }
    }
}

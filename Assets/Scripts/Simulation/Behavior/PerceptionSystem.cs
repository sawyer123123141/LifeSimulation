using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;

namespace LifeSimulation.Simulation.Behavior
{
    public readonly struct ResourceObservation
    {
        public ResourceObservation(ResourceId resourceId, int resourceIndex, float distance)
        {
            ResourceId = resourceId;
            ResourceIndex = resourceIndex;
            Distance = distance;
            IsValid = true;
        }

        public ResourceId ResourceId { get; }
        public int ResourceIndex { get; }
        public float Distance { get; }
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

    public static class PerceptionSystem
    {
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

            int minimumColumn = resourceGrid.GetColumn(origin.X - visionRange);
            int maximumColumn = resourceGrid.GetColumn(origin.X + visionRange);
            int minimumRow = resourceGrid.GetRow(origin.Y - visionRange);
            int maximumRow = resourceGrid.GetRow(origin.Y + visionRange);
            float bestDistance = float.MaxValue;
            ResourceObservation best = default;

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
                        if (distance > visionRange)
                        {
                            continue;
                        }

                        if (!best.IsValid
                            || distance < bestDistance
                            || (Math.Abs(distance - bestDistance) <= 0.00001f && candidate.Id.Value < best.ResourceId.Value))
                        {
                            best = new ResourceObservation(candidate.Id, resourceIndex, distance);
                            bestDistance = distance;
                        }
                    }
                }
            }

            return best;
        }
    }
}

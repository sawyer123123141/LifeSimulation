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

    public static class PerceptionSystem
    {
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

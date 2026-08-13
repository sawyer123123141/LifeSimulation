using System;

namespace LifeSimulation.Simulation.Resources
{
    public readonly struct ResourceRequest
    {
        public ResourceRequest(int resourceIndex, int creatureIndex, float requestedAmount)
        {
            if (resourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));
            }

            if (creatureIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(creatureIndex));
            }

            if (requestedAmount < 0f || float.IsNaN(requestedAmount) || float.IsInfinity(requestedAmount))
            {
                throw new ArgumentOutOfRangeException(nameof(requestedAmount));
            }

            ResourceIndex = resourceIndex;
            CreatureIndex = creatureIndex;
            RequestedAmount = requestedAmount;
        }

        public int ResourceIndex { get; }
        public int CreatureIndex { get; }
        public float RequestedAmount { get; }
    }

    public static class ResourceAllocationSystem
    {
        public static void Resolve(
            ResourceStore resources,
            ResourceRequest[] requests,
            int requestCount,
            float[] allocations)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            if (allocations == null)
            {
                throw new ArgumentNullException(nameof(allocations));
            }

            if (requestCount < 0 || requestCount > requests.Length || allocations.Length < requestCount)
            {
                throw new ArgumentOutOfRangeException(nameof(requestCount));
            }

            for (int index = 0; index < requestCount; index++)
            {
                allocations[index] = 0f;
            }

            for (int requestIndex = 0; requestIndex < requestCount; requestIndex++)
            {
                if (HasEarlierRequestForResource(requests, requestIndex))
                {
                    continue;
                }

                int resourceIndex = requests[requestIndex].ResourceIndex;
                float totalRequested = SumRequestsForResource(requests, requestCount, resourceIndex);
                ResourceState resource = resources.GetAt(resourceIndex);
                float availableAmount = resource.IsActive ? resource.Amount : 0f;
                float scale = totalRequested <= 0f || totalRequested <= availableAmount
                    ? 1f
                    : availableAmount / totalRequested;

                for (int allocationIndex = requestIndex; allocationIndex < requestCount; allocationIndex++)
                {
                    if (requests[allocationIndex].ResourceIndex == resourceIndex)
                    {
                        allocations[allocationIndex] = requests[allocationIndex].RequestedAmount * scale;
                    }
                }

                resources.ConsumeAt(resourceIndex, Math.Min(totalRequested, availableAmount));
            }
        }

        private static bool HasEarlierRequestForResource(ResourceRequest[] requests, int requestIndex)
        {
            int resourceIndex = requests[requestIndex].ResourceIndex;
            for (int earlierIndex = 0; earlierIndex < requestIndex; earlierIndex++)
            {
                if (requests[earlierIndex].ResourceIndex == resourceIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static float SumRequestsForResource(ResourceRequest[] requests, int requestCount, int resourceIndex)
        {
            float total = 0f;
            for (int requestIndex = 0; requestIndex < requestCount; requestIndex++)
            {
                if (requests[requestIndex].ResourceIndex == resourceIndex)
                {
                    total += requests[requestIndex].RequestedAmount;
                }
            }

            return total;
        }
    }
}

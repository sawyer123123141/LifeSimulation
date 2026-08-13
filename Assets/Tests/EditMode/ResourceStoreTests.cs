using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ResourceStoreTests
    {
        [Test]
        public void ResourceAmountsAreFiniteAndRegenerateOnlyWhileActive()
        {
            var resources = new ResourceStore(initialCapacity: 1);
            ResourceId food = resources.Add(
                ResourceKind.Food,
                new SimVector2(2f, 3f),
                interactionRadius: 1.5f,
                initialAmount: 3f,
                capacity: 10f,
                regenerationPerSecond: 4f);

            Assert.That(resources.ConsumeAt(0, 5f), Is.EqualTo(3f));
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(0f));

            resources.Regenerate(0.5f);
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(2f));

            resources.SetActive(food, false);
            resources.Regenerate(1f);
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(2f));
            Assert.That(resources.ConsumeAt(0, 1f), Is.EqualTo(0f));

            resources.SetActive(food, true);
            resources.Regenerate(10f);
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(10f));
        }

        [Test]
        public void ContestedResourceIsAllocatedProportionallyWithoutIndexPriority()
        {
            var resources = new ResourceStore(initialCapacity: 1);
            resources.Add(
                ResourceKind.Food,
                new SimVector2(0f, 0f),
                interactionRadius: 1f,
                initialAmount: 10f,
                capacity: 10f,
                regenerationPerSecond: 0f);
            var requests = new[]
            {
                new ResourceRequest(resourceIndex: 0, creatureIndex: 5, requestedAmount: 8f),
                new ResourceRequest(resourceIndex: 0, creatureIndex: 2, requestedAmount: 12f),
            };
            var allocations = new float[requests.Length];

            ResourceAllocationSystem.Resolve(resources, requests, requests.Length, allocations);

            Assert.That(allocations[0], Is.EqualTo(4f).Within(0.0001f));
            Assert.That(allocations[1], Is.EqualTo(6f).Within(0.0001f));
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(0f));
        }
    }
}

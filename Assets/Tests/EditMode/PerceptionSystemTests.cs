using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Spatial;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PerceptionSystemTests
    {
        [Test]
        public void FindOtherCreaturesKeepsFourNearestCandidatesInAscendingDistanceOrder()
        {
            var creatures = new CreatureStore(initialCapacity: 6);
            CreatureId observer = creatures.Add(Genome.Neutral, new SimVector2(0f, 0f));
            CreatureId expectedFirst = creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(2f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(3f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(4f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(5f, 0f));
            var positions = new[]
            {
                creatures.GetMovementAt(0).Position,
                creatures.GetMovementAt(1).Position,
                creatures.GetMovementAt(2).Position,
                creatures.GetMovementAt(3).Position,
                creatures.GetMovementAt(4).Position,
                creatures.GetMovementAt(5).Position,
            };
            var grid = new UniformGrid(new ArenaBounds(-6f, 6f, -6f, 6f), 2f, initialOccupantCapacity: 6);
            grid.Rebuild(positions, positions.Length);
            var candidates = new CreatureCandidateBuffer();

            PerceptionSystem.FindOtherCreatures(creatures, grid, new SimVector2(0f, 0f), visionRange: 6f, observer, ref candidates);

            Assert.That(candidates.Count, Is.EqualTo(4));
            Assert.That(candidates.GetAt(0).CreatureId, Is.EqualTo(expectedFirst));
            Assert.That(candidates.GetAt(0).Distance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(candidates.GetAt(3).Distance, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void FindOtherCreaturesExcludesObserversOwnId()
        {
            var creatures = new CreatureStore(initialCapacity: 6);
            CreatureId observer = creatures.Add(Genome.Neutral, new SimVector2(0f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(2f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(3f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(4f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(5f, 0f));
            var positions = new[]
            {
                creatures.GetMovementAt(0).Position,
                creatures.GetMovementAt(1).Position,
                creatures.GetMovementAt(2).Position,
                creatures.GetMovementAt(3).Position,
                creatures.GetMovementAt(4).Position,
                creatures.GetMovementAt(5).Position,
            };
            var grid = new UniformGrid(new ArenaBounds(-6f, 6f, -6f, 6f), 2f, initialOccupantCapacity: 6);
            grid.Rebuild(positions, positions.Length);
            var candidates = new CreatureCandidateBuffer();

            // Scan from the observer's own position so it would be its own nearest
            // candidate (distance 0) if the exclusion check did not filter it out.
            PerceptionSystem.FindOtherCreatures(creatures, grid, new SimVector2(0f, 0f), visionRange: 6f, observer, ref candidates);

            for (int index = 0; index < candidates.Count; index++)
            {
                Assert.That(candidates.GetAt(index).CreatureId, Is.Not.EqualTo(observer));
            }
        }
    }
}

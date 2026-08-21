using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusterComparisonTests
    {
        [Test]
        public void ComparisonShowsWhenAnEvenSampleBreaksAConnectedFullPopulation()
        {
            var creatures = new CreatureStore(5);
            for (int creatureIndex = 0; creatureIndex < 5; creatureIndex++)
            {
                var traits = new float[Genome.TraitCount];
                for (int traitIndex = 0; traitIndex < traits.Length; traitIndex++) traits[traitIndex] = creatureIndex * .25f;
                creatures.Add(Genome.FromTraits(traits));
            }

            PopulationGenomeSnapshot fullPopulation = PopulationGenomeSnapshot.Capture(10, creatures);
            PopulationGenomeSnapshot sample = PopulationGenomeSnapshot.CaptureSample(10, creatures, maximumCount: 3);
            GeneticClusterComparison comparison = GeneticClusterComparison.Analyze(fullPopulation, sample, threshold: .3f);

            Assert.That(comparison.FullPopulationClusterCount, Is.EqualTo(1));
            Assert.That(comparison.SampleClusterCount, Is.EqualTo(3));
        }
    }
}

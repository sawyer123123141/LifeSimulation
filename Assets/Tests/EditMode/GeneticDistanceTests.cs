using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Environment;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticDistanceTests
    {
        [Test]
        public void AnimalGenomesAtOppositeTraitExtremesHaveUnitDistance()
        {
            Genome low = Genome.FromTraits(new float[Genome.TraitCount]);
            var highTraits = new float[Genome.TraitCount];
            for (int index = 0; index < highTraits.Length; index++) highTraits[index] = 1f;

            Assert.That(GeneticDistance.Between(low, Genome.FromTraits(highTraits)), Is.EqualTo(1f).Within(.0001f));
        }

        [Test]
        public void PlantGenomesAtOppositeTraitExtremesHaveUnitDistance()
        {
            PlantGenome low = PlantGenome.FromTraits(new float[PlantGenome.TraitCount]);
            var highTraits = new float[PlantGenome.TraitCount];
            for (int index = 0; index < highTraits.Length; index++) highTraits[index] = 1f;

            Assert.That(GeneticDistance.Between(low, PlantGenome.FromTraits(highTraits)), Is.EqualTo(1f).Within(.0001f));
        }
    }
}

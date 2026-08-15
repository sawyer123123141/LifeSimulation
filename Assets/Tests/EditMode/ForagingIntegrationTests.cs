using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ForagingIntegrationTests
    {
        [Test]
        public void ConstructingAGenomeWithoutSpecifyingPersistenceDefaultsToZero()
        {
            Genome genome = Genome.Neutral;

            Assert.That(genome.Persistence, Is.EqualTo(0f));
        }

        [Test]
        public void PersistenceAboveOneIsClampedToOne()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 1.5f);

            Assert.That(genome.Persistence, Is.EqualTo(1f));
        }

        [Test]
        public void PersistenceBelowZeroIsClampedToZero()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: -0.5f);

            Assert.That(genome.Persistence, Is.EqualTo(0f));
        }

        [Test]
        public void PhenotypeFromGenomePersistenceEqualsTheGenomeValue()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0.7f);

            Phenotype phenotype = Phenotype.FromGenome(genome);

            Assert.That(phenotype.Persistence, Is.EqualTo(0.7f));
        }

        [Test]
        public void TwoGenomesDifferingOnlyInPersistenceProduceDifferentBasalEnergyCostMultiplier()
        {
            Genome lowPersistence = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0f);
            Genome highPersistence = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 1f);

            Phenotype lowPhenotype = Phenotype.FromGenome(lowPersistence);
            Phenotype highPhenotype = Phenotype.FromGenome(highPersistence);

            Assert.That(highPhenotype.BasalEnergyCostMultiplier, Is.Not.EqualTo(lowPhenotype.BasalEnergyCostMultiplier));
        }

        [Test]
        public void WithBodySizePreservesPersistence()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0.8f);

            Genome resized = genome.WithBodySize(0.9f);

            Assert.That(resized.Persistence, Is.EqualTo(0.8f));
        }
    }
}

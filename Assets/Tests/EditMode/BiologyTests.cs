using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class BiologyTests
    {
        [Test]
        public void LargerGenomeCreatesMoreReserveCapacityAndHigherBasalCost()
        {
            Phenotype small = Phenotype.FromGenome(Genome.Neutral.WithBodySize(0f));
            Phenotype large = Phenotype.FromGenome(Genome.Neutral.WithBodySize(1f));

            Assert.That(large.EnergyCapacity, Is.GreaterThan(small.EnergyCapacity));
            Assert.That(large.BasalEnergyCostMultiplier, Is.GreaterThan(small.BasalEnergyCostMultiplier));
        }

        [Test]
        public void EveryActiveGeneProvidesABenefitAndACost()
        {
            Genome neutral = Genome.Neutral;
            Phenotype lowSpeed = Phenotype.FromGenome(new Genome(neutral.BodySize, 0f, neutral.MetabolicPace, neutral.VisionRange, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype highSpeed = Phenotype.FromGenome(new Genome(neutral.BodySize, 1f, neutral.MetabolicPace, neutral.VisionRange, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype lowMetabolism = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, 0f, neutral.VisionRange, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype highMetabolism = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, 1f, neutral.VisionRange, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype lowVision = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, 0f, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype highVision = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, 1f, neutral.WaterEfficiency, neutral.FoodEfficiency));
            Phenotype lowWater = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, neutral.VisionRange, 0f, neutral.FoodEfficiency));
            Phenotype highWater = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, neutral.VisionRange, 1f, neutral.FoodEfficiency));
            Phenotype lowFood = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, neutral.VisionRange, neutral.WaterEfficiency, 0f));
            Phenotype highFood = Phenotype.FromGenome(new Genome(neutral.BodySize, neutral.MovementSpeed, neutral.MetabolicPace, neutral.VisionRange, neutral.WaterEfficiency, 1f));

            Assert.That(highSpeed.MaximumSpeed, Is.GreaterThan(lowSpeed.MaximumSpeed));
            Assert.That(highSpeed.BasalEnergyCostMultiplier, Is.GreaterThan(lowSpeed.BasalEnergyCostMultiplier));
            Assert.That(highMetabolism.DigestionRate, Is.GreaterThan(lowMetabolism.DigestionRate));
            Assert.That(highMetabolism.BasalEnergyCostMultiplier, Is.GreaterThan(lowMetabolism.BasalEnergyCostMultiplier));
            Assert.That(highVision.VisionRange, Is.GreaterThan(lowVision.VisionRange));
            Assert.That(highVision.BasalEnergyCostMultiplier, Is.GreaterThan(lowVision.BasalEnergyCostMultiplier));
            Assert.That(highWater.WaterLossMultiplier, Is.LessThan(lowWater.WaterLossMultiplier));
            Assert.That(highWater.BasalEnergyCostMultiplier, Is.GreaterThan(lowWater.BasalEnergyCostMultiplier));
            Assert.That(highFood.FoodYield, Is.GreaterThan(lowFood.FoodYield));
            Assert.That(highFood.IngestionRate, Is.LessThan(lowFood.IngestionRate));
        }

        [Test]
        public void GenomeClampsEveryGeneToTheHeritableRange()
        {
            var genome = new Genome(-1f, 2f, -2f, 3f, -4f, 5f);

            Assert.That(genome.BodySize, Is.EqualTo(0f));
            Assert.That(genome.MovementSpeed, Is.EqualTo(1f));
            Assert.That(genome.MetabolicPace, Is.EqualTo(0f));
            Assert.That(genome.VisionRange, Is.EqualTo(1f));
            Assert.That(genome.WaterEfficiency, Is.EqualTo(0f));
            Assert.That(genome.FoodEfficiency, Is.EqualTo(1f));
        }
    }
}

using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ForagingEconomicsTests
    {
        [Test]
        public void AnEmptyPatchIsWorthNothing()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float gain = ForagingEconomics.ExpectedGain(
                remainingAmount: 0f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 2f);

            Assert.That(gain, Is.EqualTo(0f));
        }

        [Test]
        public void GainIsLimitedByIngestionRateWhenThePatchHoldsFarMoreThanCanBeEaten()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            float handlingSeconds = 2f;
            float amountEatableInHandlingTime = phenotype.IngestionRate * handlingSeconds;

            float gain = ForagingEconomics.ExpectedGain(
                remainingAmount: amountEatableInHandlingTime * 1000f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: handlingSeconds);

            float expectedGain = amountEatableInHandlingTime * phenotype.PlantFoodYieldMultiplier * 1f;
            Assert.That(gain, Is.EqualTo(expectedGain).Within(0.0001f));
        }

        [Test]
        public void GainIsLimitedByRemainingAmountWhenThePatchHoldsLessThanCanBeEaten()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            float handlingSeconds = 2f;
            float amountEatableInHandlingTime = phenotype.IngestionRate * handlingSeconds;
            float remainingAmount = amountEatableInHandlingTime * 0.1f;

            float gain = ForagingEconomics.ExpectedGain(
                remainingAmount,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: handlingSeconds);

            float expectedGain = remainingAmount * phenotype.PlantFoodYieldMultiplier * 1f;
            Assert.That(gain, Is.EqualTo(expectedGain).Within(0.0001f));
        }

        [Test]
        public void HigherPlantFoodYieldMultiplierProducesStrictlyHigherGain()
        {
            Phenotype lowYield = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            Phenotype highYield = Phenotype.FromGenome(Genome.Neutral);

            float lowGain = ForagingEconomics.ExpectedGain(
                remainingAmount: 10f,
                lowYield,
                nutritionMultiplier: 1f,
                handlingSeconds: 2f);
            float highGain = ForagingEconomics.ExpectedGain(
                remainingAmount: 10f,
                highYield,
                nutritionMultiplier: 1f,
                handlingSeconds: 2f);

            Assert.That(highYield.PlantFoodYieldMultiplier, Is.GreaterThan(lowYield.PlantFoodYieldMultiplier));
            Assert.That(highGain, Is.GreaterThan(lowGain));
        }

        [Test]
        public void HigherNutritionMultiplierProducesStrictlyHigherGain()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float lowGain = ForagingEconomics.ExpectedGain(
                remainingAmount: 10f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 2f);
            float highGain = ForagingEconomics.ExpectedGain(
                remainingAmount: 10f,
                phenotype,
                nutritionMultiplier: 2f,
                handlingSeconds: 2f);

            Assert.That(highGain, Is.GreaterThan(lowGain));
        }

        [Test]
        public void NegativeRemainingAmountThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: -1f,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: 2f));
        }

        [Test]
        public void NonFiniteRemainingAmountThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: float.NaN,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: 2f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: float.PositiveInfinity,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: 2f));
        }

        [Test]
        public void NegativeHandlingSecondsThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: 10f,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: -1f));
        }

        [Test]
        public void NonFiniteHandlingSecondsThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: 10f,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: float.NaN));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.ExpectedGain(
                    remainingAmount: 10f,
                    phenotype,
                    nutritionMultiplier: 1f,
                    handlingSeconds: float.PositiveInfinity));
        }

        [Test]
        public void ZeroDistanceCostsNoEnergy()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float energy = ForagingEconomics.TravelEnergy(distance: 0f, phenotype);

            Assert.That(energy, Is.EqualTo(0f));
        }

        [Test]
        public void DoubleTheDistanceCostsExactlyDoubleTheEnergy()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float energy = ForagingEconomics.TravelEnergy(distance: 3f, phenotype);
            float doubledEnergy = ForagingEconomics.TravelEnergy(distance: 6f, phenotype);

            Assert.That(doubledEnergy, Is.EqualTo(energy * 2f).Within(0.0001f));
        }

        [Test]
        public void HeavierCreatureCostsStrictlyMoreEnergyForTheSameDistance()
        {
            Phenotype lightPhenotype = Phenotype.FromGenome(new Genome(0.2f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            Phenotype heavyPhenotype = Phenotype.FromGenome(new Genome(0.9f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));

            Assert.That(heavyPhenotype.BodyMass, Is.GreaterThan(lightPhenotype.BodyMass));

            float lightEnergy = ForagingEconomics.TravelEnergy(distance: 5f, lightPhenotype);
            float heavyEnergy = ForagingEconomics.TravelEnergy(distance: 5f, heavyPhenotype);

            Assert.That(heavyEnergy, Is.GreaterThan(lightEnergy));
        }

        [Test]
        public void ResultMatchesWhatNeedsSystemTickDeductsForTheSameMovement()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            float distance = 4f;

            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            float energyBefore = needs.Energy;
            NeedsSystem.Tick(ref needs, phenotype, deltaTime: 0f, movementDistance: distance);
            float energyDeductedByTick = energyBefore - needs.Energy;

            float travelEnergy = ForagingEconomics.TravelEnergy(distance, phenotype);

            Assert.That(travelEnergy, Is.EqualTo(energyDeductedByTick).Within(0.0001f));
        }

        [Test]
        public void NegativeDistanceThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.TravelEnergy(distance: -1f, phenotype));
        }

        [Test]
        public void NonFiniteDistanceThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.TravelEnergy(distance: float.NaN, phenotype));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.TravelEnergy(distance: float.PositiveInfinity, phenotype));
        }
    }
}

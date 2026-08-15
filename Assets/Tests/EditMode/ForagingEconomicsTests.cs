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

        [Test]
        public void RichPatchFarAwayScoresHigherThanDepletedPatchNearby()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float richFarScore = ForagingEconomics.PatchScore(
                urgency: 1f,
                remainingAmount: 100f,
                distance: 3f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 5f,
                referenceGain: 10f);

            float depletedNearScore = ForagingEconomics.PatchScore(
                urgency: 1f,
                remainingAmount: 0.5f,
                distance: 0.5f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 5f,
                referenceGain: 10f);

            Assert.That(richFarScore, Is.GreaterThan(depletedNearScore));
        }

        [Test]
        public void TravelEnergyExceedingExpectedGainProducesExactlyZeroScore()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float score = ForagingEconomics.PatchScore(
                urgency: 1f,
                remainingAmount: 1f,
                distance: 10f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 1f,
                referenceGain: 10f);

            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void ZeroUrgencyProducesZeroScoreRegardlessOfPatchQuality()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float score = ForagingEconomics.PatchScore(
                urgency: 0f,
                remainingAmount: 100f,
                distance: 1f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 5f,
                referenceGain: 10f);

            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void NearerOfTwoIdenticalPatchesScoresHigher()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float nearScore = ForagingEconomics.PatchScore(
                urgency: 1f,
                remainingAmount: 20f,
                distance: 1f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 5f,
                referenceGain: 10f);

            float farScore = ForagingEconomics.PatchScore(
                urgency: 1f,
                remainingAmount: 20f,
                distance: 4f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 5f,
                referenceGain: 10f);

            Assert.That(nearScore, Is.GreaterThan(farScore));
        }

        [Test]
        public void HeavierCreatureScoresFallFasterWithDistanceThanLighterCreatures()
        {
            Phenotype lightPhenotype = Phenotype.FromGenome(new Genome(0.2f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            Phenotype heavyPhenotype = Phenotype.FromGenome(new Genome(0.9f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));

            float lightNearScore = ForagingEconomics.PatchScore(
                urgency: 1f, remainingAmount: 50f, distance: 1f, lightPhenotype,
                nutritionMultiplier: 1f, handlingSeconds: 5f, referenceGain: 50f);
            float lightFarScore = ForagingEconomics.PatchScore(
                urgency: 1f, remainingAmount: 50f, distance: 5f, lightPhenotype,
                nutritionMultiplier: 1f, handlingSeconds: 5f, referenceGain: 50f);

            float heavyNearScore = ForagingEconomics.PatchScore(
                urgency: 1f, remainingAmount: 50f, distance: 1f, heavyPhenotype,
                nutritionMultiplier: 1f, handlingSeconds: 5f, referenceGain: 50f);
            float heavyFarScore = ForagingEconomics.PatchScore(
                urgency: 1f, remainingAmount: 50f, distance: 5f, heavyPhenotype,
                nutritionMultiplier: 1f, handlingSeconds: 5f, referenceGain: 50f);

            float lightDrop = lightNearScore - lightFarScore;
            float heavyDrop = heavyNearScore - heavyFarScore;

            Assert.That(heavyDrop, Is.GreaterThan(lightDrop));
        }

        [Test]
        public void UrgencyBelowZeroThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: -0.1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void UrgencyAboveOneThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1.1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void NonFiniteUrgencyThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: float.NaN, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: float.PositiveInfinity, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void PatchScoreNonFiniteRemainingAmountThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: float.NaN, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void PatchScoreNonFiniteDistanceThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: float.PositiveInfinity, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void NonFiniteNutritionMultiplierThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: float.NaN, handlingSeconds: 2f, referenceGain: 10f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: float.PositiveInfinity, handlingSeconds: 2f, referenceGain: 10f));
        }

        [Test]
        public void PatchScoreNonFiniteHandlingSecondsThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: float.NaN, referenceGain: 10f));
        }

        [Test]
        public void ReferenceGainAtZeroThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: 0f));
        }

        [Test]
        public void ReferenceGainBelowZeroThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: -5f));
        }

        [Test]
        public void NonFiniteReferenceGainThrows()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: float.NaN));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.PatchScore(
                    urgency: 1f, remainingAmount: 10f, distance: 1f, phenotype,
                    nutritionMultiplier: 1f, handlingSeconds: 2f, referenceGain: float.PositiveInfinity));
        }

        [Test]
        public void ZeroSecondsElapsedProducesTheStartingBonus()
        {
            float bonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 0f,
                persistence: 0.6f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds: 3f);

            Assert.That(bonus, Is.EqualTo(0.4f * 0.6f).Within(0.0001f));
        }

        [Test]
        public void OneHalfLifeElapsedHalvesTheBonus()
        {
            float commitmentHalfLifeSeconds = 3f;
            float startingBonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 0f,
                persistence: 0.6f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds);

            float bonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: commitmentHalfLifeSeconds,
                persistence: 0.6f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds);

            Assert.That(bonus, Is.EqualTo(startingBonus * 0.5f).Within(0.0001f));
        }

        [Test]
        public void TwoHalfLivesElapsedQuartersTheBonus()
        {
            float commitmentHalfLifeSeconds = 3f;
            float startingBonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 0f,
                persistence: 0.6f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds);

            float bonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: commitmentHalfLifeSeconds * 2f,
                persistence: 0.6f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds);

            Assert.That(bonus, Is.EqualTo(startingBonus * 0.25f).Within(0.0001f));
        }

        [Test]
        public void ZeroPersistenceProducesZeroBonusAtAnyElapsedTime()
        {
            float bonusAtStart = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 0f,
                persistence: 0f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds: 3f);

            float bonusLater = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 10f,
                persistence: 0f,
                commitmentStrength: 0.4f,
                commitmentHalfLifeSeconds: 3f);

            Assert.That(bonusAtStart, Is.EqualTo(0f));
            Assert.That(bonusLater, Is.EqualTo(0f));
        }

        [Test]
        public void IncreasingElapsedTimeStrictlyDecreasesTheBonusAndNeverGoesNegative()
        {
            float commitmentHalfLifeSeconds = 3f;
            float persistence = 0.6f;
            float commitmentStrength = 0.4f;

            float earlierBonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 1f, persistence, commitmentStrength, commitmentHalfLifeSeconds);
            float laterBonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 2f, persistence, commitmentStrength, commitmentHalfLifeSeconds);
            float muchLaterBonus = ForagingEconomics.CommitmentBonus(
                secondsInCurrentAction: 100f, persistence, commitmentStrength, commitmentHalfLifeSeconds);

            Assert.That(laterBonus, Is.LessThan(earlierBonus));
            Assert.That(muchLaterBonus, Is.LessThan(laterBonus));
            Assert.That(muchLaterBonus, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void NegativeSecondsInCurrentActionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: -1f, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void NonFiniteSecondsInCurrentActionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: float.NaN, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: float.PositiveInfinity, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void NegativePersistenceThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: -0.1f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void NonFinitePersistenceThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: float.NaN, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: float.PositiveInfinity, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void NegativeCommitmentStrengthThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: -0.1f, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void NonFiniteCommitmentStrengthThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: float.NaN, commitmentHalfLifeSeconds: 3f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: float.PositiveInfinity, commitmentHalfLifeSeconds: 3f));
        }

        [Test]
        public void CommitmentHalfLifeSecondsAtZeroThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: 0f));
        }

        [Test]
        public void CommitmentHalfLifeSecondsBelowZeroThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: -3f));
        }

        [Test]
        public void NonFiniteCommitmentHalfLifeSecondsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: float.NaN));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ForagingEconomics.CommitmentBonus(
                    secondsInCurrentAction: 1f, persistence: 0.6f, commitmentStrength: 0.4f, commitmentHalfLifeSeconds: float.PositiveInfinity));
        }
    }
}

using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class BiologyTests
    {
        [Test]
        public void TemperatureToleranceReducesExtremeTemperatureDamageAtAMaintenanceCost()
        {
            Phenotype lowTolerance = Phenotype.FromGenome(Genome.Neutral);
            Phenotype highTolerance = Phenotype.FromGenome(new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, temperatureTolerance: 1f));
            CreatureNeeds lowNeeds = CreatureNeeds.Full(lowTolerance);
            CreatureNeeds highNeeds = CreatureNeeds.Full(highTolerance);

            NeedsSystem.ApplyTemperatureStress(ref lowNeeds, lowTolerance, temperature: 30f, deltaTime: 1f);
            NeedsSystem.ApplyTemperatureStress(ref highNeeds, highTolerance, temperature: 30f, deltaTime: 1f);

            Assert.That(highNeeds.Health, Is.GreaterThan(lowNeeds.Health));
            Assert.That(highTolerance.BasalEnergyCostMultiplier, Is.GreaterThan(lowTolerance.BasalEnergyCostMultiplier));
        }

        [Test]
        public void TemperatureFieldIsRepeatableAndVariesAcrossTheArena()
        {
            float first = TemperatureField.Sample(new SimVector2(-10f, 0f), 100);
            float repeated = TemperatureField.Sample(new SimVector2(-10f, 0f), 100);
            float distant = TemperatureField.Sample(new SimVector2(10f, 0f), 100);

            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(distant, Is.Not.EqualTo(first));
        }

        [Test]
        public void FertilityAndLifespanGenesCreateOpposingReproductiveAndMaintenanceTradeoffs()
        {
            Phenotype low = Phenotype.FromGenome(Genome.Neutral);
            Phenotype high = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, fertilityInvestment: 1f, lifespanTendency: 1f));

            Assert.That(high.ReproductionCooldownSeconds, Is.LessThan(low.ReproductionCooldownSeconds));
            Assert.That(high.ReproductionEnergyCostFraction, Is.GreaterThan(low.ReproductionEnergyCostFraction));
            Assert.That(high.MaximumAgeSeconds, Is.GreaterThan(low.MaximumAgeSeconds));
            Assert.That(high.BasalEnergyCostMultiplier, Is.GreaterThan(low.BasalEnergyCostMultiplier));
        }

        [Test]
        public void CognitionGenesTradeLowerMemoryDecayForHigherMaintenanceAndRestCost()
        {
            Phenotype lowCognition = Phenotype.FromGenome(Genome.Neutral);
            Phenotype highCognition = Phenotype.FromGenome(new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                memoryCapacity: 1f, memoryRetention: 1f, learningRate: 1f, exploration: 1f));

            Assert.That(highCognition.MemoryConfidenceDecayPerSecond, Is.LessThan(lowCognition.MemoryConfidenceDecayPerSecond));
            Assert.That(highCognition.BasalEnergyCostMultiplier, Is.GreaterThan(lowCognition.BasalEnergyCostMultiplier));
            Assert.That(highCognition.CognitionRestCostMultiplier, Is.GreaterThan(lowCognition.CognitionRestCostMultiplier));
        }
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

        [Test]
        public void HigherMetabolicPaceConsumesMoreEnergyAndHydration()
        {
            Phenotype slow = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0f, 0.5f, 0.5f, 0.5f));
            Phenotype fast = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 1f, 0.5f, 0.5f, 0.5f));
            CreatureNeeds slowNeeds = CreatureNeeds.Full(slow);
            CreatureNeeds fastNeeds = CreatureNeeds.Full(fast);

            NeedsSystem.Tick(ref slowNeeds, slow, 1f, 0f);
            NeedsSystem.Tick(ref fastNeeds, fast, 1f, 0f);

            Assert.That(fastNeeds.Energy, Is.LessThan(slowNeeds.Energy));
            Assert.That(fastNeeds.Hydration, Is.LessThan(slowNeeds.Hydration));
        }

        [Test]
        public void TwoParentCrossoverIsDeterministicAndMutationRemainsBounded()
        {
            var firstParent = new Genome(0f, 0f, 0f, 0f, 0f, 0f);
            var secondParent = new Genome(1f, 1f, 1f, 1f, 1f, 1f);

            Genome firstChild = GenomeInheritance.CreateChild(firstParent, secondParent, 42, 7, 0.2f);
            Genome repeatedChild = GenomeInheritance.CreateChild(firstParent, secondParent, 42, 7, 0.2f);

            Assert.That(repeatedChild.BodySize, Is.EqualTo(firstChild.BodySize));
            Assert.That(repeatedChild.MovementSpeed, Is.EqualTo(firstChild.MovementSpeed));
            Assert.That(firstChild.BodySize, Is.InRange(0f, 1f));
            Assert.That(firstChild.MovementSpeed, Is.InRange(0f, 1f));
            Assert.That(firstChild.MetabolicPace, Is.InRange(0f, 1f));
            Assert.That(firstChild.VisionRange, Is.InRange(0f, 1f));
            Assert.That(firstChild.WaterEfficiency, Is.InRange(0f, 1f));
            Assert.That(firstChild.FoodEfficiency, Is.InRange(0f, 1f));
        }

        [Test]
        public void EveryGeneTransmitsFromParentsToOffspring()
        {
            // Both parents share a distinctive non-default value in every gene. Any gene that
            // CreateChild forgets to pass will fall back to its constructor default instead,
            // which this test detects. Mutation sigma is 0 so inheritance is exact.
            var parent = new Genome(
                .81f, .82f, .83f, .84f, .85f, .86f, .87f, .88f, .89f, .90f, .91f, .92f,
                .93f, .94f, .95f, .96f, .97f, .98f, .99f, .80f, .79f, .78f, .77f,
                persistence: .76f);

            Genome child = GenomeInheritance.CreateChild(parent, parent, worldSeed: 42, birthOrdinal: 0, mutationStandardDeviation: 0f);

            Assert.That(child.BodySize, Is.EqualTo(.81f).Within(.0001f));
            Assert.That(child.MovementSpeed, Is.EqualTo(.82f).Within(.0001f));
            Assert.That(child.MetabolicPace, Is.EqualTo(.83f).Within(.0001f));
            Assert.That(child.VisionRange, Is.EqualTo(.84f).Within(.0001f));
            Assert.That(child.WaterEfficiency, Is.EqualTo(.85f).Within(.0001f));
            Assert.That(child.FoodEfficiency, Is.EqualTo(.86f).Within(.0001f));
            Assert.That(child.Attack, Is.EqualTo(.87f).Within(.0001f));
            Assert.That(child.Defense, Is.EqualTo(.88f).Within(.0001f));
            Assert.That(child.Maneuverability, Is.EqualTo(.89f).Within(.0001f));
            Assert.That(child.Fear, Is.EqualTo(.90f).Within(.0001f));
            Assert.That(child.Aggression, Is.EqualTo(.91f).Within(.0001f));
            Assert.That(child.DietSpecialization, Is.EqualTo(.92f).Within(.0001f));
            Assert.That(child.MemoryCapacity, Is.EqualTo(.93f).Within(.0001f));
            Assert.That(child.MemoryRetention, Is.EqualTo(.94f).Within(.0001f));
            Assert.That(child.LearningRate, Is.EqualTo(.95f).Within(.0001f));
            Assert.That(child.Exploration, Is.EqualTo(.96f).Within(.0001f));
            Assert.That(child.TemperatureTolerance, Is.EqualTo(.97f).Within(.0001f));
            Assert.That(child.FertilityInvestment, Is.EqualTo(.98f).Within(.0001f));
            Assert.That(child.LifespanTendency, Is.EqualTo(.99f).Within(.0001f));
            Assert.That(child.UrgencyExponent, Is.EqualTo(.80f).Within(.0001f));
            Assert.That(child.TravelSensitivity, Is.EqualTo(.79f).Within(.0001f));
            Assert.That(child.RiskAversion, Is.EqualTo(.78f).Within(.0001f));
            Assert.That(child.NeutralMarker, Is.EqualTo(.77f).Within(.0001f));
            Assert.That(child.Persistence, Is.EqualTo(.76f).Within(.0001f));
        }

        [Test]
        public void PersistenceMutatesAcrossOffspringRatherThanStayingConstant()
        {
            var parent = new Genome(.5f, .5f, .5f, .5f, .5f, .5f, persistence: .5f);

            float first = GenomeInheritance.CreateChild(parent, parent, 42, 0, .03f).Persistence;
            float second = GenomeInheritance.CreateChild(parent, parent, 42, 1, .03f).Persistence;

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first, Is.Not.EqualTo(0f));
        }

        [Test]
        public void FoodAndWaterRestoreOnlyTheirMatchingNeedWithoutExceedingCapacity()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            needs.Hydration = 0f;

            NeedsSystem.ConsumeFood(ref needs, phenotype, 1000f);
            NeedsSystem.DrinkWater(ref needs, phenotype, 1000f);

            Assert.That(needs.Energy, Is.EqualTo(phenotype.EnergyCapacity));
            Assert.That(needs.Hydration, Is.EqualTo(phenotype.HydrationCapacity));
        }

        [Test]
        public void FoodEfficiencyRaisesNetEnergyRecoveredAtItsOwnIngestionRate()
        {
            Phenotype lowFood = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f));
            Phenotype highFood = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f));
            CreatureNeeds lowNeeds = CreatureNeeds.Full(lowFood);
            CreatureNeeds highNeeds = CreatureNeeds.Full(highFood);
            lowNeeds.Energy = 0f;
            highNeeds.Energy = 0f;

            NeedsSystem.ConsumeFood(ref lowNeeds, lowFood, lowFood.IngestionRate);
            NeedsSystem.ConsumeFood(ref highNeeds, highFood, highFood.IngestionRate);

            Assert.That(highNeeds.Energy, Is.GreaterThan(lowNeeds.Energy));
            Assert.That(highFood.BasalEnergyCostMultiplier, Is.GreaterThan(lowFood.BasalEnergyCostMultiplier));
        }

        [Test]
        public void PredationTraitsRemainDormantInP0AndAreHeritableForP1()
        {
            Genome neutral = Genome.Neutral;
            var firstParent = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 0f, defense: 0f, maneuverability: 0f, fear: 0f, aggression: 0f, dietSpecialization: 0f);
            var secondParent = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, defense: 1f, maneuverability: 1f, fear: 1f, aggression: 1f, dietSpecialization: 1f);
            Genome child = GenomeInheritance.CreateChild(firstParent, secondParent, 42, 7, mutationStandardDeviation: 0f);

            Assert.That(neutral.Attack, Is.EqualTo(0f));
            Assert.That(neutral.DietSpecialization, Is.EqualTo(0f));
            Assert.That(child.Attack, Is.InRange(0f, 1f));
            Assert.That(child.Defense, Is.InRange(0f, 1f));
            Assert.That(child.Maneuverability, Is.InRange(0f, 1f));
            Assert.That(child.Fear, Is.InRange(0f, 1f));
            Assert.That(child.Aggression, Is.InRange(0f, 1f));
            Assert.That(child.DietSpecialization, Is.InRange(0f, 1f));
        }

        [Test]
        public void PredationGenesCacheTheirBenefitsAlongsideMaintenanceCosts()
        {
            Genome lowGenome = Genome.Neutral;
            Genome highGenome = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                attack: 1f, defense: 1f, maneuverability: 1f,
                fear: 1f, aggression: 1f, dietSpecialization: 1f);
            Phenotype low = Phenotype.FromGenome(lowGenome);
            Phenotype high = Phenotype.FromGenome(highGenome);

            Assert.That(high.AttackPower, Is.GreaterThan(low.AttackPower));
            Assert.That(high.Defense, Is.GreaterThan(low.Defense));
            Assert.That(high.Maneuverability, Is.GreaterThan(low.Maneuverability));
            Assert.That(high.FearResponse, Is.GreaterThan(low.FearResponse));
            Assert.That(high.Aggression, Is.GreaterThan(low.Aggression));
            Assert.That(high.MeatYieldMultiplier, Is.GreaterThan(low.MeatYieldMultiplier));
            Assert.That(high.PlantFoodYieldMultiplier, Is.LessThan(low.PlantFoodYieldMultiplier));
            Assert.That(high.BasalEnergyCostMultiplier, Is.GreaterThan(low.BasalEnergyCostMultiplier));
        }

        [Test]
        public void PredationFounderFactorySeedsRepeatableUnlabeledVariation()
        {
            Genome first = PredationFounderFactory.Create(worldSeed: 42, founderOrdinal: 0);
            Genome repeated = PredationFounderFactory.Create(worldSeed: 42, founderOrdinal: 0);
            Genome second = PredationFounderFactory.Create(worldSeed: 42, founderOrdinal: 1);

            Assert.That(repeated.Attack, Is.EqualTo(first.Attack));
            Assert.That(repeated.DietSpecialization, Is.EqualTo(first.DietSpecialization));
            Assert.That(second.Attack, Is.Not.EqualTo(first.Attack));
            Assert.That(first.Attack, Is.InRange(0f, 1f));
            Assert.That(first.Defense, Is.InRange(0f, 1f));
            Assert.That(first.Maneuverability, Is.InRange(0f, 1f));
            Assert.That(first.Fear, Is.InRange(0f, 1f));
            Assert.That(first.Aggression, Is.InRange(0f, 1f));
            Assert.That(first.DietSpecialization, Is.InRange(0f, 1f));
            Assert.That(first.TemperatureTolerance, Is.InRange(0f, 1f));
            Assert.That(first.FertilityInvestment, Is.InRange(0f, 1f));
            Assert.That(first.LifespanTendency, Is.InRange(0f, 1f));
        }

        [Test]
        public void RestingRecoversTheRestNeedInsteadOfDrainingIt()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Rest = 50f;

            NeedsSystem.Tick(ref needs, phenotype, deltaTime: 1f, movementDistance: 0f, restBehaviorEnabled: true, isResting: true);

            Assert.That(needs.Rest, Is.EqualTo(55f).Within(0.0001f));
        }

        [Test]
        public void RestNeedAtZeroDamagesHealthWhenRestBehaviorIsEnabled()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Rest = 0f;
            float healthBefore = needs.Health;

            NeedsSystem.Tick(ref needs, phenotype, deltaTime: 1f, movementDistance: 0f, restBehaviorEnabled: true, isResting: false);

            Assert.That(needs.Health, Is.EqualTo(healthBefore - 3f).Within(0.0001f));
        }

        [Test]
        public void JuvenileCapabilityMultiplierRampsLinearlyFromFloorToFull()
        {
            Assert.That(JuvenileSystem.CapabilityMultiplier(age: 0f, adultAgeSeconds: 20f), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(JuvenileSystem.CapabilityMultiplier(age: 20f, adultAgeSeconds: 20f), Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(JuvenileSystem.CapabilityMultiplier(age: 40f, adultAgeSeconds: 20f), Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(JuvenileSystem.CapabilityMultiplier(age: 10f, adultAgeSeconds: 20f), Is.EqualTo(0.65f).Within(0.0001f));
        }

        [Test]
        public void WithJuvenileScalingScalesOnlySpeedVisionAndCombatFields()
        {
            Phenotype adult = Phenotype.FromGenome(new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                attack: 0.6f, defense: 0.4f, maneuverability: 0.5f));

            Phenotype scaled = adult.WithJuvenileScaling(0.3f);

            Assert.That(scaled.MaximumSpeed, Is.EqualTo(adult.MaximumSpeed * 0.3f).Within(0.0001f));
            Assert.That(scaled.VisionRange, Is.EqualTo(adult.VisionRange * 0.3f).Within(0.0001f));
            Assert.That(scaled.AttackPower, Is.EqualTo(adult.AttackPower * 0.3f).Within(0.0001f));
            Assert.That(scaled.Defense, Is.EqualTo(adult.Defense * 0.3f).Within(0.0001f));
            Assert.That(scaled.Maneuverability, Is.EqualTo(adult.Maneuverability * 0.3f).Within(0.0001f));
            Assert.That(scaled.EnergyCapacity, Is.EqualTo(adult.EnergyCapacity).Within(0.0001f));
            Assert.That(scaled.HealthCapacity, Is.EqualTo(adult.HealthCapacity).Within(0.0001f));
        }
    }
}

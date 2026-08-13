using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
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
        }
    }
}

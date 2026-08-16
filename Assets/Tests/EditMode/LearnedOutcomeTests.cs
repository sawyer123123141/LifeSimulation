using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Covers defect B-1: the learned food/water outcome used to saturate at 1.0 on nearly every
    /// feeding event because it clamped raw nutrition * 20 instead of measuring intake against an
    /// expectation. These tests exercise the real per-tick feeding path in <see cref="SimulationWorld"/>
    /// so a regression back to the old saturating formula fails them, not just a formula unit test.
    /// </summary>
    public sealed class LearnedOutcomeTests
    {
        private const int Seed = 42;

        [Test]
        public void FeedingAtARichPlaceScoresMaterialyHigherThanFeedingAtAPoorPlace()
        {
            float richOutcome = FeedOnceAndReadFoodOutcome(
                learningRate: 1f, initialAmount: 100f, capacity: 100f);
            float poorOutcome = FeedOnceAndReadFoodOutcome(
                learningRate: 1f, initialAmount: 0.01f, capacity: 0.01f);

            // Under the old formula (nutrition * 20, clamped) both cases saturate to 1.0 and would
            // be indistinguishable. The fix must keep them apart and away from that ceiling.
            Assert.That(richOutcome, Is.LessThan(1f));
            Assert.That(poorOutcome, Is.LessThan(1f));
            Assert.That(richOutcome, Is.GreaterThan(poorOutcome + 0.2f));
        }

        [Test]
        public void ATypicalFeedingEventProducesAnOutcomeStrictlyBetweenZeroAndOne()
        {
            float outcome = FeedOnceAndReadFoodOutcome(
                learningRate: 1f, initialAmount: 10f, capacity: 10f);

            // This is the actual bug: the old formula (nutrition * 20, clamped at 1) saturates to
            // exactly 1.0 for this same ordinary, unremarkable feeding event.
            Assert.That(outcome, Is.GreaterThan(0f));
            Assert.That(outcome, Is.LessThan(1f));
        }

        [Test]
        public void RepeatedFeedingAtOnePlaceConvergesTowardThatPlacesTrueQuality()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype2Defaults(Seed, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn(FeedingGenome(learningRate: 0.2f));
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 80f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 100f, 100f, 0f);

            for (int index = 0; index < 15; index++)
            {
                world.Step(config.FixedDeltaTime);
            }
            float afterFewFeedings = world.GetCreatureMemoryAt(0).FoodOutcomeValue;

            for (int index = 0; index < 45; index++)
            {
                world.Step(config.FixedDeltaTime);
            }
            float afterManyFeedings = world.GetCreatureMemoryAt(0).FoodOutcomeValue;

            // With an abundant, unchanging resource the per-tick outcome is constant, so a running
            // average weighted by LearningRate must climb monotonically toward it and settle close by.
            Assert.That(afterManyFeedings, Is.GreaterThan(afterFewFeedings));
            Assert.That(afterManyFeedings, Is.EqualTo(0.473f).Within(0.01f));
        }

        [Test]
        public void HigherLearningRateConvergesInFewerFeedingsThanLowerLearningRate()
        {
            float highLearningRateOutcome = FeedRepeatedlyAndReadFoodOutcome(learningRate: 0.9f, stepCount: 12);
            float lowLearningRateOutcome = FeedRepeatedlyAndReadFoodOutcome(learningRate: 0.1f, stepCount: 12);

            // Same place, same number of feedings: the faster learner should already be much closer
            // to the place's true quality (~0.473) than the slower learner.
            Assert.That(highLearningRateOutcome, Is.GreaterThan(lowLearningRateOutcome));
            Assert.That(highLearningRateOutcome, Is.EqualTo(0.473f).Within(0.01f));
            Assert.That(lowLearningRateOutcome, Is.LessThan(0.2f));
        }

        [Test]
        public void FeedingAtOnePlaceLeavesTheOtherResourceKindsOutcomeUnchanged()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype2Defaults(Seed, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn(FeedingGenome(learningRate: 1f));
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 80f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 100f, 100f, 0f);

            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            MemoryState memory = world.GetCreatureMemoryAt(0);
            Assert.That(memory.FoodOutcomeValue, Is.GreaterThan(0f));
            Assert.That(memory.WaterOutcomeValue, Is.EqualTo(0f));
            Assert.That(memory.WaterExperienceCount, Is.EqualTo(0));
        }

        private static Genome FeedingGenome(float learningRate)
        {
            return new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, learningRate: learningRate, exploration: 0f);
        }

        private static float FeedOnceAndReadFoodOutcome(float learningRate, float initialAmount, float capacity)
        {
            SimulationConfig config = SimulationConfig.CreatePrototype2Defaults(Seed, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn(FeedingGenome(learningRate));
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 80f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, initialAmount, capacity, 0f);

            // The scheduled decision frequency (2 Hz against a 20 Hz base) means the first decision,
            // and the feeding it triggers, lands on tick 10.
            for (int index = 0; index < 10; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            return world.GetCreatureMemoryAt(0).FoodOutcomeValue;
        }

        private static float FeedRepeatedlyAndReadFoodOutcome(float learningRate, int stepCount)
        {
            SimulationConfig config = SimulationConfig.CreatePrototype2Defaults(Seed, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn(FeedingGenome(learningRate));
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 80f;
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 100f, 100f, 0f);

            for (int index = 0; index < stepCount; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            return world.GetCreatureMemoryAt(0).FoodOutcomeValue;
        }
    }
}

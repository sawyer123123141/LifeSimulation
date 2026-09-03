using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ReproductionBottleneckTests
    {
        private const float NeedFraction = .7f;

        private static readonly Phenotype Body = Phenotype.FromGenome(Genome.Neutral);

        [Test]
        public void TheLowestNormalisedNeedIsTheMinimumAndTheOneBelowTheGateIsTheBlocker()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);

            bottleneck.Sample(NeedsAt(energy: .9f, hydration: .9f, health: .4f), Body, Ready());

            Assert.That(bottleneck.AdultSamples, Is.EqualTo(1));
            Assert.That(bottleneck.MinimumNeedCount(ReproductiveNeed.Health), Is.EqualTo(1));
            Assert.That(bottleneck.MinimumNeedCount(ReproductiveNeed.Energy), Is.EqualTo(0));
            Assert.That(bottleneck.BlockedSamples, Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Health), Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Energy), Is.EqualTo(0));
            Assert.That(bottleneck.BlockedMinimumNeedCount(ReproductiveNeed.Health), Is.EqualTo(1));
        }

        [Test]
        public void ACreatureAboveTheGateOnAllThreeReportsAMinimumButNoBlocker()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);

            bottleneck.Sample(NeedsAt(energy: .95f, hydration: .8f, health: .99f), Body, Ready());

            Assert.That(bottleneck.AdultSamples, Is.EqualTo(1));
            Assert.That(bottleneck.MinimumNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(1));
            Assert.That(bottleneck.BlockedSamples, Is.EqualTo(0));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(0));
            Assert.That(bottleneck.BlockedMinimumNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(0));
        }

        [Test]
        public void ACooldownIsANonNeedBlockerAndIsNeverCountedAsANeed()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);

            bottleneck.Sample(NeedsAt(energy: .95f, hydration: .95f, health: .95f), Body, OnCooldown());

            Assert.That(bottleneck.BlockedSamples, Is.EqualTo(1));
            Assert.That(bottleneck.CooldownBlockedSamples, Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Energy), Is.EqualTo(0));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(0));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Health), Is.EqualTo(0));
        }

        [Test]
        public void JuvenilesAreNotSampledBecauseAgeIsNotANeedAndWouldWeightTheAnswerByLifespan()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);
            CreatureNeeds juvenile = NeedsAt(energy: .2f, hydration: .2f, health: .2f);
            juvenile.Age = ReproductionSystem.AdultAgeSeconds - 1f;

            bool sampled = bottleneck.Sample(juvenile, Body, Ready());

            Assert.That(sampled, Is.False);
            Assert.That(bottleneck.AdultSamples, Is.EqualTo(0));
            Assert.That(bottleneck.JuvenileSamplesSkipped, Is.EqualTo(1));
        }

        [Test]
        public void MoreThanOneNeedCanBlockTheSameSampleAndEachIsCounted()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);

            bottleneck.Sample(NeedsAt(energy: .3f, hydration: .1f, health: .95f), Body, Ready());

            Assert.That(bottleneck.BlockedSamples, Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Energy), Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(1));
            Assert.That(bottleneck.BlockingNeedCount(ReproductiveNeed.Health), Is.EqualTo(0));
            Assert.That(bottleneck.BlockedMinimumNeedCount(ReproductiveNeed.Hydration), Is.EqualTo(1));
        }

        [Test]
        public void TheMateSeekingGateIsMeasuredAgainstTheProductionPredicateAndItsOwnMargin()
        {
            var bottleneck = new ReproductionBottleneck(NeedFraction);
            // Above the breeding gate on all three, below the mate-seeking gate on hydration only.
            float betweenGates = NeedFraction + (SimulationConfig.MateSeekingNeedMargin / 2f);
            CreatureNeeds needs = NeedsAt(energy: .95f, hydration: betweenGates, health: .95f);

            bottleneck.Sample(needs, Body, Ready());

            Assert.That(ReproductionSystem.CanReproduce(needs, Body, Ready(), NeedFraction), Is.True);
            Assert.That(ReproductionSystem.CanSeekMate(needs, Body, Ready(), NeedFraction), Is.False);
            Assert.That(bottleneck.BlockedSamples, Is.EqualTo(0));
            Assert.That(bottleneck.MateSeekingBlockedSamples, Is.EqualTo(1));
        }

        [Test]
        public void SamplingOnlyHappensOnReproductionTicks()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 11, initialPopulation: 6);
            int interval = config.Schedule.BaseFrequencyHz / config.Schedule.ReproductionHz;

            Assert.That(ReproductionBottleneck.IsReproductionTick(config.Schedule, interval), Is.True);
            Assert.That(ReproductionBottleneck.IsReproductionTick(config.Schedule, interval + 1), Is.False);
        }

        [Test]
        public void ObservingAWorldDoesNotChangeIt()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 29, initialPopulation: 8);
            var baseline = new SimulationWorld(config);
            var observed = new SimulationWorld(config);
            var bottleneck = new ReproductionBottleneck(config.ReproductionNeedFraction);

            for (int step = 0; step < 400; step++)
            {
                baseline.Step(config.FixedDeltaTime);
                observed.Step(config.FixedDeltaTime);
                bottleneck.SampleWorld(observed);
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
            Assert.That(bottleneck.AdultSamples, Is.GreaterThan(0));
        }

        private static CreatureNeeds NeedsAt(float energy, float hydration, float health)
        {
            return new CreatureNeeds
            {
                Energy = Body.EnergyCapacity * energy,
                Hydration = Body.HydrationCapacity * hydration,
                Health = Body.HealthCapacity * health,
                Rest = 100f,
                Age = ReproductionSystem.AdultAgeSeconds + 1f,
            };
        }

        private static ReproductionState Ready()
        {
            return new ReproductionState { CooldownRemaining = 0f };
        }

        private static ReproductionState OnCooldown()
        {
            return new ReproductionState { CooldownRemaining = 5f };
        }
    }
}

using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PopulationCapDiagnosticTests
    {
        private const float NeedFraction = .7f;
        private const float UnsafeThreshold = .25f;

        [Test]
        public void AWorldHeldAtItsCapWithReadyCreaturesReportsCapBlockedSamplesAndSetsTheFlag()
        {
            var diagnostic = new PopulationCapDiagnostic(NeedFraction, UnsafeThreshold);

            for (int sample = 0; sample < 10; sample++)
            {
                diagnostic.Sample(population: 500, maximumPopulation: 500, readyToReproduce: 40);
            }

            Assert.That(diagnostic.ReproductionTickSamples, Is.EqualTo(10));
            Assert.That(diagnostic.SaturatedSamples, Is.EqualTo(10));
            Assert.That(diagnostic.CapSaturationFraction, Is.EqualTo(1f));
            Assert.That(diagnostic.CapBlockedSamples, Is.EqualTo(10));
            Assert.That(diagnostic.CapBlockedFraction, Is.EqualTo(1f));
            Assert.That(diagnostic.ReadyButUnbredAtCap, Is.EqualTo(400));
            Assert.That(diagnostic.ReproductiveSkewInterpretationIsUnsafe, Is.True);
        }

        [Test]
        public void AWorldWellBelowItsCapWithReadyCreaturesReportsNoCapBlockedSamplesAndLeavesTheFlagClear()
        {
            var diagnostic = new PopulationCapDiagnostic(NeedFraction, UnsafeThreshold);

            for (int sample = 0; sample < 10; sample++)
            {
                diagnostic.Sample(population: 120, maximumPopulation: 500, readyToReproduce: 40);
            }

            Assert.That(diagnostic.SaturatedSamples, Is.EqualTo(0));
            Assert.That(diagnostic.CapBlockedSamples, Is.EqualTo(0));
            Assert.That(diagnostic.CapBlockedFraction, Is.EqualTo(0f));
            Assert.That(diagnostic.MateLimitedSamples, Is.EqualTo(10), "Below the cap, a ready creature that did not breed failed to find a mate. That is ecology, not the cap.");
            Assert.That(diagnostic.ReadyButUnbredBelowCap, Is.EqualTo(400));
            Assert.That(diagnostic.ReproductiveSkewInterpretationIsUnsafe, Is.False);
        }

        [Test]
        public void OneReadyCreatureAtTheCapIsNotCapBlockedBecauseItHasNobodyToBreedWith()
        {
            var diagnostic = new PopulationCapDiagnostic(NeedFraction, UnsafeThreshold);

            diagnostic.Sample(population: 500, maximumPopulation: 500, readyToReproduce: 1);

            Assert.That(diagnostic.SaturatedSamples, Is.EqualTo(1));
            Assert.That(diagnostic.CapBlockedSamples, Is.EqualTo(0));
        }

        [Test]
        public void TheUnsafeFlagThresholdIsAnExplicitConstructorArgumentWithNoDefault()
        {
            // A silent default here would become a fact nobody chose, so the type takes it and
            // validates it rather than supplying one.
            Assert.That(typeof(PopulationCapDiagnostic).GetConstructors().Length, Is.EqualTo(1));
            foreach (System.Reflection.ParameterInfo parameter in typeof(PopulationCapDiagnostic).GetConstructors()[0].GetParameters())
            {
                Assert.That(parameter.HasDefaultValue, Is.False, parameter.Name + " must be supplied explicitly.");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new PopulationCapDiagnostic(NeedFraction, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PopulationCapDiagnostic(NeedFraction, 1.1f));
        }

        [Test]
        public void TheFlagTracksTheThresholdRatherThanAnyCapBlockingAtAll()
        {
            var diagnostic = new PopulationCapDiagnostic(NeedFraction, UnsafeThreshold);

            diagnostic.Sample(population: 500, maximumPopulation: 500, readyToReproduce: 40);
            for (int sample = 0; sample < 9; sample++)
            {
                diagnostic.Sample(population: 100, maximumPopulation: 500, readyToReproduce: 40);
            }

            Assert.That(diagnostic.CapBlockedFraction, Is.EqualTo(.1f).Within(1e-6f));
            Assert.That(diagnostic.ReproductiveSkewInterpretationIsUnsafe, Is.False);

            for (int sample = 0; sample < 3; sample++)
            {
                diagnostic.Sample(population: 500, maximumPopulation: 500, readyToReproduce: 40);
            }

            Assert.That(diagnostic.CapBlockedFraction, Is.GreaterThanOrEqualTo(UnsafeThreshold));
            Assert.That(diagnostic.ReproductiveSkewInterpretationIsUnsafe, Is.True);
        }

        [Test]
        public void CountingReadyCreaturesUsesTheProductionPredicate()
        {
            var creatures = new CreatureStore(4);
            creatures.Add(Genome.Neutral);
            creatures.Add(Genome.Neutral);
            SetNeeds(creatures, 0, energy: .95f, hydration: .95f, health: .95f, adult: true);
            SetNeeds(creatures, 1, energy: .2f, hydration: .95f, health: .95f, adult: true);

            Assert.That(PopulationCapDiagnostic.CountReadyToReproduce(creatures, NeedFraction), Is.EqualTo(1));
        }

        [Test]
        public void ObservingAWorldDoesNotChangeIt()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 31, initialPopulation: 8);
            var baseline = new SimulationWorld(config);
            var observed = new SimulationWorld(config);
            var diagnostic = new PopulationCapDiagnostic(config.ReproductionNeedFraction, UnsafeThreshold);

            for (int step = 0; step < 400; step++)
            {
                baseline.Step(config.FixedDeltaTime);
                observed.Step(config.FixedDeltaTime);
                diagnostic.SampleWorld(observed);
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
            Assert.That(diagnostic.ReproductionTickSamples, Is.GreaterThan(0));
        }

        private static void SetNeeds(CreatureStore creatures, int index, float energy, float hydration, float health, bool adult)
        {
            Phenotype phenotype = creatures.GetPhenotypeAt(index);
            ref CreatureNeeds needs = ref creatures.GetNeedsRefAt(index);
            needs.Energy = phenotype.EnergyCapacity * energy;
            needs.Hydration = phenotype.HydrationCapacity * hydration;
            needs.Health = phenotype.HealthCapacity * health;
            needs.Age = adult ? ReproductionSystem.AdultAgeSeconds + 1f : 0f;
        }
    }
}

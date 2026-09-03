using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class IngestionRecorderTests
    {
        [Test]
        public void ARunWithTheRecorderAttachedProducesTheSameStateHashAsOneWithout()
        {
            // The test that makes the instrument trustworthy. A sweep that perturbs its own subject
            // is worse than no sweep.
            SimulationConfig config = SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 97, initialPopulation: 12);
            var baseline = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(baseline);
            var observed = new SimulationWorld(config) { Recorder = new IngestionRecorder() };
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(observed);

            for (int step = 0; step < 2_000; step++)
            {
                baseline.Step(config.FixedDeltaTime);
                observed.Step(config.FixedDeltaTime);
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
            Assert.That(observed.Recorder.TotalGrossEnergy(ResourceKind.Food), Is.GreaterThan(0d), "A recorder that saw nothing would pass the hash test vacuously.");
        }

        [Test]
        public void TheRecorderIsNullByDefault()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(worldSeed: 5, initialPopulation: 4);
            var world = new SimulationWorld(config);

            Assert.That(world.Recorder, Is.Null);
        }

        [Test]
        public void AFullCreatureRecordsPositiveGrossZeroStoredAndGrossEqualToSurplus()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            var recorder = new IngestionRecorder();
            var creatureId = new CreatureId(7);

            float before = needs.Energy;
            float gross = NeedsSystem.GrossEnergyFrom(phenotype, amount: 1f);
            NeedsSystem.ConsumeFood(ref needs, phenotype, 1f);
            recorder.Record(creatureId, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: gross, storedEnergy: needs.Energy - before);

            Assert.That(gross, Is.GreaterThan(0f));
            Assert.That(recorder.GrossEnergy(creatureId, ResourceKind.Food), Is.EqualTo((double)gross).Within(1e-4));
            Assert.That(recorder.StoredEnergy(creatureId, ResourceKind.Food), Is.EqualTo(0d).Within(1e-4));
            Assert.That(recorder.SurplusEnergy(creatureId, ResourceKind.Food), Is.EqualTo((double)gross).Within(1e-4));
        }

        [Test]
        public void AHungryCreatureRecordsStoredEqualToGrossAndNoSurplus()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var recorder = new IngestionRecorder();
            var creatureId = new CreatureId(7);

            float gross = NeedsSystem.GrossEnergyFrom(phenotype, amount: 1f);
            NeedsSystem.ConsumeFood(ref needs, phenotype, 1f);
            recorder.Record(creatureId, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: gross, storedEnergy: needs.Energy - 0f);

            Assert.That(recorder.StoredEnergy(creatureId, ResourceKind.Food), Is.EqualTo((double)gross).Within(1e-4));
            Assert.That(recorder.SurplusEnergy(creatureId, ResourceKind.Food), Is.EqualTo(0d).Within(1e-4));
        }

        [Test]
        public void PlantAndCarcassAreRecordedSeparately()
        {
            var recorder = new IngestionRecorder();
            var creatureId = new CreatureId(3);

            recorder.Record(creatureId, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 2f, grossEnergy: 10f, storedEnergy: 6f);
            recorder.Record(creatureId, ResourceKind.Carcass, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: 20f, storedEnergy: 20f);

            Assert.That(recorder.ResourceAmount(creatureId, ResourceKind.Food), Is.EqualTo(2d).Within(1e-6));
            Assert.That(recorder.GrossEnergy(creatureId, ResourceKind.Food), Is.EqualTo(10d).Within(1e-6));
            Assert.That(recorder.SurplusEnergy(creatureId, ResourceKind.Food), Is.EqualTo(4d).Within(1e-6));
            Assert.That(recorder.GrossEnergy(creatureId, ResourceKind.Carcass), Is.EqualTo(20d).Within(1e-6));
            Assert.That(recorder.SurplusEnergy(creatureId, ResourceKind.Carcass), Is.EqualTo(0d).Within(1e-6));
            Assert.That(recorder.FeedingTicks(creatureId, ResourceKind.Food), Is.EqualTo(1));
            Assert.That(recorder.FeedingTicks(creatureId, ResourceKind.Carcass), Is.EqualTo(1));
        }

        [Test]
        public void IngestionUnderAStaleSeekActionIsCountedSeparatelyBecauseTheOldProxyCouldNotSeeIt()
        {
            var recorder = new IngestionRecorder();
            var creatureId = new CreatureId(4);

            recorder.Record(creatureId, ResourceKind.Food, underStaleSeekAction: true, resourceAmount: 1f, grossEnergy: 8f, storedEnergy: 8f);
            recorder.Record(creatureId, ResourceKind.Food, underStaleSeekAction: false, resourceAmount: 1f, grossEnergy: 8f, storedEnergy: 8f);

            Assert.That(recorder.FeedingTicks(creatureId, ResourceKind.Food), Is.EqualTo(2));
            Assert.That(recorder.StaleActionFeedingTicks(creatureId, ResourceKind.Food), Is.EqualTo(1));
            Assert.That(recorder.StaleActionGrossEnergy(creatureId, ResourceKind.Food), Is.EqualTo(8d).Within(1e-6));
            Assert.That(recorder.TotalStaleActionGrossEnergy(ResourceKind.Food), Is.EqualTo(8d).Within(1e-6));
        }

        [Test]
        public void AccumulatorsAreKeyedByCreatureIdRatherThanByAnIndexThatSwapRemoveInvalidates()
        {
            var recorder = new IngestionRecorder();
            recorder.Record(new CreatureId(1), ResourceKind.Food, false, 1f, 5f, 5f);
            recorder.Record(new CreatureId(4_096), ResourceKind.Food, false, 1f, 7f, 7f);

            Assert.That(recorder.GrossEnergy(new CreatureId(1), ResourceKind.Food), Is.EqualTo(5d).Within(1e-6));
            Assert.That(recorder.GrossEnergy(new CreatureId(4_096), ResourceKind.Food), Is.EqualTo(7d).Within(1e-6));
            Assert.That(recorder.GrossEnergy(new CreatureId(2), ResourceKind.Food), Is.EqualTo(0d));
            Assert.That(recorder.TotalGrossEnergy(ResourceKind.Food), Is.EqualTo(12d).Within(1e-6));
        }

        [Test]
        public void GrossEnergyMatchesWhatConsumeFoodWouldHaveAddedToAnEmptyCreature()
        {
            // The extracted helper must be the same expression ConsumeFood uses, in the same order.
            Phenotype phenotype = Phenotype.FromGenome(new Genome(.7f, .5f, .5f, .5f, .5f, .3f));
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;

            NeedsSystem.ConsumeFood(ref needs, phenotype, .25f);

            Assert.That(needs.Energy, Is.EqualTo(NeedsSystem.GrossEnergyFrom(phenotype, .25f)));
        }
    }
}

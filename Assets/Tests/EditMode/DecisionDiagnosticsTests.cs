using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class DecisionDiagnosticsTests
    {
        [Test]
        public void NewDiagnosticsFieldsDefaultToZeroAndPreserveExistingScores()
        {
            var diagnostics = new DecisionDiagnostics(0.4f, 0.2f, foodVisible: true, waterVisible: false);

            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.4f));
            Assert.That(diagnostics.WaterScore, Is.EqualTo(0.2f));
            Assert.That(diagnostics.FleeScore, Is.EqualTo(0f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0f));
            Assert.That(diagnostics.CarcassScore, Is.EqualTo(0f));
            Assert.That(diagnostics.ThermalScore, Is.EqualTo(0f));
        }

        [Test]
        public void PredationScoresAreRecordedWithoutDisturbingForagingScores()
        {
            var diagnostics = new DecisionDiagnostics(0.4f, 0.2f, foodVisible: true, waterVisible: false)
                .WithPredationScores(fleeScore: 0.7f, huntScore: 0.1f);

            Assert.That(diagnostics.FleeScore, Is.EqualTo(0.7f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0.1f));
            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.4f));
            Assert.That(diagnostics.FoodVisible, Is.True);
        }

        [Test]
        public void WinningActionIsRecorded()
        {
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false)
                .WithWinningAction(CreatureAction.Flee);

            Assert.That(diagnostics.WinningAction, Is.EqualTo(CreatureAction.Flee));
        }

        [Test]
        public void DecidingAgainstAThreatRecordsBothPredationScores()
        {
            Phenotype prey = Phenotype.FromGenome(new Genome(0.2f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, fear: 1f));
            Phenotype predator = Phenotype.FromGenome(new Genome(0.9f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(prey);
            var observation = new CreatureObservation(new CreatureId(2), 1, 1f);
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            PredationSystem.Decide(
                needs,
                prey,
                predator,
                observation,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.FleeScore, Is.GreaterThan(0f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0f));
        }

        [Test]
        public void ConsideringACarcassRecordsItsScore()
        {
            Phenotype scavenger = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(scavenger);
            needs.Energy = 0f;
            var carcass = new ResourceObservation(new ResourceId(3), 0, 1f);
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            PredationSystem.PreferCarcassWhenUseful(
                needs,
                scavenger,
                carcass,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.CarcassScore, Is.GreaterThan(0f));
        }

        [Test]
        public void ConsideringThermalComfortRecordsItsScore()
        {
            Phenotype intolerant = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, temperatureTolerance: 0f));
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            ThermoregulationSystem.PreferThermalComfort(
                intolerant,
                new SimVector2(20f, 20f),
                0L,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.ThermalScore, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void StoredDiagnosticsRecordTheWinningAction()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            CreatureId creatureId = world.GetCreatureIdAt(0);
            world.SetCreaturePosition(creatureId, new SimVector2(0f, 0f));
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), interactionRadius: 5f, initialAmount: 50f, capacity: 50f, regenerationPerSecond: 0f);
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;

            for (int step = 0; step < 20; step++)
            {
                world.Step(config.FixedDeltaTime);
            }

            DecisionDiagnostics diagnostics = world.GetCreatureDecisionDiagnosticsAt(0);
            CreatureDecision decision = world.GetCreatureDecisionAt(0);

            Assert.That(diagnostics.WinningAction, Is.EqualTo(decision.Action));
            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.Wander));
        }
    }
}

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
    }
}

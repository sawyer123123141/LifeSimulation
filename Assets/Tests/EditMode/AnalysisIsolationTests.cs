using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class AnalysisIsolationTests
    {
        [Test]
        public void ExternalHistoryAndGenomeSamplesDoNotChangeTheSimulationHash()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(worldSeed: 73, initialPopulation: 4);
            var baseline = new SimulationWorld(config);
            var observed = new SimulationWorld(config);
            var history = new AncestryHistory();
            history.RecordFounders(0, observed.Creatures);

            for (int step = 0; step < 120; step++)
            {
                baseline.Step(config.FixedDeltaTime);
                observed.Step(config.FixedDeltaTime);
                history.Record(observed.Events);
                PopulationGenomeSnapshot.Capture(observed.CurrentTick, observed.Creatures);
                observed.Events.Clear();
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
        }
    }
}

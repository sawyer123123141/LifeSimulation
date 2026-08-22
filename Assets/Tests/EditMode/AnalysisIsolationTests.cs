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
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, observed.Creatures);
            var history = new GeneticClusterHistory(
                new ClusterHistoryPolicy(
                    minimumSupportedCurrentMembers: 1,
                    minimumCurrentSupportFraction: .5f,
                    minimumSupportingPreviousMembers: 1,
                    minimumPreviousSupportFraction: .5f,
                    maximumAncestorGenerations: 3,
                    requiredSuccessorObservations: 1,
                    requiredAbsentObservations: 2),
                new ClusterHistoryEventBuffer(256));

            for (int step = 0; step < 120; step++)
            {
                baseline.Step(config.FixedDeltaTime);
                observed.Step(config.FixedDeltaTime);
                ancestry.RecordCompleteBatch(observed.Events, observed.CurrentTick);
                GeneticClusterObservation observation = GeneticClusterObservation.Create(
                    PopulationGenomeSnapshot.Capture(observed.CurrentTick, observed.Creatures),
                    threshold: .25f);
                history.Record(observation, ancestry);
                observed.Events.Clear();
            }

            Assert.That(observed.ComputeStateHash(), Is.EqualTo(baseline.ComputeStateHash()));
        }
    }
}

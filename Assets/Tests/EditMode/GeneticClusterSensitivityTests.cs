using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusterSensitivityTests
    {
        [Test]
        public void AnalysisReportsClusterCountForEachCallerSuppliedThreshold()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.FromTraits(new float[Genome.TraitCount]));
            var highTraits = new float[Genome.TraitCount];
            for (int index = 0; index < highTraits.Length; index++) highTraits[index] = 1f;
            creatures.Add(Genome.FromTraits(highTraits));

            GeneticClusterSensitivity analysis = GeneticClusterSensitivity.Analyze(
                PopulationGenomeSnapshot.Capture(10, creatures),
                new[] { .5f, 1f });

            Assert.That(analysis.Count, Is.EqualTo(2));
            Assert.That(analysis.GetAt(0).Threshold, Is.EqualTo(.5f));
            Assert.That(analysis.GetAt(0).ClusterCount, Is.EqualTo(2));
            Assert.That(analysis.GetAt(1).Threshold, Is.EqualTo(1f));
            Assert.That(analysis.GetAt(1).ClusterCount, Is.EqualTo(1));
        }
    }
}

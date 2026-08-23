using System;
using System.Reflection;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// On 2026-08-22 three plant conclusions became permanently unverifiable because the scenario
    /// they ran on lived in a throwaway probe and was never committed. The writeups recorded the
    /// site count, the config, the seeds and the resulting occupancy - everything except the
    /// coordinates - and no amount of searching recovered them.
    ///
    /// <para>A manifest is what would have prevented that: enough provenance beside every result to
    /// re-run it, and a layout fingerprint that tells a future session whether the scenario it is
    /// holding is the one that produced those numbers.</para>
    /// </summary>
    public sealed class ExperimentManifestTests
    {
        [Test]
        public void LayoutFingerprintDistinguishesScenariosThatShareAnIdentifier()
        {
            SimulationScenario first = BuildScenario("same-id", foodX: 10f);
            SimulationScenario second = BuildScenario("same-id", foodX: 11f);

            Assert.That(first.Id, Is.EqualTo(second.Id));
            Assert.That(first.ComputeLayoutFingerprint(), Is.Not.EqualTo(second.ComputeLayoutFingerprint()),
                "an identifier alone cannot prove two scenarios are the same layout - that is exactly how the 168-site geometry was lost");
        }

        [Test]
        public void LayoutFingerprintIsStableAcrossEquivalentConstructions()
        {
            Assert.That(BuildScenario("s", 10f).ComputeLayoutFingerprint(),
                Is.EqualTo(BuildScenario("s", 10f).ComputeLayoutFingerprint()));
        }

        [Test]
        public void LayoutFingerprintNoticesFounderPlacement()
        {
            SimulationScenario placed = new SimulationScenario("s", new[] { Food(0f) }, founderPlacement: new SimVector2(1f, 2f));
            SimulationScenario unplaced = new SimulationScenario("s", new[] { Food(0f) });

            Assert.That(placed.ComputeLayoutFingerprint(), Is.Not.EqualTo(unplaced.ComputeLayoutFingerprint()));
        }

        [Test]
        public void ManifestRequiresARevisionLabel()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 12);

            Assert.That(
                () => ExperimentManifest.Describe(null, BuildScenario("s", 10f), config, firstSeed: 42, seedCount: 30, ticks: 12000),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(
                () => ExperimentManifest.Describe("   ", BuildScenario("s", 10f), config, firstSeed: 42, seedCount: 30, ticks: 12000),
                Throws.InstanceOf<ArgumentException>(),
                "a blank revision is the same failure as no revision - it cannot be traced back to code");
        }

        [Test]
        public void ManifestRecordsWhatIsNeededToReRunTheExperiment()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 12);
            SimulationScenario scenario = BuildScenario("p4-example", 10f);

            string manifest = ExperimentManifest.Describe("abc1234", scenario, config, firstSeed: 42, seedCount: 30, ticks: 12000);

            Assert.That(manifest, Does.Contain("abc1234"));
            Assert.That(manifest, Does.Contain("p4-example"));
            Assert.That(manifest, Does.Contain(scenario.ComputeLayoutFingerprint().ToString()));
            Assert.That(manifest, Does.Contain("42"));
            Assert.That(manifest, Does.Contain("12000"));
            Assert.That(manifest, Does.Contain("schema"));
        }

        [Test]
        public void ManifestMentionsEveryBehaviourFlagOnTheConfiguration()
        {
            // Reflection lives in the test, never in the simulation. If a new flag is added and the
            // manifest is not updated, this fails - which is the point: a manifest that silently
            // omits a flag is worse than none, because it looks complete.
            SimulationConfig config = SimulationConfig.CreateFullEcosystemDefaults(42, 12);
            string manifest = ExperimentManifest.Describe("abc1234", BuildScenario("s", 10f), config, 42, 30, 12000);

            foreach (PropertyInfo property in typeof(SimulationConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType != typeof(bool))
                {
                    continue;
                }

                Assert.That(manifest, Does.Contain(property.Name),
                    $"manifest omits the {property.Name} flag, so a result produced under it could not be reproduced");
            }
        }

        [Test]
        public void ComposingACsvWithoutAManifestIsRefused()
        {
            Assert.That(
                () => ExperimentCsv.Compose(null, "a,b", new[] { "1,2" }),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(
                () => ExperimentCsv.Compose("", "a,b", new[] { "1,2" }),
                Throws.InstanceOf<ArgumentException>(),
                "writing results without provenance is the failure mode this type exists to prevent");
        }

        [Test]
        public void ComposedCsvKeepsTheManifestAsCommentsAboveTheHeader()
        {
            string manifest = ExperimentManifest.Describe("abc1234", BuildScenario("s", 10f), SimulationConfig.CreatePrototype4Defaults(42, 12), 42, 30, 12000);

            string csv = ExperimentCsv.Compose(manifest, "seed,value", new[] { "42,1.0", "43,2.0" });

            string[] lines = csv.Split('\n');
            Assert.That(lines[0], Does.StartWith("#"), "provenance must be commented so a reader does not treat it as data");
            int headerIndex = Array.IndexOf(lines, "seed,value");
            Assert.That(headerIndex, Is.GreaterThan(0), "the header must follow the manifest");
            Assert.That(lines[headerIndex + 1], Is.EqualTo("42,1.0"));
            for (int index = 0; index < headerIndex; index++)
            {
                Assert.That(lines[index], Does.StartWith("#"));
            }
        }

        private static SimulationScenario BuildScenario(string id, float foodX)
        {
            return new SimulationScenario(id, new[] { Food(foodX) }, founderPlacement: new SimVector2(-12f, -8f));
        }

        private static ResourceDefinition Food(float x)
        {
            return new ResourceDefinition(ResourceKind.Food, new SimVector2(x, 0f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f);
        }
    }
}

using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// A NaN or infinity entering the simulation at a configuration or scenario boundary is never
    /// caught later: NaN propagates silently through every arithmetic operation, survives
    /// <c>Math.Max(0f, Math.Min(1f, value))</c> clamping unchanged, and poisons state hashes in a
    /// way that reads as nondeterminism rather than as bad input. These tests pin rejection at the
    /// boundary, which is the only place it is cheap.
    ///
    /// <para>Configuration follows the established convention: <c>SimulationConfig</c> constructs
    /// freely and <c>Validate()</c> is the gate, called by <c>SimulationWorld</c>'s constructor.
    /// Scenario data has no such deferred gate, so <c>ResourceDefinition</c> rejects at
    /// construction.</para>
    /// </summary>
    public sealed class FiniteValidationTests
    {
        private static readonly float[] NonFiniteValues = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };

        [Test]
        public void ClampingDoesNotRemoveNaN()
        {
            // Documents WHY validation has to happen at the boundary: the clamp everything else
            // relies on is not a filter. If this ever starts failing, the clamp changed and the
            // boundary rules below can be reconsidered.
            float clamped = Math.Max(0f, Math.Min(1f, float.NaN));

            Assert.That(float.IsNaN(clamped), Is.True, "clamping is not a NaN filter, so the boundary must reject it");
        }

        [Test]
        public void ConfigRejectsNonFiniteThreatFalloffDistance()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => CreateConfig(threatFalloffDistance: value).Validate(),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"threatFalloffDistance {value} must be rejected at construction");
            }
        }

        [Test]
        public void ConfigRejectsNonFiniteHandlingSeconds()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => CreateConfig(handlingSeconds: value).Validate(),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"handlingSeconds {value} must be rejected at construction");
            }
        }

        [Test]
        public void ConfigRejectsNonFinitePlantDefenseDeterrenceStrength()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => CreateConfig(plantDefenseDeterrenceStrength: value).Validate(),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"plantDefenseDeterrenceStrength {value} must be rejected at construction");
            }
        }

        [Test]
        public void ConfigRejectsNonFiniteSeedProductionRateDispersalCharge()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => CreateConfig(plantSeedProductionRateDispersalCharge: value).Validate(),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"plantSeedProductionRateDispersalCharge {value} must be rejected at construction");
            }
        }

        [Test]
        public void ConfigStillAcceptsItsOwnDefaults()
        {
            Assert.That(() => SimulationConfig.CreatePrototype4Defaults(42, 12).Validate(), Throws.Nothing);
            Assert.That(() => SimulationConfig.CreateFullEcosystemDefaults(42, 12).Validate(), Throws.Nothing);
            Assert.That(() => CreateConfig().Validate(), Throws.Nothing);
        }

        [Test]
        public void ResourceDefinitionRejectsNonFiniteAmounts()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, 0f), 1.5f, value, 24f, 1f),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"initialAmount {value} must be rejected");
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, 0f), 1.5f, 12f, value, 1f),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"capacity {value} must be rejected");
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, 0f), 1.5f, 12f, 24f, value),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"regenerationPerSecond {value} must be rejected");
            }
        }

        [Test]
        public void ResourceDefinitionRejectsNonFinitePositionAndRadius()
        {
            foreach (float value in NonFiniteValues)
            {
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(value, 0f), 1.5f, 12f, 24f, 1f),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"position.X {value} must be rejected");
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, value), 1.5f, 12f, 24f, 1f),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"position.Y {value} must be rejected");
                Assert.That(
                    () => new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, 0f), value, 12f, 24f, 1f),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"interactionRadius {value} must be rejected");
            }
        }

        [Test]
        public void EveryCommittedScenarioStillConstructs()
        {
            // The validation must not reject any scenario the repository actually ships.
            Assert.That(Prototype4Scenarios.ObservationStable.ResourceCount, Is.GreaterThan(0));
            Assert.That(Prototype4Scenarios.ObservationRouteRing.ResourceCount, Is.GreaterThan(0));
            Assert.That(Prototype4Scenarios.ObservationShiftingPatches.ResourceCount, Is.GreaterThan(0));
            Assert.That(Prototype4Scenarios.AbundantSiteReplicationModerate.ResourceCount, Is.EqualTo(174));
            Assert.That(Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ResourceCount, Is.GreaterThan(0));
        }

        private static SimulationConfig CreateConfig(
            float handlingSeconds = SimulationConfig.DefaultHandlingSeconds,
            float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
            float plantDefenseDeterrenceStrength = SimulationConfig.DefaultPlantDefenseDeterrenceStrength,
            float plantSeedProductionRateDispersalCharge = SimulationConfig.DefaultPlantSeedProductionRateDispersalCharge)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 4);
            return new SimulationConfig(
                worldSeed: 42,
                initialPopulation: 4,
                schedule: defaults.Schedule,
                maximumPopulation: 48,
                handlingSeconds: handlingSeconds,
                threatFalloffDistance: threatFalloffDistance,
                plantDefenseDeterrenceStrength: plantDefenseDeterrenceStrength,
                plantSeedProductionRateDispersalCharge: plantSeedProductionRateDispersalCharge);
        }
    }
}

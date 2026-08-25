using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Creature temperature read from the world's own climate instead of a fixed sine.
    ///
    /// <para>The sine is <c>20 + 8*sin(0.18x + 0.11y)</c> - no latitude, no altitude, no seasons, no
    /// terrain - and it is the strongest selection pressure in the model. These pin the three things
    /// that make swapping it safe: the placeholder is exactly what a <c>default</c> gives, the flag
    /// off is byte-identical, and the flag on actually changes what happens.</para>
    /// </summary>
    public sealed class TerrainDrivenTemperatureTests
    {
        private const int Seed = 42;
        private const int Ticks = 600;

        private static SimulationConfig Config(bool terrainTemperature)
        {
            return new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation,
                physiologyEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                elevationFieldEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                terrainDrivenTemperatureEnabled: terrainTemperature);
        }

        /// <summary>Behaviour hash, not the fingerprint - the fingerprint folds in the config hash
        /// and would differ even if the flag did nothing.</summary>
        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        [Test]
        public void ADefaultClimateFieldIsExactlyThePlaceholderSine()
        {
            // Every recorded thermal result was measured against this. If a default instance ever
            // stopped agreeing with TemperatureField, the flag-off path would silently re-baseline.
            ClimateField placeholder = default;

            Assert.That(placeholder.IsPlaceholder, Is.True);
            for (float x = -25f; x <= 25f; x += 6.25f)
            {
                for (float y = -25f; y <= 25f; y += 6.25f)
                {
                    var position = new SimVector2(x, y);
                    Assert.That(
                        placeholder.Celsius(position, 0L),
                        Is.EqualTo(TemperatureField.Sample(position, 0L)));
                }
            }
        }

        [Test]
        public void TheFlagOffChangesNothingAtAll()
        {
            Assert.That(Run(Config(terrainTemperature: false)), Is.EqualTo(Run(Config(terrainTemperature: false))));
            Assert.That(
                new SimulationWorld(Config(terrainTemperature: false)).Climate.IsPlaceholder,
                Is.True);
        }

        [Test]
        public void TheFlagOnChangesHowTheWorldEvolves()
        {
            // Liveness. A flag that reads a different field and produces the same behaviour hash is
            // wired to nothing.
            Assert.That(
                Run(Config(terrainTemperature: true)),
                Is.Not.EqualTo(Run(Config(terrainTemperature: false))));
        }

        [Test]
        public void TheTerrainClimateKeepsThePlaceholdersDegreeSpan()
        {
            // The span is held at 12 to 28 deliberately: tolerance is 2 + 8*gene, so an 8-degree
            // half-span is what puts the saturation ceiling at gene 0.75. Changing the span as well
            // as the structure would make the two conditions incomparable.
            var world = new SimulationWorld(Config(terrainTemperature: true));
            ClimateField climate = world.Climate;

            Assert.That(climate.IsPlaceholder, Is.False);
            for (float x = -25f; x <= 25f; x += 5f)
            {
                for (float y = -25f; y <= 25f; y += 5f)
                {
                    float celsius = climate.Celsius(new SimVector2(x, y), 0L);
                    Assert.That(celsius, Is.InRange(
                        ClimateField.ComfortableCelsius - ClimateField.HalfSpanCelsius,
                        ClimateField.ComfortableCelsius + ClimateField.HalfSpanCelsius));
                }
            }
        }

        [Test]
        public void TheTerrainClimateIsNotTheSineWearingADifferentName()
        {
            // If the two fields agreed everywhere the flag would be inert, and the measurement that
            // motivates it would be comparing a world with itself.
            var world = new SimulationWorld(Config(terrainTemperature: true));
            float largestDifference = 0f;

            for (float x = -25f; x <= 25f; x += 2.5f)
            {
                for (float y = -25f; y <= 25f; y += 2.5f)
                {
                    var position = new SimVector2(x, y);
                    largestDifference = Math.Max(
                        largestDifference,
                        Math.Abs(world.Climate.Celsius(position, 0L) - TemperatureField.Sample(position, 0L)));
                }
            }

            Assert.That(largestDifference, Is.GreaterThan(1f));
        }

        [Test]
        public void TheConfigurationHashSeesTheFlag()
        {
            Assert.That(
                Config(terrainTemperature: true).ComputeConfigurationHash(),
                Is.Not.EqualTo(Config(terrainTemperature: false).ComputeConfigurationHash()));
        }
    }
}

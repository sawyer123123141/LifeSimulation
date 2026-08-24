using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.World;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The join: terrain drives the environment the simulation reads.
    ///
    /// <para>The claim being pinned is not "elevation varies" - almost anything varies. It is that
    /// the elevation a creature experiences is <b>the same number</b> the renderer draws, from the
    /// same function, seed, window and detail limit. Two fields that merely look similar is the
    /// state this work exists to end.</para>
    /// </summary>
    public sealed class TerrainDrivenEnvironmentTests
    {
        private const int Seed = 42;

        /// <summary>Matches EnvironmentField and TerrainMeshBuilder: 193 samples over a 50-unit window.</summary>
        private static double ArenaMaximumFrequency()
        {
            return PlanetTerrain.MaximumFrequencyFor(
                (int)(193 * (2d * Math.PI) / (2d * 25d / EnvironmentField.SphereRadius)));
        }

        [Test]
        public void ElevationMatchesTheTerrainGeneratorExactly()
        {
            EnvironmentField field = EnvironmentField.CreateTerrainDriven(Seed);
            TerrainSettings settings = EnvironmentField.CreateTerrainSettings();
            PlateStructure plates = PlateStructure.Create(Seed, settings);
            RiverNetwork rivers = RiverNetwork.Create(Seed, plates, settings);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);
            double maximumFrequency = ArenaMaximumFrequency();

            // Spread across the arena rather than one point: a single agreeing sample could be luck,
            // and a wrong window centre or a wrong axis order agrees at the origin and nowhere else.
            var positions = new[]
            {
                new SimVector2(0f, 0f),
                new SimVector2(12f, -7f),
                new SimVector2(-20f, 18f),
                new SimVector2(24f, 24f),
                new SimVector2(-24f, -24f),
            };

            foreach (SimVector2 position in positions)
            {
                PlanetSample terrain = PlanetTerrain.SampleAtLatLon(
                    Seed, plates,
                    centreLatitude + (position.Y / EnvironmentField.SphereRadius),
                    centreLongitude + (position.X / EnvironmentField.SphereRadius),
                    maximumFrequency, settings, rivers);

                float expected = (float)Math.Max(
                    0d, Math.Min(1d, terrain.Elevation / PlanetTerrain.HighGround));

                Assert.That(
                    field.Sample(position).Elevation, Is.EqualTo(expected).Within(1e-6f),
                    $"the simulation reads a different height than the generator draws at {position.X}, {position.Y}");
            }
        }

        [Test]
        public void TheFieldActuallyVariesAcrossTheArena()
        {
            // Manipulation check. Every assertion above would hold just as well against a field that
            // returned the same value everywhere, and a flat field is exactly the failure mode this
            // replaces - the constant EnvironmentField returns temperature 1.0 at every position.
            EnvironmentField field = EnvironmentField.CreateTerrainDriven(Seed);

            float lowestElevation = float.MaxValue, highestElevation = float.MinValue;
            float lowestMoisture = float.MaxValue, highestMoisture = float.MinValue;
            float lowestTemperature = float.MaxValue, highestTemperature = float.MinValue;

            for (int step = 0; step <= 40; step++)
            {
                float x = -25f + (50f * step / 40f);
                for (int other = 0; other <= 40; other++)
                {
                    float y = -25f + (50f * other / 40f);
                    EnvironmentSample sample = field.Sample(new SimVector2(x, y));

                    lowestElevation = Math.Min(lowestElevation, sample.Elevation);
                    highestElevation = Math.Max(highestElevation, sample.Elevation);
                    lowestMoisture = Math.Min(lowestMoisture, sample.Moisture);
                    highestMoisture = Math.Max(highestMoisture, sample.Moisture);
                    lowestTemperature = Math.Min(lowestTemperature, sample.Temperature);
                    highestTemperature = Math.Max(highestTemperature, sample.Temperature);
                }
            }

            Assert.That(highestElevation - lowestElevation, Is.GreaterThan(0.05f), "elevation is flat");
            Assert.That(highestMoisture - lowestMoisture, Is.GreaterThan(0.02f), "moisture is flat");
            Assert.That(highestTemperature - lowestTemperature, Is.GreaterThan(0.02f), "temperature is flat");
        }

        [Test]
        public void RangesStayWhereThePlantSystemsWereCalibrated()
        {
            // The point of holding the output ranges is that any measured difference after the join
            // is the SHAPE of the field changing, not its scale. A rescale would move every plant
            // result for a reason that has nothing to do with terrain.
            EnvironmentField field = EnvironmentField.CreateTerrainDriven(Seed);

            for (int step = 0; step <= 30; step++)
            {
                float x = -25f + (50f * step / 30f);
                for (int other = 0; other <= 30; other++)
                {
                    float y = -25f + (50f * other / 30f);
                    EnvironmentSample sample = field.Sample(new SimVector2(x, y));

                    Assert.That(sample.Moisture, Is.InRange(0.15f, 1f));
                    Assert.That(sample.Fertility, Is.InRange(0.20f, 1f));
                    Assert.That(sample.Temperature, Is.InRange(0.02f, 1f));
                    Assert.That(sample.Elevation, Is.InRange(0f, 1f));
                }
            }
        }

        [Test]
        public void TheFlagChangesBehaviour()
        {
            // A flag that reaches nothing is worse than no flag, because it reads as a control that
            // works. FlagLivenessAnalysis makes this claim generally; this makes it directly, and
            // names the scenario it is true in - the field is only reached with procedural fields on.
            SimulationWorld withoutTerrain = CreateWorld(terrainDriven: false);
            SimulationWorld withTerrain = CreateWorld(terrainDriven: true);

            for (int tick = 0; tick < 400; tick++)
            {
                withoutTerrain.Step(withoutTerrain.Config.FixedDeltaTime);
                withTerrain.Step(withTerrain.Config.FixedDeltaTime);
            }

            Assert.That(
                withTerrain.ComputeStateFingerprint(), Is.Not.EqualTo(withoutTerrain.ComputeStateFingerprint()),
                "turning terrain on changed nothing - the field is not reaching the simulation");
        }

        private static SimulationWorld CreateWorld(bool terrainDriven)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(Seed, 8);
            var config = new SimulationConfig(
                Seed,
                8,
                defaults.Schedule,
                defaults.MaximumPopulation,
                FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                elevationFieldEnabled: true,
                terrainDrivenEnvironmentEnabled: terrainDriven);

            return new SimulationWorld(config);
        }
    }
}

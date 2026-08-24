using System;
using LifeSimulation.Simulation.World;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Rivers are the first terrain feature that is not a pure function of a point, so the things
    /// worth pinning are the ones a pure function got for free: that two worlds with one seed agree,
    /// that ground away from a channel is untouched, and that a channel is a cut rather than a wall.
    /// </summary>
    public sealed class RiverNetworkTests
    {
        private const int Seed = 42;

        private static readonly double MaximumFrequency = PlanetTerrain.MaximumFrequencyFor(
            (int)(193 * (2d * Math.PI) / (50d / 500d)));

        private static RiverNetwork Build(out PlateStructure plates, out TerrainSettings settings)
        {
            settings = new TerrainSettings();
            plates = PlateStructure.Create(Seed, settings);
            return RiverNetwork.Create(Seed, plates, settings);
        }

        [Test]
        public void TheSameSeedWalksTheSameRivers()
        {
            RiverNetwork first = Build(out PlateStructure plates, out TerrainSettings settings);
            RiverNetwork second = RiverNetwork.Create(Seed, plates, settings);

            Assert.That(second.RiverCount, Is.EqualTo(first.RiverCount));
            Assert.That(second.PointCount, Is.EqualTo(first.PointCount));

            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);
            for (int step = 0; step < 64; step++)
            {
                double latitude = centreLatitude + (((step / 63d) - 0.5d) * 0.1d);
                double longitude = centreLongitude + (((step / 63d) - 0.5d) * 0.1d);
                double cosLatitude = Math.Cos(latitude);
                double x = cosLatitude * Math.Sin(longitude);
                double y = Math.Sin(latitude);
                double z = cosLatitude * Math.Cos(longitude);

                Assert.That(second.Proximity(x, y, z), Is.EqualTo(first.Proximity(x, y, z)),
                    "rivers stopped being deterministic in the seed");
            }
        }

        [Test]
        public void TheWalkProducesRiversThatReachTheSea()
        {
            RiverNetwork rivers = Build(out PlateStructure _, out TerrainSettings _);

            // Walk() discards any path that stalls inland, so a nonzero count IS the claim that these
            // rivers reach water. Pinned because a change to the step size or the source threshold
            // can silently drop every river without failing anything else.
            Assert.That(rivers.RiverCount, Is.GreaterThan(0), "no river reached the sea");
            Assert.That(rivers.PointCount, Is.GreaterThan(100), "rivers exist but are stubs");
        }

        [Test]
        public void GroundAwayFromAChannelIsUntouched()
        {
            RiverNetwork rivers = Build(out PlateStructure plates, out TerrainSettings settings);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            int checkedPoints = 0;
            for (int row = 0; row < 48; row++)
            {
                for (int column = 0; column < 48; column++)
                {
                    double latitude = centreLatitude + (((row / 47d) - 0.5d) * 0.1d);
                    double longitude = centreLongitude + (((column / 47d) - 0.5d) * 0.1d);
                    double cosLatitude = Math.Cos(latitude);
                    double x = cosLatitude * Math.Sin(longitude);
                    double y = Math.Sin(latitude);
                    double z = cosLatitude * Math.Cos(longitude);
                    if (rivers.Proximity(x, y, z) > 0d) continue;

                    PlanetSample without = PlanetTerrain.Sample(Seed, plates, x, y, z, MaximumFrequency, settings);
                    PlanetSample with = PlanetTerrain.Sample(Seed, plates, x, y, z, MaximumFrequency, settings, rivers);

                    Assert.That(with.Elevation, Is.EqualTo(without.Elevation),
                        "a river changed ground it does not touch");
                    Assert.That(with.Moisture, Is.EqualTo(without.Moisture));
                    checkedPoints++;
                }
            }

            Assert.That(checkedPoints, Is.GreaterThan(1000), "the window was almost entirely river, which is wrong");
        }

        [Test]
        public void AChannelCutsDownAndNeverUp()
        {
            RiverNetwork rivers = Build(out PlateStructure plates, out TerrainSettings settings);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            int touched = 0;
            for (int row = 0; row < 96; row++)
            {
                for (int column = 0; column < 96; column++)
                {
                    double latitude = centreLatitude + (((row / 95d) - 0.5d) * 0.1d);
                    double longitude = centreLongitude + (((column / 95d) - 0.5d) * 0.1d);
                    double cosLatitude = Math.Cos(latitude);
                    double x = cosLatitude * Math.Sin(longitude);
                    double y = Math.Sin(latitude);
                    double z = cosLatitude * Math.Cos(longitude);
                    double proximity = rivers.Proximity(x, y, z);
                    if (proximity <= 0d) continue;

                    touched++;
                    PlanetSample without = PlanetTerrain.Sample(Seed, plates, x, y, z, MaximumFrequency, settings);
                    PlanetSample with = PlanetTerrain.Sample(Seed, plates, x, y, z, MaximumFrequency, settings, rivers);

                    Assert.That(with.Elevation, Is.LessThanOrEqualTo(without.Elevation + 1e-6f),
                        "a river raised the ground");
                    Assert.That(without.Elevation - with.Elevation,
                        Is.LessThanOrEqualTo((float)RiverNetwork.Carve(1d) + 1e-6f),
                        "a channel cut deeper than one channel depth");
                    Assert.That(with.Moisture, Is.GreaterThanOrEqualTo(without.Moisture - 1e-6f),
                        "a river dried the ground beside it");
                }
            }

            Assert.That(touched, Is.GreaterThan(0), "no channel crossed the coastal window at all");
        }
    }
}

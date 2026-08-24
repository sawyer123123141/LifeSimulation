using System;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.World;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// How far apart two neighbouring chunks' surfaces can be where they meet.
    ///
    /// <para>Chunks at different depths band-limit their elevation differently - that is the whole
    /// point, it is what makes a split add detail rather than triangles - but it also means the two
    /// surfaces disagree along their shared edge. The disagreement is a step in the ground, and a
    /// step large enough to see is a level-of-detail seam: the thing that makes chunked terrain look
    /// chunked.</para>
    ///
    /// <para>This measures it in metres rather than leaving it to the eye, because it is the number
    /// that decides whether skirts are enough or whether the levels have to be blended.</para>
    /// </summary>
    public sealed class PlanetChunkSeamTests
    {
        private const int Seed = 42;
        private const double PlanetRadius = 500d;

        /// <summary>Metres of height per unit of elevation, matching the drawn planet.</summary>
        private const double MetresPerElevation = PlanetRadius * 0.06d;

        [Test]
        public void TheStepBetweenNeighbouringLevelsIsMeasured()
        {
            TerrainSettings settings = EnvironmentField.CreateTerrainSettings();
            PlateStructure plates = PlateStructure.Create(Seed, settings);

            double worst = 0d;
            for (int depth = 1; depth <= PlanetChunkLod.MaximumDepth; depth++)
            {
                double depthWorst = 0d;
                double total = 0d;
                for (int index = 0; index < 4000; index++)
                {
                    Direction(index, out double x, out double y, out double z);

                    double coarse = ElevationAt(plates, settings, x, y, z, depth - 1);
                    double fine = ElevationAt(plates, settings, x, y, z, depth);
                    double step = Math.Abs(fine - coarse) * MetresPerElevation;

                    if (step > depthWorst) depthWorst = step;
                    total += step;
                }

                if (depthWorst > worst) worst = depthWorst;
                TestContext.WriteLine(
                    "depth " + (depth - 1) + "->" + depth +
                    ": mean " + (total / 4000d).ToString("0.000") +
                    " m, worst " + depthWorst.ToString("0.000") +
                    " m, skirt " + PlanetChunkLod.SkirtDepth(PlanetChunkLod.EdgeAt(PlanetRadius, depth)).ToString("0.000") + " m");
            }

            // A skirt hides a crack; it does not hide a step in the silhouette. The step has to be
            // small against a creature, or the seam is a visible terrace across the ground.
            Assert.That(worst, Is.LessThan(40d), "worst-case level-of-detail step, in metres");
        }

        /// <summary>Elevation at a direction, band-limited the way a chunk at this depth would.</summary>
        private static double ElevationAt(
            PlateStructure plates, TerrainSettings settings, double x, double y, double z, int depth)
        {
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(
                IcoSphereSamples(PlanetChunkLod.DetailLevelFor(depth)));
            return PlanetTerrain.Sample(Seed, plates, x, y, z, maximumFrequency, settings).Elevation;
        }

        /// <summary>
        /// <c>IcoSphere.SamplesAroundEquator</c> without the Unity assembly, so this runs headlessly.
        /// </summary>
        private static int IcoSphereSamples(int subdivisions)
        {
            double faces = 20d * Math.Pow(4d, subdivisions);
            double faceArea = 4d * Math.PI / faces;
            double edge = Math.Sqrt(faceArea * 4d / Math.Sqrt(3d));
            return Math.Max(8, (int)(2d * Math.PI / edge));
        }

        /// <summary>A deterministic spread of directions over the sphere.</summary>
        private static void Direction(int index, out double x, out double y, out double z)
        {
            double offset = 2d / 4000d;
            double increment = Math.PI * (3d - Math.Sqrt(5d));

            y = ((index * offset) - 1d) + (offset / 2d);
            double radius = Math.Sqrt(Math.Max(0d, 1d - (y * y)));
            double angle = index * increment;
            x = Math.Cos(angle) * radius;
            z = Math.Sin(angle) * radius;
        }
    }
}

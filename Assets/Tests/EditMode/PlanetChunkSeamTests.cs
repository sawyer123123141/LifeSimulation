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

        /// <summary>
        /// How far the arena patch and the backdrop chunk under it can disagree.
        ///
        /// <para>The patch is 193 samples across 50 units - about 12,000 around the equator - and the
        /// deepest chunk is about 5,300, so the patch carries octaves the backdrop does not. Where the
        /// patch is lower than the chunk beneath it, the backdrop pokes through. <c>PatchLift</c> is
        /// what holds them apart, and it was set to 0.02 by eye.</para>
        /// </summary>
        [Test]
        public void ThePatchLiftClearsTheBackdropBeneathIt()
        {
            TerrainSettings settings = EnvironmentField.CreateTerrainSettings();
            PlateStructure plates = PlateStructure.Create(Seed, settings);

            double worst = 0d;
            for (int index = 0; index < 4000; index++)
            {
                Direction(index, out double x, out double y, out double z);

                double chunk = Elevation(plates, settings, x, y, z, 5342);
                double patch = Elevation(plates, settings, x, y, z, 12060);
                double step = (chunk - patch) * MetresPerElevation;
                if (step > worst) worst = step;
            }

            TestContext.WriteLine("backdrop above patch, worst: " + worst.ToString("0.000") + " m");

            // Measured at 0.000: the octave cap is reached before either band limit binds, so the two
            // surfaces are the same surface and PatchLift has nothing to clear. This guards that -
            // raise the octave cap and the patch gains detail the backdrop does not have, which is
            // exactly when PatchLift stops being enough.
            Assert.That(worst, Is.LessThan(0.02d), "must stay under ArenaProjection.PatchLift");
        }

        /// <summary>Elevation at a direction, band-limited the way a chunk at this depth would.</summary>
        private static double ElevationAt(
            PlateStructure plates, TerrainSettings settings, double x, double y, double z, int depth)
        {
            return Elevation(plates, settings, x, y, z, IcoSphereSamples(PlanetChunkLod.DetailLevelFor(depth)));
        }

        /// <summary>Elevation sampled at a stated resolution, in samples around the equator.</summary>
        private static double Elevation(
            PlateStructure plates, TerrainSettings settings, double x, double y, double z, int samples)
        {
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(samples);
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

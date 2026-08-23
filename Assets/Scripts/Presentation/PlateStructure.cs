using System;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Presentation
{
    public enum BoundaryKind
    {
        /// <summary>Plates sliding past each other. Fault line, minimal relief.</summary>
        Transform = 0,

        /// <summary>Continental collision. High interior ranges.</summary>
        ContinentalCollision = 1,

        /// <summary>Oceanic under continental. Coastal range with an offshore trench.</summary>
        Subduction = 2,

        /// <summary>Oceanic under oceanic. Curved island arc.</summary>
        IslandArc = 3,

        /// <summary>Plates separating. Rift valley on land, mid-ocean ridge at sea.</summary>
        Divergent = 4,
    }

    public readonly struct PlateSample
    {
        public PlateSample(bool continental, float baseElevation, float boundaryDistance, BoundaryKind boundary, float intensity, bool onOceanicSide)
        {
            Continental = continental;
            BaseElevation = baseElevation;
            BoundaryDistance = boundaryDistance;
            Boundary = boundary;
            Intensity = intensity;
            OnOceanicSide = onOceanicSide;
        }

        /// <summary>Type of the plate this point sits on.</summary>
        public bool Continental { get; }

        /// <summary>Plate's own elevation offset, before any boundary contribution.</summary>
        public float BaseElevation { get; }

        /// <summary>Angular distance to the nearest plate boundary, in radians.</summary>
        public float BoundaryDistance { get; }

        public BoundaryKind Boundary { get; }

        /// <summary>0..1 from the magnitude of relative motion across that boundary.</summary>
        public float Intensity { get; }

        /// <summary>At a subduction boundary, whether this point is on the oceanic (trench) side.</summary>
        public bool OnOceanicSide { get; }
    }

    /// <summary>
    /// Tectonic plates on a sphere: T0 of <c>docs/superpowers/specs/2026-08-14-world-generation-design.md</c>.
    ///
    /// <para><b>Why this exists.</b> That spec's core principle is that <i>noise cannot produce
    /// continents</i> - every feature in a noise field is independent of every other, so nothing
    /// explains anything else and the result reads as splatter however it is tuned. Structure has to
    /// come from process. Mountain ranges lie along plate boundaries; island arcs curve because
    /// subduction curves; trenches sit offshore of coastal ranges because one plate goes under the
    /// other. Those relationships are what make terrain legible, and no amount of octave tuning
    /// produces them.</para>
    ///
    /// <para>Plate seeds are placed by Fibonacci spiral - a low-discrepancy distribution, so plates
    /// are evenly sized without being a visible lattice - then rotated by a seeded orientation. Cells
    /// are spherical Voronoi around those seeds. Every per-plate property is derived from a
    /// deterministic hash of its seed direction, so the whole structure is a pure function of the
    /// world seed.</para>
    ///
    /// <para><b>Presentation only</b>, like <see cref="PlanetTerrain"/>: a prototype of T0 that costs
    /// nothing to iterate on because no hash depends on it.</para>
    /// </summary>
    public sealed class PlateStructure
    {
        private const double GoldenAngle = 2.39996322972865332d;

        private readonly double[] _seedX;
        private readonly double[] _seedY;
        private readonly double[] _seedZ;
        private readonly double[] _driftX;
        private readonly double[] _driftY;
        private readonly double[] _driftZ;
        private readonly bool[] _continental;
        private readonly float[] _baseElevation;

        public PlateStructure(int worldSeed, int plateCount = 24, double continentalFraction = 0.42d)
        {
            if (plateCount < 4) throw new ArgumentOutOfRangeException(nameof(plateCount));

            Count = plateCount;
            _seedX = new double[plateCount];
            _seedY = new double[plateCount];
            _seedZ = new double[plateCount];
            _driftX = new double[plateCount];
            _driftY = new double[plateCount];
            _driftZ = new double[plateCount];
            _continental = new bool[plateCount];
            _baseElevation = new float[plateCount];

            // A seeded rotation, so two worlds do not share a plate layout merely because they share
            // the Fibonacci construction.
            double yaw = EnvironmentNoise.ValueNoise(worldSeed, 700, 0.5d, 0.5d, 0.5d) * Math.PI * 2d;
            double tilt = (EnvironmentNoise.ValueNoise(worldSeed, 701, 1.5d, 0.5d, 0.5d) - 0.5d) * Math.PI;

            for (int index = 0; index < plateCount; index++)
            {
                // Fibonacci sphere: even coverage without the pole clustering of a lat/lon grid.
                double y = 1d - (2d * (index + 0.5d) / plateCount);
                double radius = Math.Sqrt(Math.Max(0d, 1d - (y * y)));
                double theta = GoldenAngle * index;
                double x = Math.Cos(theta) * radius;
                double z = Math.Sin(theta) * radius;

                Rotate(ref x, ref y, ref z, yaw, tilt);
                _seedX[index] = x;
                _seedY[index] = y;
                _seedZ[index] = z;

                // Per-plate properties from a hash of the seed direction, so they travel with the
                // plate rather than with its index.
                double typeRoll = EnvironmentNoise.ValueNoise(worldSeed, 710, x * 7.3d, y * 7.3d, z * 7.3d);
                bool continental = typeRoll < continentalFraction;
                _continental[index] = continental;

                double heightRoll = EnvironmentNoise.ValueNoise(worldSeed, 720, x * 5.1d, y * 5.1d, z * 5.1d);
                _baseElevation[index] = continental
                    ? (float)(0.52d + (0.16d * heightRoll))
                    : (float)(0.12d + (0.14d * heightRoll));

                // Drift: a unit tangent at the plate centroid, so motion is along the surface.
                double driftAngle = EnvironmentNoise.ValueNoise(worldSeed, 730, x * 3.7d, y * 3.7d, z * 3.7d) * Math.PI * 2d;
                double rate = 0.35d + (0.65d * EnvironmentNoise.ValueNoise(worldSeed, 740, x * 2.9d, y * 2.9d, z * 2.9d));
                TangentBasis(x, y, z, out double ux, out double uy, out double uz, out double vx, out double vy, out double vz);
                double cos = Math.Cos(driftAngle) * rate;
                double sin = Math.Sin(driftAngle) * rate;
                _driftX[index] = (ux * cos) + (vx * sin);
                _driftY[index] = (uy * cos) + (vy * sin);
                _driftZ[index] = (uz * cos) + (vz * sin);
            }
        }

        public int Count { get; }

        /// <summary>
        /// The plate containing a direction, its distance to the nearest boundary, and that
        /// boundary's kind and intensity. This is the only interface the field layer consumes.
        /// </summary>
        public PlateSample Sample(double dx, double dy, double dz)
        {
            int nearest = -1;
            int second = -1;
            double nearestDot = -2d;
            double secondDot = -2d;

            for (int index = 0; index < Count; index++)
            {
                double dot = (dx * _seedX[index]) + (dy * _seedY[index]) + (dz * _seedZ[index]);
                if (dot > nearestDot)
                {
                    secondDot = nearestDot;
                    second = nearest;
                    nearestDot = dot;
                    nearest = index;
                }
                else if (dot > secondDot)
                {
                    secondDot = dot;
                    second = index;
                }
            }

            // Half the gap in angular distance to the two nearest seeds: zero on the Voronoi edge,
            // growing toward each cell's interior.
            double angleNearest = Math.Acos(Math.Max(-1d, Math.Min(1d, nearestDot)));
            double angleSecond = Math.Acos(Math.Max(-1d, Math.Min(1d, secondDot)));
            double boundaryDistance = (angleSecond - angleNearest) * 0.5d;

            // Relative motion across the boundary, resolved along the line joining the two plates.
            double toOtherX = _seedX[second] - _seedX[nearest];
            double toOtherY = _seedY[second] - _seedY[nearest];
            double toOtherZ = _seedZ[second] - _seedZ[nearest];
            Normalize(ref toOtherX, ref toOtherY, ref toOtherZ);

            double relativeX = _driftX[nearest] - _driftX[second];
            double relativeY = _driftY[nearest] - _driftY[second];
            double relativeZ = _driftZ[nearest] - _driftZ[second];

            double closing = (relativeX * toOtherX) + (relativeY * toOtherY) + (relativeZ * toOtherZ);
            double shearX = relativeX - (closing * toOtherX);
            double shearY = relativeY - (closing * toOtherY);
            double shearZ = relativeZ - (closing * toOtherZ);
            double shear = Math.Sqrt((shearX * shearX) + (shearY * shearY) + (shearZ * shearZ));

            bool nearestContinental = _continental[nearest];
            bool otherContinental = _continental[second];

            BoundaryKind kind;
            if (Math.Abs(closing) <= shear)
            {
                // Sliding past rather than colliding or separating.
                kind = BoundaryKind.Transform;
            }
            else if (closing > 0d)
            {
                if (nearestContinental && otherContinental) kind = BoundaryKind.ContinentalCollision;
                else if (nearestContinental || otherContinental) kind = BoundaryKind.Subduction;
                else kind = BoundaryKind.IslandArc;
            }
            else
            {
                kind = BoundaryKind.Divergent;
            }

            double intensity = Math.Min(1d, Math.Sqrt((closing * closing) + (shear * shear)) / 1.4d);

            // At a subduction margin the trench sits on the oceanic side and the range on the
            // continental side, which is why coastal ranges have deep water just offshore.
            bool onOceanicSide = kind == BoundaryKind.Subduction && !nearestContinental;

            return new PlateSample(
                nearestContinental,
                _baseElevation[nearest],
                (float)boundaryDistance,
                kind,
                (float)intensity,
                onOceanicSide);
        }

        private static void TangentBasis(
            double x, double y, double z,
            out double ux, out double uy, out double uz,
            out double vx, out double vy, out double vz)
        {
            // Any vector not parallel to the normal works; picking by the smallest component keeps
            // the cross product well conditioned.
            double ax = Math.Abs(x) < 0.9d ? 1d : 0d;
            double ay = Math.Abs(x) < 0.9d ? 0d : 1d;
            double az = 0d;

            ux = (ay * z) - (az * y);
            uy = (az * x) - (ax * z);
            uz = (ax * y) - (ay * x);
            Normalize(ref ux, ref uy, ref uz);

            vx = (y * uz) - (z * uy);
            vy = (z * ux) - (x * uz);
            vz = (x * uy) - (y * ux);
            Normalize(ref vx, ref vy, ref vz);
        }

        private static void Normalize(ref double x, ref double y, ref double z)
        {
            double length = Math.Sqrt((x * x) + (y * y) + (z * z));
            if (length <= 1e-9d) { x = 1d; y = 0d; z = 0d; return; }
            x /= length;
            y /= length;
            z /= length;
        }

        private static void Rotate(ref double x, ref double y, ref double z, double yaw, double tilt)
        {
            double cosYaw = Math.Cos(yaw);
            double sinYaw = Math.Sin(yaw);
            double x1 = (x * cosYaw) - (z * sinYaw);
            double z1 = (x * sinYaw) + (z * cosYaw);

            double cosTilt = Math.Cos(tilt);
            double sinTilt = Math.Sin(tilt);
            double y2 = (y * cosTilt) - (z1 * sinTilt);
            double z2 = (y * sinTilt) + (z1 * cosTilt);

            x = x1;
            y = y2;
            z = z2;
        }
    }
}

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

    /// <summary>
    /// One neighbouring plate, and the margin between it and the plate a point sits on.
    ///
    /// <para>Kind and intensity are properties of the <b>pair</b>, so they change the instant the
    /// neighbour does - which is why a point carries two of these. See
    /// <see cref="PlateSample.AlternateWeight"/>.</para>
    /// </summary>
    public readonly struct PlateNeighbour
    {
        public PlateNeighbour(
            bool continental, float baseElevation, float boundaryDistance,
            BoundaryKind boundary, float intensity, bool onOceanicSide, float blend)
        {
            Continental = continental;
            BaseElevation = baseElevation;
            BoundaryDistance = boundaryDistance;
            Boundary = boundary;
            Intensity = intensity;
            OnOceanicSide = onOceanicSide;
            Blend = blend;
        }

        /// <summary>Whether the neighbouring plate carries land.</summary>
        public bool Continental { get; }

        /// <summary>The neighbour's own elevation offset.</summary>
        public float BaseElevation { get; }

        /// <summary>Angular distance to the margin with this neighbour.</summary>
        public float BoundaryDistance { get; }

        /// <summary>What kind of margin the two plates make.</summary>
        public BoundaryKind Boundary { get; }

        /// <summary>How hard they are meeting, from their relative drift.</summary>
        public float Intensity { get; }

        /// <summary>At a subduction margin, whether this point is on the plate going under.</summary>
        public bool OnOceanicSide { get; }

        /// <summary>1 deep inside the plate, 0.5 on the margin: how much is this plate's own.</summary>
        public float Blend { get; }
    }

    /// <summary>
    /// The plate a point sits on, and the two neighbours whose margins it is closest to.
    ///
    /// <para><b>Why two neighbours.</b> Boundary kind and intensity belong to a pair of plates, so
    /// they change discontinuously the moment a different plate becomes second-nearest - and that
    /// happens along a line running through a cell's interior, far from any seam, where the seam
    /// blend has already saturated and smooths nothing. Measured at latitude 48.7 degrees: elevation
    /// stepped 0.277 to 0.528 between samples 1.04 metres apart - a <b>7.24 grade, an 82 degree
    /// wall</b> - with identical shelf and seam distance on both sides and the margin reading
    /// Divergent on one and ContinentalCollision on the other.</para>
    ///
    /// <para>Carrying both candidates and crossfading between them removes it: exactly where the two
    /// change places their distances are equal, so both sides of the swap evaluate the same
    /// half-and-half mixture. Same lesson as the shelf blend, one layer down - <b>any piecewise
    /// constant lookup is a cliff in whatever it feeds</b>, and ranking is a lookup.</para>
    /// </summary>
    public readonly struct PlateSample
    {
        public PlateSample(
            bool continental, float baseElevation,
            PlateNeighbour primary, PlateNeighbour alternate, float alternateWeight)
        {
            Continental = continental;
            BaseElevation = baseElevation;
            Primary = primary;
            Alternate = alternate;
            AlternateWeight = alternateWeight;
        }

        /// <summary>Type of the plate this point sits on.</summary>
        public bool Continental { get; }

        /// <summary>Plate's own elevation offset, before any boundary contribution.</summary>
        public float BaseElevation { get; }

        /// <summary>The nearest margin.</summary>
        public PlateNeighbour Primary { get; }

        /// <summary>The next nearest, which is the one about to take over.</summary>
        public PlateNeighbour Alternate { get; }

        /// <summary>
        /// How much of <see cref="Alternate"/> to mix in: 0 where the ranking is unambiguous, rising
        /// to 0.5 exactly where the two swap. It never exceeds 0.5, because past the swap the two
        /// exchange names and the same mixture is reached from the other side.
        /// </summary>
        public float AlternateWeight { get; }

        // Kept so existing callers read the nearest margin without knowing about the crossfade.
        public float BoundaryDistance { get { return Primary.BoundaryDistance; } }
        public BoundaryKind Boundary { get { return Primary.Boundary; } }
        public float Intensity { get { return Primary.Intensity; } }
        public bool OnOceanicSide { get { return Primary.OnOceanicSide; } }
        public bool NeighbourContinental { get { return Primary.Continental; } }
        public float NeighbourBaseElevation { get { return Primary.BaseElevation; } }
        public float Blend { get { return Primary.Blend; } }
    }

    public sealed class PlateStructure
    {
        private const double GoldenAngle = 2.39996322972865332d;

        /// <summary>
        /// Half-width of the seam over which two plates blend, in radians. Wide enough that the step
        /// becomes a slope the mesh can represent, narrow enough that plate interiors keep their own
        /// character. One radian is 500 metres, so this is 80 metres.
        /// </summary>
        private const double BlendWidth = 0.16d;

        /// <summary>
        /// How far either side of a rank swap the second and third neighbours crossfade, in radians -
        /// 60 metres. Sized against the height the swap can move: the worst measured case stepped
        /// 7.5 metres, and spreading that over 60 metres of ground is a 0.13 grade, comfortably under
        /// the slope the mesh can draw. Too narrow and the wall becomes a steep ramp instead of
        /// disappearing.
        /// </summary>
        private const double SwapTransition = 0.12d;

        private readonly double[] _seedX;
        private readonly double[] _seedY;
        private readonly double[] _seedZ;
        private readonly double[] _driftX;
        private readonly double[] _driftY;
        private readonly double[] _driftZ;
        private readonly bool[] _continental;
        private readonly float[] _baseElevation;

        /// <summary>
        /// <paramref name="plateCount"/> and <paramref name="continentalFraction"/> are <b>recipe
        /// parameters</b>, not constants: the world-generation design defines a world by which
        /// processes run and with what values, so that different worlds can differ in kind rather
        /// than only in seed. They are arguments here so a recipe can set them; nothing about the
        /// structure is hardcoded beyond the process itself.
        /// </summary>
        /// <summary>
        /// The plate structure the active settings describe. Use this rather than the constructor
        /// wherever the view is meant to follow the tuning panel.
        /// </summary>
        public static PlateStructure CreateActive(int worldSeed)
        {
            TerrainSettings settings = PlanetTerrain.Active;
            return new PlateStructure(worldSeed, settings.PlateCount, settings.ContinentalFraction);
        }

        public PlateStructure(int worldSeed, int plateCount = 20, double continentalFraction = 0.42d)
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
        /// Latitude and longitude of a continental plate worth looking at, preferring one whose
        /// neighbours give it a convergent margin so there is relief as well as land.
        ///
        /// <para><b>Why this is needed.</b> The flat views sample a window about 0.8 radians across,
        /// and a plate is about that size, so a flat view shows roughly one plate. Centred at
        /// coordinate zero it showed whichever plate happens to sit there - an oceanic one - so 400
        /// units of open sea was rendered while the planet as a whole was 30% land. A global land
        /// fraction says nothing about where the camera is parked.</para>
        /// </summary>
        /// <summary>
        /// Latitude and longitude of a <b>coastline</b>: the midpoint between a continental plate and
        /// an adjacent oceanic one, which is where land meets sea by construction.
        ///
        /// <para>Centring the flat views on a plate centre swung them from 99.9% ocean to 100% land -
        /// measured land fraction 1.000 with a single biome across the close view, a flat green
        /// plateau. A coast puts water, beach, lowland and whatever the margin raises in the same
        /// frame, which is what makes a view worth judging.</para>
        ///
        /// <para>Prefers a margin that subducts, since that is the landform with the strongest
        /// measured lift (0.346) and therefore supplies relief as well as shoreline.</para>
        /// </summary>
        /// <summary>Whether plate <paramref name="index"/> carries land.</summary>
        public bool IsContinental(int index)
        {
            return _continental[index];
        }

        /// <summary>
        /// Where plate <paramref name="index"/> is centred. Lets a viewer hop from continent to
        /// continent, which is the only practical way to see biomes that exist somewhere other than
        /// wherever the default view happens to sit.
        /// </summary>
        public void GetSeedLatLon(int index, out double latitude, out double longitude)
        {
            latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, _seedY[index])));
            longitude = Math.Atan2(_seedX[index], _seedZ[index]);
        }

        public void GetCoastalCentre(out double latitude, out double longitude)
        {
            int bestContinental = -1;
            int bestOceanic = -1;
            double bestScore = double.NegativeInfinity;

            for (int index = 0; index < Count; index++)
            {
                if (!_continental[index]) continue;
                for (int other = 0; other < Count; other++)
                {
                    if (_continental[other]) continue;

                    double dot = (_seedX[index] * _seedX[other]) + (_seedY[index] * _seedY[other]) + (_seedZ[index] * _seedZ[other]);
                    if (dot < 0.2d) continue; // not neighbours

                    // Adjacency first, then how much relief the margin carries.
                    double score = dot + _baseElevation[index];
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestContinental = index;
                        bestOceanic = other;
                    }
                }
            }

            if (bestContinental < 0 || bestOceanic < 0)
            {
                GetContinentalCentre(out latitude, out longitude);
                return;
            }

            // Midpoint of the two seeds lies on the Voronoi edge between them - the coast.
            double x = (_seedX[bestContinental] + _seedX[bestOceanic]) * 0.5d;
            double y = (_seedY[bestContinental] + _seedY[bestOceanic]) * 0.5d;
            double z = (_seedZ[bestContinental] + _seedZ[bestOceanic]) * 0.5d;
            Normalize(ref x, ref y, ref z);

            latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, y)));
            longitude = Math.Atan2(x, z);
        }

        public void GetContinentalCentre(out double latitude, out double longitude)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int index = 0; index < Count; index++)
            {
                if (!_continental[index]) continue;

                // Prefer a plate with an oceanic neighbour: that margin subducts, which is the
                // landform with the strongest measured lift (0.346) and gives a coastline too.
                double score = _baseElevation[index];
                for (int other = 0; other < Count; other++)
                {
                    if (other == index || _continental[other]) continue;
                    double dot = (_seedX[index] * _seedX[other]) + (_seedY[index] * _seedY[other]) + (_seedZ[index] * _seedZ[other]);
                    if (dot > 0.5d) score += 0.5d;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            if (best < 0) { latitude = 0d; longitude = 0d; return; }

            latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, _seedY[best])));
            longitude = Math.Atan2(_seedX[best], _seedZ[best]);
        }

        /// <summary>
        /// The plate containing a direction, its distance to the nearest boundary, and that
        /// boundary's kind and intensity. This is the only interface the field layer consumes.
        /// </summary>
        public PlateSample Sample(double dx, double dy, double dz)
        {
            int nearest = -1;
            int second = -1;
            int third = -1;
            double nearestDot = -2d;
            double secondDot = -2d;
            double thirdDot = -2d;

            for (int index = 0; index < Count; index++)
            {
                double dot = (dx * _seedX[index]) + (dy * _seedY[index]) + (dz * _seedZ[index]);
                if (dot > nearestDot)
                {
                    thirdDot = secondDot;
                    third = second;
                    secondDot = nearestDot;
                    second = nearest;
                    nearestDot = dot;
                    nearest = index;
                }
                else if (dot > secondDot)
                {
                    thirdDot = secondDot;
                    third = second;
                    secondDot = dot;
                    second = index;
                }
                else if (dot > thirdDot)
                {
                    thirdDot = dot;
                    third = index;
                }
            }

            if (third < 0) third = second;

            double angleNearest = Angle(nearestDot);
            double angleSecond = Angle(secondDot);
            double angleThird = Angle(thirdDot);

            // How close the second and third plates are to changing places. At the moment they do,
            // their angles are equal, the weight is 0.5, and both sides of the swap evaluate the
            // same mixture - which is what makes the field continuous across it.
            double gap = Math.Max(0d, angleThird - angleSecond);
            double alternateWeight = 0.5d * (1d - Smooth01(Math.Min(1d, gap / SwapTransition)));

            return new PlateSample(
                _continental[nearest],
                _baseElevation[nearest],
                DescribeMargin(nearest, second, angleNearest, angleSecond),
                DescribeMargin(nearest, third, angleNearest, angleThird),
                (float)alternateWeight);
        }

        /// <summary>
        /// The margin between the plate a point is on and one neighbour: what kind it is, how hard
        /// they are meeting, and how far away it is.
        /// </summary>
        private PlateNeighbour DescribeMargin(int nearest, int other, double angleNearest, double angleOther)
        {
            // Half the gap in angular distance to the two seeds: zero on the Voronoi edge, growing
            // toward each cell's interior.
            double boundaryDistance = (angleOther - angleNearest) * 0.5d;

            // Relative motion across the boundary, resolved along the line joining the two plates.
            double toOtherX = _seedX[other] - _seedX[nearest];
            double toOtherY = _seedY[other] - _seedY[nearest];
            double toOtherZ = _seedZ[other] - _seedZ[nearest];
            Normalize(ref toOtherX, ref toOtherY, ref toOtherZ);

            double relativeX = _driftX[nearest] - _driftX[other];
            double relativeY = _driftY[nearest] - _driftY[other];
            double relativeZ = _driftZ[nearest] - _driftZ[other];

            double closing = (relativeX * toOtherX) + (relativeY * toOtherY) + (relativeZ * toOtherZ);
            double shearX = relativeX - (closing * toOtherX);
            double shearY = relativeY - (closing * toOtherY);
            double shearZ = relativeZ - (closing * toOtherZ);
            double shear = Math.Sqrt((shearX * shearX) + (shearY * shearY) + (shearZ * shearZ));

            bool nearestContinental = _continental[nearest];
            bool otherContinental = _continental[other];

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

            double blend = 0.5d + (0.5d * Smooth01(Math.Min(1d, boundaryDistance / BlendWidth)));

            return new PlateNeighbour(
                otherContinental,
                _baseElevation[other],
                (float)boundaryDistance,
                kind,
                (float)intensity,
                onOceanicSide,
                (float)blend);
        }

        private static double Angle(double dot)
        {
            return Math.Acos(Math.Max(-1d, Math.Min(1d, dot)));
        }

        private static double Smooth01(double t)
        {
            return t * t * (3d - (2d * t));
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

using System;
using System.Collections.Generic;

namespace LifeSimulation.Simulation.World
{
    /// <summary>
    /// Rivers walked downhill across the finished terrain, once per world.
    ///
    /// <para><b>This is the honest cheap version.</b> The terrain generator is a pure function of one
    /// direction - it reads nothing about a point's neighbours - and where water flows depends
    /// entirely on the ground uphill of it. That is a computation over the surface, so it cannot be
    /// a coefficient. Here the surface is walked once, the paths are recorded, and sampling becomes
    /// "elevation, minus the channel if this point is near a recorded path". No valleys form around
    /// the rivers, because nothing erodes: they follow the terrain without changing it elsewhere, and
    /// they will read as painted on, because they are. Flow accumulation is the real thing and it
    /// wants the region system that adaptive detail also wants - see
    /// docs/terrain-caves-and-rivers.md.</para>
    ///
    /// <para>The walk deliberately reads a <b>coarse</b> version of the field. Rivers follow the
    /// shape of a continent, not the shape of a boulder, and a walk that sees every micro bump stops
    /// in the first one that happens to be a hollow. The channel is then carved into the fine field,
    /// so a river can sit slightly off the finest valley floor; at that point the alternative is
    /// erosion, which is R2.</para>
    /// </summary>
    public sealed class RiverNetwork
    {
        /// <summary>
        /// Detail limit for the walk. Continental shape only: enough to see which way a mountain
        /// range falls, not enough to see a hollow a metre across and mistake it for a lake.
        /// </summary>
        private const double WalkFrequency = 24d;

        /// <summary>Angular step, in radians. 1 radian is 500 metres, so this is about 1.25 m.</summary>
        private const double StepAngle = 0.0025d;

        /// <summary>Steps before a river is abandoned. At the step above, about 500 m of travel.</summary>
        private const int MaximumSteps = 400;

        /// <summary>Directions tried at each step. Eight is enough to keep a path from staircasing.</summary>
        private const int Directions = 8;

        /// <summary>Candidate source points scattered over the sphere before the highest are kept.</summary>
        private const int CandidateCount = 2048;

        /// <summary>
        /// How far a channel reaches, in radians - about 2.5 m either side at the mouth, narrower
        /// upstream. Wide enough to survive the arena mesh's quarter-metre sampling.
        /// </summary>
        private const double MaximumHalfWidth = 0.005d;

        private const double MinimumHalfWidth = 0.0018d;

        /// <summary>
        /// Depth of the channel in elevation units. One elevation unit is about 30 m, so this is a
        /// bank about 1.7 m high - a stream, not a canyon. Deeper reads as a trench cut through hills
        /// rather than as water finding its way between them; shallower and the banks stop reading as
        /// banks at the zoom a creature is one unit tall.
        /// </summary>
        private const double ChannelDepth = 0.055d;

        /// <summary>How much wetter the ground beside a river is, at the channel itself.</summary>
        private const double MoistureGift = 0.30d;

        /// <summary>Source elevation floor. Rivers start on high ground or they are puddles.</summary>
        private const double SourceElevation = 0.30d;

        private readonly List<RiverSegment> _segments = new List<RiverSegment>();
        private readonly Dictionary<long, List<int>> _index = new Dictionary<long, List<int>>();
        private readonly double _cellAngle = MaximumHalfWidth;

        /// <summary>
        /// One step of a river, stored as the <b>segment</b> from this point to the next.
        ///
        /// <para>Measuring to points alone leaves a beaded channel: between two path points a metre
        /// apart the nearest-point distance rises, so the channel floor scallops - measured at
        /// proximity 0.999 / 0.848 / 0.999 along one straight reach. A river bed that ripples at the
        /// sampling frequency of its own path is worse than no river.</para>
        /// </summary>
        private readonly struct RiverSegment
        {
            public RiverSegment(Direction from, Direction to, double halfWidth)
            {
                FromX = from.X;
                FromY = from.Y;
                FromZ = from.Z;
                ToX = to.X;
                ToY = to.Y;
                ToZ = to.Z;
                HalfWidth = halfWidth;
            }

            public double FromX { get; }
            public double FromY { get; }
            public double FromZ { get; }
            public double ToX { get; }
            public double ToY { get; }
            public double ToZ { get; }

            /// <summary>Reach of this segment, widening downstream.</summary>
            public double HalfWidth { get; }
        }

        private RiverNetwork()
        {
        }

        /// <summary>Number of recorded path segments. Diagnostic; the probe reports it.</summary>
        public int PointCount { get { return _segments.Count; } }

        /// <summary>Number of rivers that were walked and kept.</summary>
        public int RiverCount { get; private set; }

        /// <summary>
        /// Walk <paramref name="riverCount"/> rivers down the terrain of one world.
        ///
        /// <para>Deterministic in the seed and settings alone, like everything else in this folder:
        /// the candidate sources are a fixed spiral over the sphere, and the walk is steepest descent
        /// with no random component. Two worlds with the same seed have the same rivers.</para>
        /// </summary>
        public static RiverNetwork Create(int seed, PlateStructure plates, TerrainSettings settings, int riverCount = 48)
        {
            if (plates == null) throw new ArgumentNullException(nameof(plates));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var network = new RiverNetwork();
            foreach (var source in FindSources(seed, plates, settings, riverCount))
            {
                network.Walk(seed, plates, settings, source);
            }

            return network;
        }

        /// <summary>
        /// How much of a channel is at this point: 1 in midstream, 0 beyond the bank.
        ///
        /// <para>Smoothstepped rather than linear, so the channel meets the surrounding ground with
        /// matching slope. A linear falloff leaves a crease along both banks that reads as a fold in
        /// the mesh at exactly the zoom where the river is meant to look best.</para>
        /// </summary>
        public double Proximity(double dx, double dy, double dz)
        {
            if (_segments.Count == 0) return 0d;

            Cell(dx, dy, dz, out int latitudeCell, out int longitudeCell);
            double best = 0d;
            for (int latitudeOffset = -1; latitudeOffset <= 1; latitudeOffset++)
            {
                for (int longitudeOffset = -1; longitudeOffset <= 1; longitudeOffset++)
                {
                    if (!_index.TryGetValue(Key(latitudeCell + latitudeOffset, longitudeCell + longitudeOffset), out List<int> bucket))
                    {
                        continue;
                    }

                    for (int entry = 0; entry < bucket.Count; entry++)
                    {
                        RiverSegment segment = _segments[bucket[entry]];
                        double angle = AngleToSegment(segment, dx, dy, dz);
                        if (angle >= segment.HalfWidth) continue;

                        double near = Smooth01(1d - (angle / segment.HalfWidth));
                        if (near > best) best = near;
                    }
                }
            }

            return best;
        }

        /// <summary>Depth to remove from elevation at a point, given its channel proximity.</summary>
        public static double Carve(double proximity)
        {
            return ChannelDepth * proximity;
        }

        /// <summary>Moisture to add beside a river, given its channel proximity.</summary>
        public static double Wetting(double proximity)
        {
            return MoistureGift * proximity;
        }

        // ---- construction -------------------------------------------------------------------

        /// <summary>
        /// The highest candidate points, spread out. Taking the top N by elevation alone puts every
        /// river on one mountain range; requiring a minimum separation scatters them over the
        /// continents the way a planet's rivers actually are.
        /// </summary>
        private static List<Direction> FindSources(int seed, PlateStructure plates, TerrainSettings settings, int riverCount)
        {
            var candidates = new List<(Direction Direction, double Elevation)>();
            double golden = Math.PI * (3d - Math.Sqrt(5d));
            for (int index = 0; index < CandidateCount; index++)
            {
                double y = 1d - (2d * (index + 0.5d) / CandidateCount);
                double radius = Math.Sqrt(Math.Max(0d, 1d - (y * y)));
                double theta = golden * index;
                var direction = new Direction(radius * Math.Cos(theta), y, radius * Math.Sin(theta));
                double elevation = Elevation(seed, plates, settings, direction);
                if (elevation < SourceElevation) continue;

                candidates.Add((direction, elevation));
            }

            candidates.Sort((left, right) => right.Elevation.CompareTo(left.Elevation));

            var sources = new List<Direction>();
            double separation = 0.12d;
            foreach ((Direction direction, double _) in candidates)
            {
                if (sources.Count >= riverCount) break;

                bool tooClose = false;
                for (int index = 0; index < sources.Count; index++)
                {
                    if (Angle(sources[index], direction) < separation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) sources.Add(direction);
            }

            return sources;
        }

        /// <summary>
        /// Steepest descent from one source until the water reaches the sea, stalls in a hollow, or
        /// runs out of patience.
        ///
        /// <para>A stalled river is discarded rather than drained: an inland lake needs a water body
        /// with a surface height, which is a different feature, and half of one drawn as a river that
        /// stops in a field is worse than no river.</para>
        /// </summary>
        private void Walk(int seed, PlateStructure plates, TerrainSettings settings, Direction source)
        {
            var path = new List<Direction>();
            Direction current = source;
            double currentElevation = Elevation(seed, plates, settings, current);
            bool reachedSea = false;

            for (int step = 0; step < MaximumSteps; step++)
            {
                path.Add(current);
                if (currentElevation <= 0d)
                {
                    reachedSea = true;
                    break;
                }

                Tangents(current, out Direction east, out Direction north);
                double bestElevation = currentElevation;
                Direction best = current;
                bool descended = false;
                for (int index = 0; index < Directions; index++)
                {
                    double angle = 2d * Math.PI * index / Directions;
                    Direction candidate = Step(current, east, north, Math.Cos(angle), Math.Sin(angle));
                    double elevation = Elevation(seed, plates, settings, candidate);
                    if (elevation >= bestElevation) continue;

                    bestElevation = elevation;
                    best = candidate;
                    descended = true;
                }

                if (!descended) break;

                current = best;
                currentElevation = bestElevation;
            }

            if (!reachedSea || path.Count < 8) return;

            RiverCount++;
            for (int index = 0; index < path.Count - 1; index++)
            {
                // Widen downstream. A river the same width at its source and its mouth reads as a
                // drawn line; widening is the cheapest cue that water is accumulating.
                double along = index / (double)(path.Count - 1);
                double halfWidth = MinimumHalfWidth + ((MaximumHalfWidth - MinimumHalfWidth) * Math.Sqrt(along));
                Add(path[index], path[index + 1], halfWidth);
            }
        }

        /// <summary>
        /// Record one segment, filed under the cells at both of its ends.
        ///
        /// <para>Filing only the start would let a segment that crosses a cell boundary be missed by
        /// a query in the cell it ends in. A segment is about one step long and a cell about one
        /// channel width, so both ends plus the 3x3 search covers it.</para>
        /// </summary>
        private void Add(Direction from, Direction to, double halfWidth)
        {
            int segmentIndex = _segments.Count;
            _segments.Add(new RiverSegment(from, to, halfWidth));

            File(from, segmentIndex);
            File(to, segmentIndex);
        }

        private void File(Direction direction, int segmentIndex)
        {
            Cell(direction.X, direction.Y, direction.Z, out int latitudeCell, out int longitudeCell);
            long key = Key(latitudeCell, longitudeCell);
            if (!_index.TryGetValue(key, out List<int> bucket))
            {
                bucket = new List<int>();
                _index[key] = bucket;
            }

            if (bucket.Count > 0 && bucket[bucket.Count - 1] == segmentIndex) return;

            bucket.Add(segmentIndex);
        }

        /// <summary>
        /// Angle from a direction to the nearest point of a segment.
        ///
        /// <para>Solved as point-to-line-segment on the chord, then re-normalised. Over a step of
        /// 0.0025 radians the chord and the arc differ by about one part in a million, four orders of
        /// magnitude below the channel width this feeds.</para>
        /// </summary>
        private static double AngleToSegment(RiverSegment segment, double dx, double dy, double dz)
        {
            double abx = segment.ToX - segment.FromX;
            double aby = segment.ToY - segment.FromY;
            double abz = segment.ToZ - segment.FromZ;
            double lengthSquared = (abx * abx) + (aby * aby) + (abz * abz);

            double t = 0d;
            if (lengthSquared > 0d)
            {
                t = (((dx - segment.FromX) * abx) + ((dy - segment.FromY) * aby) + ((dz - segment.FromZ) * abz))
                    / lengthSquared;
                if (t < 0d) t = 0d;
                else if (t > 1d) t = 1d;
            }

            var nearest = new Direction(
                segment.FromX + (t * abx),
                segment.FromY + (t * aby),
                segment.FromZ + (t * abz));

            double dot = (nearest.X * dx) + (nearest.Y * dy) + (nearest.Z * dz);
            if (dot > 1d) dot = 1d;
            else if (dot < -1d) dot = -1d;
            return Math.Acos(dot);
        }

        // ---- geometry -----------------------------------------------------------------------

        private readonly struct Direction
        {
            public Direction(double x, double y, double z)
            {
                double length = Math.Sqrt((x * x) + (y * y) + (z * z));
                if (length <= 0d) length = 1d;
                X = x / length;
                Y = y / length;
                Z = z / length;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
        }

        private static double Elevation(int seed, PlateStructure plates, TerrainSettings settings, Direction direction)
        {
            PlanetSample sample = PlanetTerrain.Sample(
                seed, plates, direction.X, direction.Y, direction.Z, WalkFrequency, settings);
            return sample.Elevation;
        }

        /// <summary>An orthonormal pair tangent to the sphere at this point.</summary>
        private static void Tangents(Direction at, out Direction east, out Direction north)
        {
            // Any vector not parallel to the point works as a seed for the cross product; the pole
            // swap keeps it well conditioned when the point itself is near the y axis.
            double upX = Math.Abs(at.Y) > 0.9d ? 1d : 0d;
            double upY = Math.Abs(at.Y) > 0.9d ? 0d : 1d;
            east = new Direction(
                (upY * at.Z) - (0d * at.Y),
                (0d * at.X) - (upX * at.Z),
                (upX * at.Y) - (upY * at.X));
            north = new Direction(
                (at.Y * east.Z) - (at.Z * east.Y),
                (at.Z * east.X) - (at.X * east.Z),
                (at.X * east.Y) - (at.Y * east.X));
        }

        private static Direction Step(Direction at, Direction east, Direction north, double alongEast, double alongNorth)
        {
            double scale = StepAngle;
            return new Direction(
                at.X + (scale * ((alongEast * east.X) + (alongNorth * north.X))),
                at.Y + (scale * ((alongEast * east.Y) + (alongNorth * north.Y))),
                at.Z + (scale * ((alongEast * east.Z) + (alongNorth * north.Z))));
        }

        private static double Angle(Direction left, Direction right)
        {
            double dot = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
            if (dot > 1d) dot = 1d;
            else if (dot < -1d) dot = -1d;
            return Math.Acos(dot);
        }

        private void Cell(double dx, double dy, double dz, out int latitudeCell, out int longitudeCell)
        {
            double latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, dy)));
            double longitude = Math.Atan2(dx, dz);
            latitudeCell = (int)Math.Floor(latitude / _cellAngle);

            // Cells shrink toward the poles if longitude is bucketed at a constant angle, so scale the
            // longitude cell by cos(latitude): a bucket stays roughly one channel width across
            // everywhere, and the neighbour search stays a 3x3.
            double cosLatitude = Math.Max(1e-6d, Math.Cos(latitude));
            longitudeCell = (int)Math.Floor(longitude * cosLatitude / _cellAngle);
        }

        private static long Key(int latitudeCell, int longitudeCell)
        {
            return ((long)latitudeCell << 32) ^ (uint)longitudeCell;
        }

        private static double Smooth01(double t)
        {
            if (t <= 0d) return 0d;
            if (t >= 1d) return 1d;
            return t * t * (3d - (2d * t));
        }
    }
}

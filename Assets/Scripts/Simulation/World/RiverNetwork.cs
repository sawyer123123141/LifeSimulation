using System;
using System.Collections.Generic;

namespace LifeSimulation.Simulation.World
{
    /// <summary>
    /// Rivers walked downhill once per world, then <b>blended into</b> the terrain rather than cut
    /// out of it.
    ///
    /// <para><b>Why the first version read as stripes.</b> It subtracted a five-metre slot from
    /// whatever the ground happened to be, so the water surface rose and fell with the hillside it
    /// crossed and the land beside it never sloped in. A channel with no valley around it is
    /// geometrically a groove scratched across a slope, which is exactly what it looked like. The
    /// literature is unanimous on the fix - Peytavie et al., <i>Procedural Riverscapes</i>, inscribe a
    /// riverbed by combining <b>compactly supported elevation modifiers</b>: a wide valley term that
    /// pulls the surrounding ground toward the river, and a narrow bed at its floor.</para>
    ///
    /// <para><b>Three properties this version has and the first did not.</b></para>
    /// <list type="number">
    /// <item><description><b>A monotonically decreasing profile.</b> Heights along the course pass
    /// through a running minimum and are smoothed, so the river never climbs. Every method in the
    /// literature enforces this "downhill guarantee"; without it the water surface is just the
    /// terrain, offset.</description></item>
    /// <item><description><b>The ground is blended toward that profile, not offset from it.</b>
    /// Terrain inside the valley is interpolated toward the river's own height, so banks slope into
    /// the water and the fine bands - local relief is about a metre, the same size as the channel -
    /// are flattened where they would otherwise cross the stream.</description></item>
    /// <item><description><b>Rivers merge.</b> A walk that reaches an existing course joins it and
    /// stops, and the course it joins widens. Isolated non-merging strands read as scratches; a
    /// branching network reads as drainage.</description></item>
    /// </list>
    ///
    /// <para>Still not erosion: terrain away from a river is untouched, so the wider landscape does
    /// not know its own drainage. That needs flow accumulation over a finite region - R2 in
    /// docs/terrain-caves-and-rivers.md - which needs the chunk system adaptive detail also needs.
    /// </para>
    /// </summary>
    public sealed class RiverNetwork
    {
        /// <summary>
        /// Detail limit for the walk. Ranges, rolling hills and the 11-per-radian detail band, but
        /// not the 55 and 150 bands: those are metre-scale, the same size as the channel, and a walk
        /// that sees them stops in the first hollow. They are flattened by the valley blend instead.
        /// </summary>
        private const double WalkFrequency = 24d;

        /// <summary>Angular step, in radians. 1 radian is 500 metres, so this is about 1.25 m.</summary>
        private const double StepAngle = 0.0025d;

        /// <summary>Steps before a river is abandoned. At the step above, about 500 m of travel.</summary>
        private const int MaximumSteps = 400;

        /// <summary>Directions sampled around each step to estimate the downhill gradient.</summary>
        private const int Directions = 8;

        /// <summary>
        /// How much of the previous heading survives each step, 0 to 1.
        ///
        /// <para>Steepest descent alone picks one of eight compass directions, so on ground that is
        /// nearly flat across the step it alternates between two of them and the path comes out as a
        /// staircase of right-angled elbows - which is exactly what the first render of the valley
        /// version showed. Carrying momentum, as particle-erosion implementations do, turns the same
        /// descent into a curve. Too much and the water runs up the far side of its own valley.</para>
        /// </summary>
        private const double Inertia = 0.55d;

        /// <summary>Positions smoothing window, in steps either side, applied after the walk.</summary>
        private const int PathSmoothing = 4;

        /// <summary>Candidate source points scattered over the sphere before the highest are kept.</summary>
        private const int CandidateCount = 4096;

        /// <summary>
        /// Minimum angle between two sources - about 50 m.
        ///
        /// <para>Set against the valley, not by taste: valleys reach 30 m across, so sources closer
        /// than this corrugate the land into parallel troughs. Rivers are then rarer than the 50-unit
        /// arena is wide, which is what <see cref="SnapToNearestMouth"/> exists to fix.</para>
        /// </summary>
        private const double SourceSeparation = 0.15d;

        /// <summary>Source elevation floor. Rivers start on high ground or they are puddles.</summary>
        private const double SourceElevation = 0.12d;

        /// <summary>
        /// Second-pass sources: closer together, lower down, and kept <b>only if they join</b> an
        /// existing course.
        ///
        /// <para>Trunks alone never meet. Spaced far enough apart not to corrugate the land, every
        /// walk runs to the sea on its own and the result is a set of parallel strands - the network
        /// had zero confluences before this pass existed. Real drainage is trunks plus tributaries,
        /// so the tributaries are generated as such rather than hoped for.</para>
        /// </summary>
        private const double TributarySeparation = 0.045d;

        private const double TributaryElevation = 0.06d;

        /// <summary>
        /// Half-width of the <b>valley</b>, in radians: about 3 m at a source, 15 m at a mouth.
        ///
        /// <para>This is the support of the blend, and it is what makes a river look like the land
        /// drains into it. Widening downstream is the cheapest stand-in for discharge, which a real
        /// implementation takes from accumulated upstream area.</para>
        /// </summary>
        private const double MinimumValleyHalfWidth = 0.003d;

        private const double MaximumValleyHalfWidth = 0.009d;

        /// <summary>Half-width of the water itself: about 1.5 m at a source, 4 m at a mouth.</summary>
        private const double MinimumBedHalfWidth = 0.0012d;

        private const double MaximumBedHalfWidth = 0.0026d;

        /// <summary>
        /// How far the bed sits below the valley floor, in elevation units - about 0.9 m.
        ///
        /// <para>Small on purpose. The valley now supplies the depth a river reads as having; the bed
        /// only has to hold water. The first version put all of it in the bed and got a trench.</para>
        /// </summary>
        public const double BedDepth = 0.03d;

        /// <summary>How much wetter the valley floor is.</summary>
        private const double MoistureGift = 0.30d;

        /// <summary>
        /// How much a course widens per river joining it, as a fraction. Four tributaries roughly
        /// double a trunk - a stand-in for discharge, and far short of the real thing.
        /// </summary>
        private const double ConfluenceWidening = 0.25d;

        /// <summary>Smoothing window applied to the height profile, in steps either side.</summary>
        private const int ProfileSmoothing = 12;

        private readonly List<RiverSegment> _segments = new List<RiverSegment>();
        private readonly List<int> _segmentRiver = new List<int>();
        private readonly List<double> _riverWidth = new List<double>();
        private readonly List<Direction> _mouths = new List<Direction>();
        private readonly Dictionary<long, List<int>> _index = new Dictionary<long, List<int>>();

        /// <summary>Index cells are sized to the widest valley, so a query stays a 3x3 search.</summary>
        private readonly double _cellAngle = MaximumValleyHalfWidth;

        /// <summary>
        /// One step of a river: the segment between two path points, with the <b>profile</b> height at
        /// each end.
        ///
        /// <para>Segments rather than points because nearest-point distance rises between samples,
        /// which left the first version's channel floor scalloped - proximity 0.999 / 0.848 / 0.999
        /// measured along one straight reach.</para>
        /// </summary>
        private readonly struct RiverSegment
        {
            public RiverSegment(
                Direction from, Direction to, double fromHeight, double toHeight,
                double valleyHalfWidth, double bedHalfWidth)
            {
                FromX = from.X;
                FromY = from.Y;
                FromZ = from.Z;
                ToX = to.X;
                ToY = to.Y;
                ToZ = to.Z;
                FromHeight = fromHeight;
                ToHeight = toHeight;
                ValleyHalfWidth = valleyHalfWidth;
                BedHalfWidth = bedHalfWidth;
            }

            public double FromX { get; }
            public double FromY { get; }
            public double FromZ { get; }
            public double ToX { get; }
            public double ToY { get; }
            public double ToZ { get; }

            /// <summary>Water surface at each end - monotonically decreasing along the course.</summary>
            public double FromHeight { get; }

            public double ToHeight { get; }

            public double ValleyHalfWidth { get; }
            public double BedHalfWidth { get; }
        }

        /// <summary>What a river does to one point of terrain.</summary>
        public readonly struct RiverInfluence
        {
            public RiverInfluence(double weight, double targetHeight, double channel)
            {
                Weight = weight;
                TargetHeight = targetHeight;
                Channel = channel;
            }

            /// <summary>How strongly the valley claims this point: 0 outside it, 1 midstream.</summary>
            public double Weight { get; }

            /// <summary>The height the ground is pulled toward: the water surface, minus the bed.</summary>
            public double TargetHeight { get; }

            /// <summary>How much open water is here, for shading and for the moisture gift.</summary>
            public double Channel { get; }

            public bool Touches { get { return Weight > 0d; } }
        }

        private RiverNetwork()
        {
        }

        /// <summary>Number of recorded segments. Diagnostic; the probe reports it.</summary>
        public int PointCount { get { return _segments.Count; } }

        /// <summary>Number of rivers that reached the sea, or joined a river that did.</summary>
        public int RiverCount { get; private set; }

        /// <summary>Confluences: walks that ended by joining an existing course.</summary>
        public int ConfluenceCount { get; private set; }

        /// <summary>
        /// Walk the rivers of one world.
        ///
        /// <para>Deterministic in the seed and settings alone: candidate sources are a fixed spiral,
        /// and the walk is steepest descent with no random component.</para>
        /// </summary>
        public static RiverNetwork Create(int seed, PlateStructure plates, TerrainSettings settings, int riverCount = 96)
        {
            if (plates == null) throw new ArgumentNullException(nameof(plates));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var network = new RiverNetwork();
            foreach (Direction source in FindSources(
                seed, plates, settings, riverCount, SourceSeparation, SourceElevation))
            {
                network.Walk(seed, plates, settings, source, tributary: false);
            }

            foreach (Direction source in FindSources(
                seed, plates, settings, riverCount * 6, TributarySeparation, TributaryElevation))
            {
                network.Walk(seed, plates, settings, source, tributary: true);
            }

            return network;
        }

        /// <summary>
        /// What the nearest river does to this point, or a zero-weight influence if none is near.
        ///
        /// <para>Nearest by valley weight rather than by distance, so where two valleys overlap the
        /// one claiming the point more strongly wins outright. Adding them would dig the confluence
        /// twice.</para>
        /// </summary>
        public RiverInfluence Influence(double dx, double dy, double dz)
        {
            if (_segments.Count == 0) return default;

            Cell(dx, dy, dz, out int latitudeCell, out int longitudeCell);
            double bestWeight = 0d;
            double bestTarget = 0d;
            double bestChannel = 0d;

            for (int latitudeOffset = -1; latitudeOffset <= 1; latitudeOffset++)
            {
                for (int longitudeOffset = -1; longitudeOffset <= 1; longitudeOffset++)
                {
                    long key = Key(latitudeCell + latitudeOffset, longitudeCell + longitudeOffset);
                    if (!_index.TryGetValue(key, out List<int> bucket)) continue;

                    for (int entry = 0; entry < bucket.Count; entry++)
                    {
                        int index = bucket[entry];
                        RiverSegment segment = _segments[index];
                        double widthScale = _riverWidth[_segmentRiver[index]];
                        double valleyHalfWidth = segment.ValleyHalfWidth * widthScale;

                        double angle = AngleToSegment(segment, dx, dy, dz, out double along);
                        if (angle >= valleyHalfWidth) continue;

                        double weight = Smooth01(1d - (angle / valleyHalfWidth));
                        if (weight <= bestWeight) continue;

                        // The bed grows with the square root of the widening while the valley grows
                        // with all of it: a tributary makes a river meaningfully wider valley-wise
                        // long before it makes the water itself wide, and unchecked the arena filled
                        // with 16 m of open water at creature scale.
                        double bedHalfWidth = segment.BedHalfWidth * Math.Sqrt(widthScale);
                        double channel = angle >= bedHalfWidth ? 0d : Smooth01(1d - (angle / bedHalfWidth));
                        double surface = segment.FromHeight + ((segment.ToHeight - segment.FromHeight) * along);

                        bestWeight = weight;
                        bestChannel = channel;
                        bestTarget = surface;
                    }
                }
            }

            return new RiverInfluence(bestWeight, bestTarget, bestChannel);
        }

        /// <summary>
        /// True when no segment of any course rises from its start to its end.
        ///
        /// <para>The downhill guarantee, checkable. Sampling the field along a straight line cannot
        /// test it - a line crosses several rivers and the height jumps between them, which is a
        /// different river, not a river climbing.</para>
        /// </summary>
        public bool EveryCourseDescends()
        {
            for (int index = 0; index < _segments.Count; index++)
            {
                if (_segments[index].ToHeight > _segments[index].FromHeight + 1e-9d) return false;
            }

            return true;
        }

        /// <summary>Moisture to add, given how much open water is at a point.</summary>
        public static double Wetting(double channel)
        {
            return MoistureGift * channel;
        }

        /// <summary>
        /// Move a view centre to the nearest river mouth, if one is within
        /// <paramref name="within"/> radians.
        ///
        /// <para>The arena is a 50 m window on a planet 3 km around, so where it is put decides
        /// whether it holds a river at all. The renderer and the terrain-driven environment field both
        /// call this after choosing a coastline, so they keep describing the same ground.</para>
        /// </summary>
        public bool SnapToNearestMouth(ref double latitude, ref double longitude, double within = 0.25d)
        {
            if (_mouths.Count == 0) return false;

            double cosLatitude = Math.Cos(latitude);
            var at = new Direction(
                cosLatitude * Math.Sin(longitude), Math.Sin(latitude), cosLatitude * Math.Cos(longitude));

            int best = -1;
            double bestAngle = within;
            for (int index = 0; index < _mouths.Count; index++)
            {
                double angle = Angle(at, _mouths[index]);
                if (angle >= bestAngle) continue;

                bestAngle = angle;
                best = index;
            }

            if (best < 0) return false;

            Direction mouth = _mouths[best];
            latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, mouth.Y)));
            longitude = Math.Atan2(mouth.X, mouth.Z);
            return true;
        }

        // ---- construction -------------------------------------------------------------------

        /// <summary>
        /// The highest candidates, spread out. Taking the top N by height alone puts every river on
        /// one range; a minimum separation scatters them over the continents.
        /// </summary>
        private static List<Direction> FindSources(
            int seed, PlateStructure plates, TerrainSettings settings, int riverCount,
            double separation, double minimumElevation)
        {
            var candidates = new List<KeyValuePair<double, Direction>>();
            double golden = Math.PI * (3d - Math.Sqrt(5d));
            for (int index = 0; index < CandidateCount; index++)
            {
                double y = 1d - (2d * (index + 0.5d) / CandidateCount);
                double radius = Math.Sqrt(Math.Max(0d, 1d - (y * y)));
                double theta = golden * index;
                var direction = new Direction(radius * Math.Cos(theta), y, radius * Math.Sin(theta));
                double elevation = Elevation(seed, plates, settings, direction);
                if (elevation < minimumElevation) continue;

                candidates.Add(new KeyValuePair<double, Direction>(elevation, direction));
            }

            candidates.Sort((left, right) => right.Key.CompareTo(left.Key));

            var sources = new List<Direction>();
            foreach (KeyValuePair<double, Direction> candidate in candidates)
            {
                if (sources.Count >= riverCount) break;

                bool tooClose = false;
                for (int index = 0; index < sources.Count; index++)
                {
                    if (Angle(sources[index], candidate.Value) < separation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) sources.Add(candidate.Value);
            }

            return sources;
        }

        /// <summary>
        /// Steepest descent from one source until the water reaches the sea, joins another river,
        /// stalls in a hollow, or runs out of patience.
        ///
        /// <para>A stalled river is discarded rather than drained: an inland lake needs a water body
        /// with a surface height, which is a different feature, and half of one drawn as a river that
        /// stops in a field is worse than no river.</para>
        /// </summary>
        private void Walk(
            int seed, PlateStructure plates, TerrainSettings settings, Direction source, bool tributary)
        {
            var path = new List<Direction>();
            var heights = new List<double>();
            Direction current = source;
            double currentElevation = Elevation(seed, plates, settings, current);
            bool arrived = false;
            int joined = -1;
            double headingEast = 0d;
            double headingNorth = 0d;
            bool hasHeading = false;

            for (int step = 0; step < MaximumSteps; step++)
            {
                path.Add(current);
                heights.Add(currentElevation);

                if (currentElevation <= 0d)
                {
                    arrived = true;
                    break;
                }

                // A course meeting an existing one is a confluence, not a crossing. Stopping here is
                // what turns a set of parallel strands into a branching network, and the river joined
                // widens for it.
                if (step > 4)
                {
                    joined = RiverAt(current);
                    if (joined >= 0)
                    {
                        arrived = true;
                        ConfluenceCount++;
                        _riverWidth[joined] += ConfluenceWidening;
                        break;
                    }
                }

                // Gradient from a ring of samples rather than a winner-takes-all pick: every
                // direction contributes in proportion to how much it drops, so the result points down
                // the true slope instead of at one of eight compass points.
                Tangents(current, out Direction east, out Direction north);
                double downEast = 0d;
                double downNorth = 0d;
                double bestDrop = 0d;
                for (int index = 0; index < Directions; index++)
                {
                    double angle = 2d * Math.PI * index / Directions;
                    double alongEast = Math.Cos(angle);
                    double alongNorth = Math.Sin(angle);
                    Direction candidate = Step(current, east, north, alongEast, alongNorth);
                    double drop = currentElevation - Elevation(seed, plates, settings, candidate);
                    if (drop <= 0d) continue;

                    downEast += alongEast * drop;
                    downNorth += alongNorth * drop;
                    if (drop > bestDrop) bestDrop = drop;
                }

                if (bestDrop <= 0d) break;

                double length = Math.Sqrt((downEast * downEast) + (downNorth * downNorth));
                if (length <= 0d) break;

                downEast /= length;
                downNorth /= length;

                if (hasHeading)
                {
                    downEast = (Inertia * headingEast) + ((1d - Inertia) * downEast);
                    downNorth = (Inertia * headingNorth) + ((1d - Inertia) * downNorth);
                    length = Math.Sqrt((downEast * downEast) + (downNorth * downNorth));
                    if (length <= 0d) break;

                    downEast /= length;
                    downNorth /= length;
                }

                Direction next = Step(current, east, north, downEast, downNorth);
                double nextElevation = Elevation(seed, plates, settings, next);

                // Momentum must not carry the water uphill. When it would, drop the heading for this
                // step and take the plain gradient - the alternative is a river climbing out of its
                // own valley on a bend.
                if (nextElevation > currentElevation)
                {
                    downEast = (downEast - (Inertia * headingEast)) / (1d - Inertia);
                    downNorth = (downNorth - (Inertia * headingNorth)) / (1d - Inertia);
                    length = Math.Sqrt((downEast * downEast) + (downNorth * downNorth));
                    if (length <= 0d) break;

                    downEast /= length;
                    downNorth /= length;
                    next = Step(current, east, north, downEast, downNorth);
                    nextElevation = Elevation(seed, plates, settings, next);
                    if (nextElevation > currentElevation) break;
                }

                headingEast = downEast;
                headingNorth = downNorth;
                hasHeading = true;
                current = next;
                currentElevation = nextElevation;
            }

            if (!arrived || path.Count < 8) return;

            // A tributary that never met anything is just another trunk in a place chosen for
            // tributaries - too close to its neighbours, and it would corrugate the land.
            if (tributary && joined < 0) return;

            int river = _riverWidth.Count;
            _riverWidth.Add(1d);
            RiverCount++;
            // The view point is two thirds of the way down, not the mouth. A window centred on a
            // mouth is an estuary - the river fans out and the arena is mostly water; two thirds down
            // it is a river crossing a landscape, which is what the arena wants to show.
            if (joined < 0) _mouths.Add(path[(path.Count * 2) / 3]);

            Smooth(path);
            double[] profile = BuildProfile(heights);
            for (int index = 0; index < path.Count - 1; index++)
            {
                double along = index / (double)(path.Count - 1);

                // Taper the head. A course that starts at full width appears out of nothing partway
                // up a hillside, which reads as a line someone drew rather than as a stream forming.
                double head = Smooth01(along / 0.2d);
                double widening = Math.Sqrt(along) * head;
                double valleyHalfWidth = MinimumValleyHalfWidth
                    + ((MaximumValleyHalfWidth - MinimumValleyHalfWidth) * widening);
                double bedHalfWidth = MinimumBedHalfWidth
                    + ((MaximumBedHalfWidth - MinimumBedHalfWidth) * widening);

                Add(river, path[index], path[index + 1], profile[index], profile[index + 1],
                    valleyHalfWidth, bedHalfWidth);
            }
        }

        /// <summary>
        /// Take the last of the corners out of a path.
        ///
        /// <para>Momentum removes most of the staircase, but a step is 1.25 m and the ring of samples
        /// is finite, so short zigzags survive. Averaging positions over a few steps either side
        /// costs nothing and is invisible except where the path was jagged - the endpoints are left
        /// alone so a mouth stays at the sea and a source stays on its hill.</para>
        /// </summary>
        private static void Smooth(List<Direction> path)
        {
            if (path.Count < (2 * PathSmoothing) + 3) return;

            var smoothed = new List<Direction>(path);
            for (int index = PathSmoothing; index < path.Count - PathSmoothing; index++)
            {
                double x = 0d, y = 0d, z = 0d;
                for (int entry = index - PathSmoothing; entry <= index + PathSmoothing; entry++)
                {
                    x += path[entry].X;
                    y += path[entry].Y;
                    z += path[entry].Z;
                }

                smoothed[index] = new Direction(x, y, z);
            }

            for (int index = 0; index < path.Count; index++) path[index] = smoothed[index];
        }

        /// <summary>
        /// The water surface along a course: never rising, and smooth.
        ///
        /// <para>Terrain height alone is not a water surface - it rises wherever the walk crossed a
        /// bump, and a river that climbs is the most obvious way for one to look wrong. A running
        /// minimum makes it monotone and a moving average takes the steps out; the minimum is
        /// re-applied afterwards because smoothing can otherwise lift a value above its
        /// predecessor.</para>
        /// </summary>
        private static double[] BuildProfile(List<double> heights)
        {
            var profile = new double[heights.Count];
            double running = heights[0];
            for (int index = 0; index < heights.Count; index++)
            {
                running = Math.Min(running, heights[index]);
                profile[index] = running;
            }

            var smoothed = new double[profile.Length];
            for (int index = 0; index < profile.Length; index++)
            {
                int first = Math.Max(0, index - ProfileSmoothing);
                int last = Math.Min(profile.Length - 1, index + ProfileSmoothing);
                double total = 0d;
                for (int entry = first; entry <= last; entry++) total += profile[entry];
                smoothed[index] = total / (last - first + 1);
            }

            running = smoothed[0];
            for (int index = 0; index < smoothed.Length; index++)
            {
                running = Math.Min(running, smoothed[index]);
                smoothed[index] = running;
            }

            return smoothed;
        }

        /// <summary>Which river covers this point with open water, or -1.</summary>
        private int RiverAt(Direction at)
        {
            if (_segments.Count == 0) return -1;

            Cell(at.X, at.Y, at.Z, out int latitudeCell, out int longitudeCell);
            for (int latitudeOffset = -1; latitudeOffset <= 1; latitudeOffset++)
            {
                for (int longitudeOffset = -1; longitudeOffset <= 1; longitudeOffset++)
                {
                    long key = Key(latitudeCell + latitudeOffset, longitudeCell + longitudeOffset);
                    if (!_index.TryGetValue(key, out List<int> bucket)) continue;

                    for (int entry = 0; entry < bucket.Count; entry++)
                    {
                        int index = bucket[entry];
                        RiverSegment segment = _segments[index];
                        double widthScale = _riverWidth[_segmentRiver[index]];
                        double angle = AngleToSegment(segment, at.X, at.Y, at.Z, out double _);
                        if (angle < segment.BedHalfWidth * widthScale) return _segmentRiver[index];
                    }
                }
            }

            return -1;
        }

        private void Add(
            int river, Direction from, Direction to, double fromHeight, double toHeight,
            double valleyHalfWidth, double bedHalfWidth)
        {
            int segmentIndex = _segments.Count;
            _segments.Add(new RiverSegment(from, to, fromHeight, toHeight, valleyHalfWidth, bedHalfWidth));
            _segmentRiver.Add(river);

            File(from, segmentIndex);
            File(to, segmentIndex);
        }

        /// <summary>
        /// File a segment under the cells at both of its ends.
        ///
        /// <para>Filing only the start would let a segment crossing a cell boundary be missed by a
        /// query in the cell it ends in. A segment is about one step long and a cell one valley wide,
        /// so both ends plus the 3x3 search covers it.</para>
        /// </summary>
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
            return PlanetTerrain.Sample(
                seed, plates, direction.X, direction.Y, direction.Z, WalkFrequency, settings).Elevation;
        }

        /// <summary>An orthonormal pair tangent to the sphere at this point.</summary>
        private static void Tangents(Direction at, out Direction east, out Direction north)
        {
            // Any vector not parallel to the point seeds the cross product; the pole swap keeps it
            // well conditioned when the point itself is near the y axis.
            double upX = Math.Abs(at.Y) > 0.9d ? 1d : 0d;
            double upY = Math.Abs(at.Y) > 0.9d ? 0d : 1d;
            east = new Direction(
                upY * at.Z,
                -(upX * at.Z),
                (upX * at.Y) - (upY * at.X));
            north = new Direction(
                (at.Y * east.Z) - (at.Z * east.Y),
                (at.Z * east.X) - (at.X * east.Z),
                (at.X * east.Y) - (at.Y * east.X));
        }

        private static Direction Step(Direction at, Direction east, Direction north, double alongEast, double alongNorth)
        {
            return new Direction(
                at.X + (StepAngle * ((alongEast * east.X) + (alongNorth * north.X))),
                at.Y + (StepAngle * ((alongEast * east.Y) + (alongNorth * north.Y))),
                at.Z + (StepAngle * ((alongEast * east.Z) + (alongNorth * north.Z))));
        }

        private static double Angle(Direction left, Direction right)
        {
            double dot = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
            if (dot > 1d) dot = 1d;
            else if (dot < -1d) dot = -1d;
            return Math.Acos(dot);
        }

        /// <summary>
        /// Angle from a direction to the nearest point of a segment, and how far along that point is.
        ///
        /// <para>Solved as point-to-line-segment on the chord, then re-normalised. Over a step of
        /// 0.0025 radians the chord and the arc differ by about one part in a million, four orders of
        /// magnitude below the widths this feeds.</para>
        /// </summary>
        private static double AngleToSegment(RiverSegment segment, double dx, double dy, double dz, out double along)
        {
            double abx = segment.ToX - segment.FromX;
            double aby = segment.ToY - segment.FromY;
            double abz = segment.ToZ - segment.FromZ;
            double lengthSquared = (abx * abx) + (aby * aby) + (abz * abz);

            along = 0d;
            if (lengthSquared > 0d)
            {
                along = (((dx - segment.FromX) * abx) + ((dy - segment.FromY) * aby) + ((dz - segment.FromZ) * abz))
                    / lengthSquared;
                if (along < 0d) along = 0d;
                else if (along > 1d) along = 1d;
            }

            var nearest = new Direction(
                segment.FromX + (along * abx),
                segment.FromY + (along * aby),
                segment.FromZ + (along * abz));

            double dot = (nearest.X * dx) + (nearest.Y * dy) + (nearest.Z * dz);
            if (dot > 1d) dot = 1d;
            else if (dot < -1d) dot = -1d;
            return Math.Acos(dot);
        }

        private void Cell(double dx, double dy, double dz, out int latitudeCell, out int longitudeCell)
        {
            double latitude = Math.Asin(Math.Max(-1d, Math.Min(1d, dy)));
            double longitude = Math.Atan2(dx, dz);
            latitudeCell = (int)Math.Floor(latitude / _cellAngle);

            // Longitude cells shrink toward the poles if bucketed at a constant angle, so scale by
            // cos(latitude): a bucket stays about one valley across everywhere and the search stays a
            // 3x3.
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

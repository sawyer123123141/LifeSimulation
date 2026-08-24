using System;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The rules that decide how finely the planet is drawn where the camera is looking.
    ///
    /// <para>No Unity types, so the headless project compiles it and the decisions can be checked
    /// without a person flying around in Play mode - the same reason <see cref="FreeCameraMotion"/>
    /// exists. What is here is every rule that has a threshold in it; the geometry that follows from
    /// those rules is in <c>PlanetChunkMesh</c>.</para>
    ///
    /// <para><b>The problem being solved.</b> The planet was one icosphere at subdivision 5 - about
    /// 20,000 triangles of roughly 19 metres each over the whole 500-unit sphere - so zooming in
    /// added no detail at all and standing anywhere but the arena meant standing on a facet the size
    /// of a house. Splitting only what is near the camera buys detail where it is looked at without
    /// paying for it on the far side.</para>
    /// </summary>
    public static class PlanetChunkLod
    {
        /// <summary>
        /// Triangles per chunk edge. A power of two so a chunk's detail level is exact.
        ///
        /// <para><b>32 with a depth cap of 5 was tried and is worse.</b> The reasoning was that
        /// bigger chunks would mean fewer of them and one less seam for the same finest triangle.
        /// The finest triangle and the chunk count both held - 908 chunks became 764 - but the
        /// triangle count went from 232k to 782k, because raising the band limit by a level makes
        /// the *coarse* chunks four times denser as well, and those are most of the sphere. One
        /// fewer seam is not worth three and a half times the geometry.</para>
        /// </summary>
        public const int Segments = 16;

        /// <summary>
        /// Extra icosphere subdivisions a chunk's own grid is worth.
        /// <c>log2(<see cref="Segments"/>)</c>, so a chunk at depth d has the sample spacing of a
        /// whole icosphere at subdivision <c>d + 4</c> - which lets the band limit be read straight
        /// out of the function the single-mesh planet already used.
        /// </summary>
        public const int SegmentLevels = 4;

        /// <summary>
        /// Deepest split. Depth 6 puts a chunk's triangles at about 0.6 units - smaller than a
        /// creature - and holds the tree to a few hundred leaves under a camera on the ground.
        /// </summary>
        public const int MaximumDepth = 6;

        /// <summary>
        /// How close, in multiples of a chunk's own edge length, the camera has to be before that
        /// chunk splits.
        ///
        /// <para>The standard screen-space-error rule in the form that needs no projection matrix: a
        /// chunk is subdivided while it subtends more than roughly a fixed angle. Raising this makes
        /// the world sharper and the tree larger, in the square.</para>
        /// </summary>
        public const double SplitFactor = 2.6d;

        /// <summary>
        /// How much further away a chunk must be to merge again than it was to split.
        ///
        /// <para>Without a gap, a camera sitting exactly on a threshold splits and merges the same
        /// chunk every frame, rebuilding meshes forever. This is the whole of the fix.</para>
        /// </summary>
        public const double MergeHysteresis = 1.3d;

        /// <summary>Angular half-width of the arena window on the planet: 50 units on a 500 radius.</summary>
        public const double ArenaHalfAngle = 0.05d;

        /// <summary>Whether a chunk at this depth and distance should be drawn as four smaller ones.</summary>
        /// <param name="edgeWorld">Length of the chunk's edge in world units.</param>
        /// <param name="distance">Camera distance to the nearest point of the chunk.</param>
        public static bool ShouldSplit(double edgeWorld, double distance, int depth, int maximumDepth)
        {
            if (depth >= maximumDepth) return false;
            return distance < edgeWorld * SplitFactor;
        }

        /// <summary>Whether four chunks at this depth should collapse back into their parent.</summary>
        public static bool ShouldMerge(double edgeWorld, double distance, int depth)
        {
            if (depth <= 0) return false;
            return distance > edgeWorld * SplitFactor * MergeHysteresis;
        }

        /// <summary>
        /// The icosphere subdivision level a chunk at this depth samples at.
        ///
        /// <para>Fed to <c>PlanetTerrain.MaximumFrequencyFor</c> so each chunk carries exactly the
        /// octaves its own grid can hold. Getting this wrong in either direction is visible: too few
        /// and a close chunk is as smooth as the far ones, too many and it is static, which is the
        /// artefact the band limit was introduced to remove in the first place.</para>
        /// </summary>
        public static int DetailLevelFor(int depth)
        {
            return depth + SegmentLevels;
        }

        /// <summary>
        /// How far a chunk's border skirt hangs below its edge, in world units.
        ///
        /// <para>Neighbouring chunks may differ by a level, so their shared edge is sampled at two
        /// different resolutions and the two surfaces do not meet - a crack straight through to the
        /// sky. A rim hanging inward from every chunk edge fills it. The depth has to beat the
        /// elevation the two resolutions can disagree by. <b>That disagreement was measured</b>, in
        /// <c>PlanetChunkSeamTests</c>: worst case about 0.04 of the chunk's edge at every level, so
        /// 0.05 clears it everywhere with room to spare. It was 0.08 first, which is a rim nearly
        /// three times taller than the crack it fills - and made no visible difference either way,
        /// since a skirt is only ever seen edge-on.</para>
        /// </summary>
        public static double SkirtDepth(double edgeWorld)
        {
            double depth = edgeWorld * 0.05d;
            return depth < 0.15d ? 0.15d : depth;
        }

        /// <summary>
        /// Whether a chunk is hidden underneath the arena patch and need not be drawn.
        ///
        /// <para>The arena is drawn separately and at a higher resolution, lifted clear by
        /// <c>ArenaProjection.PatchLift</c>. A backdrop chunk entirely underneath it is invisible and
        /// - once chunks get fine enough to disagree with the patch by more than that lift - is also
        /// what would poke through it. Dropping the ones fully inside costs nothing and removes most
        /// of the overlap; the ring that straddles the border is still drawn, because there the patch
        /// has an edge that would otherwise show a hole beside it.</para>
        /// </summary>
        /// <param name="angleToArena">Angle between the chunk's centre and the arena's centre.</param>
        /// <param name="angularRadius">Angular radius of the chunk itself.</param>
        public static bool HiddenByArena(double angleToArena, double angularRadius)
        {
            return angleToArena + angularRadius < ArenaHalfAngle;
        }

        /// <summary>
        /// Roughly how many chunks a camera at this height ends up drawing.
        ///
        /// <para>A cost model, so that raising <see cref="SplitFactor"/> or
        /// <see cref="MaximumDepth"/> fails a test rather than turning up later as a frame rate
        /// nobody attributes to it. <b>A chunk exists because its parent split</b>, not because it
        /// did - which is why the first version of this was wrong by a factor of six, predicting 150
        /// where the renderer drew 908.</para>
        /// </summary>
        public static int ApproximateLeafCount(double planetRadius, double altitude, int maximumDepth)
        {
            double visible = 2d * Math.PI * planetRadius * planetRadius;
            double height = altitude < 1d ? 1d : altitude;
            int total = 0;

            for (int depth = 1; depth <= maximumDepth; depth++)
            {
                // Distance is measured to a chunk's nearest part, so the threshold reaches half a
                // chunk further than the chunk's own centre.
                double outer = Reach(EdgeAt(planetRadius, depth - 1), height);
                double inner = depth < maximumDepth ? Reach(EdgeAt(planetRadius, depth), height) : 0d;
                if (outer <= inner) continue;

                double band = Math.PI * ((outer * outer) - (inner * inner));
                if (band > visible) band = visible;

                double chunkArea = 4d * Math.PI * planetRadius * planetRadius / (20d * Math.Pow(4d, depth));
                total += (int)(band / chunkArea);
            }

            return total;
        }

        /// <summary>Ground distance at which a chunk of this size stops splitting, seen from a height.</summary>
        private static double Reach(double edgeWorld, double height)
        {
            double slant = (edgeWorld * SplitFactor) + (edgeWorld * 0.5d);
            double squared = (slant * slant) - (height * height);
            return squared <= 0d ? 0d : Math.Sqrt(squared);
        }

        /// <summary>Edge length of a chunk at a depth, in world units.</summary>
        public static double EdgeAt(double planetRadius, int depth)
        {
            return planetRadius * 1.107d / Math.Pow(2d, depth);
        }
    }
}

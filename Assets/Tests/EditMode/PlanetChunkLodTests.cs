using LifeSimulation.Presentation;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The level-of-detail rules, checked without a renderer.
    ///
    /// <para>What can go wrong here is not subtle and is expensive to find by looking: a tree that
    /// splits forever, one that thrashes on a threshold, or one whose chunks all sample at the same
    /// band limit so splitting adds triangles and no detail. All three are arithmetic.</para>
    /// </summary>
    public sealed class PlanetChunkLodTests
    {
        private const double PlanetRadius = 500d;

        /// <summary>Far away, nothing splits - the whole point of paying only for what is looked at.</summary>
        [Test]
        public void ADistantPlanetStaysCoarse()
        {
            double edge = PlanetChunkLod.EdgeAt(PlanetRadius, 0);
            Assert.That(
                PlanetChunkLod.ShouldSplit(edge, PlanetRadius * 4d, 0, PlanetChunkLod.MaximumDepth),
                Is.False);
        }

        [Test]
        public void StandingOnTheGroundSplitsToTheLimit()
        {
            for (int depth = 0; depth < PlanetChunkLod.MaximumDepth; depth++)
            {
                double edge = PlanetChunkLod.EdgeAt(PlanetRadius, depth);
                Assert.That(
                    PlanetChunkLod.ShouldSplit(edge, 2d, depth, PlanetChunkLod.MaximumDepth),
                    Is.True,
                    "a camera on the surface should split every level down to the cap, failed at " + depth);
            }
        }

        /// <summary>The cap is what stops the tree, and nothing else does.</summary>
        [Test]
        public void TheDepthCapIsHonoured()
        {
            double edge = PlanetChunkLod.EdgeAt(PlanetRadius, PlanetChunkLod.MaximumDepth);
            Assert.That(
                PlanetChunkLod.ShouldSplit(edge, 0d, PlanetChunkLod.MaximumDepth, PlanetChunkLod.MaximumDepth),
                Is.False);
        }

        /// <summary>
        /// A chunk must not merge at the distance it splits at, or a camera sitting on the boundary
        /// rebuilds meshes every frame forever.
        /// </summary>
        [Test]
        public void SplittingAndMergingDoNotOverlap()
        {
            double edge = PlanetChunkLod.EdgeAt(PlanetRadius, 2);
            double boundary = edge * PlanetChunkLod.SplitFactor;

            Assert.That(PlanetChunkLod.ShouldSplit(edge, boundary * 0.99d, 2, PlanetChunkLod.MaximumDepth), Is.True);
            Assert.That(PlanetChunkLod.ShouldMerge(edge, boundary * 0.99d, 3), Is.False);
            Assert.That(PlanetChunkLod.ShouldMerge(edge, boundary * 1.01d, 3), Is.False, "still inside the gap");
            Assert.That(PlanetChunkLod.ShouldMerge(edge, boundary * 1.5d, 3), Is.True);
        }

        /// <summary>Roots have no parent to collapse into.</summary>
        [Test]
        public void RootsNeverMerge()
        {
            Assert.That(PlanetChunkLod.ShouldMerge(1d, 1e9d, 0), Is.False);
        }

        /// <summary>
        /// The reason the whole exercise is worth anything: a deeper chunk samples finer. If this
        /// were constant, splitting would draw the same smooth surface with more triangles.
        /// </summary>
        [Test]
        public void DeeperChunksSampleFiner()
        {
            for (int depth = 1; depth <= PlanetChunkLod.MaximumDepth; depth++)
            {
                Assert.That(
                    PlanetChunkLod.DetailLevelFor(depth),
                    Is.GreaterThan(PlanetChunkLod.DetailLevelFor(depth - 1)));
            }
        }

        /// <summary>
        /// A chunk's own grid is worth four icosphere subdivisions, so the deepest chunk is drawn at
        /// the detail of a whole sphere at subdivision 10 - which is what it would cost to draw the
        /// planet that finely everywhere, and the reason nobody does.
        /// </summary>
        [Test]
        public void TheDeepestChunkMatchesAVeryFineSphere()
        {
            Assert.That(PlanetChunkLod.DetailLevelFor(PlanetChunkLod.MaximumDepth), Is.EqualTo(10));
        }

        /// <summary>Chunks must not be finer than a creature is large, or the tree pays for nothing.</summary>
        [Test]
        public void TheFinestTrianglesAreAboutACreatureWide()
        {
            double edge = PlanetChunkLod.EdgeAt(PlanetRadius, PlanetChunkLod.MaximumDepth);
            double triangle = edge / PlanetChunkLod.Segments;

            Assert.That(triangle, Is.LessThan(1d), "detail should reach below creature scale");
            Assert.That(triangle, Is.GreaterThan(0.1d), "and not so far below that it is wasted");
        }

        /// <summary>
        /// The skirt has to beat the height two neighbouring resolutions can disagree by, which grows
        /// with the chunk - and must never be zero, or the crack is open at the finest level.
        /// </summary>
        [Test]
        public void SkirtsScaleWithTheChunkAndNeverVanish()
        {
            double coarse = PlanetChunkLod.SkirtDepth(PlanetChunkLod.EdgeAt(PlanetRadius, 0));
            double fine = PlanetChunkLod.SkirtDepth(PlanetChunkLod.EdgeAt(PlanetRadius, PlanetChunkLod.MaximumDepth));

            Assert.That(coarse, Is.GreaterThan(fine));
            Assert.That(fine, Is.GreaterThan(0d));
        }

        /// <summary>A chunk buried under the arena patch is not drawn; one straddling its edge is.</summary>
        [Test]
        public void ChunksUnderTheArenaAreDropped()
        {
            Assert.That(PlanetChunkLod.HiddenByArena(0d, 0.01d), Is.True);
            Assert.That(PlanetChunkLod.HiddenByArena(0.04d, 0.02d), Is.False, "straddles the patch edge");
            Assert.That(PlanetChunkLod.HiddenByArena(1d, 0.01d), Is.False);
        }

        /// <summary>
        /// A cost figure, so raising the split factor or the depth cap fails here rather than in a
        /// frame-rate drop nobody attributes to it.
        ///
        /// <para>Calibrated against the renderer, not guessed: the offline capture from 20 metres up
        /// logged <b>908 chunks across depths 1 to 6</b>. An earlier version of the estimator said
        /// 150, because it counted a chunk as existing when it split rather than when its parent
        /// did.</para>
        /// </summary>
        [Test]
        public void TheTreeStaysSmallEnoughToDraw()
        {
            int onTheGround = PlanetChunkLod.ApproximateLeafCount(PlanetRadius, 20d, PlanetChunkLod.MaximumDepth);
            int inOrbit = PlanetChunkLod.ApproximateLeafCount(PlanetRadius, PlanetRadius * 4d, PlanetChunkLod.MaximumDepth);

            Assert.That(inOrbit, Is.LessThan(30), "far away the planet is barely more than its base faces");
            Assert.That(onTheGround, Is.InRange(400, 1600), "measured 908 in the capture");
        }
    }
}

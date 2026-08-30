using System.Collections.Generic;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Clicking a creature to inspect it stopped working when creatures became models.
    ///
    /// <para>The old path was <c>Physics.Raycast</c> matched against <c>view == hit.transform</c>.
    /// That worked only because <c>GameObject.CreatePrimitive</c> hands back a capsule with a
    /// collider already on the same transform. An instantiated FBX has <b>no collider at all</b> -
    /// nothing in the project adds one - so the raycast hits nothing and selection is dead. Terrain
    /// has no collider either, so there is no occlusion to respect and nothing to gain from
    /// reintroducing physics for this.</para>
    ///
    /// <para>These tests cover the pure half: given where each creature landed on screen, which one
    /// does a click at a given point select. The Unity half - projecting world positions to screen -
    /// stays in the presenter.</para>
    /// </summary>
    [TestFixture]
    public sealed class CreaturePickingTests
    {
        private const float Radius = 30f;

        [Test]
        public void SelectsTheCreatureUnderTheClick()
        {
            var candidates = new List<CreaturePickCandidate>
            {
                Candidate(1, 100f, 100f, depth: 10f),
                Candidate(2, 500f, 400f, depth: 10f),
            };

            Assert.That(CreaturePicking.TrySelectClosest(102f, 98f, candidates, Radius, out CreatureId picked), Is.True);
            Assert.That(picked.Value, Is.EqualTo(1));
        }

        [Test]
        public void SelectsNothingWhenTheClickIsFarFromEveryCreature()
        {
            var candidates = new List<CreaturePickCandidate> { Candidate(1, 100f, 100f, depth: 10f) };

            Assert.That(CreaturePicking.TrySelectClosest(400f, 400f, candidates, Radius, out _), Is.False);
        }

        /// <summary>
        /// The complaint that started this work is that creatures clump, so overlapping creatures are
        /// the normal case rather than the edge case. The nearer one is the one being pointed at.
        /// </summary>
        [Test]
        public void PrefersTheNearerCreatureWhenTwoOverlapAtTheSamePoint()
        {
            var candidates = new List<CreaturePickCandidate>
            {
                Candidate(1, 200f, 200f, depth: 50f),
                Candidate(2, 200f, 200f, depth: 8f),
            };

            Assert.That(CreaturePicking.TrySelectClosest(200f, 200f, candidates, Radius, out CreatureId picked), Is.True);
            Assert.That(picked.Value, Is.EqualTo(2), "the creature closest to the camera should win a tie on screen position");
        }

        /// <summary>
        /// Screen distance dominates depth: a creature actually under the cursor beats a nearer one
        /// off to the side, or clicking one animal would select its neighbour.
        /// </summary>
        [Test]
        public void ScreenDistanceBeatsDepth()
        {
            var candidates = new List<CreaturePickCandidate>
            {
                Candidate(1, 200f, 200f, depth: 90f),
                Candidate(2, 225f, 200f, depth: 5f),
            };

            Assert.That(CreaturePicking.TrySelectClosest(200f, 200f, candidates, Radius, out CreatureId picked), Is.True);
            Assert.That(picked.Value, Is.EqualTo(1));
        }

        /// <summary>Behind the camera is not clickable, however close it lands on screen.</summary>
        [Test]
        public void IgnoresCreaturesBehindTheCamera()
        {
            var candidates = new List<CreaturePickCandidate>
            {
                new CreaturePickCandidate(new CreatureId(1), 200f, 200f, -3f),
            };

            Assert.That(CreaturePicking.TrySelectClosest(200f, 200f, candidates, Radius, out _), Is.False);
        }

        [Test]
        public void SelectsNothingWhenThereAreNoCreatures()
        {
            Assert.That(
                CreaturePicking.TrySelectClosest(0f, 0f, new List<CreaturePickCandidate>(), Radius, out _),
                Is.False);
        }

        /// <summary>Exactly at the radius counts as a hit; beyond it does not.</summary>
        [Test]
        public void TheRadiusIsInclusive()
        {
            var candidates = new List<CreaturePickCandidate> { Candidate(1, 100f, 100f, depth: 10f) };

            Assert.That(CreaturePicking.TrySelectClosest(100f + Radius, 100f, candidates, Radius, out _), Is.True);
            Assert.That(CreaturePicking.TrySelectClosest(100f + Radius + 0.5f, 100f, candidates, Radius, out _), Is.False);
        }

        private static CreaturePickCandidate Candidate(int id, float x, float y, float depth)
        {
            return new CreaturePickCandidate(new CreatureId(id), x, y, depth);
        }
    }
}

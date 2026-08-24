using LifeSimulation.Presentation;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The first automated checks any camera in this project has had.
    ///
    /// <para>Four camera bugs shipped in one session and every one was found by a person in Play
    /// mode, because the rules lived inside <c>LateUpdate</c>. These cover the three that decide
    /// whether the camera is usable - how fast it flies, how far it may go, and how far it may
    /// tip - and they are the reason that arithmetic was moved out of the MonoBehaviour.</para>
    /// </summary>
    public sealed class FreeCameraMotionTests
    {
        /// <summary>
        /// The point of the whole design: one camera that works at two scales three orders of
        /// magnitude apart, because the height above the ground is the scale the viewer is at.
        /// </summary>
        [Test]
        public void SpeedFollowsHeightAboveTheGround()
        {
            float beside = FreeCameraMotion.SpeedAt(2f, 500f, 1f, boost: false, slow: false);
            float above = FreeCameraMotion.SpeedAt(400f, 500f, 1f, boost: false, slow: false);

            Assert.That(beside, Is.LessThan(5f), "beside a 1-unit creature the camera must crawl");
            Assert.That(above, Is.GreaterThan(beside * 50f), "at altitude it must cover ground");
        }

        /// <summary>Sitting on the ground must not stop the camera dead.</summary>
        [Test]
        public void SpeedNeverFallsToZeroOnTheSurface()
        {
            Assert.That(
                FreeCameraMotion.SpeedAt(0f, 50f, 1f, boost: false, slow: false),
                Is.EqualTo(FreeCameraMotion.MinimumSpeed).Within(1e-4f));
        }

        /// <summary>Below the surface the camera still moves, and moves at the same rate.</summary>
        [Test]
        public void SpeedIgnoresTheSignOfTheHeight()
        {
            Assert.That(
                FreeCameraMotion.SpeedAt(-30f, 500f, 1f, boost: false, slow: false),
                Is.EqualTo(FreeCameraMotion.SpeedAt(30f, 500f, 1f, boost: false, slow: false)).Within(1e-4f));
        }

        /// <summary>
        /// The extent is a ceiling, so an arena-scale world cannot be flown out of at planet speed
        /// however high the camera has drifted.
        /// </summary>
        [Test]
        public void SpeedIsCappedByWhatIsOnScreen()
        {
            float far = FreeCameraMotion.SpeedAt(4000f, 50f, 1f, boost: false, slow: false);
            Assert.That(far, Is.EqualTo(50f).Within(1e-3f));
        }

        [Test]
        public void BoostAndSlowScaleTheSameSpeed()
        {
            float plain = FreeCameraMotion.SpeedAt(100f, 500f, 1f, boost: false, slow: false);

            Assert.That(
                FreeCameraMotion.SpeedAt(100f, 500f, 1f, boost: true, slow: false),
                Is.EqualTo(plain * FreeCameraMotion.BoostFactor).Within(1e-3f));
            Assert.That(
                FreeCameraMotion.SpeedAt(100f, 500f, 1f, boost: false, slow: true),
                Is.EqualTo(plain * FreeCameraMotion.SlowFactor).Within(1e-3f));
        }

        /// <summary>
        /// A floor deeper than any relief the generator produces. Every valley and ocean floor is
        /// reachable; leaving the world underneath is not.
        /// </summary>
        [Test]
        public void HeightIsBoundedBelowByTheDeepestGround()
        {
            Assert.That(FreeCameraMotion.ClampAltitude(-1000f, 500f), Is.EqualTo(-FreeCameraMotion.Underground));
            Assert.That(FreeCameraMotion.ClampAltitude(-10f, 500f), Is.EqualTo(-10f), "a valley is not out of bounds");
        }

        /// <summary>Far enough out to see the whole thing, not so far that nothing is on screen.</summary>
        [Test]
        public void HeightIsBoundedAboveByTheExtent()
        {
            Assert.That(FreeCameraMotion.ClampAltitude(9000f, 500f), Is.EqualTo(1500f));
            Assert.That(FreeCameraMotion.ClampAltitude(120f, 500f), Is.EqualTo(120f));
        }

        /// <summary>
        /// Stopping short of vertical is what keeps roll out of the view: the orientation is rebuilt
        /// each frame from a forward and an up, and those do not describe a rotation when parallel.
        /// </summary>
        [Test]
        public void PitchStopsShortOfVertical()
        {
            Assert.That(FreeCameraMotion.ClampPitch(80f, 40f), Is.EqualTo(FreeCameraMotion.PitchLimit));
            Assert.That(FreeCameraMotion.ClampPitch(-80f, -40f), Is.EqualTo(-FreeCameraMotion.PitchLimit));
            Assert.That(FreeCameraMotion.PitchLimit, Is.LessThan(90f));
        }

        /// <summary>
        /// The speed dial is multiplicative, so a notch covers the same fraction at every setting -
        /// and bounded, so it cannot be wound to a value the camera cannot be flown at.
        /// </summary>
        [Test]
        public void TheSpeedDialIsMultiplicativeAndBounded()
        {
            float dial = 1f;
            for (int notch = 0; notch < 200; notch++) dial = FreeCameraMotion.AdjustDial(dial, 1f);
            Assert.That(dial, Is.LessThanOrEqualTo(12f));

            for (int notch = 0; notch < 400; notch++) dial = FreeCameraMotion.AdjustDial(dial, -1f);
            Assert.That(dial, Is.GreaterThanOrEqualTo(0.1f));
        }
    }
}

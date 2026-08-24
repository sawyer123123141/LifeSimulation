namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The arithmetic behind the free-fly camera, with no Unity input and no transform.
    ///
    /// <para>Separated so it can be tested. Every camera bug this project has shipped was found by a
    /// human in Play mode, because the rules lived inside <c>LateUpdate</c> where nothing could reach
    /// them. The rules that actually misbehaved - what the speed is here, where the camera is not
    /// allowed to go, how far the pitch may tip - are all in this file and all pure.</para>
    /// </summary>
    public static class FreeCameraMotion
    {
        /// <summary>
        /// Slowest the camera flies, in units per second, before the speed dial is applied. Standing
        /// on the ground beside a 1-unit creature, anything quicker overshoots it.
        /// </summary>
        public const float MinimumSpeed = 1.8f;

        /// <summary>Multiplier while the boost key is held, and while the slow key is.</summary>
        public const float BoostFactor = 5f;

        /// <summary>Multiplier while the slow key is held.</summary>
        public const float SlowFactor = 0.2f;

        /// <summary>Steepest look, up or down. Level with the horizon is zero.</summary>
        public const float PitchLimit = 88f;

        /// <summary>
        /// How far below the surface the camera may sink, in units.
        ///
        /// <para>Deeper than any relief the generator produces - elevation runs to about one unit at
        /// thirty units of height each - so every valley, shore and ocean floor is reachable, and
        /// leaving the world underneath it is not.</para>
        /// </summary>
        public const float Underground = 45f;

        /// <summary>
        /// Flight speed in units per second at a given height above the surface.
        ///
        /// <para><b>This is the whole reason a free camera works at both scales.</b> A fixed speed is
        /// either unusable beside a creature or unusable at planet distance, and the height above the
        /// ground is exactly the scale the viewer is working at: a metre up they are looking at an
        /// animal, five hundred up they are looking at a continent.</para>
        /// </summary>
        /// <param name="altitude">Height above the nominal surface. Sign is ignored.</param>
        /// <param name="extent">Radius of what is being looked at - the speed ceiling.</param>
        /// <param name="dial">The wheel-adjusted multiplier, nominally one.</param>
        public static float SpeedAt(float altitude, float extent, float dial, bool boost, bool slow)
        {
            float height = altitude < 0f ? -altitude : altitude;
            float ceiling = extent < MinimumSpeed ? MinimumSpeed : extent;
            float speed = height < MinimumSpeed ? MinimumSpeed : height;
            if (speed > ceiling) speed = ceiling;

            speed *= dial;
            if (boost) speed *= BoostFactor;
            if (slow) speed *= SlowFactor;
            return speed;
        }

        /// <summary>
        /// The height the camera is allowed to be at, given the one it is trying to reach.
        ///
        /// <para>A free camera has no focus point, so there is no pan box to get wrong - the whole
        /// class of clamp bug the orbit rig had cannot occur here. What remains is a floor, so the
        /// view does not end up under the world looking at unlit back faces, and a ceiling, so
        /// holding a key does not fly off into empty space with nothing on screen to navigate by.</para>
        /// </summary>
        public static float ClampAltitude(float altitude, float extent)
        {
            float ceiling = extent * 3f;
            if (altitude > ceiling) return ceiling;
            if (altitude < -Underground) return -Underground;
            return altitude;
        }

        /// <summary>
        /// Pitch after a mouse movement, in degrees above the horizon.
        ///
        /// <para>Stops just short of vertical because the orientation is rebuilt each frame from a
        /// forward direction and an up direction, and those two are not enough to describe a
        /// rotation when they are parallel. Stopping short is what keeps roll out of the view without
        /// any roll being tracked.</para>
        /// </summary>
        public static float ClampPitch(float pitch, float delta)
        {
            float next = pitch + delta;
            if (next > PitchLimit) return PitchLimit;
            if (next < -PitchLimit) return -PitchLimit;
            return next;
        }

        /// <summary>The speed dial after a wheel movement. Multiplicative, and bounded.</summary>
        public static float AdjustDial(float dial, float notches)
        {
            float next = dial * (1f + (notches * 0.15f));
            if (next < 0.1f) return 0.1f;
            if (next > 12f) return 12f;
            return next;
        }
    }
}

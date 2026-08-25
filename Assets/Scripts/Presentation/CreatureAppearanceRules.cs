using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The genome-to-appearance mapping. Pure, total, and deterministic.
    /// </summary>
    public static class CreatureAppearanceRules
    {
        /// <summary>Cold-adapted end of the thermal hue ramp.</summary>
        private static readonly float[] Cold = { 0.24f, 0.45f, 0.95f };

        /// <summary>The founder value, and the colour a population starts at.</summary>
        private static readonly float[] Temperate = { 0.92f, 0.90f, 0.78f };

        /// <summary>Heat-adapted end.</summary>
        private static readonly float[] Hot = { 0.95f, 0.34f, 0.14f };

        /// <summary>Matches the body scale the view already applies, so one call replaces both lines.</summary>
        public const float MinimumScale = 0.7f;
        public const float MaximumScale = 1.35f;

        /// <summary>
        /// Hue carries <b>temperature tolerance</b>, size carries body size.
        ///
        /// <para>Thermal tolerance is the trait with by far the strongest measured selection, so it is
        /// the one whose movement a viewer can actually see - but read
        /// <c>docs/experiments/p6-why-temperature-tolerance-2026-08-24.md</c> before expecting it to
        /// keep moving. It <b>saturates</b>: the field deviates by at most 8 degrees, tolerance is
        /// <c>2 + 8*gene</c>, so a gene of 0.75 covers the world and the mean plateaus near 0.78 by
        /// about tick 8,000 and then stops. Every population ends roughly the same colour.</para>
        ///
        /// <para>Which means <b>the thing to watch is the spread, not the mean</b>. Founders are drawn
        /// around 0.50 and scatter across the ramp; selection kills the cold tail and the crowd goes
        /// from mottled to uniform. A heterogeneous population becoming homogeneous is the clearest
        /// picture of directional selection this model can produce, and it is over by two-thirds of
        /// the way through a run.</para>
        ///
        /// <para><see cref="Genome.NeutralMarker"/> must never reach this function. It is the drift
        /// control; giving it a visible channel would make drift and selection look alike.</para>
        /// </summary>
        public static CreatureAppearance FromGenome(Genome genome)
        {
            float thermal = Clamp01(genome.TemperatureTolerance);
            float[] low = thermal < 0.5f ? Cold : Temperate;
            float[] high = thermal < 0.5f ? Temperate : Hot;
            float fraction = thermal < 0.5f ? thermal * 2f : (thermal - 0.5f) * 2f;

            return new CreatureAppearance(
                Lerp(low[0], high[0], fraction),
                Lerp(low[1], high[1], fraction),
                Lerp(low[2], high[2], fraction),
                Lerp(MinimumScale, MaximumScale, Clamp01(genome.BodySize)));
        }

        private static float Lerp(float from, float to, float fraction)
        {
            return from + ((to - from) * fraction);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}

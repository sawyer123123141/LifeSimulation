using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    /// <summary>
    /// Where a creature's temperature in degrees comes from.
    ///
    /// <para><b>Why this exists.</b> Temperature tolerance is the strongest selection in the model by
    /// a distance - the population moves a quarter of the trait range at t = 24 against a control at
    /// t = 0.07 - and until now it was adapting to <see cref="TemperatureField"/>, a decorative sine
    /// <c>20 + 8*sin(0.18x + 0.11y)</c> with no seasons, no altitude, no latitude and no connection
    /// to the terrain. Every other environmental quantity comes from the world; this one did not. See
    /// <c>docs/experiments/p6-why-temperature-tolerance-2026-08-24.md</c>.</para>
    ///
    /// <para><b>Not an ambient setting.</b> A <c>default</c> instance is the placeholder sine, which
    /// is what makes the flag-off path byte-identical without a branch at every call site. The
    /// terrain-driven instance carries the world's own <see cref="EnvironmentField"/>, so two worlds
    /// with equal configuration hashes cannot disagree about the climate - the mistake decision 13 in
    /// the handoff exists to prevent.</para>
    /// </summary>
    public readonly struct ClimateField
    {
        /// <summary>
        /// The span the placeholder sine covers, 12 to 28 degrees, kept deliberately.
        ///
        /// <para>Tolerance in degrees is <c>2 + 8*gene</c>, so an 8-degree half-span is what puts the
        /// saturation ceiling at gene 0.75. Holding the span fixed means switching the flag changes
        /// the field's <b>spatial structure</b> and nothing else, and a difference in the measured
        /// equilibrium is attributable to that alone.</para>
        /// </summary>
        public const float ComfortableCelsius = 20f;
        public const float HalfSpanCelsius = 8f;

        private readonly EnvironmentField _terrain;

        private ClimateField(EnvironmentField terrain)
        {
            _terrain = terrain;
        }

        /// <summary>The world's own field, mapped onto the same degree span as the placeholder.</summary>
        public static ClimateField FromTerrain(EnvironmentField terrain)
        {
            return new ClimateField(terrain);
        }

        /// <summary>True when this is the fixed sine rather than a real field.</summary>
        public bool IsPlaceholder => _terrain == null;

        public float Celsius(SimVector2 position, long tick)
        {
            if (_terrain == null) return TemperatureField.Sample(position, tick);

            // The environment field's temperature is a 0..1 climate index - latitude band, climate
            // noise, a local band across the arena, and a lapse rate that costs warmth with height.
            // Centred on 0.5 so the midpoint is comfortable, exactly as 20 degrees is for the sine.
            float index = _terrain.Sample(position).Temperature;
            return ComfortableCelsius + (HalfSpanCelsius * ((index * 2f) - 1f));
        }
    }
}

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Every tunable of the terrain generator, in one mutable object.
    ///
    /// <para><b>Why this exists.</b> These values were <c>private const</c> inside
    /// <see cref="PlanetTerrain"/>, which meant the only way to try a different number was to edit
    /// source and wait for a domain reload - and the record of this work is that reasoning about the
    /// field produced six wrong diagnoses while looking at it produced the answers. Terrain is judged
    /// by eye against a one-metre creature, so the loop has to be fast enough to actually turn.</para>
    ///
    /// <para><b>A freshly constructed instance is the shipped generator.</b> Nothing about the
    /// default look depends on the panel ever being opened; the panel starts from these values and
    /// can always be reset to them.</para>
    ///
    /// <para><b>Two defaults differ from the values in use before this type existed:</b>
    /// <c>LocalAmplitude</c> was 0.16 and <c>MicroAmplitude</c> 0.08. Both were above the slope
    /// ceiling, so <c>SlopeLimited</c> clipped them to 0.109 and 0.040 and the amplitudes were never
    /// really chosen - two bands both riding the ceiling. Measured over the 200-unit view, that put
    /// the median land grade at <b>0.243</b> against <b>0.085</b> for the planet-scale bands alone;
    /// the values here measure <b>0.119</b>.</para>
    ///
    /// <para><b>Pure C# on purpose</b> - no UnityEngine types - so an offline probe can compile the
    /// generator and sweep these numbers without Unity in the loop.</para>
    ///
    /// <para><b>Frequencies are in cycles per radian</b> and one radian is 500 metres, so a frequency
    /// <c>f</c> is a wavelength of <c>500 / f</c> metres. Amplitudes are in elevation units and 1.0
    /// is about 30 metres. Both conversions are needed to read any of these numbers; without them a
    /// constant here is meaningless, which is how <c>MaximumSlope</c> once ended up a 3% grade.</para>
    /// </summary>
    public sealed class TerrainSettings
    {
        /// <summary>Continental shelf: where land is at all. 1.15 cycles/radian is a 435 m feature.</summary>
        public double ContinentFrequency = 1.15d;

        /// <summary>Roughness added to the shelf, so a coastline is not a clean Voronoi seam.</summary>
        public double ShelfNoiseStrength = 0.32d;

        /// <summary>Mountain belts along plate margins. 3.6 cycles/radian, a 139 m feature.</summary>
        public double MountainFrequency = 3.6d;

        /// <summary>Peak height of the ridged band, before the slope limit applies.</summary>
        public double RangeAmplitude = 0.34d;

        /// <summary>Rolling ground over all land. 6.5 cycles/radian, a 77 m feature.</summary>
        public double HillFrequency = 6.5d;

        public double RollingAmplitude = 0.22d;

        /// <summary>Coarse surface detail. 11 cycles/radian, a 45 m feature.</summary>
        public double DetailFrequency = 11d;

        public double DetailAmplitude = 0.09d;

        /// <summary>
        /// Local relief at the scale of the thing walking on it: 55 cycles/radian is a 9 m
        /// undulation. Visible in the close view and in the arena, absent from the wide view because
        /// the mesh there cannot resolve it.
        /// </summary>
        public double LocalFrequency = 55d;

        public double LocalAmplitude = 0.036d;

        /// <summary>Ground texture: 150 cycles/radian is a 3.3 m feature, ankle-scale.</summary>
        public double MicroFrequency = 150d;

        public double MicroAmplitude = 0.012d;

        /// <summary>
        /// Steepest slope the renderer can represent, in elevation units per radian. A ceiling, not
        /// a target: a band whose chosen amplitude exceeds it is clipped to it, and two bands both
        /// sitting at the ceiling sum to something no ground does.
        /// </summary>
        public double MaximumSlope = 6d;

        /// <summary>Domain warp on the plate lookup, so coastlines wander off the cell edge.</summary>
        public double WarpFrequency = 2.1d;

        public double WarpStrength = 0.32d;

        public double Lacunarity = 2d;
        public double Gain = 0.5d;

        public double MoistureFrequency = 1.9d;
        public double ClimateNoiseFrequency = 2.4d;
        public double JitterFrequency = 16d;

        /// <summary>Share of temperature set by latitude; the remainder is noise.</summary>
        public double TemperatureLatitudeWeight = 0.78d;

        /// <summary>How much high ground cools. Raise for more snow, lower for less.</summary>
        public double AltitudeCooling = 0.30d;

        /// <summary>Contrast expansion on raw moisture. Raw fBm spans about .37-.82, which cannot
        /// reach the dry end at all, so deserts are impossible without this.</summary>
        public double MoistureContrast = 2.2d;

        /// <summary>How much continental interiors dry out. This is what puts deserts inland.</summary>
        public double Continentality = 0.85d;

        /// <summary>Number of tectonic plates. Fewer, larger plates give fewer, larger continents.</summary>
        public int PlateCount = 20;

        /// <summary>Share of plates that are continental. The main control on how much land exists.</summary>
        public double ContinentalFraction = 0.42d;

        /// <summary>A copy, so a panel can edit one and compare against the shipped defaults.</summary>
        public TerrainSettings Clone()
        {
            return (TerrainSettings)MemberwiseClone();
        }

        /// <summary>True when every value still matches a freshly constructed instance.</summary>
        public bool IsDefault()
        {
            var reference = new TerrainSettings();
            return ContinentFrequency == reference.ContinentFrequency
                && ShelfNoiseStrength == reference.ShelfNoiseStrength
                && MountainFrequency == reference.MountainFrequency
                && RangeAmplitude == reference.RangeAmplitude
                && HillFrequency == reference.HillFrequency
                && RollingAmplitude == reference.RollingAmplitude
                && DetailFrequency == reference.DetailFrequency
                && DetailAmplitude == reference.DetailAmplitude
                && LocalFrequency == reference.LocalFrequency
                && LocalAmplitude == reference.LocalAmplitude
                && MicroFrequency == reference.MicroFrequency
                && MicroAmplitude == reference.MicroAmplitude
                && MaximumSlope == reference.MaximumSlope
                && WarpFrequency == reference.WarpFrequency
                && WarpStrength == reference.WarpStrength
                && Lacunarity == reference.Lacunarity
                && Gain == reference.Gain
                && MoistureFrequency == reference.MoistureFrequency
                && ClimateNoiseFrequency == reference.ClimateNoiseFrequency
                && JitterFrequency == reference.JitterFrequency
                && TemperatureLatitudeWeight == reference.TemperatureLatitudeWeight
                && AltitudeCooling == reference.AltitudeCooling
                && MoistureContrast == reference.MoistureContrast
                && Continentality == reference.Continentality
                && PlateCount == reference.PlateCount
                && ContinentalFraction == reference.ContinentalFraction;
        }
    }
}

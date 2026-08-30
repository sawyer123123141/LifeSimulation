using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// How full a resource site looks: the height of its marker and the colour that marker is
    /// drawn in, given the amount standing in it and nothing else.
    ///
    /// <para><b>Why this is its own type.</b> P4a's acceptance criterion is that a player can see
    /// resource recovery "without reading logs", and the rule that makes that visible was four lines
    /// inside <c>Prototype1Presenter.Views.cs</c> - a place no test can reach and the capture harness
    /// does not run. So the only evidence it worked was that somebody had read it. The arithmetic is
    /// here, testable headlessly like <see cref="CreatureAppearance"/> and
    /// <see cref="CreaturePicking"/>, and the presenter and the arena capture both call it.</para>
    ///
    /// <para><b>Both callers, deliberately.</b> The capture used to build its own scene and drifted
    /// from the runtime, which turned its PNGs into evidence about something nobody was looking at.
    /// A shared rule is what stops a picture of a depleting patch being a picture of the capture's
    /// idea of one.</para>
    /// </summary>
    public readonly struct ResourceMarkerAppearance
    {
        public ResourceMarkerAppearance(float height, float red, float green, float blue, float fillFraction)
        {
            Height = height;
            Red = red;
            Green = green;
            Blue = blue;
            FillFraction = fillFraction;
        }

        /// <summary>Marker height in world units. Never zero: an empty site still marks the place it is.</summary>
        public float Height { get; }

        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }

        /// <summary>Amount over capacity, clamped to 0..1. Exposed so a test can say what it read.</summary>
        public float FillFraction { get; }

        /// <summary>An emptied site keeps this much of its height, so the place stays findable.</summary>
        public const float EmptyHeight = .08f;

        /// <summary>A full site's height.</summary>
        public const float FullHeight = .5f;

        /// <summary>How dark an emptied site's colour goes, as a fraction of its full colour.</summary>
        public const float EmptyColorFraction = .2f;

        public static ResourceMarkerAppearance For(ResourceKind kind, float amount, float capacity)
        {
            float fill = capacity <= 0f ? 0f : Clamp01(amount / capacity);
            float height = EmptyHeight + ((FullHeight - EmptyHeight) * fill);

            BaseColor(kind, out float red, out float green, out float blue);
            float tint = EmptyColorFraction + ((1f - EmptyColorFraction) * fill);
            return new ResourceMarkerAppearance(height, red * tint, green * tint, blue * tint, fill);
        }

        /// <summary>
        /// The colour a full site of this kind is drawn in. Food amber, water blue, anything else
        /// the carcass red - the same three the HUD legend documents.
        /// </summary>
        public static void BaseColor(ResourceKind kind, out float red, out float green, out float blue)
        {
            if (kind == ResourceKind.Food)
            {
                red = .95f;
                green = .72f;
                blue = .15f;
                return;
            }

            if (kind == ResourceKind.Water)
            {
                red = .15f;
                green = .65f;
                blue = 1f;
                return;
            }

            red = .55f;
            green = .12f;
            blue = .08f;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}

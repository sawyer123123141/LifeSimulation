using System.Collections.Generic;

namespace LifeSimulation.Presentation
{
    /// <summary>How much of the HUD is drawn. Cycled with Tab.</summary>
    public enum HudDetail
    {
        /// <summary>Everything. The default, and it needs 1,276 x 808 of screen.</summary>
        Full = 0,

        /// <summary>The status box, and the creature inspector when something is selected.</summary>
        Compact = 1,

        /// <summary>One line saying how to get it back.</summary>
        Hidden = 2,
    }

    public enum HudSection
    {
        Status,
        PopulationCondition,
        P5History,
        Inspector,
        SelectedCreatureHistory,
        TerrainControls,
        Help,
    }

    /// <summary>A HUD panel's box, in the same screen pixels <c>GUI.Box</c> uses.</summary>
    public readonly struct HudRect
    {
        public HudRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public float Right => X + Width;
        public float Bottom => Y + Height;

        /// <summary>Touching edges do not count: two panels may sit flush without one hiding the other.</summary>
        public bool Overlaps(HudRect other)
        {
            return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
        }
    }

    /// <summary>
    /// Which HUD panels are drawn, and where.
    ///
    /// <para><b>Why this is a type and not sixteen y values.</b> The HUD once drew four pairs of
    /// labels on top of each other in every configuration with the elevation field on - which was
    /// every ecosystem configuration - and nobody noticed because nobody had looked. The panel boxes
    /// live here so a test can assert that no two of them overlap in any mode and any flag
    /// combination, which is the thing that was silently untrue.</para>
    ///
    /// <para><b>Why <see cref="HudDetail"/> exists.</b> At full detail the HUD spans 1,276 x 808
    /// pixels. On a 1,280 x 720 play window that is the entire screen, the P5 panel runs off the
    /// right edge and the help lines run off the bottom, and there was no key in the arena to turn
    /// any of it off - <c>_hudHidden</c> was only ever set by the terrain viewer and the planet view.
    /// The user's report on 2026-08-30 was that the HUD "covers my entire screen basically".</para>
    ///
    /// <para>Pure and Unity-free on purpose, like <see cref="CreatureAppearance"/> and
    /// <see cref="ResourceMarkerAppearance"/>, so the layout is checkable without opening the
    /// editor.</para>
    /// </summary>
    public static class HudLayout
    {
        public static readonly HudRect Status = new HudRect(12f, 12f, 440f, 284f);
        public static readonly HudRect PopulationCondition = new HudRect(464f, 12f, 280f, 264f);
        public static readonly HudRect P5History = new HudRect(756f, 12f, 520f, 340f);
        public static readonly HudRect Inspector = new HudRect(12f, 300f, 440f, 324f);
        public static readonly HudRect SelectedCreatureHistory = new HudRect(464f, 300f, 280f, 268f);
        public static readonly HudRect TerrainControls = new HudRect(24f, 632f, 430f, 66f);
        public static readonly HudRect Help = new HudRect(24f, 698f, 1240f, 110f);

        public static HudDetail Next(HudDetail detail)
        {
            return detail == HudDetail.Full ? HudDetail.Compact
                : detail == HudDetail.Compact ? HudDetail.Hidden
                : HudDetail.Full;
        }

        public static string Describe(HudDetail detail)
        {
            return detail == HudDetail.Full ? "full"
                : detail == HudDetail.Compact ? "compact"
                : "hidden";
        }

        /// <summary>
        /// The panels drawn at this detail level.
        ///
        /// <para>Compact keeps the status box, because population, scenario and speed are what a
        /// watcher reads constantly, and the inspector when a creature is selected, because clicking
        /// one is a request to see it. Everything else is reference material that can be summoned
        /// back with Tab.</para>
        /// </summary>
        public static List<HudSection> VisibleSections(HudDetail detail, bool elevationFieldEnabled, bool creatureSelected)
        {
            var sections = new List<HudSection>();
            if (detail == HudDetail.Hidden) return sections;

            sections.Add(HudSection.Status);
            if (detail == HudDetail.Compact)
            {
                if (creatureSelected) sections.Add(HudSection.Inspector);
                return sections;
            }

            sections.Add(HudSection.PopulationCondition);
            sections.Add(HudSection.P5History);
            sections.Add(HudSection.Inspector);
            sections.Add(HudSection.SelectedCreatureHistory);
            if (elevationFieldEnabled) sections.Add(HudSection.TerrainControls);
            sections.Add(HudSection.Help);
            return sections;
        }

        public static HudRect RectOf(HudSection section)
        {
            switch (section)
            {
                case HudSection.Status: return Status;
                case HudSection.PopulationCondition: return PopulationCondition;
                case HudSection.P5History: return P5History;
                case HudSection.Inspector: return Inspector;
                case HudSection.SelectedCreatureHistory: return SelectedCreatureHistory;
                case HudSection.TerrainControls: return TerrainControls;
                default: return Help;
            }
        }

        /// <summary>The box every visible panel fits inside, which is what "it covers the screen" means as a number.</summary>
        public static HudRect Bounds(HudDetail detail, bool elevationFieldEnabled, bool creatureSelected)
        {
            List<HudSection> sections = VisibleSections(detail, elevationFieldEnabled, creatureSelected);
            if (sections.Count == 0) return new HudRect(0f, 0f, 0f, 0f);

            float left = float.MaxValue;
            float top = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MinValue;
            foreach (HudSection section in sections)
            {
                HudRect rect = RectOf(section);
                if (rect.X < left) left = rect.X;
                if (rect.Y < top) top = rect.Y;
                if (rect.Right > right) right = rect.Right;
                if (rect.Bottom > bottom) bottom = rect.Bottom;
            }

            return new HudRect(left, top, right - left, bottom - top);
        }
    }
}

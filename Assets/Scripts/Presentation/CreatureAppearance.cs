using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// What a creature should look like, given its genes and nothing else.
    ///
    /// <para>Selection is measurably running and none of it is visible: the view tints by current
    /// action and scales by one gene, so a run where the population shifts a quarter of the thermal
    /// range looks identical to one where nothing happens. The plan is
    /// <c>docs/creature-appearance.md</c>.</para>
    ///
    /// <para><b>This is deliberately the half that survives real models.</b> Deciding what a creature
    /// should look like is arithmetic over a <see cref="Genome"/>; applying it to whatever a creature
    /// is made of is not, and is confined to the few lines in <c>Prototype1Presenter.Views.cs</c> so
    /// a model swap edits one place. Nothing here knows about capsules, renderers, materials or
    /// <c>UnityEngine</c> - which is also why it is testable headlessly, like
    /// <see cref="FreeCameraMotion"/> and <see cref="PlanetChunkLod"/>.</para>
    ///
    /// <para>Colour is <b>not</b> to silently replace the action colours. The HUD legend documents
    /// them and they are how behaviour is read at a glance; genome tinting belongs behind a toggle,
    /// because two pictures of the same population answer two different questions.</para>
    /// </summary>
    public readonly struct CreatureAppearance
    {
        public CreatureAppearance(float red, float green, float blue, float scaleMultiplier)
        {
            Red = red;
            Green = green;
            Blue = blue;
            ScaleMultiplier = scaleMultiplier;
        }

        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }

        /// <summary>Multiplies the body scale the view already applies. Not a size in metres.</summary>
        public float ScaleMultiplier { get; }
    }
}

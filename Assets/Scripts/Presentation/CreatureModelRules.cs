using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// What kind of animal a creature reads as, derived from traits that are real today.
    ///
    /// <para><b>This is the seam the whole model pipeline turns on.</b> The simulation decides
    /// "this is a predator". It never decides "this is Wolf.fbx". Model files are chosen one layer
    /// out, in <see cref="CreatureModelCatalog"/>, so replacing the art is a table edit and not a
    /// code change.</para>
    ///
    /// <para><b>Three roles, not twelve.</b> `docs/creature-appearance.md` records the reason:
    /// there are no species yet, so assigning one model per animal would be arbitrary, and an
    /// arbitrary assignment teaches a viewer that model means decoration - a lesson that has to be
    /// un-taught when model should mean species. Three roles each map to a handful of models and
    /// every one of them is derived from a gene that already does something.</para>
    /// </summary>
    public enum CreatureModelRole : byte
    {
        /// <summary>Hunts. High aggression and a meat-leaning diet.</summary>
        Predator = 0,

        /// <summary>Large-bodied grazer - cattle, horses, alpacas.</summary>
        LargeHerbivore = 1,

        /// <summary>Small or light-bodied herbivore - deer and the like.</summary>
        SmallHerbivore = 2,
    }

    /// <summary>
    /// Pure mapping from genome to display role. No <c>UnityEngine</c> types, so it is unit-tested
    /// headlessly like <see cref="CreatureAppearanceRules"/>.
    /// </summary>
    public static class CreatureModelRules
    {
        /// <summary>
        /// Meat-leaning diet, on the same 0-1 scale <c>PredationSystem.HasViableHuntingStrategy</c>
        /// reads. Kept as a named constant because it is a display threshold, not a behaviour one -
        /// changing it must never change what a creature *does*.
        /// </summary>
        public const float PredatorDietThreshold = 0.55f;

        /// <summary>Aggression a creature needs before it reads as a hunter rather than a bully.</summary>
        public const float PredatorAggressionThreshold = 0.5f;

        /// <summary>Body size splitting the two herbivore silhouettes.</summary>
        public const float LargeBodyThreshold = 0.5f;

        public static CreatureModelRole SelectRole(Genome genome)
        {
            // Both conditions, deliberately. DietSpecialization alone would make every aggressive
            // grazer a wolf, and Aggression alone would make a placid carnivore a cow.
            if (genome.DietSpecialization >= PredatorDietThreshold
                && genome.Aggression >= PredatorAggressionThreshold)
            {
                return CreatureModelRole.Predator;
            }

            return genome.BodySize >= LargeBodyThreshold
                ? CreatureModelRole.LargeHerbivore
                : CreatureModelRole.SmallHerbivore;
        }

        /// <summary>
        /// Which model within a role a given creature uses. Keyed on the creature id so it is
        /// stable for that creature's whole life and identical across runs of the same seed -
        /// picking at random would make two runs of one seed look different, which would break the
        /// paired-seed comparison this project's screenshots are read against.
        /// </summary>
        public static int SelectVariant(long creatureId, int variantCount)
        {
            if (variantCount <= 1)
            {
                return 0;
            }

            // Non-negative modulo: creature ids are positive today, but a negative index here would
            // be an index-out-of-range at the one call site that instantiates a model.
            int index = (int)(creatureId % variantCount);
            return index < 0 ? index + variantCount : index;
        }
    }
}

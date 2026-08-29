using System;
using LifeSimulation.Simulation.Behavior;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// One model file and the clip names it uses.
    ///
    /// <para><b>Every clip is overridable, not just the attack one.</b> The current pack happens to
    /// share <c>Idle</c>, <c>Walk</c>, <c>Gallop</c>, <c>Death</c> and <c>Eating</c> across all
    /// twelve models, so today's table only has to name the attack clip. **That shared set is a
    /// property of this pack and not an assumption this type makes** - a different pack may call
    /// them <c>Run</c>, <c>Idle_A</c> or <c>Chomp</c>, and the failure mode of guessing wrong is
    /// silent: the animator is asked for a state that does not exist and nothing plays. Any clip
    /// left unspecified falls back to the current pack's name.</para>
    /// </summary>
    public readonly struct CreatureModelDefinition
    {
        public CreatureModelDefinition(
            string modelName,
            string attackClip = null,
            string idleClip = null,
            string walkClip = null,
            string gallopClip = null,
            string eatingClip = null,
            string deathClip = null)
        {
            ModelName = modelName;
            AttackClip = string.IsNullOrEmpty(attackClip) ? CreatureModelCatalog.DefaultAttackClip : attackClip;
            IdleClip = string.IsNullOrEmpty(idleClip) ? CreatureModelCatalog.IdleClip : idleClip;
            WalkClip = string.IsNullOrEmpty(walkClip) ? CreatureModelCatalog.WalkClip : walkClip;
            GallopClip = string.IsNullOrEmpty(gallopClip) ? CreatureModelCatalog.GallopClip : gallopClip;
            EatingClip = string.IsNullOrEmpty(eatingClip) ? CreatureModelCatalog.EatingClip : eatingClip;
            DeathClip = string.IsNullOrEmpty(deathClip) ? CreatureModelCatalog.DeathClip : deathClip;
        }

        /// <summary>File name without extension, resolved under <see cref="CreatureModelCatalog.ResourcePath"/>.</summary>
        public string ModelName { get; }

        public string AttackClip { get; }

        public string IdleClip { get; }

        public string WalkClip { get; }

        public string GallopClip { get; }

        public string EatingClip { get; }

        public string DeathClip { get; }

        public bool IsValid => !string.IsNullOrEmpty(ModelName);
    }

    /// <summary>
    /// Which model files stand for which role, and which animation clip stands for which action.
    ///
    /// <para><b>This is the swap point.</b> Replacing the art means editing the three arrays below
    /// and dropping different files into <c>Assets/Resources/CreatureModels</c>. No other file
    /// changes - not the view code, not the role rules, not the appearance mapping. Moving later to
    /// one model per species means changing <see cref="CreatureModelRules.SelectRole"/> to return a
    /// cluster id and re-keying this table; the call site does not move either way.</para>
    ///
    /// <para><b>Currently the CC0 Quaternius Ultimate Animated Animals pack</b> - twelve models, all
    /// of which happen to share <c>Idle</c>, <c>Walk</c>, <c>Gallop</c>, <c>Death</c> and
    /// <c>Eating</c>, so only the attack clip has to be named per model here. **Do not read that as
    /// a rule.** A future pack need share no clip name with this one; every clip is overridable per
    /// model precisely so that swapping packs stays a table edit.</para>
    ///
    /// <para>No <c>UnityEngine</c> types here on purpose, so the whole mapping is unit-tested
    /// headlessly. Loading the actual prefab is the view layer's job.</para>
    /// </summary>
    public static class CreatureModelCatalog
    {
        /// <summary>Folder under <c>Assets/Resources</c>, in the form <c>Resources.Load</c> wants.</summary>
        public const string ResourcePath = "CreatureModels";

        // The current pack's names. These are DEFAULTS a model may override, not a contract every
        // pack must satisfy - see CreatureModelDefinition.
        public const string IdleClip = "Idle";
        public const string WalkClip = "Walk";
        public const string GallopClip = "Gallop";
        public const string EatingClip = "Eating";
        public const string DeathClip = "Death";
        public const string DefaultAttackClip = "Attack";

        private const string CarnivoreAttack = DefaultAttackClip;
        private const string HoofedAttack = "Attack_Headbutt";

        private static readonly CreatureModelDefinition[] Predators =
        {
            new CreatureModelDefinition("Wolf", CarnivoreAttack),
            new CreatureModelDefinition("Fox", CarnivoreAttack),
            new CreatureModelDefinition("Husky", CarnivoreAttack),
            new CreatureModelDefinition("ShibaInu", CarnivoreAttack),
        };

        private static readonly CreatureModelDefinition[] LargeHerbivores =
        {
            new CreatureModelDefinition("Cow", HoofedAttack),
            new CreatureModelDefinition("Bull", HoofedAttack),
            new CreatureModelDefinition("Horse", HoofedAttack),
            new CreatureModelDefinition("Horse_White", HoofedAttack),
            new CreatureModelDefinition("Donkey", HoofedAttack),
            new CreatureModelDefinition("Alpaca", HoofedAttack),
        };

        private static readonly CreatureModelDefinition[] SmallHerbivores =
        {
            new CreatureModelDefinition("Deer", HoofedAttack),
            new CreatureModelDefinition("Stag", HoofedAttack),
        };

        public static CreatureModelDefinition[] ModelsFor(CreatureModelRole role)
        {
            switch (role)
            {
                case CreatureModelRole.Predator: return Predators;
                case CreatureModelRole.LargeHerbivore: return LargeHerbivores;
                case CreatureModelRole.SmallHerbivore: return SmallHerbivores;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, "Unmapped display role.");
            }
        }

        public static CreatureModelDefinition Select(CreatureModelRole role, long creatureId)
        {
            CreatureModelDefinition[] models = ModelsFor(role);
            return models[CreatureModelRules.SelectVariant(creatureId, models.Length)];
        }

        /// <summary>
        /// The clip a creature should be playing for what it is currently doing.
        ///
        /// <para>Travelling actions all read as <c>Walk</c> and the two urgent ones as
        /// <c>Gallop</c>, which is the distinction a viewer can actually see from a distance -
        /// "that animal is in a hurry" is legible, "that animal is seeking water rather than food"
        /// is not, and colour already carries the second.</para>
        /// </summary>
        public static string ClipFor(CreatureAction action, in CreatureModelDefinition model)
        {
            switch (action)
            {
                case CreatureAction.Flee:
                case CreatureAction.SeekPrey:
                    return model.GallopClip;

                case CreatureAction.Attack:
                    return model.AttackClip;

                case CreatureAction.Eat:
                case CreatureAction.Drink:
                case CreatureAction.FeedCarcass:
                    return model.EatingClip;

                case CreatureAction.Rest:
                case CreatureAction.Reproduce:
                    return model.IdleClip;

                default:
                    return model.WalkClip;
            }
        }
    }
}

using System.Collections.Generic;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The seam between "what a creature is" and "which model file draws it".
    ///
    /// <para>The rules are pure so they can be pinned here rather than judged by eye in Play mode,
    /// which is the same reason <c>CreatureAppearanceRules</c> is a pure function. The point of
    /// pinning them is that the art is expected to be replaced: these tests must keep passing
    /// across a model swap, because nothing here names a file that a swap would remove except
    /// through <see cref="CreatureModelCatalog"/>.</para>
    /// </summary>
    public sealed class CreatureModelRulesTests
    {
        private static Genome Genome(float diet = 0f, float aggression = 0f, float bodySize = 0.5f)
        {
            return new Genome(
                bodySize: bodySize,
                movementSpeed: 0.5f,
                metabolicPace: 0.5f,
                visionRange: 0.5f,
                waterEfficiency: 0.5f,
                foodEfficiency: 0.5f,
                aggression: aggression,
                dietSpecialization: diet);
        }

        [Test]
        public void AMeatEaterThatWillFightReadsAsAPredator()
        {
            Assert.That(
                CreatureModelRules.SelectRole(Genome(diet: 0.9f, aggression: 0.9f)),
                Is.EqualTo(CreatureModelRole.Predator));
        }

        [Test]
        public void AnAggressiveGrazerIsNotAPredator()
        {
            // Aggression alone must not do it, or every bad-tempered cow becomes a wolf.
            Assert.That(
                CreatureModelRules.SelectRole(Genome(diet: 0.05f, aggression: 0.95f)),
                Is.Not.EqualTo(CreatureModelRole.Predator));
        }

        [Test]
        public void APlacidCarnivoreIsNotAPredator()
        {
            // And diet alone must not do it either. Both conditions, deliberately.
            Assert.That(
                CreatureModelRules.SelectRole(Genome(diet: 0.95f, aggression: 0.05f)),
                Is.Not.EqualTo(CreatureModelRole.Predator));
        }

        [Test]
        public void HerbivoresSplitOnBodySize()
        {
            Assert.That(CreatureModelRules.SelectRole(Genome(bodySize: 0.9f)), Is.EqualTo(CreatureModelRole.LargeHerbivore));
            Assert.That(CreatureModelRules.SelectRole(Genome(bodySize: 0.1f)), Is.EqualTo(CreatureModelRole.SmallHerbivore));
        }

        [Test]
        public void AVariantIsStableForACreatureAndInRange()
        {
            // Stability matters more than variety: picking at random would make two runs of the
            // same seed look different, and this project reads screenshots against paired seeds.
            for (long id = 0; id < 50; id++)
            {
                int first = CreatureModelRules.SelectVariant(id, 4);
                Assert.That(first, Is.EqualTo(CreatureModelRules.SelectVariant(id, 4)));
                Assert.That(first, Is.InRange(0, 3));
            }
        }

        [Test]
        public void ASingleModelRoleAlwaysPicksIt()
        {
            Assert.That(CreatureModelRules.SelectVariant(12345L, 1), Is.Zero);
            Assert.That(CreatureModelRules.SelectVariant(12345L, 0), Is.Zero);
        }

        [Test]
        public void EveryRoleHasAtLeastOneModel()
        {
            foreach (CreatureModelRole role in System.Enum.GetValues(typeof(CreatureModelRole)))
            {
                Assert.That(CreatureModelCatalog.ModelsFor(role), Is.Not.Empty, $"{role} has no model");
            }
        }

        [Test]
        public void EveryModelNamesItsOwnAttackClip()
        {
            // The one clip that varies across the pack. A model with no attack clip named would
            // silently play nothing when it fought.
            foreach (CreatureModelRole role in System.Enum.GetValues(typeof(CreatureModelRole)))
            {
                foreach (CreatureModelDefinition model in CreatureModelCatalog.ModelsFor(role))
                {
                    Assert.That(model.IsValid, Is.True);
                    Assert.That(model.AttackClip, Is.Not.Null.And.Not.Empty, model.ModelName);
                }
            }
        }

        [Test]
        public void NoModelIsListedUnderTwoRoles()
        {
            var seen = new HashSet<string>();
            foreach (CreatureModelRole role in System.Enum.GetValues(typeof(CreatureModelRole)))
            {
                foreach (CreatureModelDefinition model in CreatureModelCatalog.ModelsFor(role))
                {
                    Assert.That(seen.Add(model.ModelName), Is.True, $"{model.ModelName} appears twice");
                }
            }
        }

        [Test]
        public void EveryActionResolvesToAClip()
        {
            CreatureModelDefinition wolf = CreatureModelCatalog.Select(CreatureModelRole.Predator, 0);
            foreach (CreatureAction action in System.Enum.GetValues(typeof(CreatureAction)))
            {
                Assert.That(CreatureModelCatalog.ClipFor(action, wolf), Is.Not.Null.And.Not.Empty, action.ToString());
            }
        }

        [Test]
        public void UrgentActionsGallopAndCalmOnesDoNot()
        {
            CreatureModelDefinition deer = CreatureModelCatalog.Select(CreatureModelRole.SmallHerbivore, 0);
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.Flee, deer), Is.EqualTo(CreatureModelCatalog.GallopClip));
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.SeekPrey, deer), Is.EqualTo(CreatureModelCatalog.GallopClip));
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.SeekFood, deer), Is.EqualTo(CreatureModelCatalog.WalkClip));
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.Rest, deer), Is.EqualTo(CreatureModelCatalog.IdleClip));
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.Eat, deer), Is.EqualTo(CreatureModelCatalog.EatingClip));
        }

        [Test]
        public void AHoofedModelUsesItsOwnAttackClipRatherThanTheCarnivoreOne()
        {
            CreatureModelDefinition deer = CreatureModelCatalog.Select(CreatureModelRole.SmallHerbivore, 0);
            CreatureModelDefinition wolf = CreatureModelCatalog.Select(CreatureModelRole.Predator, 0);
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.Attack, deer), Is.EqualTo("Attack_Headbutt"));
            Assert.That(CreatureModelCatalog.ClipFor(CreatureAction.Attack, wolf), Is.EqualTo("Attack"));
        }
    }
}

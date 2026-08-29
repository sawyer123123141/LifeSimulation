using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    public sealed partial class Prototype1Presenter : MonoBehaviour
    {

        private void CaptureRecentEvent()
        {
            if (_world.Events.Count > 0)
            {
                _recentEvent = _world.Events.GetAt(_world.Events.Count - 1);
                _hasRecentEvent = true;
            }
        }

        private void CreateResourceView(ResourceState resource)
        {
            PrimitiveType primitive = resource.Kind == ResourceKind.Food
                ? PrimitiveType.Cylinder
                : resource.Kind == ResourceKind.Water ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var view = GameObject.CreatePrimitive(primitive);
            view.name = resource.Kind.ToString();
            view.transform.position = new Vector3(resource.Position.X, GroundHeightAt(resource.Position.X, resource.Position.Y) + 0.25f, resource.Position.Y);
            view.transform.localScale = new Vector3(2f, 0.5f, 2f);
            var renderer = view.GetComponent<Renderer>();
            renderer.material.color = GetResourceColor(resource.Kind);
            _resourceViews.Add(view.transform);
            _resourceRenderers.Add(renderer);
        }

        private void SynchronizeResourceViews()
        {
            for (int index = 0; index < _world.Resources.Count; index++)
            {
                var resource = _world.Resources.GetAt(index);
                if (index >= _resourceViews.Count)
                {
                    CreateResourceView(resource);
                }

                Transform view = _resourceViews[index];
                float fraction = resource.Capacity <= 0f ? 0f : resource.Amount / resource.Capacity;
                float height = Mathf.Lerp(0.08f, 0.5f, fraction);
                view.position = ArenaProjection.ToWorld(
                    resource.Position.X, resource.Position.Y,
                    GroundHeightAt(resource.Position.X, resource.Position.Y) + (height * 0.5f));
                view.rotation = ArenaProjection.Upright(resource.Position.X, resource.Position.Y);
                view.localScale = new Vector3(2f, height, 2f);
                Color baseColor = GetResourceColor(resource.Kind);
                _resourceRenderers[index].material.color = Color.Lerp(baseColor * 0.2f, baseColor, fraction);
            }
        }

        private static Color GetResourceColor(ResourceKind kind)
        {
            if (kind == ResourceKind.Food) return new Color(0.95f, 0.72f, 0.15f);
            if (kind == ResourceKind.Water) return new Color(0.15f, 0.65f, 1f);
            return new Color(0.55f, 0.12f, 0.08f);
        }

        private void SynchronizePresentation()
        {
            RemoveStaleCreatureViews();
            SynchronizeResourceViews();
            for (int index = 0; index < _world.CreatureCount; index++)
            {
                CreatureId id = _world.GetCreatureIdAt(index);
                if (!_creatureViews.TryGetValue(id, out Transform view))
                {
                    view = CreateCreatureView(id, _world.Creatures.GetGenomeAt(index));
                    // Born while a preview or the planet view is up: stay hidden until the arena
                    // comes back. The planet view pauses the world, so this is only reachable if
                    // something spawns a creature while paused - but a view that appears in the
                    // middle of a globe shot is exactly the sort of thing nobody notices until it
                    // is in a screenshot.
                    if (_sphericalArena
                        || (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off))
                    {
                        view.gameObject.SetActive(false);
                    }
                }

                var movement = _world.GetCreatureMovementAt(index);
                CreatureAction action = _world.GetCreatureDecisionAt(index).Action;
                float groundHeight = GroundHeightAt(movement.Position.X, movement.Position.Y);
                view.position = ArenaProjection.ToWorld(
                    movement.Position.X, movement.Position.Y, groundHeight + 0.55f);
                view.rotation = ArenaProjection.Upright(movement.Position.X, movement.Position.Y)
                    * Quaternion.Euler(0f, HeadingYaw(id, movement), 0f);
                float ageScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_world.GetCreatureNeedsAt(index).Age / 4f));
                float bodyScale = Mathf.Lerp(0.7f, 1.35f, _world.Creatures.GetGenomeAt(index).BodySize);
                bool hasModel = _creatureModels.TryGetValue(id, out CreatureModelDefinition model);
                float modelScale = hasModel ? model.ModelScale : 1f;
                view.localScale = Vector3.one * (GetActionScale(action) * ageScale * bodyScale * modelScale);

                // Only the capsule fallback is tinted. A model carries its own colours - the pack's
                // wolf is grey, its fox orange, its cow brown - and those already say "which animal
                // is this" better than a flat action colour would, while ACTION is now carried by
                // the animation instead. Repainting them would also be wrong twice over: these
                // meshes have four to eight materials each and `renderer.material` touches only the
                // first, so a tinted wolf would come out partly grey and partly gold; and touching
                // `.material` per creature instantiates a material per creature, which at the
                // populations this ecology reaches is exactly the batching cost worth avoiding.
                if (!hasModel)
                {
                    _creatureRenderers[id].material.color = GetActionColor(action);
                }

                PlayActionAnimation(id, action, model);
            }
        }

        /// <summary>
        /// Builds the view for one creature: its model if the pack is present, a capsule if not.
        ///
        /// <para><b>This is the only place that knows a mesh exists.</b> Which model a creature
        /// gets is decided by <see cref="CreatureModelRules"/> from its genome and looked up in
        /// <see cref="CreatureModelCatalog"/>, so replacing the art is a table edit and never
        /// reaches this method.</para>
        ///
        /// <para><b>The capsule path is not dead code.</b> A clone with no model pack, or a pack
        /// whose files are named differently, must still produce a running world rather than a
        /// scene full of nulls - so a failed load falls back rather than throwing.</para>
        /// </summary>
        private Transform CreateCreatureView(CreatureId id, Genome genome)
        {
            CreatureModelRole role = CreatureModelRules.SelectRole(genome);
            CreatureModelDefinition model = CreatureModelCatalog.Select(role, id.Value);

            var prefab = Resources.Load<GameObject>($"{CreatureModelCatalog.ResourcePath}/{model.ModelName}");
            Transform view;
            if (prefab == null)
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                model = default;
            }
            else
            {
                view = Instantiate(prefab).transform;
                _creatureModels.Add(id, model);

                Animation animation = view.GetComponentInChildren<Animation>();
                if (animation != null)
                {
                    animation.playAutomatically = false;
                    _creatureAnimations.Add(id, animation);
                }
            }

            view.name = $"Creature {id.Value}";
            _creatureViews.Add(id, view);

            Renderer renderer = view.GetComponentInChildren<Renderer>();
            _creatureRenderers.Add(id, renderer);
            return view;
        }

        /// <summary>
        /// Crossfades to the clip for what the creature is doing, and only when that changes.
        ///
        /// <para>Calling play every frame would restart the clip every frame and freeze every
        /// creature on its first pose - the animation equivalent of the per-frame
        /// <c>GetComponent</c> this file used to do.</para>
        ///
        /// <para>Playback is deliberately confined to this one method. It is the only code that
        /// knows the pack is driven through the legacy <c>Animation</c> component rather than an
        /// <c>AnimatorController</c>, so moving to Mecanim later is a change here and nowhere
        /// else.</para>
        /// </summary>
        private void PlayActionAnimation(CreatureId id, CreatureAction action, in CreatureModelDefinition model)
        {
            if (!_creatureAnimations.TryGetValue(id, out Animation animation) || animation == null)
            {
                return;
            }

            if (_creatureActions.TryGetValue(id, out CreatureAction previous) && previous == action)
            {
                return;
            }

            _creatureActions[id] = action;
            string clip = CreatureModelCatalog.ClipFor(action, model);

            // A missing clip is the silent failure this whole pipeline is built to avoid, so it is
            // checked rather than assumed - CrossFade on an absent state logs nothing and does
            // nothing. CreatureModelImportReport.Validate proves the table matches the assets; this
            // keeps a mismatched pack from being invisible at runtime too.
            if (animation.GetClip(clip) == null)
            {
                return;
            }

            animation.CrossFade(clip, 0.15f);
        }

        /// <summary>
        /// Which way a creature is facing, from the step it just took.
        ///
        /// <para>The simulation stores no heading - it does not need one, since movement is a
        /// position update - so it is recovered from the difference between the previous and
        /// current position. Creatures used to face a fixed direction regardless of where they were
        /// going, which is why a herd crossing the map read as a set of statues sliding sideways.</para>
        ///
        /// <para>A stationary creature keeps its last heading rather than snapping to zero: the
        /// step is exactly zero whenever it is eating, drinking or resting, and a model that spins
        /// to face north every time it stops to graze looks broken in a way the numbers never
        /// showed.</para>
        /// </summary>
        private float HeadingYaw(CreatureId id, in MovementState movement)
        {
            float deltaX = movement.Position.X - movement.PreviousPosition.X;
            float deltaY = movement.Position.Y - movement.PreviousPosition.Y;

            // Squared, to avoid a square root on every creature every frame. The threshold is well
            // below a single frame's travel for the slowest creature.
            if ((deltaX * deltaX) + (deltaY * deltaY) > 1e-8f)
            {
                // Arena Y is world Z; Unity yaw is measured from +Z toward +X.
                float yaw = Mathf.Atan2(deltaX, deltaY) * Mathf.Rad2Deg;
                _creatureHeadings[id] = yaw;
                return yaw;
            }

            return _creatureHeadings.TryGetValue(id, out float previous) ? previous : 0f;
        }

        private void RemoveStaleCreatureViews()
        {
            _staleCreatureIds.Clear();
            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (!_world.TryGetCreatureIndex(pair.Key, out _))
                {
                    if (_hasSelectedCreature && pair.Key.Equals(_selectedCreature))
                    {
                        _hasSelectedCreature = false;
                    }

                    Destroy(pair.Value.gameObject);
                    _staleCreatureIds.Add(pair.Key);
                    _creatureRenderers.Remove(pair.Key);
                    _creatureAnimations.Remove(pair.Key);
                    _creatureActions.Remove(pair.Key);
                    _creatureModels.Remove(pair.Key);
                    _creatureHeadings.Remove(pair.Key);
                }
            }

            for (int index = 0; index < _staleCreatureIds.Count; index++)
            {
                _creatureViews.Remove(_staleCreatureIds[index]);
            }
        }

        private static Color GetActionColor(CreatureAction action)
        {
            switch (action)
            {
                case CreatureAction.SeekFood:
                case CreatureAction.Eat:
                    return new Color(1f, 0.72f, 0.12f);
                case CreatureAction.SeekWater:
                case CreatureAction.Drink:
                    return new Color(0.15f, 0.68f, 1f);
                case CreatureAction.Reproduce:
                case CreatureAction.SeekMate:
                    return new Color(0.9f, 0.25f, 0.9f);
                case CreatureAction.SeekPrey:
                case CreatureAction.Attack:
                    return new Color(0.98f, 0.2f, 0.16f);
                case CreatureAction.Flee:
                    return new Color(0.2f, 0.95f, 0.95f);
                case CreatureAction.SeekThermalComfort:
                    return new Color(1f, 0.45f, 0.08f);

                // Scavenging and resting used to fall through to the wander colour, so a creature
                // crossing the map to a carcass and one milling about looked identical. Both are
                // actions IntentUtilityV1 actually emits - CreatureIntent.SeekCarcass and .Rest -
                // so this was three behaviours sharing one colour.
                case CreatureAction.SeekCarcass:
                case CreatureAction.FeedCarcass:
                    return new Color(0.55f, 0.35f, 0.22f);
                case CreatureAction.Rest:
                    return new Color(0.55f, 0.6f, 0.55f);
                default:
                    return new Color(0.35f, 0.92f, 0.45f);
            }
        }

        private static float GetActionScale(CreatureAction action)
        {
            if (action == CreatureAction.Eat || action == CreatureAction.Drink || action == CreatureAction.Reproduce || action == CreatureAction.Attack)
            {
                return 1.12f + (0.08f * Mathf.Sin(Time.unscaledTime * 8f));
            }

            return 1f;
        }

        /// <summary>
        /// Keep the history pointed at whatever is selected. Called once per simulated step, so the
        /// history's resolution is the tick rather than the frame and is therefore independent of
        /// frame rate and of the speed multiplier.
        /// </summary>
        private void ObserveSelectedCreature()
        {
            if (!_hasSelectedCreature)
            {
                if (_selectedCreatureHistory.IsTracking) _selectedCreatureHistory.Clear();
                return;
            }

            _selectedCreatureHistory.Track(_selectedCreature);
            _selectedCreatureHistory.Observe(_world);
        }
    }
}

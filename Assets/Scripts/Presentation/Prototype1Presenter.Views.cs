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
            view.GetComponent<Renderer>().material.color = GetResourceColor(resource.Kind);
            _resourceViews.Add(view.transform);
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
                view.GetComponent<Renderer>().material.color = Color.Lerp(baseColor * 0.2f, baseColor, fraction);
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
                    view = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                    view.name = $"Creature {id.Value}";
                    float hue = (id.Value * 0.61803398875f) % 1f;
                    view.GetComponent<Renderer>().material.color = Color.HSVToRGB(hue, 0.55f, 0.95f);
                    _creatureViews.Add(id, view);
                    if (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off)
                    {
                        // Born while a preview is up: stay hidden until the arena comes back.
                        view.gameObject.SetActive(false);
                    }
                }

                var movement = _world.GetCreatureMovementAt(index);
                CreatureAction action = _world.GetCreatureDecisionAt(index).Action;
                float groundHeight = GroundHeightAt(movement.Position.X, movement.Position.Y);
                view.position = ArenaProjection.ToWorld(
                    movement.Position.X, movement.Position.Y, groundHeight + 0.55f);
                view.rotation = ArenaProjection.Upright(movement.Position.X, movement.Position.Y);
                float ageScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_world.GetCreatureNeedsAt(index).Age / 4f));
                float bodyScale = Mathf.Lerp(0.7f, 1.35f, _world.Creatures.GetGenomeAt(index).BodySize);
                view.localScale = Vector3.one * (GetActionScale(action) * ageScale * bodyScale);
                view.GetComponent<Renderer>().material.color = GetActionColor(action);
            }
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

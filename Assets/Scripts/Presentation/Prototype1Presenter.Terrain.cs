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

        /// <summary>
        /// Elevation as a readable relief map: everything below <see cref="OverlaySeaLevel"/> reads
        /// as water, and land ramps beach -> grass -> rock -> snow. Banded rather than a smooth
        /// gradient because the point of looking at terrain is to see where the contours are, and a
        /// continuous ramp hides exactly the ridge structure the ridged-multifractal field exists to
        /// produce.
        /// </summary>
        private static Color ShadeElevation(float elevation)
        {
            if (elevation <= OverlaySeaLevel)
            {
                float depth = OverlaySeaLevel <= 0f ? 0f : Mathf.Clamp01(elevation / OverlaySeaLevel);
                return Color.Lerp(new Color(0.043f, 0.129f, 0.278f), new Color(0.176f, 0.408f, 0.616f), depth);
            }

            float land = Mathf.Clamp01((elevation - OverlaySeaLevel) / (1f - OverlaySeaLevel));
            if (land < 0.08f) return Color.Lerp(new Color(0.827f, 0.776f, 0.573f), new Color(0.573f, 0.678f, 0.376f), land / 0.08f);
            if (land < 0.45f) return Color.Lerp(new Color(0.573f, 0.678f, 0.376f), new Color(0.353f, 0.478f, 0.278f), (land - 0.08f) / 0.37f);
            if (land < 0.78f) return Color.Lerp(new Color(0.353f, 0.478f, 0.278f), new Color(0.478f, 0.451f, 0.412f), (land - 0.45f) / 0.33f);
            return Color.Lerp(new Color(0.478f, 0.451f, 0.412f), Color.white, (land - 0.78f) / 0.22f);
        }

        /// <summary>
        /// Ground height under a simulation position, in Unity units, read from the cached height
        /// grid with bilinear interpolation.
        ///
        /// <para><b>Cosmetic only.</b> The simulation is a flat plane: every position is a
        /// <c>SimVector2</c>, distance is 2D, and nothing in <c>Assets/Scripts/Simulation</c> knows
        /// this function exists. Creatures are drawn standing on the relief; they do not walk up it,
        /// and a hill costs them nothing. Making elevation affect movement is a simulation change -
        /// flag, tests and an experiment - not a presentation one.</para>
        ///
        /// <para>Reading the cache rather than resampling matters twice over: creatures land exactly
        /// on the drawn surface rather than on a separately computed one, and a creature costs an
        /// array read per frame instead of a noise evaluation.</para>
        /// </summary>
        private float GroundHeightAt(float x, float z)
        {
            if (_terrainHeights == null || _world == null || !_world.Config.ElevationFieldEnabled) return 0f;

            int side = TerrainMeshBuilder.PatchResolution;
            float u = Mathf.Clamp01((x + TerrainHalfWidth) / (2f * TerrainHalfWidth)) * (side - 1);
            float v = Mathf.Clamp01((z + TerrainHalfWidth) / (2f * TerrainHalfWidth)) * (side - 1);
            int column = Mathf.Clamp((int)u, 0, side - 2);
            int row = Mathf.Clamp((int)v, 0, side - 2);
            float fx = u - column;
            float fz = v - row;

            float bottomLeft = _terrainHeights[row * side + column];
            float bottomRight = _terrainHeights[row * side + column + 1];
            float topLeft = _terrainHeights[(row + 1) * side + column];
            float topRight = _terrainHeights[(row + 1) * side + column + 1];
            return Mathf.Lerp(Mathf.Lerp(bottomLeft, bottomRight, fx), Mathf.Lerp(topLeft, topRight, fx), fz);
        }

        /// <summary>Sample the field once per grid cell. Only ever redone when the world changes.</summary>
        private void EnsureRawElevation(int side, int cells)
        {
            if (_rawElevationValid && _rawElevation != null && _rawElevation.Length == cells && _rawElevationSeed == _world.Config.WorldSeed)
            {
                return;
            }

            if (_rawElevation == null || _rawElevation.Length != cells) _rawElevation = new float[cells];
            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-TerrainHalfWidth, TerrainHalfWidth, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-TerrainHalfWidth, TerrainHalfWidth, column / (float)(side - 1));
                    _rawElevation[row * side + column] = _world.Environment.Sample(new SimVector2(x, z)).Elevation;
                }
            }

            _rawElevationSeed = _world.Config.WorldSeed;
            _rawElevationValid = true;
            _blurredForRadius = -1f;
        }

        /// <summary>
        /// Blur the cached samples on the grid. One sample per cell, then N passes of a 3x3
        /// neighbourhood where N is the radius in cells - correct at any radius, and its cost does
        /// not grow with the radius. Recomputed only when the radius actually changes.
        /// </summary>
        private void EnsureBlurredElevation(int side, int cells)
        {
            if (_blurredElevation != null && _blurredElevation.Length == cells && Mathf.Approximately(_blurredForRadius, _terrainSmoothingRadius))
            {
                return;
            }

            if (_blurredElevation == null || _blurredElevation.Length != cells) _blurredElevation = new float[cells];
            System.Array.Copy(_rawElevation, _blurredElevation, cells);

            float cellSize = 2f * TerrainHalfWidth / (side - 1);
            int passes = Mathf.Clamp(Mathf.RoundToInt(_terrainSmoothingRadius / Mathf.Max(cellSize, 0.0001f)), 0, 32);
            var scratch = new float[cells];
            float[] source = _blurredElevation;
            for (int pass = 0; pass < passes; pass++)
            {
                for (int row = 0; row < side; row++)
                {
                    for (int column = 0; column < side; column++)
                    {
                        float total = 0f;
                        int taps = 0;
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int sampleRow = row + dz;
                            if (sampleRow < 0 || sampleRow >= side) continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int sampleColumn = column + dx;
                                if (sampleColumn < 0 || sampleColumn >= side) continue;
                                total += source[sampleRow * side + sampleColumn];
                                taps++;
                            }
                        }

                        scratch[row * side + column] = total / taps;
                    }
                }

                float[] swap = source;
                source = scratch;
                scratch = swap;
            }

            if (!ReferenceEquals(source, _blurredElevation)) System.Array.Copy(source, _blurredElevation, cells);
            _blurredForRadius = _terrainSmoothingRadius;
        }

        /// <summary>
        /// Water sits just above y=0, which is where the ground sits once everything at or below sea
        /// level has been flattened. Hidden entirely when there is no elevation field, since a sea
        /// over a flat world would be covering the whole arena.
        /// </summary>
        private void UpdateWaterSurface()
        {
            if (_waterSurface == null) return;

            bool hasTerrain = _world != null && _world.Config.ElevationFieldEnabled;

            // Curved, the sea is the planet's ocean sphere rather than a local surface: a flat sheet
            // over a curved patch cuts through the ground at the edges of the view, which reads as
            // the sea flooding uphill.
            _waterSurface.SetActive(hasTerrain && !_sphericalArena);
            _waterSurface.transform.position = Vector3.zero;
        }

        /// <summary>
        /// Show or hide everything that belongs to the arena when a preview opens or closes.
        ///
        /// <para>Hiding the ground alone was not enough: creatures, resources and the sea stayed
        /// where they were, so the planet appeared to hover over a field of animals floating in
        /// empty space. A preview replaces the scene rather than being added to it.</para>
        ///
        /// <para>The camera is re-framed too, because its zoom ceiling and pan clamp are sized for a
        /// 50-unit arena - a 400-unit patch could not be pulled away from far enough to see, which
        /// is why it read as a featureless flat plane.</para>
        /// </summary>
        private void ApplyTerrainPreviewMode(TerrainPreview.Mode mode)
        {
            bool arenaVisible = mode == TerrainPreview.Mode.Off;

            // A preview REPLACES the scene rather than being added to it. Hiding only the ground left
            // creatures, resources and the sea floating in the middle of a planet.
            _terrainRenderer.enabled = arenaVisible;
            if (_waterSurface != null)
            {
                _waterSurface.SetActive(arenaVisible && _world != null && _world.Config.ElevationFieldEnabled);
            }

            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (pair.Value != null) pair.Value.gameObject.SetActive(arenaVisible);
            }

            for (int index = 0; index < _resourceViews.Count; index++)
            {
                if (_resourceViews[index] != null) _resourceViews[index].gameObject.SetActive(arenaVisible);
            }

            // The HUD is laid out in fixed pixels and covers most of a small Game view, so at the
            // resolutions this is actually looked at the terrain was mostly hidden behind panels.
            // A viewer you cannot see is not a viewer.
            _hudHidden = !arenaVisible;

            var cameraController = _simulationCamera == null ? null : _simulationCamera.GetComponent<FreeFlyCameraController>();
            if (cameraController == null) return;

            if (arenaVisible) cameraController.ResetFrame();
            else cameraController.Frame(_terrainPreview.FramingRadius);
        }

        /// <summary>
        /// Enter or leave the planet view.
        ///
        /// <para><b>O used to only add.</b> It curved the arena onto its planet and drew the globe,
        /// and left everything else exactly where it was: creatures walking about, resources, the
        /// HUD over the top, and the simulation ticking away underneath a view nobody was watching
        /// the simulation in. A mode that only adds is not a mode.</para>
        ///
        /// <para>What is hidden and what is not: creatures, resources, the sea and the HUD go,
        /// because none of them belong in a view of a whole world. <b>The arena ground stays</b> -
        /// it is the finest patch of the planet's surface, and the backdrop deliberately drops the
        /// chunks underneath it, so hiding it would open a hole where the arena is.</para>
        ///
        /// <para>The simulation pauses, and the pause state from before is restored on the way out,
        /// so leaving the planet view does not silently start a run somebody had deliberately
        /// stopped.</para>
        /// </summary>
        /// <summary>How long the tuning values must sit still before the planet is re-meshed.</summary>
        private const float PlanetRebuildSettle = 0.2f;

        /// <summary>
        /// Re-mesh the planet if a tuning change has settled. Called every frame; costs a comparison.
        /// </summary>
        private void UpdatePlanetRebuild()
        {
            if (_planetRebuildDue <= 0f || Time.unscaledTime < _planetRebuildDue) return;

            _planetRebuildDue = 0f;
            EnsureArenaPlates();
            UpdatePlanetBackdrop();
        }

        private void ApplyPlanetView(bool active)
        {
            if (active)
            {
                // A flat preview and the planet are two different scenes; opening one closes the
                // other rather than drawing both into the same frame.
                if (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off)
                {
                    _terrainPreview.Hide();
                    ApplyTerrainPreviewMode(TerrainPreview.Mode.Off);
                }

                _pausedBeforePlanetView = _isPaused;
                _isPaused = true;
            }
            else
            {
                _isPaused = _pausedBeforePlanetView;
            }

            _hudHidden = active;

            if (_waterSurface != null)
            {
                _waterSurface.SetActive(!active && _world != null && _world.Config.ElevationFieldEnabled);
            }

            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (pair.Value != null) pair.Value.gameObject.SetActive(!active);
            }

            for (int index = 0; index < _resourceViews.Count; index++)
            {
                if (_resourceViews[index] != null) _resourceViews[index].gameObject.SetActive(!active);
            }
        }

        /// <summary>
        /// Live terrain tuning. The correct height and smoothing cannot be derived - they depend on
        /// how the relief reads beside a 1-unit creature - so they are dialled here rather than
        /// guessed in source and recompiled.
        /// </summary>
        private void HandleTerrainTuningInput()
        {
            bool changed = false;
            // Two bindings each, because bracket and comma keys are not in the same place on every
            // keyboard layout and a tuning control you cannot find is not a tuning control.
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.PageDown)) { _terrainHeightScale = Mathf.Max(0f, _terrainHeightScale - 2f); changed = true; }
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.PageUp)) { _terrainHeightScale += 2f; changed = true; }
            if (Input.GetKeyDown(KeyCode.Comma) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) { _terrainSmoothingRadius = Mathf.Max(0f, _terrainSmoothingRadius - 0.4f); changed = true; }
            if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) { _terrainSmoothingRadius += 0.4f; changed = true; }
            if (!changed) return;

            RebuildTerrainViews();
        }

        /// <summary>
        /// Rebuild everything drawn from the terrain generator: the arena ground, the sea, and the
        /// K viewer if it is open. Called after any tuning change, from the keys or the panel.
        /// </summary>
        private void RebuildTerrainViews()
        {
            BuildTerrainMesh();
            UpdateWaterSurface();
            if (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off)
            {
                _terrainPreview.HeightScale = _terrainHeightScale;
                _terrainPreview.Rebuild(_world);
            }

            // The planet too, and this is the one that was missing. The chunked surface is built
            // once and then only rebuilt when the seed or the settings revision changes - so with
            // the planet view open, every slider on the J panel moved the arena mesh underneath and
            // left the globe on screen showing the previous world. A tuning control whose effect is
            // not visible in the view you are tuning in is not a tuning control.
            // Marked, not done. A slider being dragged fires this every frame, and re-meshing the
            // planet takes longer than a frame - so doing it here restarted the work continuously and
            // it never finished. The rebuild happens once the values stop moving.
            if (_sphericalArena) _planetRebuildDue = Time.unscaledTime + PlanetRebuildSettle;
        }

        /// <summary>
        /// Rebuild the ground as a displaced grid over the arena. Flat when the elevation flag is
        /// off, which keeps every existing scenario looking exactly as it did.
        /// </summary>
        /// <summary>
        /// The arena ground.
        ///
        /// <para>With the elevation field on, this is built from <see cref="PlanetTerrain"/> through
        /// the shared <see cref="TerrainMeshBuilder"/> - the same generator, window and shading the
        /// K viewer uses - so the playable arena is a 50-unit window on the planet rather than a
        /// separate flat world. Creatures stand on it because <see cref="GroundHeightAt"/> reads the
        /// heights cached from this very mesh.</para>
        ///
        /// <para><b>Cosmetic.</b> The simulation still samples its own <c>EnvironmentField</c> for
        /// moisture, fertility and temperature, and nothing under <c>Assets/Scripts/Simulation</c>
        /// reads PlanetTerrain. Creatures are drawn on this relief but do not experience it: a hill
        /// costs them nothing. Making elevation affect movement is a simulation change needing a
        /// flag, tests and a re-measure.</para>
        /// </summary>
        private void BuildTerrainMesh()
        {
            if (_terrainMesh == null) return;

            bool planetTerrain = _world != null && _world.Config.ElevationFieldEnabled;
            if (!planetTerrain)
            {
                BuildFlatArenaMesh();
                return;
            }

            EnsureArenaPlates();
            float heightScale = TerrainMeshBuilder.PatchHeightScale(TerrainHalfWidth) * (_terrainHeightScale / 14f);
            TerrainMeshBuilder.BuildPatch(
                _world.Config.WorldSeed, _arenaPlates, _arenaCentreLatitude, _arenaCentreLongitude,
                TerrainHalfWidth, heightScale,
                out Vector3[] vertices, out Color[] colors, out int[] triangles,
                ArenaTerrainSettings());

            // Heights are cached from the FLAT vertices, before any curving. They are read back as
            // "how high is the ground at this arena position", which is a question in simulation
            // coordinates; taking them from curved vertices would fold the planet's radius into
            // every creature's height.
            CacheArenaHeights(vertices);

            ArenaProjection.Spherical = _sphericalArena;
            ArenaProjection.ProjectVertices(vertices);
            UpdatePlanetBackdrop();

            Mesh built = TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Arena Terrain");
            _terrainMesh.Clear();
            _terrainMesh.vertices = built.vertices;
            _terrainMesh.colors = built.colors;
            _terrainMesh.triangles = built.triangles;
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();
            if (_arenaTerrainMaterial == null) _arenaTerrainMaterial = TerrainMeshBuilder.CreateTerrainMaterial();
            _terrainRenderer.sharedMaterial = _arenaTerrainMaterial;
            Destroy(built);
        }

        /// <summary>
        /// Heights for creature placement, taken from the mesh the arena actually draws, so creatures
        /// stand on the drawn surface rather than on a separately computed one.
        /// </summary>
        private void CacheArenaHeights(Vector3[] vertices)
        {
            if (_terrainHeights == null || _terrainHeights.Length != vertices.Length)
            {
                _terrainHeights = new float[vertices.Length];
            }

            for (int index = 0; index < vertices.Length; index++)
            {
                _terrainHeights[index] = vertices[index].y;
            }
        }

        /// <summary>
        /// Which terrain settings the arena is drawn from.
        ///
        /// <para>Once terrain drives the ecology the arena must be drawn from the settings the
        /// <b>simulation</b> generates with, not the viewer's - otherwise moving a tuning slider
        /// changes the hill on screen without changing the hill a creature climbs. The K viewer stays
        /// on the panel's settings, because it is a look at the generator rather than at this
        /// world.</para>
        /// </summary>
        private TerrainSettings ArenaTerrainSettings()
        {
            return _world != null && _world.Config.TerrainDrivenEnvironmentEnabled
                ? EnvironmentField.CreateTerrainSettings()
                : TerrainView.Settings;
        }

        /// <summary>
        /// The planet the arena is a window on, drawn behind it at true scale.
        ///
        /// <para>Radius 500 - the same number <c>EnvironmentField.SphereRadius</c> uses - rather than
        /// the preview's 60. Relief is a fraction of radius, so 0.06 gives 30 units of height per
        /// elevation unit at this size, which is exactly what the arena patch uses. The two meshes
        /// are the same surface at the same scale; only their detail differs, and the patch is lifted
        /// clear by <c>ArenaProjection.PatchLift</c> so they do not fight for the same pixels.</para>
        ///
        /// <para>Nothing lives out here. Every creature is inside the patch, and stays there until
        /// the simulation's own spatial model is spherical.</para>
        /// </summary>
        private void UpdatePlanetBackdrop()
        {
            if (!_sphericalArena)
            {
                if (_planetBackdrop != null) _planetBackdrop.SetActive(false);
                return;
            }

            if (_planetBackdrop == null)
            {
                _planetBackdrop = new GameObject("Planet Backdrop");
                _planetBackdrop.transform.position = ArenaProjection.Centre;
                _planetSurface = _planetBackdrop.AddComponent<PlanetChunkedSurface>();

                var ocean = new GameObject("Planet Ocean");
                ocean.transform.SetParent(_planetBackdrop.transform, false);
                ocean.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
                TerrainMeshBuilder.BuildOceanSphere(
                    out Vector3[] oceanVertices, out int[] oceanTriangles, ArenaProjection.PlanetRadius);
                ocean.AddComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.FlatShaded(oceanVertices, null, oceanTriangles, "Planet Ocean");
            }

            // The surface is a quadtree that rebuilds itself as the camera moves, so it has to be
            // pointed at the world again whenever the seed or the tuning panel changes it - the old
            // single mesh was built once and never revisited, which is why turning a dial moved the
            // arena and left the globe behind it showing the previous world.
            int seed = _world.Config.WorldSeed;
            int revision = TerrainView.SettingsRevision;
            if (_planetSurfaceSeed != seed)
            {
                _planetSurface.Configure(
                    _simulationCamera, TerrainMeshBuilder.CreateTerrainMaterial(), seed, _arenaPlates,
                    ArenaTerrainSettings(), ArenaProjection.PlanetRadius,
                    TerrainMeshBuilder.PlanetReliefFraction, Vector3.up);
            }
            else if (_planetSurfaceRevision != revision)
            {
                // Same world, different dials. Configure would throw away nine hundred GameObjects
                // and their meshes and stream them all back; the tree's shape depends only on where
                // the camera is, and tuning does not move the camera. Only the geometry is stale.
                _planetSurface.Reshape(seed, _arenaPlates, ArenaTerrainSettings());
            }

            _planetSurfaceSeed = seed;
            _planetSurfaceRevision = revision;

            _planetBackdrop.SetActive(true);
        }

        /// <summary>
        /// What scale the camera is flying at. Curved, the arena is part of a 500-unit planet and the
        /// whole point is being able to retreat far enough to see it; flat, the arena's own scale is
        /// right and a larger one only lets someone get lost.
        /// </summary>
        private void ApplyCameraRange()
        {
            // The presenter builds its own camera; Camera.main only finds one tagged MainCamera, and
            // when it found nothing this method returned early and left the arena's own scale in
            // place - so the planet was there and could not be flown away from.
            var cameraController = _simulationCamera == null
                ? null
                : _simulationCamera.GetComponent<FreeFlyCameraController>();
            if (cameraController == null) return;

            if (_sphericalArena)
            {
                // Height is now measured from the sphere rather than from y = 0, so flight speed
                // scales with height above the ground the camera is actually over, and up points away
                // from the planet's centre wherever it goes.
                cameraController.SetExtent(
                    ArenaProjection.PlanetRadius, ArenaProjection.Centre, ArenaProjection.PlanetRadius);
                _simulationCamera.farClipPlane = ArenaProjection.PlanetRadius * 8f;
            }
            else
            {
                cameraController.ResetExtent();
                _simulationCamera.farClipPlane = 1000f;
            }
        }

        private void EnsureArenaPlates()
        {
            int seed = _world.Config.WorldSeed;
            int revision = TerrainView.SettingsRevision;
            if (_arenaPlates != null && _arenaPlateSeed == seed && _arenaPlateRevision == revision) return;

            _arenaPlates = PlateStructure.Create(seed, ArenaTerrainSettings());
            _arenaPlateSeed = seed;
            _arenaPlateRevision = revision;
            _arenaPlates.GetCoastalCentre(out _arenaCentreLatitude, out _arenaCentreLongitude);
        }

        /// <summary>Flat ground for every scenario that does not use the elevation field.</summary>
        private void BuildFlatArenaMesh()
        {
            const float halfWidth = TerrainHalfWidth;
            int side = TerrainMeshBuilder.PatchResolution;
            var vertices = new Vector3[side * side];
            var triangles = new int[(side - 1) * (side - 1) * 6];

            if (_terrainHeights == null || _terrainHeights.Length != side * side)
            {
                _terrainHeights = new float[side * side];
            }

            System.Array.Clear(_terrainHeights, 0, _terrainHeights.Length);

            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-halfWidth, halfWidth, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, column / (float)(side - 1));
                    vertices[(row * side) + column] = new Vector3(x, 0f, z);
                }
            }

            // Featureless ground is still a piece of the planet's surface, so it curves too.
            // Without this the sea-level plane stayed flat and cut through the globe behind it.
            ArenaProjection.ProjectVertices(vertices);

            int triangle = 0;
            for (int row = 0; row + 1 < side; row++)
            {
                for (int column = 0; column + 1 < side; column++)
                {
                    int bottomLeft = (row * side) + column;
                    int topLeft = bottomLeft + side;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = topLeft + 1;
                }
            }

            _terrainMesh.Clear();
            _terrainMesh.vertices = vertices;
            _terrainMesh.triangles = triangles;
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();
        }

        /// <summary>
        /// Times the heatmap rebuild, which is the leading suspect for the stutter in the first real
        /// performance reading: median 3.02 ms against a <b>worst frame of 197.52 ms</b>. This is
        /// 128x128 = <b>16,384 terrain samples on the main thread every two seconds</b>. Suspicion is
        /// not measurement, so it is measured rather than assumed.
        /// </summary>
        private void UpdateTemperatureHeatmapIfNeeded()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            UpdateTemperatureHeatmapCore();
            RecordSection("heatmap", timer.Elapsed.TotalMilliseconds);
        }

        private void UpdateTemperatureHeatmapCore()
        {
            if (_world == null || _heatmapUpdateAccumulator < HeatmapUpdateInterval)
            {
                return;
            }

            Bounds terrainBounds = _terrainRenderer.bounds;
            float minX = terrainBounds.min.x;
            float maxX = terrainBounds.max.x;
            float minZ = terrainBounds.min.z;
            float maxZ = terrainBounds.max.z;
            for (int y = 0; y < HeatmapResolution; y++)
            {
                float z = Mathf.Lerp(minZ, maxZ, (y + 0.5f) / HeatmapResolution);
                int rowStart = y * HeatmapResolution;
                for (int x = 0; x < HeatmapResolution; x++)
                {
                    float worldX = Mathf.Lerp(minX, maxX, (x + 0.5f) / HeatmapResolution);
                    var position = new SimVector2(worldX, z);
                    if (_overlay == TerrainOverlay.Elevation)
                    {
                        _temperaturePixels[rowStart + x] = ShadeElevation(_world.Environment.Sample(position).Elevation);
                    }
                    else if (_overlay == TerrainOverlay.Biome)
                    {
                        EnvironmentSample sample = _world.Environment.Sample(position);
                        // Shade each biome by its own fertility so the map shows gradient within a
                        // region, not just flat colour blocks.
                        _temperaturePixels[rowStart + x] = BiomeColors[ClassifyBiome(sample)]
                            * Mathf.Lerp(0.72f, 1.18f, sample.Fertility);
                    }
                    else
                    {
                        float temperature = TemperatureField.Sample(position, _world.CurrentTick);
                        float temperatureFraction = Mathf.InverseLerp(ColdTemperature, HotTemperature, temperature);
                        _temperaturePixels[rowStart + x] = Color.Lerp(Color.blue, Color.red, temperatureFraction);
                    }
                }
            }

            _temperatureHeatmap.SetPixels(_temperaturePixels);
            _temperatureHeatmap.Apply();
            _heatmapUpdateAccumulator = 0f;
            if (_overlay != TerrainOverlay.None)
            {
                ApplyTemperatureHeatmap();
            }
        }
    }
}

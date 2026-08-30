using System.Diagnostics;
using System.IO;
using System.Linq;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.World;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// The whole arena, at the population the ecology actually reaches, rendered to PNG.
    ///
    /// <para><b>Two questions nobody has ever been able to answer.</b> The only Play-mode profile
    /// this project has was taken with 9 to 17 creatures against a cap of 100
    /// (<c>p6-play-mode-profiled-2026-08-24.md</c>); the pressured cell settles at 200 to 500, and
    /// creature rendering at full population has never been measured at all. Separately, nobody has
    /// seen whether a world of animated animals at that density reads as an ecosystem or as a
    /// carpet of overlapping meshes.</para>
    ///
    /// <para>This runs the real simulation - same scenario and flags as the sweeps - then builds a
    /// view per creature through the same role and catalog path the presenter uses, animates them,
    /// and both renders and times it. Needs a real graphics device, so run WITHOUT
    /// <c>-nographics</c>.</para>
    ///
    /// <para><b>The ground is the arena's real terrain</b>, built through
    /// <see cref="TerrainMeshBuilder"/> from the seed, plates and settings the simulation itself
    /// generates from, with creatures standing on its surface. It was a flat green primitive plane
    /// until 2026-08-29, which made every arena PNG a picture of a world this project does not run:
    /// this cell has the elevation field on and terrain driving the ecology.</para>
    /// </summary>
    public static class CreatureArenaCapture
    {
        private const string OutputFolder = "Logs/creature-models";
        private const int Width = 1600;
        private const int Height = 900;
        private const int Ticks = 12000;
        private const int TimedFrames = 40;

        private static bool _geneVision;

        /// <summary>
        /// Ground heights cached from the drawn mesh, exactly as the presenter caches them, so a
        /// creature stands on the surface in the picture rather than on the plane it used to.
        /// </summary>
        private static float[] _terrainHeights;

        [MenuItem("LifeSimulation/Capture the arena")]
        public static void CaptureArena()
        {
            _geneVision = false;
            Capture("arena", Ticks);
        }

        /// <summary>
        /// The same population in gene vision, so the two pictures can be compared directly. That
        /// comparison is the whole argument for the toggle: one picture says which animal and what
        /// it is doing, the other says what the population has become.
        /// </summary>
        [MenuItem("LifeSimulation/Capture the arena in gene vision")]
        public static void CaptureArenaGenes()
        {
            _geneVision = true;
            Capture("genes-late", Ticks);
        }

        /// <summary>
        /// Gene vision early, before selection has finished with the thermal ramp.
        ///
        /// <para>This is the pair that makes selection visible at all.
        /// <c>CreatureAppearanceRules</c> records why a single late picture cannot:
        /// temperature tolerance <b>saturates</b> - the field deviates by at most eight degrees, so
        /// a gene of 0.75 already covers the world and the mean plateaus by about tick 8,000. Every
        /// population ends roughly the same colour, which is exactly what the late capture shows.
        /// <b>The spread is the signal, not the mean:</b> founders scatter across the ramp, then
        /// selection kills the cold tail and a mottled crowd turns uniform.</para>
        ///
        /// <para><b>MEASURED, AND THIS CELL CANNOT SHOW THAT CONTRAST.</b> Population is <b>10 at
        /// tick 6,000 and 15 at tick 2,500</b>, against <b>126 at tick 12,000</b> - so the whole
        /// period when the genes are still diverse has almost nobody in it, and by the time there is
        /// a crowd to look at, selection has already finished. The mottled-to-uniform picture needs
        /// a scenario whose population is large early, which this one is not. Kept because the
        /// method is right and the finding is worth not rediscovering.</para>
        /// </summary>
        [MenuItem("LifeSimulation/Capture the arena in gene vision, early")]
        public static void CaptureArenaGenesEarly()
        {
            _geneVision = true;
            Capture("genes-early", 6000);
        }

        private static void Capture(string prefix, int ticks)
        {
            Directory.CreateDirectory(OutputFolder);

            SimulationConfig config = BuildPressuredCellConfig();
            var world = new SimulationWorld(config);

            // WithRegeneration(2.0) is what `--regen=2.0` does in CreatureSweep, and it is not
            // cosmetic: the base scenario settles this cell at about fifteen creatures, which is a
            // picture of a different world than the one every recent measurement describes.
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate
                .WithRegeneration("p6-defense-calibration-regen2.00", 2f)
                .ApplyTo(world);

            var simulationClock = Stopwatch.StartNew();
            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            simulationClock.Stop();
            SimulationStatistics statistics = world.CaptureStatistics();
            Debug.Log(
                $"ARENA population={world.CreatureCount} ticks={ticks}"
                + $" simSeconds={simulationClock.Elapsed.TotalSeconds:0.0}"
                + $" energy={statistics.MeanEnergyFraction:0.000}"
                + $" fleeing={statistics.FleeingFraction:P1}");

            var root = new GameObject("ArenaCaptureRoot");
            try
            {
                BuildLighting(root);
                BuildGround(world, root.transform);

                int models = 0;
                int capsules = 0;
                for (int index = 0; index < world.CreatureCount; index++)
                {
                    if (SpawnCreature(world, index, root.transform))
                    {
                        models++;
                    }
                    else
                    {
                        capsules++;
                    }
                }

                Debug.Log($"ARENA views models={models} capsules={capsules}");

                Camera camera = BuildCamera(root);
                double wide = Render(camera, GroundRelative(0f, 34f, -46f), Quaternion.Euler(32f, 0f, 0f), $"{prefix}-wide.png");
                double close = Render(camera, GroundRelative(-6f, 4f, -20f), Quaternion.Euler(6f, 12f, 0f), $"{prefix}-close.png");
                double top = Render(camera, GroundRelative(0f, 62f, 0f), Quaternion.Euler(90f, 0f, 0f), $"{prefix}-top.png");

                Debug.Log(
                    $"ARENA render ms/frame wide={wide:0.00} close={close:0.00} top={top:0.00}"
                    + $" at {world.CreatureCount} creatures");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// The cell every recent measurement was taken in: cap 500, regeneration 2.0, brake 1.5,
        /// gate 0.45, predation on. Matched to <c>CreatureSweep</c> so the picture is of the world
        /// the numbers describe and not of some other one.
        /// </summary>
        private static SimulationConfig BuildPressuredCellConfig()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 12);
            return new SimulationConfig(
                42,
                12,
                defaults.Schedule,
                500,
                FounderProfile.PredationVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                reproductionNeedFraction: 0.45f,
                gradedFertilityEnabled: true,
                gradedFertilityStrength: 1.5f);
        }

        private static bool SpawnCreature(SimulationWorld world, int index, Transform parent)
        {
            var id = world.GetCreatureIdAt(index);
            CreatureModelRole role = CreatureModelRules.SelectRole(world.Creatures.GetGenomeAt(index));
            CreatureModelDefinition definition = CreatureModelCatalog.Select(role, id.Value);
            var prefab = _geneVision
                ? null
                : Resources.Load<GameObject>($"{CreatureModelCatalog.ResourcePath}/{definition.ModelName}");

            Transform view;
            bool isModel = prefab != null;
            if (isModel)
            {
                view = Object.Instantiate(prefab, parent).transform;
            }
            else
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                view.SetParent(parent);
            }

            var movement = world.GetCreatureMovementAt(index);
            CreatureAction action = world.GetCreatureDecisionAt(index).Action;
            float bodyScale = Mathf.Lerp(0.7f, 1.35f, world.Creatures.GetGenomeAt(index).BodySize);

            // Juveniles are smaller, exactly as the presenter draws them. Without this the capture
            // rendered every creature at adult size, which overstates what the population looks
            // like: this cell is pinned to the mating gate, so a real share of it is young.
            //
            // The presenter's other scale term, GetActionScale, is deliberately NOT reproduced. It
            // is a pulse driven by Time.unscaledTime - a live animation cue that means nothing in a
            // still, and that would make two captures of the same world differ by whenever the
            // editor happened to be up to.
            float ageScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(world.GetCreatureNeedsAt(index).Age / 4f));

            // On the ground, not at y=0. The +0.55 is the presenter's own offset, which lifts a
            // creature's origin to its feet.
            view.position = new Vector3(
                movement.Position.X,
                GroundHeightAt(movement.Position.X, movement.Position.Y) + 0.55f,
                movement.Position.Y);

            // Facing comes from the step the creature just took, exactly as the presenter does it.
            // The capture used an arbitrary per-id yaw before, which made a herd read as statues
            // pointing in random directions and hid whether facing was working at all.
            float deltaX = movement.Position.X - movement.PreviousPosition.X;
            float deltaY = movement.Position.Y - movement.PreviousPosition.Y;
            float yaw = (deltaX * deltaX) + (deltaY * deltaY) > 1e-8f
                ? Mathf.Atan2(deltaX, deltaY) * Mathf.Rad2Deg
                : 0f;
            view.rotation = Quaternion.Euler(0f, definition.YawOffsetDegrees + yaw, 0f);
            view.localScale = Vector3.one * (ageScale * bodyScale * definition.ModelScale);

            if (_geneVision)
            {
                CreatureAppearance appearance = CreatureAppearanceRules.FromGenome(world.Creatures.GetGenomeAt(index));
                view.GetComponent<Renderer>().material.color =
                    new Color(appearance.Red, appearance.Green, appearance.Blue);
                view.localScale = Vector3.one * (ageScale * appearance.ScaleMultiplier);
            }

            // Animated, not posed. An unsampled skinned mesh costs almost nothing to draw, so a
            // timing taken on still models would flatter the real cost badly.
            if (isModel)
            {
                var animation = view.GetComponentInChildren<Animation>();
                string clip = CreatureModelCatalog.ClipFor(action, definition);
                if (animation != null && animation.GetClip(clip) != null)
                {
                    animation.Play(clip);
                    animation.Sample();
                }
            }

            return isModel;
        }

        /// <summary>
        /// The arena ground, built through <see cref="TerrainMeshBuilder"/> on the same seed, plates,
        /// window and settings the presenter uses - so this is the terrain the game draws and the
        /// terrain the simulation reads, not a third one.
        ///
        /// <para><b>Why this replaced a plane.</b> The capture drew a flat green primitive, so every
        /// arena PNG was a picture of a world that does not exist: this cell runs with the elevation
        /// field on and terrain driving the ecology, which means the ground has relief, a coastline
        /// and biome colour, and creatures are distributed over all of it. A picture that omits the
        /// terrain cannot show a herd standing on a ridge, cannot show anything gathered in a valley,
        /// and quietly answers "does this read as an ecosystem" against the wrong scene.</para>
        ///
        /// <para>The plates come from <see cref="EnvironmentField.CreateTerrainSettings"/> when the
        /// world is terrain-driven, which is what <c>EnvironmentField</c> itself generates from.
        /// Taking the viewer's tuning settings instead would draw a different coast from the one the
        /// creatures were placed by.</para>
        /// </summary>
        private static void BuildGround(SimulationWorld world, Transform parent)
        {
            _terrainHeights = null;

            // Flat arena for any scenario without the elevation field, matching the presenter's own
            // fallback. This cell has it on; the branch exists so that flipping the config produces
            // the runtime's picture rather than a silently wrong one.
            if (!world.Config.ElevationFieldEnabled)
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "Ground";
                plane.transform.SetParent(parent);
                plane.transform.localScale = new Vector3(6f, 1f, 6f);
                plane.GetComponent<Renderer>().sharedMaterial.color = new Color(0.30f, 0.34f, 0.26f);
                return;
            }

            TerrainSettings settings = world.Config.TerrainDrivenEnvironmentEnabled
                ? EnvironmentField.CreateTerrainSettings()
                : TerrainView.Settings;
            PlateStructure plates = PlateStructure.Create(world.Config.WorldSeed, settings);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            // The presenter scales this by `_terrainHeightScale / 14f`, and 14 is its default, so the
            // factor is 1 unless somebody has pressed [ or ] in Play mode. The capture takes the
            // default deliberately: a picture tuned by a live keypress is not reproducible.
            const float halfWidth = TerrainMeshBuilder.ArenaHalfWidth;
            TerrainMeshBuilder.BuildPatch(
                world.Config.WorldSeed, plates, centreLatitude, centreLongitude,
                halfWidth, TerrainMeshBuilder.PatchHeightScale(halfWidth),
                out Vector3[] vertices, out Color[] colors, out int[] triangles, settings);

            // Heights cached from the FLAT vertices, before any projection, for the same reason the
            // presenter caches them there: they answer "how high is the ground at this arena
            // position", which is a question in simulation coordinates.
            _terrainHeights = new float[vertices.Length];
            for (int index = 0; index < vertices.Length; index++)
            {
                _terrainHeights[index] = vertices[index].y;
            }

            // Flat, not curved. The globe is a separate view; the arena capture is the ground-level
            // picture, and ArenaProjection is static state that some other tool may have left set.
            ArenaProjection.Spherical = false;

            // Logged so the "is this the world the numbers describe" check can be made from the log
            // alone. An arena capture of the wrong world has happened once already and looked
            // entirely plausible; a coastline whose height range or centre has moved is the tell.
            float lowest = float.MaxValue;
            float highest = float.MinValue;
            int submerged = 0;
            for (int index = 0; index < _terrainHeights.Length; index++)
            {
                float height = _terrainHeights[index];
                if (height < lowest) lowest = height;
                if (height > highest) highest = height;
                if (height <= 0f) submerged++;
            }

            Debug.Log(
                $"ARENA terrain centre=({centreLatitude:0.0000}, {centreLongitude:0.0000})"
                + $" height={lowest:0.00}..{highest:0.00}"
                + $" water={submerged / (float)_terrainHeights.Length:P1}"
                + $" terrainDriven={world.Config.TerrainDrivenEnvironmentEnabled}");

            var ground = new GameObject("Arena Terrain");
            ground.transform.SetParent(parent);
            ground.AddComponent<MeshFilter>().sharedMesh =
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Arena Terrain");
            ground.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateTerrainMaterial();

            // Sea, at exactly zero because elevation is signed displacement from sea level. Without
            // it the coastal window renders its sea bed as bumpy blue ground, which reads as land.
            var water = new GameObject("Water");
            water.transform.SetParent(parent);
            TerrainMeshBuilder.BuildWaterSurface(
                halfWidth, 0f, out Vector3[] waterVertices, out int[] waterTriangles);
            water.AddComponent<MeshFilter>().sharedMesh =
                TerrainMeshBuilder.SmoothShaded(waterVertices, waterTriangles, "Water");
            water.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
            water.transform.position = Vector3.zero;
        }

        /// <summary>
        /// A camera height measured from the ground rather than from y=0.
        ///
        /// <para>The framing heights were chosen against a flat plane. Relief is 30 world units per
        /// unit of elevation, so on real terrain a fixed y=4 close shot can sit <b>inside</b> a hill
        /// - and a camera buried in the ground renders a plausible-looking picture of the inside of a
        /// mesh. Offsetting by the ground beneath keeps each shot the distance above the surface it
        /// was framed to be.</para>
        /// </summary>
        private static Vector3 GroundRelative(float x, float height, float z)
        {
            return new Vector3(x, GroundHeightAt(x, z) + height, z);
        }

        /// <summary>Ground height under an arena position, read exactly as the presenter reads it.</summary>
        private static float GroundHeightAt(float x, float z)
        {
            return TerrainMeshBuilder.SampleCachedHeight(
                _terrainHeights, TerrainMeshBuilder.ArenaHalfWidth, x, z);
        }

        /// <summary>
        /// Key and fill through <see cref="TerrainMeshBuilder.ConfigureLighting"/>, the shared
        /// lighting path.
        ///
        /// <para>This was a single directional light, which is fine on a creature and wrong on
        /// terrain: every face angled away from it goes black, and on a displaced mesh those read as
        /// hard dark bands in the geometry that are not there. The terrain capture learned this
        /// already; now that this scene has terrain in it, it inherits the same answer.</para>
        /// </summary>
        private static void BuildLighting(GameObject root)
        {
            var keyObject = new GameObject("CaptureLight");
            keyObject.transform.SetParent(root.transform);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.3f;
            key.shadows = LightShadows.None;

            var fillObject = new GameObject("CaptureFill");
            fillObject.transform.SetParent(root.transform);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.72f, 0.80f, 0.92f);
            fill.shadows = LightShadows.None;

            TerrainMeshBuilder.ConfigureLighting(keyObject.transform, fillObject.transform);
        }

        private static Camera BuildCamera(GameObject root)
        {
            var cameraObject = new GameObject("CaptureCamera");
            cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.55f, 0.68f);
            camera.farClipPlane = 400f;
            return camera;
        }

        private static double Render(Camera camera, Vector3 position, Quaternion rotation, string fileName)
        {
            camera.transform.SetPositionAndRotation(position, rotation);

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;

                camera.Render();
                var clock = Stopwatch.StartNew();
                for (int frame = 0; frame < TimedFrames; frame++)
                {
                    camera.Render();
                }

                clock.Stop();

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                readback.Apply();
                File.WriteAllBytes(Path.Combine(OutputFolder, fileName), readback.EncodeToPNG());
                return clock.Elapsed.TotalMilliseconds / TimedFrames;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}

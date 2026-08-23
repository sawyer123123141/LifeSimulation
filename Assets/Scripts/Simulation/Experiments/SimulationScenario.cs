using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct ResourceDefinition
    {
        public ResourceDefinition(
            ResourceKind kind,
            SimVector2 position,
            float interactionRadius,
            float initialAmount,
            float capacity,
            float regenerationPerSecond,
            bool isActive = true,
            float nutritionMultiplier = 1f,
            PlantGenome? plantGenome = null)
        {
            // Scenario data is the other boundary a non-finite value can enter through, and it is
            // the one experiments touch most. NaN survives clamping and every later arithmetic
            // step, so a bad amount here would surface as an unreproducible run rather than as an
            // error. Reject it where it is cheap and the caller can still see which field is wrong.
            RequireFinite(position.X, nameof(position));
            RequireFinite(position.Y, nameof(position));
            RequireFinite(interactionRadius, nameof(interactionRadius));
            RequireFinite(initialAmount, nameof(initialAmount));
            RequireFinite(capacity, nameof(capacity));
            RequireFinite(regenerationPerSecond, nameof(regenerationPerSecond));
            RequireFinite(nutritionMultiplier, nameof(nutritionMultiplier));

            Kind = kind;
            Position = position;
            InteractionRadius = interactionRadius;
            InitialAmount = initialAmount;
            Capacity = capacity;
            RegenerationPerSecond = regenerationPerSecond;
            IsActive = isActive;
            NutritionMultiplier = nutritionMultiplier;
            PlantGenome = plantGenome;
        }

        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
            }
        }

        public ResourceKind Kind { get; }
        public SimVector2 Position { get; }
        public float InteractionRadius { get; }
        public float InitialAmount { get; }
        public float Capacity { get; }
        public float RegenerationPerSecond { get; }
        public bool IsActive { get; }
        public float NutritionMultiplier { get; }
        public PlantGenome? PlantGenome { get; }

        public ResourceId AddTo(ResourceStore resources, float populationScale)
        {
            if (populationScale <= 0f || float.IsNaN(populationScale) || float.IsInfinity(populationScale))
            {
                throw new ArgumentOutOfRangeException(nameof(populationScale));
            }

            ResourceId id = resources.Add(
                Kind,
                Position,
                InteractionRadius,
                InitialAmount * populationScale,
                Capacity * populationScale,
                RegenerationPerSecond * populationScale,
                NutritionMultiplier);
            if (!IsActive)
            {
                resources.SetActive(id, false);
            }

            return id;
        }
    }

    public sealed class SimulationScenario
    {
        private readonly ResourceDefinition[] _resources;
        private readonly SimVector2? _founderPlacement;

        public SimulationScenario(string id, ResourceDefinition[] resources, SimVector2? founderPlacement = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Scenario identifier is required.", nameof(id));
            }

            Id = id;
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _founderPlacement = founderPlacement;
        }

        public string Id { get; }
        public int ResourceCount => _resources.Length;

        public void ApplyTo(SimulationWorld world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.Resources.Count != 0)
            {
                throw new InvalidOperationException("A scenario can only be applied to a world with no existing resources.");
            }

            if (_founderPlacement.HasValue)
            {
                SimVector2 placement = _founderPlacement.Value;
                for (int index = 0; index < world.CreatureCount; index++)
                {
                    world.SetCreaturePosition(world.GetCreatureIdAt(index), placement);
                }
            }

            float populationScale = Math.Max(1f, world.Config.InitialPopulation / 4f);
            for (int index = 0; index < _resources.Length; index++)
            {
                ResourceDefinition definition = _resources[index];
                ResourceId resourceId = definition.AddTo(world.Resources, populationScale);
                if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food && definition.IsActive)
                {
                    float capacity = definition.Capacity * populationScale;
                    float biomass = definition.InitialAmount * populationScale;
                    float growthRate = capacity <= 0f ? 0f : (4f * definition.RegenerationPerSecond) / capacity;
                    int patchIndex = world.AddPlantPatch(resourceId, definition.Position, biomass, capacity, growthRate, nutrition: definition.NutritionMultiplier, defense: 0f);
                    if (definition.PlantGenome.HasValue)
                    {
                        PlantPatchState patch = world.Plants.GetAt(patchIndex);
                        world.Plants.SetGenomeAndLineage(patchIndex, definition.PlantGenome.Value, patch.Lineage);
                    }
                    if (world.Config.PlantSiteCompetitionEnabled)
                    {
                        world.PlantSites.Register(index);
                    }
                }
                else if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food && !definition.IsActive)
                {
                    world.PlantSites.Register(index);
                }
            }
        }
    }

    public static class Prototype1Scenarios
    {
        public static SimulationScenario Baseline { get; } = new SimulationScenario(
            "baseline",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.75f, waterAmount: 12f, waterRegeneration: 0.75f));

        public static SimulationScenario Drought { get; } = new SimulationScenario(
            "drought",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.75f, waterAmount: 12f, waterRegeneration: 0.25f));

        public static SimulationScenario FoodScarcity { get; } = new SimulationScenario(
            "food-scarcity",
            CreateResources(foodAmount: 12f, foodRegeneration: 0.25f, waterAmount: 12f, waterRegeneration: 0.75f));

        public static SimulationScenario ForagingMemoryDemo { get; } = new SimulationScenario(
            "foraging-memory-demo",
            CreateForagingMemoryDemoResources());

        private static ResourceDefinition[] CreateForagingMemoryDemoResources()
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-10f, -10f), 1.5f, 15f, 15f, 0.8f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-8f, -5f), 1.5f, 3f, 3f, 0.15f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-2f, -9f), 1.5f, 30f, 30f, 1.2f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(8f, -6f), 1.5f, 50f, 50f, 2f),
            };
        }

        private static ResourceDefinition[] CreateResources(
            float foodAmount,
            float foodRegeneration,
            float waterAmount,
            float waterRegeneration)
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, waterAmount, waterAmount, waterRegeneration),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, waterAmount, waterAmount, waterRegeneration),
            };
        }
    }

    public static class Prototype3Scenarios
    {
        public static SimulationScenario PlantNutritionPoor { get; } = CreateNutritionScenario("p3-plant-nutrition-poor", 0.5f);
        public static SimulationScenario PlantNutritionRich { get; } = CreateNutritionScenario("p3-plant-nutrition-rich", 1.5f);

        private static SimulationScenario CreateNutritionScenario(string id, float plantNutrition)
        {
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: plantNutrition),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: plantNutrition),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, 12f, 12f, 0.75f),
            });
        }
    }

    public static class Prototype4Scenarios
    {
        public static SimulationScenario PlantBackedBaseline { get; } = new SimulationScenario(
            "p4-plant-backed-baseline",
            CreatePlantSites());
        public static SimulationScenario WatchableStarterHabitat { get; } = new SimulationScenario(
            "p4-watchable-starter-habitat",
            CreateWatchableStarterHabitat(),
            founderPlacement: new SimVector2(-12f, -8f));
        public static SimulationScenario ObservationStable { get; } = CreateObservationScenario("p4-observation-stable", 600f, 30f, 60f, 3f);
        public static SimulationScenario ObservationScarcity { get; } = CreateObservationScenario("p4-observation-scarcity", 60f, 3f, 24f, 1f);
        public static SimulationScenario ObservationMigration { get; } = new SimulationScenario("p4-observation-migration", CreateObservationMigrationResources(), new SimVector2(-18f, -8f));
        /// <summary>
        /// Route geometry: eight sites on a radius-8 ring, alternating Food and Water, founders at
        /// the centre. Adjacent food-to-water separation is 6.12 units, so a shuttle route exists
        /// inside one local region; each site has two different opposite-kind neighbours at equal
        /// distance, so the choice of which one to use is a genuine tie the utility score cannot
        /// break on travel cost alone. Total capacity and regeneration match ObservationStable
        /// (1200 food / 60 per second, 120 water / 6 per second) so survival stays comparable.
        /// </summary>
        public static SimulationScenario ObservationRouteRing { get; } = new SimulationScenario(
            "p4a-observation-route-ring",
            CreateRouteRingResources(),
            founderPlacement: new SimVector2(0f, 0f));

        /// <summary>
        /// Three local regions, each with a permanent Water site at its centre, two active Food
        /// sites 7 units out on opposite sides, and four dormant Food sites as dispersal targets.
        /// Cluster centres are 23-28 apart, beyond any creature's vision, so a cluster is a genuine
        /// local region. Food sits 6-9 units from water inside a cluster, the separation that
        /// produced repeatable shuttling in ObservationRouteRing.
        ///
        /// <para>The food map is allowed to change: with plant mortality enabled, a dying patch
        /// calls <c>SetActive(false)</c> on its site and a successful dispersal calls
        /// <c>SetActiveAt(true)</c> on a dormant one. Nothing new is required for that - this
        /// scenario simply declares the dormant sites the existing mechanism needs.</para>
        ///
        /// <para>Simultaneously active founder productivity is matched to ObservationStable
        /// (1200 food capacity at 60/s, 120 water at 6/s) so a survival difference is attributable
        /// to turnover and layout rather than to a change in total output.</para>
        /// </summary>
        public static SimulationScenario ObservationShiftingPatches { get; } = new SimulationScenario(
            "p4a-observation-shifting-patches",
            CreateShiftingPatchResources(),
            founderPlacement: new SimVector2(-14f, -9f));

        private static ResourceDefinition[] CreateShiftingPatchResources()
        {
            var definitions = new List<ResourceDefinition>();
            SimVector2[] clusterCentres =
            {
                new SimVector2(-14f, -9f),
                new SimVector2(13f, -6f),
                new SimVector2(-2f, 12f),
            };

            foreach (SimVector2 centre in clusterCentres)
            {
                definitions.Add(new ResourceDefinition(ResourceKind.Water, centre, 1.5f, 40f, 40f, 2f));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X - 7f, centre.Y), 1.5f, 200f, 200f, 10f));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X + 7f, centre.Y), 1.5f, 200f, 200f, 10f));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X, centre.Y + 7f), 1.5f, 0f, 200f, 0f, isActive: false));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X, centre.Y - 7f), 1.5f, 0f, 200f, 0f, isActive: false));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X + 5f, centre.Y + 5f), 1.5f, 0f, 200f, 0f, isActive: false));
                definitions.Add(new ResourceDefinition(ResourceKind.Food, new SimVector2(centre.X - 5f, centre.Y - 5f), 1.5f, 0f, 200f, 0f, isActive: false));
            }

            return definitions.ToArray();
        }

        private static ResourceDefinition[] CreateRouteRingResources()
        {
            const float diagonal = 5.656854f;
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(8f, 0f), 1.5f, 300f, 300f, 15f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(diagonal, diagonal), 1.5f, 30f, 30f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, 8f), 1.5f, 300f, 300f, 15f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-diagonal, diagonal), 1.5f, 30f, 30f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-8f, 0f), 1.5f, 300f, 300f, 15f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-diagonal, -diagonal), 1.5f, 30f, 30f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(0f, -8f), 1.5f, 300f, 300f, 15f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(diagonal, -diagonal), 1.5f, 30f, 30f, 1.5f),
            };
        }

        public static SimulationScenario ObservationMating { get; } = new SimulationScenario("p4-observation-mating", new[]
        {
            new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 900f, 900f, 45f),
            new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, 90f, 90f, 4.5f),
        }, new SimVector2(-12f, -8f));

        private static ResourceDefinition[] CreatePlantSites()
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 12f, 12f, 0.75f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-18f, -14f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-4f, -15f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(4f, -3f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, -1f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-18f, 6f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-2f, 5f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(3f, 19f), 1.5f, 0f, 12f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, 19f), 1.5f, 0f, 12f, 0f, isActive: false),
            };
        }

        private static ResourceDefinition[] CreateWatchableStarterHabitat()
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 600f, 600f, 30f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, 60f, 60f, 3f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 600f, 600f, 30f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(10f, 12f), 1.5f, 60f, 60f, 3f),
            };
        }

        private static SimulationScenario CreateObservationScenario(string id, float foodAmount, float foodRegeneration, float waterAmount, float waterRegeneration)
        {
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, waterAmount, waterAmount, waterRegeneration),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(12f, 8f), 1.5f, foodAmount, foodAmount, foodRegeneration),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(12f, 8f), 1.5f, waterAmount, waterAmount, waterRegeneration),
            }, new SimVector2(-12f, -8f));
        }

        private static ResourceDefinition[] CreateObservationMigrationResources()
        {
            return new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-18f, -8f), 1.5f, 36f, 36f, 1f), new ResourceDefinition(ResourceKind.Water, new SimVector2(-18f, -8f), 1.5f, 24f, 24f, 1f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-6f, -8f), 1.5f, 180f, 180f, 9f), new ResourceDefinition(ResourceKind.Water, new SimVector2(-6f, -8f), 1.5f, 48f, 48f, 2f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(6f, -8f), 1.5f, 360f, 360f, 18f), new ResourceDefinition(ResourceKind.Water, new SimVector2(6f, -8f), 1.5f, 72f, 72f, 3f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, -8f), 1.5f, 600f, 600f, 30f), new ResourceDefinition(ResourceKind.Water, new SimVector2(18f, -8f), 1.5f, 96f, 96f, 4f),
            };
        }


        public static SimulationScenario DefendedPlants { get; } = CreateDefenseScenario("p4-defended-plants", defense: .85f);
        public static SimulationScenario UndefendedPlants { get; } = CreateDefenseScenario("p4-undefended-plants", defense: 0f);
        public static SimulationScenario ConsumerDefenseCalibrationControl { get; } = CreateConsumerDefenseCalibrationScenario("p4-defense-calibration-control", defense: 0f);
        public static SimulationScenario ConsumerDefenseCalibrationModerate { get; } = CreateConsumerDefenseCalibrationScenario("p4-defense-calibration-moderate", defense: .3f);

        private static SimulationScenario CreateDefenseScenario(string id, float defense)
        {
            PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, defense, .5f, .5f, .5f);
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 12f, 12f, .75f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-7f, -8f), 1.5f, 12f, 12f, .75f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 12f, 12f, .75f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(5f, 12f), 1.5f, 12f, 12f, .75f),
            });
        }

        // RegenerationPerSecond retuned from 1.5f to 12f after the growth-rate
        // conversion fix (docs/superpowers/plans/2026-08-16-plant-growth-rate-fix.md, Task 1):
        // 12 is the smallest doubling-search candidate starting at 1.5f that
        // produces a nonzero final population in all 5 seeds (42-46) of
        // ConsumerDefenseCalibrationControl at 12 founders / 48 population cap / 12,000 ticks.
        // Active site count raised from 2 to 6 on 2026-08-17
        // (docs/experiments/p4-calibration-unblocked-carrying-capacity-2026-08-17.md).
        // At 2 active sites, carrying capacity sits below the 48 population cap, so the
        // population grows to the cap and collapses: 22/30 seeds extinct, 0 plant generations.
        //
        // Both site count AND spatial arrangement matter, measured over seeds 42-71 through
        // ExperimentRunner with mortality and site competition enabled:
        //   4 sites spread across the arena -> 16/30 extinct  (corners cannot reach each other)
        //   4 sites clustered along one edge ->  0/30 extinct
        //   6 sites spread with two central  ->  0/30 extinct, 12 plant generations
        //   8 sites clustered                ->  0/30 extinct
        // Six spread sites are used rather than four clustered ones: clustering satisfies the
        // constraints but collapses the spatial structure the prototype exists to exercise. The
        // two central sites at (-1, 2) and (-1, -18) bridge the corner sites, which is what makes
        // the spread arrangement survivable when a patch dies.
        //
        // Dispersal targets are placed per active site at (x-8, y), (x, y+8) and (x+4, y+4).
        /// <summary>
        /// REPLICATION CONDITION for the 168-site low-occupancy operating point. **This is not the
        /// original scenario.** The original lived in a throwaway probe that was never committed
        /// and cannot be recovered: git history contains no ZZZ probe, and the writeups record the
        /// site *count* (6 active plus 162 inactive targets), the config, the seeds (42-161), the
        /// tick count (12,000) and the resulting occupancy (0.322-0.332), but never the target
        /// coordinates. See docs/experiments/p4-168-site-replication-2026-08-22.md.
        ///
        /// <para>Active sites, their water, capacities, regeneration and founder genome are
        /// identical to <see cref="ConsumerDefenseCalibrationModerate"/>. Only the dispersal-target
        /// layout differs, and it is fully specified here: a 13 x 13 lattice spanning 114 units on
        /// both axes (spacing 9.5, corners at +/-57), excluding any point within 2.0 of an active
        /// food site, then taking the first 162 remaining points in row-major order.</para>
        ///
        /// <para>The span was calibrated, not guessed. Occupancy is a cliff in this parameter, not a
        /// gradient: spacing 4 gives 0.833, spacing 8 gives 0.528, spacing 9.5 gives <b>0.311</b>,
        /// spacing 11 gives 0.085 with 3/10 seeds extinct, and spacing 13.3 collapses the ecosystem
        /// entirely (9/10 extinct). Only a narrow window reproduces the recorded 0.322-0.332 with
        /// clean survival. See docs/experiments/p4-occupancy-calibration-2026-08-22.md.</para>
        ///
        /// <para><b>Known ecological difference:</b> at this spacing the outer targets sit beyond the
        /// hard-coded creature arena of +/-25, so patches establishing there are never grazed. That
        /// is a refugium the 24-site condition does not have, and it is not known whether the
        /// original 168-site layout had one. Any trait conclusion drawn here must state it.</para>
        ///
        /// <para>Selecting a layout that achieves low occupancy is faithful to the original method -
        /// that writeup records discarding a 42-site version after a preflight because it stayed
        /// ~0.88 occupied. Fidelity is therefore judged on reproducing the *condition* (occupancy
        /// near 0.32-0.33), never on byte-equivalence, which is not achievable.</para>
        /// </summary>
        public static SimulationScenario AbundantSiteReplicationModerate { get; } =
            CreateAbundantSiteReplicationScenario("p4-abundant-site-replication-moderate", defense: .3f);

        private static SimulationScenario CreateAbundantSiteReplicationScenario(string id, float defense)
        {
            PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, defense, .5f, .5f, .5f);
            SimVector2[] activeSites =
            {
                new SimVector2(-12f, -8f),
                new SimVector2(10f, 12f),
                new SimVector2(10f, -8f),
                new SimVector2(-12f, 12f),
                new SimVector2(-1f, 2f),
                new SimVector2(-1f, -18f),
            };

            var definitions = new List<ResourceDefinition>();
            foreach (SimVector2 site in activeSites)
            {
                definitions.Add(new ResourceDefinition(ResourceKind.Food, site, 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome));
                definitions.Add(new ResourceDefinition(ResourceKind.Water, site, 1.5f, 24f, 24f, 1.5f));
            }

            const int targetCount = 162;
            const float span = 114f;
            const float half = span / 2f;
            const float step = span / 12f;
            int placed = 0;
            for (int row = 0; row < 13 && placed < targetCount; row++)
            {
                for (int column = 0; column < 13 && placed < targetCount; column++)
                {
                    var candidate = new SimVector2(-half + (column * step), -half + (row * step));
                    bool tooCloseToActiveSite = false;
                    foreach (SimVector2 site in activeSites)
                    {
                        if (SimVector2.Distance(candidate, site) < 2f)
                        {
                            tooCloseToActiveSite = true;
                            break;
                        }
                    }

                    if (tooCloseToActiveSite)
                    {
                        continue;
                    }

                    definitions.Add(new ResourceDefinition(ResourceKind.Food, candidate, 1.5f, 0f, 24f, 0f, isActive: false));
                    placed++;
                }
            }

            return new SimulationScenario(id, definitions.ToArray(), founderPlacement: new SimVector2(-12f, -8f));
        }

        private static SimulationScenario CreateConsumerDefenseCalibrationScenario(string id, float defense)
        {
            PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, defense, .5f, .5f, .5f);
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, -8f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(10f, -8f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, 12f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, 12f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-1f, 2f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-1f, 2f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-1f, -18f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-1f, -18f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-20f, -8f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, 0f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-8f, -4f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(2f, 12f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 20f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(14f, 16f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(2f, -8f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 0f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(14f, -4f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-20f, 12f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, 20f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-8f, 16f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-9f, 2f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-1f, 10f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(3f, 6f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-9f, -18f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-1f, -10f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(3f, -14f), 1.5f, 0f, 24f, 0f, isActive: false),
            }, founderPlacement: new SimVector2(-12f, -8f));
        }
    }

    public static class DecisionPolicyScenarios
    {
        public static SimulationScenario NearAdequateFood { get; } = CreateTravelScenario("policy-near-adequate", nearFoodAmount: 10f, farFoodAmount: 3f);
        public static SimulationScenario FarRichFood { get; } = CreateTravelScenario("policy-far-rich", nearFoodAmount: 3f, farFoodAmount: 10f);

        private static SimulationScenario CreateTravelScenario(string id, float nearFoodAmount, float farFoodAmount)
        {
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-4f, 0f), 1.5f, nearFoodAmount, nearFoodAmount, 0.35f, nutritionMultiplier: 1f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, 0f), 1.5f, farFoodAmount, farFoodAmount, 0.75f, nutritionMultiplier: 1.5f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-4f, -5f), 1.5f, 12f, 12f, 0.75f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(18f, -5f), 1.5f, 12f, 12f, 0.75f),
            }, founderPlacement: new SimVector2(0f, 0f));
        }
    }
}

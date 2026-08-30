using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Behavior;

namespace LifeSimulation.Tools.SitePilot
{
    /// <summary>
    /// The site-count pilot for generated plant placement.
    ///
    /// <para><b>Arm 1 is the control and it is fingerprint-identical to `Y`.</b> It must reproduce
    /// the recorded numbers - population near 96, mean nearest-neighbour 0.824, mean energy 0.806 -
    /// or the harness is broken and nothing else printed here is evidence. That check is not
    /// optional: two probes on 2026-08-29 produced numbers that read exactly like ecology findings
    /// and were harness bugs, and the only thing that caught either was an arm whose answer was
    /// already known.</para>
    ///
    /// <para>The manipulation is <see cref="SimulationScenario.SplitSites"/>: each active site
    /// becomes N sites sharing the original's amount, capacity and regeneration, so total
    /// productivity is unchanged and the only variable is how many places food is.</para>
    /// </summary>
    internal static class Program
    {
        private const int Ticks = 12000;
        private const int FirstSeed = 42;

        /// <summary>Matches SimulationConfig.DefaultArenaHalfWidth; the clumping index needs the area.</summary>
        private const double ArenaHalfWidth = 25d;

        private static int _seedCount = 6;

        /// <summary>Arm indices to run, so a survival question can be asked at many seeds without paying for every arm. Index 0, the control, is always kept.</summary>
        private static int[] _armFilter;

        private readonly struct Arm
        {
            public Arm(string name, int parts, float spread, bool splitWater, float generatedSpacing = 0f, float generatedThreshold = 0f, float generatedFixedCapacity = 0f, float generatedWaterDistance = 0f, float anchorRadius = 0f, int anchorCount = 4, bool slopeCost = true, bool terrainTemperature = true)
            {
                Name = name;
                Parts = parts;
                Spread = spread;
                SplitWater = splitWater;
                GeneratedSpacing = generatedSpacing;
                GeneratedThreshold = generatedThreshold;
                GeneratedFixedCapacity = generatedFixedCapacity;
                GeneratedWaterDistance = generatedWaterDistance;
                AnchorRadius = anchorRadius;
                AnchorCount = anchorCount;
                SlopeCost = slopeCost;
                TerrainTemperature = terrainTemperature;
            }

            public string Name { get; }
            public int Parts { get; }
            public float Spread { get; }
            public bool SplitWater { get; }

            /// <summary>Zero leaves generated placement off, which is what every split arm wants.</summary>
            public float GeneratedSpacing { get; }

            public float GeneratedThreshold { get; }

            public float GeneratedFixedCapacity { get; }

            /// <summary>Zero leaves generated sites unfiltered by distance to water, which is how the first pilot ran.</summary>
            public float GeneratedWaterDistance { get; }

            public float AnchorRadius { get; }

            public int AnchorCount { get; }

            /// <summary>Both default to Y's own setting, so an arm that does not name them is Y.</summary>
            public bool SlopeCost { get; }

            public bool TerrainTemperature { get; }
        }

        private sealed class RunResult
        {
            public string Arm;
            public int Seed;
            public int Population;
            public bool Extinct;
            public double MeanNearest;
            public double ShareUnderHalf;
            public double ShareUnderOne;
            public double MeanEnergy;
            public int ActiveFoodSites;
            public int LivePlants;
            public int FoodSiteCount;
            public long ExtinctionTick = -1;
            public double MeanSiteSpacing;
            public double ClumpIndex;
            public double CreaturesBelowWaterline;
            public double MeanCreatureElevation;
            public double ArenaBelowWaterline;
            public double MeanArenaElevation;
            public double ActiveFoodBelowWaterline;
            public double WaterSitesBelowWaterline;
            public ulong LayoutFingerprint;
        }

        private static int Main(string[] arguments)
        {
            foreach (string argument in arguments)
            {
                if (argument.StartsWith("--seeds=", StringComparison.Ordinal))
                {
                    _seedCount = int.Parse(argument.Substring(8), CultureInfo.InvariantCulture);
                }
                else if (argument.StartsWith("--arms=", StringComparison.Ordinal))
                {
                    _armFilter = argument.Substring(7).Split(',').Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
                }
            }

            var arms = new List<Arm>
            {
                new Arm("control (Y, 6 food sites)", 1, 0f, false),
                // Spread 0 puts all four copies at the SAME coordinate, so the capacity split
                // happens and the geometry does not. It separates "food is in more places" from
                // "each resource entry holds less", which every other arm changes together.
                new Arm("food x4, spread 0 (capacity split only)", 4, 0f, false),
                new Arm("food x2, spread 3", 2, 3f, false),
                new Arm("food x4, spread 3", 4, 3f, false),
                new Arm("food x8, spread 3", 8, 3f, false),
                new Arm("food x4, spread 6", 4, 6f, false),
                new Arm("food+water x4, spread 3", 4, 3f, true),
                new Arm("generated, spacing 4, fertility .45", 1, 0f, false, 4f, .45f),
                new Arm("generated, spacing 5, fertility .45", 1, 0f, false, 5f, .45f),
                new Arm("generated, spacing 6, fertility .45", 1, 0f, false, 6f, .45f),
                new Arm("generated, spacing 5, fertility .60", 1, 0f, false, 5f, .60f),
                new Arm("generated, spacing 5, capacity 24 each", 1, 0f, false, 5f, .45f, 24f),
                new Arm("generated, spacing 6, capacity 24 each", 1, 0f, false, 6f, .45f, 24f),
                new Arm("generated, spacing 4, water <= 6", 1, 0f, false, 4f, .45f, 0f, 6f),
                new Arm("generated, spacing 4, water <= 8", 1, 0f, false, 4f, .45f, 0f, 8f),
                new Arm("generated, spacing 5, water <= 8", 1, 0f, false, 5f, .45f, 0f, 8f),
                new Arm("generated, spacing 4, water <= 10", 1, 0f, false, 4f, .45f, 0f, 10f),
                new Arm("generated, spacing 3, water <= 6", 1, 0f, false, 3f, .45f, 0f, 6f),
                new Arm("anchored, ring 6, 4 per water", 1, 0f, false, 5f, .45f, 0f, 0f, 6f, 4),
                new Arm("anchored, ring 6, 6 per water", 1, 0f, false, 5f, .45f, 0f, 0f, 6f, 6),
                new Arm("anchored, ring 8, 4 per water", 1, 0f, false, 5f, .45f, 0f, 0f, 8f, 4),
                new Arm("anchored, ring 6, 8 per water", 1, 0f, false, 5f, .45f, 0f, 0f, 6f, 8),
                new Arm("anchored, ring 4, 4 per water", 1, 0f, false, 5f, .45f, 0f, 0f, 4f, 4),
                // The drift arms. All three are the SHIPPED Y layout - the four-way split at radius
                // 6 - so the only thing that differs is the flag named.
                new Arm("shipped Y (split 4, spread 6)", 4, 6f, false),
                new Arm("shipped Y, slope cost OFF", 4, 6f, false, 0f, 0f, 0f, 0f, 0f, 4, false, true),
                new Arm("shipped Y, terrain temperature OFF", 4, 6f, false, 0f, 0f, 0f, 0f, 0f, 4, true, false),
            };

            if (_armFilter != null)
            {
                arms = arms.Where((arm, index) => index == 0 || _armFilter.Contains(index)).ToList();
            }

            ulong controlFingerprint = Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ComputeLayoutFingerprint();
            Console.WriteLine($"Y layout fingerprint: {controlFingerprint:x16}");
            Console.WriteLine($"seeds {FirstSeed}..{FirstSeed + _seedCount - 1}, {Ticks} ticks, {arms.Count} arms");
            Console.WriteLine();

            var specs = new List<(Arm Arm, int Seed)>();
            foreach (Arm arm in arms)
            {
                for (int offset = 0; offset < _seedCount; offset++)
                {
                    specs.Add((arm, FirstSeed + offset));
                }
            }

            var results = new RunResult[specs.Count];
            Parallel.For(0, specs.Count, index =>
            {
                results[index] = Execute(specs[index].Arm, specs[index].Seed);
            });

            Console.WriteLine("| arm | sites | alive | population | clump index | mean nearest | <0.5 | energy | active food | site spacing |");
            Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (Arm arm in arms)
            {
                RunResult[] armResults = results.Where(result => result.Arm == arm.Name).ToArray();
                RunResult[] survivors = armResults.Where(result => !result.Extinct).ToArray();
                string cell(Func<RunResult, double> selector) =>
                    survivors.Length == 0 ? "-" : survivors.Average(selector).ToString("0.000", CultureInfo.InvariantCulture);

                Console.WriteLine(string.Join(" | ",
                    "| " + arm.Name,
                    armResults[0].FoodSiteCount.ToString(CultureInfo.InvariantCulture),
                    $"{survivors.Length} of {armResults.Length}",
                    cell(result => result.Population),
                    cell(result => result.ClumpIndex),
                    cell(result => result.MeanNearest),
                    cell(result => result.ShareUnderHalf),
                    cell(result => result.MeanEnergy),
                    cell(result => result.ActiveFoodSites),
                    cell(result => result.MeanSiteSpacing) + " |"));
            }

            Console.WriteLine();
            Console.WriteLine("per-seed clumping index (1.0 = randomly dispersed, lower = clumped), population in brackets");
            foreach (Arm arm in arms)
            {
                RunResult[] armResults = results.Where(result => result.Arm == arm.Name).OrderBy(result => result.Seed).ToArray();
                Console.WriteLine($"  {arm.Name}: " + string.Join(", ", armResults.Select(result =>
                    result.Extinct ? $"{result.Seed}:extinct@{result.ExtinctionTick}" : $"{result.Seed}:{result.ClumpIndex.ToString("0.000", CultureInfo.InvariantCulture)}({result.Population})")));
            }

            Console.WriteLine();
            Console.WriteLine("where they stand: share at or below the waterline (arena share is the baseline)");
            Console.WriteLine("| arm | creatures below | ARENA below | mean creature elevation | mean arena elevation | active food below | water sites below |");
            Console.WriteLine("|---|---|---|---|---|---|---|");
            foreach (Arm arm in arms)
            {
                RunResult[] survivors = results.Where(result => result.Arm == arm.Name && !result.Extinct).ToArray();
                if (survivors.Length == 0) continue;
                string share(Func<RunResult, double> selector) =>
                    survivors.Average(selector).ToString("0.000", CultureInfo.InvariantCulture);

                Console.WriteLine(string.Join(" | ",
                    "| " + arm.Name,
                    share(result => result.CreaturesBelowWaterline),
                    share(result => result.ArenaBelowWaterline),
                    share(result => result.MeanCreatureElevation),
                    share(result => result.MeanArenaElevation),
                    share(result => result.ActiveFoodBelowWaterline),
                    share(result => result.WaterSitesBelowWaterline) + " |"));
            }

            RunResult controlSample = results.First(result => result.Arm == arms[0].Name);
            Console.WriteLine();
            Console.WriteLine(controlSample.LayoutFingerprint == controlFingerprint
                ? "CONTROL LAYOUT IDENTICAL to Y."
                : $"CONTROL LAYOUT DIFFERS from Y ({controlSample.LayoutFingerprint:x16}) - the harness is wrong, stop.");
            return 0;
        }

        /// <summary>
        /// `Y`'s configuration exactly, as <c>Prototype1Presenter.ResetTerrainPlaytest</c> builds
        /// it. Copied rather than shared because Simulation must not reference Presentation; if that
        /// method changes, this drifts, which is what the fingerprint line exists to notice.
        /// </summary>
        private static SimulationConfig CreateConfig(int seed, Arm arm)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: seed, initialPopulation: 4);
            return new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 96,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
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
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: arm.SlopeCost,
                terrainDrivenTemperatureEnabled: arm.TerrainTemperature,
                healthRecoveryEnabled: true,
                wanderHomeHysteresisEnabled: true,
                feedInPlaceEnabled: true,
                arenaHalfWidth: SimulationConfig.DefaultArenaHalfWidth,
                generatedPlantSitesEnabled: arm.GeneratedSpacing > 0f,
                generatedPlantSiteSpacing: arm.GeneratedSpacing > 0f ? arm.GeneratedSpacing : SimulationConfig.DefaultGeneratedPlantSiteSpacing,
                generatedPlantSiteFertilityThreshold: arm.GeneratedThreshold,
                generatedPlantSiteFixedCapacity: arm.GeneratedFixedCapacity,
                generatedPlantSiteMaximumWaterDistance: arm.GeneratedWaterDistance,
                generatedPlantSiteAnchorRingRadius: arm.AnchorRadius,
                generatedPlantSiteAnchorCount: arm.AnchorCount);
        }

        private static RunResult Execute(Arm arm, int seed)
        {
            SimulationConfig config = CreateConfig(seed, arm);
            SimulationScenario scenario = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            if (arm.Parts > 1)
            {
                scenario = scenario.SplitSites($"pilot-split-{arm.Parts}", arm.Parts, arm.Spread, splitFood: true, splitWater: arm.SplitWater);
            }

            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);

            // WHEN a world dies decides what failed: an early death is an establishment failure and
            // has a different fix from a grown population collapsing. The bigger-world pilot was
            // read as a carrying-capacity result until its survivors turned out to be at full
            // population, which made every failure an early one.
            long extinctionTick = -1;
            for (int tick = 0; tick < Ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
                if (extinctionTick < 0 && world.CreatureCount == 0) extinctionTick = tick;
            }

            RunResult result = Measure(arm, seed, world, scenario);
            result.ExtinctionTick = extinctionTick;
            return result;
        }

        private static RunResult Measure(Arm arm, int seed, SimulationWorld world, SimulationScenario scenario)
        {
            int count = world.CreatureCount;
            var positions = new SimVector2[count];
            double energySum = 0d;
            for (int index = 0; index < count; index++)
            {
                positions[index] = world.GetCreatureMovementAt(index).Position;
                // As a FRACTION of capacity, which is what the 0.806 on record is - Energy itself is
                // absolute and scales with body size, so a mean of it is not comparable to anything.
                float capacity = world.Creatures.GetPhenotypeAt(index).EnergyCapacity;
                energySum += capacity <= 0f ? 0d : world.GetCreatureNeedsAt(index).Energy / capacity;
            }

            double nearestSum = 0d;
            int underHalf = 0;
            int underOne = 0;
            for (int index = 0; index < count; index++)
            {
                double nearest = double.MaxValue;
                for (int other = 0; other < count; other++)
                {
                    if (other == index) continue;
                    double distance = SimVector2.Distance(positions[index], positions[other]);
                    if (distance < nearest) nearest = distance;
                }

                if (nearest == double.MaxValue) continue;
                nearestSum += nearest;
                if (nearest < 0.5d) underHalf++;
                if (nearest < 1.0d) underOne++;
            }

            // WHERE THE GROUND IS, for the drift question. EnvironmentField.Elevation is
            // clamp01(elevation / HighGround) and elevation is signed displacement from sea level,
            // so exactly zero means at or below the waterline - which is the ground the renderer
            // paints as sea. Nothing in the simulation treats it as different, so this is a
            // description of where creatures ARE, not of a rule they are following.
            int creaturesBelow = 0;
            double creatureElevationSum = 0d;
            for (int index = 0; index < count; index++)
            {
                float elevation = world.Environment.Sample(positions[index]).Elevation;
                creatureElevationSum += elevation;
                if (elevation <= 0f) creaturesBelow++;
            }

            // The arena's own share of sea, which is the number the creature share has to be read
            // against. Half a world of ocean and a uniformly spread herd would put half the animals
            // in the water with no drift at all.
            const int ArenaSamples = 129;
            int arenaBelow = 0;
            double arenaElevationSum = 0d;
            for (int row = 0; row < ArenaSamples; row++)
            {
                for (int column = 0; column < ArenaSamples; column++)
                {
                    var probe = new SimVector2(
                        (float)(-ArenaHalfWidth + (2d * ArenaHalfWidth * column / (ArenaSamples - 1))),
                        (float)(-ArenaHalfWidth + (2d * ArenaHalfWidth * row / (ArenaSamples - 1))));
                    float elevation = world.Environment.Sample(probe).Elevation;
                    arenaElevationSum += elevation;
                    if (elevation <= 0f) arenaBelow++;
                }
            }

            int arenaProbes = ArenaSamples * ArenaSamples;

            int foodSites = 0;
            var activeFoodPositions = new List<SimVector2>();
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind != ResourceKind.Food) continue;
                foodSites++;
                if (resource.IsActive) activeFoodPositions.Add(resource.Position);
            }

            int activeFood = activeFoodPositions.Count;

            // How far apart the food itself is, which is the quantity generated placement would
            // control directly. Coincident sites count as zero distance, which is the honest reading
            // of the spread-0 arm: four entries at one coordinate are one location.
            double siteSpacingSum = 0d;
            for (int index = 0; index < activeFood; index++)
            {
                double nearest = double.MaxValue;
                for (int other = 0; other < activeFood; other++)
                {
                    if (other == index) continue;
                    double distance = SimVector2.Distance(activeFoodPositions[index], activeFoodPositions[other]);
                    if (distance < nearest) nearest = distance;
                }

                if (nearest != double.MaxValue) siteSpacingSum += nearest;
            }

            int activeFoodBelow = 0;
            foreach (SimVector2 site in activeFoodPositions)
            {
                if (world.Environment.Sample(site).Elevation <= 0f) activeFoodBelow++;
            }

            int waterSites = 0;
            int waterBelow = 0;
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind != ResourceKind.Water || !resource.IsActive) continue;
                waterSites++;
                if (world.Environment.Sample(resource.Position).Elevation <= 0f) waterBelow++;
            }

            int livePlants = 0;
            for (int index = 0; index < world.Plants.Count; index++)
            {
                if (world.Plants.GetAt(index).Biomass > 0f) livePlants++;
            }

            // Mean nearest-neighbour DEPENDS ON POPULATION - ninety animals in a fixed arena are
            // closer together than seventy for no behavioural reason at all - and the generated arms
            // finish with fewer creatures than the control. Comparing the raw distance between them
            // credits an arm for killing animals.
            //
            // The index divides by what the same number of animals would give if they were scattered
            // at random: the expected nearest-neighbour distance of a Poisson process at intensity
            // N/A is 0.5*sqrt(A/N). So 1.0 reads as randomly dispersed, below 1.0 as clumped, and the
            // number is comparable across arms with different populations.
            double area = 4d * ArenaHalfWidth * ArenaHalfWidth;
            double expectedNearest = count < 2 ? 0d : .5d * Math.Sqrt(area / count);
            double meanNearest = count < 2 ? 0d : nearestSum / count;

            return new RunResult
            {
                Arm = arm.Name,
                Seed = seed,
                Population = count,
                Extinct = count == 0,
                MeanNearest = meanNearest,
                ClumpIndex = expectedNearest <= 0d ? 0d : meanNearest / expectedNearest,
                ShareUnderHalf = count < 2 ? 0d : underHalf / (double)count,
                ShareUnderOne = count < 2 ? 0d : underOne / (double)count,
                MeanEnergy = count == 0 ? 0d : energySum / count,
                ActiveFoodSites = activeFood,
                LivePlants = livePlants,
                FoodSiteCount = foodSites,
                MeanSiteSpacing = activeFood < 2 ? 0d : siteSpacingSum / activeFood,
                CreaturesBelowWaterline = count == 0 ? 0d : creaturesBelow / (double)count,
                MeanCreatureElevation = count == 0 ? 0d : creatureElevationSum / count,
                ArenaBelowWaterline = arenaBelow / (double)arenaProbes,
                MeanArenaElevation = arenaElevationSum / arenaProbes,
                ActiveFoodBelowWaterline = activeFood == 0 ? 0d : activeFoodBelow / (double)activeFood,
                WaterSitesBelowWaterline = waterSites == 0 ? 0d : waterBelow / (double)waterSites,
                LayoutFingerprint = scenario.ComputeLayoutFingerprint(),
            };
        }
    }
}

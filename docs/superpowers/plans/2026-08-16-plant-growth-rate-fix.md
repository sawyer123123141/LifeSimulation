# Plant Growth-Rate Conversion Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the plant-cohort growth-rate conversion so `PlantCohortsEnabled` scenarios stop guaranteeing extinction under real consumer grazing pressure, and prove it via the P4 consumer-defense calibration scenario.

**Architecture:** Two independent-but-sequenced fixes: (1) correct the logistic growth-rate unit conversion in `SimulationScenario.cs` and add a small self-recovery floor in `PlantGrowthSystem.cs`, both under the existing `PlantCohortsEnabled` gate (no new flag needed — it already defaults `false`); (2) once the corrected formula exists, empirically retune the calibration scenario's regen value and add dispersal-target sites so it actually survives multi-generation grazing, proven by a committed integration test.

**Tech Stack:** C#, Unity EditMode NUnit tests (`Assets/Tests/EditMode`), headless test runner at `tools/HeadlessTests` (`cd tools/HeadlessTests && dotnet test`).

## Global Constraints

- `SimulationConfig.PlantCohortsEnabled` already defaults to `false` — this is a correction under the existing gate, not new opt-in behavior. No new `SimulationConfig` flag is needed.
- The standard hash-regression baseline (`PredationVariation`/`Legacy` scenario, which never sets `PlantCohortsEnabled`) must remain unaffected — prove this with a hash-regression test, but do not assume the constant `12050501592762519865UL` holds without deriving it fresh via a throwaway worktree at the pre-task commit, per this project's established methodology.
- Runs where `PlantCohortsEnabled: true` are *expected* to produce different results than before this fix — that is the fix working, not a regression to prevent.
- No change to `EnvironmentField`'s hardcoded fertility/temperature (`= 1f` always) — that is defect B-7, explicitly deferred to a separate world-generation spec. Do not touch it here.
- No change to `DefendedPlants`, `UndefendedPlants`, `PlantBackedBaseline`, or `WatchableStarterHabitat` scenario values — only `ConsumerDefenseCalibrationControl`/`ConsumerDefenseCalibrationModerate` get retuned.
- No change to the logistic growth curve family itself, and no change to `PlantReproductionSystem`'s dispersal mechanics — only which scenario registers dispersal targets.

---

### Task 1: Correct the growth-rate formula and add the self-recovery floor

**Files:**
- Modify: `Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs`
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs:116-117`
- Modify: `Assets/Tests/EditMode/PlantGrowthTests.cs` (update one existing test's expected value)
- Test: `Assets/Tests/EditMode/PlantGrowthTests.cs` (new tests), `Assets/Tests/EditMode/ResourceExperimentTests.cs` (new test), `Assets/Tests/EditMode/CoreSimulationTests.cs` (new hash-regression test)

**Interfaces:**
- Consumes: `PlantPatchStore.GetAt(int) -> PlantPatchState` (existing), `PlantPatchState.Biomass`/`Capacity`/`GrowthRate`/`Genome` (existing readonly fields), `PlantPhenotype.FromGenome(PlantGenome) -> PlantPhenotype` (existing), `EnvironmentField.Sample(SimVector2) -> EnvironmentSample` (existing), `ResourceDefinition.RegenerationPerSecond`/`Capacity` (existing, `SimulationScenario.cs`).
- Produces: `PlantGrowthSystem.Step`'s corrected growth formula (same signature, `Step(PlantPatchStore, EnvironmentField, float) -> float`, no interface change — only internal math changes). `SimulationScenario.ApplyTo`'s corrected `growthRate` local variable computation (internal, no public interface change).

- [ ] **Step 1: Write the failing test for the corrected growth-rate conversion**

Read `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs` around line 107-129 first to confirm the exact current `ApplyTo` body (the `populationScale` and `growthRate` computation) has not shifted from:

```csharp
float populationScale = Math.Max(1f, world.Config.InitialPopulation / 4f);
for (int index = 0; index < _resources.Length; index++)
{
    ResourceDefinition definition = _resources[index];
    ResourceId resourceId = definition.AddTo(world.Resources, populationScale);
    if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food && definition.IsActive)
    {
        float capacity = definition.Capacity * populationScale;
        float biomass = definition.InitialAmount * populationScale;
        float growthRate = capacity <= 0f ? 0f : definition.RegenerationPerSecond / capacity;
        int patchIndex = world.AddPlantPatch(resourceId, definition.Position, biomass, capacity, growthRate, nutrition: definition.NutritionMultiplier, defense: 0f);
        ...
```

Add to `Assets/Tests/EditMode/ResourceExperimentTests.cs` (append inside the existing test class — check the file's current `using` statements and namespace before appending, they should already include `LifeSimulation.Simulation.Core`, `LifeSimulation.Simulation.Experiments`, `LifeSimulation.Simulation.Resources`):

```csharp
        [Test]
        public void PlantPatchGrowthRateMatchesCorrectedFourTimesConversion()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 12);
            var world = new SimulationWorld(config);

            Prototype4Scenarios.ConsumerDefenseCalibrationControl.ApplyTo(world);

            float populationScale = Math.Max(1f, config.InitialPopulation / 4f);
            float expectedCapacity = 24f * populationScale;
            float expectedGrowthRate = (4f * 1.5f) / expectedCapacity;
            Assert.That(world.Plants.GetAt(0).GrowthRate, Is.EqualTo(expectedGrowthRate).Within(0.0001f));
        }
```

(`24f` and `1.5f` are `ConsumerDefenseCalibrationControl`'s current `Capacity`/`RegenerationPerSecond` values for its first food resource, from `SimulationScenario.cs`'s `CreateConsumerDefenseCalibrationScenario` — confirm these haven't shifted before writing this test.)

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter PlantPatchGrowthRateMatchesCorrectedFourTimesConversion`
Expected: FAIL — actual `GrowthRate` is `expectedGrowthRate / 4` under today's uncorrected formula.

- [ ] **Step 3: Fix the growth-rate conversion**

In `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`, change:

```csharp
                    float growthRate = capacity <= 0f ? 0f : definition.RegenerationPerSecond / capacity;
```

to:

```csharp
                    float growthRate = capacity <= 0f ? 0f : (4f * definition.RegenerationPerSecond) / capacity;
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd tools/HeadlessTests && dotnet test --filter PlantPatchGrowthRateMatchesCorrectedFourTimesConversion`
Expected: PASS.

- [ ] **Step 5: Write the failing test for the zero-biomass self-recovery floor**

Read `Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs` in full first to confirm its current exact body has not shifted from:

```csharp
using System;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantGrowthSystem
    {
        public static float Step(PlantPatchStore patches, EnvironmentField field, float deltaTime)
        {
            float addedBiomass = 0f;
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                if (patch.Biomass <= 0f || patch.Biomass >= patch.Capacity) continue;
                EnvironmentSample sample = field.Sample(patch.Position);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                float moistureAdaptation = sample.Moisture <= 0f
                    ? 0f
                    : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * patch.Genome.WaterEfficiency + .3f * patch.Genome.MoistureTolerance)));
                float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(sample.Fertility, sample.Temperature)));
                float growth = patch.GrowthRate * phenotype.GrowthRateMultiplier * patch.Biomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
                float next = Math.Min(patch.Capacity, patch.Biomass + growth);
                patches.SetBiomass(index, next);
                addedBiomass += next - patch.Biomass;
            }

            return addedBiomass;
        }

        public static void ProjectFoodResources(PlantPatchStore patches, ResourceStore resources)
        {
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                resources.SetFoodProjection(patch.FoodResourceId, patch.Biomass, patch.Nutrition * phenotype.NutritionMultiplier, phenotype.Defense);
            }
        }
    }
}
```

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs` (append inside the existing `PlantGrowthTests` class — check its current `using` statements first, they should already include `LifeSimulation.Simulation.Core`, `LifeSimulation.Simulation.Environment`, `LifeSimulation.Simulation.Resources`):

```csharp
        [Test]
        public void ZeroBiomassPatchStillProducesNonzeroGrowth()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 0f, 10f, 1f, 1f, 0f);

            float added = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            Assert.That(added, Is.GreaterThan(0f));
            Assert.That(patches.GetAt(0).Biomass, Is.GreaterThan(0f));
        }

        [Test]
        public void SproutFloorContributionIsSmallRelativeToNormalGrowthAtHalfCapacity()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 5f, 10f, 1f, 1f, 0f);

            float addedWithFloor = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            float mult = PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
            float growthWithoutFloor = 1f * mult * 5f * (1f - (5f / 10f)) * 1f * 1f;

            float relativeDifference = (addedWithFloor - growthWithoutFloor) / growthWithoutFloor;
            Assert.That(relativeDifference, Is.LessThan(0.05f));
        }
```

(`relativeDifference` at `Biomass=5, Capacity=10`: floor adds `0.01*10=0.1` to `sproutBiomass`, i.e. `5.1` vs `5` — a `2%` relative increase in the biomass term, well under the `5%` tolerance.)

- [ ] **Step 6: Run the tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter ZeroBiomassPatchStillProducesNonzeroGrowth`
Expected: FAIL — today's code returns `added = 0f` because the `patch.Biomass <= 0f` guard skips the patch entirely.

Run: `cd tools/HeadlessTests && dotnet test --filter SproutFloorContributionIsSmallRelativeToNormalGrowthAtHalfCapacity`
Expected: PASS already (today's formula has no floor, so `addedWithFloor` currently equals `growthWithoutFloor` exactly, `relativeDifference = 0`) — this test is a forward-looking guard, not a red/green step for this particular change; confirm it passes both before and after Step 7.

- [ ] **Step 7: Implement the floor and remove the permanent-trap guard**

In `Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs`, replace the whole `Step` method body with:

```csharp
        private const float SproutFloorFraction = 0.01f;

        public static float Step(PlantPatchStore patches, EnvironmentField field, float deltaTime)
        {
            float addedBiomass = 0f;
            for (int index = 0; index < patches.Count; index++)
            {
                PlantPatchState patch = patches.GetAt(index);
                if (patch.Biomass >= patch.Capacity) continue;
                EnvironmentSample sample = field.Sample(patch.Position);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                float moistureAdaptation = sample.Moisture <= 0f
                    ? 0f
                    : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * patch.Genome.WaterEfficiency + .3f * patch.Genome.MoistureTolerance)));
                float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(sample.Fertility, sample.Temperature)));
                float sproutBiomass = patch.Biomass + (SproutFloorFraction * patch.Capacity);
                float growth = patch.GrowthRate * phenotype.GrowthRateMultiplier * sproutBiomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
                float next = Math.Min(patch.Capacity, patch.Biomass + growth);
                patches.SetBiomass(index, next);
                addedBiomass += next - patch.Biomass;
            }

            return addedBiomass;
        }
```

(`ProjectFoodResources` below it in the same file is unchanged — leave it exactly as-is.)

- [ ] **Step 8: Run the tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter ZeroBiomassPatchStillProducesNonzeroGrowth`
Expected: PASS.

Run: `cd tools/HeadlessTests && dotnet test --filter SproutFloorContributionIsSmallRelativeToNormalGrowthAtHalfCapacity`
Expected: PASS (relative difference now `~0.02`, still under `0.05`).

- [ ] **Step 9: Update the existing test whose expected value changes**

In `Assets/Tests/EditMode/PlantGrowthTests.cs`, `LogisticGrowthIsLimitedByTheEnvironmentAndCapacity` currently reads:

```csharp
        [Test]
        public void LogisticGrowthIsLimitedByTheEnvironmentAndCapacity()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 1f, 0f);

            float added = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            float expectedGrowth = 1.6f * PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
            Assert.That(added, Is.EqualTo(expectedGrowth).Within(.0001f));
            Assert.That(patches.GetAt(0).Biomass, Is.EqualTo(2f + expectedGrowth).Within(.0001f));
        }
```

Change `1.6f` to `1.68f` in both places it appears (the `expectedGrowth` computation is used in both assertions via the same variable, so only one literal needs changing):

```csharp
            float expectedGrowth = 1.68f * PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
```

(Derivation: `biomass=2, capacity=10, growthRate=1`: `sproutBiomass = 2 + (0.01*10) = 2.1`; `growth = 1 * mult * 2.1 * (1 - 2/10) * 1 * 1 = 1 * mult * 2.1 * 0.8 = 1.68 * mult`.)

`ZeroMoisturePreventsPlantGrowth` (the next test in the same file) is unaffected — moisture `0` makes `limit = 0`, which zeroes out growth regardless of the floor term (`growth = ... * sproutBiomass * ... * 0 * ... = 0`), so its assertions (`Step` returns `0f`, biomass stays `2f`) still hold unchanged. Do not modify it.

- [ ] **Step 10: Run the full test suite**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — all tests including the updated and new ones.

- [ ] **Step 11: Derive and write the hash-regression test**

Record the exact current commit:

```bash
git log --oneline -1 main
```

Use this as `PRE_TASK_COMMIT`. In a throwaway worktree:

```bash
git worktree add /c/ls-work/plant-growth-baseline PRE_TASK_COMMIT
```

In that worktree, add a temporary test running:

```csharp
SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
var config = new SimulationConfig(
    worldSeed: 99, initialPopulation: 2, schedule: schedule,
    founderProfile: FounderProfile.PredationVariation);
var world = new SimulationWorld(config);
for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }
// print world.ComputeStateHash()
```

Run it, capture the printed hash, then remove the worktree:

```bash
cd /c/Users/sawye/OneDrive/Claude\ Code\ Roblox\ Game
git worktree remove /c/ls-work/plant-growth-baseline
git worktree prune
```

This scenario never sets `PlantCohortsEnabled: true`, so it is expected to reproduce `12050501592762519865UL` — but derive it fresh rather than assuming, per this project's established methodology.

Append to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        // Captured from the pre-Task-1 commit PRE_TASK_COMMIT (the commit this task's changes were
        // built on top of), by running this exact setup for 50 ticks and reading
        // world.ComputeStateHash(). Pinning this value confirms that correcting the plant
        // growth-rate conversion in SimulationScenario.cs and adding the sprout floor in
        // PlantGrowthSystem.cs is invisible to any scenario that never enables
        // Config.PlantCohortsEnabled.
        private const ulong ExpectedPlantGrowthRateFixUnaffectedHash = 12050501592762519865UL;

        [Test]
        public void PlantGrowthRateFixDoesNotAffectNonPlantCohortScenarios()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var config = new SimulationConfig(
                worldSeed: 99, initialPopulation: 2, schedule: schedule,
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedPlantGrowthRateFixUnaffectedHash));
        }
```

Replace `PRE_TASK_COMMIT` in the comment with the actual commit hash recorded above. If the derived hash differs from `12050501592762519865UL`, use the actually-derived value instead.

- [ ] **Step 12: Run the hash-regression test and the full suite**

Run: `cd tools/HeadlessTests && dotnet test --filter PlantGrowthRateFixDoesNotAffectNonPlantCohortScenarios`
Expected: PASS.

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite green.

- [ ] **Step 13: Commit**

```bash
git add Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs Assets/Scripts/Simulation/Experiments/SimulationScenario.cs Assets/Tests/EditMode/PlantGrowthTests.cs Assets/Tests/EditMode/ResourceExperimentTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Correct plant growth-rate conversion and add zero-biomass recovery floor"
```

---

### Task 2: Retune the calibration scenario and add dispersal targets

**Files:**
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs` (`CreateConsumerDefenseCalibrationScenario`)
- Test: `Assets/Tests/EditMode/ResourceExperimentTests.cs` (new integration test)

**Interfaces:**
- Consumes: Task 1's corrected `growthRate` conversion and sprout-floor formula (already merged/available — this task only makes sense after Task 1's formula fix exists, since the empirical retuning in Step 2 below must be measured against the corrected formula, not the old buggy one). `Prototype4Scenarios.ConsumerDefenseCalibrationControl`/`ConsumerDefenseCalibrationModerate` (existing, `SimulationScenario.cs`). `ExperimentRunner.Run(SimulationConfig, SimulationScenario, int) -> ExperimentResult` (existing, `Assets/Scripts/Simulation/Experiments/ExperimentRunner.cs`). `SimulationStatistics.Population` (existing).
- Produces: Retuned `ConsumerDefenseCalibrationControl`/`ConsumerDefenseCalibrationModerate` scenarios (same public static properties, same `SimulationScenario` type, only their internal resource definitions and `RegenerationPerSecond` value change) that survive multi-generation grazing.

- [ ] **Step 1: Read the current scenario and batch-entry config fresh**

Read `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`'s `CreatePlantSites` method (for the dispersal-target-site pattern to mirror) and `CreateConsumerDefenseCalibrationScenario` method (the scenario to modify) in full. Read `Assets/Editor/PrototypeBatchEntry.cs`'s `RunPrototype4ConsumerDefenseCalibration` method in full to confirm the exact config used for calibration runs (12 founders, 48 population cap, `IntentUtilityV1` + `cognitionEnabled` + `physiologyEnabled` + `plantCohortsEnabled`, 12,000 ticks, seeds `{42, 43, 44, 45, 46}`). Confirm none of these have shifted from what this plan assumes before proceeding.

- [ ] **Step 2: Add dispersal-target sites to the calibration scenario**

In `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`, change `CreateConsumerDefenseCalibrationScenario` from:

```csharp
        private static SimulationScenario CreateConsumerDefenseCalibrationScenario(string id, float defense)
        {
            PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, defense, .5f, .5f, .5f);
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 1.5f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 1.5f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 1.5f),
            }, founderPlacement: new SimVector2(-12f, -8f));
        }
```

to (adding 6 inactive dispersal-target `Food` sites spread around the two active patches, following `CreatePlantSites`' pattern of `isActive: false` zero-amount definitions; the `RegenerationPerSecond` value `1.5f` on the two active food definitions is a placeholder here — Step 3 below replaces it with the empirically-derived value):

```csharp
        private static SimulationScenario CreateConsumerDefenseCalibrationScenario(string id, float defense)
        {
            PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, defense, .5f, .5f, .5f);
            return new SimulationScenario(id, new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 1.5f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 1.5f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(10f, 12f), 1.5f, 24f, 24f, 1.5f),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-20f, -8f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -20f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(-4f, -8f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(18f, 12f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(10f, 22f), 1.5f, 0f, 24f, 0f, isActive: false),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(2f, 12f), 1.5f, 0f, 24f, 0f, isActive: false),
            }, founderPlacement: new SimVector2(-12f, -8f));
        }
```

(Positions are 8 units out from each active patch in the four cardinal-ish directions, avoiding overlap with either active patch or the founder placement.)

- [ ] **Step 3: Empirically derive the retuned `RegenerationPerSecond` value**

Write a throwaway diagnostic test (do not commit it — delete it once the value is found) in `Assets/Tests/EditMode/`, e.g. `ZZZTempCalibrationRetune.cs`. This builds the scenario's resources inline (bypassing `Prototype4Scenarios.ConsumerDefenseCalibrationControl`/`ApplyTo` entirely) so each candidate `RegenerationPerSecond` can be swept in a single automated run, without editing production code between iterations:

```csharp
using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public class ZZZTempCalibrationRetune
    {
        [Test]
        public void FindMinimumSustainableRegenerationRate()
        {
            int[] seeds = { 42, 43, 44, 45, 46 };
            float candidate = 1.5f;
            for (int iteration = 0; iteration < 10; iteration++)
            {
                int survivorCount = 0;
                foreach (int seed in seeds)
                {
                    SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(seed, 12);
                    var config = new SimulationConfig(
                        seed,
                        initialPopulation: 12,
                        defaults.Schedule,
                        maximumPopulation: 48,
                        defaults.FounderProfile,
                        cognitionEnabled: true,
                        physiologyEnabled: true,
                        decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                        plantCohortsEnabled: true);
                    var world = new SimulationWorld(config);
                    float populationScale = Math.Max(1f, config.InitialPopulation / 4f);
                    PlantGenome genome = new PlantGenome(.55f, .5f, .5f, .65f, 0f, .5f, .5f, .5f);
                    for (int index = 0; index < world.CreatureCount; index++)
                    {
                        world.SetCreaturePosition(world.GetCreatureIdAt(index), new SimVector2(-12f, -8f));
                    }
                    SimVector2[] activePositions = { new SimVector2(-12f, -8f), new SimVector2(10f, 12f) };
                    foreach (SimVector2 position in activePositions)
                    {
                        float capacity = 24f * populationScale;
                        float biomass = 24f * populationScale;
                        ResourceId foodId = world.Resources.Add(ResourceKind.Food, position, 1.5f, biomass, capacity, candidate * populationScale, nutritionMultiplier: 1f);
                        int patchIndex = world.AddPlantPatch(foodId, position, biomass, capacity, (4f * candidate) / capacity, nutrition: 1f, defense: 0f);
                        PlantPatchState patch = world.Plants.GetAt(patchIndex);
                        world.Plants.SetGenomeAndLineage(patchIndex, genome, patch.Lineage);
                        world.Resources.Add(ResourceKind.Water, position, 1.5f, 24f * populationScale, 24f * populationScale, 1.5f * populationScale, nutritionMultiplier: 1f);
                    }
                    SimVector2[] dispersalTargets =
                    {
                        new SimVector2(-20f, -8f), new SimVector2(-12f, -20f), new SimVector2(-4f, -8f),
                        new SimVector2(18f, 12f), new SimVector2(10f, 22f), new SimVector2(2f, 12f),
                    };
                    foreach (SimVector2 position in dispersalTargets)
                    {
                        ResourceId siteId = world.Resources.Add(ResourceKind.Food, position, 1.5f, 0f, 24f, 0f, nutritionMultiplier: 1f);
                        world.Resources.SetActive(siteId, false);
                        world.PlantSites.Register(world.Resources.Count - 1);
                    }

                    for (int i = 0; i < 12000; i++) { world.Step(config.FixedDeltaTime); }
                    if (world.Statistics.Population > 0) survivorCount++;
                }
                TestContext.WriteLine(string.Format("candidate={0} survivors={1}/5", candidate, survivorCount));
                if (survivorCount == 5) break;
                candidate *= 2f;
            }
        }
    }
}
```

(This mirrors the dispersal-target sites already added to the real scenario in Step 2, so the derived value reflects the actual final scenario shape.) Run it: `cd tools/HeadlessTests && dotnet test --filter FindMinimumSustainableRegenerationRate --logger "console;verbosity=detailed"`. Read the `TestContext.WriteLine` output for the first `candidate` value that reaches `5/5` survivors — that is the derived value.

- [ ] **Step 4: Apply the derived value and delete the diagnostic**

In `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`, replace both `1.5f` `RegenerationPerSecond` values on the two active `Food` definitions in `CreateConsumerDefenseCalibrationScenario` (the two `ResourceDefinition(ResourceKind.Food, ...)` lines, not the `Water` lines) with the derived value from Step 3, and add a comment above the method recording the derivation:

```csharp
        // RegenerationPerSecond retuned from 1.5f to <DERIVED_VALUE>f after the growth-rate
        // conversion fix (docs/superpowers/plans/2026-08-16-plant-growth-rate-fix.md, Task 1):
        // <DERIVED_VALUE> is the smallest doubling-search candidate starting at 1.5f that
        // produces a nonzero final population in all 5 seeds (42-46) of
        // ConsumerDefenseCalibrationControl at 12 founders / 48 population cap / 12,000 ticks.
        private static SimulationScenario CreateConsumerDefenseCalibrationScenario(string id, float defense)
```

Delete `Assets/Tests/EditMode/ZZZTempCalibrationRetune.cs` — it was a throwaway diagnostic, not part of the permanent suite.

- [ ] **Step 5: Write the committed integration regression test**

Add to `Assets/Tests/EditMode/ResourceExperimentTests.cs`:

```csharp
        [Test]
        public void ConsumerDefenseCalibrationControlSustainsNonzeroPopulationAcrossAllSeeds()
        {
            int[] seeds = { 42, 43, 44, 45, 46 };
            foreach (int seed in seeds)
            {
                SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(seed, 12);
                var config = new SimulationConfig(
                    seed,
                    initialPopulation: 12,
                    defaults.Schedule,
                    maximumPopulation: 48,
                    defaults.FounderProfile,
                    cognitionEnabled: true,
                    physiologyEnabled: true,
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                    plantCohortsEnabled: true);
                ExperimentResult result = ExperimentRunner.Run(config, Prototype4Scenarios.ConsumerDefenseCalibrationControl, ticks: 12000);
                Assert.That(result.FinalStatistics.Population, Is.GreaterThan(0), $"Seed {seed} went extinct.");
            }
        }
```

- [ ] **Step 6: Run the new test and the full suite**

Run: `cd tools/HeadlessTests && dotnet test --filter ConsumerDefenseCalibrationControlSustainsNonzeroPopulationAcrossAllSeeds`
Expected: PASS — nonzero population in all 5 seeds.

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite green.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Simulation/Experiments/SimulationScenario.cs Assets/Tests/EditMode/ResourceExperimentTests.cs
git commit -m "Retune P4 consumer-defense calibration scenario to survive under corrected plant growth"
```

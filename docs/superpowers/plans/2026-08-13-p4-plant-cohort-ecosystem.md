# P4 Plant-Cohort Ecosystem Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make plant cohorts the deterministic source of food biomass while preserving current creature behavior and experiments.

**Architecture:** A fixed-capacity `PlantPatchStore` owns producer biomass and traits. Existing food `ResourceStore` entries remain stable compatibility projections during this milestone; allocation routes consumption back to a paired plant patch. Environment sampling is an array-backed simulation service, never Unity terrain truth.

**Tech Stack:** Unity 6, C#, NUnit EditMode, pure simulation core.

## Global Constraints

- Simulation truth remains separate from Unity `GameObject` presentation.
- Existing P0--P3 configurations default to static renewable food and retain their deterministic behavior.
- Plant mode uses fixed-capacity arrays, stable IDs, explicit loops, and no managed allocations/delegates/throws in fixed-tick hot paths.
- Food resource IDs and their positions remain stable during a run.
- Plant quantities are non-negative; biomass accounting is reported and tested.
- Do not add individual plants, terrain generation, biomes, full Burst conversion, species labels, or planet presentation in this plan.

---

## File structure

- `Assets/Scripts/Simulation/Environment/PlantTypes.cs` — value IDs, patch state, and immutable definitions.
- `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs` — fixed-capacity authoritative plant state and resource pairing.
- `Assets/Scripts/Simulation/Environment/EnvironmentField.cs` — deterministic constant/sampleable moisture, fertility, temperature field API.
- `Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs` — growth, consumption reconciliation, and resource projection.
- `Assets/Scripts/Simulation/Core/SimulationConfig.cs` — explicit plant-mode settings, disabled by default.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs` — scheduling, setup, statistics, and compatibility bridge ownership.
- `Assets/Scripts/Simulation/Core/SimulationTypes.cs` — P4 statistics fields.
- `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs` — opt-in plant-backed scenarios only.
- `Assets/Tests/EditMode/PlantPatchStoreTests.cs` — store, pairing, and projection tests.
- `Assets/Tests/EditMode/PlantGrowthTests.cs` — growth/conservation/determinism tests.
- `Assets/Tests/EditMode/CoreSimulationTests.cs` — world scheduling and legacy regression tests.

### Task 1: Define the fixed plant-cohort contract

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/PlantTypes.cs`
- Create: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`
- Test: `Assets/Tests/EditMode/PlantPatchStoreTests.cs`

**Consumes:** `SimVector2`, `ResourceId`.

**Produces:**
```csharp
public readonly struct PlantPatchId { public int Value { get; } }
public readonly struct PlantPatchState { public PlantPatchId Id { get; } public ResourceId FoodResourceId { get; } public SimVector2 Position { get; } public float Biomass { get; } public float Capacity { get; } public float GrowthRate { get; } public float WaterDemand { get; } public float Nutrition { get; } public float Defense { get; } }
public sealed class PlantPatchStore { public int Count { get; } public int Add(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float waterDemand, float nutrition, float defense); public PlantPatchState GetAt(int index); public float ConsumeForResource(int resourceIndex, float amount); public void SetBiomass(int index, float biomass); }
```

- [ ] **Step 1: Write failing store tests**
```csharp
[Test] public void PlantPatchUsesStablePairedFoodResourceAndClampsConsumption()
{
    var store = new PlantPatchStore(2);
    int index = store.Add(new ResourceId(7), new SimVector2(2f, 3f), 3f, 10f, .5f, .2f, 1.1f, .1f);
    Assert.That(store.GetAt(index).FoodResourceId.Value, Is.EqualTo(7));
    Assert.That(store.ConsumeForResource(index, 9f), Is.EqualTo(3f));
    Assert.That(store.GetAt(index).Biomass, Is.EqualTo(0f));
}
```
- [ ] **Step 2: Run the test and confirm it fails because the plant types do not exist.**
- [ ] **Step 3: Implement only the value types and array-backed store.** Validate capacity in setup only; hot methods clamp instead of throwing.
- [ ] **Step 4: Run all `PlantPatchStoreTests` and confirm stable IDs, capacity, and clamped consumption pass.**
- [ ] **Step 5: Commit** `feat: add fixed plant patch store`.

### Task 2: Add deterministic environment sampling and biomass growth

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/EnvironmentField.cs`
- Create: `Assets/Scripts/Simulation/Environment/PlantGrowthSystem.cs`
- Create: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Consumes:** `PlantPatchStore`, `ResourceStore`.

**Produces:**
```csharp
public readonly struct EnvironmentSample { public float Moisture { get; } public float Fertility { get; } public float Temperature { get; } }
public sealed class EnvironmentField { public EnvironmentSample Sample(SimVector2 position); }
public static class PlantGrowthSystem { public static float Step(PlantPatchStore patches, ResourceStore resources, EnvironmentField field, float deltaTime); public static void ProjectFoodResources(PlantPatchStore patches, ResourceStore resources); }
```

- [ ] **Step 1: Write failing tests for logistic growth under a constant field, zero growth under zero moisture, and food-resource projection.**
```csharp
[Test] public void GrowthProjectsPlantBiomassIntoItsPairedFoodResource()
{
    // Seed patch biomass 2, capacity 10, then step one second in an all-one field.
    PlantGrowthSystem.Step(patches, resources, field, 1f);
    Assert.That(resources.GetAt(foodIndex).Amount, Is.EqualTo(patches.GetAt(0).Biomass));
}
```
- [ ] **Step 2: Run the focused tests and confirm failure.**
- [ ] **Step 3: Implement constant initial fields and logistic growth.** Use `Math.Min(moisture, fertility, temperature)` as the limiting factor and clamp results to `[0, capacity]`. The step returns total added biomass; projection only changes paired food amount/nutrition, not food position/ID.
- [ ] **Step 4: Add conservation test:** initial biomass + reported growth - plant food consumed equals final biomass within `0.0001f`.
- [ ] **Step 5: Run focused tests and commit** `feat: add deterministic plant growth projection`.

### Task 3: Wire opt-in plant food through `SimulationWorld`

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Scripts/Simulation/Resources/ResourceAllocationSystem.cs`
- Modify: `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Consumes:** `PlantGrowthSystem.Step`, `PlantGrowthSystem.ProjectFoodResources`.

**Produces:**
```csharp
public bool EnablePlantCohorts { get; }
public SimulationWorld(...); // exposes existing Resources and new Plants read-only
```

- [ ] **Step 1: Write a Legacy regression test that two disabled-plant worlds retain the established identical state hash.**
- [ ] **Step 2: Write a plant-mode test that eating from a paired food resource reduces both its available amount and authoritative patch biomass.**
- [ ] **Step 3: Run both tests; confirm the new plant-mode test fails.**
- [ ] **Step 4: Add disabled-by-default config fields and construct plant patches only from food definitions when enabled.** Keep existing scenario resource creation unchanged when disabled.
- [ ] **Step 5: Schedule environment/plant growth immediately before creature perception; after allocation, reconcile allocated food with the paired patch, then reproject.** Water and carcasses bypass plant code.
- [ ] **Step 6: Run CoreSimulation tests plus full EditMode suite in Unity; commit** `feat: wire opt-in plant backed food`.

### Task 4: Add P4 accounting, diagnostics, and a minimal playable scenario

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationTypes.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`
- Modify: `Assets/Editor/PrototypeBatchEntry.cs`
- Modify: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Consumes:** plant growth return value, paired resource consumption.

**Produces:** `SimulationStatistics.TotalPlantBiomass`, `CumulativePlantGrowth`, `CumulativePlantFoodConsumed`, `DormantPlantPatchCount`, `PlantBiomassResidual` and an explicitly named plant-backed P4 scenario.

- [ ] **Step 1: Write a failing accounting test that runs a fixed plant scenario and asserts the reported residual magnitude is at most `0.0001f`.**
- [ ] **Step 2: Implement statistics accumulation in the world, calculated at the existing statistics cadence.** Do not scan from the Unity presenter.
- [ ] **Step 3: Add an Editor menu batch entry named `Life Simulation/Run Prototype 4 Plant Biomass Smoke Test`; it writes an ignored CSV with seed, population, plant biomass, growth, consumption, and residual.**
- [ ] **Step 4: Add `SimulationScenario.CreatePlantBackedBaseline` that opts in without changing any earlier factory defaults.**
- [ ] **Step 5: Run the smoke test once in Unity, inspect non-negative biomass and bounded residual, then run all EditMode tests.**
- [ ] **Step 6: Commit** `feat: report plant biomass accounting`.

### Task 5: Record the P4 baseline and define the ecological follow-on gate

**Files:**
- Create: `docs/experiments/p4-plant-biomass-baseline-YYYY-MM-DD.md`
- Modify: `README.md`
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Record the exact config, seeds, tick count, final population, plant accounting fields, and test result from Task 4.** State plainly that this is a biomass/compatibility milestone, not evidence of coevolution.
- [ ] **Step 2: Document the next three independent P4 slices:** plant genomes + clonal mutation, seed dispersal/competition, and plant-defense/consumer-digestion paired experiments.
- [ ] **Step 3: Update README with the plant-cohort simulation boundary and the Unity menu location.**
- [ ] **Step 4: Check `git status`, preserve unrelated Unity-generated files, then commit** `docs: record P4 plant biomass baseline`.

## Self-review

- **Spec coverage:** Tasks 1--4 implement authoritative cohorts, a food compatibility bridge, deterministic fields, growth/consumption accounting, stable IDs, tests, diagnostics, and an opt-in playable scenario. Task 5 records the result and sequences genes, dispersal, defenses, and climate experiments. Individual plants, terrain, biomes, LOD, species, and planet work are deliberately absent.
- **Placeholder scan:** No implementation steps rely on unspecified interfaces; the only date in the experiment record is filled at execution time because the filename represents the actual run date.
- **Type consistency:** `PlantPatchStore`, `EnvironmentField`, and `PlantGrowthSystem` are defined before world wiring. The world uses existing `ResourceStore` and `ResourceAllocationSystem` rather than an invented consumer API.


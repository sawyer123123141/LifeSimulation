# P4 Plant Heredity and Dispersal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deterministic clonal plant inheritance, bounded seed dispersal, and stable-slot establishment without changing existing animal resource contracts.

**Architecture:** Each active plant cohort gains one compact `PlantGenome`, a `PlantLineage`, and a seed budget. A scenario preallocates inactive food-resource slots; establishment activates one of those slots and pairs a child cohort to it. A plant is therefore authoritative while the resource ID remains stable from activation onward.

**Tech Stack:** Unity 6, C#, NUnit EditMode, pure simulation core.

## Global Constraints

- P0--P3 configurations remain disabled by default and deterministic.
- Plant reproduction is clonal mutation only; pollen/two-parent crossover is deferred.
- No allocation or `GameObject` creation occurs in the plant fixed-step loop.
- Plant genome traits are normalized `[0, 1]` and all have explicit countervailing costs.
- Establishment cannot create biomass: seed investment is removed from the parent before a child is created.

### Task 1: Add plant genome and lineage value contracts

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/PlantGenome.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantTypes.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`
- Test: `Assets/Tests/EditMode/PlantPatchStoreTests.cs`

- [ ] Write failing tests for normalized plant genes, deterministic clone mutation, and child lineage generation one greater than its parent.
- [ ] Add `PlantGenome` fields: growth, seed investment, water efficiency, nutrition, defense, dispersal, moisture tolerance, and temperature tolerance.
- [ ] Map traits to patch phenotype: growth increases growth rate and water demand; seed investment reduces edible capacity but increases seed budget; nutrition reduces growth; defense reduces projected edible nutrition and costs growth; dispersal reduces establishment probability; broad tolerance reduces growth.
- [ ] Add stable `PlantLineage` and store arrays for genome, lineage, age, and seed reserve; preserve current patch IDs and resource pairing.
- [ ] Run plant-focused tests and commit `feat: add plant genome and lineage`.

### Task 2: Preallocate stable resource slots for offspring

**Files:**
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`
- Modify: `Assets/Scripts/Simulation/Resources/ResourceStore.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs`

- [ ] Write a failing scenario test showing its configured inactive food slots have stable IDs, are absent from available-resource perception, and can be activated without adding/reordering resources.
- [ ] Add an explicit P4 `PlantSiteDefinition` containing position, capacity, and environmental suitability; scenario setup creates one food resource per site, active only when occupied.
- [ ] Reserve a fixed site capacity (initial target: 32) in the P4 scenario. The plant store maps a site/slot to its resource index, never scans or allocates resource entries during plant reproduction.
- [ ] Expose only setup-time activation/deactivation methods on `ResourceStore`; active plant projection sets amount/nutrition without changing ID/position/radius.
- [ ] Run CoreSimulation and plant tests, then commit `feat: reserve stable plant establishment sites`.

### Task 3: Add deterministic seed production and clonal establishment

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationTypes.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`
- Test: `Assets/Tests/EditMode/PlantGrowthTests.cs`

- [ ] Write failing tests proving identical seed/configuration yields identical child genome, parent biomass decreases by seed investment, and no child establishes if no inactive site is suitable.
- [ ] Add separate `RandomDomain.PlantMutation`, `PlantDispersal`, and `PlantEstablishment` values. Key all rolls by world seed, tick, parent patch ID, and seed ordinal.
- [ ] At plant reproduction cadence after growth and consumption: mature cohorts convert a capped biomass fraction into seeds; choose target sites by bounded deterministic samples; mutate a clone; require field suitability and vacant slot; establish a declared seed biomass.
- [ ] Track plant births, failed establishments, and dormant/recovered cohorts. Plant death/deactivation releases only its paired reserved slot after biomass reaches zero and its seed reserve is exhausted.
- [ ] Run focused tests plus full EditMode suite and commit `feat: add deterministic plant seed establishment`.

### Task 4: Add the first heredity experiment and reporting

**Files:**
- Modify: `Assets/Editor/PrototypeBatchEntry.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationTypes.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Create: `docs/experiments/p4-plant-heredity-baseline-YYYY-MM-DD.md`

- [ ] Add mean plant trait, highest plant generation, plant birth/death, and active-site statistics.
- [ ] Add `Life Simulation/Run Prototype 4 Plant Heredity Smoke Test` with three fixed seeds and output columns for plant generations, trait means, births, biomass residual, and state hash.
- [ ] Run it twice with identical seeds and require matching output/state hashes before interpreting trait drift.
- [ ] Record the results as heredity/determinism evidence only; do not claim selection until the following defense/digestion or climate-pressure controls.
- [ ] Commit `feat: add plant heredity smoke experiment`.

## Self-review

The plan separates value contracts, stable-resource migration, reproduction, and evidence. It prevents the two high-risk shortcuts: silently adding food slots mid-run and treating visual plants as simulation truth. Plant competition, defense/digestion selection, rainfall, and biome terrain remain later P4/P6 work.


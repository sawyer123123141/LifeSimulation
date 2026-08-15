# Plant System Phase 1 Fixes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix four gaps between the plant system's design intent and what's actually built, found by auditing `Assets/Scripts/Simulation/Environment/` against `docs/superpowers/specs/2026-08-13-p4-plant-cohort-ecosystem-design.md` and `2026-08-13-p4-plant-heredity-and-dispersal.md`: a dead `WaterDemand` field, a `Dispersal` gene with no cost, seed-site selection that can silently fail or degrade at scale, and reproduction with no pacing.

**Architecture:** No new subsystems. `WaterDemand` is removed rather than fake-wired — there is no soil-water store to deplete yet, and pretending a formula change is a budget would be dishonest complexity (real soil moisture is Phase 2, alongside terrain). A new `PlantSiteRegistry` gives seed dispersal a fixed, pre-built list of eligible target slots instead of scanning the whole resource array. `Dispersal`'s cost becomes a real establishment-probability penalty that grows with how far a seed traveled relative to its own range, computed as a pure testable function. A per-patch cooldown after a successful establishment paces reproduction.

**Tech Stack:** Unity 6, C# 9, NUnit EditMode. Verify with the headless harness (`tools/HeadlessTests`, `dotnet test`) — no Unity required for any task in this plan.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` references in `Assets/Scripts/Simulation/`. No LINQ. No allocation in per-tick hot paths.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`. All randomness through `DeterministicRandom`, keyed by `(worldSeed, domain, tick/ordinal, entityA, entityB, purpose)`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. Edit only the files each task names.
- Plant quantities are non-negative; existing conservation/determinism tests must keep passing unchanged.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Simulation/Environment/PlantTypes.cs` | `PlantPatchState`/`PlantLineage` value types | 1, 4 |
| `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs` | Fixed-capacity plant state array | 1, 4 |
| `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs` | Seed production, site search, establishment | 1, 3, 4 |
| `Assets/Scripts/Simulation/Environment/PlantSiteRegistry.cs` | New. Fixed list of eligible dispersal targets | 2 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Owns `Plants`, `PlantSites`; schedules plant systems | 1, 2, 4 |
| `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs` | Builds scenarios; registers plant sites | 1, 2 |
| `Assets/Tests/EditMode/PlantPatchStoreTests.cs` | Store/registry tests | 1, 2, 4 |
| `Assets/Tests/EditMode/PlantGrowthTests.cs` | Growth/reproduction tests | 1, 3, 4 |

---

### Task 1: Remove the dead `WaterDemand` field

**Files:**
- Modify: `Assets/Scripts/Simulation/Environment/PlantTypes.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`
- Modify: `Assets/Tests/EditMode/PlantPatchStoreTests.cs`
- Modify: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Why:** `WaterDemand` is stored per-patch, threaded through `PlantPatchStore.Add`, `PlantPatchState`, and reproduction's clone-copy — and read nowhere. Every call site passes `0f`. It describes a soil-water budget that doesn't exist. Keeping it implies behavior that isn't there; a future reader would reasonably assume growth is water-limited by more than the moisture field. It is not.

**Contract:**

```csharp
// PlantPatchStore.cs
public int Add(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense);
// PlantPatchState no longer has a WaterDemand property; its constructor drops that parameter.
```

```csharp
// SimulationWorld.cs
public int AddPlantPatch(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense, bool countsAsInitialBiomass = true);
```

**Behaviour:**

| Case | Expectation |
|---|---|
| `PlantPatchStore.Add` with the new 7-argument signature | Compiles; behaves identically to the old call minus the removed argument |
| Existing growth, projection, and reproduction tests | Pass unchanged in outcome (only their `Add`/`AddPlantPatch` call sites lose one argument) |
| A repository-wide search for `WaterDemand` | Zero results outside this plan's diff |

- [ ] **Step 1: Update the existing tests' call sites first, so the compile error in Step 2 is the only one.**

In `PlantPatchStoreTests.cs`, change:
```csharp
int index = store.Add(new ResourceId(7), new SimVector2(2f, 3f), 3f, 10f, .5f, .2f, 1.1f, .1f);
```
to:
```csharp
int index = store.Add(new ResourceId(7), new SimVector2(2f, 3f), 3f, 10f, .5f, 1.1f, .1f);
```
and:
```csharp
patches.Add(resourceId, new SimVector2(0f, 0f), 3f, 10f, .5f, .2f, 1.25f, 0f);
```
to:
```csharp
patches.Add(resourceId, new SimVector2(0f, 0f), 3f, 10f, .5f, 1.25f, 0f);
```
and:
```csharp
int index = store.Add(new ResourceId(3), new SimVector2(0f, 0f), 1f, 2f, .1f, 0f, 1f, 0f);
```
to:
```csharp
int index = store.Add(new ResourceId(3), new SimVector2(0f, 0f), 1f, 2f, .1f, 1f, 0f);
```

In `PlantGrowthTests.cs`, change both:
```csharp
patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 0f, 1f, 0f);
```
to:
```csharp
patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 1f, 0f);
```
and:
```csharp
int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 0f, 1f, 0f);
```
to:
```csharp
int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
```

- [ ] **Step 2: Run the full suite and confirm it fails to compile** because `PlantPatchStore.Add` and `SimulationWorld.AddPlantPatch` still expect `waterDemand`.

Run: `cd tools/HeadlessTests && dotnet test`
Expected: build error, `PlantPatchStore` does not contain a matching `Add` overload.

- [ ] **Step 3: Remove `WaterDemand` from the production code.**

In `PlantTypes.cs`: drop the `waterDemand` constructor parameter and the `WaterDemand` property from `PlantPatchState`.

In `PlantPatchStore.cs`: drop the `_waterDemands` array, the `waterDemand` parameter from `Add`, its validation clause (`waterDemand < 0f`), its assignment, its `EnsureCapacity` resize line, and its position in the `GetAt` constructor call.

In `PlantReproductionSystem.cs`: in `Step`, change
```csharp
int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.WaterDemand, parent.Nutrition, parent.Defense);
```
to:
```csharp
int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.Nutrition, parent.Defense);
```

In `SimulationWorld.cs`: change
```csharp
public int AddPlantPatch(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float waterDemand, float nutrition, float defense, bool countsAsInitialBiomass = true)
{
    int patchIndex = Plants.Add(foodResourceId, position, biomass, capacity, growthRate, waterDemand, nutrition, defense);
```
to:
```csharp
public int AddPlantPatch(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense, bool countsAsInitialBiomass = true)
{
    int patchIndex = Plants.Add(foodResourceId, position, biomass, capacity, growthRate, nutrition, defense);
```

In `SimulationScenario.cs`: change
```csharp
int patchIndex = world.AddPlantPatch(resourceId, definition.Position, biomass, capacity, growthRate, waterDemand: 0f, nutrition: definition.NutritionMultiplier, defense: 0f);
```
to:
```csharp
int patchIndex = world.AddPlantPatch(resourceId, definition.Position, biomass, capacity, growthRate, nutrition: definition.NutritionMultiplier, defense: 0f);
```

- [ ] **Step 4: Run the full suite and confirm every test passes with no behavior change.**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, same count as before this task.

- [ ] **Step 5: Commit** `refactor: remove dead WaterDemand plant field`.

---

### Task 2: Add a `PlantSiteRegistry` of eligible dispersal targets

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/PlantSiteRegistry.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`
- Modify: `Assets/Tests/EditMode/PlantPatchStoreTests.cs`

**Why:** `PlantReproductionSystem.FindSite` currently samples a random index across the *entire* `ResourceStore` — food, water, active, inactive alike — and rejects most draws. In a scenario without pre-reserved inactive `Food` slots, dispersal can never succeed: no error, no diagnostic, just permanent silent failure. And success probability falls as total resource count grows even in scenarios that do reserve slots, since the 4 fixed attempts are spread over more resources. A registry built once from the actual reserved slots fixes both.

**Contract:**

```csharp
public sealed class PlantSiteRegistry
{
    public PlantSiteRegistry(int initialCapacity);
    public int Count { get; }
    public void Register(int resourceIndex);
    public int GetResourceIndexAt(int slot);
}
```

`Register` takes a `ResourceStore` index (not a `ResourceId`) — the same index space `ResourceState`/`ResourceStore.GetAt` already use, since resource indices are stable for the life of a run (per the existing plant-site reservation rule).

**Behaviour:**

| Case | Expectation |
|---|---|
| New registry | `Count` is `0` |
| `Register(5)` then `Register(2)` | `Count` is `2`; `GetResourceIndexAt(0)` is `5`; `GetResourceIndexAt(1)` is `2` |
| `Register` beyond initial capacity | Grows; no data loss, no allocation-related exception |
| `SimulationScenario.ApplyTo` with `PlantCohortsEnabled` on and a scenario containing inactive `Food` definitions | Every inactive `Food` resource's index is registered exactly once; active or non-`Food` resources are never registered |
| `SimulationScenario.ApplyTo` with `PlantCohortsEnabled` off | `world.PlantSites.Count` stays `0` — registration is skipped entirely, matching how plant patches themselves are only created when the flag is on |

- [ ] **Step 1: Write the failing registry test.**

```csharp
[Test]
public void PlantSiteRegistryReturnsRegisteredResourceIndicesInOrder()
{
    var registry = new PlantSiteRegistry(1);
    registry.Register(5);
    registry.Register(2);

    Assert.That(registry.Count, Is.EqualTo(2));
    Assert.That(registry.GetResourceIndexAt(0), Is.EqualTo(5));
    Assert.That(registry.GetResourceIndexAt(1), Is.EqualTo(2));
}
```

Add this to `PlantPatchStoreTests.cs`.

- [ ] **Step 2: Run the test and confirm it fails to compile** because `PlantSiteRegistry` doesn't exist.

Run: `cd tools/HeadlessTests && dotnet test`
Expected: build error, type or namespace `PlantSiteRegistry` not found.

- [ ] **Step 3: Implement `PlantSiteRegistry`** as a fixed-capacity, doubling-resize `int[]` store, following the exact pattern `PlantPatchStore` already uses for `EnsureCapacity`.

- [ ] **Step 4: Wire it into `SimulationWorld` and `SimulationScenario`.**

In `SimulationWorld.cs`, alongside the existing `Plants = new PlantPatchStore(initialCapacity: 8);` line, add:
```csharp
PlantSites = new PlantSiteRegistry(initialCapacity: 8);
```
and add the property next to `public PlantPatchStore Plants { get; }`:
```csharp
public PlantSiteRegistry PlantSites { get; }
```

In `SimulationScenario.cs`, inside `ApplyTo`'s existing `if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food && definition.IsActive)` block, add the paired `else` that registers inactive food sites:
```csharp
else if (world.Config.PlantCohortsEnabled && definition.Kind == ResourceKind.Food && !definition.IsActive)
{
    world.PlantSites.Register(index);
}
```
Place this as a sibling branch to the existing `if`, keyed off the same loop's resource `index` (the position in `world.Resources` the definition was just added at — `ApplyTo` adds resources in the same order as `_resources`, so `index` is also the resource's store index here; confirm this holds by checking `ResourceStore.Add` appends rather than reorders before relying on it).

- [ ] **Step 5: Run the full suite and confirm the new test passes and nothing else changed.**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, one more than before.

- [ ] **Step 6: Commit** `feat: add plant site registry for dispersal targets`.

---

### Task 3: Rewrite `FindSite` to use the registry and give `Dispersal` a real cost

**Files:**
- Modify: `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Why:** Two defects, one rewrite. `FindSite` scans all resources instead of the registry (see Task 2). And the `Dispersal` gene widens the search radius with no downside — the design document says "long dispersal reduces local establishment efficiency," but nothing in code reduces anything. Fix: establishment success probability now falls as the chosen site's distance approaches the plant's dispersal range, so reaching further is possible but not free.

**Contract:**

```csharp
public static class PlantReproductionSystem
{
    // Existing signature, now also takes the registry:
    public static int Step(PlantPatchStore patches, ResourceStore resources, PlantSiteRegistry sites, int worldSeed, long tick, ref long seedOrdinal);

    // New, pure and independently testable:
    public static float EstablishmentSuccessProbability(float distance, float dispersalRange);
}
```

`EstablishmentSuccessProbability` returns `1f - Clamp01(distance / Max(0.01f, dispersalRange))`, i.e. a site right next to the parent succeeds nearly always; a site at the edge of the dispersal range succeeds rarely.

**Behaviour:**

| Case | Expectation |
|---|---|
| `distance` is `0` | Returns `1f` |
| `distance` equals `dispersalRange` | Returns `0f` |
| `distance` is half of `dispersalRange` | Returns `0.5f` |
| `distance` exceeds `dispersalRange` | Returns `0f`, not negative |
| `dispersalRange` is `0` | Does not throw or divide by zero; returns `0f` for any positive distance |
| `PlantSiteRegistry` has zero registered sites | `Step` produces zero births, no exception |
| A site within range fails its establishment roll | `Step` tries the next of the 4 attempts rather than stopping |
| Same seed, tick, and ordinal | Same establishment outcome every run (determinism unchanged) |

- [ ] **Step 1: Write the failing tests.**

```csharp
[Test]
public void EstablishmentSuccessProbabilityFallsLinearlyWithDistanceAcrossDispersalRange()
{
    Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(0f, 10f), Is.EqualTo(1f));
    Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(10f, 10f), Is.EqualTo(0f));
    Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(5f, 10f), Is.EqualTo(.5f).Within(.0001f));
    Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(15f, 10f), Is.EqualTo(0f));
}
```

Add this to `PlantGrowthTests.cs`. Add the remaining behaviour-table rows (empty registry, retry-on-failed-roll, determinism) as additional `[Test]` methods following the existing `MaturePlantTransfersBiomassToADeterministicClonalSeedling` test's style — construct a `PlantSiteRegistry`, register the resource indices the test needs, and pass it into `Step`.

- [ ] **Step 2: Run the tests and confirm they fail to compile** because `EstablishmentSuccessProbability` doesn't exist and `Step` doesn't accept a registry.

Run: `cd tools/HeadlessTests && dotnet test`

- [ ] **Step 3: Implement.** Add `EstablishmentSuccessProbability` per its contract. Rewrite `FindSite` to take `PlantSiteRegistry sites` and `float dispersal` (the genome's raw `[0,1]` dispersal value, not `DispersalRange`), loop over `sites.Count` slots via `sites.GetResourceIndexAt`, and for each candidate within range roll a second deterministic value using `RandomDomain.PlantEstablishment` (already declared, currently unused) against `EstablishmentSuccessProbability(distance, range)` before accepting it. Update `Step`'s call to `FindSite` to pass `sites` and `parent.Genome.Dispersal`.

In `SimulationWorld.cs`, update the existing call:
```csharp
_plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, Config.WorldSeed, nextTick, ref _plantSeedOrdinal);
```
to:
```csharp
_plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, ref _plantSeedOrdinal);
```

- [ ] **Step 4: Run the full suite and confirm all tests pass**, including the existing `MaturePlantTransfersBiomassToADeterministicClonalSeedling` test — update its setup to construct and populate a `PlantSiteRegistry` (register the one inactive site it creates) and pass it to `Step`, since the signature changed.

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all pass.

- [ ] **Step 5: Commit** `feat: use plant site registry and add dispersal-distance establishment cost`.

---

### Task 4: Per-patch reproduction cooldown

**Files:**
- Modify: `Assets/Scripts/Simulation/Environment/PlantTypes.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Why:** A mature patch (biomass ≥ 75% capacity) currently retries reproduction every single tick it's due, until either biomass drops back under the threshold or the registry runs out of valid nearby sites. Seed cost is only 2–12% of biomass, so one mature patch can burst-spawn several children in quick succession — fast, uncontrolled colonization rather than paced spread. A cooldown after each successful establishment paces it.

**Contract:**

```csharp
// PlantTypes.cs — PlantPatchState gains:
public float ReproductionCooldownRemaining { get; }
```

```csharp
// PlantReproductionSystem.cs — Step gains a deltaTime parameter:
public static int Step(PlantPatchStore patches, ResourceStore resources, PlantSiteRegistry sites, int worldSeed, long tick, float deltaTime, ref long seedOrdinal);

private const float ReproductionCooldownSeconds = 20f;
```

**Behaviour:**

| Case | Expectation |
|---|---|
| A newly-created patch | `ReproductionCooldownRemaining` is `0` |
| A mature patch that just established a child | Its `ReproductionCooldownRemaining` becomes `ReproductionCooldownSeconds` |
| `Step` called again immediately with the same mature, still-eligible parent | It is skipped — cooldown has not elapsed, no second birth from the same parent this call |
| `Step` called repeatedly until cumulative `deltaTime` exceeds `ReproductionCooldownSeconds` | Cooldown reaches `0`; the parent becomes eligible again |
| A parent whose reproduction attempt fails (no valid site found) | Cooldown is **not** set — only a successful establishment starts it |
| Existing `MaturePlantTransfersBiomassToADeterministicClonalSeedling` test | Still passes with a `deltaTime` argument added to its `Step` call |

- [ ] **Step 1: Write the failing tests.**

```csharp
[Test]
public void SuccessfulEstablishmentStartsAReproductionCooldownOnTheParent()
{
    var resources = new ResourceStore(1);
    ResourceId childSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
    resources.SetActive(childSite, false);
    var sites = new PlantSiteRegistry(1);
    sites.Register(0);
    var patches = new PlantPatchStore(2);
    int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
    long ordinal = 0;

    PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);

    Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.EqualTo(PlantReproductionSystem.ReproductionCooldownSeconds));
}
```

Add this to `PlantGrowthTests.cs`. Add the remaining behaviour-table rows (skipped-while-on-cooldown, cooldown decay to zero, no cooldown on failed attempt) as additional `[Test]` methods in the same style. `ReproductionCooldownSeconds` needs to be `internal` or `public` for the test to reference it directly — match whichever visibility `MaturityFraction` and the other existing constants already use, and if they're `private`, expose the value the same way the test would need it (e.g. hardcode `20f` in the test with a comment noting it must match the constant, rather than changing the constant's visibility against the file's existing convention).

- [ ] **Step 2: Run the tests and confirm they fail to compile** because `ReproductionCooldownRemaining` and the new `Step` parameter don't exist.

Run: `cd tools/HeadlessTests && dotnet test`

- [ ] **Step 3: Implement.** Add `ReproductionCooldownRemaining` to `PlantPatchState` and a backing `_reproductionCooldowns` array to `PlantPatchStore`, following the exact pattern already used for `_ages`/`_seedReserves` (default `0f`, resized in `EnsureCapacity`, returned by `GetAt`). Add `PlantPatchStore.SetReproductionCooldown(int index, float value)`, clamped to non-negative. In `PlantReproductionSystem.Step`: skip a parent whose `ReproductionCooldownRemaining > 0f` after decrementing it by `deltaTime` (clamped at `0f`); on a successful establishment, call `patches.SetReproductionCooldown(parentIndex, ReproductionCooldownSeconds)`.

In `SimulationWorld.cs`, update the call site again:
```csharp
_plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, ref _plantSeedOrdinal);
```
to:
```csharp
_plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal);
```
(`resourceDeltaTime` is already computed two lines above this call, from Task 3's context.)

- [ ] **Step 4: Update the existing `MaturePlantTransfersBiomassToADeterministicClonalSeedling` test** to pass a `deltaTime` (e.g. `1f`) into its `Step` call, matching the new signature.

- [ ] **Step 5: Run the full suite and confirm all tests pass.**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all pass.

- [ ] **Step 6: Commit** `feat: add per-patch reproduction cooldown`.

---

## Completion checklist

- [ ] `WaterDemand` no longer exists anywhere in the plant system
- [ ] `PlantSiteRegistry` exists, is populated at scenario setup, and `FindSite` only ever samples from it
- [ ] `Dispersal` has a measurable, tested establishment cost that grows with distance
- [ ] A patch cannot reproduce again immediately after a successful establishment
- [ ] All four tasks committed separately
- [ ] Full headless suite (`cd tools/HeadlessTests && dotnet test`) passes after every task, not just at the end
- [ ] No allocation added to per-tick hot paths; `PlantSiteRegistry` follows `PlantPatchStore`'s existing doubling-resize pattern

## Self-review

- **Spec coverage:** all four items from the phased-spec Phase 1 list are covered — WaterDemand removal, Dispersal cost, site-registry fix, reproduction cooldown.
- **Placeholder scan:** no step describes what to do without showing the exact before/after code; the one deliberately open call (whether `ReproductionCooldownSeconds` needs a visibility change) gives an explicit fallback instead of leaving it vague.
- **Type consistency:** `PlantSiteRegistry`, `EstablishmentSuccessProbability`, and the growing `PlantReproductionSystem.Step` signature are introduced once and referenced identically in every later task and test.
- **Order:** Task 1 is independent. Task 2 must precede Task 3 (registry must exist before `FindSite` can use it). Task 3 must precede Task 4 (both touch `Step`'s signature; doing them in sequence avoids a double signature change in one diff). This is also commit order.

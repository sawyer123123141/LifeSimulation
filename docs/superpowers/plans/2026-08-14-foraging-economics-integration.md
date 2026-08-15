# Foraging Economics: Integration

> **For agentic workers:** Implement one task at a time, in order. Read `AGENTS.md` first.

**Goal:** wire the scoring functions into the simulation so creatures value patches by yield, commit to choices, and leave exhausted patches.

**Spec:** `docs/superpowers/specs/2026-08-14-foraging-economics-design.md`

**Prerequisite:** `2026-08-14-foraging-economics-scoring.md` must be complete. This plan calls the functions it built.

**Architecture:** a new gene, a new per-creature state sidecar, and a branch in the decision path. Everything is gated behind `ForagingEconomicsEnabled`, default off, so existing scenarios behave exactly as they do today.

**Tech Stack:** C# 9, Unity 6, Unity Test Framework, NUnit.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` in `Assets/Scripts/Simulation/`. No LINQ. No allocation in per-tick code.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. Edit only the files each task names.

## Read this before Task 1

**This plan changes the state hash.** Adding a gene means `ComputeStateHash` mixes an extra value, so recorded hashes change even with the flag off. That is expected and was expected when the cognition and physiology genes were added.

What must **not** change is behaviour. With `ForagingEconomicsEnabled = false`, every creature must make the same decisions and end in the same positions as before.

So the rule for this plan is different from the earlier ones:

- A **behaviour** test failing means you broke something. Stop.
- A **hash** test failing is expected once, at Task 2, and the plan says how to handle it.

If you are unsure which kind a failing test is, stop and report rather than guessing.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` | `Persistence` gene and its cost | 1, 2 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Hash the gene; update state; use scoring | 2, 4, 5 |
| `Assets/Scripts/Simulation/Core/SimulationTypes.cs` | `ForagingState` struct | 3 |
| `Assets/Scripts/Simulation/Core/CreatureStore.cs` | `ForagingState` sidecar array | 3 |
| `Assets/Scripts/Simulation/Core/SimulationConfig.cs` | Flag and five constants | 4 |
| `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` | Score patches with the new path when enabled | 5 |
| `Assets/Tests/EditMode/ForagingIntegrationTests.cs` | New. All tests in this plan | 1–6 |

---

## Task 1: The `Persistence` gene

**Files:** `GenomePhenotype.cs`, new test file.

**Contract:** `Genome` gains a `Persistence` property, set through a new optional constructor parameter placed **last**, defaulting to `0f`. `Phenotype` gains a `Persistence` property passed through from the genome.

Follow exactly how `LifespanTendency` is already declared, constructed, clamped, and carried into `Phenotype` — same ordering, same style. `WithBodySize` must carry the new field too.

**Behaviour:**

| Case | Expectation |
|---|---|
| Constructing a genome without specifying persistence | `Persistence` is `0f` |
| Constructing with a value above one or below zero | Clamped into `[0, 1]`, like every other gene |
| `Phenotype.FromGenome` | `Phenotype.Persistence` equals the genome value |
| Two genomes differing only in `Persistence` | Different `BasalEnergyCostMultiplier` — the gene costs something |
| `WithBodySize` | Preserves `Persistence` |

Row 4 is required by the project rule that every heritable trait carries an explicit cost. Add `Persistence` to the `maintenance` sum in `Phenotype.FromGenome` with a weight in the same range as the neighbouring genes.

- [ ] **Step 1:** Write one test per row in a new `ForagingIntegrationTests.cs`.
- [ ] **Step 2:** Run tests. Expected: compile error, `Persistence` does not exist.
- [ ] **Step 3:** Add the gene, the clamp, the maintenance term, and the phenotype passthrough.
- [ ] **Step 4:** Run tests. Expected: new tests pass. **Existing tests may now fail on hashes — do not fix them yet.** Task 2 handles that.
- [ ] **Step 5:** Commit — `feat: add Persistence gene with a maintenance cost`

---

## Task 2: Hash the gene and regenerate recorded hashes

**Files:** `SimulationWorld.cs`, plus any test file holding a recorded hash constant.

**Contract:** `ComputeStateHash` hashes `Persistence` alongside the other genome fields, in the same order they are declared.

**What to do about failing hash tests:** they are expected. For each one, confirm the *behaviour* is unchanged first, then update the recorded constant.

Confirming behaviour: run the same scenario for the same tick count and check creature count, positions, and needs against the pre-change values. Only if those match may you update a hash constant. **If any behaviour differs, stop and report — the gene has leaked into behaviour and it must not.**

**Behaviour:**

| Case | Expectation |
|---|---|
| Two worlds with identical seeds and configs | Identical hashes |
| Two worlds differing only in a creature's `Persistence` | Different hashes |
| A fixed scenario run twice | Same creature count, positions, and needs as before this plan began |

- [ ] **Step 1:** Write the three tests above.
- [ ] **Step 2:** Run them. Expected: the first two fail, the third passes.
- [ ] **Step 3:** Add `Persistence` to `ComputeStateHash`.
- [ ] **Step 4:** Run the full suite. Confirm behaviour tests pass; update recorded hash constants where behaviour is proven unchanged.
- [ ] **Step 5:** Commit — `feat: hash the Persistence gene and regenerate recorded hashes`

---

## Task 3: Per-creature foraging state

**Files:** `SimulationTypes.cs`, `CreatureStore.cs`, test file.

**Contract:** a mutable struct

```csharp
public struct ForagingState
{
    public float SecondsInCurrentAction;
    public float RecentIntakeRate;
}
```

stored in `CreatureStore` as a dense array with a `ref` accessor, following exactly how `MemoryState` is already stored — same array pattern, same growth in `EnsureCapacity`, same swap on `Remove`, same reset on `Add`.

**Behaviour:**

| Case | Expectation |
|---|---|
| A newly spawned creature | Both fields are zero |
| Mutating through the `ref` accessor | The change persists in the store |
| Removing a creature | The surviving creature's state is intact and matches its own id, not the removed one's |
| Growing past initial capacity | No data loss |

Row 3 is the one that catches a wrong swap-remove, which is the usual bug in this pattern.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the struct and the store array.
- [ ] **Step 4:** Run tests. Expected: pass, and all existing tests still pass.
- [ ] **Step 5:** Commit — `feat: add per-creature foraging state`

---

## Task 4: Configuration and state updates

**Files:** `SimulationConfig.cs`, `SimulationWorld.cs`, test file.

**Contract:** `SimulationConfig` gains `bool ForagingEconomicsEnabled` (default `false`) and five constants with documented defaults: `HandlingSeconds`, `ReferenceGain`, `CommitmentStrength`, `CommitmentHalfLifeSeconds`, `GiveUpSensitivity`. `Validate()` rejects non-positive values for all five.

`SimulationWorld` maintains `ForagingState` each tick:

- `SecondsInCurrentAction` increases while the chosen action is unchanged, and resets to zero when it changes.
- `RecentIntakeRate` is an exponential moving average of energy gained per second, updated where food is consumed.

**Behaviour:**

| Case | Expectation |
|---|---|
| A creature keeping the same action across ticks | `SecondsInCurrentAction` grows by the elapsed time |
| A creature changing action | `SecondsInCurrentAction` resets to zero on that tick |
| A creature that eats | `RecentIntakeRate` rises |
| A creature that stops eating | `RecentIntakeRate` decays toward zero |
| `Validate()` with any of the five constants at zero or below | Throws |
| Flag off | Behaviour unchanged from before this plan |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile errors on the new config members.
- [ ] **Step 3:** Add the flag, constants, validation, and the state updates.
- [ ] **Step 4:** Run tests. Expected: pass; existing behaviour tests still pass.
- [ ] **Step 5:** Commit — `feat: add foraging configuration and state tracking`

---

## Task 5: Score patches with the new path

**Files:** `DecisionSystem.cs`, `SimulationWorld.cs`, test file.

**Contract:** when `ForagingEconomicsEnabled` is true, food and water candidates are scored with `ForagingEconomics.PatchScore` instead of `Urgency × Availability`, plus `ForagingEconomics.CommitmentBonus` for whichever action the creature is already performing. When false, the existing path runs untouched.

`PatchScore` needs the candidate's remaining amount, so perception must carry it. Add the remaining amount to `ResourceObservation` as a new property set at construction; the existing constructor keeps working by defaulting it.

**Behaviour:**

| Case | Expectation |
|---|---|
| Flag on, rich far patch vs depleted near patch | Chooses the rich far one |
| Flag off, same scenario | Chooses the near one, as today |
| Flag on, two near-tied options across successive decision ticks | Does not alternate between them |
| Flag on, patch costing more energy than it yields | Never selected |
| Flag off | Positions, needs, and decisions identical to before this plan |

Row 2 and row 5 together are what prove the gate works. Row 4 is what stops creatures starving beside stripped food.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1, 3, and 4 fail.
- [ ] **Step 3:** Add the remaining-amount property to `ResourceObservation`, then branch the scoring on the flag.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: score patches by net energy gain when enabled`

---

## Task 6: Abandon exhausted patches

**Files:** `SimulationWorld.cs`, test file.

**Contract:** when the flag is on, a creature currently feeding calls `ForagingEconomics.ShouldAbandon` each decision tick with its current patch intake rate and its `RecentIntakeRate`. Abandoning clears the target and resets `SecondsInCurrentAction`.

**Behaviour:**

| Case | Expectation |
|---|---|
| A creature on a patch draining below its recent average | Stops targeting it within a few decision ticks |
| Two creatures differing only in `Persistence` | The higher-persistence one abandons later |
| A creature on a rich patch | Does not abandon |
| A scenario run to depletion, flag on vs flag off | Fewer starvation deaths occur within interaction radius of a depleted resource with the flag on |
| Flag off | Behaviour unchanged |

Row 4 is the exit gate for the whole spec — it is the measurable form of "creatures leave exhausted patches instead of dying on them". Use the death-cause counters from `2026-08-14-death-causes.md` if that plan is complete; otherwise count deaths by proximity to a depleted resource.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1, 2, and 4 fail.
- [ ] **Step 3:** Wire in `ShouldAbandon`.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: abandon patches once intake falls below the habitat average`

---

## Completion checklist

- [ ] Six tasks committed
- [ ] With the flag off, positions, needs, and decisions match pre-plan behaviour exactly
- [ ] Recorded hashes regenerated, and only after behaviour was proven unchanged
- [ ] `Persistence` carries a maintenance cost
- [ ] No allocation added to per-tick code
- [ ] No existing test weakened or deleted

If you could not run the Unity tests, say so rather than describing the work as verified.

# Mating Behaviour

> **For agentic workers:** Implement one task at a time, in order. Read `AGENTS.md` first.

**Goal:** make reproduction something creatures do — notice a mate, travel to it, court, conceive — instead of something that happens when two of them stand close enough.

**Spec:** `docs/superpowers/specs/2026-08-14-mating-behaviour-design.md`

**Prerequisite:** both foraging plans complete. Approach only works because of commitment; without it a creature abandons the trip halfway.

**Architecture:** a top-K perception query, a new mating system, a courtship state, and one new gene. `ReproductionSystem` stops searching for pairs and starts confirming pairs that behaviour produced. Gated behind `MatingBehaviourEnabled`, default off.

**Tech Stack:** C# 9, Unity 6, Unity Test Framework, NUnit.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` in `Assets/Scripts/Simulation/`. No LINQ. No allocation in per-tick code.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. Edit only the files each task names.

**Hard rule specific to this plan:** no species identifier, cluster id, or category label may appear anywhere in the mating path. Mate quality derives from genes and phenotype only. This is a permanent architectural principle, not a preference.

**Hash note:** adding `MateChoosiness` changes recorded hashes, exactly as `Persistence` did. Behaviour with the flag off must not change.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Simulation/Behavior/PerceptionSystem.cs` | Top-K creature query | 1 |
| `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` | `MateChoosiness` gene and cost | 2 |
| `Assets/Scripts/Simulation/Behavior/MatingSystem.cs` | New. Quality, distance, scoring | 2, 3 |
| `Assets/Scripts/Simulation/Core/SimulationTypes.cs` | `CourtshipState`, `Courting` action | 4 |
| `Assets/Scripts/Simulation/Core/CreatureStore.cs` | Courtship sidecar | 4 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Wire scoring and courtship | 3, 5 |
| `Assets/Scripts/Simulation/Biology/ReproductionSystem.cs` | Confirm courted pairs | 5 |
| `Assets/Tests/EditMode/MatingBehaviourTests.cs` | New. All tests in this plan | 1–6 |

---

## Task 1: See more than one creature

**Files:** `PerceptionSystem.cs`, new test file.

**Contract:**

```csharp
public static int FindNearbyCreatures(
    CreatureStore creatures,
    UniformGrid creatureGrid,
    SimVector2 origin,
    float visionRange,
    CreatureId excludedCreatureId,
    Span<CreatureObservation> results);
```

Fills `results` with up to its length, nearest first, and returns how many were written. **Keep the existing `FindNearestOtherCreature` unchanged** — predation still uses it.

Choice among candidates is impossible with a single result, which is why this task exists before any mating logic.

**Behaviour:**

| Case | Expectation |
|---|---|
| More candidates in range than the span holds | Fills the span, returns its length, keeps the nearest |
| Fewer candidates than the span holds | Returns the actual count; the rest of the span is untouched |
| Results ordering | Strictly nearest first |
| The excluded creature | Never appears in results |
| Creatures beyond vision range | Never appear |
| Empty span passed in | Returns zero, throws nothing |
| Repeated identical calls | Identical results, no allocation |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement the query, reusing the grid-cell walk `FindNearestOtherCreature` already uses.
- [ ] **Step 4:** Run tests. Expected: pass; existing predation tests unchanged.
- [ ] **Step 5:** Commit — `feat: add a top-K nearby creature query`

---

## Task 2: Mate quality and the choosiness gene

**Files:** `GenomePhenotype.cs`, new `MatingSystem.cs`, test file.

**Contract:** `Genome` gains `MateChoosiness`, declared last with a default of `0f`, clamped, carried through `WithBodySize` and into `Phenotype` — following exactly how `LifespanTendency` is handled. It contributes to the `maintenance` sum, so it costs something.

`MatingSystem` gains two pure functions:

```csharp
public static float GeneticDistance(Genome first, Genome second);
public static float MateQuality(CreatureNeeds candidateNeeds, Phenotype candidatePhenotype, CombatState candidateCombat, Genome selfGenome, Genome candidateGenome, float mateChoosiness);
```

`GeneticDistance` is a normalised distance over genome fields, in `[0, 1]`. `MateQuality` combines candidate vigour (energy and health fractions), a wound penalty from `WoundSeverity`, and a choosiness term: above `0.5` prefers genetically similar candidates, below prefers dissimilar, at `0.5` ignores distance entirely.

**Behaviour:**

| Case | Expectation |
|---|---|
| A genome compared with itself | `GeneticDistance` is zero |
| Two maximally different genomes | Distance is one |
| Distance is symmetric | `d(a,b)` equals `d(b,a)` |
| Healthy well-fed candidate vs starving one | Higher quality for the healthy one |
| Wounded candidate vs unwounded | Lower quality for the wounded one |
| Choosiness above `0.5` | Prefers the genetically closer of two otherwise identical candidates |
| Choosiness below `0.5` | Prefers the genetically further one |
| Choosiness exactly `0.5` | Both score equally |
| Two genomes differing only in `MateChoosiness` | Different `BasalEnergyCostMultiplier` |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the gene and both functions. Hash `MateChoosiness` in `ComputeStateHash` and regenerate recorded hashes **only after confirming behaviour is unchanged**, as in the foraging integration plan.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: add mate quality and the MateChoosiness gene`

---

## Task 3: `SeekMate` becomes a real decision

**Files:** `MatingSystem.cs`, `SimulationWorld.cs`, `SimulationConfig.cs`, test file.

**Contract:**

```csharp
public static float MateScore(float ownReadiness, float partnerReadiness, float mateQuality, float distance, Phenotype self, float energyBudgetForCourtship);
```

Travel cost uses `ForagingEconomics.TravelEnergy` — the same energy expression foraging uses. Score falls to zero when travel energy exceeds the budget.

Readiness reuses the existing conditions in `ReproductionSystem.IsReady`. Extract them into a shared method rather than duplicating the thresholds.

When the flag is on, a ready creature scores its nearby candidates and may choose `CreatureAction.SeekMate`.

New config: `MatingBehaviourEnabled`, `MateCandidateLimit`, `EnergyBudgetForCourtship`, `CourtshipThreshold`, `CourtshipSeconds`.

**Behaviour:**

| Case | Expectation |
|---|---|
| A ready creature with a ready partner in range | Produces `SeekMate` |
| A creature that is not ready | Never produces `SeekMate` |
| A ready creature whose only candidate is not ready | Does not produce `SeekMate` |
| Two candidates, one higher quality | Targets the higher-quality one |
| A candidate whose travel energy exceeds the budget | Score is zero, not pursued |
| A starving creature with a mate available | Foraging outscores mating |
| Flag off | No `SeekMate` is ever produced; behaviour unchanged |

Row 6 matters: mating must lose to survival, or creatures will court themselves to death.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1, 3, 4, 5 fail.
- [ ] **Step 3:** Extract readiness, add `MateScore`, add config, branch the decision path on the flag.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: score mate candidates as a real action`

---

## Task 4: Courtship state

**Files:** `SimulationTypes.cs`, `CreatureStore.cs`, test file.

**Contract:** `CreatureAction.Courting` added to the action enum, appended last so existing values keep their numbers. A struct:

```csharp
public struct CourtshipState
{
    public CreatureId Partner;
    public float SecondsRemaining;
    public bool IsCourting;
}
```

stored as a dense sidecar in `CreatureStore`, following the `MemoryState` pattern.

**Behaviour:**

| Case | Expectation |
|---|---|
| A newly spawned creature | Not courting, no partner |
| Existing `CreatureAction` values | Unchanged numbering after adding `Courting` |
| Removing a creature | Survivor's courtship state intact and its own |
| Growing past initial capacity | No data loss |

Row 2 matters because action values are hashed; renumbering them would break every recorded hash for no reason.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the action, the struct, and the sidecar.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: add courtship state`

---

## Task 5: Courtship, and reproduction stops manufacturing pairs

**Files:** `SimulationWorld.cs`, `ReproductionSystem.cs`, test file.

**Contract:** two mutually seeking creatures within `MateDistance`, both scoring above `CourtshipThreshold`, enter courtship for `CourtshipSeconds`. On completion, the pair is handed to `ReproductionSystem`, which runs its **existing** crossover, mutation, placement, cost, and cooldown logic unchanged.

Courtship breaks if either creature dies, chooses a different action, or moves beyond `MateDistance`.

When the flag is on, `ReproductionSystem.Step` no longer searches the grid for proximate pairs. When off, it behaves exactly as today.

**Behaviour:**

| Case | Expectation |
|---|---|
| Two mutually seeking adjacent creatures | Both enter `Courting` |
| Courtship running to completion | Exactly one child, both parents recorded in lineage |
| Courtship interrupted by a threat | No child; neither cooldown consumed |
| One partner dying mid-courtship | The other exits courtship cleanly |
| Flag on | No birth occurs without a completed courtship |
| Flag off | Births occur exactly as they do today |
| Child genome | Produced by the same crossover and mutation as before |

Row 5 is the point of the whole plan. Row 7 guarantees this is a mechanism change, not a genetics change.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1–5 fail.
- [ ] **Step 3:** Implement the courtship transitions and branch `ReproductionSystem.Step` on the flag.
- [ ] **Step 4:** Run tests. Expected: pass; flag-off reproduction tests unchanged.
- [ ] **Step 5:** Commit — `feat: conceive through courtship instead of proximity`

---

## Task 6: No labels, and the birth rate did not move

**Files:** test file only.

**Contract:** no code change expected. Two properties that are easy to violate accidentally.

**Behaviour:**

| Case | Expectation |
|---|---|
| A grep of `MatingSystem.cs` and the mating path | No species, cluster, or category identifier appears |
| Baseline scenario, flag on vs flag off, same seed | Total births within a documented tolerance of each other |

The second is the important one. If courtship halves the birth rate, this stopped being a mechanism change and became a fertility change, and the constants need tuning before the plan is done.

- [ ] **Step 1:** Write both tests. For the first, assert on the mate-quality inputs rather than grepping — `MateQuality` takes genomes and phenotypes only, so a label could not reach it.
- [ ] **Step 2:** Run tests. Expected: the first passes. The second may fail.
- [ ] **Step 3:** If births moved outside tolerance, tune `CourtshipThreshold` and `CourtshipSeconds` until it holds. Do not change crossover, mutation, or readiness thresholds.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `test: confirm mating is a mechanism change, not a fertility change`

---

## Completion checklist

- [ ] Six tasks committed
- [ ] Every birth follows a completed courtship when the flag is on
- [ ] Flag off reproduces today's behaviour exactly
- [ ] Birth rate unchanged within tolerance between the two paths
- [ ] No species or cluster label anywhere in the mating path
- [ ] `MateChoosiness` carries a maintenance cost
- [ ] Recorded hashes regenerated only after behaviour was proven unchanged
- [ ] No allocation added to per-tick code

If you could not run the Unity tests, say so rather than describing the work as verified.

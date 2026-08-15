# Place Memory

> **For agentic workers:** Implement one task at a time, in order. Read `AGENTS.md` first.

**Goal:** replace three fixed memory slots with a capacity-N ring of remembered places, so a creature can prefer one patch over another and can be wrong about a place it has not visited lately.

**Spec:** `docs/superpowers/specs/2026-08-14-place-memory-design.md`

**Prerequisite:** both foraging plans complete. Remembered places are scored with `ForagingEconomics.PatchScore`.

**Architecture:** a dense sidecar of place records indexed by creature slot, with deterministic eviction. Gated behind the existing `CognitionEnabled` flag — no new flag.

**Tech Stack:** C# 9, Unity 6, Unity Test Framework, NUnit.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` in `Assets/Scripts/Simulation/`. No LINQ. No allocation in per-tick code.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. Edit only the files each task names.

**This plan changes behaviour when `CognitionEnabled` is true.** Cognition-mode recorded results will shift and must be re-run. Results recorded with cognition off must not change at all — if they do, stop.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Simulation/Core/SimulationTypes.cs` | `PlaceMemory` struct | 1 |
| `Assets/Scripts/Simulation/Core/CreatureStore.cs` | Dense place sidecar with slot ranges | 1 |
| `Assets/Scripts/Simulation/Behavior/MemorySystem.cs` | Observe, evict, decay, learn | 2, 3, 4 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Write perceptions; corrected outcome formula | 4, 5 |
| `Assets/Scripts/Simulation/Behavior/ForagingEconomics.cs` | Threat avoidance term | 6 |
| `Assets/Tests/EditMode/PlaceMemoryTests.cs` | New. All tests in this plan | 1–6 |

---

## Task 1: The place record and its storage

**Files:** `SimulationTypes.cs`, `CreatureStore.cs`, new test file.

**Contract:**

```csharp
public struct PlaceMemory
{
    public SimVector2 Position;
    public ResourceKind Kind;
    public float LastKnownAmount;
    public float OutcomeValue;
    public int VisitCount;
    public float Confidence;
    public long LastSeenTick;
}
```

`CreatureStore` holds one flat `PlaceMemory[]` sized `capacity × maximumSlots`, so slot `s` of creature `i` lives at `i × maximumSlots + s`. Expose the slot range for a creature and a `ref` accessor to a slot. Follow how `MemoryState` is already stored for growth, swap-remove, and reset.

Per-creature capacity comes from the genome:

```text
slots = MinimumMemorySlots + round(MemoryCapacity × AdditionalMemorySlots)
```

**Behaviour:**

| Case | Expectation |
|---|---|
| A newly spawned creature | All its slots are cleared, not inherited from a previous occupant |
| A high-`MemoryCapacity` genome vs a low one | More usable slots |
| Removing a creature | The surviving creature's slots are intact and belong to it |
| Growing past initial capacity | No slot data is lost or shifted between creatures |

Row 3 is where swap-remove bugs show up, and row 1 is where stale-slot bugs show up. Both are easy to get wrong and invisible without a test.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the struct, the flat array, the accessors, and the capacity formula. Add `MinimumMemorySlots` and `AdditionalMemorySlots` to `SimulationConfig`.
- [ ] **Step 4:** Run tests. Expected: pass; existing tests unchanged.
- [ ] **Step 5:** Commit — `feat: add per-creature place memory storage`

---

## Task 2: Observe a place, with deterministic eviction

**Files:** `MemorySystem.cs`, test file.

**Contract:** a method that records an observed place into a creature's slots. If the place is already remembered — same kind, within `SamePlaceRadius` — update it in place. Otherwise fill a free slot. If none is free, evict.

**Eviction order:** lowest `Confidence × OutcomeValue` first. Ties break by lowest `LastSeenTick`, then lowest slot index. No randomness anywhere.

**Behaviour:**

| Case | Expectation |
|---|---|
| Observing a new place with a free slot | Occupies the free slot, others untouched |
| Re-observing a known place | Updates it, does not create a duplicate |
| Observing with all slots full | Evicts the lowest `Confidence × OutcomeValue` entry |
| Two entries tied on that product | Evicts the one with the older `LastSeenTick` |
| The same observation sequence replayed | Produces byte-identical slot contents |
| Eviction candidate is a high-value entry | Not evicted, even if it is the oldest |

Row 6 distinguishes value-based eviction from age-based, which is the whole point.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement observe and evict. Add `SamePlaceRadius` to `SimulationConfig`.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: record observed places with deterministic eviction`

---

## Task 3: Confidence decay and failed searches

**Files:** `MemorySystem.cs`, test file.

**Contract:** a decay method reducing every slot's `Confidence` by the creature's `MemoryConfidenceDecayPerSecond`, and a failed-search method that sharply cuts the confidence of the specific place a creature travelled to and found empty.

The existing `RecordFailedSearch` cuts confidence by a fixed factor. Keep that factor; apply it to one place rather than to a whole resource kind.

**Behaviour:**

| Case | Expectation |
|---|---|
| Time passing | All confidences fall |
| Confidence reaching zero | Clamps at zero, never negative |
| A creature with higher `MemoryRetention` | Its confidence falls more slowly |
| A failed search at one place | Only that place's confidence drops sharply; others unaffected |
| Decay applied with zero elapsed time | Nothing changes |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement decay and per-place failed search.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: decay place confidence and penalise failed searches`

---

## Task 4: Learned value that is not saturated

**Files:** `MemorySystem.cs`, `SimulationWorld.cs`, test file.

**Contract:** feeding updates the `OutcomeValue` of the place fed at, as a running average weighted by the creature's `LearningRate`.

The outcome fed in is **intake measured against expectation**, not raw nutrition:

```text
outcome = Clamp01(actualIntakeRate / ExpectedIntakeRate)
```

`ExpectedIntakeRate` is a new `SimulationConfig` constant.

**This replaces the current formula**, which multiplies nutrition by 20 and clamps at 1, so it pins to 1.0 on essentially every feeding event and carries no information. Find both places in `SimulationWorld` where the old formula is used — one for food, one for water — and replace them.

**Behaviour:**

| Case | Expectation |
|---|---|
| Feeding at a rich place vs a poor place | Materially different `OutcomeValue` — not both near 1.0 |
| A typical feeding event | Outcome is strictly between 0 and 1, not clamped |
| Repeated feeding at one place | `OutcomeValue` converges toward that place's true quality |
| Higher `LearningRate` | Converges in fewer feedings |
| Feeding at one place | Other places' values unchanged |

Row 2 is the actual bug fix. Write it as an assertion that the value is strictly below 1.0 for a normal feeding event.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1 and 2 fail against the current formula.
- [ ] **Step 3:** Add `ExpectedIntakeRate`, implement per-place learning, replace both old call sites.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `fix: measure learned outcome against expectation instead of saturating`

---

## Task 5: Remembered places compete with visible ones

**Files:** `SimulationWorld.cs`, test file.

**Contract:** when nothing suitable is visible, a creature scores its remembered places with `ForagingEconomics.PatchScore`, substituting `LastKnownAmount` for observed amount and multiplying the result by that place's `Confidence`. The best-scoring remembered place becomes the travel target.

**Behaviour:**

| Case | Expectation |
|---|---|
| Two remembered places, equal distance, different `OutcomeValue` | Travels to the higher-value one |
| A visible patch and a remembered one of equal quality | Prefers the visible one, because confidence discounts memory |
| A remembered place whose confidence has decayed near zero | Not pursued |
| Travelling to a remembered place that is now empty | Arrives, finds nothing, and that place's confidence drops |
| `CognitionEnabled` false | Behaviour unchanged from today |

Row 4 is a P2 exit requirement: stale memory must cause a visible, explainable mistake. It is also the first behaviour in this project that looks like an animal being wrong.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1–4 fail.
- [ ] **Step 3:** Wire remembered-place scoring into the decision path.
- [ ] **Step 4:** Run tests. Expected: pass; cognition-off behaviour unchanged.
- [ ] **Step 5:** Commit — `feat: pursue remembered places, discounted by confidence`

---

## Task 6: Threat memory is finally read

**Files:** `ForagingEconomics.cs`, `SimulationWorld.cs`, test file.

**Contract:** a function returning an avoidance penalty for a candidate destination, summed over the creature's remembered threat places:

```csharp
public static float ThreatAvoidance(SimVector2 candidate, ReadOnlySpan<PlaceMemory> places, Phenotype phenotype, float falloffDistance);
```

Each remembered threat contributes `FearResponse × Confidence × falloff(distance)`. The total is subtracted from the destination's score.

Threat places are already written by `RememberThreat` and, until now, read by nothing.

**Behaviour:**

| Case | Expectation |
|---|---|
| A candidate far from every remembered threat | Zero penalty |
| A candidate near a remembered threat | Penalty above zero |
| Two creatures differing only in `Fear` | The more fearful one gets the larger penalty |
| A threat whose confidence has decayed | Smaller penalty than a fresh one |
| Two remembered threats near the candidate | Penalty larger than either alone |
| No remembered threats | Zero penalty, no allocation, no exception |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement avoidance and subtract it in the decision path. Add `ThreatFalloffDistance` to `SimulationConfig`.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: avoid places near remembered threats`

---

## Task 7: Memory does not cross generations

**Files:** test file only.

**Contract:** no code change expected. This task proves a rule the P2 spec states and that nothing currently enforces: offspring inherit cognition genes and nothing else.

**Behaviour:**

| Case | Expectation |
|---|---|
| A newborn from two parents with rich memories | Zero remembered places |
| The same newborn | Inherits `MemoryCapacity`, `MemoryRetention`, `LearningRate`, `Exploration` from crossover |
| A creature reusing a dead creature's store slot | No remembered places carried over |

If any of these fail, that is a real defect — fix it, and say so in the commit.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: pass. If any fails, fix the leak before continuing.
- [ ] **Step 3:** Commit — `test: prove learned memory dies with the creature`

---

## Completion checklist

- [ ] Seven tasks committed
- [ ] `MemoryCapacity` now buys slots as well as costing metabolism
- [ ] Learned outcome spans its range instead of pinning to 1.0
- [ ] Eviction is deterministic and value-based
- [ ] Threat memory affects decisions
- [ ] With `CognitionEnabled` false, behaviour is unchanged
- [ ] No allocation added to per-tick code

If you could not run the Unity tests, say so rather than describing the work as verified.

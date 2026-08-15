# Juvenile Behaviour

> **For agentic workers:** Implement one task at a time, in order. Read `AGENTS.md` first.

**Goal:** give young animals a real early life — weaker senses and slower legs first, then following a parent.

**Spec:** `docs/superpowers/specs/2026-08-14-juvenile-behaviour-design.md`

**Prerequisite:** the mating plan, for parent identity to be meaningful. The foraging plans, so following can lose to hunger.

**Architecture:** two stages. Stage 1 scales three existing phenotype values by age and adds no new state, no new gene, and no new action. Stage 2 adds a following action and one gene. Both behind `JuvenileBehaviourEnabled`, default off.

**Stage 1 is worth shipping alone.** It changes nothing visible on screen but makes juvenile mortality age-structured, which is what finally gives the existing `FertilityInvestment` gene something to trade against.

**Tech Stack:** C# 9, Unity 6, Unity Test Framework, NUnit.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` in `Assets/Scripts/Simulation/`. No LINQ. No allocation in per-tick code.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. Edit only the files each task names.

## File Structure

| File | Responsibility | Task |
|---|---|---|
| `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` | Maturity scaling; `ParentalAttachment` gene | 1, 4 |
| `Assets/Scripts/Simulation/Core/SimulationConfig.cs` | Flag and constants | 1, 4 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Use scaled phenotype; wire following | 2, 5 |
| `Assets/Scripts/Simulation/Behavior/JuvenileSystem.cs` | New. Follow scoring | 5 |
| `Assets/Scripts/Simulation/Core/SimulationTypes.cs` | `FollowParent` action | 5 |
| `Assets/Tests/EditMode/JuvenileBehaviourTests.cs` | New. All tests in this plan | 1–6 |

---

# Stage 1 — Reduced capability

## Task 1: Maturity scaling

**Files:** `GenomePhenotype.cs`, `SimulationConfig.cs`, new test file.

**Contract:**

```csharp
public Phenotype AtMaturity(float maturity);
```

on `Phenotype`. `maturity` is in `[0, 1]`. It returns a copy with three values scaled:

- `VisionRange` scaled from `JuvenileVisionFraction` at maturity zero, up to full at maturity one
- `MaximumSpeed` scaled from `JuvenileSpeedFraction` up to full
- `FearResponse` scaled from `JuvenileFearMultiplier` times normal, down to normal

Everything else is copied unchanged. New config: `JuvenileBehaviourEnabled`, `JuvenileVisionFraction`, `JuvenileSpeedFraction`, `JuvenileFearMultiplier`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Maturity of one | Every value equals the unscaled phenotype exactly |
| Maturity of zero | Vision and speed are at their juvenile fractions; fear is at its multiplier |
| Maturity of one half | Every scaled value lies strictly between the two ends |
| Increasing maturity | Vision and speed increase monotonically; fear decreases monotonically |
| Any value not listed above | Identical to the unscaled phenotype |
| Maturity outside `[0, 1]` | Throws `ArgumentOutOfRangeException` |

Row 5 catches the common mistake of rebuilding the phenotype and silently dropping a field.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the config values and `AtMaturity`.
- [ ] **Step 4:** Run tests. Expected: pass; existing tests unchanged.
- [ ] **Step 5:** Commit — `feat: scale juvenile senses, speed, and fear by maturity`

---

## Task 2: Use the scaled phenotype

**Files:** `SimulationWorld.cs`, test file.

**Contract:** when the flag is on, perception, movement, and decisions use `phenotype.AtMaturity(maturity)` where

```text
maturity = Clamp01(needs.Age / AdultAgeSeconds)
```

`AdultAgeSeconds` already exists in `ReproductionSystem` as a private constant. Move it to `SimulationConfig` so both can read it, and change nothing about its value or how reproduction uses it.

Metabolism keeps using the unscaled phenotype — a juvenile is not cheaper to run, it is just less capable.

**Behaviour:**

| Case | Expectation |
|---|---|
| A newborn, flag on | Its effective vision equals `JuvenileVisionFraction` of adult vision |
| A creature at `AdultAgeSeconds`, flag on | Effective values equal adult values |
| A juvenile, flag on | Moves measurably slower than an adult with the same genome |
| Metabolic cost | Identical for juvenile and adult with the same genome |
| Flag off | Positions, needs, and decisions identical to before this plan |
| Reproduction readiness age | Unchanged by moving the constant |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: rows 1–3 fail.
- [ ] **Step 3:** Move the constant, apply the scaled phenotype in the three places.
- [ ] **Step 4:** Run tests. Expected: pass; flag-off behaviour unchanged.
- [ ] **Step 5:** Commit — `feat: apply maturity-scaled capability to young creatures`

---

## Task 3: Juvenile mortality is age-structured

**Files:** test file only.

**Contract:** no code change expected. This proves stage 1 did something real, since nothing visible changed.

Use the death-cause counters from `2026-08-14-death-causes.md` if that plan is complete; otherwise count deaths by age band directly from the event buffer.

**Behaviour:**

| Case | Expectation |
|---|---|
| Baseline scenario, flag on | Deaths below `AdultAgeSeconds` are a higher share of total deaths than with the flag off |
| Same scenario | Adult mortality is not materially higher than with the flag off |

Row 2 guards against having made the whole population weaker rather than just the young.

- [ ] **Step 1:** Write both tests.
- [ ] **Step 2:** Run them. Expected: pass. If row 1 fails, the scaling is too weak to matter — report the measured shares rather than tuning constants until the test goes green.
- [ ] **Step 3:** Commit — `test: confirm juvenile mortality is age-structured`

**Stage 1 is complete here. Stage 2 may be deferred indefinitely.**

---

# Stage 2 — Parental following

## Task 4: The `ParentalAttachment` gene

**Files:** `GenomePhenotype.cs`, `SimulationConfig.cs`, test file.

**Contract:** `Genome` gains `ParentalAttachment`, declared last with a default of `0f`, clamped, carried through `WithBodySize`, contributing to `maintenance`, and passed into `Phenotype` — following exactly how `LifespanTendency` is handled. Add `LeashRadius` to `SimulationConfig`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Constructing without specifying it | Value is `0f` |
| Values outside `[0, 1]` | Clamped |
| `Phenotype.FromGenome` | Carries the value through |
| Two genomes differing only in this gene | Different `BasalEnergyCostMultiplier` |
| `WithBodySize` | Preserves it |

Hash the gene and regenerate recorded hashes only after confirming behaviour is unchanged, as in the foraging integration plan.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the gene, the cost, and the config value.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: add ParentalAttachment gene with a maintenance cost`

---

## Task 5: Follow a parent

**Files:** new `JuvenileSystem.cs`, `SimulationTypes.cs`, `SimulationWorld.cs`, test file.

**Contract:** `CreatureAction.FollowParent` appended last to the action enum, so existing values keep their numbers. Then:

```csharp
public static float FollowScore(float maturity, float parentalAttachment, float distanceToParent, float leashRadius);
```

returning `(1 − maturity) × parentalAttachment × Clamp01(1 − distanceToParent / leashRadius)`.

Parent identity comes from the existing `Lineage` record. If neither parent is alive, the score is zero. Following competes with other actions rather than overriding them.

**Behaviour:**

| Case | Expectation |
|---|---|
| A juvenile near a living parent, no urgent need | Produces `FollowParent` |
| The same juvenile, starving | Forages instead |
| The same juvenile, threatened | Flees instead |
| A juvenile whose parents are both dead | Never produces `FollowParent` |
| An adult | Score is zero regardless of parent distance |
| A juvenile beyond `LeashRadius` | Score is zero |
| Existing `CreatureAction` values | Unchanged numbering |
| Flag off | Behaviour unchanged |

Rows 2 and 3 are what keep following from killing juveniles.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Add the action, `FollowScore`, and the parent lookup, then wire it into the decision path behind the flag.
- [ ] **Step 4:** Run tests. Expected: pass.
- [ ] **Step 5:** Commit — `feat: let juveniles follow a surviving parent`

---

## Task 6: Family groups are real

**Files:** test file only.

**Contract:** no code change expected. Proves the payoff.

**Behaviour:**

| Case | Expectation |
|---|---|
| Baseline scenario, flag on | Mean distance between a juvenile and its nearest living parent is lower than with the flag off |
| Same scenario | Juvenile starvation deaths are not higher than with the flag off |

Row 2 guards against following having trapped juveniles on a patch their parent already stripped, which is the obvious failure mode.

- [ ] **Step 1:** Write both tests.
- [ ] **Step 2:** Run them. Expected: pass. If row 2 fails, following is outcompeting hunger — report the measured values rather than weakening the test.
- [ ] **Step 3:** Commit — `test: confirm juveniles stay near parents without starving`

---

## Completion checklist

**Stage 1**

- [ ] Tasks 1–3 committed
- [ ] Juvenile mortality is measurably age-structured
- [ ] Metabolic cost is unchanged by maturity
- [ ] Flag off reproduces today's behaviour exactly

**Stage 2**

- [ ] Tasks 4–6 committed
- [ ] Juveniles stay closer to parents without starving more
- [ ] `ParentalAttachment` carries a maintenance cost
- [ ] Existing `CreatureAction` values were not renumbered
- [ ] No allocation added to per-tick code

If you could not run the Unity tests, say so rather than describing the work as verified.

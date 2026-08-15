# Foraging Economics: Scoring Functions

> **For agentic workers:** Implement one task at a time, in order. Read `AGENTS.md` first.

**Goal:** build the pure scoring functions that value a patch, charge travel in energy, and decide when to abandon a patch.

**Spec:** `docs/superpowers/specs/2026-08-14-foraging-economics-design.md`

**Architecture:** one new static class of pure functions. Nothing calls it yet, nothing existing changes, no simulation behaviour moves. Integration is the sibling plan `2026-08-14-foraging-economics-integration.md`.

**Tech Stack:** C# 9, Unity 6, Unity Test Framework, NUnit.

## Global Constraints

From `AGENTS.md`:

- No `UnityEngine` in `Assets/Scripts/Simulation/`.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, `async`.
- No allocation in per-tick code. No LINQ.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names.
- Edit only the files each task names.

**Stop and report if** an existing test fails, or a required file is not listed in your task.

**This whole plan adds new files only.** If you find yourself editing an existing simulation file, you have gone off-plan — stop.

## File Structure

| File | Responsibility |
|---|---|
| `Assets/Scripts/Simulation/Behavior/ForagingEconomics.cs` | New. All scoring functions in this plan |
| `Assets/Tests/EditMode/ForagingEconomicsTests.cs` | New. All tests in this plan |

---

## Task 1: Expected gain from a patch

**Files:** create both files listed above.

**Contract:**

```csharp
namespace LifeSimulation.Simulation.Behavior
{
    public static class ForagingEconomics
    {
        public static float ExpectedGain(float remainingAmount, Phenotype phenotype, float nutritionMultiplier, float handlingSeconds);
    }
}
```

**What it must do:** a creature feeding for `handlingSeconds` ingests at `phenotype.IngestionRate`, but cannot take more than `remainingAmount`. What it takes is worth `phenotype.PlantFoodYieldMultiplier × nutritionMultiplier` per unit.

**Behaviour:**

| Case | Expectation |
|---|---|
| Patch holds far more than the creature can eat in `handlingSeconds` | Gain is limited by ingestion rate, not by amount |
| Patch holds less than the creature could eat | Gain is limited by amount |
| `remainingAmount` is zero | Gain is zero |
| Higher `PlantFoodYieldMultiplier` | Strictly higher gain, all else equal |
| Higher `nutritionMultiplier` | Strictly higher gain, all else equal |
| Negative or non-finite `remainingAmount` or `handlingSeconds` | Throws `ArgumentOutOfRangeException` |

**Template test** — copy this exactly, then write the rest in the same style:

```csharp
using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ForagingEconomicsTests
    {
        [Test]
        public void AnEmptyPatchIsWorthNothing()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);

            float gain = ForagingEconomics.ExpectedGain(
                remainingAmount: 0f,
                phenotype,
                nutritionMultiplier: 1f,
                handlingSeconds: 2f);

            Assert.That(gain, Is.EqualTo(0f));
        }
    }
}
```

- [ ] **Step 1:** Write the template test plus one test per row of the behaviour table.
- [ ] **Step 2:** Run EditMode tests. Expected: compile error, `ForagingEconomics` does not exist.
- [ ] **Step 3:** Create `ForagingEconomics.cs` and implement `ExpectedGain`. Validate arguments at entry, matching the pattern already used in `NeedsSystem.Tick`.
- [ ] **Step 4:** Run tests. Expected: all pass, and every existing test still passes.
- [ ] **Step 5:** Commit — `feat: add expected gain scoring for resource patches`

---

## Task 2: Travel cost in energy

**Files:** modify both files from Task 1.

**Contract:**

```csharp
public static float TravelEnergy(float distance, Phenotype phenotype);
```

**What it must do:** return the energy a creature spends moving `distance`.

**This must use the same expression `NeedsSystem.Tick` already charges for movement.** Open `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`, find where movement distance is turned into an energy cost inside `Tick`, and reuse that expression exactly. Do not invent a constant. Do not modify `NeedsSystem.cs`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Zero distance | Zero energy |
| Double the distance | Exactly double the energy |
| Heavier creature (larger `BodyMass`) | Strictly more energy for the same distance |
| Result matches `NeedsSystem` | For a chosen distance and phenotype, the value equals what `NeedsSystem.Tick` deducts for that same movement |
| Negative or non-finite distance | Throws `ArgumentOutOfRangeException` |

The fourth row is the important one. Write it as a real assertion, not a comment: construct a `CreatureNeeds`, call `NeedsSystem.Tick` with a known movement distance and zero elapsed time, and check the energy drop equals `TravelEnergy` for that distance.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error, `TravelEnergy` does not exist.
- [ ] **Step 3:** Implement `TravelEnergy`.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: charge foraging travel in energy using the movement formula`

---

## Task 3: Patch score

**Files:** modify both files.

**Contract:**

```csharp
public static float PatchScore(
    float urgency,
    float remainingAmount,
    float distance,
    Phenotype phenotype,
    float nutritionMultiplier,
    float handlingSeconds,
    float referenceGain);
```

**What it must do:** combine the previous two. Net gain is expected gain minus travel energy. Score is `urgency` times net gain normalised by `referenceGain`, clamped into `[0, 1]`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Rich patch far away vs depleted patch nearby | The rich far patch scores higher |
| Travel energy exceeds expected gain | Score is exactly zero |
| Urgency is zero | Score is zero regardless of patch quality |
| Two identical patches at different distances | The nearer scores higher |
| Heavier creature, same two patches | Its scores fall faster with distance than a lighter creature's |
| Any input non-finite, or `urgency` outside `[0, 1]`, or `referenceGain` at or below zero | Throws `ArgumentOutOfRangeException` |

Row 2 is the point of the whole task — it is what stops creatures travelling to patches that cost more than they yield. Row 5 is what makes body size change foraging range without extra tuning.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement `PatchScore` in terms of `ExpectedGain` and `TravelEnergy`. Do not duplicate their arithmetic.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: score resource patches by net energy gain`

---

## Task 4: Commitment bonus

**Files:** modify both files.

**Contract:**

```csharp
public static float CommitmentBonus(
    float secondsInCurrentAction,
    float persistence,
    float commitmentStrength,
    float commitmentHalfLifeSeconds);
```

**What it must do:** return a bonus added to the score of the action a creature is already performing, so near-ties do not flip every decision tick. The bonus decays exponentially with a half-life, so it never locks a creature in permanently.

Decay is `0.5` raised to the power of `secondsInCurrentAction / commitmentHalfLifeSeconds`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Zero seconds elapsed | Bonus equals `commitmentStrength × persistence` |
| Exactly one half-life elapsed | Bonus is half its starting value |
| Two half-lives elapsed | Bonus is a quarter of its starting value |
| Persistence of zero | Bonus is zero at any elapsed time |
| Increasing elapsed time | Bonus strictly decreases, never negative |
| Negative or non-finite inputs, or `commitmentHalfLifeSeconds` at or below zero | Throws `ArgumentOutOfRangeException` |

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement `CommitmentBonus`.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: add decaying commitment bonus for the current action`

---

## Task 5: Give-up rule

**Files:** modify both files.

**Contract:**

```csharp
public static bool ShouldAbandon(
    float currentPatchIntakeRate,
    float recentIntakeRate,
    float persistence,
    float giveUpSensitivity);
```

**What it must do:** decide whether to leave the current patch. This is the marginal value theorem: leave when here is worse than the habitat average. Abandon when `currentPatchIntakeRate` falls below `recentIntakeRate × (1 − persistence) × giveUpSensitivity`.

**Behaviour:**

| Case | Expectation |
|---|---|
| Current rate well above recent average | Does not abandon |
| Current rate far below recent average | Abandons |
| Two creatures, same rates, different persistence | The higher-persistence creature abandons at a lower current rate than the lower-persistence one |
| Persistence of one | Never abandons |
| `recentIntakeRate` is zero (creature has eaten nothing yet) | Does not abandon — there is no average to be worse than |
| Negative or non-finite inputs, or `persistence` outside `[0, 1]` | Throws `ArgumentOutOfRangeException` |

Row 3 is what produces visibly different personalities. Row 5 prevents newborns abandoning their first patch instantly.

- [ ] **Step 1:** Write one test per row.
- [ ] **Step 2:** Run tests. Expected: compile error.
- [ ] **Step 3:** Implement `ShouldAbandon`.
- [ ] **Step 4:** Run tests. Expected: all pass.
- [ ] **Step 5:** Commit — `feat: add marginal-value-theorem give-up rule`

---

## Task 6: Allocation guard

**Files:** modify the test file only.

**What it must do:** prove the scoring path allocates nothing, which the spec requires and which `AGENTS.md` requires of all per-tick code.

Write one test that calls `PatchScore` 100,000 times in a loop and asserts that managed allocation did not increase. Use `GC.GetTotalMemory(false)` before and after, and allow a small tolerance for test-harness noise rather than requiring exactly zero.

- [ ] **Step 1:** Write the test.
- [ ] **Step 2:** Run it. Expected: PASS. If it fails, something in the scoring path is boxing or allocating — report which function rather than adding tolerance until it passes.
- [ ] **Step 3:** Commit — `test: guard foraging scoring against allocation`

---

## Completion checklist

- [ ] Six tasks committed
- [ ] Only the two files in the File Structure table were created or edited
- [ ] No existing simulation file was modified
- [ ] Every existing test still passes
- [ ] Every function validates its arguments and throws `ArgumentOutOfRangeException` on bad input
- [ ] `TravelEnergy` provably agrees with `NeedsSystem`

Nothing calls `ForagingEconomics` yet. Wiring it into the simulation is the sibling plan, `2026-08-14-foraging-economics-integration.md`.

If you could not run the Unity tests, say so rather than describing the work as verified.

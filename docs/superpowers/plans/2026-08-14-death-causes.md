# A-1 Death Causes Implementation Plan

> **For agentic workers:** Implement this plan one task at a time, in order. Steps use checkbox (`- [ ]`) syntax. Read `AGENTS.md` in the repository root before starting.

**Goal:** Record *which* need killed each creature, so population collapse can be diagnosed as starvation, dehydration, predation, or old age instead of a single undifferentiated total.

**Architecture:** Additive and hash-safe. `DeathCause` is not covered by `SimulationWorld.ComputeStateHash()`, so no simulation behaviour changes and no recorded experiment results shift. `DeathCause.Starvation` and `DeathCause.Dehydration` already exist in the enum and have never been emitted.

**Tech Stack:** C# 9, Unity 6 (6000.2.14f1), Unity Test Framework, NUnit.

**Related:** defect A-1 in `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`. Sibling plan: `2026-08-14-decision-diagnostics.md` (independent — either may be done first).

## Global Constraints

Copied from `AGENTS.md`. These apply to **every** task in this plan.

- No `UnityEngine` code in `Assets/Scripts/Simulation/`.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, or `async` anywhere in `Assets/Scripts/Simulation/`.
- No allocation (`new` on arrays, lists, or classes) inside anything called from `SimulationWorld.Step`.
- No LINQ in `Assets/Scripts/Simulation/`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. `movementDistance`, not `moveDist`.
- Edit only the files each task lists. Do not refactor anything else.

**Stop and report instead of proceeding if:** an existing test fails, a file you need to edit is not listed in your task, or the code does not match what this plan shows.

**Expected outcome for the whole plan:** every existing test continues to pass, unchanged. An existing test failing means you broke something.

## File Structure

| File | Responsibility | Tasks |
|---|---|---|
| `Assets/Scripts/Simulation/Biology/NeedsSystem.cs` | Pure death-cause classification | 1 |
| `Assets/Tests/EditMode/DeathCauseTests.cs` | New. All tests for this plan | 1, 2, 3 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Use classification; count deaths by cause | 2, 3 |
| `Assets/Scripts/Presentation/Prototype1Presenter.cs` | Display the breakdown | 4 |

---

## Task 1: Classify metabolic death causes

Adds a pure function deciding which exhausted need is responsible. Nothing calls it yet.

**Files:**
- Modify: `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`
- Create: `Assets/Tests/EditMode/DeathCauseTests.cs`

**Interfaces:**
- Consumes: `CreatureNeeds` (already in `NeedsSystem.cs`), `DeathCause` (in `LifeSimulation.Simulation.Core`)
- Produces: `NeedsSystem.ClassifyMetabolicDeath(in CreatureNeeds needs)` returning `DeathCause`

- [ ] **Step 1: Create the test file with the first failing test**

Create `Assets/Tests/EditMode/DeathCauseTests.cs` with exactly this content:

```csharp
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class DeathCauseTests
    {
        [Test]
        public void EmptyEnergyIsReportedAsStarvation()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;

            Assert.That(NeedsSystem.ClassifyMetabolicDeath(needs), Is.EqualTo(DeathCause.Starvation));
        }
    }
}
```

- [ ] **Step 2: Run the test and verify it fails to compile**

Open Unity, then run EditMode tests from `Window > General > Test Runner`.

Expected: compile error, `'NeedsSystem' does not contain a definition for 'ClassifyMetabolicDeath'`.

Any other error: stop and report.

- [ ] **Step 3: Add the classification method**

In `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`, add this using directive below the existing `using System;`:

```csharp
using LifeSimulation.Simulation.Core;
```

Add this method inside `public static class NeedsSystem`, immediately after `Tick` and before `ApplyTemperatureStress`:

```csharp
        /// <summary>
        /// Reports which exhausted need is responsible for a creature's health reaching zero.
        /// Dehydration outranks starvation because it drains health faster, so when both needs
        /// are empty the faster cause is the one reported.
        /// </summary>
        public static DeathCause ClassifyMetabolicDeath(in CreatureNeeds needs)
        {
            if (needs.Hydration <= 0f)
            {
                return DeathCause.Dehydration;
            }

            if (needs.Energy <= 0f)
            {
                return DeathCause.Starvation;
            }

            return DeathCause.Health;
        }
```

- [ ] **Step 4: Run the test and verify it passes**

Expected: `EmptyEnergyIsReportedAsStarvation` PASSES. Every other test still passes.

- [ ] **Step 5: Add the remaining classification tests**

Add these three tests inside `DeathCauseTests`, after `EmptyEnergyIsReportedAsStarvation`:

```csharp
        [Test]
        public void EmptyHydrationIsReportedAsDehydration()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Hydration = 0f;

            Assert.That(NeedsSystem.ClassifyMetabolicDeath(needs), Is.EqualTo(DeathCause.Dehydration));
        }

        [Test]
        public void BothNeedsEmptyReportsTheFasterCause()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            needs.Hydration = 0f;

            Assert.That(NeedsSystem.ClassifyMetabolicDeath(needs), Is.EqualTo(DeathCause.Dehydration));
        }

        [Test]
        public void IntactNeedsReportGenericHealthLoss()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);

            Assert.That(NeedsSystem.ClassifyMetabolicDeath(needs), Is.EqualTo(DeathCause.Health));
        }
```

- [ ] **Step 6: Run the tests and verify all four pass**

Expected: all four `DeathCauseTests` PASS. Every other test still passes.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/NeedsSystem.cs Assets/Tests/EditMode/DeathCauseTests.cs
git commit -m "feat: classify metabolic death as starvation or dehydration"
```

---

## Task 2: Report the classified cause when a creature dies

`SimulationWorld` currently passes `DeathCause.Health` for every metabolic death.

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (one line, near line 394)
- Modify: `Assets/Tests/EditMode/DeathCauseTests.cs`

**Interfaces:**
- Consumes: `NeedsSystem.ClassifyMetabolicDeath(in CreatureNeeds)` from Task 1
- Produces: death events carrying `Starvation` or `Dehydration` instead of `Health`

- [ ] **Step 1: Write the failing integration test**

Add this using directive to the top of `Assets/Tests/EditMode/DeathCauseTests.cs`:

```csharp
using LifeSimulation.Simulation.Behavior;
```

Add this test inside `DeathCauseTests`:

```csharp
        [Test]
        public void AThirstyCreatureDiesOfDehydrationRatherThanGenericHealthLoss()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Hydration = 0f;
            needs.Health = 0.01f;

            for (int step = 0; step < 40 && world.CreatureCount > 0; step++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.CreatureCount, Is.EqualTo(0));
            bool foundDehydrationDeath = false;
            for (int index = 0; index < world.Events.Count; index++)
            {
                SimulationEvent simulationEvent = world.Events.GetAt(index);
                if (simulationEvent.Kind == SimulationEventKind.Death
                    && simulationEvent.DeathCause == DeathCause.Dehydration)
                {
                    foundDehydrationDeath = true;
                }
            }

            Assert.That(foundDehydrationDeath, Is.True, "Expected a death event with cause Dehydration.");
        }
```

- [ ] **Step 2: Run the test and verify it fails**

Expected: FAIL with `Expected a death event with cause Dehydration.` — the death is currently reported as `Health`.

Failing for any other reason: stop and report.

- [ ] **Step 3: Use the classification**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, inside `TickNeeds`, find this line (near line 394):

```csharp
                    RequestDeath(Creatures.GetIdAt(index), DeathCause.Health);
```

Replace that single line with:

```csharp
                    RequestDeath(Creatures.GetIdAt(index), NeedsSystem.ClassifyMetabolicDeath(needs));
```

Change nothing else in this file.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: the new test PASSES. All existing tests still pass, including every determinism and state-hash test.

**If any existing test now fails, stop immediately and report it.** This change is behaviour-neutral; a failure means something is wrong.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DeathCauseTests.cs
git commit -m "feat: report starvation and dehydration as distinct death causes"
```

---

## Task 3: Count deaths by cause

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/DeathCauseTests.cs`

**Interfaces:**
- Produces: `SimulationWorld.GetDeathCount(DeathCause cause)` returning `int`

- [ ] **Step 1: Write the failing test**

Add this test inside `DeathCauseTests`:

```csharp
        [Test]
        public void DeathsAreCountedByCause()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Hydration = 0f;
            needs.Health = 0.01f;

            for (int step = 0; step < 40 && world.CreatureCount > 0; step++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.GetDeathCount(DeathCause.Dehydration), Is.EqualTo(1));
            Assert.That(world.GetDeathCount(DeathCause.Starvation), Is.EqualTo(0));
            Assert.That(world.GetDeathCount(DeathCause.Predation), Is.EqualTo(0));
        }
```

- [ ] **Step 2: Run the test and verify it fails to compile**

Expected: compile error, `'SimulationWorld' does not contain a definition for 'GetDeathCount'`.

- [ ] **Step 3: Add the counter field**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, find this field near the top of the class (near line 30):

```csharp
        private int _predationDeathCount;
```

Add this line immediately after it:

```csharp
        private readonly int[] _deathCountsByCause = new int[7];
```

Seven entries because `DeathCause` has seven members: `None`, `Debug`, `Starvation`, `Dehydration`, `Age`, `Health`, `Predation`.

- [ ] **Step 4: Increment the counter when a death is applied**

In the same file, inside `Step`, find this block (near line 249):

```csharp
                    _deathCount++;
                    if (_pendingDeathCauses[index] == DeathCause.Predation)
                    {
                        _predationDeathCount++;
                    }
```

Replace it with:

```csharp
                    _deathCount++;
                    _deathCountsByCause[(int)_pendingDeathCauses[index]]++;
                    if (_pendingDeathCauses[index] == DeathCause.Predation)
                    {
                        _predationDeathCount++;
                    }
```

- [ ] **Step 5: Add the accessor**

In the same file, add this method immediately after the existing `GetCreatureIdAt` method (near line 72):

```csharp
        public int GetDeathCount(DeathCause cause)
        {
            return _deathCountsByCause[(int)cause];
        }
```

- [ ] **Step 6: Run the tests and verify they pass**

Expected: `DeathsAreCountedByCause` PASSES. All existing tests still pass.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DeathCauseTests.cs
git commit -m "feat: count deaths by cause"
```

---

## Task 4: Display the death-cause breakdown

Unity presentation code. Not covered by EditMode tests; verify by entering Play mode.

**Files:**
- Modify: `Assets/Scripts/Presentation/Prototype1Presenter.cs`

**Interfaces:**
- Consumes: `SimulationWorld.GetDeathCount(DeathCause)` from Task 3

- [ ] **Step 1: Add the panel method**

In `Assets/Scripts/Presentation/Prototype1Presenter.cs`, add this method immediately after the existing `DrawSelectedCreatureInspector` method:

```csharp
        private void DrawDeathCauseBreakdown()
        {
            GUI.Box(new Rect(464f, 232f, 260f, 140f), "Deaths By Cause");
            GUI.Label(new Rect(476f, 258f, 240f, 22f), $"Starvation: {_world.GetDeathCount(DeathCause.Starvation)}");
            GUI.Label(new Rect(476f, 280f, 240f, 22f), $"Dehydration: {_world.GetDeathCount(DeathCause.Dehydration)}");
            GUI.Label(new Rect(476f, 302f, 240f, 22f), $"Predation: {_world.GetDeathCount(DeathCause.Predation)}");
            GUI.Label(new Rect(476f, 324f, 240f, 22f), $"Old age: {_world.GetDeathCount(DeathCause.Age)}");
            GUI.Label(new Rect(476f, 346f, 240f, 22f), $"Other: {_world.GetDeathCount(DeathCause.Health)}");
        }
```

- [ ] **Step 2: Call the new method**

Find the line that calls `DrawSelectedCreatureInspector();` and add a call immediately after it:

```csharp
            DrawSelectedCreatureInspector();
            DrawDeathCauseBreakdown();
```

- [ ] **Step 3: Verify in Play mode**

Enter Play mode. Confirm the "Deaths By Cause" panel appears and its numbers increase over time. If it overlaps another panel, adjust only the `Rect` coordinates.

- [ ] **Step 4: Run the EditMode tests one final time**

Expected: every test passes.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation/Prototype1Presenter.cs
git commit -m "feat: display death-cause breakdown in the overlay"
```

---

## Completion checklist

- [ ] All four tasks committed
- [ ] Every existing test still passes, unmodified
- [ ] Only the four files in the File Structure table were edited
- [ ] `DeterministicRandom.cs` and `TemperatureField.cs` were not modified
- [ ] No allocation added to per-tick code
- [ ] Defect A-1 is resolved

If you could not run the Unity tests, say so explicitly rather than describing the work as verified.

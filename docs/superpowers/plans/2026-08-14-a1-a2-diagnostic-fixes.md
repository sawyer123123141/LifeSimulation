# A-1 and A-2 Diagnostic Fixes Implementation Plan

> **For agentic workers:** Implement this plan one task at a time, in order. Steps use checkbox (`- [ ]`) syntax for tracking. Read `AGENTS.md` in the repository root before starting.

**Goal:** Make deaths and decisions explainable — record *which* need killed a creature, and record the scores behind flee, hunt, carcass, and thermal decisions.

**Architecture:** Both fixes are additive and hash-safe. Death causes, decision diagnostics, and statistics are not covered by `SimulationWorld.ComputeStateHash()`, so no simulation behavior changes and no recorded experiment results shift. New public methods are added as overloads so every existing call site keeps compiling unchanged.

**Tech Stack:** C# 9, Unity 6 (6000.2.14f1), Unity Test Framework, NUnit.

## Global Constraints

Copied from `AGENTS.md`. These apply to **every** task in this plan.

- No `UnityEngine` code in `Assets/Scripts/Simulation/`.
- No `System.Random`, `UnityEngine.Random`, `DateTime`, `Guid.NewGuid()`, threads, or `async` anywhere in `Assets/Scripts/Simulation/`.
- No allocation (`new` on arrays, lists, or classes) inside anything called from `SimulationWorld.Step`.
- No LINQ in `Assets/Scripts/Simulation/`.
- Do not modify `DeterministicRandom.cs` or `TemperatureField.cs`.
- Do not change existing values in the `RandomDomain` enum.
- Do not modify, weaken, delete, or `[Ignore]` any existing test.
- Full words in names. `movementDistance`, not `moveDist`.
- Edit only the files each task lists. Do not refactor anything else.

**Stop and report instead of proceeding if:** an existing test fails, a file you need to edit is not listed in your task, or the code does not match what this plan shows.

**Expected test outcome for the entire plan:** every existing test continues to pass, unchanged. If an existing test fails, you have broken something — stop.

---

## File Structure

| File | Responsibility | Tasks |
|---|---|---|
| `Assets/Scripts/Simulation/Biology/NeedsSystem.cs` | Add pure death-cause classification | 1 |
| `Assets/Tests/EditMode/DeathCauseTests.cs` | New. Tests for classification and death reporting | 1, 2, 3 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Use classification; count deaths by cause; thread diagnostics | 2, 3, 8 |
| `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` | Extend `DecisionDiagnostics` | 4 |
| `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs` | New. Tests for diagnostics fields and scores | 4, 5, 6, 7 |
| `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` | Report flee, hunt, carcass scores | 5, 6 |
| `Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs` | Report thermal score | 7 |
| `Assets/Scripts/Presentation/Prototype1Presenter.cs` | Display death causes and decision scores | 9 |

---

# Part 1 — A-1: Record which need caused each death

## Task 1: Classify metabolic death causes

`DeathCause.Starvation` and `DeathCause.Dehydration` are declared in `SimulationTypes.cs` but never emitted. This task adds a pure function that decides which one applies. Nothing calls it yet.

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

Open Unity, then run the EditMode tests from `Window > General > Test Runner`.

Expected: compile error, `'NeedsSystem' does not contain a definition for 'ClassifyMetabolicDeath'`.

If you see a different error, stop and report it.

- [ ] **Step 3: Add the classification method**

In `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`, add this using directive below the existing `using System;` line:

```csharp
using LifeSimulation.Simulation.Core;
```

Then add this method inside `public static class NeedsSystem`, immediately after the `Tick` method and before `ApplyTemperatureStress`:

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

Run the EditMode tests again.

Expected: `EmptyEnergyIsReportedAsStarvation` PASSES. Every other test still passes.

- [ ] **Step 5: Add the remaining classification tests**

Add these three tests inside the `DeathCauseTests` class, after `EmptyEnergyIsReportedAsStarvation`:

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

Expected: all four `DeathCauseTests` tests PASS. Every other test still passes.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/NeedsSystem.cs Assets/Tests/EditMode/DeathCauseTests.cs
git commit -m "feat: classify metabolic death as starvation or dehydration"
```

---

## Task 2: Report the classified cause when a creature dies

`SimulationWorld` currently passes `DeathCause.Health` for every metabolic death. This task makes it use the classification from Task 1.

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (one line, at line 394)
- Modify: `Assets/Tests/EditMode/DeathCauseTests.cs`

**Interfaces:**
- Consumes: `NeedsSystem.ClassifyMetabolicDeath(in CreatureNeeds)` from Task 1
- Produces: death events carrying `Starvation` or `Dehydration` instead of `Health`

- [ ] **Step 1: Write the failing integration test**

Add this test inside `DeathCauseTests`, and add these using directives to the top of the file if they are not already present:

```csharp
using LifeSimulation.Simulation.Behavior;
```

The test:

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

Expected: FAIL with `Expected a death event with cause Dehydration.` — because the death is currently reported as `Health`.

If the test fails for any other reason, stop and report it.

- [ ] **Step 3: Use the classification**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, find this line inside `TickNeeds` (near line 394):

```csharp
                    RequestDeath(Creatures.GetIdAt(index), DeathCause.Health);
```

Replace that single line with:

```csharp
                    RequestDeath(Creatures.GetIdAt(index), NeedsSystem.ClassifyMetabolicDeath(needs));
```

Change nothing else in this file.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: `AThirstyCreatureDiesOfDehydrationRatherThanGenericHealthLoss` PASSES. All existing tests still pass, including every determinism and state-hash test.

**If any existing test now fails, stop immediately and report it.** This change is supposed to be behaviour-neutral; a failure means something is wrong.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DeathCauseTests.cs
git commit -m "feat: report starvation and dehydration as distinct death causes"
```

---

## Task 3: Count deaths by cause

Recording the cause is only useful if you can read the totals. This task adds counters and an accessor.

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

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, find this field declaration near the top of the class (near line 30):

```csharp
        private int _predationDeathCount;
```

Add this line immediately after it:

```csharp
        private readonly int[] _deathCountsByCause = new int[7];
```

The array has seven entries because `DeathCause` has seven members: `None`, `Debug`, `Starvation`, `Dehydration`, `Age`, `Health`, `Predation`.

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

# Part 2 — A-2: Explain flee, hunt, carcass, and thermal decisions

`DecisionDiagnostics` currently carries only food and water scores, so the inspector can explain a foraging choice and nothing else.

## Task 4: Extend the diagnostics record

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`
- Create: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Produces: `DecisionDiagnostics.FleeScore`, `.HuntScore`, `.CarcassScore`, `.ThermalScore`, `.WinningAction`, and the methods `WithPredationScores(float, float)`, `WithCarcassScore(float)`, `WithThermalScore(float)`, `WithWinningAction(CreatureAction)`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs` with exactly this content:

```csharp
using LifeSimulation.Simulation.Behavior;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class DecisionDiagnosticsTests
    {
        [Test]
        public void NewDiagnosticsFieldsDefaultToZeroAndPreserveExistingScores()
        {
            var diagnostics = new DecisionDiagnostics(0.4f, 0.2f, foodVisible: true, waterVisible: false);

            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.4f));
            Assert.That(diagnostics.WaterScore, Is.EqualTo(0.2f));
            Assert.That(diagnostics.FleeScore, Is.EqualTo(0f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0f));
            Assert.That(diagnostics.CarcassScore, Is.EqualTo(0f));
            Assert.That(diagnostics.ThermalScore, Is.EqualTo(0f));
        }

        [Test]
        public void PredationScoresAreRecordedWithoutDisturbingForagingScores()
        {
            var diagnostics = new DecisionDiagnostics(0.4f, 0.2f, foodVisible: true, waterVisible: false)
                .WithPredationScores(fleeScore: 0.7f, huntScore: 0.1f);

            Assert.That(diagnostics.FleeScore, Is.EqualTo(0.7f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0.1f));
            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.4f));
            Assert.That(diagnostics.FoodVisible, Is.True);
        }

        [Test]
        public void WinningActionIsRecorded()
        {
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false)
                .WithWinningAction(CreatureAction.Flee);

            Assert.That(diagnostics.WinningAction, Is.EqualTo(CreatureAction.Flee));
        }
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail to compile**

Expected: compile error, `'DecisionDiagnostics' does not contain a definition for 'FleeScore'`.

- [ ] **Step 3: Replace the diagnostics struct**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, find the entire existing `DecisionDiagnostics` struct:

```csharp
    public readonly struct DecisionDiagnostics
    {
        public DecisionDiagnostics(float foodScore, float waterScore, bool foodVisible, bool waterVisible)
        {
            FoodScore = foodScore;
            WaterScore = waterScore;
            FoodVisible = foodVisible;
            WaterVisible = waterVisible;
        }

        public float FoodScore { get; }
        public float WaterScore { get; }
        public bool FoodVisible { get; }
        public bool WaterVisible { get; }
    }
```

Replace it entirely with:

```csharp
    public readonly struct DecisionDiagnostics
    {
        public DecisionDiagnostics(float foodScore, float waterScore, bool foodVisible, bool waterVisible)
            : this(foodScore, waterScore, foodVisible, waterVisible, 0f, 0f, 0f, 0f, CreatureAction.Wander)
        {
        }

        private DecisionDiagnostics(
            float foodScore,
            float waterScore,
            bool foodVisible,
            bool waterVisible,
            float fleeScore,
            float huntScore,
            float carcassScore,
            float thermalScore,
            CreatureAction winningAction)
        {
            FoodScore = foodScore;
            WaterScore = waterScore;
            FoodVisible = foodVisible;
            WaterVisible = waterVisible;
            FleeScore = fleeScore;
            HuntScore = huntScore;
            CarcassScore = carcassScore;
            ThermalScore = thermalScore;
            WinningAction = winningAction;
        }

        public float FoodScore { get; }
        public float WaterScore { get; }
        public bool FoodVisible { get; }
        public bool WaterVisible { get; }
        public float FleeScore { get; }
        public float HuntScore { get; }
        public float CarcassScore { get; }
        public float ThermalScore { get; }
        public CreatureAction WinningAction { get; }

        public DecisionDiagnostics WithPredationScores(float fleeScore, float huntScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                fleeScore, huntScore, CarcassScore, ThermalScore, WinningAction);
        }

        public DecisionDiagnostics WithCarcassScore(float carcassScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, carcassScore, ThermalScore, WinningAction);
        }

        public DecisionDiagnostics WithThermalScore(float thermalScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, CarcassScore, thermalScore, WinningAction);
        }

        public DecisionDiagnostics WithWinningAction(CreatureAction winningAction)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, CarcassScore, ThermalScore, winningAction);
        }
    }
```

The four-argument constructor is kept so every existing call site compiles unchanged. `DecisionDiagnostics` remains a `readonly struct`, so the `With...` methods return copies on the stack and allocate nothing.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: all three `DecisionDiagnosticsTests` tests PASS. All existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: extend decision diagnostics with predation, carcass, and thermal scores"
```

---

## Task 5: Report flee and hunt scores from PredationSystem

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithPredationScores(float, float)` from Task 4
- Produces: overload `PredationSystem.Decide(CreatureNeeds, Phenotype, Phenotype, CreatureObservation, CreatureDecision, ref DecisionDiagnostics)`

- [ ] **Step 1: Write the failing test**

Add these using directives to the top of `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`:

```csharp
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
```

Add this test inside `DecisionDiagnosticsTests`:

```csharp
        [Test]
        public void DecidingAgainstAThreatRecordsBothPredationScores()
        {
            Phenotype prey = Phenotype.FromGenome(new Genome(0.2f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, fear: 1f));
            Phenotype predator = Phenotype.FromGenome(new Genome(0.9f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(prey);
            var observation = new CreatureObservation(new CreatureId(2), 1, 1f);
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            PredationSystem.Decide(
                needs,
                prey,
                predator,
                observation,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.FleeScore, Is.GreaterThan(0f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0f));
        }
```

- [ ] **Step 2: Run the test and verify it fails to compile**

Expected: compile error about no `Decide` overload taking six arguments.

- [ ] **Step 3: Add the overload**

In `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`, find the existing `Decide` method. Replace its opening — from the `public static CreatureDecision Decide(` line down to and including the line `            float hunt = HuntCapability(self, other) * hunger * distanceAvailability;` — with the following. Everything after that line stays exactly as it is.

```csharp
        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return Decide(needs, self, other, otherObservation, survivalDecision, ref ignoredDiagnostics);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision,
            ref DecisionDiagnostics diagnostics)
        {
            if (!otherObservation.IsValid)
            {
                return survivalDecision;
            }

            float distanceAvailability = 1f / (1f + otherObservation.Distance);
            float hunger = 1f - (needs.Energy / self.EnergyCapacity);
            float threat = Threat(other, self) * self.FearResponse * distanceAvailability;
            float hunt = HuntCapability(self, other) * hunger * distanceAvailability;
            diagnostics = diagnostics.WithPredationScores(threat, hunt);
```

- [ ] **Step 4: Run the tests and verify they pass**

Expected: `DecidingAgainstAThreatRecordsBothPredationScores` PASSES. The two existing `PredationSystem.Decide` tests in `SpatialBehaviorTests.cs` still pass unchanged, because the five-argument overload still exists.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report flee and hunt scores from predation decisions"
```

---

## Task 6: Report the carcass score

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithCarcassScore(float)` from Task 4
- Produces: overload `PredationSystem.PreferCarcassWhenUseful(CreatureNeeds, Phenotype, ResourceObservation, CreatureDecision, ref DecisionDiagnostics)`

- [ ] **Step 1: Write the failing test**

Add this using directive to the top of `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs` if not already present:

```csharp
using LifeSimulation.Simulation.Resources;
```

Add this test inside `DecisionDiagnosticsTests`:

```csharp
        [Test]
        public void ConsideringACarcassRecordsItsScore()
        {
            Phenotype scavenger = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(scavenger);
            needs.Energy = 0f;
            var carcass = new ResourceObservation(new ResourceId(3), 0, 1f);
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            PredationSystem.PreferCarcassWhenUseful(
                needs,
                scavenger,
                carcass,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.CarcassScore, Is.GreaterThan(0f));
        }
```

- [ ] **Step 2: Run the test and verify it fails to compile**

Expected: compile error about no `PreferCarcassWhenUseful` overload taking five arguments.

- [ ] **Step 3: Add the overload**

In `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`, find the entire existing `PreferCarcassWhenUseful` method and replace it with:

```csharp
        public static CreatureDecision PreferCarcassWhenUseful(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation carcass,
            CreatureDecision currentDecision)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return PreferCarcassWhenUseful(needs, phenotype, carcass, currentDecision, ref ignoredDiagnostics);
        }

        public static CreatureDecision PreferCarcassWhenUseful(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation carcass,
            CreatureDecision currentDecision,
            ref DecisionDiagnostics diagnostics)
        {
            if (!carcass.IsValid)
            {
                return currentDecision;
            }

            float hunger = 1f - (needs.Energy / phenotype.EnergyCapacity);
            float score = hunger * phenotype.MeatYieldMultiplier / (1f + carcass.Distance);
            diagnostics = diagnostics.WithCarcassScore(score);
            return score > currentDecision.Score && score >= 0.10f
                ? new CreatureDecision(CreatureAction.SeekCarcass, carcass.ResourceIndex, score)
                : currentDecision;
        }
```

- [ ] **Step 4: Run the tests and verify they pass**

Expected: `ConsideringACarcassRecordsItsScore` PASSES. All existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report carcass score from scavenging decisions"
```

---

## Task 7: Report the thermal comfort score

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithThermalScore(float)` from Task 4
- Produces: overload `ThermoregulationSystem.PreferThermalComfort(Phenotype, SimVector2, long, CreatureDecision, ref DecisionDiagnostics)`

- [ ] **Step 1: Write the failing test**

Add this test inside `DecisionDiagnosticsTests`:

```csharp
        [Test]
        public void ConsideringThermalComfortRecordsItsScore()
        {
            Phenotype intolerant = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, temperatureTolerance: 0f));
            var diagnostics = new DecisionDiagnostics(0f, 0f, foodVisible: false, waterVisible: false);

            ThermoregulationSystem.PreferThermalComfort(
                intolerant,
                new SimVector2(20f, 20f),
                0L,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                ref diagnostics);

            Assert.That(diagnostics.ThermalScore, Is.GreaterThanOrEqualTo(0f));
        }
```

- [ ] **Step 2: Run the test and verify it fails to compile**

Expected: compile error about no `PreferThermalComfort` overload taking five arguments.

- [ ] **Step 3: Add the overload**

In `Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs`, find the entire existing `PreferThermalComfort` method and replace it with:

```csharp
        public static CreatureDecision PreferThermalComfort(
            Phenotype phenotype,
            SimVector2 position,
            long tick,
            CreatureDecision currentDecision)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return PreferThermalComfort(phenotype, position, tick, currentDecision, ref ignoredDiagnostics);
        }

        public static CreatureDecision PreferThermalComfort(
            Phenotype phenotype,
            SimVector2 position,
            long tick,
            CreatureDecision currentDecision,
            ref DecisionDiagnostics diagnostics)
        {
            float discomfort = Math.Max(0f, Math.Abs(TemperatureField.Sample(position, tick) - ComfortableTemperature) - phenotype.TemperatureTolerance);
            float score = discomfort / 8f;
            diagnostics = diagnostics.WithThermalScore(score);
            return score > currentDecision.Score && score >= 0.15f
                ? new CreatureDecision(CreatureAction.SeekThermalComfort, -1, score)
                : currentDecision;
        }
```

You are calling `TemperatureField.Sample`. You are **not** modifying `TemperatureField.cs`. Do not open it.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: `ConsideringThermalComfortRecordsItsScore` PASSES. The existing `PreferThermalComfort` test in `SpatialBehaviorTests.cs` still passes unchanged.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report thermal comfort score from thermoregulation decisions"
```

---

## Task 8: Thread diagnostics through the decision tick

The overloads exist but `SimulationWorld` still calls the old ones, so the new scores are never stored.

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: the `ref DecisionDiagnostics` overloads from Tasks 5, 6, and 7
- Produces: stored diagnostics containing predation, carcass, thermal scores, and the winning action

- [ ] **Step 1: Write the failing test**

Add this test inside `DecisionDiagnosticsTests`:

```csharp
        [Test]
        public void StoredDiagnosticsRecordTheWinningAction()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);

            for (int step = 0; step < 20; step++)
            {
                world.Step(config.FixedDeltaTime);
            }

            DecisionDiagnostics diagnostics = world.GetCreatureDecisionDiagnosticsAt(0);
            CreatureDecision decision = world.GetCreatureDecisionAt(0);

            Assert.That(diagnostics.WinningAction, Is.EqualTo(decision.Action));
        }
```

- [ ] **Step 2: Run the test and verify it fails**

Expected: FAIL, because `WinningAction` is always `CreatureAction.Wander` while the creature's decision may differ.

If the creature happens to be wandering, this test would pass by accident. If it passes at this step, stop and report it — the test needs a different scenario.

- [ ] **Step 3: Pass diagnostics into the predation calls**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, inside `TickDecisions`, find this call (near line 565):

```csharp
                        decision = PredationSystem.Decide(
                            Creatures.GetNeedsAt(index),
                            phenotype,
                            Creatures.GetPhenotypeAt(other.CreatureIndex),
                            other,
                            decision);
```

Replace it with:

```csharp
                        decision = PredationSystem.Decide(
                            Creatures.GetNeedsAt(index),
                            phenotype,
                            Creatures.GetPhenotypeAt(other.CreatureIndex),
                            other,
                            decision,
                            ref diagnostics);
```

- [ ] **Step 4: Pass diagnostics into the carcass call**

In the same method, find this call (near line 577):

```csharp
                    decision = PredationSystem.PreferCarcassWhenUseful(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        carcass,
                        decision);
```

Replace it with:

```csharp
                    decision = PredationSystem.PreferCarcassWhenUseful(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        carcass,
                        decision,
                        ref diagnostics);
```

- [ ] **Step 5: Pass diagnostics into the thermal call**

In the same method, find this line (near line 585):

```csharp
                    decision = ThermoregulationSystem.PreferThermalComfort(phenotype, movement.Position, tick, decision);
```

Replace it with:

```csharp
                    decision = ThermoregulationSystem.PreferThermalComfort(phenotype, movement.Position, tick, decision, ref diagnostics);
```

- [ ] **Step 6: Record the winning action when storing diagnostics**

In the same method, find this line at the end of the loop (near line 628):

```csharp
                Creatures.SetDecisionDiagnosticsAt(index, diagnostics);
```

Replace it with:

```csharp
                Creatures.SetDecisionDiagnosticsAt(index, diagnostics.WithWinningAction(decision.Action));
```

- [ ] **Step 7: Run the tests and verify they pass**

Expected: `StoredDiagnosticsRecordTheWinningAction` PASSES. All existing tests still pass, including every determinism and state-hash test.

**If any existing test fails, stop immediately and report it.** Diagnostics are not part of the state hash, so this change must not alter simulation results.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: store predation, carcass, thermal scores and winning action in diagnostics"
```

---

## Task 9: Display death causes and decision scores in the inspector

This task edits Unity presentation code. It cannot be covered by EditMode tests; verify it by entering Play mode and reading the on-screen overlay.

**Files:**
- Modify: `Assets/Scripts/Presentation/Prototype1Presenter.cs`

**Interfaces:**
- Consumes: `SimulationWorld.GetDeathCount(DeathCause)` from Task 3, and the diagnostics fields from Task 4

- [ ] **Step 1: Extend the decision explanation line**

In `Assets/Scripts/Presentation/Prototype1Presenter.cs`, inside `DrawSelectedCreatureInspector`, find this line (near line 363):

```csharp
            GUI.Label(new Rect(24f, 346f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
```

Replace that single line with these two lines:

```csharp
            GUI.Label(new Rect(24f, 346f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
            GUI.Label(new Rect(24f, 456f, 420f, 22f), $"Also: flee {diagnostics.FleeScore:0.00} | hunt {diagnostics.HuntScore:0.00} | carcass {diagnostics.CarcassScore:0.00} | warmth {diagnostics.ThermalScore:0.00}");
```

- [ ] **Step 2: Enlarge the inspector box so the new line is visible**

In the same method, find this line (near line 345):

```csharp
            GUI.Box(new Rect(12f, 232f, 440f, 184f), "Creature Inspector");
```

Replace it with:

```csharp
            GUI.Box(new Rect(12f, 232f, 440f, 250f), "Creature Inspector");
```

- [ ] **Step 3: Add a death-cause breakdown**

Add this method to the same class, immediately after `DrawSelectedCreatureInspector`:

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

- [ ] **Step 4: Call the new method**

Find the line that calls `DrawSelectedCreatureInspector();` and add a call immediately after it:

```csharp
            DrawSelectedCreatureInspector();
            DrawDeathCauseBreakdown();
```

- [ ] **Step 5: Verify in Play mode**

Enter Play mode. Confirm:

- The "Deaths By Cause" panel appears and its numbers increase over time.
- Clicking a creature shows the "Also:" line with four scores.
- Pressing `P` (predator mode) then selecting a creature shows non-zero flee or hunt scores.
- Pressing `T` (physiology mode) then selecting a creature shows a non-zero warmth score.

If any panel is missing or overlaps another, adjust only the `Rect` coordinates.

- [ ] **Step 6: Run the EditMode tests one final time**

Expected: every test passes.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Presentation/Prototype1Presenter.cs
git commit -m "feat: display death causes and full decision scores in the inspector"
```

---

## Completion checklist

- [ ] All nine tasks committed
- [ ] Every existing test still passes, unmodified
- [ ] No file outside the eight listed in the File Structure table was edited
- [ ] `DeterministicRandom.cs` and `TemperatureField.cs` were not modified
- [ ] No allocation was added to per-tick code
- [ ] `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md` items A-1 and A-2 are now resolved

If you could not run the Unity tests, say so explicitly rather than describing the work as verified.

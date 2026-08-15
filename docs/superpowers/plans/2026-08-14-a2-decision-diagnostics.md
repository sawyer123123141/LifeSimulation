# A-2 Decision Diagnostics Implementation Plan

> **For agentic workers:** Implement this plan one task at a time, in order. Steps use checkbox (`- [ ]`) syntax. Read `AGENTS.md` in the repository root before starting.

**Goal:** Make every decision explainable. `DecisionDiagnostics` currently carries only food and water scores, so the inspector can explain a foraging choice and nothing else — not fleeing, hunting, scavenging, or seeking warmth.

**Architecture:** Additive and hash-safe. Diagnostics are stored via `SetDecisionDiagnosticsAt` and never hashed or read by simulation logic, so no recorded results shift. Every new capability is an **overload** that delegates to the existing signature, so all current call sites and tests compile unchanged.

**Tech Stack:** C# 9, Unity 6 (6000.2.14f1), Unity Test Framework, NUnit.

**Related:** defect A-2 in `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`. Sibling plan: `2026-08-14-a1-death-causes.md` (independent — either may be done first).

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
| `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` | Extend the `DecisionDiagnostics` record | 1 |
| `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs` | New. All tests for this plan | 1, 2, 3, 4, 5 |
| `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` | Report flee, hunt, carcass scores | 2, 3 |
| `Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs` | Report thermal score | 4 |
| `Assets/Scripts/Simulation/Core/SimulationWorld.cs` | Thread diagnostics through the decision tick | 5 |
| `Assets/Scripts/Presentation/Prototype1Presenter.cs` | Display the scores | 6 |

---

## Task 1: Extend the diagnostics record

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`
- Create: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Produces: `DecisionDiagnostics.FleeScore`, `.HuntScore`, `.CarcassScore`, `.ThermalScore`, `.WinningAction`, and the methods `WithPredationScores(float, float)`, `WithCarcassScore(float)`, `WithThermalScore(float)`, `WithWinningAction(CreatureAction)`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs` with exactly this content:

```csharp
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
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

Open Unity, then run EditMode tests from `Window > General > Test Runner`.

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

The four-argument constructor is kept so every existing call site compiles unchanged. `DecisionDiagnostics` stays a `readonly struct`, so the `With...` methods return stack copies and allocate nothing.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: all three `DecisionDiagnosticsTests` PASS. All existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: extend decision diagnostics with predation, carcass, and thermal scores"
```

---

## Task 2: Report flee and hunt scores

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithPredationScores(float, float)` from Task 1
- Produces: overload `PredationSystem.Decide(CreatureNeeds, Phenotype, Phenotype, CreatureObservation, CreatureDecision, ref DecisionDiagnostics)`

- [ ] **Step 1: Write the failing test**

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

Expected: the new test PASSES. The two existing `PredationSystem.Decide` tests in `SpatialBehaviorTests.cs` still pass unchanged, because the five-argument overload still exists.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report flee and hunt scores from predation decisions"
```

---

## Task 3: Report the carcass score

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithCarcassScore(float)` from Task 1
- Produces: overload `PredationSystem.PreferCarcassWhenUseful(CreatureNeeds, Phenotype, ResourceObservation, CreatureDecision, ref DecisionDiagnostics)`

- [ ] **Step 1: Write the failing test**

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

Expected: the new test PASSES. All existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report carcass score from scavenging decisions"
```

---

## Task 4: Report the thermal comfort score

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `DecisionDiagnostics.WithThermalScore(float)` from Task 1
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

You are **calling** `TemperatureField.Sample`. You are **not** modifying `TemperatureField.cs`. Do not open it.

- [ ] **Step 4: Run the tests and verify they pass**

Expected: the new test PASSES. The existing `PreferThermalComfort` test in `SpatialBehaviorTests.cs` still passes unchanged.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/ThermoregulationSystem.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: report thermal comfort score from thermoregulation decisions"
```

---

## Task 5: Thread diagnostics through the decision tick

The overloads exist but `SimulationWorld` still calls the old ones, so the new scores are never stored.

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs`

**Interfaces:**
- Consumes: the `ref DecisionDiagnostics` overloads from Tasks 2, 3, and 4
- Produces: stored diagnostics containing all scores and the winning action

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

**If it passes at this step, stop and report it.** The creature happened to be wandering and the test proved nothing; the scenario needs changing.

- [ ] **Step 3: Pass diagnostics into the predation call**

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

Expected: the new test PASSES. All existing tests still pass, including every determinism and state-hash test.

**If any existing test fails, stop immediately and report it.** Diagnostics are not part of the state hash, so this must not alter simulation results.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionDiagnosticsTests.cs
git commit -m "feat: store predation, carcass, thermal scores and winning action in diagnostics"
```

---

## Task 6: Display the scores in the inspector

Unity presentation code. Not covered by EditMode tests; verify by entering Play mode.

**Files:**
- Modify: `Assets/Scripts/Presentation/Prototype1Presenter.cs`

**Interfaces:**
- Consumes: the diagnostics fields from Task 1

- [ ] **Step 1: Add the second explanation line**

In `Assets/Scripts/Presentation/Prototype1Presenter.cs`, inside `DrawSelectedCreatureInspector`, find this line (near line 363):

```csharp
            GUI.Label(new Rect(24f, 346f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
```

Add this line immediately after it:

```csharp
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

- [ ] **Step 3: Verify in Play mode**

Enter Play mode. Confirm:

- Clicking a creature shows the "Also:" line with four scores.
- Pressing `P` (predator mode), then selecting a creature, shows a non-zero flee or hunt score.
- Pressing `T` (physiology mode), then selecting a creature, shows a non-zero warmth score.

If panels overlap, adjust only the `Rect` coordinates.

- [ ] **Step 4: Run the EditMode tests one final time**

Expected: every test passes.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation/Prototype1Presenter.cs
git commit -m "feat: display full decision scores in the creature inspector"
```

---

## Completion checklist

- [ ] All six tasks committed
- [ ] Every existing test still passes, unmodified
- [ ] Only the six files in the File Structure table were edited
- [ ] `DeterministicRandom.cs` and `TemperatureField.cs` were not modified
- [ ] No allocation added to per-tick code
- [ ] Defect A-2 is resolved

If you could not run the Unity tests, say so explicitly rather than describing the work as verified.

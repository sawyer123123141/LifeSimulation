# Mate Selection (C-2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `ReproductionSystem` pairing respect a creature's own `SeekMate` decision instead of pure spatial proximity, behind a new flag.

**Architecture:** `ReproductionSystem` gains a `mateSelectionEnabled` constructor flag. `Step`'s per-candidate pairing search branches: when the flag is off, it calls the existing `FindNearestReadyMate` exactly as today; when on, it calls a new `FindSeekMateTarget`, which reads the candidate's current `CreatureDecision` (via `CreatureStore.GetDecisionAt`) and only pairs it with the specific creature it is actively seeking (`CreatureAction.SeekMate` + `TargetCreatureId`), provided that target is ready, unmatched, and in range. The target does not need to be seeking back.

**Tech Stack:** C#, Unity EditMode NUnit tests (`Assets/Tests/EditMode`), no new dependencies.

## Global Constraints

- Every new behavior stays behind a `SimulationConfig` bool flag defaulting to `false`; when off, code must be byte-identical to pre-change behavior, proven by a hash-regression test using the standard `PredationVariation`/`Legacy` scenario (expected hash `12050501592762519865UL`, per `docs/superpowers/specs/2026-08-16-mate-selection-design.md`'s Hash Safety section).
- New flags are added as the new *last* optional constructor parameter of `SimulationConfig`, with a matching `{ get; }` property placed immediately after the previous flag's property.
- `secondIndex` in `FindSeekMateTarget` must **not** be required to be seeking `firstIndex` back — one-sided pursuit is sufficient (explicit design decision, see spec's "Fix" section).
- No courtship state, no new gene, no genetic-distance mate quality, no top-K mate candidate perception. Conception stays instantaneous at the moment `Step` finds a valid target in range, exactly like today's proximity-pairing.
- `Legacy` decision policy is untouched — `ScoreMate` is only ever called under `IntentUtilityV1`, so `mateSelectionEnabled: true` under `Legacy` simply means no creature ever satisfies `FindSeekMateTarget` (every creature's decision is never `SeekMate`), and no births occur via that path. This is expected, not a bug to work around.

---

### Task 1: Mate-selection flag, `FindSeekMateTarget`, and wiring

**Files:**
- Modify: `Assets/Scripts/Simulation/Biology/ReproductionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs:71`
- Test: `Assets/Tests/EditMode/ReproductionSystemTests.cs` (new file)
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs` (append)

**Interfaces:**
- Consumes: `CreatureStore.GetDecisionAt(int) -> CreatureDecision` (existing, `Assets/Scripts/Simulation/Core/CreatureStore.cs:213`), `CreatureStore.TryGetIndex(CreatureId, out int) -> bool` (existing, `CreatureStore.cs:162`), `CreatureDecision.Action -> CreatureAction`, `CreatureDecision.TargetCreatureId -> CreatureId` (existing, `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs:178-199`), `ReproductionSystem.IsReady(int) -> bool` (existing private method, this file), `ReproductionSystem.MateDistance` (existing private const, `= 2f`).
- Produces: `ReproductionSystem` constructor gains `bool mateSelectionEnabled = false` as its 5th parameter. `ReproductionSystem.FindSeekMateTargetForTest(int firstIndex, int candidateCount) -> int` (new public forwarding method, test-only, mirrors this session's established pattern for exercising private logic from `Assets/Tests/EditMode`, which has no `InternalsVisibleTo`). `SimulationConfig.MateSelectionEnabled -> bool` (new property, default `false`).

- [ ] **Step 1: Write the failing unit tests for `FindSeekMateTarget`**

Create `Assets/Tests/EditMode/ReproductionSystemTests.cs`:

```csharp
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public class ReproductionSystemTests
    {
        private static (CreatureStore creatures, ReproductionSystem reproduction) CreateHarness()
        {
            var creatures = new CreatureStore(initialCapacity: 4);
            var arena = new ArenaBounds(-100f, 100f, -100f, 100f);
            var reproduction = new ReproductionSystem(creatures, arena, initialCapacity: 4, physiologyEnabled: false, mateSelectionEnabled: true);
            return (creatures, reproduction);
        }

        private static int AddReadyAdult(CreatureStore creatures, SimVector2 position)
        {
            CreatureId id = creatures.Add(Genome.Neutral, position);
            creatures.TryGetIndex(id, out int index);
            creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            return index;
        }

        [Test]
        public void FindSeekMateTargetReturnsTargetWhenSeekingAReadyInRangePartner()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(secondIndex));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenNotSeekingMate()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.Wander, -1, 0f));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenTargetOutOfRange()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(50f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenTargetNotReady()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            CreatureId secondId = creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            creatures.TryGetIndex(secondId, out int secondIndex);
            creatures.GetNeedsRefAt(secondIndex).Age = 0f;
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: secondId));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindSeekMateTargetIsSufficientEvenWhenTargetIsNotSeekingBack()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));
            creatures.SetDecisionAt(secondIndex, new CreatureDecision(CreatureAction.Wander, -1, 0f));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(secondIndex));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `cd tools/HeadlessTests && dotnet test --filter ReproductionSystemTests`
Expected: FAIL — compile error, `mateSelectionEnabled` parameter and `FindSeekMateTargetForTest` do not exist yet.

- [ ] **Step 3: Add the flag, `FindSeekMateTarget`, and the test forwarder to `ReproductionSystem`**

In `Assets/Scripts/Simulation/Biology/ReproductionSystem.cs`, add a field next to `_physiologyEnabled` (currently declared at line 17):

```csharp
        private readonly bool _physiologyEnabled;
        private readonly bool _mateSelectionEnabled;
```

Change the constructor (currently at lines 23-38):

```csharp
        public ReproductionSystem(CreatureStore creatures, ArenaBounds arena, int initialCapacity, bool physiologyEnabled, bool mateSelectionEnabled = false)
        {
            _creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            _physiologyEnabled = physiologyEnabled;
            _mateSelectionEnabled = mateSelectionEnabled;
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            int capacity = Math.Max(initialCapacity, 1);
            Grid = new UniformGrid(arena, cellSize: 5f, initialOccupantCapacity: capacity);
            _creaturePositions = new SimVector2[capacity];
            _candidates = new int[capacity];
            _matched = new bool[capacity];
            _creatureIndexComparer = new CreatureIndexComparer(_creatures);
        }
```

In `Step` (currently at lines 62-92), change the `secondIndex` assignment:

```csharp
                int firstIndex = _candidates[candidateIndex];
                if (_matched[firstIndex] || !IsReady(firstIndex))
                {
                    continue;
                }

                int secondIndex = _mateSelectionEnabled
                    ? FindSeekMateTarget(firstIndex, candidateCount)
                    : FindNearestReadyMate(firstIndex, candidateCount);
                if (secondIndex < 0)
                {
                    continue;
                }
```

Add `FindSeekMateTarget` immediately after `FindNearestReadyMate` (which currently ends at line 147, just before `CreateChild`):

```csharp
        private int FindSeekMateTarget(int firstIndex, int candidateCount)
        {
            CreatureDecision decision = _creatures.GetDecisionAt(firstIndex);
            if (decision.Action != CreatureAction.SeekMate
                || !_creatures.TryGetIndex(decision.TargetCreatureId, out int secondIndex))
            {
                return -1;
            }

            if (secondIndex < 0 || secondIndex >= candidateCount || secondIndex == firstIndex
                || _matched[secondIndex] || !IsReady(secondIndex))
            {
                return -1;
            }

            float distance = SimVector2.Distance(
                _creatures.GetMovementAt(firstIndex).Position,
                _creatures.GetMovementAt(secondIndex).Position);
            return distance <= MateDistance ? secondIndex : -1;
        }

        /// <summary>Test-only forwarder — Assets/Tests/EditMode has no InternalsVisibleTo, so private logic is exercised through a public passthrough, matching this session's established pattern.</summary>
        public int FindSeekMateTargetForTest(int firstIndex, int candidateCount)
        {
            return FindSeekMateTarget(firstIndex, candidateCount);
        }
```

`CreatureDecision` and `CreatureAction` are already in scope in this file via the existing `using LifeSimulation.Simulation.Behavior;` (line 3) — no new `using` needed.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter ReproductionSystemTests`
Expected: PASS — all 5 tests green.

- [ ] **Step 5: Add `SimulationConfig.MateSelectionEnabled`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, add the new parameter as the constructor's new last optional parameter (currently ends at line 115 with `bool learnedResourceQualityEnabled = false`):

```csharp
            bool learnedResourceQualityEnabled = false,
            bool mateSelectionEnabled = false)
```

Add the assignment as the new last line inside the constructor body (currently ends at line 144 with `LearnedResourceQualityEnabled = learnedResourceQualityEnabled;`):

```csharp
            LearnedResourceQualityEnabled = learnedResourceQualityEnabled;
            MateSelectionEnabled = mateSelectionEnabled;
```

Add the property immediately after `LearnedResourceQualityEnabled` (currently line 172):

```csharp
        public bool LearnedResourceQualityEnabled { get; }
        public bool MateSelectionEnabled { get; }
```

- [ ] **Step 6: Wire the flag into `SimulationWorld`'s `ReproductionSystem` construction**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs:71`, change:

```csharp
            _reproduction = new ReproductionSystem(Creatures, Arena, Config.InitialPopulation, Config.PhysiologyEnabled);
```

to:

```csharp
            _reproduction = new ReproductionSystem(Creatures, Arena, Config.InitialPopulation, Config.PhysiologyEnabled, Config.MateSelectionEnabled);
```

- [ ] **Step 7: Run the full test suite to confirm nothing else broke**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — all existing tests plus the 5 new `ReproductionSystemTests` green.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/ReproductionSystem.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/ReproductionSystemTests.cs
git commit -m "Add mate-selection flag: ReproductionSystem pairs by SeekMate decision when enabled"
```

- [ ] **Step 9: Write the failing integration test**

Append to `Assets/Tests/EditMode/CoreSimulationTests.cs` (inside the existing test class, near the other `IntentUtilityV1`-flag integration tests such as `JuvenileMovesTowardParentWhenWanderingAndFlagEnabled`):

```csharp
        [Test]
        public void MateSelectionEnabledPairsActiveSeekerButNotAnUninvolvedThirdReadyCreature()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 31,
                initialPopulation: 0,
                schedule: schedule,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                mateSelectionEnabled: true);
            var world = new SimulationWorld(config);
            CreatureId seeker = world.Spawn(Genome.Neutral);
            CreatureId partner = world.Spawn(Genome.Neutral);
            CreatureId bystander = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(seeker, out int seekerIndex);
            world.Creatures.TryGetIndex(partner, out int partnerIndex);
            world.Creatures.TryGetIndex(bystander, out int bystanderIndex);
            world.Creatures.GetNeedsRefAt(seekerIndex).Age = ReproductionSystem.AdultAgeSeconds;
            world.Creatures.GetNeedsRefAt(partnerIndex).Age = ReproductionSystem.AdultAgeSeconds;
            world.Creatures.GetNeedsRefAt(bystanderIndex).Age = ReproductionSystem.AdultAgeSeconds;
            world.SetCreaturePosition(seeker, new SimVector2(0f, 0f));
            world.SetCreaturePosition(partner, new SimVector2(1f, 0f));
            world.SetCreaturePosition(bystander, new SimVector2(-1f, 0f));
            world.Creatures.SetDecisionAt(seekerIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: partner));

            int countBefore = world.CreatureCount;
            world.Step(config.FixedDeltaTime);

            Assert.That(world.CreatureCount, Is.GreaterThan(countBefore));
            Assert.That(world.Creatures.TryGetIndex(bystander, out int bystanderIndexAfter), Is.True);
            CreatureDecision bystanderDecision = world.Creatures.GetDecisionAt(bystanderIndexAfter);
            Assert.That(bystanderDecision.Action, Is.Not.EqualTo(CreatureAction.Reproduce));
        }
```

Note: `TickDecisions` runs before `TickReproduction` within `world.Step`, so it will normally overwrite the manually-set `SeekMate` decision on `seekerIndex` before `ReproductionSystem.Step` reads it — unless the freshly-computed decision for that tick also resolves to `SeekMate` toward `partner` (likely, since `partner` is the nearest other creature and both are ready). If this test proves flaky in Step 10 because `TickDecisions` picks a different action that tick (e.g. `Wander` if urgency doesn't clear the `MinimumUrgencyToSeekResource` threshold), the assertion granularity in Step 3 (unit tests) is what actually enforces the causal link — this integration test is a secondary end-to-end sanity check. If it proves unreliable, the implementer should adjust it in Step 10 (e.g. by asserting on the decision `world.Creatures.GetDecisionAt(seekerIndex)` immediately after `TickDecisions` resolves, or accept whatever concrete outcome the real system produces and assert consistently with it) — report which adjustment was needed in the task report.

- [ ] **Step 10: Run the integration test, verify it passes (or adjust per the note above), then run the full suite**

Run: `cd tools/HeadlessTests && dotnet test --filter MateSelectionEnabledPairsActiveSeekerButNotAnUninvolvedThirdReadyCreature`
Expected: PASS (after any adjustment noted in Step 9).

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite green.

- [ ] **Step 11: Derive and write the hash-regression test**

Before writing this test, record the exact current commit:

```bash
git log --oneline -1 main
```

Use this exact commit hash as `PRE_TASK_COMMIT` below. Then, in a throwaway worktree:

```bash
git worktree add /c/ls-work/mate-selection-baseline PRE_TASK_COMMIT
```

In that worktree, add a temporary test file running:

```csharp
SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
var config = new SimulationConfig(
    worldSeed: 99, initialPopulation: 2, schedule: schedule,
    founderProfile: FounderProfile.PredationVariation);
var world = new SimulationWorld(config);
for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }
// print world.ComputeStateHash()
```

Run it, capture the printed hash, then remove the worktree:

```bash
cd /c/Users/sawye/OneDrive/Claude\ Code\ Roblox\ Game
git worktree remove /c/ls-work/mate-selection-baseline
git worktree prune
```

This scenario uses `DecisionPolicyVersion.Legacy` (the default) and never sets `MateSelectionEnabled`, so it is expected to reproduce the same `12050501592762519865UL` value every prior flag's hash-regression test has produced this session — but derive it fresh rather than assuming, per this program's established methodology.

Append to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        // Captured from the pre-Task-1 commit PRE_TASK_COMMIT (the commit this task's changes were
        // built on top of), by running this exact setup (with mateSelectionEnabled omitted, since
        // that constructor parameter did not exist yet) for 50 ticks and reading
        // world.ComputeStateHash(). Pinning this value confirms that adding
        // Config.MateSelectionEnabled and its call-site wiring in ReproductionSystem.cs is
        // byte-identical to prior behavior when the flag is left at its default (false).
        private const ulong ExpectedMateSelectionDisabledHash = 12050501592762519865UL;

        [Test]
        public void MateSelectionDisabledProducesIdenticalHashToPreExistingBehavior()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var config = new SimulationConfig(
                worldSeed: 99, initialPopulation: 2, schedule: schedule,
                founderProfile: FounderProfile.PredationVariation,
                mateSelectionEnabled: false);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedMateSelectionDisabledHash));
        }
```

Replace `PRE_TASK_COMMIT` in the comment with the actual commit hash recorded above. If the derived hash differs from `12050501592762519865UL`, use the actually-derived value instead — do not force the old constant.

- [ ] **Step 12: Run the hash-regression test**

Run: `cd tools/HeadlessTests && dotnet test --filter MateSelectionDisabledProducesIdenticalHashToPreExistingBehavior`
Expected: PASS.

- [ ] **Step 13: Write and run the informational birth-rate comparison**

Append to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void MateSelectionEnabledBirthRateComparedToDisabledUnderIntentUtilityV1()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var configDisabled = new SimulationConfig(
                worldSeed: 55, initialPopulation: 20, schedule: schedule,
                founderProfile: FounderProfile.PredationVariation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                maximumPopulation: 500,
                mateSelectionEnabled: false);
            var worldDisabled = new SimulationWorld(configDisabled);
            for (int index = 0; index < worldDisabled.CreatureCount; index++)
            {
                worldDisabled.Creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            }
            for (int i = 0; i < 200; i++) { worldDisabled.Step(configDisabled.FixedDeltaTime); }
            int birthsDisabled = worldDisabled.Statistics.BirthCount;

            var configEnabled = new SimulationConfig(
                worldSeed: 55, initialPopulation: 20, schedule: schedule,
                founderProfile: FounderProfile.PredationVariation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                maximumPopulation: 500,
                mateSelectionEnabled: true);
            var worldEnabled = new SimulationWorld(configEnabled);
            for (int index = 0; index < worldEnabled.CreatureCount; index++)
            {
                worldEnabled.Creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            }
            for (int i = 0; i < 200; i++) { worldEnabled.Step(configEnabled.FixedDeltaTime); }
            int birthsEnabled = worldEnabled.Statistics.BirthCount;

            TestContext.WriteLine($"Births disabled: {birthsDisabled}, births enabled: {birthsEnabled}");
            Assert.That(birthsEnabled, Is.GreaterThanOrEqualTo(0));
        }
```

This asserts nothing about the relative counts (informational only, per the spec) — it exists so the printed `TestContext.WriteLine` output captures the actual birth-count delta. `world.Step` returns `void`; the running total is read from `world.Statistics.BirthCount` (`SimulationWorld.cs:93`, `BirthCount` on `SimulationStatistics`, `SimulationTypes.cs:190` — confirmed by grepping the existing `Prototype1Presenter.cs:100` usage `stats.BirthCount`) after the loop, not accumulated per-tick.

- [ ] **Step 14: Run it and report the numbers**

Run: `cd tools/HeadlessTests && dotnet test --filter MateSelectionEnabledBirthRateComparedToDisabledUnderIntentUtilityV1 -- --logger "console;verbosity=detailed"`
Expected: PASS, with the births-disabled and births-enabled counts visible in the test output. Record both numbers in the task report.

- [ ] **Step 15: Run the full suite one final time and commit**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite green.

```bash
git add Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Add mate-selection integration, hash-regression, and birth-rate comparison tests"
```

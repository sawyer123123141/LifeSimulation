# Parental Following (C-5, part 2 of 3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A juvenile creature (`Age < ReproductionSystem.AdultAgeSeconds`) with `SimulationConfig.ParentalFollowingEnabled` set moves toward its nearest alive parent instead of wandering independently, but only when it has no more urgent decided action (`Wander` only) — flag defaults `false` and is byte-identical to today's behavior when off.

**Architecture:** One new `SimulationConfig` bool. One new private helper (`FindNearestAliveParent`) and one new branch inside the existing `SimulationWorld.GetMovementTarget` method, inserted immediately before the existing `CognitionEnabled` home-radius `Wander` branch so it takes priority. No new storage, no new `CreatureIntent`/`CreatureAction`, no `DecisionSystem` changes — `CreatureLineage.FirstParent`/`SecondParent` already exist and are already populated by `ReproductionSystem`/`CreatureStore.AddChild`.

**Tech Stack:** C#, Unity Test Framework (NUnit), EditMode tests.

## Global Constraints

- New flag `SimulationConfig.ParentalFollowingEnabled`, default `false`.
- Follow radius: `2f` world units (constant, spec: "2 world units (Recommended)").
- Juvenile threshold: `Creatures.GetNeedsAt(creatureIndex).Age < ReproductionSystem.AdultAgeSeconds` (reuse existing public const, do not duplicate `20f`).
- Priority order in `GetMovementTarget`'s `Wander` handling: parental following checked **before** `CognitionEnabled` home-radius homing (spec: "Parental following wins (Recommended)").
- `DeterministicRandom.Float01` selector index `3` for the follow-radius random offset (existing home-radius block already uses `2`, plain exploration uses `0` — must not alias).
- When the flag is `false`, `GetMovementTarget` must execute identically to before this task — proven by a hash-regression test using the established methodology (throwaway worktree at the pre-task commit, same fixed `PredationVariation` scenario, 50 `Step()` calls, `ComputeStateHash()`).
- This task does not touch `DecisionSystem`, does not add a `CreatureIntent`/`CreatureAction`, and does not change parent behavior (one-sided following only) — per the spec's explicit scope boundary.

---

### Task 1: Parental following flag, helper, and movement-target branch

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs:87-139` (constructor + properties)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs:528-627` (`GetMovementTarget`, add new private helper)
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Consumes: `ReproductionSystem.AdultAgeSeconds` (public const `float`, `20f`), `CreatureStore.GetLineageAt(int)` (returns `CreatureLineage` with `FirstParent`/`SecondParent` of type `CreatureId`), `CreatureStore.TryGetIndex(CreatureId, out int)` (returns `bool`), `CreatureStore.GetMovementAt(int)` (returns `MovementState` with `.Position` of type `SimVector2`), `CreatureStore.GetNeedsAt(int)` (returns `CreatureNeeds` with `.Age` of type `float`), `SimVector2.Distance(SimVector2, SimVector2)` (returns `float`), `DeterministicRandom.Float01(int, RandomDomain, long, long, int, int)` (returns `float` in `[0,1)`, matches the existing call shape at `SimulationWorld.cs:603-609`).
- Produces: `SimulationConfig.ParentalFollowingEnabled` (`bool` property, default `false`), `SimulationWorld.FindNearestAliveParent(CreatureLineage lineage, SimVector2 position)` (private, returns `SimVector2?`) — used only within this task, not consumed elsewhere.

- [ ] **Step 1: Add `ParentalFollowingEnabled` to `SimulationConfig`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, add a new constructor parameter immediately after `juvenileCapabilityEnabled` (the current last parameter, line 112):

```csharp
            bool juvenileCapabilityEnabled = false,
            bool parentalFollowingEnabled = false)
```

Add the assignment immediately after `JuvenileCapabilityEnabled = juvenileCapabilityEnabled;` (line 138):

```csharp
            JuvenileCapabilityEnabled = juvenileCapabilityEnabled;
            ParentalFollowingEnabled = parentalFollowingEnabled;
```

Add the property immediately after `public bool JuvenileCapabilityEnabled { get; }` (line 164):

```csharp
        public bool JuvenileCapabilityEnabled { get; }
        public bool ParentalFollowingEnabled { get; }
```

- [ ] **Step 2: Write the failing unit tests for `FindNearestAliveParent`**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`, inside the same test class as the other `CoreSimulationTests` tests (the class containing `JuvenileCreatureMovesLessDistanceThanAdultAcrossAStep`, near the end of the file, before the final closing braces):

```csharp
        [Test]
        public void FindNearestAliveParentReturnsCloserOfTwoAliveParents()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(worldSeed: 11, initialPopulation: 0, schedule: schedule);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.Creatures.TryGetIndex(secondParent, out int secondIndex);
            world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(0f, 0f);
            world.Creatures.GetMovementRefAt(secondIndex).Position = new SimVector2(10f, 0f);
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(1f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            CreatureLineage lineage = world.Creatures.GetLineageAt(childIndex);

            SimVector2? nearest = world.FindNearestAliveParentForTest(lineage, new SimVector2(1f, 0f));

            Assert.That(nearest.HasValue, Is.True);
            Assert.That(nearest.Value.X, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void FindNearestAliveParentReturnsLoneAliveParentWhenOtherIsDead()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(worldSeed: 12, initialPopulation: 0, schedule: schedule);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(5f, 5f);
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            CreatureLineage lineage = world.Creatures.GetLineageAt(childIndex);
            world.Creatures.Remove(secondParent);

            SimVector2? nearest = world.FindNearestAliveParentForTest(lineage, new SimVector2(0f, 0f));

            Assert.That(nearest.HasValue, Is.True);
            Assert.That(nearest.Value.X, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(nearest.Value.Y, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void FindNearestAliveParentReturnsNullWhenBothParentsDead()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(worldSeed: 13, initialPopulation: 0, schedule: schedule);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            CreatureLineage lineage = world.Creatures.GetLineageAt(childIndex);
            world.Creatures.Remove(firstParent);
            world.Creatures.Remove(secondParent);

            SimVector2? nearest = world.FindNearestAliveParentForTest(lineage, new SimVector2(0f, 0f));

            Assert.That(nearest.HasValue, Is.False);
        }

        [Test]
        public void FindNearestAliveParentReturnsNullForCreatureWithNoLineage()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(worldSeed: 14, initialPopulation: 0, schedule: schedule);
            var world = new SimulationWorld(config);
            CreatureId solo = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(solo, out int soloIndex);
            CreatureLineage lineage = world.Creatures.GetLineageAt(soloIndex);

            SimVector2? nearest = world.FindNearestAliveParentForTest(lineage, new SimVector2(0f, 0f));

            Assert.That(nearest.HasValue, Is.False);
        }
```

These tests call `world.Creatures.GetMovementRefAt(int)` (confirmed at `CreatureStore.cs:207`, returns `ref MovementState`) and `world.Creatures.Remove(CreatureId)` (confirmed at `CreatureStore.cs:237`, returns `bool`). These tests also call a temporary test-only forwarding method `world.FindNearestAliveParentForTest(...)` — add this `public` forwarding method in Step 4 alongside the real private helper, since `FindNearestAliveParent` itself is private and `Assets/Tests/EditMode` is a separate assembly from `Assets/Scripts/Simulation`.

- [ ] **Step 3: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FindNearestAliveParent"`
Expected: FAIL — `FindNearestAliveParentForTest` does not exist (compile error) or `ParentalFollowingEnabled`/related symbols are missing.

- [ ] **Step 4: Implement `FindNearestAliveParent` and the forwarding method**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, add this private method immediately after `GetMovementTarget` (which ends at line 627):

```csharp
        private SimVector2? FindNearestAliveParent(CreatureLineage lineage, SimVector2 position)
        {
            SimVector2? firstPosition = null;
            if (Creatures.TryGetIndex(lineage.FirstParent, out int firstIndex))
            {
                firstPosition = Creatures.GetMovementAt(firstIndex).Position;
            }

            SimVector2? secondPosition = null;
            if (Creatures.TryGetIndex(lineage.SecondParent, out int secondIndex))
            {
                secondPosition = Creatures.GetMovementAt(secondIndex).Position;
            }

            if (!firstPosition.HasValue)
            {
                return secondPosition;
            }

            if (!secondPosition.HasValue)
            {
                return firstPosition;
            }

            float firstDistance = SimVector2.Distance(position, firstPosition.Value);
            float secondDistance = SimVector2.Distance(position, secondPosition.Value);
            return firstDistance <= secondDistance ? firstPosition : secondPosition;
        }

        public SimVector2? FindNearestAliveParentForTest(CreatureLineage lineage, SimVector2 position)
        {
            return FindNearestAliveParent(lineage, position);
        }
```

- [ ] **Step 5: Run tests to verify the `FindNearestAliveParent` tests pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "FindNearestAliveParent"`
Expected: PASS (4/4)

- [ ] **Step 6: Write the failing integration test — juvenile follows parent when idle**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void JuvenileMovesTowardParentWhenWanderingAndFlagEnabled()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 21,
                initialPopulation: 0,
                schedule: schedule,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                parentalFollowingEnabled: true);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(50f, 0f);
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            world.Creatures.GetNeedsRefAt(childIndex).Age = 0f;
            SimVector2 childBefore = world.Creatures.GetMovementAt(childIndex).Position;

            world.Step(config.FixedDeltaTime);

            SimVector2 childAfter = world.Creatures.GetMovementAt(childIndex).Position;
            float distanceToParentBefore = SimVector2.Distance(childBefore, new SimVector2(50f, 0f));
            float distanceToParentAfter = SimVector2.Distance(childAfter, new SimVector2(50f, 0f));
            Assert.That(distanceToParentAfter, Is.LessThan(distanceToParentBefore));
        }
```

- [ ] **Step 7: Run test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter "JuvenileMovesTowardParentWhenWanderingAndFlagEnabled"`
Expected: FAIL — child does not move measurably closer to the parent (still plain random exploration).

- [ ] **Step 8: Implement the `GetMovementTarget` branch**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, insert this new branch immediately before the existing `if (Config.CognitionEnabled && decision.Action == CreatureAction.Wander)` block (currently at line 588):

```csharp
            if (Config.ParentalFollowingEnabled
                && decision.Action == CreatureAction.Wander
                && Creatures.GetNeedsAt(creatureIndex).Age < ReproductionSystem.AdultAgeSeconds)
            {
                CreatureLineage lineage = Creatures.GetLineageAt(creatureIndex);
                SimVector2? parentPosition = FindNearestAliveParent(lineage, position);
                if (parentPosition.HasValue)
                {
                    const float followRadius = 2f;
                    if (SimVector2.Distance(position, parentPosition.Value) > followRadius)
                    {
                        return parentPosition.Value;
                    }

                    long followEpoch = tick / (Config.Schedule.BaseFrequencyHz * 5L);
                    float followAngle = DeterministicRandom.Float01(
                        Config.WorldSeed,
                        RandomDomain.Exploration,
                        followEpoch,
                        creatureId.Value,
                        0,
                        3) * ((float)Math.PI * 2f);
                    return new SimVector2(
                        parentPosition.Value.X + ((float)Math.Cos(followAngle) * followRadius),
                        parentPosition.Value.Y + ((float)Math.Sin(followAngle) * followRadius));
                }
            }

            if (Config.CognitionEnabled && decision.Action == CreatureAction.Wander)
```

(The last line above is the existing line already in the file — this shows where the new block ends and the existing block begins immediately after, unchanged.)

- [ ] **Step 9: Run tests to verify Steps 2 and 6's tests all pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "FindNearestAliveParent|JuvenileMovesTowardParentWhenWanderingAndFlagEnabled"`
Expected: PASS (5/5)

- [ ] **Step 10: Write the failing integration test — urgent need overrides following**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void JuvenileWithUrgentNeedIgnoresParentEvenWithFlagEnabled()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 22,
                initialPopulation: 0,
                schedule: schedule,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                parentalFollowingEnabled: true);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 10f, 10f, 0f);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(50f, 50f);
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(5f, 0f), firstParent, secondParent);
            world.SetCreaturePosition(child, new SimVector2(5f, 0f));
            world.Creatures.TryGetIndex(child, out int childIndex);
            world.Creatures.GetNeedsRefAt(childIndex).Age = 0f;
            world.Creatures.GetNeedsRefAt(childIndex).Energy = 0f;
            SimVector2 childBefore = world.Creatures.GetMovementAt(childIndex).Position;

            world.Step(config.FixedDeltaTime);

            SimVector2 childAfter = world.Creatures.GetMovementAt(childIndex).Position;
            float distanceToFoodBefore = SimVector2.Distance(childBefore, new SimVector2(0f, 0f));
            float distanceToFoodAfter = SimVector2.Distance(childAfter, new SimVector2(0f, 0f));
            Assert.That(distanceToFoodAfter, Is.LessThan(distanceToFoodBefore));
        }
```

`ResourceStore.Add(ResourceKind, SimVector2, float interactionRadius, float initialAmount, float capacity, float regenerationPerSecond, float nutritionMultiplier = 1f)` and `world.SetCreaturePosition(CreatureId, SimVector2)` are confirmed existing methods (`Assets/Scripts/Simulation/Resources/ResourceStore.cs:46`), matching the pattern used by the existing `DecisionTraceRecordsLegacyWinnerScoresAndSwitchesForItsSampledCreature` test in this same file. Setting `Energy = 0f` directly (as that existing test does at line 172) is sufficient to force a `SeekFood` decision under `IntentUtilityV1`.

- [ ] **Step 11: Run test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter "JuvenileWithUrgentNeedIgnoresParentEvenWithFlagEnabled"`
Expected: FAIL if the new branch was placed incorrectly (e.g. before the urgent-action checks earlier in `GetMovementTarget`) — should actually PASS already given Step 8's placement (the new branch only fires for `decision.Action == CreatureAction.Wander`, and a starving creature decides `SeekFood`, not `Wander`, upstream in `DecisionSystem`). If it already passes at this point, that confirms the priority ordering from Step 8 is correct — proceed to Step 12 without further changes.

- [ ] **Step 12: Write the failing integration test — adult unaffected**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void AdultWanderIsUnaffectedByParentalFollowingFlag()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var configFlagOff = new SimulationConfig(
                worldSeed: 23,
                initialPopulation: 0,
                schedule: schedule,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                parentalFollowingEnabled: false);
            var worldFlagOff = new SimulationWorld(configFlagOff);
            CreatureId adultOff = worldFlagOff.Spawn(Genome.Neutral);
            worldFlagOff.Creatures.TryGetIndex(adultOff, out int adultOffIndex);
            worldFlagOff.Creatures.GetNeedsRefAt(adultOffIndex).Age = ReproductionSystem.AdultAgeSeconds;
            SimVector2 beforeOff = worldFlagOff.Creatures.GetMovementAt(adultOffIndex).Position;
            worldFlagOff.Step(configFlagOff.FixedDeltaTime);
            SimVector2 afterOff = worldFlagOff.Creatures.GetMovementAt(adultOffIndex).Position;

            var configFlagOn = new SimulationConfig(
                worldSeed: 23,
                initialPopulation: 0,
                schedule: schedule,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                parentalFollowingEnabled: true);
            var worldFlagOn = new SimulationWorld(configFlagOn);
            CreatureId adultOn = worldFlagOn.Spawn(Genome.Neutral);
            worldFlagOn.Creatures.TryGetIndex(adultOn, out int adultOnIndex);
            worldFlagOn.Creatures.GetNeedsRefAt(adultOnIndex).Age = ReproductionSystem.AdultAgeSeconds;
            SimVector2 beforeOn = worldFlagOn.Creatures.GetMovementAt(adultOnIndex).Position;
            worldFlagOn.Step(configFlagOn.FixedDeltaTime);
            SimVector2 afterOn = worldFlagOn.Creatures.GetMovementAt(adultOnIndex).Position;

            Assert.That(afterOn.X, Is.EqualTo(afterOff.X).Within(0.0001f));
            Assert.That(afterOn.Y, Is.EqualTo(afterOff.Y).Within(0.0001f));
        }
```

This creature has no lineage (spawned via `Spawn`, not `AddChild`), so both worlds should produce identical movement regardless of the flag — proving the flag never engages for an adult (the `Age < AdultAgeSeconds` guard) and never engages for a lineage-less creature (the `FindNearestAliveParent` null-return path) even if it were somehow an adult.

- [ ] **Step 13: Run test to verify it fails, then passes**

Run: `cd tools/HeadlessTests && dotnet test --filter "AdultWanderIsUnaffectedByParentalFollowingFlag"`
Expected: since the implementation from Step 8 already correctly guards on `Age < AdultAgeSeconds`, this should PASS immediately — confirming no further implementation change is needed for this test. If it fails, re-check the guard condition placement from Step 8.

- [ ] **Step 14: Derive the hash-regression baseline**

From the repo root (not inside this worktree), create a throwaway worktree at this task's starting commit (the commit this plan's Task 1 was built on top of — check `git log --oneline -1` on `main` before this task began; record it here as `<PRE_TASK_COMMIT>`):

```bash
git worktree add /c/ls-work/parental-following-baseline <PRE_TASK_COMMIT>
cd /c/ls-work/parental-following-baseline/tools/HeadlessTests
```

Add a temporary test file (or temporary test method) running:

```csharp
SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
var config = new SimulationConfig(
    worldSeed: 99,
    initialPopulation: 2,
    schedule: schedule,
    founderProfile: FounderProfile.PredationVariation);
var world = new SimulationWorld(config);
for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }
Console.WriteLine(world.ComputeStateHash());
```

Run it, capture the printed `ulong` value, then remove the throwaway worktree:

```bash
cd /c/ls-work
git worktree remove /c/ls-work/parental-following-baseline
```

Expected: the printed hash equals `12050501592762519865UL` (every prior hash-regression test this session, using this exact scenario, has produced this identical value, since the scenario never exercises any of the newly flag-gated code paths — including this one, since none of the `PredationVariation` founders in this scenario are ever spawned via `AddChild`, so `ParentalFollowingEnabled`'s branch condition is never reached even when true, let alone when false as it is here).

- [ ] **Step 15: Write the failing hash-regression test**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`, immediately after `JuvenileCapabilityDisabledProducesIdenticalHashToPreExistingBehavior` (ends at line 1216):

```csharp
        // Captured from the pre-Task-1 commit <PRE_TASK_COMMIT> (the commit this task's changes
        // were built on top of), by running this exact setup (with parentalFollowingEnabled
        // omitted, since that constructor parameter did not exist yet) for 50 ticks and reading
        // world.ComputeStateHash(). Pinning this value confirms that adding
        // Config.ParentalFollowingEnabled and its call-site wiring in SimulationWorld.cs is
        // byte-identical to prior behavior when the flag is left at its default (false).
        private const ulong ExpectedParentalFollowingDisabledHash = 12050501592762519865UL;

        [Test]
        public void ParentalFollowingDisabledProducesIdenticalHashToPreExistingBehavior()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var config = new SimulationConfig(
                worldSeed: 99,
                initialPopulation: 2,
                schedule: schedule,
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedParentalFollowingDisabledHash));
        }
```

Replace `<PRE_TASK_COMMIT>` in the comment with the actual commit hash recorded in Step 14.

- [ ] **Step 16: Run the full test suite**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including every new test added in this task (12 new tests: 4 `FindNearestAliveParent` unit tests, 3 integration tests, 1 hash-regression test, plus this task must not have broken any of the 298 pre-existing tests — expect 298 + 8 = 306 passing, adjust if the actual pre-existing count differs).

- [ ] **Step 17: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Add parental following: juveniles move toward their nearest alive parent when idle"
```

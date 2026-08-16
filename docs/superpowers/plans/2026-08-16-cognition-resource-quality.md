# Cognition-Mode Resource Quality (B-3 remainder) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Under `Config.CognitionEnabled` (Legacy decision policy), when `SimulationConfig.LearnedResourceQualityEnabled` is set, `DecisionSystem.DecideFromLearnedOutcomes` weighs a visible resource's remaining `Amount` alongside its existing remembered-quality signal, instead of ignoring `Amount` entirely - flag defaults `false` and is byte-identical to today's behavior when off.

**Architecture:** Extract the Amount-weighted `needGain` formula already used by `ResourceUtility` (the `IntentUtilityV1` resource-scoring path) into a shared `ComputeNeedGain` helper. `ResourceUtility` switches to calling it (behavior-neutral refactor). `DecideFromLearnedOutcomes` gains a `ResourceStore` parameter and a flag, and multiplies `ComputeNeedGain`'s result into its existing score alongside the remembered-quality term.

**Tech Stack:** C#, Unity Test Framework (NUnit), EditMode tests.

## Global Constraints

- New flag `SimulationConfig.LearnedResourceQualityEnabled`, default `false`.
- The new Amount-weighted term MULTIPLIES alongside the existing remembered-quality (`foodValue`/`waterValue`) term - it does not replace it. Both signals must independently affect the score.
- Scope: only `DecisionSystem.DecideFromLearnedOutcomes` (Legacy policy + `CognitionEnabled`) changes behavior. `IntentUtilityV1`'s `ResourceUtility` path already reads `Amount` - untouched except for the internal `ComputeNeedGain` extraction, which must be behavior-neutral (same formula, same inputs, same output).
- No change to `PreferRememberedResource`/`ScoreRememberedResource` (memory-only paths with no live `ResourceState` to weigh).
- When the flag is `false`, `DecideFromLearnedOutcomes` must execute identically to before this task - proven by a hash-regression test using the established methodology (throwaway worktree at the pre-task commit, same fixed `PredationVariation` scenario, 50 `Step()` calls, `ComputeStateHash()`).
- **Coverage note:** the standard hash-regression scenario (`PredationVariation` founder, default `DecisionPolicyVersion.Legacy`, `CognitionEnabled` left at its default `false`) never reaches `DecideFromLearnedOutcomes` at all (that call site is gated on `Config.CognitionEnabled`, `SimulationWorld.cs:860`) and never reaches `ResourceUtility` either (that's only called from `DecideIntentUtilityV1`, which the scenario doesn't use). This is consistent with every prior hash-regression test this session - the hash test proves "the default path is provably unaffected," not "the new code was exercised." The `ComputeNeedGain` refactor's correctness is proven separately, by Task 1's dedicated unit test comparing its output against the pre-refactor inline formula - not by the hash-regression test.

---

### Task 1: `ComputeNeedGain` extraction, `DecideFromLearnedOutcomes` scoring, and flag wiring

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` (`ResourceUtility` at line 737, `DecideFromLearnedOutcomes` at line 809)
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (constructor + properties)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (`TickDecisions`, line 862)
- Modify: `Assets/Tests/EditMode/SpatialBehaviorTests.cs` (line 225, compile fix for the new required parameter)
- Test: `Assets/Tests/EditMode/DecisionSystemTests.cs` (unit tests for `ComputeNeedGain` and `DecideFromLearnedOutcomes`)
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs` (integration test, hash-regression test)

**Interfaces:**
- Consumes: `ResourceStore.GetAt(int)` (returns `ResourceState` with `.Amount`/`.NutritionMultiplier`), `Phenotype.FoodYield`/`.EnergyCapacity`/`.HydrationCapacity`, `CreatureNeeds.Energy`/`.Hydration`, `ResourceObservation.IsValid`/`.ResourceIndex`/`.Distance`.
- Produces: `DecisionSystem.ComputeNeedGain(bool, CreatureNeeds, Phenotype, ResourceState)` (private static, returns `float`) - used by both `ResourceUtility` and `DecideFromLearnedOutcomes` within `DecisionSystem.cs`, not consumed elsewhere. `SimulationConfig.LearnedResourceQualityEnabled` (`bool` property, default `false`). `DecisionSystem.DecideFromLearnedOutcomes`'s new signature: `(CreatureNeeds needs, Phenotype phenotype, MemoryState memory, ResourceObservation food, ResourceObservation water, ResourceStore resources, out DecisionDiagnostics diagnostics, bool learnedResourceQualityEnabled = false)`.

- [ ] **Step 1: Write the failing unit tests for `ComputeNeedGain`**

Add to `Assets/Tests/EditMode/DecisionSystemTests.cs`, inside the `DecisionSystemTests` class:

```csharp
        [Test]
        public void ComputeNeedGainMatchesTheOriginalResourceUtilityInlineFormulaForFood()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 0.5f, defense: 0.5f, maneuverability: 0.5f, energyCapacity: 100f);
            var needs = new CreatureNeeds { Energy = 20f, Hydration = 100f };
            var resource = new ResourceState(new ResourceId(1), ResourceKind.Food, new SimVector2(0f, 0f), interactionRadius: 1f, amount: 5f, capacity: 10f, regenerationPerSecond: 0f, isActive: true, nutritionMultiplier: 1f);

            float needGain = DecisionSystem.ComputeNeedGainForTest(seekingWater: false, needs, phenotype, resource);

            // Manually reproduces ResourceUtility's original inline formula (pre-refactor):
            // missing = 100 - 20 = 80; perUnitGain = 20 * FoodYield * 1; needGain = min(1, (5 * perUnitGain) / 80)
            float missing = 100f - 20f;
            float perUnitGain = 20f * phenotype.FoodYield * resource.NutritionMultiplier;
            float expected = Math.Min(1f, (resource.Amount * perUnitGain) / missing);
            Assert.That(needGain, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ComputeNeedGainMatchesTheOriginalResourceUtilityInlineFormulaForWater()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 0.5f, defense: 0.5f, maneuverability: 0.5f, energyCapacity: 100f);
            var needs = new CreatureNeeds { Energy = 100f, Hydration = 30f };
            var resource = new ResourceState(new ResourceId(2), ResourceKind.Water, new SimVector2(0f, 0f), interactionRadius: 1f, amount: 8f, capacity: 10f, regenerationPerSecond: 0f, isActive: true, nutritionMultiplier: 1f);

            float needGain = DecisionSystem.ComputeNeedGainForTest(seekingWater: true, needs, phenotype, resource);

            // Water's perUnitGain is a flat 20f (not FoodYield/NutritionMultiplier-scaled) per the
            // original ResourceUtility formula.
            float missing = 100f - 30f;
            float expected = Math.Min(1f, (resource.Amount * 20f) / missing);
            Assert.That(needGain, Is.EqualTo(expected).Within(0.0001f));
        }
```

Check `Assets/Tests/EditMode/DecisionSystemTests.cs`'s existing `MakePhenotype` helper (already in that file, per its `PhenotypeConstructor` reflection pattern) for the exact parameter names/defaults it accepts, and use it exactly as it already exists - do not redefine it. Also verify `ResourceState`'s exact constructor parameter order/names against `Assets/Scripts/Simulation/Resources/ResourceTypes.cs` before finalizing this step (confirmed as `ResourceState(ResourceId id, ResourceKind kind, SimVector2 position, float interactionRadius, float amount, float capacity, float regenerationPerSecond, bool isActive, float nutritionMultiplier, float plantDefense = 0f)` as of this plan's writing - re-check if it has changed).

These tests call `DecisionSystem.ComputeNeedGainForTest(...)`, a test-only forwarding method (added in Step 4) since `ComputeNeedGain` is private and `Assets/Tests/EditMode` is a separate assembly with no `InternalsVisibleTo` (confirmed absent from this repo across every prior task this session).

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "ComputeNeedGain"`
Expected: FAIL - compile error (`ComputeNeedGainForTest` doesn't exist yet).

- [ ] **Step 3: Extract `ComputeNeedGain` and refactor `ResourceUtility`**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, add this new private static method immediately before `ResourceUtility` (currently at line 737):

```csharp
        private static float ComputeNeedGain(bool seekingWater, CreatureNeeds needs, Phenotype phenotype, ResourceState resource)
        {
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float missing = Math.Max(0.0001f, capacity - current);
            float perUnitGain = seekingWater ? 20f : 20f * phenotype.FoodYield * resource.NutritionMultiplier;
            return Math.Min(1f, (resource.Amount * perUnitGain) / missing);
        }
```

Modify `ResourceUtility` to call it instead of its inline calculation. Its current body (lines 737-757) is:

```csharp
        private static float ResourceUtility(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceState resource,
            float distance,
            CreatureObservation threat,
            float threatIntensity)
        {
            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float missing = Math.Max(0.0001f, capacity - current);
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float perUnitGain = seekingWater ? 20f : 20f * phenotype.FoodYield * resource.NutritionMultiplier;
            float needGain = Math.Min(1f, (resource.Amount * perUnitGain) / missing);
            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float dangerPenalty = threat.IsValid ? Math.Max(0f, threatIntensity) * genome.RiskAversion * (distance / Math.Max(0.01f, phenotype.MaximumSpeed)) : 0f;
            return Math.Max(0f, (urgency * needGain) - travelBurden - dangerPenalty);
        }
```

Replace it with (removing the now-duplicated `perUnitGain`/`needGain` lines, keeping `capacity`/`current` since `urgency` still needs them directly):

```csharp
        private static float ResourceUtility(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceState resource,
            float distance,
            CreatureObservation threat,
            float threatIntensity)
        {
            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float needGain = ComputeNeedGain(seekingWater, needs, phenotype, resource);
            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float dangerPenalty = threat.IsValid ? Math.Max(0f, threatIntensity) * genome.RiskAversion * (distance / Math.Max(0.01f, phenotype.MaximumSpeed)) : 0f;
            return Math.Max(0f, (urgency * needGain) - travelBurden - dangerPenalty);
        }
```

This is behavior-neutral: `ComputeNeedGain` computes `missing`/`perUnitGain`/`needGain` with the exact same formula and inputs `ResourceUtility` used inline, so its return value is identical for every input.

- [ ] **Step 4: Add the test-only forwarding method**

Add this `public` forwarding method to `DecisionSystem.cs`, at the end of the `DecisionSystem` static class (immediately before its closing brace):

```csharp
        public static float ComputeNeedGainForTest(bool seekingWater, CreatureNeeds needs, Phenotype phenotype, ResourceState resource)
        {
            return ComputeNeedGain(seekingWater, needs, phenotype, resource);
        }
```

- [ ] **Step 5: Run tests to verify Step 1's tests pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "ComputeNeedGain"`
Expected: PASS (2/2)

- [ ] **Step 6: Run the full existing suite to confirm the `ResourceUtility` refactor is behavior-neutral**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all 314 pre-existing tests still pass (this refactor must not change any existing test's outcome, including every `IntentUtilityV1` predation/foraging test that exercises `ResourceUtility` transitively).

- [ ] **Step 7: Add `LearnedResourceQualityEnabled` to `SimulationConfig`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, add a new constructor parameter immediately after `kinRecognitionEnabled` (the current last parameter):

```csharp
            bool kinRecognitionEnabled = false,
            bool learnedResourceQualityEnabled = false)
```

Add the assignment immediately after `KinRecognitionEnabled = kinRecognitionEnabled;`:

```csharp
            KinRecognitionEnabled = kinRecognitionEnabled;
            LearnedResourceQualityEnabled = learnedResourceQualityEnabled;
```

Add the property immediately after `public bool KinRecognitionEnabled { get; }`:

```csharp
        public bool KinRecognitionEnabled { get; }
        public bool LearnedResourceQualityEnabled { get; }
```

- [ ] **Step 8: Write the failing unit tests for `DecideFromLearnedOutcomes`**

Add to `Assets/Tests/EditMode/DecisionSystemTests.cs`:

```csharp
        [Test]
        public void LearnedOutcomesPrefersWellStockedPatchOverNearlyDepletedPatchWhenQualityEnabled()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 0.5f, defense: 0.5f, maneuverability: 0.5f, energyCapacity: 100f);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var memory = new MemoryState { FoodOutcomeValue = 0.5f, WaterOutcomeValue = 0f, FoodExperienceCount = 1, WaterExperienceCount = 0 };
            var richResources = new ResourceStore(initialCapacity: 2);
            richResources.Add(ResourceKind.Food, new SimVector2(1f, 0f), interactionRadius: 1f, initialAmount: 10f, capacity: 10f, regenerationPerSecond: 0f);
            var richFood = new ResourceObservation(new ResourceId(1), 0, distance: 1f);
            var noWater = new ResourceObservation(new ResourceId(2), -1, distance: 1f);

            CreatureDecision richDecision = DecisionSystem.DecideFromLearnedOutcomes(
                needs, phenotype, memory, richFood, default, richResources, out DecisionDiagnostics richDiagnostics, learnedResourceQualityEnabled: true);

            var poorResources = new ResourceStore(initialCapacity: 2);
            poorResources.Add(ResourceKind.Food, new SimVector2(1f, 0f), interactionRadius: 1f, initialAmount: 0.1f, capacity: 10f, regenerationPerSecond: 0f);
            var poorFood = new ResourceObservation(new ResourceId(1), 0, distance: 1f);

            CreatureDecision poorDecision = DecisionSystem.DecideFromLearnedOutcomes(
                needs, phenotype, memory, poorFood, default, poorResources, out DecisionDiagnostics poorDiagnostics, learnedResourceQualityEnabled: true);

            Assert.That(richDiagnostics.FoodScore, Is.GreaterThan(poorDiagnostics.FoodScore));
        }

        [Test]
        public void LearnedOutcomesStillWeighsRememberedValueAlongsideAmountWhenQualityEnabled()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 0.5f, defense: 0.5f, maneuverability: 0.5f, energyCapacity: 100f);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 2);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), interactionRadius: 1f, initialAmount: 5f, capacity: 10f, regenerationPerSecond: 0f);
            var food = new ResourceObservation(new ResourceId(1), 0, distance: 1f);

            var goodHistoryMemory = new MemoryState { FoodOutcomeValue = 1f, FoodExperienceCount = 5 };
            CreatureDecision goodHistoryDecision = DecisionSystem.DecideFromLearnedOutcomes(
                needs, phenotype, goodHistoryMemory, food, default, resources, out DecisionDiagnostics goodHistoryDiagnostics, learnedResourceQualityEnabled: true);

            var noHistoryMemory = new MemoryState { FoodOutcomeValue = 0f, FoodExperienceCount = 0 };
            CreatureDecision noHistoryDecision = DecisionSystem.DecideFromLearnedOutcomes(
                needs, phenotype, noHistoryMemory, food, default, resources, out DecisionDiagnostics noHistoryDiagnostics, learnedResourceQualityEnabled: true);

            // Same Amount in both runs, so if remembered value still matters (multiplied
            // alongside, not replaced by the new Amount term), the good-history run must
            // score strictly higher than the no-history run.
            Assert.That(goodHistoryDiagnostics.FoodScore, Is.GreaterThan(noHistoryDiagnostics.FoodScore));
        }
```

Verify `DecisionDiagnostics`'s exact constructor/property names (`FoodScore`/`WaterScore`, confirmed via `DecideFromLearnedOutcomes`'s existing `diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);` line) and `ResourceStore.Add`'s exact signature (confirmed as `Add(ResourceKind kind, SimVector2 position, float interactionRadius, float initialAmount, float capacity, float regenerationPerSecond, float nutritionMultiplier = 1f)` at `ResourceStore.cs:46`, used already elsewhere this session) before finalizing this step - re-check if either has changed. `default` for the unused `water`/`ResourceObservation` and `MemoryState` fields not set explicitly is safe (an invalid/zeroed `ResourceObservation.IsValid` is `false` by construction, matching how every other test in this file passes `default` for an unused side).

- [ ] **Step 9: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "LearnedOutcomes"`
Expected: FAIL - compile error (`DecideFromLearnedOutcomes` doesn't accept a `ResourceStore` argument or `learnedResourceQualityEnabled` named parameter yet).

- [ ] **Step 10: Implement the `DecideFromLearnedOutcomes` scoring change**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, replace the current `DecideFromLearnedOutcomes` (lines 809-830):

```csharp
        public static CreatureDecision DecideFromLearnedOutcomes(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            ResourceObservation food,
            ResourceObservation water,
            ResourceStore resources,
            out DecisionDiagnostics diagnostics,
            bool learnedResourceQualityEnabled = false)
        {
            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodNeedGain = learnedResourceQualityEnabled && food.IsValid
                ? ComputeNeedGain(false, needs, phenotype, resources.GetAt(food.ResourceIndex))
                : 1f;
            float waterNeedGain = learnedResourceQualityEnabled && water.IsValid
                ? ComputeNeedGain(true, needs, phenotype, resources.GetAt(water.ResourceIndex))
                : 1f;
            float foodScore = food.IsValid ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance) * foodValue * foodNeedGain : -1f;
            float waterScore = water.IsValid ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance) * waterValue * waterNeedGain : -1f;
            diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);
            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            return waterScore > foodScore
                ? new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore)
                : new CreatureDecision(CreatureAction.SeekFood, food.ResourceIndex, foodScore);
        }
```

- [ ] **Step 11: Fix the pre-existing call site in `SpatialBehaviorTests.cs`**

In `Assets/Tests/EditMode/SpatialBehaviorTests.cs`, the existing test `CognitiveResourceDecisionUsesLearnedOutcomesRatherThanAssumingWaterIsAlwaysBest` (around line 217-234) calls `DecisionSystem.DecideFromLearnedOutcomes` with 5 positional arguments followed by `out _`. Its current call (lines 225-231):

```csharp
            CreatureDecision decision = DecisionSystem.DecideFromLearnedOutcomes(
                needs,
                phenotype,
                memory,
                new ResourceObservation(new ResourceId(1), 0, 1f),
                new ResourceObservation(new ResourceId(2), 1, 1f),
                out _);
```

Update it to pass an empty `ResourceStore` as the new required 6th argument (safe here since `learnedResourceQualityEnabled` stays at its default `false`, so `resources.GetAt(...)` is never actually called for this test's inputs):

```csharp
            CreatureDecision decision = DecisionSystem.DecideFromLearnedOutcomes(
                needs,
                phenotype,
                memory,
                new ResourceObservation(new ResourceId(1), 0, 1f),
                new ResourceObservation(new ResourceId(2), 1, 1f),
                new ResourceStore(initialCapacity: 0),
                out _);
```

Check whether `Assets/Tests/EditMode/SpatialBehaviorTests.cs` already has a `using LifeSimulation.Simulation.Resources;` (needed for `ResourceStore`) at its top - add it if missing.

- [ ] **Step 12: Update the `SimulationWorld.TickDecisions` call site**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, change the current call (line 862):

```csharp
                    decision = DecisionSystem.DecideFromLearnedOutcomes(Creatures.GetNeedsAt(index), phenotype, Creatures.GetMemoryRefAt(index), food, water, out diagnostics);
```

to:

```csharp
                    decision = DecisionSystem.DecideFromLearnedOutcomes(Creatures.GetNeedsAt(index), phenotype, Creatures.GetMemoryRefAt(index), food, water, Resources, out diagnostics, Config.LearnedResourceQualityEnabled);
```

- [ ] **Step 13: Run tests to verify Steps 8 and 11's tests pass, and nothing else broke**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all pass. Compile errors from Step 11's call site are resolved; `LearnedOutcomes` tests from Step 8 now pass (2/2); the pre-existing `CognitiveResourceDecisionUsesLearnedOutcomesRatherThanAssumingWaterIsAlwaysBest` test still passes unchanged (its behavior is identical since `learnedResourceQualityEnabled` defaults `false` there).

- [ ] **Step 14: Write the failing integration test**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void CognitiveCreaturePrefersFartherRicherResourceOverCloserNearlyDepletedOneWhenQualityEnabled()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 41,
                initialPopulation: 0,
                schedule: schedule,
                cognitionEnabled: true,
                learnedResourceQualityEnabled: true);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), interactionRadius: 1f, initialAmount: 0.1f, capacity: 10f, regenerationPerSecond: 0f);
            world.Resources.Add(ResourceKind.Food, new SimVector2(6f, 0f), interactionRadius: 1f, initialAmount: 10f, capacity: 10f, regenerationPerSecond: 0f);
            CreatureId creature = world.Spawn(Genome.Neutral);
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));
            world.Creatures.TryGetIndex(creature, out int index);
            world.Creatures.GetNeedsRefAt(index).Energy = 0f;

            world.Step(config.FixedDeltaTime);

            CreatureDecision decision = world.Creatures.GetDecisionAt(index);
            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(1));
        }
```

Check `world.SetCreaturePosition` exists (confirmed used in part 2/part 3's tests this session); if not present under that exact name, use `world.Creatures.GetMovementRefAt(index).Position = new SimVector2(0f, 0f);` instead, matching the fallback pattern already used elsewhere this session. `CreatePrototype1Defaults`'s default vision range and this config's default schedule frequencies must let the creature perceive both resources at distances 1 and 6 within one `Step()` - if the default `SimulationConfig` constructor's phenotype/vision assumptions make resource index 1 (distance 6) not visible, reduce its distance in this test (e.g. place it at `(3f, 0f)`) so both stay within default vision range while still being farther than the depleted one.

- [ ] **Step 15: Run test to verify it fails, then implement/adjust as needed**

Run: `cd tools/HeadlessTests && dotnet test --filter "PrefersFartherRicherResource"`
Expected: FAIL before any adjustment only if the test's distances/vision assumptions don't hold (in which case adjust distances per Step 14's note, not the production code - the scoring implementation from Steps 3/10 is already complete and correct at this point). Once distances are confirmed workable, this test should PASS immediately, since it exercises code already implemented in Steps 3-12.

- [ ] **Step 16: Derive the hash-regression baseline**

From the repo root, record the current `main` tip before this task's changes (`git log --oneline -1 main`) as `<PRE_TASK_COMMIT>`. Create a throwaway worktree at that commit:

```bash
git worktree add /c/ls-work/cognition-resource-quality-baseline <PRE_TASK_COMMIT>
cd /c/ls-work/cognition-resource-quality-baseline/tools/HeadlessTests
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
git worktree remove /c/ls-work/cognition-resource-quality-baseline
```

Expected: the printed hash equals `12050501592762519865UL` (every prior hash-regression test this session, using this exact scenario, has produced this identical value - this scenario uses default `DecisionPolicyVersion.Legacy` with `CognitionEnabled` left `false`, so it never reaches `DecideFromLearnedOutcomes` at all, and never reaches `ResourceUtility`/`ComputeNeedGain` either since that requires `IntentUtilityV1`. This hash test proves the untouched default path stays untouched; it does not exercise this task's new code - that's covered by Steps 1, 8, and 14's dedicated tests instead).

- [ ] **Step 17: Write the failing hash-regression test**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`, immediately after `KinRecognitionDisabledProducesIdenticalHashToPreExistingBehavior` (the most recent hash-regression test in the file):

```csharp
        // Captured from the pre-Task-1 commit <PRE_TASK_COMMIT> (the commit this task's changes
        // were built on top of), by running this exact setup (with learnedResourceQualityEnabled
        // omitted, since that constructor parameter did not exist yet) for 50 ticks and reading
        // world.ComputeStateHash(). This scenario uses default DecisionPolicyVersion.Legacy with
        // CognitionEnabled left false, so it never reaches DecideFromLearnedOutcomes or
        // ResourceUtility/ComputeNeedGain at all - it proves the untouched default path stays
        // untouched, not that this task's new code was exercised (see Steps 1/8/14 for that).
        private const ulong ExpectedLearnedResourceQualityDisabledHash = 12050501592762519865UL;

        [Test]
        public void LearnedResourceQualityDisabledProducesIdenticalHashToPreExistingBehavior()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var config = new SimulationConfig(
                worldSeed: 99,
                initialPopulation: 2,
                schedule: schedule,
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedLearnedResourceQualityDisabledHash));
        }
```

Replace `<PRE_TASK_COMMIT>` in the comment with the actual commit hash recorded in Step 16.

- [ ] **Step 18: Run the full test suite**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass. This task adds 7 new tests (2 `ComputeNeedGain` unit tests, 2 `DecideFromLearnedOutcomes` unit tests, 1 integration test, 1 hash-regression test, in `DecisionSystemTests.cs`/`CoreSimulationTests.cs`) on top of the 314 already passing as of the last merge to `main` - expect 321 passing. Adjust if the actual pre-task count differs (check `git log --oneline -1 main` first).

- [ ] **Step 19: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/SpatialBehaviorTests.cs Assets/Tests/EditMode/DecisionSystemTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Weigh resource Amount alongside remembered quality in cognition-mode decisions (B-3 remainder)"
```

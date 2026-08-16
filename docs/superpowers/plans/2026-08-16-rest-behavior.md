# Rest Behavior (C-4) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `CreatureAction.Rest` a real, functional action — resting recovers the `Rest` need, letting it hit `0` causes health damage (mirroring `Energy`/`Hydration`), `IntentUtilityV1` can actually select `Rest` when it's urgent, and a resting creature stays put instead of wandering — all gated behind `SimulationConfig.RestBehaviorEnabled` (default `false`).

**Architecture:** A single task touching four files: `NeedsSystem.cs` (recovery + consequence), `DecisionSystem.cs` (new `CreatureIntent.Rest` + scoring), `SimulationConfig.cs` (new flag), `SimulationWorld.cs` (movement target + wiring both call sites).

**Tech Stack:** C#, Unity, headless NUnit test harness (`tools/HeadlessTests`, plain `dotnet test`, .NET 8).

## Global Constraints

- `restBehaviorEnabled`/`isResting` parameters on `NeedsSystem.Tick` and `restBehaviorEnabled` on `DecideIntentUtilityV1` all default to `false`.
- When `SimulationConfig.RestBehaviorEnabled` is `false` (the default), every existing scenario's output must be byte-identical to today's — proven by a hash-regression test.
- `RestCapacity = 100f` (matches `CreatureNeeds.Full`'s existing hardcoded `Rest = 100f`), `RestRecoveryPerSecond = 5f`, `RestExhaustionHealthCostPerSecond = 3f` (between `Energy`'s `4f` and `Hydration`'s `5f` cost-at-zero rates in `NeedsSystem.Tick`) — exact values, do not substitute different numbers.
- `CreatureIntent.Rest` is appended as the LAST value in the `CreatureIntent` enum (`DecisionSystem.cs`) so no existing enum value's numeric ordinal changes.

---

### Task 1: Rest recovery, consequence, scoring, and movement

**Files:**
- Modify: `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` (`CreatureIntent` enum, `ToDecision`, both `DecideIntentUtilityV1` overloads)
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (new `RestBehaviorEnabled` flag, mirroring `MultiThreatPerceptionEnabled`'s placement)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (`GetMovementTarget`, `TickNeeds`, the `DecideIntentUtilityV1` call site)
- Test: `Assets/Tests/EditMode/BiologyTests.cs`, `Assets/Tests/EditMode/DecisionSystemTests.cs`, `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Consumes: existing `CreatureNeeds`, `Phenotype`, `CreatureIntent`/`CreatureAction`/`DecisionCandidate`/`DecisionCandidateBuffer` (all unchanged in shape except the new enum value), `Urgency` (existing private static helper in `DecisionSystem.cs`).
- Produces: `NeedsSystem.RestCapacity` (new public const, consumed by `DecisionSystem.cs`), `NeedsSystem.Tick`'s two new trailing params, `CreatureIntent.Rest`, `SimulationConfig.RestBehaviorEnabled` — no other task in this plan depends on these, this is the only task.

**`NeedsSystem.cs`** — current `Tick` method (`NeedsSystem.cs:31-65`):

```csharp
public static void Tick(ref CreatureNeeds needs, Phenotype phenotype, float deltaTime, float movementDistance)
{
    if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
    {
        throw new ArgumentOutOfRangeException(nameof(deltaTime));
    }

    if (movementDistance < 0f || float.IsNaN(movementDistance) || float.IsInfinity(movementDistance))
    {
        throw new ArgumentOutOfRangeException(nameof(movementDistance));
    }

    float energyCost = (phenotype.BasalEnergyCostMultiplier * deltaTime)
        + (movementDistance * phenotype.BodyMass * 0.5f);
    float hydrationCost = phenotype.BodyMass
        * phenotype.DigestionRate
        * phenotype.WaterLossMultiplier
        * 0.75f
        * deltaTime;

    needs.Energy = Math.Max(0f, needs.Energy - energyCost);
    needs.Hydration = Math.Max(0f, needs.Hydration - hydrationCost);
    needs.Rest = Math.Max(0f, needs.Rest - (0.1f * phenotype.CognitionRestCostMultiplier * deltaTime));
    needs.Age += deltaTime;

    if (needs.Energy <= 0f)
    {
        needs.Health = Math.Max(0f, needs.Health - (4f * deltaTime));
    }

    if (needs.Hydration <= 0f)
    {
        needs.Health = Math.Max(0f, needs.Health - (5f * deltaTime));
    }
}
```

New — add two trailing optional parameters, replace the single `needs.Rest = ...` line with a conditional branch, and add a new health-consequence check after the existing two:

```csharp
public static void Tick(ref CreatureNeeds needs, Phenotype phenotype, float deltaTime, float movementDistance, bool restBehaviorEnabled = false, bool isResting = false)
{
    if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
    {
        throw new ArgumentOutOfRangeException(nameof(deltaTime));
    }

    if (movementDistance < 0f || float.IsNaN(movementDistance) || float.IsInfinity(movementDistance))
    {
        throw new ArgumentOutOfRangeException(nameof(movementDistance));
    }

    float energyCost = (phenotype.BasalEnergyCostMultiplier * deltaTime)
        + (movementDistance * phenotype.BodyMass * 0.5f);
    float hydrationCost = phenotype.BodyMass
        * phenotype.DigestionRate
        * phenotype.WaterLossMultiplier
        * 0.75f
        * deltaTime;

    needs.Energy = Math.Max(0f, needs.Energy - energyCost);
    needs.Hydration = Math.Max(0f, needs.Hydration - hydrationCost);
    if (restBehaviorEnabled && isResting)
    {
        needs.Rest = Math.Min(RestCapacity, needs.Rest + (RestRecoveryPerSecond * deltaTime));
    }
    else
    {
        needs.Rest = Math.Max(0f, needs.Rest - (0.1f * phenotype.CognitionRestCostMultiplier * deltaTime));
    }
    needs.Age += deltaTime;

    if (needs.Energy <= 0f)
    {
        needs.Health = Math.Max(0f, needs.Health - (4f * deltaTime));
    }

    if (needs.Hydration <= 0f)
    {
        needs.Health = Math.Max(0f, needs.Health - (5f * deltaTime));
    }

    if (restBehaviorEnabled && needs.Rest <= 0f)
    {
        needs.Health = Math.Max(0f, needs.Health - (RestExhaustionHealthCostPerSecond * deltaTime));
    }
}
```

Add three new constants near the existing `FoodEnergyPerUnit`/`WaterHydrationPerUnit` constants (`NeedsSystem.cs:28-29`):

```csharp
private const float FoodEnergyPerUnit = 20f;
private const float WaterHydrationPerUnit = 20f;
public const float RestCapacity = 100f;
private const float RestRecoveryPerSecond = 5f;
private const float RestExhaustionHealthCostPerSecond = 3f;
```

**`DecisionSystem.cs`** — `CreatureIntent` enum, current (`DecisionSystem.cs:26-36`):

```csharp
public enum CreatureIntent : byte
{
    Wander = 0,
    SeekFood = 1,
    SeekWater = 2,
    SeekPrey = 3,
    Flee = 4,
    SeekCarcass = 5,
    SeekThermalComfort = 6,
    SeekMate = 7,
}
```

New — append `Rest = 8`:

```csharp
public enum CreatureIntent : byte
{
    Wander = 0,
    SeekFood = 1,
    SeekWater = 2,
    SeekPrey = 3,
    Flee = 4,
    SeekCarcass = 5,
    SeekThermalComfort = 6,
    SeekMate = 7,
    Rest = 8,
}
```

`ToDecision`'s switch, current (`DecisionSystem.cs:439-455`):

```csharp
private static CreatureDecision ToDecision(DecisionCandidate candidate)
{
    CreatureAction action;
    switch (candidate.Intent)
    {
        case CreatureIntent.SeekWater: action = CreatureAction.SeekWater; break;
        case CreatureIntent.SeekPrey: action = CreatureAction.SeekPrey; break;
        case CreatureIntent.Flee: action = CreatureAction.Flee; break;
        case CreatureIntent.SeekCarcass: action = CreatureAction.SeekCarcass; break;
        case CreatureIntent.SeekThermalComfort: action = CreatureAction.SeekThermalComfort; break;
        case CreatureIntent.SeekMate: action = CreatureAction.SeekMate; break;
        case CreatureIntent.Wander: action = CreatureAction.Wander; break;
        default: action = CreatureAction.SeekFood; break;
    }

    return new CreatureDecision(action, candidate.TargetResourceIndex, candidate.Score, targetCreatureId: candidate.TargetCreatureId);
}
```

New — add a `Rest` case:

```csharp
private static CreatureDecision ToDecision(DecisionCandidate candidate)
{
    CreatureAction action;
    switch (candidate.Intent)
    {
        case CreatureIntent.SeekWater: action = CreatureAction.SeekWater; break;
        case CreatureIntent.SeekPrey: action = CreatureAction.SeekPrey; break;
        case CreatureIntent.Flee: action = CreatureAction.Flee; break;
        case CreatureIntent.SeekCarcass: action = CreatureAction.SeekCarcass; break;
        case CreatureIntent.SeekThermalComfort: action = CreatureAction.SeekThermalComfort; break;
        case CreatureIntent.SeekMate: action = CreatureAction.SeekMate; break;
        case CreatureIntent.Rest: action = CreatureAction.Rest; break;
        case CreatureIntent.Wander: action = CreatureAction.Wander; break;
        default: action = CreatureAction.SeekFood; break;
    }

    return new CreatureDecision(action, candidate.TargetResourceIndex, candidate.Score, targetCreatureId: candidate.TargetCreatureId);
}
```

Both `DecideIntentUtilityV1` overloads (current full text at `DecisionSystem.cs:329-357` short form, `:359-437` full form) gain one new trailing parameter, `bool restBehaviorEnabled = false`, after `multiThreatPerceptionEnabled`. The short overload's signature becomes:

```csharp
public static CreatureDecision DecideIntentUtilityV1(
    CreatureNeeds needs,
    Genome genome,
    Phenotype phenotype,
    ResourceStore resources,
    SimVector2 origin,
    ResourceCandidateBuffer foodCandidates,
    ResourceCandidateBuffer waterCandidates,
    ResourceObservation carcass,
    MemoryState memory,
    bool cognitionEnabled,
    CreatureObservation threat,
    float threatIntensity,
    Phenotype otherPhenotype,
    bool predationEnabled,
    bool physiologyEnabled,
    long tick,
    out DecisionDiagnostics diagnostics,
    bool economicsEnabled = false,
    float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
    PredationCandidateBuffer otherCandidates = default,
    bool multiThreatPerceptionEnabled = false,
    bool restBehaviorEnabled = false)
{
    return DecideIntentUtilityV1(
        needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
        cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
        default, default, default, default, default, false, tick, out diagnostics, economicsEnabled,
        threatFalloffDistance, otherCandidates, multiThreatPerceptionEnabled, restBehaviorEnabled);
}
```

The full overload's signature gains the same trailing parameter:

```csharp
public static CreatureDecision DecideIntentUtilityV1(
    CreatureNeeds needs,
    Genome genome,
    Phenotype phenotype,
    ResourceStore resources,
    SimVector2 origin,
    ResourceCandidateBuffer foodCandidates,
    ResourceCandidateBuffer waterCandidates,
    ResourceObservation carcass,
    MemoryState memory,
    bool cognitionEnabled,
    CreatureObservation threat,
    float threatIntensity,
    Phenotype otherPhenotype,
    bool predationEnabled,
    bool physiologyEnabled,
    ReproductionState reproduction,
    CreatureObservation mate,
    CreatureNeeds mateNeeds,
    Phenotype matePhenotype,
    ReproductionState mateReproduction,
    bool reproductionEnabled,
    long tick,
    out DecisionDiagnostics diagnostics,
    bool economicsEnabled = false,
    float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
    PredationCandidateBuffer otherCandidates = default,
    bool multiThreatPerceptionEnabled = false,
    bool restBehaviorEnabled = false)
```

and its body's current thermal-comfort block (`DecisionSystem.cs:414-422`):

```csharp
float thermalScore = 0f;
if (physiologyEnabled)
{
    thermalScore = ThermoregulationSystem.ScoreThermalComfort(phenotype, origin, tick);
    if (thermalScore >= 0.15f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekThermalComfort, -1, default, thermalScore));
    }
}
```

gets a new sibling block immediately after it:

```csharp
float thermalScore = 0f;
if (physiologyEnabled)
{
    thermalScore = ThermoregulationSystem.ScoreThermalComfort(phenotype, origin, tick);
    if (thermalScore >= 0.15f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekThermalComfort, -1, default, thermalScore));
    }
}
if (restBehaviorEnabled)
{
    float restScore = Urgency(needs.Rest, NeedsSystem.RestCapacity);
    if (restScore >= 0.15f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.Rest, -1, default, restScore));
    }
}
```

**`SimulationConfig.cs`**: add `restBehaviorEnabled` as the new LAST optional constructor parameter (after `multiThreatPerceptionEnabled`), assigned to a new `RestBehaviorEnabled { get; }` property placed immediately after `MultiThreatPerceptionEnabled`'s property — the exact same two-edit pattern (constructor parameter + body assignment + property) used for every flag added this session.

**`SimulationWorld.cs` `GetMovementTarget`** — current relevant section (`SimulationWorld.cs:577-581`):

```csharp
if (Config.PhysiologyEnabled && decision.Action == CreatureAction.SeekThermalComfort)
{
    return ThermoregulationSystem.FindNearbyComfortTarget(position, tick, Arena);
}
```

New — add a `Rest` case immediately after it:

```csharp
if (Config.PhysiologyEnabled && decision.Action == CreatureAction.SeekThermalComfort)
{
    return ThermoregulationSystem.FindNearbyComfortTarget(position, tick, Arena);
}

if (decision.Action == CreatureAction.Rest)
{
    return position;
}
```

**`SimulationWorld.cs` `TickNeeds`** — current (`SimulationWorld.cs:477-510`):

```csharp
private void TickNeeds()
{
    float deltaTime = 1f / Config.Schedule.NeedsHz;
    for (int index = 0; index < Creatures.Count; index++)
    {
        ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
        ref MovementState movement = ref Creatures.GetMovementRefAt(index);
        NeedsSystem.Tick(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, movement.DistanceSinceLastNeeds);
        if (Config.PhysiologyEnabled)
        {
            NeedsSystem.ApplyTemperatureStress(ref needs, Creatures.GetPhenotypeAt(index), TemperatureField.Sample(movement.Position, CurrentTick + 1), deltaTime);
        }
        movement.DistanceSinceLastNeeds = 0f;
        if (needs.Health <= 0f)
        {
            DeathCause cause = needs.Hydration <= 0f
                ? DeathCause.Dehydration
                : needs.Energy <= 0f ? DeathCause.Starvation : DeathCause.Health;
            RequestDeath(Creatures.GetIdAt(index), cause);
        }
        else if (Config.PhysiologyEnabled && needs.Age >= Creatures.GetPhenotypeAt(index).MaximumAgeSeconds)
        {
            RequestDeath(Creatures.GetIdAt(index), DeathCause.Age);
        }

        if (Config.CognitionEnabled)
        {
            MemorySystem.TickDecay(
                ref Creatures.GetMemoryRefAt(index),
                deltaTime,
                Creatures.GetPhenotypeAt(index).MemoryConfidenceDecayPerSecond);
        }
    }
}
```

New — only the `NeedsSystem.Tick` call line changes, appending the two new arguments:

```csharp
private void TickNeeds()
{
    float deltaTime = 1f / Config.Schedule.NeedsHz;
    for (int index = 0; index < Creatures.Count; index++)
    {
        ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
        ref MovementState movement = ref Creatures.GetMovementRefAt(index);
        bool isResting = Config.RestBehaviorEnabled && Creatures.GetDecisionAt(index).Action == CreatureAction.Rest;
        NeedsSystem.Tick(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, movement.DistanceSinceLastNeeds, Config.RestBehaviorEnabled, isResting);
        if (Config.PhysiologyEnabled)
        {
            NeedsSystem.ApplyTemperatureStress(ref needs, Creatures.GetPhenotypeAt(index), TemperatureField.Sample(movement.Position, CurrentTick + 1), deltaTime);
        }
        movement.DistanceSinceLastNeeds = 0f;
        if (needs.Health <= 0f)
        {
            DeathCause cause = needs.Hydration <= 0f
                ? DeathCause.Dehydration
                : needs.Energy <= 0f ? DeathCause.Starvation : DeathCause.Health;
            RequestDeath(Creatures.GetIdAt(index), cause);
        }
        else if (Config.PhysiologyEnabled && needs.Age >= Creatures.GetPhenotypeAt(index).MaximumAgeSeconds)
        {
            RequestDeath(Creatures.GetIdAt(index), DeathCause.Age);
        }

        if (Config.CognitionEnabled)
        {
            MemorySystem.TickDecay(
                ref Creatures.GetMemoryRefAt(index),
                deltaTime,
                Creatures.GetPhenotypeAt(index).MemoryConfidenceDecayPerSecond);
        }
    }
}
```

**`SimulationWorld.cs` `DecideIntentUtilityV1` call site** — search for `DecisionSystem.DecideIntentUtilityV1(` (`SimulationWorld.cs:726`). The current final two lines of that call are:

```csharp
                        otherCandidates,
                        Config.MultiThreatPerceptionEnabled);
```

New — append `Config.RestBehaviorEnabled` as the new trailing argument:

```csharp
                        otherCandidates,
                        Config.MultiThreatPerceptionEnabled,
                        Config.RestBehaviorEnabled);
```

**Behavior table:**

| Scenario | `restBehaviorEnabled` | `isResting` | Expected |
|---|---|---|---|
| 1. Resting | `true` | `true` | `needs.Rest` increases (capped at `100f`) |
| 2. Not resting, flag on | `true` | `false` | `needs.Rest` decreases (same formula as today) |
| 3. Flag off (default) | `false` | (either) | `needs.Rest` decreases exactly as today - byte-identical |
| 4. Exhausted, flag on | `true` | `false`, `needs.Rest == 0f` | `needs.Health` decreases by `3f * deltaTime` |
| 5. Exhausted, flag off | `false` | (either), `needs.Rest == 0f` | `needs.Health` unchanged by this mechanism (today's behavior) |

For the scoring test: `needs.Rest = 10f` (deeply tired, `RestCapacity = 100f` gives `Urgency = 0.9`, comfortably above the `0.15f` threshold), all other needs full (`CreatureNeeds.Full(phenotype)` then only lower `Rest`), no food/water/carcass/other candidates, `restBehaviorEnabled: true` — `DecideIntentUtilityV1` must return `CreatureAction.Rest`.

- [ ] **Step 1: Write the failing tests**

Append to `Assets/Tests/EditMode/BiologyTests.cs`, inside the `BiologyTests` class:

```csharp
[Test]
public void RestingRecoversTheRestNeedInsteadOfDrainingIt()
{
    Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
    CreatureNeeds needs = CreatureNeeds.Full(phenotype);
    needs.Rest = 50f;

    NeedsSystem.Tick(ref needs, phenotype, deltaTime: 1f, movementDistance: 0f, restBehaviorEnabled: true, isResting: true);

    Assert.That(needs.Rest, Is.EqualTo(55f).Within(0.0001f));
}

[Test]
public void RestNeedAtZeroDamagesHealthWhenRestBehaviorIsEnabled()
{
    Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
    CreatureNeeds needs = CreatureNeeds.Full(phenotype);
    needs.Rest = 0f;
    float healthBefore = needs.Health;

    NeedsSystem.Tick(ref needs, phenotype, deltaTime: 1f, movementDistance: 0f, restBehaviorEnabled: true, isResting: false);

    Assert.That(needs.Health, Is.EqualTo(healthBefore - 3f).Within(0.0001f));
}
```

Append to `Assets/Tests/EditMode/DecisionSystemTests.cs`, inside the `DecisionSystemTests` class:

```csharp
[Test]
public void IntentUtilitySelectsRestWhenRestNeedIsLowAndNothingElseIsUrgent()
{
    Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
    CreatureNeeds needs = CreatureNeeds.Full(phenotype);
    needs.Rest = 10f;
    var resources = new ResourceStore(initialCapacity: 0);

    CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
        needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
        carcass: default, memory: default, cognitionEnabled: false, threat: default,
        threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
        reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
        mateReproduction: default, reproductionEnabled: false, tick: 0,
        diagnostics: out _, restBehaviorEnabled: true);

    Assert.That(decision.Action, Is.EqualTo(CreatureAction.Rest));
}
```

Write the remaining two tests yourself:
1. A `CoreSimulationTests.cs`-style integration test proving a resting creature's position stays put across a `Step()`: build a `SimulationConfig` with `decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1`, `restBehaviorEnabled: true`, spawn one creature with `Genome.Neutral`, force `needs.Rest` low via `world.Creatures.GetNeedsRefAt(0).Rest = 10f` before stepping, record `movement.Position` before `Step()`, call `Step()` once, and assert the position after is equal to the position before (the creature stayed put rather than wandering) — only meaningful if the decision that tick actually resolves to `Rest`, so also assert `world.Creatures.GetDecisionAt(0).Action == CreatureAction.Rest` to make the test self-diagnosing if the setup doesn't actually trigger resting.
2. A flag-off hash-regression test, following the exact template from this session's prior tasks' `ExpectedDecisionStaggerDisabledHash`/`ExpectedMultiThreatPerceptionDisabledHash` (`CoreSimulationTests.cs`): derive `ExpectedRestBehaviorDisabledHash` by running the same `PredationVariation` scenario this session's hash tests use (`SimulationSchedule(60,60,30,10,10,10,5,1)`, `worldSeed: 99`, `initialPopulation: 2`, `founderProfile: FounderProfile.PredationVariation`, `restBehaviorEnabled` omitted since it doesn't exist at the pre-change commit, 50 `Step()` calls) against a throwaway worktree at the commit immediately before your changes to this file.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~RestingRecoversTheRestNeedInsteadOfDrainingIt|FullyQualifiedName~RestNeedAtZeroDamagesHealthWhenRestBehaviorIsEnabled|FullyQualifiedName~IntentUtilitySelectsRestWhenRestNeedIsLowAndNothingElseIsUrgent"`

Expected: FAIL to compile (`restBehaviorEnabled`/`isResting` parameters and `CreatureIntent.Rest` don't exist yet).

- [ ] **Step 3: Implement the changes**

Apply the exact changes shown above to `NeedsSystem.cs`, `DecisionSystem.cs`, `SimulationConfig.cs`, and `SimulationWorld.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the five new ones from this task. Total count should be 294 (289 existing + 5 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/NeedsSystem.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/BiologyTests.cs Assets/Tests/EditMode/DecisionSystemTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Make Rest a functional action: recovery, consequence, scoring, and staying put"
```

# Threat Avoidance in IntentUtilityV1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `DecisionSystem.ScoreRememberedResource` apply the same threat-avoidance penalty `SimulationWorld.TryScoreBestRememberedPlace` already applies under `DecisionPolicyVersion.Legacy`, so `DecisionPolicyVersion.IntentUtilityV1` (the policy the live `P`-keybind predator demo uses) also avoids remembered danger near remembered food/water.

**Architecture:** `ScoreRememberedResource` gains three new parameters (`threatPosition`, `threatConfidence`, `threatFalloffDistance`) and subtracts `ForagingEconomics.ThreatAvoidance` from its score before the final non-negative clamp, using the exact same single-slot-span construction `TryScoreBestRememberedPlace` already uses. `DecideIntentUtilityV1` (both overloads) gains a trailing `threatFalloffDistance` parameter and threads `memory.ThreatPosition`/`memory.ThreatConfidence` into both remembered-resource call sites. `SimulationWorld.cs`'s call site passes `Config.ThreatFalloffDistance` explicitly.

**Tech Stack:** C#, Unity, headless NUnit test harness (`tools/HeadlessTests`, plain `dotnet test`, .NET 8).

## Global Constraints

- `ForagingEconomics.ThreatAvoidance` formula and signature are NOT changed — reuse it as-is: `ThreatAvoidance(SimVector2 candidate, ReadOnlySpan<PlaceMemory> places, Phenotype phenotype, float falloffDistance)`.
- New parameters are added at the END of `ScoreRememberedResource`'s parameter list (before the `ref` params, which must stay last per existing convention) and at the END of both `DecideIntentUtilityV1` overloads' parameter lists (as an optional parameter, since C# requires optional parameters last and both overloads already end with `bool economicsEnabled = false`).
- `threatFalloffDistance` defaults to `SimulationConfig.DefaultThreatFalloffDistance` (the existing public const, value `10f`) — do not introduce a second, independent default value.
- No new config flag / no flag-gating: when `threatConfidence <= 0f` the new code path computes `avoidance = 0f` and the resulting `score` is bit-identical to today's formula — this is what keeps existing recorded/frozen scenarios (which never populate `MemoryState.ThreatPosition`/`ThreatConfidence`, defaulting to `0f`) unaffected.
- Do not touch `MemoryState`, `MemorySystem.RememberThreat`, or add multi-slot threat storage — that is explicitly out of scope for this plan (see spec's "Out of scope" section).

---

### Task 1: Thread threat avoidance into IntentUtilityV1's remembered-resource scoring

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs:272-296` (short `DecideIntentUtilityV1` overload)
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs:298-366` (full `DecideIntentUtilityV1` overload)
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs:488-522` (`ScoreRememberedResource`)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (the `DecideIntentUtilityV1` call site, around line 709 — search for `DecisionSystem.DecideIntentUtilityV1(` to find it exactly)
- Test: `Assets/Tests/EditMode/DecisionSystemTests.cs`

**Interfaces:**
- Consumes: `ForagingEconomics.ThreatAvoidance(SimVector2 candidate, ReadOnlySpan<PlaceMemory> places, Phenotype phenotype, float falloffDistance)` (existing, `Assets/Scripts/Simulation/Behavior/ForagingEconomics.cs:137-156`, unchanged). `PlaceMemory` struct (existing, `Assets/Scripts/Simulation/Core/SimulationTypes.cs:265`, has `Position` and `Confidence` fields, unchanged). `SimulationConfig.DefaultThreatFalloffDistance` (existing public const `10f`, `Assets/Scripts/Simulation/Core/SimulationConfig.cs:85`). `SimulationConfig.ThreatFalloffDistance` (existing instance property, same file, line 152).
- Produces: `ScoreRememberedResource` and both `DecideIntentUtilityV1` overloads gain new parameters as specified below — no other task in this plan depends on this, this is the only task.

This is the complete, exact code for every changed member. Do not deviate from these signatures.

`ScoreRememberedResource` — current signature (`DecisionSystem.cs:488-500`):

```csharp
private static void ScoreRememberedResource(
    CreatureIntent intent,
    CreatureNeeds needs,
    Genome genome,
    Phenotype phenotype,
    SimVector2 origin,
    SimVector2 location,
    float confidence,
    float age,
    float learnedValue,
    int experienceCount,
    ref DecisionCandidateBuffer candidates,
    ref float bestScore)
```

New signature — insert three parameters immediately before the two `ref` parameters:

```csharp
private static void ScoreRememberedResource(
    CreatureIntent intent,
    CreatureNeeds needs,
    Genome genome,
    Phenotype phenotype,
    SimVector2 origin,
    SimVector2 location,
    float confidence,
    float age,
    float learnedValue,
    int experienceCount,
    SimVector2 threatPosition,
    float threatConfidence,
    float threatFalloffDistance,
    ref DecisionCandidateBuffer candidates,
    ref float bestScore)
```

Current body (`DecisionSystem.cs:501-522`):

```csharp
{
    if (confidence <= 0f)
    {
        return;
    }

    bool seekingWater = intent == CreatureIntent.SeekWater;
    float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
    float current = seekingWater ? needs.Hydration : needs.Energy;
    float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
    float distance = SimVector2.Distance(origin, location);
    float expectedValue = KnownOutcomeOrCuriosity(learnedValue, experienceCount, phenotype.Exploration);
    float staleness = 1f / (1f + Math.Max(0f, age));
    float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
    float score = Math.Max(0f, (urgency * confidence * staleness * expectedValue) - travelBurden);
    if (score > bestScore)
    {
        bestScore = score;
    }

    candidates.TryAdd(new DecisionCandidate(intent, -1, default, score));
}
```

New body — replace the `float score = ...` line with an avoidance computation and an updated score line, everything else identical:

```csharp
{
    if (confidence <= 0f)
    {
        return;
    }

    bool seekingWater = intent == CreatureIntent.SeekWater;
    float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
    float current = seekingWater ? needs.Hydration : needs.Energy;
    float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
    float distance = SimVector2.Distance(origin, location);
    float expectedValue = KnownOutcomeOrCuriosity(learnedValue, experienceCount, phenotype.Exploration);
    float staleness = 1f / (1f + Math.Max(0f, age));
    float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
    float avoidance = 0f;
    if (threatConfidence > 0f)
    {
        Span<PlaceMemory> threatPlaces = stackalloc PlaceMemory[1];
        threatPlaces[0] = new PlaceMemory { Position = threatPosition, Confidence = threatConfidence };
        avoidance = ForagingEconomics.ThreatAvoidance(location, threatPlaces, phenotype, threatFalloffDistance);
    }
    float score = Math.Max(0f, (urgency * confidence * staleness * expectedValue) - travelBurden - avoidance);
    if (score > bestScore)
    {
        bestScore = score;
    }

    candidates.TryAdd(new DecisionCandidate(intent, -1, default, score));
}
```

Short `DecideIntentUtilityV1` overload — current (`DecisionSystem.cs:272-296`):

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
    bool economicsEnabled = false)
{
    return DecideIntentUtilityV1(
        needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
        cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
        default, default, default, default, default, false, tick, out diagnostics, economicsEnabled);
}
```

New — add `threatFalloffDistance` as the new last parameter, forward it in the call:

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
    float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance)
{
    return DecideIntentUtilityV1(
        needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
        cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
        default, default, default, default, default, false, tick, out diagnostics, economicsEnabled,
        threatFalloffDistance);
}
```

Full `DecideIntentUtilityV1` overload — current (`DecisionSystem.cs:298-366`):

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
    bool economicsEnabled = false)
{
    var candidates = new DecisionCandidateBuffer();
    float bestFoodScore = -1f;
    float bestWaterScore = -1f;

    ScoreResourceCandidates(CreatureIntent.SeekFood, needs, genome, phenotype, resources, foodCandidates, threat, threatIntensity, ref candidates, ref bestFoodScore);
    ScoreResourceCandidates(CreatureIntent.SeekWater, needs, genome, phenotype, resources, waterCandidates, threat, threatIntensity, ref candidates, ref bestWaterScore);
    if (cognitionEnabled)
    {
        ScoreRememberedResource(CreatureIntent.SeekFood, needs, genome, phenotype, origin, memory.FoodPosition, memory.FoodConfidence, memory.FoodAge, memory.FoodOutcomeValue, memory.FoodExperienceCount, ref candidates, ref bestFoodScore);
        ScoreRememberedResource(CreatureIntent.SeekWater, needs, genome, phenotype, origin, memory.WaterPosition, memory.WaterConfidence, memory.WaterAge, memory.WaterOutcomeValue, memory.WaterExperienceCount, ref candidates, ref bestWaterScore);
    }
    float carcassScore = 0f;
    float fleeScore = 0f;
    float huntScore = 0f;
    if (predationEnabled)
    {
        ScoreCarcass(needs, phenotype, resources, carcass, ref candidates, out carcassScore);
        ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, out fleeScore, out huntScore);
    }
    float thermalScore = 0f;
    if (physiologyEnabled)
    {
        thermalScore = ThermoregulationSystem.ScoreThermalComfort(phenotype, origin, tick);
        if (thermalScore >= 0.15f)
        {
            candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekThermalComfort, -1, default, thermalScore));
        }
    }
    if (reproductionEnabled)
    {
        ScoreMate(needs, phenotype, reproduction, mate, mateNeeds, matePhenotype, mateReproduction, ref candidates);
    }
    diagnostics = new DecisionDiagnostics(bestFoodScore, bestWaterScore, foodCandidates.Count > 0, waterCandidates.Count > 0)
        .WithPredationScores(fleeScore, huntScore)
        .WithCarcassScore(carcassScore)
        .WithThermalScore(thermalScore);
    if (!candidates.TryGetBest(out DecisionCandidate best) || best.Score < MinimumUrgencyToSeekResource)
    {
        return new CreatureDecision(CreatureAction.Wander, -1, 0f);
    }

    return ToDecision(best);
}
```

New — add `threatFalloffDistance` as the new last parameter (after `economicsEnabled`), and pass `memory.ThreatPosition`, `memory.ThreatConfidence`, `threatFalloffDistance` into both `ScoreRememberedResource` calls:

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
    float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance)
{
    var candidates = new DecisionCandidateBuffer();
    float bestFoodScore = -1f;
    float bestWaterScore = -1f;

    ScoreResourceCandidates(CreatureIntent.SeekFood, needs, genome, phenotype, resources, foodCandidates, threat, threatIntensity, ref candidates, ref bestFoodScore);
    ScoreResourceCandidates(CreatureIntent.SeekWater, needs, genome, phenotype, resources, waterCandidates, threat, threatIntensity, ref candidates, ref bestWaterScore);
    if (cognitionEnabled)
    {
        ScoreRememberedResource(CreatureIntent.SeekFood, needs, genome, phenotype, origin, memory.FoodPosition, memory.FoodConfidence, memory.FoodAge, memory.FoodOutcomeValue, memory.FoodExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestFoodScore);
        ScoreRememberedResource(CreatureIntent.SeekWater, needs, genome, phenotype, origin, memory.WaterPosition, memory.WaterConfidence, memory.WaterAge, memory.WaterOutcomeValue, memory.WaterExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestWaterScore);
    }
    float carcassScore = 0f;
    float fleeScore = 0f;
    float huntScore = 0f;
    if (predationEnabled)
    {
        ScoreCarcass(needs, phenotype, resources, carcass, ref candidates, out carcassScore);
        ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, out fleeScore, out huntScore);
    }
    float thermalScore = 0f;
    if (physiologyEnabled)
    {
        thermalScore = ThermoregulationSystem.ScoreThermalComfort(phenotype, origin, tick);
        if (thermalScore >= 0.15f)
        {
            candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekThermalComfort, -1, default, thermalScore));
        }
    }
    if (reproductionEnabled)
    {
        ScoreMate(needs, phenotype, reproduction, mate, mateNeeds, matePhenotype, mateReproduction, ref candidates);
    }
    diagnostics = new DecisionDiagnostics(bestFoodScore, bestWaterScore, foodCandidates.Count > 0, waterCandidates.Count > 0)
        .WithPredationScores(fleeScore, huntScore)
        .WithCarcassScore(carcassScore)
        .WithThermalScore(thermalScore);
    if (!candidates.TryGetBest(out DecisionCandidate best) || best.Score < MinimumUrgencyToSeekResource)
    {
        return new CreatureDecision(CreatureAction.Wander, -1, 0f);
    }

    return ToDecision(best);
}
```

`SimulationWorld.cs` call site: search for `DecisionSystem.DecideIntentUtilityV1(` — it is the only call site outside tests calling the full (22+ param) overload. Add `Config.ThreatFalloffDistance` as the new trailing argument, matching how `Config.PredationEconomicsEnabled` was added as the trailing `economicsEnabled` argument in the prior session's follow-up (i.e. it will now be the argument immediately after whatever currently passes `Config.PredationEconomicsEnabled`). Do not reorder any existing argument — only append the new one at the end.

**Behavior table** (all cases use `Genome.Neutral` — every gene `0.5f` — and a phenotype built via the `MakePhenotype` reflection helper already present in `DecisionSystemTests.cs`, called with `attackPower: 1f, defense: 1f, maneuverability: 1f` since predation fields are irrelevant to this scoring path; all other named values keep `MakePhenotype`'s defaults: `energyCapacity: 100f`, `hydrationCapacity: 100f` (fixed in the helper), `fearResponse: 0.5f` (fixed), `exploration: 0.5f` (fixed), `bodyMass: 1f`, `maximumSpeed: 2f`, `basalEnergyCostMultiplier: 1f`, `digestionRate: 1f`, `waterLossMultiplier: 1f`). Scenario: `intent = SeekFood`, `needs.Energy = 0f` (full deficit, so `Urgency = 1f` regardless of exponent), `origin = (0,0)`, remembered food `location = (10,0)` (`distance = 10`), `confidence = 1f`, `age = 0f` (`staleness = 1f`), `learnedValue = 0f`, `experienceCount = 0` (curiosity path: `expectedValue = 0.5 + 0.5*0.5 = 0.75`). With these fixed values, `travelBurden` works out to exactly `0.0859375f` (`travelTime = 5`, `energyCost = 10`, `hydrationCost = 3.75`, `EstimateTravelBurden = 0.06875`, `travelBurden = 1.25 * 0.06875`), so the pre-avoidance score is `0.75 - 0.0859375 = 0.6640625f`.

| Row | `threatConfidence` | `threatPosition` | `threatFalloffDistance` | Expected `avoidance` | Expected `score` |
|---|---|---|---|---|---|
| 1. No threat memory | `0f` | `(0,0)` (irrelevant, gated off) | `10f` | `0f` | `0.6640625f` (unchanged from today's formula) |
| 2. Threat memory at the remembered food location | `1f` | `(10,0)` (same as `location`, distance `0`) | `10f` (`SimulationConfig.DefaultThreatFalloffDistance`) | `phenotype.FearResponse(0.5) * threatConfidence(1) * falloff(1) = 0.5f` | `0.6640625 - 0.5 = 0.1640625f` |

- [ ] **Step 1: Write the failing tests**

Append to `Assets/Tests/EditMode/DecisionSystemTests.cs`, inside the `DecisionSystemTests` class (after the existing `IntentUtilityDiagnosticsReportsNonZeroHuntScoreForAFavorableMatchup` test):

```csharp
// Behavior table row 1: no remembered threat -> remembered-food score is
// exactly today's pre-existing formula (avoidance term is 0).
[Test]
public void RememberedFoodScoreIsUnaffectedWhenNoThreatIsRemembered()
{
    Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
    CreatureNeeds needs = CreatureNeeds.Full(phenotype);
    needs.Energy = 0f;
    var resources = new ResourceStore(initialCapacity: 0);
    var memory = new MemoryState
    {
        FoodPosition = new SimVector2(10f, 0f),
        FoodConfidence = 1f,
        FoodAge = 0f,
        FoodOutcomeValue = 0f,
        FoodExperienceCount = 0,
    };

    DecisionSystem.DecideIntentUtilityV1(
        needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
        carcass: default, memory: memory, cognitionEnabled: true, threat: default,
        threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
        reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
        mateReproduction: default, reproductionEnabled: false, tick: 0,
        diagnostics: out DecisionDiagnostics diagnostics);

    Assert.That(diagnostics.FoodScore, Is.EqualTo(0.6640625f).Within(0.0001f));
}

// Behavior table row 2: a remembered threat sitting at the remembered food's
// exact location applies the same avoidance penalty
// TryScoreBestRememberedPlace already applies under Legacy, lowering the
// remembered-food score.
[Test]
public void RememberedFoodScoreIsLoweredByARememberedThreatAtTheSameLocation()
{
    Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
    CreatureNeeds needs = CreatureNeeds.Full(phenotype);
    needs.Energy = 0f;
    var resources = new ResourceStore(initialCapacity: 0);
    var memory = new MemoryState
    {
        FoodPosition = new SimVector2(10f, 0f),
        FoodConfidence = 1f,
        FoodAge = 0f,
        FoodOutcomeValue = 0f,
        FoodExperienceCount = 0,
        ThreatPosition = new SimVector2(10f, 0f),
        ThreatConfidence = 1f,
    };

    DecisionSystem.DecideIntentUtilityV1(
        needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
        carcass: default, memory: memory, cognitionEnabled: true, threat: default,
        threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
        reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
        mateReproduction: default, reproductionEnabled: false, tick: 0,
        diagnostics: out DecisionDiagnostics diagnostics,
        threatFalloffDistance: SimulationConfig.DefaultThreatFalloffDistance);

    Assert.That(diagnostics.FoodScore, Is.EqualTo(0.1640625f).Within(0.0001f));
}
```

Write the remaining behavior-table coverage yourself following this template: at minimum, confirm row 1 and row 2 as specified above. If `MemoryState`'s fields are `init`-only or otherwise cannot be set via object initializer, construct it however the existing `MemoryState` usages in this same test file or `SimulationWorld.cs` do (check `MemoryState`'s declaration in `Assets/Scripts/Simulation/Core/SimulationTypes.cs` if the object-initializer syntax above does not compile) — this is a mechanical adjustment, not a design change.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~RememberedFoodScoreIsUnaffectedWhenNoThreatIsRemembered|FullyQualifiedName~RememberedFoodScoreIsLoweredByARememberedThreatAtTheSameLocation"`

Expected: both FAIL to compile (new `MemoryState.ThreatPosition`/`ThreatConfidence` usage is fine since those fields already exist — the failure should be that `DecideIntentUtilityV1` doesn't yet accept `threatFalloffDistance`, or once that's added mechanically to make it compile, the second test FAILs on the assertion because `ScoreRememberedResource` doesn't yet apply the avoidance term). If it's more practical to write the signature changes first and then the tests, that's fine — the requirement is: run the tests before implementing the avoidance-subtraction logic in `ScoreRememberedResource`'s body, and confirm test 2 fails with `diagnostics.FoodScore` still at `0.6640625f` (proving the test actually exercises the new code path once it's added).

- [ ] **Step 3: Implement the changes**

Apply the exact signature and body changes shown above to `ScoreRememberedResource`, both `DecideIntentUtilityV1` overloads, and the `SimulationWorld.cs` call site.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the two new ones. Total count should be 283 (281 existing + 2 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionSystemTests.cs
git commit -m "Apply remembered-threat avoidance to IntentUtilityV1's remembered-resource scoring"
```

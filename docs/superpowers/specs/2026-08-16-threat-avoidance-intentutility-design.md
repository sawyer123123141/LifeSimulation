# Threat Avoidance in IntentUtilityV1 - Design

## Problem

B-6 (`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`)
states remembered threats are never read by any decision path. That claim is
now stale: `ForagingEconomics.ThreatAvoidance` exists and is applied inside
`SimulationWorld.TryScoreBestRememberedPlace`, penalizing remembered
food/water places near a remembered threat, scaled by `Phenotype.FearResponse`
(derived from `Genome.Fear`).

However, that call site is gated to
`Config.CognitionEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy`
(`SimulationWorld.cs:791`). The live `P`-keybind predator demo runs
`DecisionPolicyVersion.IntentUtilityV1`, which has its own separate
remembered-resource scorer, `DecisionSystem.ScoreRememberedResource`
(`DecisionSystem.cs:488-522`), called from `DecideIntentUtilityV1`
(`DecisionSystem.cs:332-333`) when `cognitionEnabled` is true. This scorer
never applies any threat term, so under `IntentUtilityV1`, remembered threats
have zero effect on remembered-resource choice. This is the third instance
this session of features wired into `Legacy` and forgotten under
`IntentUtilityV1` (predation formula, predation diagnostics, now this).

Out of scope for this task: upgrading `MemoryState.ThreatPosition`/
`ThreatConfidence` from single-slot to true multi-slot storage (matching the
multi-slot `PlaceMemory` array resources already use). That is a separate,
larger architectural change tracked as a future follow-up, not bundled here.

## Fix

Apply the same `ForagingEconomics.ThreatAvoidance` formula
`TryScoreBestRememberedPlace` already uses, inside
`DecisionSystem.ScoreRememberedResource`, so `IntentUtilityV1` gets the same
threat-avoidance behavior `Legacy` already has for remembered places.

### `ScoreRememberedResource` - new signature

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

Body change: after computing `score` (line 515 today,
`Math.Max(0f, (urgency * confidence * staleness * expectedValue) - travelBurden)`),
subtract an avoidance term before the final clamp:

```csharp
float avoidance = 0f;
if (threatConfidence > 0f)
{
    Span<PlaceMemory> threatPlaces = stackalloc PlaceMemory[1];
    threatPlaces[0] = new PlaceMemory { Position = threatPosition, Confidence = threatConfidence };
    avoidance = ForagingEconomics.ThreatAvoidance(location, threatPlaces, phenotype, threatFalloffDistance);
}
float score = Math.Max(0f, (urgency * confidence * staleness * expectedValue) - travelBurden - avoidance);
```

This exactly mirrors `TryScoreBestRememberedPlace`'s existing pattern
(`SimulationWorld.cs:1263-1276`): same single-slot span construction, same
`ForagingEconomics.ThreatAvoidance` call, same source fields
(`memory.ThreatPosition`/`memory.ThreatConfidence`).

### `DecideIntentUtilityV1` - both overloads

Both overloads (`DecisionSystem.cs:272-296` short form,
`DecisionSystem.cs:298-322` full form) gain one new trailing parameter:

```csharp
float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance
```

Placed after `economicsEnabled` (last position, since it's optional and C#
requires optional parameters last). The short overload forwards it unchanged
to the full overload. `SimulationConfig.DefaultThreatFalloffDistance` is the
existing public const (`10f`) already used as `SimulationConfig`'s own
constructor default - reusing it here means a caller that never mentions
threat falloff gets identical behavior to today's `SimulationConfig` default,
not a second, potentially-divergent default value.

Inside the full overload's body, the two `ScoreRememberedResource` calls
(`DecisionSystem.cs:332-333`) pass the new arguments:

```csharp
ScoreRememberedResource(CreatureIntent.SeekFood, needs, genome, phenotype, origin, memory.FoodPosition, memory.FoodConfidence, memory.FoodAge, memory.FoodOutcomeValue, memory.FoodExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestFoodScore);
ScoreRememberedResource(CreatureIntent.SeekWater, needs, genome, phenotype, origin, memory.WaterPosition, memory.WaterConfidence, memory.WaterAge, memory.WaterOutcomeValue, memory.WaterExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestWaterScore);
```

### Call site update

`SimulationWorld.cs`'s `DecideIntentUtilityV1` call site (around line 709)
passes `Config.ThreatFalloffDistance` explicitly as the new trailing
argument, rather than relying on the default - matching how
`Config.PredationEconomicsEnabled` was wired in the prior B-5 follow-up.

## Hash safety

This changes `IntentUtilityV1` decision output (which candidate/score wins)
whenever `cognitionEnabled && memory.ThreatConfidence > 0f` - i.e. only once
a creature has actually observed a threat and still remembers it. No
flag-gating: `ThreatAvoidance` is already unconditional under `Legacy`
wherever `TryScoreBestRememberedPlace` runs (no config flag exists for it
today), and any existing recorded/frozen hash scenario that never triggers
threat memory (`memory.ThreatConfidence == 0f` throughout, e.g. no predator
present) is provably unaffected - `threatConfidence > 0f` gates the new
`avoidance` computation entirely, so `avoidance` stays exactly `0f` and
`score` is bit-identical to today's formula. A hash-regression test should
still confirm this for a no-threat scenario, mirroring
`CoreSimulationTests.cs`'s existing pattern.

## Testing

Extend `DecisionSystemTests.cs`:
1. Remembered food/water candidate's score is lower (or removed, if driven
   below `MinimumUrgencyToSeekResource`) when `memory.ThreatConfidence > 0f`
   and the remembered threat is near the remembered resource, vs. an
   otherwise-identical case with `memory.ThreatConfidence == 0f`.
2. A no-threat-memory hash-regression style check (or reuse of an existing
   `CoreSimulationTests.cs` scenario) confirming `IntentUtilityV1` decision
   output is unchanged when `memory.ThreatConfidence == 0f` throughout a run.

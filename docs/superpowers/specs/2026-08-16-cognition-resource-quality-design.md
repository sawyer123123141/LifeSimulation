# Cognition-Mode Resource Quality (B-3 remainder) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
B-3: "resource quality is absent from decision scoring." A `2026-08-15
update` in that spec notes the non-cognition path
(`DecisionSystem.ResourceUtility`) was already fixed to weigh
`ResourceState.Amount` against the creature's missing need. The
cognition-mode path was not: `DecisionSystem.DecideFromLearnedOutcomes`
(`DecisionSystem.cs:809-830`, the Legacy-policy path used when
`Config.CognitionEnabled` is set, called from
`SimulationWorld.cs:862`) scores `foodScore`/`waterScore` as `Urgency *
Availability(distance) * KnownOutcomeOrCuriosity(learnedValue)` - the
observed resource's remaining `Amount` never enters the score. A
creature under cognition mode chooses the nearest food, never the
richest, exactly the defect B-3 describes, just confined to this one
decision path now.

`DecideFromLearnedOutcomes`'s signature only takes `ResourceObservation
food`/`water` (index + distance, no `ResourceState`) - it structurally
cannot read `Amount` without a new parameter. `SimulationWorld.TickDecisions`
already has `Resources` (the `ResourceStore`) in scope at the call site.

Scoped through discussion with the user: the fix multiplies a new
Amount-weighted term alongside the existing remembered-quality term
(`foodValue`/`waterValue`), not in place of it - both "I remember food is
usually good here" and "this specific visible patch is well-stocked right
now" should matter together. Replacing the learned-value term would
silently delete an existing, working signal - a real behavior regression,
not what B-3 asks for.

## Fix

### Shared `ComputeNeedGain` helper

`ResourceUtility` (`DecisionSystem.cs:737-757`) already computes exactly
the Amount-weighted term this needs, inline:

```csharp
float perUnitGain = seekingWater ? 20f : 20f * phenotype.FoodYield * resource.NutritionMultiplier;
float needGain = Math.Min(1f, (resource.Amount * perUnitGain) / missing);
```

Extract this into a shared private static helper so both call sites use
identical math (DRY - no duplicated formula):

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

`ResourceUtility` is refactored to call `ComputeNeedGain(seekingWater,
needs, phenotype, resource)` in place of its inline calculation - a
behavior-neutral refactor (same formula, same inputs, same output),
already covered by the existing hash-regression tests for that path (no
new test needed for the refactor itself, since it changes no observable
value).

### `DecideFromLearnedOutcomes` - new parameters and scoring

Two new parameters: `ResourceStore resources` (required, no default -
every call site already has one in scope) and `bool
learnedResourceQualityEnabled = false`.

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

When `learnedResourceQualityEnabled` is `false` (default),
`foodNeedGain`/`waterNeedGain` are hardcoded `1f` - not computed and
discarded, genuinely never evaluated - so `foodScore`/`waterScore`
multiply by exactly `1f`, identical to today's formula. This guarantees
hash safety without relying on `ComputeNeedGain` happening to return `1f`
for any particular resource state.

### Call sites

`TickDecisions`'s call to `DecideFromLearnedOutcomes`
(`SimulationWorld.cs:862`) passes `Resources` and
`Config.LearnedResourceQualityEnabled` as the two new arguments.

A second, pre-existing call site exists in
`Assets/Tests/EditMode/SpatialBehaviorTests.cs:225`
(`CognitiveResourceDecisionUsesLearnedOutcomesRatherThanAssumingWaterIsAlwaysBest`).
Since `resources` has no default value, this test must also be updated to
pass a `ResourceStore` (e.g. `new ResourceStore(initialCapacity: 0)` - the
test's `ResourceObservation`s reference indices that are never actually
looked up, since `learnedResourceQualityEnabled` stays at its default
`false` there and `foodNeedGain`/`waterNeedGain` are hardcoded `1f`
without calling `resources.GetAt(...)` in that branch, so an empty
`ResourceStore` is safe).

### `SimulationConfig.LearnedResourceQualityEnabled` - new flag

New bool, default `false`, added as the new last optional constructor
parameter + `{ get; }` property - identical two-edit pattern used for
every flag this program.

## Scope boundary

- Only `DecideFromLearnedOutcomes` (Legacy + `CognitionEnabled`) changes
  behavior. `IntentUtilityV1`'s resource scoring already reads `Amount`
  via `ResourceUtility` - untouched except for the internal
  `ComputeNeedGain` refactor, which is behavior-neutral.
- No change to `PreferRememberedResource` or `ScoreRememberedResource`
  (the memory-based "go to a remembered spot with no visible resource"
  paths) - those don't observe a live `ResourceState` at all, so there is
  no `Amount` to weigh; B-3's fix is specifically about *visible*
  resources.
- No change to the learned-value (`foodValue`/`waterValue`) computation
  itself, or to how memory is written/decayed.

## Hash safety

When `SimulationConfig.LearnedResourceQualityEnabled` is `false`
(default), `foodNeedGain`/`waterNeedGain` are always exactly `1f`,
making `DecideFromLearnedOutcomes`'s output identical to today's. The
`ResourceUtility` refactor to call `ComputeNeedGain` produces the same
formula with the same inputs, so its output is unchanged regardless of
the new flag. Both proven by a hash-regression test, same methodology as
every prior task this session.

## Testing

1. `ComputeNeedGain`: unit tests confirming it matches `ResourceUtility`'s
   pre-refactor inline formula for a few representative
   (needs, phenotype, resource) combinations - a regression guard for the
   extraction itself.
2. `DecideFromLearnedOutcomes` with `learnedResourceQualityEnabled: true`:
   a well-stocked food patch (high `Amount`) scores higher than an
   otherwise-identical, nearly-depleted patch (low `Amount`) at the same
   distance with the same remembered value.
3. `DecideFromLearnedOutcomes` with `learnedResourceQualityEnabled: true`:
   a food patch with a strong remembered-value history still scores
   higher than a patch with no history, even when both have identical
   `Amount` - proves the multiply-alongside design, not a replacement.
4. Integration: a creature under `CognitionEnabled` with
   `LearnedResourceQualityEnabled: true` prefers a farther-but-richer
   resource over a closer-but-nearly-depleted one, when the richer one's
   score still clears the nearer one's after the Amount weighting.
5. Hash-regression test with the flag `false` (default), covering both
   the flag's own gate and the `ResourceUtility` refactor.

# Rest Behavior (C-4) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-4: `CreatureAction.Rest` is declared in the action enum and produced by no
scoring path. Investigation this session found the gap is larger than the
doc's one-line summary: `CreatureNeeds.Rest` (`NeedsSystem.cs`) decays every
tick via `CognitionRestCostMultiplier`, but nothing ever recovers it,
nothing penalizes it reaching `0` (unlike `Energy`/`Hydration`, which cause
health damage at `0`), and `GetMovementTarget` (`SimulationWorld.cs:528`)
has no case for `CreatureAction.Rest` at all — if it were somehow selected
today, the creature would just wander randomly instead of resting. `Rest`
is a fully dead stat plus a fully dead action, not just a missing decision
branch.

## Fix

Full loop, flag-gated behind `SimulationConfig.RestBehaviorEnabled`
(default `false`), following this session's established convention.

### 1. Recovery and consequence - `NeedsSystem.Tick`

Add two new trailing parameters: `bool restBehaviorEnabled = false, bool
isResting = false`. The existing unconditional drain line:

```csharp
needs.Rest = Math.Max(0f, needs.Rest - (0.1f * phenotype.CognitionRestCostMultiplier * deltaTime));
```

becomes conditional:

```csharp
if (restBehaviorEnabled && isResting)
{
    needs.Rest = Math.Min(RestCapacity, needs.Rest + (RestRecoveryPerSecond * deltaTime));
}
else
{
    needs.Rest = Math.Max(0f, needs.Rest - (0.1f * phenotype.CognitionRestCostMultiplier * deltaTime));
}
```

`RestCapacity` (new `public const float = 100f`, matching
`CreatureNeeds.Full`'s existing hardcoded initial value) and
`RestRecoveryPerSecond` (new `private const float = 5f`) are added to
`NeedsSystem`. A new health-at-zero consequence, mirroring the existing
`Energy`/`Hydration` pattern, is added after those existing checks:

```csharp
if (restBehaviorEnabled && needs.Rest <= 0f)
{
    needs.Health = Math.Max(0f, needs.Health - (RestExhaustionHealthCostPerSecond * deltaTime));
}
```

`RestExhaustionHealthCostPerSecond` (new `private const float = 3f` -
deliberately below both `Energy`'s `4f` and `Hydration`'s `5f` cost-at-zero
rates, since `Rest` is the softer, more passively recoverable need: it
drains slowly and recovers just by standing still, unlike `Energy`/
`Hydration` which require actively finding and consuming a resource).

When `restBehaviorEnabled` is `false` (the default for every existing
caller and test), both new parameters are `false`, so the `if
(restBehaviorEnabled && isResting)` branch never taken (falls to the
unchanged `else`, byte-identical to today's line) and the health-penalty
block never runs — fully hash-safe.

### 2. Scoring - `DecisionSystem`

Add `CreatureIntent.Rest` to the `CreatureIntent` enum, appended after
`SeekMate` (preserves every existing enum value's numeric ordinal). Add a
`case CreatureIntent.Rest: action = CreatureAction.Rest; break;` to
`ToDecision`'s switch.

`DecideIntentUtilityV1` (both overloads) gains one new trailing parameter,
`bool restBehaviorEnabled = false`. Inside the full overload, alongside the
existing `physiologyEnabled`/thermal block:

```csharp
float restScore = 0f;
if (restBehaviorEnabled)
{
    restScore = Urgency(needs.Rest, NeedsSystem.RestCapacity);
    if (restScore >= 0.15f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.Rest, -1, default, restScore));
    }
}
```

(`0.15f` matches `ScoreThermalComfort`'s existing add-threshold — the
closest sibling "passive homeostatic need" score in the codebase.)

### 3. Movement - `SimulationWorld.GetMovementTarget`

Add a case so a resting creature stays where it is instead of falling
through to the random-exploration default:

```csharp
if (decision.Action == CreatureAction.Rest)
{
    return position;
}
```

This is harmless and unreachable when the flag is off (no scoring path
ever selects `Rest` in that case), so it needs no flag guard itself.

### 4. Wiring - `SimulationWorld.TickNeeds` and the `DecideIntentUtilityV1` call site

`TickNeeds`'s call to `NeedsSystem.Tick` passes
`Config.RestBehaviorEnabled` and `Config.RestBehaviorEnabled &&
Creatures.GetDecisionAt(index).Action == CreatureAction.Rest` as the two
new trailing arguments. The `DecideIntentUtilityV1` call site
(`SimulationWorld.cs`, `TickDecisions`) appends `Config.RestBehaviorEnabled`
as the new trailing argument, after the `MultiThreatPerceptionEnabled`
argument added by C-3.

`SimulationConfig.RestBehaviorEnabled` (bool, default `false`) is added the
same way as every flag before it this session.

## Hash safety

Same proof pattern as B-5/B-6/B-8/C-3: every new branch is gated by
`restBehaviorEnabled`/`isResting`, both `false` by default and for every
existing test/scenario. With the flag off, `NeedsSystem.Tick`'s output is
line-for-line identical to today, the health-penalty block never executes,
and `DecideIntentUtilityV1` never adds a `Rest` candidate — so
`decision.Action` can never become `CreatureAction.Rest` and
`GetMovementTarget`'s new case is unreachable. Proven by a hash-regression
test, same methodology as prior tasks.

## Testing

1. Recovery: with `restBehaviorEnabled: true` and `isResting: true`,
   `NeedsSystem.Tick` increases `needs.Rest` (capped at `RestCapacity`)
   instead of decreasing it.
2. Consequence: with `restBehaviorEnabled: true`, `needs.Rest` at `0`, and
   `isResting: false`, `needs.Health` decreases.
3. Scoring: with `restBehaviorEnabled: true` and `needs.Rest` low, a
   `DecideIntentUtilityV1` call produces `CreatureAction.Rest` when no
   other need is more urgent.
4. Movement: `GetMovementTarget` is private, so this is verified indirectly
   through `SimulationWorld` - construct a scenario where a creature is
   resting (`decision.Action == CreatureAction.Rest`, achievable by forcing
   low `needs.Rest` with `restBehaviorEnabled: true` and no more urgent
   need), run one `Step()`, and assert `movement.Position` is unchanged
   from before the step (proving the creature stayed put rather than
   wandering to a random exploration target).
5. Hash-regression test with the flag `false` (default), mirroring the
   established pattern from this session's prior tasks.

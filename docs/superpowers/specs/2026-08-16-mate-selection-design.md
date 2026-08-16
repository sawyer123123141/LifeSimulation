# Mate Selection (C-2) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-2: reproduction is a spatial coincidence, not a behaviour. Under the
`IntentUtilityV1` decision policy, `DecisionSystem.ScoreMate` already scores
`CreatureAction.SeekMate` toward a specific target - the single-nearest-other
creature, gated by `ReproductionSystem.CanSeekMate` on both sides - and
`SimulationWorld`'s movement resolution already steers the creature toward
that `TargetCreatureId` (`SimulationWorld.cs:557`). But
`ReproductionSystem.Step` (`Assets/Scripts/Simulation/Biology/ReproductionSystem.cs:42-95`)
never looks at either creature's decision. It independently rebuilds its own
spatial grid every call and pairs any two `IsReady` + unmatched creatures
within `MateDistance = 2f`, via `FindNearestReadyMate`. A creature that is
`Flee`ing or `Wander`ing and happens to drift within range of another ready
creature is bred exactly as readily as one that chose to approach that
specific partner. `SeekMate` currently has zero causal effect on who mates.

A prior, stale, unexecuted spec/plan pair
(`2026-08-14-mating-behaviour-design.md` / `-plan.md`) proposed a much larger
fix: a new `MatingSystem`, a `MateChoosiness` gene, genetic-distance-based
mate quality, and a courtship state machine with a watchable delay. Discussed
with the user and scoped down: none of that is required to resolve the
stated defect (mate choice having zero causal effect), and the genetics/
courtship pieces were explicitly speculative in the old spec ("add only if
choice remains behaviorally impoverished"), unverified at any scale. This
spec implements the minimal fix that actually connects choice to outcome,
gated behind a flag, matching every other defect fix this program.

Also discussed and rejected: requiring **mutual** `SeekMate` (both creatures
independently choosing each other as their nearest-creature target in the
same tick). Real mate-seeking is rarely symmetric - pursuit is one-sided,
receptivity is the gate on the other side, not reciprocal pursuit. Mutual
nearest-neighbor pairing would also measurably cut the birth rate in
populations of 3+ nearby ready creatures (A's nearest neighbor might be B,
while B's nearest neighbor is C - neither mutual, nobody pairs). The design
below uses a one-sided requirement instead.

## Fix

### `ReproductionSystem` gains a `mateSelectionEnabled` flag

New constructor parameter, stored as a field (`_mateSelectionEnabled`),
following the same pattern as the existing `_physiologyEnabled` field:

```csharp
public ReproductionSystem(CreatureStore creatures, ArenaBounds arena, int initialCapacity, bool physiologyEnabled, bool mateSelectionEnabled = false)
```

### `Step` - pairing search branches on the flag

Today, `Step`'s per-candidate loop (`ReproductionSystem.cs:62-92`) calls
`FindNearestReadyMate(firstIndex, candidateCount)` unconditionally to find
`secondIndex`. This becomes:

```csharp
int secondIndex = _mateSelectionEnabled
    ? FindSeekMateTarget(firstIndex, candidateCount)
    : FindNearestReadyMate(firstIndex, candidateCount);
```

`FindNearestReadyMate` is untouched - still used when the flag is `false`,
preserving today's behavior exactly.

### `FindSeekMateTarget` - new one-sided lookup

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
```

`firstIndex` must be the one currently choosing `SeekMate` toward
`secondIndex` - a real, active decision from the prior `TickDecisions` call,
not a spatial re-derivation. `secondIndex` only needs to satisfy the existing
`IsReady` gate (energy/hydration/health/age/cooldown) - the same receptivity
check `Step` already applies to every candidate today. `secondIndex` does
**not** need to be simultaneously choosing `SeekMate` back toward
`firstIndex` - one-sided pursuit is sufficient, matching how courtship works
in most animals (pursuit is directional; readiness is the receiving side's
gate).

Because `_creatures.GetDecisionAt` is a per-creature O(1) lookup, no grid
lookup is needed for `secondIndex`'s side of the check - unlike
`FindNearestReadyMate`, which scans the reproduction grid's cells.
`RebuildGrid` still runs every `Step` call regardless of the flag, since
`Grid` is a public property other code may still rely on for other purposes
(unchanged either way - out of scope to touch).

### `SimulationWorld` - constructor call site and flag threading

`SimulationWorld`'s single `new ReproductionSystem(...)` call site
(`SimulationWorld.cs:71`) passes `Config.MateSelectionEnabled` as the fifth
argument.

### `SimulationConfig.MateSelectionEnabled` - new flag

New bool, default `false`, added as the new last optional constructor
parameter + `{ get; }` property - identical two-edit pattern used for every
flag added this program.

## Explicit scope boundary

- No courtship state, no courtship delay, no interruption-by-threat logic.
  Conception remains instantaneous at the moment `Step` finds a valid
  `SeekMate` target in range, exactly as today's instantaneous
  proximity-pairing.
- No `MateChoosiness` gene, no genetic-distance mate quality, no vigour/wound
  scoring. `ScoreMate`'s existing scoring (`0.25f * safety / (1f +
  mate.Distance)`, `DecisionSystem.cs:493-515`) is untouched - it already
  produces a single target per creature (the nearest-other observation), so
  there is no "choice among candidates" to add here.
- No top-K mate-candidate perception. `ScoreMate` still only ever sees the
  single nearest other creature (the same `other` observation reused for
  predation/danger scoring) - unchanged.
- `Legacy` decision policy is untouched. `ScoreMate` is only ever called
  under `IntentUtilityV1` (`SimulationWorld.cs:828` passes `true`
  unconditionally for `reproductionEnabled` in that branch); the `Legacy`
  path never produces `SeekMate`, so `mateSelectionEnabled` has no visible
  effect there - a creature under `Legacy` can never satisfy
  `FindSeekMateTarget`'s `decision.Action == CreatureAction.SeekMate` check,
  so it always returns `-1` and that creature simply never reproduces under
  `Legacy` + `MateSelectionEnabled: true`. This mirrors how every other
  `IntentUtilityV1`-only fix this program (C-3, C-4, C-5, B-3's
  `IntentUtilityV1` half) has left `Legacy` unchanged.

## Hash safety

When `SimulationConfig.MateSelectionEnabled` is `false` (default), `Step`
calls `FindNearestReadyMate` exactly as it does today - `FindSeekMateTarget`
is never called, `_mateSelectionEnabled` is read once as a branch condition
with no other effect. Proven by a hash-regression test, same methodology as
every prior task this session (the standard `PredationVariation`/`Legacy`
scenario, which never sets `MateSelectionEnabled` or reaches
`IntentUtilityV1`, so this flag's default-off path is exercised
identically to every other flag's hash-regression baseline).

## Testing

1. `FindSeekMateTarget`: returns the target index when `firstIndex`'s
   decision is `SeekMate` toward a ready, unmatched, in-range creature.
2. `FindSeekMateTarget`: returns `-1` when `firstIndex`'s decision is not
   `SeekMate` (e.g. `Wander`, `Flee`) - the coincidental-proximity case this
   fix eliminates.
3. `FindSeekMateTarget`: returns `-1` when the target is out of `MateDistance`
   range (decision may be stale relative to current position, since decisions
   and reproduction ticks run on different cadences).
4. `FindSeekMateTarget`: returns `-1` when the target is not ready (fails
   `IsReady`), even though `firstIndex` is actively seeking it.
5. `FindSeekMateTarget`: returns `-1` when the target is already `_matched`
   this `Step` call.
6. `FindSeekMateTarget`: does **not** require the target's own decision to be
   `SeekMate` back toward `firstIndex` - one-sided pursuit alone is
   sufficient, proven by constructing a target whose decision is `Wander`.
7. Integration: with `MateSelectionEnabled: true` and `IntentUtilityV1`, a
   creature that actively decides `SeekMate` toward a ready partner and
   closes to within `MateDistance` produces a birth; an equally-ready,
   equally-in-range third creature that never appeared as anyone's
   `SeekMate` target does not get paired, even though it satisfies every
   check `FindNearestReadyMate` would have used.
8. Hash-regression test with the flag `false` (default).
9. Birth-rate comparison (informational, not a pass/fail gate): run the same
   scenario for a fixed tick count with `MateSelectionEnabled` false vs true
   under `IntentUtilityV1`, report the birth count delta in the task report
   so the actual behavior-change magnitude is documented, not guessed.

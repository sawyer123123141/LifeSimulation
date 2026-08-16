# Kin Recognition (C-5, part 3 of 3) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-5: creatures have no concept of family beyond the raw `CreatureLineage`
data already recorded at birth (`FirstParent`/`SecondParent`, populated by
`CreatureStore.AddChild`). Parts 1 (juvenile capability reduction) and 2
(parental following) are both merged. This is the final sub-feature:
creatures currently flee from and predation-target their own parents,
children, and siblings exactly like strangers.

Scoped through discussion with the user: this is *not* mate-avoidance
(explicitly out of scope - real animals don't reliably avoid inbreeding
either, and the user doesn't want that complexity) and *not* a full
"teams"/coordination system (a much larger feature - shared goals, group
movement - that recognition alone doesn't provide). It's narrowly: kin stop
panicking and preying on each other. This directly extends
`docs/ROADMAP.md`'s P4a line - "safety-gated rendezvous and two-parent
reproduction, creating short-lived family/local groups without hardcoded
species packs" - and is a small step toward P5's "genetic-distance
tracking" and "species clustering", without building either of those now.

Investigation found `DecisionSystem.ScorePredation`/`ScorePredationMulti`
(the flee/hunt scoring for the `IntentUtilityV1` decision policy - the
Legacy policy uses `PredationSystem.Decide` directly and is out of scope,
matching every other C-5/C-3/C-4/B-6 change this session) are pure static
functions taking value-type parameters only, with no `CreatureStore`
access. `SimulationWorld.TickDecisions` is what resolves perceived
creatures to indices (via `CombatGrid`) and already has `CreatureStore`
access to fetch `CreatureLineage` for both the observer and each candidate.

## Fix

### Kin test

A new static helper on `DecisionSystem`:

```csharp
private static bool IsKin(CreatureId selfId, CreatureLineage selfLineage, CreatureId otherId, CreatureLineage otherLineage)
{
    if (otherId.Equals(selfLineage.FirstParent) || otherId.Equals(selfLineage.SecondParent))
    {
        return true;
    }

    if (selfId.Equals(otherLineage.FirstParent) || selfId.Equals(otherLineage.SecondParent))
    {
        return true;
    }

    if (selfLineage.FirstParent.Value != 0
        && (selfLineage.FirstParent.Equals(otherLineage.FirstParent) || selfLineage.FirstParent.Equals(otherLineage.SecondParent)))
    {
        return true;
    }

    if (selfLineage.SecondParent.Value != 0
        && (selfLineage.SecondParent.Equals(otherLineage.FirstParent) || selfLineage.SecondParent.Equals(otherLineage.SecondParent)))
    {
        return true;
    }

    return false;
}
```

Parent/child checks come first (direct lineage match). The sibling checks
guard on `.Value != 0` before comparing - `default(CreatureId)` (value `0`)
is what a founder's unset parent slot holds, and two unrelated founders
must never register as siblings by both having an unset first parent.

### `ScorePredation` - single-candidate path (used when `MultiThreatPerceptionEnabled` is `false`)

Two new parameters, `CreatureId selfId, CreatureLineage selfLineage,
CreatureId otherId, CreatureLineage otherLineage`, plus a new
`bool kinRecognitionEnabled` flag parameter. At the top of the method,
immediately after the existing `if (!observation.IsValid) { return; }`
guard:

```csharp
if (kinRecognitionEnabled && IsKin(selfId, selfLineage, otherId, otherLineage))
{
    return;
}
```

`fleeScore`/`huntScore` stay at their already-initialized `0f` and no
candidates are added - identical shape to the existing invalid-observation
early return.

### `ScorePredationMulti` - multi-candidate path (used when `MultiThreatPerceptionEnabled` is `true`)

`PredationCandidateBuffer` (the existing top-K buffer from C-3,
`Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`) gains a
`CreatureLineage` alongside each stored `CreatureObservation`/`Phenotype`
pair, and a new `GetLineageAt(int)` accessor mirroring the existing
`GetObservationAt`/`GetPhenotypeAt`. `ScorePredationMulti` gains
`CreatureId selfId, CreatureLineage selfLineage, bool kinRecognitionEnabled`
parameters; inside its scoring loop, immediately after fetching
`observation`/`otherPhenotype`:

```csharp
if (kinRecognitionEnabled && IsKin(selfId, selfLineage, observation.CreatureId, others.GetLineageAt(i)))
{
    continue;
}
```

A skipped kin candidate is simply never compared as a flee/hunt candidate -
it cannot become `bestFleeTarget`/`bestHuntTarget` - but stays present in
the buffer for whatever else the buffer might carry information for
(nothing else currently reads it, but this keeps the skip local to scoring
rather than filtering the buffer's contents, matching how the analogous
`multiThreatPerceptionEnabled`-off path only affects scoring, not
perception).

### `SimulationWorld.TickDecisions` - supplying lineage data

At both call sites (the `ScorePredation`/`ScorePredationMulti` branch,
`SimulationWorld.cs` inside `TickDecisions`), fetch
`Creatures.GetLineageAt(index)` for the self creature once per creature
(alongside the existing `Genome genome = Creatures.GetGenomeAt(index);`-style
per-creature fetches already there), and thread it through:

- For `ScorePredation`: also fetch `Creatures.GetLineageAt(other.CreatureIndex)` when `other.IsValid`, pass `Creatures.GetIdAt(index)`, `selfLineage`, `other.CreatureId`, that lineage, and `Config.KinRecognitionEnabled`.
- For `ScorePredationMulti`: when building `otherCandidates` via `PerceptionSystem.FindOtherCreatures`, the call site already loops per-candidate (`CreatureCandidateBuffer`) before converting into `PredationCandidateBuffer` - add `Creatures.GetLineageAt(candidateIndex)` to each `Add(...)` call there. Pass `Creatures.GetIdAt(index)`, `selfLineage`, and `Config.KinRecognitionEnabled` into `ScorePredationMulti`.

### `SimulationConfig.KinRecognitionEnabled` - new flag

New bool, default `false`, added as the new last optional constructor
parameter + `{ get; }` property - identical two-edit pattern used for every
flag this session.

## Explicit scope boundary

- No mate-avoidance: `ScoreMate` is untouched. Kin can still be selected as
  a mate.
- No change to perception itself: the food/water danger-penalty term (fed
  by the single-nearest `other`/`threatIntensity` computation, per the
  scope note already recorded in C-3's spec) still treats a nearby kin
  creature as a threat presence for that purpose. Only `Flee`/`SeekPrey`
  scoring skips kin.
- No group/team behavior: recognized kin do not coordinate, share targets,
  or move together (parental following, from part 2, already covers the
  one form of "staying near family" this program builds - this part only
  stops kin from treating each other as flee/hunt targets).
- Legacy decision policy untouched, matching every other C-5/C-3/C-4/B-6
  change.

## Hash safety

When `SimulationConfig.KinRecognitionEnabled` is `false` (default),
`IsKin` is never called (both new call sites short-circuit on the flag
before evaluating it), so `ScorePredation`/`ScorePredationMulti` execute
identically to today. Proven by a hash-regression test, same methodology
as every prior task this session.

## Testing

1. `IsKin`: true for parent, true for child, true for sibling (shared
   non-default parent), false for unrelated creatures, false for two
   founders (both with default/unset parents - the sibling guard's
   `.Value != 0` check).
2. `ScorePredation`: with `kinRecognitionEnabled: true` and a kin
   observation, `fleeScore`/`huntScore` are both `0f` and no candidates are
   added, even when the underlying threat/hunt values would otherwise be
   well above the `0.10f` addition threshold.
3. `ScorePredationMulti`: with a mix of kin and non-kin candidates in the
   buffer, only the non-kin candidate can become the best flee/hunt target
   - verified by constructing a buffer where the kin candidate has the
   objectively higher threat/hunt score, and asserting the non-kin
   candidate wins anyway (or that no candidate is added, if the non-kin one
   scores below `0.10f`).
4. Integration: a creature with a kin threat nearby and
   `KinRecognitionEnabled: true` does not decide `Flee`/`SeekPrey` against
   that kin, under both `MultiThreatPerceptionEnabled: true` and `false`.
5. Hash-regression test with the flag `false` (default).

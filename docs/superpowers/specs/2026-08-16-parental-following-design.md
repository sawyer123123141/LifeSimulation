# Parental Following (C-5, part 2 of 3) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-5: offspring spawn at the parents' midpoint and immediately act fully
independently - no parental following, no kin recognition. Part 1
(`2026-08-16-juvenile-capability-design.md`, merged `dc1187e`) reduced a
juvenile's own capability (speed/vision/combat) but didn't change its
behavior. This is part 2 of 3: juveniles should stay near a parent instead
of wandering independently, while a fully-scoped survival need (hunger,
thirst, threat, etc.) still takes priority. Part 3 (kin recognition) is
separate and deferred.

Investigation found `CreatureLineage` (`SimulationTypes.cs:51`) already
stores `FirstParent`/`SecondParent` (`CreatureId`) and `Generation`,
populated by `CreatureStore.AddChild` and readable via
`CreatureStore.GetLineageAt(index)`. `CreatureStore.TryGetIndex(CreatureId,
out int)` resolves a `CreatureId` to a live index (returns `false` if the
creature is dead or the id is a default/never-assigned value - creatures
spawned via `CreatureStore.Add()` instead of `AddChild()` have default
`CreatureId` parents, which correctly report not-found). No new storage is
needed; this is purely a new movement-targeting branch.

## Fix

### `SimulationWorld.GetMovementTarget` - new Wander branch

A juvenile is any creature with `Creatures.GetNeedsAt(creatureIndex).Age <
ReproductionSystem.AdultAgeSeconds` (the same public threshold `AdultAgeSeconds`
part 1 exposed, reused rather than duplicated).

New branch inserted into `GetMovementTarget`, placed immediately before the
existing `Config.CognitionEnabled && decision.Action == CreatureAction.Wander`
block (so it is checked first and, when it applies, takes priority over
memory-based homing):

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
```

The random-offset-within-radius tail mirrors the existing `CognitionEnabled`
home-radius block exactly (`SimulationWorld.cs:588-613`), centered on the
parent's *current* position instead of a cached memory position, so a
juvenile within the follow radius still moves naturally instead of freezing
in place. The `DeterministicRandom.Float01` call uses selector index `3`
(the existing home-radius block uses `2`, plain exploration uses `0`) so the
two don't alias to the same random stream for a creature that could
theoretically hit both in different ticks.

If both parents are dead (or the creature was never assigned real parents),
`FindNearestAliveParent` returns `null` and control falls through to the
existing `CognitionEnabled` homing block, then to plain random exploration -
orphaned juveniles behave exactly as juveniles do today, no special-casing.

### `FindNearestAliveParent` - new private helper

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
```

### `SimulationConfig.ParentalFollowingEnabled` - new flag

New bool, default `false`, added as the new last optional constructor
parameter + `{ get; }` property - identical two-edit pattern used for every
flag this program (`MultiThreatPerceptionEnabled`, `RestBehaviorEnabled`,
`JuvenileCapabilityEnabled`).

## Scope boundary

This only changes a juvenile's own `Wander` target. It does not change:
- What counts as an "urgent" action - `Flee`, `SeekFood`, `SeekWater`,
  `SeekPrey`, `SeekCarcass`, `SeekThermalComfort`, `SeekMate`, `Rest` are
  all decided exactly as today, upstream in `DecisionSystem`, and all still
  win over `Wander` before `GetMovementTarget` is ever reached for those
  actions. A hungry, thirsty, or threatened juvenile still acts on its own,
  matching the "fallback only" design choice.
- `DecisionSystem`'s intent-scoring at all - no new `CreatureIntent` or
  `CreatureAction`, no signature changes. This is purely a movement-target
  override for the existing `Wander` action, mirroring how the existing
  `CognitionEnabled` home-radius override already works.
- Parent behavior - a following parent does not slow down, wait for, or
  otherwise react to a following juvenile. This is deliberately one-sided,
  matching the existing precedent that `Flee`'s threat and `SeekPrey`'s prey
  target also don't react to being targeted.

## Hash safety

When `SimulationConfig.ParentalFollowingEnabled` is `false` (default), the
new branch's condition is never true, so `GetMovementTarget` executes
identically to today for every creature. Proven by a hash-regression test,
same methodology as every prior task this session.

## Testing

1. `FindNearestAliveParent`: returns the closer of two alive parents;
   returns the lone alive parent when the other is dead; returns `null`
   when both are dead; returns `null` for a creature with default/no
   lineage (spawned via `Add()`, not `AddChild()`).
2. Integration: a juvenile with `ParentalFollowingEnabled: true`, a live
   parent placed beyond `followRadius`, and no urgent need (`Wander`
   decided) moves toward the parent's position across a `Step()`.
3. Integration: a juvenile with an urgent need (e.g. spawned with `Energy`
   near zero so `SeekFood` is decided instead of `Wander`) ignores the
   parent and pursues food normally, even with the flag enabled - proves
   the "fallback only" priority.
4. Integration: an adult (`Age >= AdultAgeSeconds`) with the flag enabled
   is unaffected - normal `Wander`/`CognitionEnabled` homing behavior,
   parent position never consulted.
5. Hash-regression test with the flag `false` (default).

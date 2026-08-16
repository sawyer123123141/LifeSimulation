# Juvenile Capability Reduction (C-5, part 1 of 3) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-5: `AdultAgeSeconds = 20f` (`ReproductionSystem.cs:11`, currently
`private`) gates reproduction only. Offspring spawn at the parents'
midpoint as fully capable agents - no reduced juvenile capability, no
parental following, no kin recognition. This is the first of three
sequential sub-features for C-5 (agreed with the user): capability
reduction first (this spec), parental following next, kin recognition
last (kin recognition naturally scales by existing `MemoryCapacity`/
`LearningRate` genes - no separate "intelligence cap" system needed).

Investigation found `Phenotype` (`GenomePhenotype.cs:121`) is a
`readonly struct` computed once at spawn via `Phenotype.FromGenome(genome)`
and cached in `CreatureStore._phenotypes[index]` - never recomputed. There
is no existing mechanism that scales any capability by age.

## Fix

### 1. `Phenotype.WithJuvenileScaling` - scaled copy, no storage change

Add a new public instance method inside the `Phenotype` struct (it can call
its own private constructor since it's the same type):

```csharp
public Phenotype WithJuvenileScaling(float multiplier)
{
    return new Phenotype(
        BodyMass, EnergyCapacity, HydrationCapacity, HealthCapacity,
        MaximumSpeed * multiplier,
        VisionRange * multiplier,
        FoodYield, IngestionRate, DigestionRate, WaterLossMultiplier, BasalEnergyCostMultiplier,
        AttackPower * multiplier,
        Defense * multiplier,
        Maneuverability * multiplier,
        FearResponse, Aggression, PlantFoodYieldMultiplier, MeatYieldMultiplier,
        MemoryConfidenceDecayPerSecond, CognitionRestCostMultiplier, TemperatureTolerance,
        LearningRate, Exploration, ReproductionCooldownSeconds, ReproductionEnergyCostFraction,
        MaximumAgeSeconds, Persistence);
}
```

Only `MaximumSpeed`, `VisionRange`, `AttackPower`, `Defense`, and
`Maneuverability` scale. Every other field (capacities, yields, cognition
fields, reproduction fields) is copied unchanged - a juvenile isn't
weaker-bodied or worse at digesting food, just slower, shorter-sighted, and
worse in a fight. `CreatureStore._phenotypes` and `Phenotype.FromGenome`
are NOT modified - this is purely an additive, on-demand transform.

### 2. `JuvenileSystem` - the ramp formula

New small static class, `Assets/Scripts/Simulation/Biology/JuvenileSystem.cs`,
mirroring `NeedsSystem`'s/`ReproductionSystem`'s scale:

```csharp
public static class JuvenileSystem
{
    public const float CapabilityFloor = 0.3f;

    public static float CapabilityMultiplier(float age, float adultAgeSeconds)
    {
        if (adultAgeSeconds <= 0f)
        {
            return 1f;
        }

        float t = Math.Max(0f, Math.Min(1f, age / adultAgeSeconds));
        return CapabilityFloor + ((1f - CapabilityFloor) * t);
    }
}
```

`0.3f` at `age = 0` (newborn), ramping linearly to `1.0f` at
`age >= adultAgeSeconds`. `0.3f` rather than `0f` because a literal-zero
vision range would make `PerceptionSystem` return nothing, and zero speed
would make a newborn structurally unable to ever reach food/water -
degenerate, not "weak."

### 3. `ReproductionSystem.AdultAgeSeconds` - exposed

Changed from `private const` to `public const` (`ReproductionSystem.cs:11`)
so `JuvenileSystem`'s caller can reuse the exact same threshold reproduction
already uses, rather than duplicating the value `20f` a second place.

### 4. `SimulationWorld.GetEffectivePhenotype` - the single insertion point

New private helper:

```csharp
private Phenotype GetEffectivePhenotype(int index)
{
    Phenotype phenotype = Creatures.GetPhenotypeAt(index);
    if (!Config.JuvenileCapabilityEnabled)
    {
        return phenotype;
    }

    float multiplier = JuvenileSystem.CapabilityMultiplier(Creatures.GetNeedsAt(index).Age, ReproductionSystem.AdultAgeSeconds);
    return phenotype.WithJuvenileScaling(multiplier);
}
```

Three existing call sites, each reading a creature's OWN phenotype for its
own action that tick, switch from `Creatures.GetPhenotypeAt(index)` to
`GetEffectivePhenotype(index)`:

- `TickDecisions`'s per-creature `Phenotype phenotype = ...` local (feeds
  vision-range perception calls and self-side predation scoring for that
  tick).
- `TickMovement`'s `MovementSystem.MoveToward(..., Creatures.GetPhenotypeAt(index).MaximumSpeed, ...)`.
- `TickCombat`'s three phenotype reads (`attacker`, `defender` in the
  hit-resolution loop, and `defender` in the damage-application loop) -
  each is that creature's own capability being read for its own combat
  role that tick (attacking or defending), not another creature's
  perception of it, so all three are in-scope for this task.

### Explicit scope boundary (deferred, not part of this task)

This only reduces a juvenile's OWN capability when it acts. It does NOT
make other creatures perceive a juvenile as weaker/easier prey - every
`Creatures.GetPhenotypeAt(other.CreatureIndex)` site (threat scoring, mate
selection, multi-threat perception in `DecisionSystem`) still reads the
juvenile's raw, unscaled phenotype from that OTHER creature's point of
view. Making predators correctly assess a juvenile as an easier target is
a separate, larger change (touches every cross-creature phenotype read in
`TickDecisions`) and is intentionally deferred to a future task, not
silently dropped - noted here per the user's request to track it rather
than build it now.

## Hash safety

When `SimulationConfig.JuvenileCapabilityEnabled` is `false` (default),
`GetEffectivePhenotype` returns `Creatures.GetPhenotypeAt(index)` unchanged
- byte-identical to today's three call sites. Proven by a hash-regression
test, same methodology as every prior task this session.

## Testing

1. `JuvenileSystem.CapabilityMultiplier`: `0.3f` at `age = 0`, `1.0f` at
   `age >= adultAgeSeconds`, linear midpoint (e.g. `0.65f` at
   `age = adultAgeSeconds / 2`).
2. `Phenotype.WithJuvenileScaling`: scales exactly the five named fields,
   leaves every other field unchanged.
3. Integration: a young creature's effective `MaximumSpeed` (and by
   extension its actual movement distance across a `Step()`) is lower than
   an otherwise-identical adult's, with `JuvenileCapabilityEnabled: true`.
4. Hash-regression test with the flag `false` (default).

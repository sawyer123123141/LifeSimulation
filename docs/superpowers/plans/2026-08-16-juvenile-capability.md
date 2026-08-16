# Juvenile Capability Reduction (C-5 part 1/3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make young creatures genuinely weaker - slower, shorter-sighted, worse in a fight - ramping to full adult capability by `AdultAgeSeconds`, gated behind `SimulationConfig.JuvenileCapabilityEnabled` (default `false`).

**Architecture:** Single task, five files. `Phenotype` gains a scaled-copy method (no storage change - `Phenotype` stays a cached, immutable-per-creature `readonly struct`). A new `JuvenileSystem` supplies the ramp formula. `SimulationWorld` gains one private helper (`GetEffectivePhenotype`) and swaps three existing "read my own phenotype" call sites to use it.

**Tech Stack:** C#, Unity, headless NUnit test harness (`tools/HeadlessTests`, plain `dotnet test`, .NET 8).

## Global Constraints

- `Phenotype.FromGenome` and `CreatureStore._phenotypes` are NOT modified - `Phenotype` remains a cached-at-spawn value. Juvenile scaling is a pure on-demand transform, never stored.
- Only `MaximumSpeed`, `VisionRange`, `AttackPower`, `Defense`, and `Maneuverability` scale. Every other `Phenotype` field is copied unchanged.
- `JuvenileSystem.CapabilityFloor = 0.3f` exactly - not `0f` (would zero out vision/speed, a degenerate state) and not any other value.
- When `SimulationConfig.JuvenileCapabilityEnabled` is `false` (the default), `GetEffectivePhenotype` must return the exact same value `Creatures.GetPhenotypeAt(index)` would - proven by a hash-regression test.
- Explicitly OUT OF SCOPE for this task (do not implement): making other creatures perceive a juvenile as weaker. Every `Creatures.GetPhenotypeAt(other.CreatureIndex)`-style read (threat scoring, mate selection, multi-threat perception) is left untouched. This is deferred to a separate future task.

---

### Task 1: Juvenile capability scaling

**Files:**
- Modify: `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` (add `WithJuvenileScaling` to the `Phenotype` struct)
- Create: `Assets/Scripts/Simulation/Biology/JuvenileSystem.cs`
- Modify: `Assets/Scripts/Simulation/Biology/ReproductionSystem.cs:11` (`AdultAgeSeconds` visibility)
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (new `JuvenileCapabilityEnabled` flag)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (`GetEffectivePhenotype` helper; `TickDecisions`, `TickMovement`, `TickCombat` call sites)
- Test: `Assets/Tests/EditMode/BiologyTests.cs`, `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Consumes: `Phenotype`'s existing 27-field private constructor and public properties (`GenomePhenotype.cs:121-207`, unchanged), `ReproductionSystem.AdultAgeSeconds` (visibility change only, value `20f` unchanged).
- Produces: `Phenotype.WithJuvenileScaling(float multiplier)`, `JuvenileSystem.CapabilityFloor`/`CapabilityMultiplier`, `SimulationConfig.JuvenileCapabilityEnabled`, `SimulationWorld.GetEffectivePhenotype(int index)` - no other task in this plan depends on these, this is the only task.

**`GenomePhenotype.cs`** - add this method inside the `Phenotype` struct (`GenomePhenotype.cs:121-207`), placed immediately after the `Persistence` property (line 207), before `FromGenome` (line 209):

```csharp
public Phenotype WithJuvenileScaling(float multiplier)
{
    return new Phenotype(
        BodyMass,
        EnergyCapacity,
        HydrationCapacity,
        HealthCapacity,
        MaximumSpeed * multiplier,
        VisionRange * multiplier,
        FoodYield,
        IngestionRate,
        DigestionRate,
        WaterLossMultiplier,
        BasalEnergyCostMultiplier,
        AttackPower * multiplier,
        Defense * multiplier,
        Maneuverability * multiplier,
        FearResponse,
        Aggression,
        PlantFoodYieldMultiplier,
        MeatYieldMultiplier,
        MemoryConfidenceDecayPerSecond,
        CognitionRestCostMultiplier,
        TemperatureTolerance,
        LearningRate,
        Exploration,
        ReproductionCooldownSeconds,
        ReproductionEnergyCostFraction,
        MaximumAgeSeconds,
        Persistence);
}
```

This argument order matches the private constructor's parameter order exactly (`GenomePhenotype.cs:123-150`) - do not reorder.

**`JuvenileSystem.cs`** - new file, complete contents:

```csharp
using System;

namespace LifeSimulation.Simulation.Biology
{
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
}
```

**`ReproductionSystem.cs`** - current (`ReproductionSystem.cs:11`):

```csharp
private const float AdultAgeSeconds = 20f;
```

New:

```csharp
public const float AdultAgeSeconds = 20f;
```

No other line in `ReproductionSystem.cs` changes - `AdultAgeSeconds` is still used exactly as before within the class, this only widens its visibility.

**`SimulationConfig.cs`**: add `juvenileCapabilityEnabled` as the new LAST optional constructor parameter (after `restBehaviorEnabled`), assigned to a new `JuvenileCapabilityEnabled { get; }` property placed immediately after `RestBehaviorEnabled`'s property - the exact same two-edit pattern (constructor parameter + body assignment + property) used for every flag added this session.

**`SimulationWorld.cs`**: add a new private method, placed immediately after `GetMovementTarget` (`SimulationWorld.cs:528-627`) and before `TickDecisions` (`SimulationWorld.cs:629`):

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

Three call-site changes, each replacing an existing `Creatures.GetPhenotypeAt(...)` read of a creature's OWN phenotype for its own action that tick:

1. `TickDecisions` (`SimulationWorld.cs:640`), current:

```csharp
Phenotype phenotype = Creatures.GetPhenotypeAt(index);
```

New:

```csharp
Phenotype phenotype = GetEffectivePhenotype(index);
```

2. `TickMovement` (`SimulationWorld.cs:513-527`), current:

```csharp
private void TickMovement(long nextTick)
{
    for (int index = 0; index < Creatures.Count; index++)
    {
        CreatureId id = Creatures.GetIdAt(index);
        ref MovementState movement = ref Creatures.GetMovementRefAt(index);
        SimVector2 target = GetMovementTarget(index, id, nextTick, movement.Position);
        MovementSystem.MoveToward(
            ref movement,
            target,
            Creatures.GetPhenotypeAt(index).MaximumSpeed,
            Config.FixedDeltaTime,
            Arena);
    }
}
```

New - only the `MaximumSpeed` source changes:

```csharp
private void TickMovement(long nextTick)
{
    for (int index = 0; index < Creatures.Count; index++)
    {
        CreatureId id = Creatures.GetIdAt(index);
        ref MovementState movement = ref Creatures.GetMovementRefAt(index);
        SimVector2 target = GetMovementTarget(index, id, nextTick, movement.Position);
        MovementSystem.MoveToward(
            ref movement,
            target,
            GetEffectivePhenotype(index).MaximumSpeed,
            Config.FixedDeltaTime,
            Arena);
    }
}
```

3. `TickCombat` (`SimulationWorld.cs:1161-1222`), current:

```csharp
private void TickCombat(long tick)
{
    EnsureCombatDamageCapacity(Creatures.Count);
    Array.Clear(_combatDamage, 0, Creatures.Count);
    for (int index = 0; index < Creatures.Count; index++)
    {
        ref CombatState combat = ref Creatures.GetCombatRefAt(index);
        combat.AttackRecoveryRemaining = Math.Max(0f, combat.AttackRecoveryRemaining - Config.FixedDeltaTime);
        CreatureDecision decision = Creatures.GetDecisionAt(index);
        if (decision.Action != CreatureAction.Attack
            || combat.AttackRecoveryRemaining > 0f
            || !Creatures.TryGetIndex(decision.TargetCreatureId, out int targetIndex)
            || targetIndex == index)
        {
            continue;
        }

        MovementState attackerMovement = Creatures.GetMovementAt(index);
        MovementState defenderMovement = Creatures.GetMovementAt(targetIndex);
        float engagementDistance = SimVector2.Distance(attackerMovement.Position, defenderMovement.Position);
        if (engagementDistance > 1.1f)
        {
            continue;
        }

        Phenotype attacker = Creatures.GetPhenotypeAt(index);
        Phenotype defender = Creatures.GetPhenotypeAt(targetIndex);
        float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender, engagementDistance, Config.PredationEconomicsEnabled));
        float roll = DeterministicRandom.Float01(
            Config.WorldSeed,
            RandomDomain.AttackResolution,
            tick,
            Creatures.GetIdAt(index).Value,
            decision.TargetCreatureId.Value,
            0);
        combat.AttackRecoveryRemaining = 0.75f;
        if (roll > hitChance)
        {
            continue;
        }

        float damage = 4f + (12f * attacker.AttackPower);
        _combatDamage[targetIndex] += damage;
        _attackHitCount++;
    }

    for (int index = 0; index < Creatures.Count; index++)
    {
        float damage = _combatDamage[index];
        if (damage <= 0f)
        {
            continue;
        }

        Phenotype defender = Creatures.GetPhenotypeAt(index);
        ref CreatureNeeds targetNeeds = ref Creatures.GetNeedsRefAt(index);
        ref CombatState targetCombat = ref Creatures.GetCombatRefAt(index);
        targetNeeds.Health -= damage;
        targetCombat.WoundSeverity += damage / defender.HealthCapacity;
        if (targetNeeds.Health <= 0f) RequestDeath(Creatures.GetIdAt(index), DeathCause.Predation);
    }
}
```

New - three `Creatures.GetPhenotypeAt(...)` reads become `GetEffectivePhenotype(...)`, nothing else changes:

```csharp
private void TickCombat(long tick)
{
    EnsureCombatDamageCapacity(Creatures.Count);
    Array.Clear(_combatDamage, 0, Creatures.Count);
    for (int index = 0; index < Creatures.Count; index++)
    {
        ref CombatState combat = ref Creatures.GetCombatRefAt(index);
        combat.AttackRecoveryRemaining = Math.Max(0f, combat.AttackRecoveryRemaining - Config.FixedDeltaTime);
        CreatureDecision decision = Creatures.GetDecisionAt(index);
        if (decision.Action != CreatureAction.Attack
            || combat.AttackRecoveryRemaining > 0f
            || !Creatures.TryGetIndex(decision.TargetCreatureId, out int targetIndex)
            || targetIndex == index)
        {
            continue;
        }

        MovementState attackerMovement = Creatures.GetMovementAt(index);
        MovementState defenderMovement = Creatures.GetMovementAt(targetIndex);
        float engagementDistance = SimVector2.Distance(attackerMovement.Position, defenderMovement.Position);
        if (engagementDistance > 1.1f)
        {
            continue;
        }

        Phenotype attacker = GetEffectivePhenotype(index);
        Phenotype defender = GetEffectivePhenotype(targetIndex);
        float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender, engagementDistance, Config.PredationEconomicsEnabled));
        float roll = DeterministicRandom.Float01(
            Config.WorldSeed,
            RandomDomain.AttackResolution,
            tick,
            Creatures.GetIdAt(index).Value,
            decision.TargetCreatureId.Value,
            0);
        combat.AttackRecoveryRemaining = 0.75f;
        if (roll > hitChance)
        {
            continue;
        }

        float damage = 4f + (12f * attacker.AttackPower);
        _combatDamage[targetIndex] += damage;
        _attackHitCount++;
    }

    for (int index = 0; index < Creatures.Count; index++)
    {
        float damage = _combatDamage[index];
        if (damage <= 0f)
        {
            continue;
        }

        Phenotype defender = GetEffectivePhenotype(index);
        ref CreatureNeeds targetNeeds = ref Creatures.GetNeedsRefAt(index);
        ref CombatState targetCombat = ref Creatures.GetCombatRefAt(index);
        targetNeeds.Health -= damage;
        targetCombat.WoundSeverity += damage / defender.HealthCapacity;
        if (targetNeeds.Health <= 0f) RequestDeath(Creatures.GetIdAt(index), DeathCause.Predation);
    }
}
```

**Behavior table:**

| Case | `age` | `adultAgeSeconds` | Expected `CapabilityMultiplier` |
|---|---|---|---|
| 1. Newborn | `0f` | `20f` | `0.3f` |
| 2. Adult | `20f` | `20f` | `1.0f` |
| 3. Past adult | `40f` | `20f` | `1.0f` (clamped, not extrapolated) |
| 4. Midpoint | `10f` | `20f` | `0.3 + 0.7*0.5 = 0.65f` |

For `Phenotype.WithJuvenileScaling`: given any `Phenotype` with `MaximumSpeed = 4f, VisionRange = 10f, AttackPower = 0.6f, Defense = 0.4f, Maneuverability = 0.5f` and `multiplier = 0.3f`, the returned `Phenotype` has `MaximumSpeed == 1.2f, VisionRange == 3f, AttackPower == 0.18f, Defense == 0.12f, Maneuverability == 0.15f` (all `Within(0.0001f)`), and every other property (e.g. `EnergyCapacity`, `HealthCapacity`) unchanged from the original.

- [ ] **Step 1: Write the failing tests**

Append to `Assets/Tests/EditMode/BiologyTests.cs`, inside the `BiologyTests` class:

```csharp
[Test]
public void JuvenileCapabilityMultiplierRampsLinearlyFromFloorToFull()
{
    Assert.That(JuvenileSystem.CapabilityMultiplier(age: 0f, adultAgeSeconds: 20f), Is.EqualTo(0.3f).Within(0.0001f));
    Assert.That(JuvenileSystem.CapabilityMultiplier(age: 20f, adultAgeSeconds: 20f), Is.EqualTo(1.0f).Within(0.0001f));
    Assert.That(JuvenileSystem.CapabilityMultiplier(age: 40f, adultAgeSeconds: 20f), Is.EqualTo(1.0f).Within(0.0001f));
    Assert.That(JuvenileSystem.CapabilityMultiplier(age: 10f, adultAgeSeconds: 20f), Is.EqualTo(0.65f).Within(0.0001f));
}

[Test]
public void WithJuvenileScalingScalesOnlySpeedVisionAndCombatFields()
{
    Phenotype adult = Phenotype.FromGenome(new Genome(
        0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
        attack: 0.6f, defense: 0.4f, maneuverability: 0.5f));

    Phenotype scaled = adult.WithJuvenileScaling(0.3f);

    Assert.That(scaled.MaximumSpeed, Is.EqualTo(adult.MaximumSpeed * 0.3f).Within(0.0001f));
    Assert.That(scaled.VisionRange, Is.EqualTo(adult.VisionRange * 0.3f).Within(0.0001f));
    Assert.That(scaled.AttackPower, Is.EqualTo(adult.AttackPower * 0.3f).Within(0.0001f));
    Assert.That(scaled.Defense, Is.EqualTo(adult.Defense * 0.3f).Within(0.0001f));
    Assert.That(scaled.Maneuverability, Is.EqualTo(adult.Maneuverability * 0.3f).Within(0.0001f));
    Assert.That(scaled.EnergyCapacity, Is.EqualTo(adult.EnergyCapacity).Within(0.0001f));
    Assert.That(scaled.HealthCapacity, Is.EqualTo(adult.HealthCapacity).Within(0.0001f));
}
```

Write the remaining two tests yourself:
1. A `CoreSimulationTests.cs`-style integration test proving a young creature moves less distance across one `Step()` than an otherwise-identical adult, with `JuvenileCapabilityEnabled: true`: build a `SimulationConfig` with `decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1`, `juvenileCapabilityEnabled: true`, spawn two creatures with the same `Genome.Neutral`, set one's `needs.Age` to `0f` and the other's to `ReproductionSystem.AdultAgeSeconds` via `world.Creatures.GetNeedsRefAt(index).Age = ...` before stepping, record both `movement.Position` before `Step()`, call `Step()` once, and assert the young creature's movement distance (`SimVector2.Distance` between before/after position) is less than the adult's. Use a schedule where `DecisionsHz == BaseFrequencyHz` (e.g. `SimulationSchedule(1,1,1,1,1,1,1,1)`, matching this session's C-4 task's fix for the same 10-tick-decision-interval issue) so both creatures actually get a fresh decision within the single `Step()`.
2. A flag-off hash-regression test, following the exact template from this session's prior tasks (`ExpectedRestBehaviorDisabledHash` etc. in `CoreSimulationTests.cs`): derive `ExpectedJuvenileCapabilityDisabledHash` by running the same `PredationVariation` scenario this session's hash tests use (`SimulationSchedule(60,60,30,10,10,10,5,1)`, `worldSeed: 99`, `initialPopulation: 2`, `founderProfile: FounderProfile.PredationVariation`, `juvenileCapabilityEnabled` omitted since it doesn't exist at the pre-change commit, 50 `Step()` calls) against a throwaway worktree at the commit immediately before your changes.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~JuvenileCapabilityMultiplierRampsLinearlyFromFloorToFull|FullyQualifiedName~WithJuvenileScalingScalesOnlySpeedVisionAndCombatFields"`

Expected: FAIL to compile (`JuvenileSystem` and `Phenotype.WithJuvenileScaling` don't exist yet).

- [ ] **Step 3: Implement the changes**

Apply the exact changes shown above to `GenomePhenotype.cs`, create `JuvenileSystem.cs`, and apply the changes to `ReproductionSystem.cs`, `SimulationConfig.cs`, and `SimulationWorld.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the four new ones from this task. Total count should be 298 (294 existing + 4 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/GenomePhenotype.cs Assets/Scripts/Simulation/Biology/JuvenileSystem.cs Assets/Scripts/Simulation/Biology/ReproductionSystem.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/BiologyTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Scale a juvenile's own speed, vision, and combat stats toward adult capability with age"
```

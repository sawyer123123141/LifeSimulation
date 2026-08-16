# B-5: Predation Hunting Economics — Design Spec

## Problem

`Assets/Scripts/Simulation/Behavior/PredationSystem.cs` gates hunting behind a hard boolean threshold:

```csharp
private const float MinimumHuntingDiet = 0.58f;
private const float MinimumHuntingAggression = 0.35f;

public static float HuntCapability(Phenotype attacker, Phenotype defender)
{
    float diet = Clamp01((attacker.MeatYieldMultiplier - 0.5f) / 1f);
    if (!HasViableHuntingStrategy(attacker)) { return 0f; }
    float advantage = attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f);
    return Clamp01(advantage * attacker.Aggression * diet);
}

public static bool HasViableHuntingStrategy(Phenotype phenotype)
{
    float diet = Clamp01((phenotype.MeatYieldMultiplier - 0.5f) / 1f);
    return diet >= MinimumHuntingDiet && phenotype.Aggression >= MinimumHuntingAggression;
}
```

Below the threshold, `HuntCapability` returns exactly `0f` regardless of opportunity quality. This is a categorical predator/prey flag hiding in a float comparison — violates the project rule that ecological roles must be derived, never authoritative biology flags (AGENTS.md), and P1's explicit ban on predator/prey booleans.

`HasViableHuntingStrategy` IS used externally: `SimulationWorld.cs:1345` calls it inside `TickStatistics` to compute a `viableHunterCount` telemetry stat (not hash-covered — statistics are explicitly excluded from `ComputeStateHash()`). It stays untouched as a standalone legacy/statistics-only helper, decoupled from the new `HuntCapability` formula — the "viable hunter" telemetry threshold keeps its current meaning regardless of `PredationEconomicsEnabled`.

## Fix

Replace the hard gate with a continuous expected-value comparison: expected meat energy gained × success probability, minus expected injury cost and pursuit energy cost. `DietSpecialization` (via `MeatYieldMultiplier`) and `Aggression` shift the balance instead of forbidding the behavior below a cutoff.

### `HuntCapability` — single signature, flag-selected internally

Rather than two parallel overloads, one signature takes both `distance` and `economicsEnabled`; every call site already has or can cheaply compute a distance value, so there is no benefit to a distance-less overload:

```csharp
public static float HuntCapability(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
{
    if (!economicsEnabled)
    {
        float diet = Clamp01((attacker.MeatYieldMultiplier - 0.5f) / 1f);
        if (!HasViableHuntingStrategy(attacker)) { return 0f; }
        float legacyAdvantage = attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f);
        return Clamp01(legacyAdvantage * attacker.Aggression * diet);
    }

    float successChance = Clamp01(attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f));
    float expectedGain = defender.EnergyCapacity * attacker.MeatYieldMultiplier * successChance;
    float expectedInjuryCost = defender.AttackPower * (1f - successChance) * InjuryCostScale;
    float expectedPursuitCost = PursuitCostPerDistance * distance;
    float netEnergyValue = expectedGain - expectedInjuryCost - expectedPursuitCost;
    return Clamp01(netEnergyValue / NormalizingEnergyScale) * attacker.Aggression;
}
```

The legacy branch is byte-for-byte the current production formula (including its call to `HasViableHuntingStrategy`, which stays public and untouched) — guarantees identical output, and therefore identical hash, whenever `economicsEnabled` is `false`.

- `successChance`: unchanged from today's `advantage` term — pure combat capability (AttackPower vs Defense/Maneuverability).
- `expectedGain`: prey's `EnergyCapacity` (size proxy) × attacker's `MeatYieldMultiplier` (diet specialization, how much of the kill the attacker can actually use) × `successChance`.
- `expectedInjuryCost`: prey's own `AttackPower` scaled by fail chance — bigger/stronger prey make failed hunts costlier.
- `expectedPursuitCost`: linear in `distance` — replaces the old outer `distanceAvailability` multiply (see below), single source of distance-cost truth.
- `Aggression`: applied once, as a final multiplier on net EV — temperament/risk-tolerance, kept separate from combat capability per the genome's existing separation of temperament vs capability genes.
- New private consts in `PredationSystem.cs`: `InjuryCostScale`, `PursuitCostPerDistance`, `NormalizingEnergyScale`. Exact values chosen and pinned via test cases during plan-writing (must keep typical mid-range phenotype pairs producing non-degenerate 0..1 hunt scores).
- `MinimumHuntingDiet`/`MinimumHuntingAggression` and the old (no-distance) `HuntCapability`/`Decide` overloads are kept, renamed to a legacy path (see Flag gating) — not deleted, since the flag must be able to select either behavior. `HasViableHuntingStrategy` is untouched (see caller note above).

### `Threat()` (flee side) — kept symmetric, same single-signature approach

```csharp
public static float Threat(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
{
    float huntScore = HuntCapability(attacker, defender, distance, economicsEnabled);
    if (huntScore <= 0f) { return 0f; }
    float pressure = attacker.AttackPower * (0.5f + attacker.Aggression);
    float resistance = defender.Defense + (0.25f * defender.Maneuverability) + 0.01f;
    return Clamp01(pressure / (pressure + resistance));
}
```

Same gate shape as today (`<= 0f`) and same pressure/resistance math — only the `HuntCapability` call underneath changes behavior, and only when `economicsEnabled` is `true`. When `false`, `HuntCapability` returns the byte-identical legacy value, so `Threat`'s output is byte-identical too regardless of what `distance` is passed (the legacy branch ignores it).

### `Decide()` — drop double-counted distance, thread the flag

Current (6-arg overload used by the `PredationVariation` + `Legacy` policy branch in `SimulationWorld.cs:837`):
```csharp
float distanceAvailability = 1f / (1f + otherObservation.Distance);
float hunger = 1f - (needs.Energy / self.EnergyCapacity);
float threat = Threat(other, self) * self.FearResponse * distanceAvailability;
float hunt = HuntCapability(self, other) * hunger * distanceAvailability;
```

New — both public `Decide` overloads gain a trailing `bool economicsEnabled = false` parameter (default keeps the zero-arg-diagnostics overload's existing callers, if any, compiling unchanged):
```csharp
float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + otherObservation.Distance);
float hunger = 1f - (needs.Energy / self.EnergyCapacity);
float threat = Threat(other, self, otherObservation.Distance, economicsEnabled) * self.FearResponse * distanceAvailability;
float hunt = HuntCapability(self, other, otherObservation.Distance, economicsEnabled) * hunger * distanceAvailability;
```

`distanceAvailability` becomes a no-op multiplier (`1f`) when `economicsEnabled` is `true`, since `HuntCapability`'s `expectedPursuitCost` term already accounts for distance in that mode — avoids double-penalizing. When `economicsEnabled` is `false`, `distanceAvailability` keeps its exact current formula and the legacy `HuntCapability`/`Threat` branches ignore the `distance` parameter entirely, so the whole expression is byte-identical to today's code. This was a real bug in an earlier draft of this spec (it dropped `distanceAvailability` unconditionally, which would have broken legacy-mode hash-safety) — caught and fixed during plan-writing.

### Flag gating

Per this session's established precedent (`ForagingEconomicsEnabled`, `PlantCohortsEnabled`, `DecisionPolicyVersion` all gated new B-tier behavior behind a flag defaulting to the legacy value, satisfying the project's "versioned migration" requirement without forcing an immediate re-run of every recorded scenario):

- Add `PredationEconomicsEnabled` (bool, default `false`) to `SimulationConfig`, same optional-constructor-param pattern as `ForagingEconomicsEnabled`.
- Thread it through `SimulationWorld.TickDecisions` into `PredationSystem.Decide(...)`, alongside the existing diagnostics `ref` param.
- `PredationSystem.Decide` needs a code path selector: when `PredationEconomicsEnabled` is `false`, use the current threshold-gated formula (kept, renamed internally e.g. `HuntCapabilityLegacy`, still using the old signature without `distance`); when `true`, use the new continuous formula above. Existing recorded scenarios (predation demo, any prototype config not explicitly opting in) keep producing identical hashes until deliberately migrated.

### All `PredationSystem` call sites in `SimulationWorld.cs` (confirmed via grep, must all be updated or explicitly left on the legacy overload)

- `SimulationWorld.cs:667` — `PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype)` inside the `DecisionPolicyVersion.IntentUtilityV1` branch of `TickDecisions`, feeds a `threatIntensity` float into that policy's own scoring (unrelated to the Legacy-policy `Decide` path this fix targets). `other` here is a `CreatureObservation` with a `.Distance` field (same type used in `PredationSystem.Decide`). Pass `other.Distance` through so this compiles against the new `Threat` signature; this call site is NOT gated by `PredationEconomicsEnabled` — it always uses the new `Threat(attacker, defender, distance)` signature (distance is just a parameter now, always available here), independent of which hunting formula (legacy/new) is active elsewhere.
- `SimulationWorld.cs:837` — `PredationSystem.Decide(needs, phenotype, otherPhenotype, other, decision, ref diagnostics)` inside `TickDecisions`, gated by `Config.FounderProfile == FounderProfile.PredationVariation && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy`. This is the primary call site the flag must thread through: add `Config.PredationEconomicsEnabled` as a new final argument.
- `SimulationWorld.cs:1158` — `PredationSystem.Threat(attacker, defender)` inside `TickCombat`, used to compute `hitChance` for an already-adjacent pair (`SimVector2.Distance(attackerMovement.Position, defenderMovement.Position)` already computed at line 1151 as the adjacency check). Pass that same computed distance through.
- `SimulationWorld.cs:1345` — `PredationSystem.HasViableHuntingStrategy(phenotype)` inside `TickStatistics`, for the `viableHunterCount` telemetry stat. Untouched — see caller note above.

### Files touched

- `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` — core change: new `HuntCapability` overload (with `distance` param), new `Threat` overload (with `distance` param), updated `Decide` (new `economicsEnabled` bool param selecting legacy vs new path), legacy `HuntCapability`/`Threat`/`Decide` behavior preserved under the old (no-distance) parameter shapes where still needed for the Legacy path, `HasViableHuntingStrategy` untouched.
- `Assets/Scripts/Simulation/Core/SimulationConfig.cs` — add `PredationEconomicsEnabled` field + optional constructor param (default `false`), following the exact pattern of `ForagingEconomicsEnabled` at line 97/118/139 of the current file.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs` — update all three call sites listed above (667, 837, 1158); 1345 stays as-is.
- Tests: create `Assets/Tests/EditMode/PredationSystemTests.cs` (no existing test file for this class today, confirmed via directory listing) covering: legacy path unchanged when flag `false`; new path produces higher hunt score for closer/weaker/bigger prey and lower for farther/stronger/smaller prey; zero-incentive pairs (e.g. herbivore attacker) produce zero threat; `Aggression` scales net EV monotonically.

## Out of scope

- Tuning the new consts for "good" gameplay balance beyond making tests pass with sensible relative orderings — real balance tuning happens after re-running evidence with the flag on, a separate follow-up.
- Migrating any existing recorded scenario/demo to the new flag — deliberate follow-up per the project's versioned-migration rule, not part of this fix.
- Changes to `PreferCarcassWhenUseful` (unrelated overloads in the same file, untouched by this defect).

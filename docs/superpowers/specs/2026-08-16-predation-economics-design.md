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

Below the threshold, `HuntCapability` returns exactly `0f` regardless of opportunity quality. This is a categorical predator/prey flag hiding in a float comparison — violates the project rule that ecological roles must be derived, never authoritative biology flags (AGENTS.md), and P1's explicit ban on predator/prey booleans. `HasViableHuntingStrategy` has no external callers (confirmed via repo-wide grep) — safe to remove entirely.

## Fix

Replace the hard gate with a continuous expected-value comparison: expected meat energy gained × success probability, minus expected injury cost and pursuit energy cost. `DietSpecialization` (via `MeatYieldMultiplier`) and `Aggression` shift the balance instead of forbidding the behavior below a cutoff.

### New `HuntCapability`

```csharp
public static float HuntCapability(Phenotype attacker, Phenotype defender, float distance)
{
    float successChance = Clamp01(attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f));
    float expectedGain = defender.EnergyCapacity * attacker.MeatYieldMultiplier * successChance;
    float expectedInjuryCost = defender.AttackPower * (1f - successChance) * InjuryCostScale;
    float expectedPursuitCost = PursuitCostPerDistance * distance;
    float netEnergyValue = expectedGain - expectedInjuryCost - expectedPursuitCost;
    return Clamp01(netEnergyValue / NormalizingEnergyScale) * attacker.Aggression;
}
```

- `successChance`: unchanged from today's `advantage` term — pure combat capability (AttackPower vs Defense/Maneuverability).
- `expectedGain`: prey's `EnergyCapacity` (size proxy) × attacker's `MeatYieldMultiplier` (diet specialization, how much of the kill the attacker can actually use) × `successChance`.
- `expectedInjuryCost`: prey's own `AttackPower` scaled by fail chance — bigger/stronger prey make failed hunts costlier.
- `expectedPursuitCost`: linear in `distance` — replaces the old outer `distanceAvailability` multiply (see below), single source of distance-cost truth.
- `Aggression`: applied once, as a final multiplier on net EV — temperament/risk-tolerance, kept separate from combat capability per the genome's existing separation of temperament vs capability genes.
- New private consts in `PredationSystem.cs`: `InjuryCostScale`, `PursuitCostPerDistance`, `NormalizingEnergyScale`. Exact values chosen and pinned via test cases during plan-writing (must keep typical mid-range phenotype pairs producing non-degenerate 0..1 hunt scores).
- `HasViableHuntingStrategy` and `MinimumHuntingDiet`/`MinimumHuntingAggression` deleted.

### `Threat()` (flee side) — kept symmetric

```csharp
public static float Threat(Phenotype attacker, Phenotype defender, float distance)
{
    float huntScore = HuntCapability(attacker, defender, distance);
    if (huntScore <= 0f) { return 0f; }
    float pressure = attacker.AttackPower * (0.5f + attacker.Aggression);
    float resistance = defender.Defense + (0.25f * defender.Maneuverability) + 0.01f;
    return Clamp01(pressure / (pressure + resistance));
}
```

Same gate shape as today (`<= 0f`), now driven by continuous economics instead of a hard threshold: a creature with zero economic incentive to hunt still poses zero threat (e.g. a herbivore never triggers prey fear). Minimal diff from current code.

### `Decide()` — drop double-counted distance

Current:
```csharp
float distanceAvailability = 1f / (1f + otherObservation.Distance);
float hunger = 1f - (needs.Energy / self.EnergyCapacity);
float threat = Threat(other, self) * self.FearResponse * distanceAvailability;
float hunt = HuntCapability(self, other) * hunger * distanceAvailability;
```

New:
```csharp
float hunger = 1f - (needs.Energy / self.EnergyCapacity);
float threat = Threat(other, self, otherObservation.Distance) * self.FearResponse;
float hunt = HuntCapability(self, other, otherObservation.Distance) * hunger;
```

`distanceAvailability` variable removed — distance cost now lives once, inside `HuntCapability`'s `expectedPursuitCost` term, consumed by both `Decide()`'s hunt score and `Threat()`'s gate. Avoids double-penalizing distance.

### Flag gating

Per this session's established precedent (`ForagingEconomicsEnabled`, `PlantCohortsEnabled`, `DecisionPolicyVersion` all gated new B-tier behavior behind a flag defaulting to the legacy value, satisfying the project's "versioned migration" requirement without forcing an immediate re-run of every recorded scenario):

- Add `PredationEconomicsEnabled` (bool, default `false`) to `SimulationConfig`, same optional-constructor-param pattern as `ForagingEconomicsEnabled`.
- Thread it through `SimulationWorld.TickDecisions` into `PredationSystem.Decide(...)`, alongside the existing diagnostics `ref` param.
- `PredationSystem.Decide` needs a code path selector: when `PredationEconomicsEnabled` is `false`, use the current threshold-gated formula (kept, renamed internally e.g. `HuntCapabilityLegacy`, still using the old signature without `distance`); when `true`, use the new continuous formula above. Existing recorded scenarios (predation demo, any prototype config not explicitly opting in) keep producing identical hashes until deliberately migrated.

### Files touched

- `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` — core change: new `HuntCapability` overload, updated `Threat`, updated `Decide`, legacy path preserved under old names.
- `Assets/Scripts/Simulation/Core/SimulationConfig.cs` — add `PredationEconomicsEnabled` field + optional constructor param + `Default` const.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs` — thread the flag from config into the `PredationSystem.Decide` call site in `TickDecisions`.
- Tests: create `Assets/Tests/EditMode/PredationSystemTests.cs` (no existing test file for this class today, confirmed via directory listing) covering: legacy path unchanged when flag `false`; new path produces higher hunt score for closer/weaker/bigger prey and lower for farther/stronger/smaller prey; zero-incentive pairs (e.g. herbivore attacker) produce zero threat; `Aggression` scales net EV monotonically.

## Out of scope

- Tuning the new consts for "good" gameplay balance beyond making tests pass with sensible relative orderings — real balance tuning happens after re-running evidence with the flag on, a separate follow-up.
- Migrating any existing recorded scenario/demo to the new flag — deliberate follow-up per the project's versioned-migration rule, not part of this fix.
- Changes to `PreferCarcassWhenUseful` (unrelated overloads in the same file, untouched by this defect).

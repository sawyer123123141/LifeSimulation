# Predation Hunting Economics (B-5) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `PredationSystem`'s hard diet/aggression threshold gate with a continuous expected-value formula, gated behind a new `PredationEconomicsEnabled` flag (default `false`) so existing recorded scenarios keep producing identical hashes.

**Architecture:** `PredationSystem.HuntCapability`/`Threat`/`Decide` each gain a `bool economicsEnabled` parameter (and `HuntCapability`/`Threat` gain a `float distance` parameter). When `false`, every method's legacy branch reproduces today's exact math byte-for-byte. When `true`, `HuntCapability` computes `netEnergyValue = expectedGain - expectedInjuryCost - expectedPursuitCost`, scales it into `[0,1]`, and multiplies by `Aggression`. `SimulationConfig.PredationEconomicsEnabled` threads the flag through all three `PredationSystem` call sites in `SimulationWorld.cs`.

**Tech Stack:** C#, Unity (EditMode NUnit tests), headless test harness at `tools/HeadlessTests` (plain `dotnet test`, mirrors `Assets/Scripts/Simulation/**` + `Assets/Tests/EditMode/**`).

## Global Constraints

- Full spec: `docs/superpowers/specs/2026-08-16-predation-economics-design.md`.
- When `economicsEnabled` is `false`, every changed method must produce output byte-identical to the current production code — this is the hash-safety requirement (`SimulationWorld.ComputeStateHash()` covers decisions/combat, so any drift is evidence-breaking).
- New tunable consts in `PredationSystem.cs`, exact values (do not change without re-deriving against the magnitude ranges below): `InjuryCostScale = 20f`, `PursuitCostPerDistance = 0.5f`, `NormalizingEnergyScale = 150f`.
- Typical `Phenotype` magnitude ranges (from `GenomePhenotype.cs`, for sanity-checking test values): `EnergyCapacity` 60–240, `AttackPower`/`Defense` 0–~1.95, `Maneuverability` 1–3, `MeatYieldMultiplier` 0.5–1.5, `Aggression`/genes 0–1.
- `HasViableHuntingStrategy` and its two `Minimum*` consts stay completely untouched — real external caller at `SimulationWorld.cs:1345` (statistics, not hash-covered).
- Existing tests `Assets/Tests/EditMode/DecisionDiagnosticsTests.cs:54` and `Assets/Tests/EditMode/SpatialBehaviorTests.cs:140,159` call `PredationSystem.Decide` without an `economicsEnabled` argument — the new trailing parameter must default to `false` so these compile and pass unmodified.
- Unity's bundled NUnit is older than the headless harness's NuGet-restored NUnit 4.3.0 — avoid newer NUnit syntax (e.g. `Is.AnyOf` on non-`IEnumerable` args); stick to `Is.EqualTo`, `Is.GreaterThan`, `Is.LessThan`, `Is.True`/`Is.False`. Headless-passing tests are necessary but not sufficient — after merging, pull into the real Unity project (`C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim`) and check its Console for compile errors.
- Run headless tests with `cd tools/HeadlessTests && dotnet test`.

---

### Task 1: Add `PredationEconomicsEnabled` config flag

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs:87-145`
- Test: `Assets/Tests/EditMode/SimulationConfigTests.cs` (create if it doesn't already exist — check first)

**Interfaces:**
- Produces: `SimulationConfig.PredationEconomicsEnabled` (bool, get-only property), a new optional constructor parameter `bool predationEconomicsEnabled = false` inserted immediately after the existing `foragingEconomicsEnabled` parameter (constructor line 97) — later tasks read this property.

**Contract (mirror the existing `ForagingEconomicsEnabled` field exactly — same pattern, lines 97/118/139 of the current file):**

```csharp
// In the constructor parameter list, immediately after:
//     bool foragingEconomicsEnabled = false,
// add:
bool predationEconomicsEnabled = false,

// In the constructor body, immediately after:
//     ForagingEconomicsEnabled = foragingEconomicsEnabled;
// add:
PredationEconomicsEnabled = predationEconomicsEnabled;

// Immediately after the existing property:
//     public bool ForagingEconomicsEnabled { get; }
// add:
public bool PredationEconomicsEnabled { get; }
```

- [ ] **Step 1: Write the failing test**

```csharp
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class SimulationConfigTests
    {
        [Test]
        public void PredationEconomicsEnabledDefaultsToFalseAndCanBeSetToTrue()
        {
            var schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var defaultConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule);
            var enabledConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule, predationEconomicsEnabled: true);

            Assert.That(defaultConfig.PredationEconomicsEnabled, Is.False);
            Assert.That(enabledConfig.PredationEconomicsEnabled, Is.True);
        }
    }
}
```

If `SimulationConfigTests.cs` already exists, add this test method to it instead of creating a new file (check `Assets/Tests/EditMode/` first).

- [ ] **Step 2: Run test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter PredationEconomicsEnabledDefaultsToFalseAndCanBeSetToTrue`
Expected: FAIL — `predationEconomicsEnabled` is not a recognized constructor parameter name / `PredationEconomicsEnabled` does not exist.

- [ ] **Step 3: Implement the config field**

Apply the contract above to `Assets/Scripts/Simulation/Core/SimulationConfig.cs`.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd tools/HeadlessTests && dotnet test --filter PredationEconomicsEnabledDefaultsToFalseAndCanBeSetToTrue`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Tests/EditMode/SimulationConfigTests.cs
git commit -m "feat: add PredationEconomicsEnabled config flag"
```

---

### Task 2: Rewrite `PredationSystem.HuntCapability` and `Threat` with economics formula

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` (the `MinimumHuntingDiet`/`MinimumHuntingAggression` consts stay; `HuntCapability` and `Threat` methods change signature; `HasViableHuntingStrategy` and `PreferCarcassWhenUseful` overloads stay untouched)
- Create: `Assets/Tests/EditMode/PredationSystemTests.cs` (no existing test file for this class — confirmed via directory listing)

**Interfaces:**
- Consumes: nothing new from other tasks.
- Produces: `HuntCapability(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)` returning `float` in `[0,1]`; `Threat(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)` returning `float` in `[0,1]`. Task 3's `Decide` calls both with these exact signatures.

**Contract:**

```csharp
private const float MinimumHuntingDiet = 0.58f;       // unchanged, still used in the legacy branch
private const float MinimumHuntingAggression = 0.35f; // unchanged, still used in the legacy branch
private const float InjuryCostScale = 20f;
private const float PursuitCostPerDistance = 0.5f;
private const float NormalizingEnergyScale = 150f;

public static float HuntCapability(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
{
    if (!economicsEnabled)
    {
        float legacyDiet = Clamp01((attacker.MeatYieldMultiplier - 0.5f) / 1f);
        if (!HasViableHuntingStrategy(attacker)) { return 0f; }
        float legacyAdvantage = attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f);
        return Clamp01(legacyAdvantage * attacker.Aggression * legacyDiet);
    }

    float successChance = Clamp01(attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f));
    float expectedGain = defender.EnergyCapacity * attacker.MeatYieldMultiplier * successChance;
    float expectedInjuryCost = defender.AttackPower * (1f - successChance) * InjuryCostScale;
    float expectedPursuitCost = PursuitCostPerDistance * distance;
    float netEnergyValue = expectedGain - expectedInjuryCost - expectedPursuitCost;
    return Clamp01(netEnergyValue / NormalizingEnergyScale) * attacker.Aggression;
}

public static float Threat(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
{
    float huntScore = HuntCapability(attacker, defender, distance, economicsEnabled);
    if (huntScore <= 0f) { return 0f; }
    float pressure = attacker.AttackPower * (0.5f + attacker.Aggression);
    float resistance = defender.Defense + (0.25f * defender.Maneuverability) + 0.01f;
    return Clamp01(pressure / (pressure + resistance));
}
```

Delete the old two-parameter `HuntCapability(Phenotype, Phenotype)` and `Threat(Phenotype, Phenotype)` signatures entirely — Task 3 updates the only production call sites (in `Decide`), and Task 4 updates the two direct `SimulationWorld.cs` call sites, so no callers are left on the old shape after this plan completes. `HasViableHuntingStrategy` is unchanged and still public.

**Behavior table** (write one test per row; `Clamp01(x) = Math.Max(0f, Math.Min(1f, x))`):

| # | economicsEnabled | attacker (AttackPower, Defense, Maneuverability, Aggression, MeatYieldMultiplier) | defender (AttackPower, Defense, Maneuverability, EnergyCapacity) | distance | Expected `HuntCapability` |
|---|---|---|---|---|---|
| 1 | false | AttackPower=1.5, Defense=0.5, Maneuverability=1, Aggression=0.8, MeatYieldMultiplier=1.2 (diet=0.7, above 0.58 threshold) | Defense=0.5, Maneuverability=1 | (ignored) | Must equal legacy formula: `advantage = 1.5/(1.5+0.5+0.25+0.01) = 0.6725...`; `diet = Clamp01((1.2-0.5)/1) = 0.7`; result `= Clamp01(0.6725 * 0.8 * 0.7) ≈ 0.3766` |
| 2 | false | MeatYieldMultiplier=0.9 (diet=0.4, below 0.58 threshold), Aggression=0.8 | any | (ignored) | `0f` exactly (legacy gate blocks it) |
| 3 | true | AttackPower=1.9, Defense=0.1, Maneuverability=1, Aggression=0.8, MeatYieldMultiplier=1.3 | AttackPower=0.2, Defense=0.1, Maneuverability=1, EnergyCapacity=200 | 1 | Strongly favorable matchup — result must be `> 0.5` (strong attacker, big weak close prey) |
| 4 | true | AttackPower=0.3, Defense=1.8, Maneuverability=2.5, Aggression=0.8, MeatYieldMultiplier=0.6 | AttackPower=1.7, Defense=1.8, Maneuverability=2.5, EnergyCapacity=150 | 14 | Unfavorable matchup — net EV goes negative — result must equal `0f` exactly |
| 5 | true | AttackPower=1.9, Defense=0.1, Maneuverability=1, Aggression=0f, MeatYieldMultiplier=1.3 | AttackPower=0.2, Defense=0.1, Maneuverability=1, EnergyCapacity=200 | 1 | Same as row 3 but `Aggression=0` — result must equal `0f` exactly (final multiplier zeroes it) |
| 6 | true | Same attacker/defender as row 3, distance=1 vs distance=10 | — | 1 and 10 | Result at distance=1 must be strictly greater than result at distance=10 (pursuit cost increases with distance) |
| 7 | false | Same attacker/defender/params as row 1 | — | any distance | `Threat(attacker, defender, distance, false)` must equal legacy `Threat` output: `pressure = 1.5*(0.5+0.8) = 1.95`; `resistance = 0.5+0.25+0.01 = 0.76`; result `= Clamp01(1.95/(1.95+0.76)) ≈ 0.7196` |
| 8 | true | Same as row 4 (huntScore == 0) | — | 14 | `Threat(...) == 0f` exactly (gated by zero hunt score) |
| 9 | true | Same as row 3 (huntScore > 0) | — | 1 | `Threat(...) > 0f`, equals `Clamp01(pressure/(pressure+resistance))` with the same pressure/resistance formula as row 7 (distance/economics only affect the gate, not the pressure math once past it) |

- [ ] **Step 1: Write the failing tests**

```csharp
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PredationSystemTests
    {
        private static Phenotype MakePhenotype(
            float attackPower, float defense, float maneuverability, float aggression = 0.5f,
            float meatYieldMultiplier = 1f, float energyCapacity = 100f)
        {
            return new Phenotype(
                bodyMass: 1f, energyCapacity: energyCapacity, hydrationCapacity: 100f, visionRange: 8f,
                movementSpeed: 2f, perceptionRange: 8f, foodEfficiency: 1f, waterEfficiency: 1f,
                metabolicPace: 1f, basalEnergyCostMultiplier: 1f, maintenanceCost: 1f,
                attackPower: attackPower, defense: defense, maneuverability: maneuverability,
                fearResponse: 0.5f, aggression: aggression, plantFoodYieldMultiplier: 1f,
                meatYieldMultiplier: meatYieldMultiplier, memoryConfidenceDecayPerSecond: 0.05f,
                cognitionRestCostMultiplier: 1f, temperatureTolerance: 5f, learningRate: 0.5f,
                exploration: 0.5f, reproductionCooldownSeconds: 10f, reproductionEnergyCostFraction: 0.2f,
                maximumAgeSeconds: 180f, persistence: 0.5f);
        }

        [Test]
        public void LegacyHuntCapabilityMatchesTodaysFormulaWhenDietAboveThreshold()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.5f, defense: 999f, maneuverability: 999f, aggression: 0.8f, meatYieldMultiplier: 1.2f);
            Phenotype defender = MakePhenotype(attackPower: 999f, defense: 0.5f, maneuverability: 1f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 5f, economicsEnabled: false);

            Assert.That(result, Is.EqualTo(0.3766f).Within(0.001f));
        }
    }
}
```

`MakePhenotype`'s exact `Phenotype` constructor parameter order/names must be verified against `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` before writing further tests — read that file's constructor first if any parameter mismatch causes a compile error. Write the remaining 8 rows from the behavior table above as additional `[Test]` methods following this same pattern (one assertion block per row; for row 6's distance comparison, call `HuntCapability` twice with the two distance values and assert `resultAtDistance1 > resultAtDistance10`).

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter PredationSystemTests`
Expected: FAIL — `HuntCapability`/`Threat` don't accept 4 arguments yet.

- [ ] **Step 3: Implement the new formula**

Apply the contract above to `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`, replacing the old `HuntCapability` and `Threat` methods.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter PredationSystemTests`
Expected: PASS (all 9 test methods)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/PredationSystemTests.cs
git commit -m "feat: continuous expected-value hunting formula behind PredationEconomicsEnabled"
```

---

### Task 3: Update `PredationSystem.Decide` to thread the flag and drop double-counted distance

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PredationSystem.cs` (both `Decide` overloads, lines ~14-53 in the pre-change file)

**Interfaces:**
- Consumes: `HuntCapability(Phenotype, Phenotype, float, bool)` and `Threat(Phenotype, Phenotype, float, bool)` from Task 2.
- Produces: `Decide(CreatureNeeds, Phenotype, Phenotype, CreatureObservation, CreatureDecision, bool economicsEnabled = false)` and `Decide(CreatureNeeds, Phenotype, Phenotype, CreatureObservation, CreatureDecision, ref DecisionDiagnostics, bool economicsEnabled = false)` — Task 4's `SimulationWorld.cs` call sites use the second overload with an explicit `economicsEnabled` argument.

**Contract:**

```csharp
public static CreatureDecision Decide(
    CreatureNeeds needs, Phenotype self, Phenotype other,
    CreatureObservation otherObservation, CreatureDecision survivalDecision,
    bool economicsEnabled = false)
{
    DecisionDiagnostics ignoredDiagnostics = default;
    return Decide(needs, self, other, otherObservation, survivalDecision, ref ignoredDiagnostics, economicsEnabled);
}

public static CreatureDecision Decide(
    CreatureNeeds needs, Phenotype self, Phenotype other,
    CreatureObservation otherObservation, CreatureDecision survivalDecision,
    ref DecisionDiagnostics diagnostics, bool economicsEnabled = false)
{
    if (!otherObservation.IsValid) { return survivalDecision; }

    float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + otherObservation.Distance);
    float hunger = 1f - (needs.Energy / self.EnergyCapacity);
    float threat = Threat(other, self, otherObservation.Distance, economicsEnabled) * self.FearResponse * distanceAvailability;
    float hunt = HuntCapability(self, other, otherObservation.Distance, economicsEnabled) * hunger * distanceAvailability;
    diagnostics = diagnostics.WithPredationScores(threat, hunt);

    if (threat > Math.Max(0.10f, hunt) && threat > survivalDecision.Score)
    {
        return new CreatureDecision(CreatureAction.Flee, -1, threat, targetCreatureId: otherObservation.CreatureId);
    }
    if (hunt > survivalDecision.Score && hunt >= 0.10f)
    {
        return new CreatureDecision(CreatureAction.SeekPrey, -1, hunt, targetCreatureId: otherObservation.CreatureId);
    }
    return survivalDecision;
}
```

Note the `economicsEnabled` parameter defaults to `false` on both overloads — this is why `DecisionDiagnosticsTests.cs:54` and `SpatialBehaviorTests.cs:140,159` (which call `Decide` without this argument) keep compiling and keep producing their existing expected outputs unmodified.

**Behavior table:**

| # | Setup | Expected |
|---|---|---|
| 1 | Call the 5-arg overload with `economicsEnabled` omitted, using the same phenotypes/observation that `SpatialBehaviorTests.cs:140` already uses today | Output identical to what that existing test already asserts — run the existing test suite to confirm, no new test needed for this row (covered by regression) |
| 2 | `economicsEnabled: true`, attacker/defender/distance from Task 2's behavior-table row 3 (strongly favorable hunt), `needs.Energy` low (hungry), `survivalDecision.Score = 0.05f` | Returned decision's `Action == CreatureAction.SeekPrey` and `TargetCreatureId == otherObservation.CreatureId` |
| 3 | `economicsEnabled: true`, attacker/defender/distance from Task 2's behavior-table row 4 (unfavorable, huntScore and threat both 0), `survivalDecision.Score = 0.05f` | Returned decision equals `survivalDecision` unchanged (neither flee nor seek-prey threshold met) |

- [ ] **Step 1: Write the failing tests**

```csharp
[Test]
public void EconomicsEnabledDecideChoosesSeekPreyForAFavorableMatchup()
{
    Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
    Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
    CreatureNeeds needs = new CreatureNeeds(energy: 10f, hydration: 100f);
    CreatureObservation observation = new CreatureObservation(creatureIndex: 1, creatureId: default, distance: 1f, isValid: true);
    CreatureDecision survival = new CreatureDecision(CreatureAction.Wander, -1, 0.05f);

    CreatureDecision decision = PredationSystem.Decide(needs, attacker, defender, observation, survival, economicsEnabled: true);

    Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
}
```

Check `CreatureNeeds`, `CreatureObservation`, and `CreatureDecision`'s exact constructor signatures in `Assets/Scripts/Simulation/Core/` before writing this — adjust field/constructor-argument names to match if they differ from this sketch (do not guess at defaults; read the actual struct definitions). Write the second test method (unfavorable matchup → decision unchanged) following the same pattern with Task 2's row-4 phenotypes.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter Decide`
Expected: FAIL — `Decide` doesn't accept an `economicsEnabled` argument yet.

- [ ] **Step 3: Implement**

Apply the contract above to `Assets/Scripts/Simulation/Behavior/PredationSystem.cs`.

- [ ] **Step 4: Run tests to verify they pass, and confirm the full existing suite still passes**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — all tests including the new ones and the pre-existing `DecisionDiagnosticsTests`/`SpatialBehaviorTests` that call `Decide` without the new argument.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PredationSystem.cs Assets/Tests/EditMode/PredationSystemTests.cs
git commit -m "feat: thread economicsEnabled through PredationSystem.Decide"
```

---

### Task 4: Wire `SimulationWorld.cs` call sites to the config flag

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (three call sites: line 667 in `TickDecisions`, line 837 in `TickDecisions`, line 1158 in `TickCombat`; line 1345 in `TickStatistics` is untouched)

**Interfaces:**
- Consumes: `SimulationConfig.PredationEconomicsEnabled` (Task 1), `PredationSystem.Threat(Phenotype, Phenotype, float, bool)` and `PredationSystem.Decide(..., ref DecisionDiagnostics, bool)` (Tasks 2-3).

**Contract:**

Call site at line 667 (inside the `DecisionPolicyVersion.IntentUtilityV1` branch) — change:
```csharp
threatIntensity = PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype);
```
to:
```csharp
threatIntensity = PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype, other.Distance, Config.PredationEconomicsEnabled);
```

Call site at line 837 (inside the `FounderProfile.PredationVariation && DecisionPolicyVersion.Legacy` branch) — change:
```csharp
decision = PredationSystem.Decide(
    Creatures.GetNeedsAt(index),
    phenotype,
    Creatures.GetPhenotypeAt(other.CreatureIndex),
    other,
    decision,
    ref diagnostics);
```
to:
```csharp
decision = PredationSystem.Decide(
    Creatures.GetNeedsAt(index),
    phenotype,
    Creatures.GetPhenotypeAt(other.CreatureIndex),
    other,
    decision,
    ref diagnostics,
    Config.PredationEconomicsEnabled);
```

Call site at line 1158 (inside `TickCombat`) — the distance is currently computed inline and discarded; store it in a local first. Change:
```csharp
if (SimVector2.Distance(attackerMovement.Position, defenderMovement.Position) > 1.1f)
{
    continue;
}

Phenotype attacker = Creatures.GetPhenotypeAt(index);
Phenotype defender = Creatures.GetPhenotypeAt(targetIndex);
float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender));
```
to:
```csharp
float engagementDistance = SimVector2.Distance(attackerMovement.Position, defenderMovement.Position);
if (engagementDistance > 1.1f)
{
    continue;
}

Phenotype attacker = Creatures.GetPhenotypeAt(index);
Phenotype defender = Creatures.GetPhenotypeAt(targetIndex);
float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender, engagementDistance, Config.PredationEconomicsEnabled));
```

**Behavior table:**

| # | Setup | Expected |
|---|---|---|
| 1 | Build a `SimulationWorld` with `PredationEconomicsEnabled: false` (default), `FounderProfile.PredationVariation`, `DecisionPolicyVersion.Legacy`, seed a predator and prey creature close together, run several `Tick()` calls | `ComputeStateHash()` output for this run matches a hash captured from the same setup on the `main` branch commit before this plan's changes (regression check — confirms the flag-off path is byte-identical) |
| 2 | Same setup but `PredationEconomicsEnabled: true` | Simulation runs without exceptions; at least one `SeekPrey` or `Flee` decision occurs across the run for a viable predator/prey pair (sanity check the new path is live and producing decisions, not asserting exact values since this is integration-level) |

- [ ] **Step 1: Write the failing test**

```csharp
[Test]
public void PredationEconomicsDisabledProducesIdenticalHashToPreExistingLegacyBehavior()
{
    SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
    var config = new SimulationConfig(
        worldSeed: 99, initialPopulation: 2, schedule: schedule,
        founderProfile: FounderProfile.PredationVariation,
        predationEconomicsEnabled: false);
    var world = new SimulationWorld(config);

    for (int i = 0; i < 50; i++) { world.Tick(); }

    // This value must be captured by running the identical setup against the
    // pre-change PredationSystem.cs on main (git stash this task's diff, run once, note the hash, restore).
    Assert.That(world.ComputeStateHash(), Is.EqualTo(EXPECTED_LEGACY_HASH));
}
```

Before writing the real assertion, check `SimulationWorld`'s actual constructor and `Tick`/`ComputeStateHash` method names against the file (do not guess). To get `EXPECTED_LEGACY_HASH`: run this exact test body (with a placeholder `Assert.That(true)`) against the `main` branch commit that exists before this task's changes are applied, print `world.ComputeStateHash()`, and hard-code that printed value as the constant. This is the only way to pin a true "unchanged" regression value.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter PredationEconomicsDisabledProducesIdenticalHashToPreExistingLegacyBehavior`
Expected: FAIL initially with a compile error (constructor doesn't accept `predationEconomicsEnabled` before Task 1) — by this point in the plan Task 1-3 are already done, so it should compile; if `EXPECTED_LEGACY_HASH` is still a placeholder, expected failure is a hash mismatch until the real value is filled in per Step 1's instructions.

- [ ] **Step 3: Implement**

Apply the three call-site changes above to `Assets/Scripts/Simulation/Core/SimulationWorld.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite, including the new hash-regression test and a second test for row 2 (economics-enabled sanity check, write following the same setup pattern with `predationEconomicsEnabled: true` and asserting the run completes with no exceptions and `Creatures.Count >= 0` after 50 ticks — a smoke test, not a hash pin, since row 2's behavior is deliberately not yet tuned/frozen).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/*.cs
git commit -m "feat: wire PredationEconomicsEnabled through SimulationWorld call sites"
```

---

## Post-plan verification (not a task — do after all 4 tasks are reviewed and merged)

1. Push to `origin/main`.
2. `cd` to `C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim` and `git pull`.
3. Open Unity, check Console for compile errors (catches any NUnit-version API gap the headless harness wouldn't).
4. Commit any newly-generated `.meta` files for `PredationSystemTests.cs` / `SimulationConfigTests.cs` from the real Unity project, push.
5. Report to the user: this fix ships with the flag **off** by default — the predation demo (`P` keybind / `PredationVariation` founder profile) will look unchanged until someone deliberately builds a scenario with `predationEconomicsEnabled: true`. That's a deliberate follow-up, not part of this plan (see spec's "Out of scope").

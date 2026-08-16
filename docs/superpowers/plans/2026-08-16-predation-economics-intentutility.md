# Predation Economics for IntentUtilityV1 Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the remaining B-5 gap — `DecisionSystem.ScorePredation` (used by the `IntentUtilityV1` decision policy, which is what the live `P`-keybind predator demo actually runs) still calls `PredationSystem.HuntCapability`'s legacy 2-arg shim unconditionally, so `PredationEconomicsEnabled` has no effect there. Thread the flag through so it does.

**Architecture:** Add a trailing `bool economicsEnabled = false` parameter to `DecisionSystem.ScorePredation` and both `DecideIntentUtilityV1` overloads, mirroring the exact `distanceAvailability` no-op pattern already used in `PredationSystem.Decide` (B-5's original fix). Source the value from `Config.PredationEconomicsEnabled` at the single production call site in `SimulationWorld.cs:709`.

**Tech Stack:** C#, Unity EditMode NUnit tests, headless test harness (`tools/HeadlessTests`, plain `dotnet test`).

## Global Constraints

- When `economicsEnabled` is `false` (the default), output must be byte-identical to current production behavior — same hash-safety requirement as the original B-5 plan.
- **Critical positional-argument hazard**: `SimulationWorld.cs:709`'s call to `DecisionSystem.DecideIntentUtilityV1` passes ALL 22 arguments positionally (no named arguments) — see exact current call below. Inserting a new parameter into `DecideIntentUtilityV1`'s signature anywhere before the end WITHOUT also updating this call site would silently misalign every argument after the insertion point (e.g. two adjacent `bool` parameters would still type-check, just mean the wrong thing) — this would NOT be a compile error, it would be a silent logic bug. This task updates that call site explicitly as part of Task 1 — do not rely on the new parameter's default value to make the existing call "just work."
- Test call sites in `Assets/Tests/EditMode/SpatialBehaviorTests.cs` (lines 504, 523, 544, 545, 570, 585, 599) all use named arguments from `carcass:` onward and are safe to leave unmodified (the new parameter's default of `false` covers them) — EXCEPT verify this is actually true by reading the file before assuming.

---

### Task 1: Thread `economicsEnabled` through `ScorePredation` and `DecideIntentUtilityV1`

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` (both `DecideIntentUtilityV1` overloads at lines 272-295 and 297-357, and `ScorePredation` at lines 417-444)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (the call site at lines 709-732)
- Test: `Assets/Tests/EditMode/PredationSystemTests.cs` or a new `DecisionSystemTests.cs` if more appropriate — check what already exists for `DecisionSystem` first

**Interfaces:**
- Consumes: `PredationSystem.HuntCapability(Phenotype, Phenotype, float, bool)` (already exists from the original B-5 plan).
- Produces: `ScorePredation(..., bool economicsEnabled)`, `DecideIntentUtilityV1(..., bool economicsEnabled = false, ...)` — no other task depends on this.

**Exact current `ScorePredation` (before this task):**
```csharp
private static void ScorePredation(
    CreatureNeeds needs,
    Genome genome,
    Phenotype self,
    Phenotype other,
    CreatureObservation observation,
    float threatIntensity,
    ref DecisionCandidateBuffer candidates)
{
    if (!observation.IsValid)
    {
        return;
    }

    float distanceAvailability = 1f / (1f + observation.Distance);
    float hunger = Urgency(needs.Energy, self.EnergyCapacity);
    float fleeScore = Math.Max(0f, threatIntensity * genome.RiskAversion * distanceAvailability);
    float huntScore = PredationSystem.HuntCapability(self, other) * hunger * distanceAvailability;
    if (fleeScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.Flee, -1, observation.CreatureId, fleeScore));
    }

    if (huntScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekPrey, -1, observation.CreatureId, huntScore));
    }
}
```

**New `ScorePredation`:**
```csharp
private static void ScorePredation(
    CreatureNeeds needs,
    Genome genome,
    Phenotype self,
    Phenotype other,
    CreatureObservation observation,
    float threatIntensity,
    ref DecisionCandidateBuffer candidates,
    bool economicsEnabled)
{
    if (!observation.IsValid)
    {
        return;
    }

    float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + observation.Distance);
    float hunger = Urgency(needs.Energy, self.EnergyCapacity);
    float fleeScore = Math.Max(0f, threatIntensity * genome.RiskAversion * distanceAvailability);
    float huntScore = PredationSystem.HuntCapability(self, other, observation.Distance, economicsEnabled) * hunger * distanceAvailability;
    if (fleeScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.Flee, -1, observation.CreatureId, fleeScore));
    }

    if (huntScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekPrey, -1, observation.CreatureId, huntScore));
    }
}
```

Note `fleeScore`'s formula is untouched (it doesn't call `HuntCapability` directly — `threatIntensity` is computed by the caller via `PredationSystem.Threat`, already flag-aware since `SimulationWorld.cs:667` was fixed in the original B-5 plan). Only `distanceAvailability`'s no-op condition and the `HuntCapability` call itself change.

**Both `DecideIntentUtilityV1` overloads gain a trailing `bool economicsEnabled = false` parameter, inserted immediately before `long tick`:**

Short (5→6-arg-shape) overload — change:
```csharp
        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            long tick,
            out DecisionDiagnostics diagnostics)
        {
            return DecideIntentUtilityV1(
                needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
                cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
                default, default, default, default, default, false, tick, out diagnostics);
        }
```
to:
```csharp
        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            bool economicsEnabled,
            long tick,
            out DecisionDiagnostics diagnostics)
        {
            return DecideIntentUtilityV1(
                needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
                cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
                default, default, default, default, default, false, economicsEnabled, tick, out diagnostics);
        }
```
(Note: this overload's new `economicsEnabled` parameter has NO default value — it's placed here without one because the full 22-arg overload right below it also needs one, and this file's existing convention doesn't default this specific style of boolean-flag parameter on the short overload either; check `predationEnabled`/`physiologyEnabled` above it — they have no defaults on this overload. Match that convention: no default here. This overload currently has zero callers outside tests per the earlier grep — if the implementer's own grep finds none, this is safe; if it finds a caller relying on omitting trailing args, give that parameter a default instead and flag it.)

Full (22→23-arg) overload — change the signature to insert `bool economicsEnabled,` immediately before `long tick,`:
```csharp
        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            ReproductionState reproduction,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            Phenotype matePhenotype,
            ReproductionState mateReproduction,
            bool reproductionEnabled,
            bool economicsEnabled,
            long tick,
            out DecisionDiagnostics diagnostics)
```
And change the `ScorePredation` call inside its body:
```csharp
            if (predationEnabled)
            {
                ScoreCarcass(needs, phenotype, resources, carcass, ref candidates);
                ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled);
            }
```

**`SimulationWorld.cs:709-732` — exact current call (verify against the live file first, line numbers may have drifted slightly since the review that found this):**
```csharp
                    decision = DecisionSystem.DecideIntentUtilityV1(
                        Creatures.GetNeedsAt(index),
                        Creatures.GetGenomeAt(index),
                        phenotype,
                        Resources,
                        movement.Position,
                        foodCandidates,
                        waterCandidates,
                        carcass,
                        Creatures.GetMemoryRefAt(index),
                        Config.CognitionEnabled,
                        other,
                        threatIntensity,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        Config.FounderProfile == FounderProfile.PredationVariation,
                        Config.PhysiologyEnabled,
                        Creatures.GetReproductionRefAt(index),
                        other,
                        other.IsValid ? Creatures.GetNeedsAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetReproductionRefAt(other.CreatureIndex) : default,
                        true,
                        tick,
                        out diagnostics);
```
Change to (inserting `Config.PredationEconomicsEnabled` immediately before `tick`, matching the new parameter position — everything else stays in the exact same order):
```csharp
                    decision = DecisionSystem.DecideIntentUtilityV1(
                        Creatures.GetNeedsAt(index),
                        Creatures.GetGenomeAt(index),
                        phenotype,
                        Resources,
                        movement.Position,
                        foodCandidates,
                        waterCandidates,
                        carcass,
                        Creatures.GetMemoryRefAt(index),
                        Config.CognitionEnabled,
                        other,
                        threatIntensity,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        Config.FounderProfile == FounderProfile.PredationVariation,
                        Config.PhysiologyEnabled,
                        Creatures.GetReproductionRefAt(index),
                        other,
                        other.IsValid ? Creatures.GetNeedsAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetReproductionRefAt(other.CreatureIndex) : default,
                        true,
                        Config.PredationEconomicsEnabled,
                        tick,
                        out diagnostics);
```

**Behavior table:**

| # | Setup | Expected |
|---|---|---|
| 1 | `ScorePredation` called with `economicsEnabled: false`, same phenotypes/observation as any existing passing `SpatialBehaviorTests.cs` predation case | Output identical to current behavior (regression — covered by running the existing suite, no new test needed for this row) |
| 2 | `ScorePredation` called with `economicsEnabled: true`, a strongly favorable attacker/defender pair and close distance (reuse the exact phenotype values from the original B-5 plan's Task 2 behavior-table row 3: attacker AttackPower=1.9, Defense=0.1, Maneuverability=1, Aggression=0.8, MeatYieldMultiplier=1.3; defender AttackPower=0.2, Defense=0.1, Maneuverability=1, EnergyCapacity=200; distance=1) | A `SeekPrey` candidate is added with `Score > 0` (use `MakePhenotype` from `PredationSystemTests.cs` if reusing it here, or an equivalent local helper — check what's already available before writing a new one) |
| 3 | `ScorePredation` called with `economicsEnabled: true`, an unfavorable pair (reuse Task 2's row 4: attacker AttackPower=0.3, Defense=1.8, Maneuverability=2.5, Aggression=0.8, MeatYieldMultiplier=0.6; defender AttackPower=1.7, Defense=1.8, Maneuverability=2.5, EnergyCapacity=150; distance=14) | No `SeekPrey` candidate added (huntScore is 0, same as the original B-5 formula's row 4 result) |

- [ ] **Step 1: Write the failing tests**

Check whether `Assets/Tests/EditMode/DecisionSystemTests.cs` already exists. If not, create it. Write two `[Test]` methods for behavior-table rows 2 and 3. `ScorePredation` is `private` — test it indirectly through `DecisionSystem.DecideIntentUtilityV1`'s public 22-arg overload with `predationEnabled: true, economicsEnabled: true`, checking the returned `CreatureDecision.Action` is `SeekPrey` (row 2) or NOT `SeekPrey` (row 3, e.g. `Wander` since no other candidate would qualify with default/empty resources). Read `Assets/Tests/EditMode/SpatialBehaviorTests.cs:585` first for a working example of how existing tests construct a full `DecideIntentUtilityV1` call with a predation observation — mirror that pattern, using `default` for the unrelated resource/reproduction/memory parameters as that example does.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter DecisionSystemTests` (or whatever the new test class is named)
Expected: FAIL — `DecideIntentUtilityV1` doesn't accept an `economicsEnabled` argument yet.

- [ ] **Step 3: Implement**

Apply all the contract changes above (`ScorePredation`, both `DecideIntentUtilityV1` overloads, and the `SimulationWorld.cs:709` call site). Re-verify the exact current line numbers/content in the live files before editing — this plan's line numbers may have shifted since the review that identified this gap.

- [ ] **Step 4: Run tests to verify they pass, and confirm the full suite still passes**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: PASS — full suite including the 278 tests from the original B-5 plan (all `SpatialBehaviorTests.cs` calls to `DecideIntentUtilityV1` must still compile and pass unmodified, relying on `economicsEnabled`'s default of `false`) plus the 2 new tests.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionSystemTests.cs
git commit -m "feat: thread PredationEconomicsEnabled through IntentUtilityV1's predation scoring"
```

---

## Post-plan verification (not a task)

1. Push to `origin/main`, pull into the real Unity project (`C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim`), check Console for compile errors, commit generated `.meta` files.
2. Once merged, flip `predationEconomicsEnabled: true` in `Prototype1Presenter.ResetPredationSimulation()` (the `P`-keybind demo, `Assets/Scripts/Presentation/Prototype1Presenter.cs:298-310`) as a separate small follow-up so the user can visually test — NOT part of this plan, since that presenter file wasn't read/scoped here.

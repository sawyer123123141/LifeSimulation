# Decision Staggering (B-8) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spread decision-making cost across ticks and break population-wide lockstep by phase-staggering `TickDecisions` per creature index, gated behind a new `SimulationConfig.DecisionStaggerEnabled` flag defaulting to `false`.

**Architecture:** `SimulationWorld.Step()`'s single `IsDue`-gated call to `TickDecisions` becomes conditional on the new flag: unconditional-every-tick when the flag is on, unchanged when off. `TickDecisions`'s per-creature loop gains a skip check as its first line, active only when the flag is on.

**Tech Stack:** C#, Unity, headless NUnit test harness (`tools/HeadlessTests`, plain `dotnet test`, .NET 8).

## Global Constraints

- `SimulationConfig.DecisionStaggerEnabled` defaults to `false` and is added to the constructor's optional-parameter list in the same style as `predationEconomicsEnabled` — a plain `bool` param with a `bool` default, assigned to a `{ get; }` property.
- When the flag is `false`, behavior must be byte-identical to today's: this must be proven by a hash-regression test against a scenario's pinned `ComputeStateHash()` value.
- `ComputeStateHash()` (`SimulationWorld.cs:329-457`) is NOT modified by this task — it already includes `decision.DecisionTick`, which is exactly what makes the flag-off path's hash stability worth testing and the flag-on path's hash divergence expected and out of scope for pinning.
- Do not touch `MemoryState`, `MemorySystem`, `ForagingEconomics`, or any decision-scoring formula — this task only changes *when* a creature's decision runs, never *what* it computes.

---

### Task 1: Add DecisionStaggerEnabled flag and phase-stagger TickDecisions

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (constructor + property, mirroring `predationEconomicsEnabled`/`PredationEconomicsEnabled` at lines 98/120/142)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs:266-269` (the `Step()` call site)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs:623-625` (the `TickDecisions` loop header)
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Consumes: `SimulationConfig.Schedule.BaseFrequencyHz`, `SimulationConfig.Schedule.DecisionsHz` (existing, `SimulationConfig.cs:41,45`), `SimulationWorld.IsDue(long tick, int frequencyHz)` (existing private method, `SimulationWorld.cs:471-475`, unchanged).
- Produces: `SimulationConfig.DecisionStaggerEnabled` (new public bool property) — no other task in this plan depends on it, this is the only task.

This is the complete, exact code for every changed member.

**`SimulationConfig.cs` constructor** — current (`SimulationConfig.cs:87-108`):

```csharp
public SimulationConfig(
    int worldSeed,
    int initialPopulation,
    SimulationSchedule schedule,
    int maximumPopulation = 1000,
    FounderProfile founderProfile = FounderProfile.Prototype1,
    bool cognitionEnabled = false,
    bool physiologyEnabled = false,
    DecisionPolicyVersion decisionPolicyVersion = DecisionPolicyVersion.Legacy,
    bool plantCohortsEnabled = false,
    bool foragingEconomicsEnabled = false,
    bool predationEconomicsEnabled = false,
    float handlingSeconds = DefaultHandlingSeconds,
    float referenceGain = DefaultReferenceGain,
    float commitmentStrength = DefaultCommitmentStrength,
    float commitmentHalfLifeSeconds = DefaultCommitmentHalfLifeSeconds,
    float giveUpSensitivity = DefaultGiveUpSensitivity,
    int minimumMemorySlots = DefaultMinimumMemorySlots,
    int additionalMemorySlots = DefaultAdditionalMemorySlots,
    float samePlaceRadius = DefaultSamePlaceRadius,
    float expectedIntakeRate = DefaultExpectedIntakeRate,
    float threatFalloffDistance = DefaultThreatFalloffDistance)
{
    WorldSeed = worldSeed;
    InitialPopulation = initialPopulation;
    Schedule = schedule;
    MaximumPopulation = maximumPopulation;
    FounderProfile = founderProfile;
    CognitionEnabled = cognitionEnabled;
    PhysiologyEnabled = physiologyEnabled;
    DecisionPolicyVersion = decisionPolicyVersion;
    PlantCohortsEnabled = plantCohortsEnabled;
    ForagingEconomicsEnabled = foragingEconomicsEnabled;
    PredationEconomicsEnabled = predationEconomicsEnabled;
    HandlingSeconds = handlingSeconds;
    ReferenceGain = referenceGain;
    CommitmentStrength = commitmentStrength;
    CommitmentHalfLifeSeconds = commitmentHalfLifeSeconds;
    GiveUpSensitivity = giveUpSensitivity;
    MinimumMemorySlots = minimumMemorySlots;
    AdditionalMemorySlots = additionalMemorySlots;
    SamePlaceRadius = samePlaceRadius;
    ExpectedIntakeRate = expectedIntakeRate;
    ThreatFalloffDistance = threatFalloffDistance;
}
```

New — add `decisionStaggerEnabled` as the new LAST optional parameter (after `threatFalloffDistance`), and assign it in the body:

```csharp
public SimulationConfig(
    int worldSeed,
    int initialPopulation,
    SimulationSchedule schedule,
    int maximumPopulation = 1000,
    FounderProfile founderProfile = FounderProfile.Prototype1,
    bool cognitionEnabled = false,
    bool physiologyEnabled = false,
    DecisionPolicyVersion decisionPolicyVersion = DecisionPolicyVersion.Legacy,
    bool plantCohortsEnabled = false,
    bool foragingEconomicsEnabled = false,
    bool predationEconomicsEnabled = false,
    float handlingSeconds = DefaultHandlingSeconds,
    float referenceGain = DefaultReferenceGain,
    float commitmentStrength = DefaultCommitmentStrength,
    float commitmentHalfLifeSeconds = DefaultCommitmentHalfLifeSeconds,
    float giveUpSensitivity = DefaultGiveUpSensitivity,
    int minimumMemorySlots = DefaultMinimumMemorySlots,
    int additionalMemorySlots = DefaultAdditionalMemorySlots,
    float samePlaceRadius = DefaultSamePlaceRadius,
    float expectedIntakeRate = DefaultExpectedIntakeRate,
    float threatFalloffDistance = DefaultThreatFalloffDistance,
    bool decisionStaggerEnabled = false)
{
    WorldSeed = worldSeed;
    InitialPopulation = initialPopulation;
    Schedule = schedule;
    MaximumPopulation = maximumPopulation;
    FounderProfile = founderProfile;
    CognitionEnabled = cognitionEnabled;
    PhysiologyEnabled = physiologyEnabled;
    DecisionPolicyVersion = decisionPolicyVersion;
    PlantCohortsEnabled = plantCohortsEnabled;
    ForagingEconomicsEnabled = foragingEconomicsEnabled;
    PredationEconomicsEnabled = predationEconomicsEnabled;
    HandlingSeconds = handlingSeconds;
    ReferenceGain = referenceGain;
    CommitmentStrength = commitmentStrength;
    CommitmentHalfLifeSeconds = commitmentHalfLifeSeconds;
    GiveUpSensitivity = giveUpSensitivity;
    MinimumMemorySlots = minimumMemorySlots;
    AdditionalMemorySlots = additionalMemorySlots;
    SamePlaceRadius = samePlaceRadius;
    ExpectedIntakeRate = expectedIntakeRate;
    ThreatFalloffDistance = threatFalloffDistance;
    DecisionStaggerEnabled = decisionStaggerEnabled;
}
```

Add the property immediately after `ThreatFalloffDistance` (`SimulationConfig.cs:152`):

```csharp
public float ThreatFalloffDistance { get; }
public bool DecisionStaggerEnabled { get; }
```

**`SimulationWorld.cs` `Step()` call site** — current (`SimulationWorld.cs:266-269`):

```csharp
if (IsDue(nextTick, Config.Schedule.DecisionsHz))
{
    TickDecisions(nextTick);
}
```

New:

```csharp
if (Config.DecisionStaggerEnabled || IsDue(nextTick, Config.Schedule.DecisionsHz))
{
    TickDecisions(nextTick);
}
```

**`SimulationWorld.cs` `TickDecisions` loop header** — current (`SimulationWorld.cs:623-626`):

```csharp
private void TickDecisions(long tick)
{
    for (int index = 0; index < Creatures.Count; index++)
    {
```

New — insert the interval computation and skip check between the method opening and the existing loop body (nothing inside the loop body changes):

```csharp
private void TickDecisions(long tick)
{
    int interval = Config.Schedule.BaseFrequencyHz / Config.Schedule.DecisionsHz;
    for (int index = 0; index < Creatures.Count; index++)
    {
        if (Config.DecisionStaggerEnabled && (tick + index) % interval != 0)
        {
            continue;
        }

```

Everything from the current line `MovementState movement = Creatures.GetMovementAt(index);` onward is unchanged — only these lines are inserted before it.

**Behavior/verification description:**

*Flag on, staggering observed:* with `SimulationSchedule(baseFrequencyHz: 60, movementHz: 60, perceptionHz: 30, needsHz: 10, decisionsHz: 15, resourcesHz: 10, reproductionHz: 5, statisticsHz: 1)` (`interval = 60 / 15 = 4`) and `initialPopulation: 4`, `decisionStaggerEnabled: true`, running exactly 4 `Step()` calls (`tick` goes 1, 2, 3, 4) causes each of the 4 creatures to get its first-ever `DecisionTick` on a *different* one of those 4 ticks: creature index 3 decides at tick 1 (`(1+3)%4==0`), index 2 at tick 2, index 1 at tick 3, index 0 at tick 4. With the flag off (today's behavior), all 4 creatures would decide together at tick 4 (the first tick where `tick % interval == 0`, per the pre-existing `IsDue` check) — this task's test only needs to prove the flag-on case produces 4 distinct `DecisionTick` values, not re-derive the flag-off case (already covered by the hash-regression test below).

*Flag off, hash-regression:* same methodology as the existing `ExpectedLegacyHash` test in `CoreSimulationTests.cs:826-847` (from the B-5 predation-economics task) — compute the expected hash by checking out a throwaway git worktree at the commit immediately before this task's changes, running the exact scenario below for 50 ticks, and reading `world.ComputeStateHash()`. Hardcode that value as a new `private const ulong` in the test class, following the exact comment style of `ExpectedLegacyHash` (cite the commit hash this was captured from and why it proves the flag-off path is unaffected).

- [ ] **Step 1: Write the failing tests**

Append to `Assets/Tests/EditMode/CoreSimulationTests.cs`, inside the `CoreSimulationTests` class:

```csharp
[Test]
public void DecisionStaggerEnabledSpreadsDecisionsAcrossDistinctTicks()
{
    SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 15, 10, 5, 1);
    var config = new SimulationConfig(
        worldSeed: 7, initialPopulation: 4, schedule: schedule,
        decisionStaggerEnabled: true);
    var world = new SimulationWorld(config);

    for (int i = 0; i < 4; i++) { world.Step(config.FixedDeltaTime); }

    var decisionTicks = new System.Collections.Generic.HashSet<long>();
    for (int index = 0; index < world.CreatureCount; index++)
    {
        decisionTicks.Add(world.Creatures.GetDecisionAt(index).DecisionTick);
    }

    Assert.That(decisionTicks.Count, Is.EqualTo(4));
}
```

Write the flag-off hash-regression test yourself, following this exact template (fill in `<CAPTURED_HASH>` by running the scenario against a throwaway worktree checked out at the commit immediately before your changes to this file, as described in the behavior/verification section above — this mirrors `CoreSimulationTests.cs:826-847`'s existing pattern for `ExpectedLegacyHash` exactly, including its comment style):

```csharp
// Captured from the pre-decision-staggering commit <FILL_IN_COMMIT_HASH>, by running this
// exact setup (with decisionStaggerEnabled omitted, since that constructor parameter did not
// exist yet) for 50 ticks and reading world.ComputeStateHash(). Pinning this value confirms
// that adding Config.DecisionStaggerEnabled and its call-site checks in SimulationWorld.cs is
// byte-identical to prior behavior when the flag is left at its default (false).
private const ulong ExpectedDecisionStaggerDisabledHash = <CAPTURED_HASH>UL;

[Test]
public void DecisionStaggerDisabledProducesIdenticalHashToPreExistingBehavior()
{
    SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 15, 10, 5, 1);
    var config = new SimulationConfig(
        worldSeed: 7, initialPopulation: 4, schedule: schedule,
        decisionStaggerEnabled: false);
    var world = new SimulationWorld(config);

    for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

    Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedDecisionStaggerDisabledHash));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~DecisionStaggerEnabledSpreadsDecisionsAcrossDistinctTicks|FullyQualifiedName~DecisionStaggerDisabledProducesIdenticalHashToPreExistingBehavior"`

Expected: both FAIL to compile initially (`decisionStaggerEnabled` doesn't exist on `SimulationConfig` yet). Add the `SimulationConfig` change first if needed to get them to compile, then confirm `DecisionStaggerEnabledSpreadsDecisionsAcrossDistinctTicks` fails on the assertion (`decisionTicks.Count` will be 1, not 4, since without the `TickDecisions` change every creature that decides at all still decides in lockstep on the same tick).

- [ ] **Step 3: Implement the changes**

Apply the exact changes shown above to `SimulationConfig.cs` and `SimulationWorld.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the two new ones. Total count should be 285 (283 existing + 2 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Phase-stagger creature decisions behind DecisionStaggerEnabled flag"
```

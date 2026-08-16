# Multi-Threat Perception for IntentUtilityV1 (C-3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `IntentUtilityV1` the ability to react to up to 4 simultaneously visible creatures for flee/hunt scoring (best-of-K per action), instead of only the single nearest one, gated behind `SimulationConfig.MultiThreatPerceptionEnabled` (default `false`).

**Architecture:** Task 1 adds a standalone, independently-testable top-K creature perception query to `PerceptionSystem` (`CreatureCandidateBuffer` + `FindOtherCreatures`), mirroring the existing `ResourceCandidateBuffer`/`FindAvailableResources` pattern exactly. Task 2 wires it into `DecisionSystem` (a new `PredationCandidateBuffer` + `ScorePredationMulti`, and new trailing parameters on `DecideIntentUtilityV1`) and `SimulationWorld.cs`'s call site, gated by the new flag.

**Tech Stack:** C#, Unity, headless NUnit test harness (`tools/HeadlessTests`, plain `dotnet test`, .NET 8).

## Global Constraints

- `FindNearestOtherCreature` (`PerceptionSystem.cs:152-213`) is NOT modified — `Legacy` continues to call it exactly as today.
- The single nearest-creature `other`/`threatIntensity`/`otherPhenotype` computation in `SimulationWorld.cs`'s `TickDecisions` (lines 663-676, 726-736) is NOT removed or altered — it still feeds `ScoreResourceCandidates`'s danger penalty and the `mate`/`mateNeeds`/`matePhenotype`/`mateReproduction` arguments exactly as today (yes, `other` is reused as both the threat/predation candidate AND the mate candidate in the existing call site — this plan does not change that). Only new, additive computation is introduced alongside it.
- `ScorePredation` (`DecisionSystem.cs:431-...`) is NOT modified — it remains the exact code path used when `MultiThreatPerceptionEnabled` is `false`.
- `SimulationConfig.MultiThreatPerceptionEnabled` defaults to `false` and is added the same way as `PredationEconomicsEnabled`/`DecisionStaggerEnabled` before it — a plain `bool` constructor param + `{ get; }` property.
- When the flag is `false`, behavior must be byte-identical to today's — proven by a hash-regression test against a pinned `ComputeStateHash()` value, following this session's established methodology (throwaway worktree at the pre-change commit).
- `ComputeStateHash()` itself is NOT modified by this plan.

---

### Task 1: Top-K creature perception query

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/PerceptionSystem.cs` (add `CreatureCandidateBuffer` struct and `FindOtherCreatures` method; `FindNearestOtherCreature` at lines 152-213 stays untouched)
- Test: `Assets/Tests/EditMode/PerceptionSystemTests.cs` (new file)

**Interfaces:**
- Consumes: `CreatureObservation` (existing struct, `PerceptionSystem.cs:26-40`, unchanged), `CreatureStore`/`UniformGrid` (existing, used identically to `FindNearestOtherCreature`'s existing parameters).
- Produces: `CreatureCandidateBuffer` (new public struct) and `PerceptionSystem.FindOtherCreatures(CreatureStore creatures, UniformGrid creatureGrid, SimVector2 origin, float visionRange, CreatureId excludedCreatureId, ref CreatureCandidateBuffer candidates)` (new public method) — Task 2 depends on both by exact name.

This is the complete, exact code to add. Insert the `CreatureCandidateBuffer` struct immediately after `ResourceCandidateBuffer`'s closing brace (`PerceptionSystem.cs:111`), before `public static class PerceptionSystem` (`PerceptionSystem.cs:113`):

```csharp
public struct CreatureCandidateBuffer
{
    public const int Capacity = 4;

    private CreatureObservation _candidate0;
    private CreatureObservation _candidate1;
    private CreatureObservation _candidate2;
    private CreatureObservation _candidate3;
    private int _count;

    public int Count => _count;

    public CreatureObservation GetAt(int index)
    {
        switch (index)
        {
            case 0: return _candidate0;
            case 1: return _candidate1;
            case 2: return _candidate2;
            default: return _candidate3;
        }
    }

    public void Consider(CreatureObservation candidate)
    {
        int insertionIndex = _count;
        for (int index = 0; index < _count; index++)
        {
            if (IsBefore(candidate, GetAt(index)))
            {
                insertionIndex = index;
                break;
            }
        }

        if (insertionIndex >= Capacity)
        {
            return;
        }

        int lastIndex = _count < Capacity ? _count : Capacity - 1;
        for (int index = lastIndex; index > insertionIndex; index--)
        {
            SetAt(index, GetAt(index - 1));
        }

        SetAt(insertionIndex, candidate);
        if (_count < Capacity)
        {
            _count++;
        }
    }

    private static bool IsBefore(CreatureObservation left, CreatureObservation right)
    {
        return left.Distance < right.Distance
            || (Math.Abs(left.Distance - right.Distance) <= 0.00001f && left.CreatureId.Value < right.CreatureId.Value);
    }

    private void SetAt(int index, CreatureObservation candidate)
    {
        switch (index)
        {
            case 0: _candidate0 = candidate; break;
            case 1: _candidate1 = candidate; break;
            case 2: _candidate2 = candidate; break;
            default: _candidate3 = candidate; break;
        }
    }
}
```

Add `FindOtherCreatures` to `PerceptionSystem` — insert it immediately after `FindNearestOtherCreature`'s closing brace (`PerceptionSystem.cs:213`), before `FindNearestAvailableResource` (`PerceptionSystem.cs:215`):

```csharp
public static void FindOtherCreatures(
    CreatureStore creatures,
    UniformGrid creatureGrid,
    SimVector2 origin,
    float visionRange,
    CreatureId excludedCreatureId,
    ref CreatureCandidateBuffer candidates)
{
    if (creatures == null)
    {
        throw new ArgumentNullException(nameof(creatures));
    }

    if (creatureGrid == null)
    {
        throw new ArgumentNullException(nameof(creatureGrid));
    }

    if (visionRange < 0f || float.IsNaN(visionRange) || float.IsInfinity(visionRange))
    {
        throw new ArgumentOutOfRangeException(nameof(visionRange));
    }

    int minimumColumn = creatureGrid.GetColumn(origin.X - visionRange);
    int maximumColumn = creatureGrid.GetColumn(origin.X + visionRange);
    int minimumRow = creatureGrid.GetRow(origin.Y - visionRange);
    int maximumRow = creatureGrid.GetRow(origin.Y + visionRange);

    for (int row = minimumRow; row <= maximumRow; row++)
    {
        for (int column = minimumColumn; column <= maximumColumn; column++)
        {
            int cellIndex = creatureGrid.GetCellIndex(column, row);
            for (int occupant = creatureGrid.GetCellStart(cellIndex); occupant < creatureGrid.GetCellEnd(cellIndex); occupant++)
            {
                int creatureIndex = creatureGrid.GetOccupantIndexAt(occupant);
                CreatureId candidateId = creatures.GetIdAt(creatureIndex);
                if (candidateId.Equals(excludedCreatureId))
                {
                    continue;
                }

                float distance = SimVector2.Distance(origin, creatures.GetMovementAt(creatureIndex).Position);
                if (distance > visionRange)
                {
                    continue;
                }

                candidates.Consider(new CreatureObservation(candidateId, creatureIndex, distance));
            }
        }
    }
}
```

**Behavior table:** with 5 creatures at distances 1, 2, 3, 4, 5 from the origin (all within a 6-unit `visionRange`), `FindOtherCreatures` returns the 4 nearest (distances 1, 2, 3, 4) in ascending distance order, `Count == 4`; the 5th (distance 5) is dropped since `Capacity == 4`. This exactly mirrors `ResourcePerceptionKeepsFourNearestCandidatesInStableDistanceOrder`'s existing test shape (`Assets/Tests/EditMode/SpatialBehaviorTests.cs:82-101`) applied to creatures instead of resources.

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/PerceptionSystemTests.cs`:

```csharp
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Spatial;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PerceptionSystemTests
    {
        [Test]
        public void FindOtherCreaturesKeepsFourNearestCandidatesInAscendingDistanceOrder()
        {
            var creatures = new CreatureStore(initialCapacity: 6);
            CreatureId observer = creatures.Add(Genome.Neutral, new SimVector2(0f, 0f));
            CreatureId expectedFirst = creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(2f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(3f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(4f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(5f, 0f));
            var positions = new[]
            {
                creatures.GetMovementAt(0).Position,
                creatures.GetMovementAt(1).Position,
                creatures.GetMovementAt(2).Position,
                creatures.GetMovementAt(3).Position,
                creatures.GetMovementAt(4).Position,
                creatures.GetMovementAt(5).Position,
            };
            var grid = new UniformGrid(new ArenaBounds(-6f, 6f, -6f, 6f), 2f, initialOccupantCapacity: 6);
            grid.Rebuild(positions, positions.Length);
            var candidates = new CreatureCandidateBuffer();

            PerceptionSystem.FindOtherCreatures(creatures, grid, new SimVector2(0f, 0f), visionRange: 6f, observer, ref candidates);

            Assert.That(candidates.Count, Is.EqualTo(4));
            Assert.That(candidates.GetAt(0).CreatureId, Is.EqualTo(expectedFirst));
            Assert.That(candidates.GetAt(0).Distance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(candidates.GetAt(3).Distance, Is.EqualTo(4f).Within(0.0001f));
        }
    }
}
```

Write one additional test yourself confirming `FindOtherCreatures` excludes the observer's own id (spawn the observer among the candidates being scanned, e.g. reuse the fixture above but assert `observer`'s id never appears in any `GetAt(i).CreatureId` across `0..candidates.Count-1`) — this exercises the `candidateId.Equals(excludedCreatureId)` branch, which the test above never triggers since the observer is never within any other creature's position match.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~PerceptionSystemTests"`
Expected: FAIL to compile (`CreatureCandidateBuffer`/`FindOtherCreatures` don't exist yet).

- [ ] **Step 3: Write the implementation**

Add the exact `CreatureCandidateBuffer` struct and `FindOtherCreatures` method shown above to `PerceptionSystem.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the two new ones. Total count should be 287 (285 existing + 2 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/PerceptionSystem.cs Assets/Tests/EditMode/PerceptionSystemTests.cs
git commit -m "Add top-K creature perception query (FindOtherCreatures)"
```

---

### Task 2: Wire multi-threat scoring into IntentUtilityV1

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (new `MultiThreatPerceptionEnabled` flag, mirroring `DecisionStaggerEnabled`'s placement pattern from the prior task)
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` (new `PredationCandidateBuffer` struct, new `ScorePredationMulti` method, both `DecideIntentUtilityV1` overloads gain two new trailing parameters)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (the `TickDecisions` `IntentUtilityV1` branch, lines 663-676, and the `DecideIntentUtilityV1` call site, lines 713-740)
- Test: `Assets/Tests/EditMode/DecisionSystemTests.cs`, `Assets/Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Consumes: `PerceptionSystem.CreatureCandidateBuffer` and `PerceptionSystem.FindOtherCreatures` (from Task 1, exact names above). `PredationSystem.Threat(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)` and `PredationSystem.HuntCapability(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)` (existing, unchanged, already used by `ScorePredation`).
- Produces: `SimulationConfig.MultiThreatPerceptionEnabled`, `DecisionSystem.PredationCandidateBuffer`, `DecisionSystem.DecideIntentUtilityV1`'s two new trailing parameters — no other task in this plan depends on these, this is the final task.

**`SimulationConfig.cs`**: add `multiThreatPerceptionEnabled` as the new LAST optional constructor parameter (after `decisionStaggerEnabled`), assign to a new `MultiThreatPerceptionEnabled { get; }` property placed immediately after `DecisionStaggerEnabled`'s property — follow the exact same two-edit pattern (constructor parameter + body assignment + property) used for `decisionStaggerEnabled`/`DecisionStaggerEnabled` in the prior task's diff (`git log` or `git show` that commit if you need to see the exact diff shape; the pattern is identical, just a new bool named `multiThreatPerceptionEnabled`/`MultiThreatPerceptionEnabled` defaulting to `false`).

**`DecisionSystem.cs`**: add `PredationCandidateBuffer` immediately before the `DecisionSystem` static class opens (same placement style as `DecisionDiagnostics`, which sits before `public static class DecisionSystem` at line 268):

```csharp
public struct PredationCandidateBuffer
{
    public const int Capacity = 4;

    private CreatureObservation _observation0;
    private Phenotype _phenotype0;
    private CreatureObservation _observation1;
    private Phenotype _phenotype1;
    private CreatureObservation _observation2;
    private Phenotype _phenotype2;
    private CreatureObservation _observation3;
    private Phenotype _phenotype3;
    private int _count;

    public int Count => _count;

    public CreatureObservation GetObservationAt(int index)
    {
        switch (index)
        {
            case 0: return _observation0;
            case 1: return _observation1;
            case 2: return _observation2;
            default: return _observation3;
        }
    }

    public Phenotype GetPhenotypeAt(int index)
    {
        switch (index)
        {
            case 0: return _phenotype0;
            case 1: return _phenotype1;
            case 2: return _phenotype2;
            default: return _phenotype3;
        }
    }

    public void Add(CreatureObservation observation, Phenotype phenotype)
    {
        if (_count >= Capacity)
        {
            return;
        }

        switch (_count)
        {
            case 0: _observation0 = observation; _phenotype0 = phenotype; break;
            case 1: _observation1 = observation; _phenotype1 = phenotype; break;
            case 2: _observation2 = observation; _phenotype2 = phenotype; break;
            default: _observation3 = observation; _phenotype3 = phenotype; break;
        }

        _count++;
    }
}
```

Add `ScorePredationMulti` immediately after `ScorePredation`'s closing brace:

```csharp
private static void ScorePredationMulti(
    CreatureNeeds needs,
    Genome genome,
    Phenotype self,
    PredationCandidateBuffer others,
    ref DecisionCandidateBuffer candidates,
    bool economicsEnabled,
    out float fleeScore,
    out float huntScore)
{
    fleeScore = 0f;
    huntScore = 0f;
    if (others.Count == 0)
    {
        return;
    }

    float hunger = Urgency(needs.Energy, self.EnergyCapacity);
    CreatureId bestFleeTarget = default;
    CreatureId bestHuntTarget = default;
    for (int i = 0; i < others.Count; i++)
    {
        CreatureObservation observation = others.GetObservationAt(i);
        Phenotype otherPhenotype = others.GetPhenotypeAt(i);
        float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + observation.Distance);
        float candidateThreatIntensity = PredationSystem.Threat(otherPhenotype, self, observation.Distance, economicsEnabled);
        float candidateFleeScore = Math.Max(0f, candidateThreatIntensity * genome.RiskAversion * distanceAvailability);
        float candidateHuntScore = PredationSystem.HuntCapability(self, otherPhenotype, observation.Distance, economicsEnabled) * hunger * distanceAvailability;
        if (candidateFleeScore > fleeScore)
        {
            fleeScore = candidateFleeScore;
            bestFleeTarget = observation.CreatureId;
        }

        if (candidateHuntScore > huntScore)
        {
            huntScore = candidateHuntScore;
            bestHuntTarget = observation.CreatureId;
        }
    }

    if (fleeScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.Flee, -1, bestFleeTarget, fleeScore));
    }

    if (huntScore >= 0.10f)
    {
        candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekPrey, -1, bestHuntTarget, huntScore));
    }
}
```

Both `DecideIntentUtilityV1` overloads (current full text at `DecisionSystem.cs:272-298` and `:300-369`) gain two new trailing parameters, after `threatFalloffDistance`:

```csharp
PredationCandidateBuffer otherCandidates = default,
bool multiThreatPerceptionEnabled = false
```

The short overload's forwarding call must also pass these two through unchanged (append them after `threatFalloffDistance` in the forwarded call). The full overload's `predationEnabled` block (currently):

```csharp
if (predationEnabled)
{
    ScoreCarcass(needs, phenotype, resources, carcass, ref candidates, out carcassScore);
    ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, out fleeScore, out huntScore);
}
```

becomes:

```csharp
if (predationEnabled)
{
    ScoreCarcass(needs, phenotype, resources, carcass, ref candidates, out carcassScore);
    if (multiThreatPerceptionEnabled)
    {
        ScorePredationMulti(needs, genome, phenotype, otherCandidates, ref candidates, economicsEnabled, out fleeScore, out huntScore);
    }
    else
    {
        ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, out fleeScore, out huntScore);
    }
}
```

**`SimulationWorld.cs`**: inside the `Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1` block (`SimulationWorld.cs:663-676`), after the existing `other`/`threatIntensity` computation (do not remove or reorder any of it), add:

```csharp
var otherCandidates = new PredationCandidateBuffer();
if (Config.MultiThreatPerceptionEnabled)
{
    var creatureCandidates = new CreatureCandidateBuffer();
    PerceptionSystem.FindOtherCreatures(Creatures, CombatGrid, movement.Position, phenotype.VisionRange, Creatures.GetIdAt(index), ref creatureCandidates);
    for (int candidateIndex = 0; candidateIndex < creatureCandidates.Count; candidateIndex++)
    {
        CreatureObservation candidateObservation = creatureCandidates.GetAt(candidateIndex);
        otherCandidates.Add(candidateObservation, Creatures.GetPhenotypeAt(candidateObservation.CreatureIndex));
    }
}
```

`otherCandidates` must be declared even when the flag is off (its `Count` stays `0`, which is exactly what `ScorePredationMulti` treats as a no-op via its `others.Count == 0` early return — though that branch is never reached when the flag is off anyway, since the `DecideIntentUtilityV1` call site below only takes the `ScorePredation` path in that case). Then, at the `DecideIntentUtilityV1` call site (`SimulationWorld.cs:715-740`), append the two new trailing arguments after `Config.ThreatFalloffDistance`:

```csharp
Config.ThreatFalloffDistance,
otherCandidates,
Config.MultiThreatPerceptionEnabled);
```

(replacing the current call's final line `Config.ThreatFalloffDistance);` with the three lines above).

**Behavior table** (mirrors this session's B-5 `IntentUtilityWithEconomicsEnabledSeeksPreyForStronglyFavorableMatchup`-style fixtures, reusing `DecisionSystemTests.cs`'s existing `MakePhenotype` reflection helper): with `multiThreatPerceptionEnabled: true`, `economicsEnabled: true`, and a `PredationCandidateBuffer` containing two candidates — one weak/favorable-to-hunt (`attackPower: 0.2f, defense: 0.1f, maneuverability: 1f`, distance 5, closer) and one strong/dangerous (`attackPower: 1.9f, defense: 0.1f, maneuverability: 1f`, distance 1, nearer) — the winning `SeekPrey` candidate must target the weak/favorable one (proving best-of-K selects across candidates by score, not merely the nearest), while the `Flee` candidate (if any clears the `0.10` threshold) must target the strong/dangerous one.

- [ ] **Step 1: Write the failing tests**

Append to `Assets/Tests/EditMode/DecisionSystemTests.cs`, inside the `DecisionSystemTests` class:

```csharp
// Proves best-of-K selection: with two visible creatures, the winning SeekPrey candidate
// targets the favorable-to-hunt one even though it is farther away than the dangerous one,
// and the Flee candidate (if present) targets the dangerous one - not just "the nearest".
[Test]
public void MultiThreatPerceptionSelectsBestHuntTargetAcrossMultipleVisibleCreatures()
{
    Phenotype self = MakePhenotype(attackPower: 1f, defense: 0.5f, maneuverability: 1f, aggression: 0.8f);
    Phenotype weakFavorableTarget = MakePhenotype(attackPower: 0.1f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
    Phenotype strongDangerousTarget = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f);
    CreatureNeeds needs = CreatureNeeds.Full(self);
    needs.Energy = 0f;
    var resources = new ResourceStore(initialCapacity: 0);
    var otherCandidates = new PredationCandidateBuffer();
    var weakObservation = new CreatureObservation(new CreatureId(2), 1, 5f);
    var strongObservation = new CreatureObservation(new CreatureId(3), 2, 1f);
    otherCandidates.Add(strongObservation, strongDangerousTarget);
    otherCandidates.Add(weakObservation, weakFavorableTarget);

    CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
        needs, Genome.Neutral, self, resources, new SimVector2(0f, 0f), default, default,
        carcass: default, memory: default, cognitionEnabled: false, threat: default,
        threatIntensity: 0f, otherPhenotype: default, predationEnabled: true, physiologyEnabled: false,
        reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
        mateReproduction: default, reproductionEnabled: false, tick: 0,
        diagnostics: out _, economicsEnabled: true,
        threatFalloffDistance: SimulationConfig.DefaultThreatFalloffDistance,
        otherCandidates: otherCandidates, multiThreatPerceptionEnabled: true);

    Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
    Assert.That(decision.TargetCreatureId, Is.EqualTo(weakObservation.CreatureId));
}
```

Write the flag-off hash-regression test yourself, following the exact template from the prior task's `DecisionStaggerDisabledProducesIdenticalHashToPreExistingBehavior` (`CoreSimulationTests.cs`) and `ExpectedDecisionStaggerDisabledHash`'s comment style: derive `ExpectedMultiThreatPerceptionDisabledHash` by running the same `PredationVariation` scenario this session's B-5 hash test uses (`SimulationSchedule(60,60,30,10,10,10,5,1)`, `worldSeed: 99`, `initialPopulation: 2`, `founderProfile: FounderProfile.PredationVariation`, `multiThreatPerceptionEnabled` omitted since it doesn't exist at the pre-change commit, 50 `Step()` calls) against a throwaway worktree at the commit immediately before your Task 2 changes.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~MultiThreatPerceptionSelectsBestHuntTargetAcrossMultipleVisibleCreatures|FullyQualifiedName~MultiThreatPerceptionDisabled"`
Expected: FAIL to compile (`PredationCandidateBuffer` and the new `DecideIntentUtilityV1` parameters don't exist yet).

- [ ] **Step 3: Implement the changes**

Apply the exact changes shown above to `SimulationConfig.cs`, `DecisionSystem.cs`, and `SimulationWorld.cs`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass, including the two new ones from this task. Total count should be 289 (287 from Task 1 + 2 new).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionSystemTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Wire multi-threat perception into IntentUtilityV1 predation scoring"
```

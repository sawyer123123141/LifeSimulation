# Kin Recognition (C-5, part 3 of 3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Under `IntentUtilityV1`, when `SimulationConfig.KinRecognitionEnabled` is set, a creature never scores `Flee` or `SeekPrey` against a recognized parent, child, or sibling (shared non-default parent) - flag defaults `false` and is byte-identical to today's behavior when off.

**Architecture:** A pure static `DecisionSystem.IsKin` helper compares two `CreatureLineage`/`CreatureId` pairs. `ScorePredation` (single-candidate path) and `ScorePredationMulti` (multi-candidate path, from C-3) both gain a kin check that skips/short-circuits scoring for a recognized-kin candidate. `PredationCandidateBuffer` (already carries per-slot `CreatureObservation`/`Phenotype`) gains a third per-slot field, `CreatureLineage`, so `ScorePredationMulti`'s loop can test each candidate. `SimulationWorld.TickDecisions` supplies the lineage data - it already has `CreatureStore` access, the pure `DecisionSystem` functions don't.

**Tech Stack:** C#, Unity Test Framework (NUnit), EditMode tests.

## Global Constraints

- New flag `SimulationConfig.KinRecognitionEnabled`, default `false`.
- Kin definition: A and B are kin if A is B's recorded parent, B is A's recorded parent, or they share a non-default (`.Value != 0`) parent. Two founders (both parent slots `default(CreatureId)`, value `0`) must never register as siblings.
- Scope: `IntentUtilityV1` only - the Legacy decision policy (`PredationSystem.Decide`, called directly from `SimulationWorld.cs` around line 930) is untouched, matching every other C-5/C-3/C-4/B-6 change this session.
- No mate-avoidance: `ScoreMate` is not touched. No change to the food/water danger-penalty term (`ScoreResourceCandidates`'s existing `threat`/`threatIntensity` consumption) - kin still counts as a threat presence there, only `Flee`/`SeekPrey` scoring changes.
- When the flag is `false`, `ScorePredation`/`ScorePredationMulti` must execute identically to before this task - proven by a hash-regression test using the established methodology (throwaway worktree at the pre-task commit, same fixed `PredationVariation` scenario, 50 `Step()` calls, `ComputeStateHash()`).

---

### Task 1: Kin test, buffer lineage slot, and scoring skip

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs` (`PredationCandidateBuffer` struct at line 269, `DecideIntentUtilityV1` both overloads at lines 330 and 361, `ScorePredation` at line 511, `ScorePredationMulti` at line 545)
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (constructor + properties)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` (`TickDecisions`, lines 703-834)
- Test: `Assets/Tests/EditMode/DecisionSystemTests.cs` (unit tests for `IsKin`, `ScorePredation`, `ScorePredationMulti`)
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs` (integration tests, hash-regression test)

**Interfaces:**
- Consumes: `CreatureStore.GetLineageAt(int)` (returns `CreatureLineage` with `FirstParent`/`SecondParent` of type `CreatureId`), `CreatureStore.GetIdAt(int)` (returns `CreatureId`), `CreatureId.Equals(CreatureId)`, `CreatureId.Value` (`long`).
- Produces: `DecisionSystem.IsKin(CreatureId, CreatureLineage, CreatureId, CreatureLineage)` (private static, returns `bool`) - used only within `DecisionSystem.cs`, not consumed elsewhere. `PredationCandidateBuffer.GetLineageAt(int)` (public, returns `CreatureLineage`) and `PredationCandidateBuffer.Add(CreatureObservation, Phenotype, CreatureLineage)` (signature change, breaking - the only existing call site is `SimulationWorld.cs:765`, updated in this same task). `SimulationConfig.KinRecognitionEnabled` (`bool` property, default `false`).

- [ ] **Step 1: Add `KinRecognitionEnabled` to `SimulationConfig`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, add a new constructor parameter immediately after `parentalFollowingEnabled` (the current last parameter):

```csharp
            bool parentalFollowingEnabled = false,
            bool kinRecognitionEnabled = false)
```

Add the assignment immediately after `ParentalFollowingEnabled = parentalFollowingEnabled;`:

```csharp
            ParentalFollowingEnabled = parentalFollowingEnabled;
            KinRecognitionEnabled = kinRecognitionEnabled;
```

Add the property immediately after `public bool ParentalFollowingEnabled { get; }`:

```csharp
        public bool ParentalFollowingEnabled { get; }
        public bool KinRecognitionEnabled { get; }
```

- [ ] **Step 2: Write the failing unit tests for kin recognition**

`Assets/Tests/EditMode/DecisionSystemTests.cs` already tests `DecisionSystem`'s private scoring functions exclusively by calling the public `DecideIntentUtilityV1` entry point and inspecting the returned `CreatureDecision`/`DecisionDiagnostics` (see e.g. `IntentUtilityWithEconomicsEnabledSeeksPreyForStronglyFavorableMatchup` and `IntentUtilityDiagnosticsReportsNonZeroHuntScoreForAFavorableMatchup` already in that file) - follow that exact pattern, do not add new public forwarding methods to `DecisionSystem`.

Add to `Assets/Tests/EditMode/DecisionSystemTests.cs`:

```csharp
        [Test]
        public void IntentUtilityDoesNotFleeOrHuntAParentWhenKinRecognitionEnabled()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            CreatureNeeds needs = CreatureNeeds.Full(attacker);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var parentId = new CreatureId(2);
            var selfId = new CreatureId(3);
            var threatObservation = new CreatureObservation(parentId, 1, 1f);
            var selfLineage = new CreatureLineage(selfId, parentId, default, generation: 1);
            var parentLineage = new CreatureLineage(parentId, default, default, generation: 0);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, attacker, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threatObservation,
                threatIntensity: 5f, otherPhenotype: defender, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: true, tick: 0,
                diagnostics: out DecisionDiagnostics diagnostics,
                selfId: selfId, selfLineage: selfLineage, otherLineage: parentLineage, kinRecognitionEnabled: true);

            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.SeekPrey));
            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.Flee));
            Assert.That(diagnostics.FleeScore, Is.EqualTo(0f));
            Assert.That(diagnostics.HuntScore, Is.EqualTo(0f));
        }

        [Test]
        public void IntentUtilityStillHuntsAnUnrelatedCreatureWhenKinRecognitionEnabled()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            CreatureNeeds needs = CreatureNeeds.Full(attacker);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var strangerId = new CreatureId(99);
            var selfId = new CreatureId(3);
            var threatObservation = new CreatureObservation(strangerId, 1, 1f);
            var selfLineage = new CreatureLineage(selfId, new CreatureId(2), default, generation: 1);
            var strangerLineage = new CreatureLineage(strangerId, default, default, generation: 0);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, attacker, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threatObservation,
                threatIntensity: 0f, otherPhenotype: defender, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: true, tick: 0,
                diagnostics: out _,
                selfId: selfId, selfLineage: selfLineage, otherLineage: strangerLineage, kinRecognitionEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
        }

        [Test]
        public void IntentUtilityMultiThreatSkipsKinCandidateEvenWhenItScoresHigherThanNonKin()
        {
            // The sibling sits far closer (0.5) than the stranger (5), so if the kin skip did
            // NOT work, the sibling would dominate flee scoring and produce a much higher
            // FleeScore than considering the stranger alone. Compares the kin-enabled and
            // kin-disabled runs' reported diagnostics.FleeScore directly.
            Phenotype self = MakePhenotype(attackPower: 0.2f, defense: 1.9f, maneuverability: 1f, energyCapacity: 200f);
            Phenotype siblingPhenotype = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype strangerPhenotype = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            CreatureNeeds needs = CreatureNeeds.Full(self);
            var resources = new ResourceStore(initialCapacity: 0);
            var selfId = new CreatureId(3);
            var siblingId = new CreatureId(10);
            var strangerId = new CreatureId(11);
            var sharedParent = new CreatureId(1);
            var selfLineage = new CreatureLineage(selfId, sharedParent, default, generation: 1);
            var siblingLineage = new CreatureLineage(siblingId, sharedParent, default, generation: 1);
            var strangerLineage = new CreatureLineage(strangerId, default, default, generation: 0);

            var kinEnabledOthers = new PredationCandidateBuffer();
            kinEnabledOthers.Add(new CreatureObservation(siblingId, creatureIndex: 0, distance: 0.5f), siblingPhenotype, siblingLineage);
            kinEnabledOthers.Add(new CreatureObservation(strangerId, creatureIndex: 1, distance: 5f), strangerPhenotype, strangerLineage);
            DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, self, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: false, tick: 0,
                diagnostics: out DecisionDiagnostics kinEnabledDiagnostics,
                otherCandidates: kinEnabledOthers, multiThreatPerceptionEnabled: true,
                selfId: selfId, selfLineage: selfLineage, kinRecognitionEnabled: true);

            var kinDisabledOthers = new PredationCandidateBuffer();
            kinDisabledOthers.Add(new CreatureObservation(siblingId, creatureIndex: 0, distance: 0.5f), siblingPhenotype, siblingLineage);
            kinDisabledOthers.Add(new CreatureObservation(strangerId, creatureIndex: 1, distance: 5f), strangerPhenotype, strangerLineage);
            DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, self, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: false, tick: 0,
                diagnostics: out DecisionDiagnostics kinDisabledDiagnostics,
                otherCandidates: kinDisabledOthers, multiThreatPerceptionEnabled: true,
                selfId: selfId, selfLineage: selfLineage, kinRecognitionEnabled: false);

            Assert.That(kinEnabledDiagnostics.FleeScore, Is.LessThan(kinDisabledDiagnostics.FleeScore));
        }
```

These tests rely on `PredationCandidateBuffer.Add` accepting a third `CreatureLineage` argument (Step 5) and `DecideIntentUtilityV1` accepting the four new trailing parameters `selfId`, `selfLineage`, `otherLineage`, `kinRecognitionEnabled` (Step 7).

- [ ] **Step 3: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "KinRecognition|DoesNotFleeOrHuntAParent|StillHuntsAnUnrelatedCreature|MultiThreatSkipsKinCandidate"`
Expected: FAIL - compile error (`PredationCandidateBuffer.Add` doesn't accept a third argument; `DecideIntentUtilityV1` doesn't accept `selfId`/`selfLineage`/`otherLineage`/`kinRecognitionEnabled` named arguments yet; `CreatureLineage` constructor confirmed as `CreatureLineage(CreatureId lineageId, CreatureId firstParent, CreatureId secondParent, int generation)` in `Assets/Scripts/Simulation/Core/SimulationTypes.cs:53` - adjust test code if the actual constructor differs).

- [ ] **Step 4: Add the `IsKin` helper**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, add immediately before `ScorePredation` (currently at line 511):

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

- [ ] **Step 5: Add the `CreatureLineage` slot to `PredationCandidateBuffer`**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, modify the `PredationCandidateBuffer` struct (lines 269-324):

```csharp
    public struct PredationCandidateBuffer
    {
        public const int Capacity = 4;

        private CreatureObservation _observation0;
        private Phenotype _phenotype0;
        private CreatureLineage _lineage0;
        private CreatureObservation _observation1;
        private Phenotype _phenotype1;
        private CreatureLineage _lineage1;
        private CreatureObservation _observation2;
        private Phenotype _phenotype2;
        private CreatureLineage _lineage2;
        private CreatureObservation _observation3;
        private Phenotype _phenotype3;
        private CreatureLineage _lineage3;
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

        public CreatureLineage GetLineageAt(int index)
        {
            switch (index)
            {
                case 0: return _lineage0;
                case 1: return _lineage1;
                case 2: return _lineage2;
                default: return _lineage3;
            }
        }

        public void Add(CreatureObservation observation, Phenotype phenotype, CreatureLineage lineage)
        {
            if (_count >= Capacity)
            {
                return;
            }

            switch (_count)
            {
                case 0: _observation0 = observation; _phenotype0 = phenotype; _lineage0 = lineage; break;
                case 1: _observation1 = observation; _phenotype1 = phenotype; _lineage1 = lineage; break;
                case 2: _observation2 = observation; _phenotype2 = phenotype; _lineage2 = lineage; break;
                default: _observation3 = observation; _phenotype3 = phenotype; _lineage3 = lineage; break;
            }

            _count++;
        }
    }
```

`CreatureLineage` is a `readonly struct` (`Assets/Scripts/Simulation/Core/SimulationTypes.cs:51`) - its default value (`default(CreatureLineage)`) has `LineageId`/`FirstParent`/`SecondParent` all `default(CreatureId)` (value `0`) and `Generation` `0`, which is a safe, harmless default for any unused slot.

- [ ] **Step 6: Add the kin skip to `ScorePredation` and `ScorePredationMulti`**

Modify `ScorePredation` (currently lines 511-543) in `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`:

```csharp
        private static void ScorePredation(
            CreatureNeeds needs,
            Genome genome,
            Phenotype self,
            Phenotype other,
            CreatureObservation observation,
            float threatIntensity,
            ref DecisionCandidateBuffer candidates,
            bool economicsEnabled,
            CreatureId selfId,
            CreatureLineage selfLineage,
            CreatureLineage otherLineage,
            bool kinRecognitionEnabled,
            out float fleeScore,
            out float huntScore)
        {
            fleeScore = 0f;
            huntScore = 0f;
            if (!observation.IsValid)
            {
                return;
            }

            if (kinRecognitionEnabled && IsKin(selfId, selfLineage, observation.CreatureId, otherLineage))
            {
                return;
            }

            float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + observation.Distance);
            float hunger = Urgency(needs.Energy, self.EnergyCapacity);
            fleeScore = Math.Max(0f, threatIntensity * genome.RiskAversion * distanceAvailability);
            huntScore = PredationSystem.HuntCapability(self, other, observation.Distance, economicsEnabled) * hunger * distanceAvailability;
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

Modify `ScorePredationMulti` (currently lines 545-595):

```csharp
        private static void ScorePredationMulti(
            CreatureNeeds needs,
            Genome genome,
            Phenotype self,
            PredationCandidateBuffer others,
            ref DecisionCandidateBuffer candidates,
            bool economicsEnabled,
            CreatureId selfId,
            CreatureLineage selfLineage,
            bool kinRecognitionEnabled,
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
                if (kinRecognitionEnabled && IsKin(selfId, selfLineage, observation.CreatureId, others.GetLineageAt(i)))
                {
                    continue;
                }

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

- [ ] **Step 7: Thread the new parameters through both `DecideIntentUtilityV1` overloads**

In `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`, modify the shorter overload (currently lines 330-359):

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
            out DecisionDiagnostics diagnostics,
            bool economicsEnabled = false,
            float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
            PredationCandidateBuffer otherCandidates = default,
            bool multiThreatPerceptionEnabled = false,
            bool restBehaviorEnabled = false,
            CreatureId selfId = default,
            CreatureLineage selfLineage = default,
            CreatureLineage otherLineage = default,
            bool kinRecognitionEnabled = false)
        {
            return DecideIntentUtilityV1(
                needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
                cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
                default, default, default, default, default, false, tick, out diagnostics, economicsEnabled,
                threatFalloffDistance, otherCandidates, multiThreatPerceptionEnabled, restBehaviorEnabled,
                selfId, selfLineage, otherLineage, kinRecognitionEnabled);
        }
```

Modify the longer overload (currently lines 361-389, containing the actual body):

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
            long tick,
            out DecisionDiagnostics diagnostics,
            bool economicsEnabled = false,
            float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
            PredationCandidateBuffer otherCandidates = default,
            bool multiThreatPerceptionEnabled = false,
            bool restBehaviorEnabled = false,
            CreatureId selfId = default,
            CreatureLineage selfLineage = default,
            CreatureLineage otherLineage = default,
            bool kinRecognitionEnabled = false)
```

(Only the parameter list changes - the body stays the same except the two internal calls below.)

Inside that same method's body, change the `ScorePredationMulti`/`ScorePredation` calls (currently lines 410 and 414):

```csharp
                if (multiThreatPerceptionEnabled)
                {
                    ScorePredationMulti(needs, genome, phenotype, otherCandidates, ref candidates, economicsEnabled, selfId, selfLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
                }
                else
                {
                    ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, selfId, selfLineage, otherLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
                }
```

- [ ] **Step 8: Run tests to verify Step 2's tests pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "KinRecognition|DoesNotFleeOrHuntAParent|StillHuntsAnUnrelatedCreature|MultiThreatSkipsKinCandidate"`
Expected: PASS (3/3)

- [ ] **Step 9: Update `SimulationWorld.TickDecisions` to supply lineage data**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, inside `TickDecisions` (currently starting at line 703), add a self-lineage fetch immediately after the existing `Phenotype phenotype = GetEffectivePhenotype(index);` line (currently line 714):

```csharp
                Phenotype phenotype = GetEffectivePhenotype(index);
                CreatureLineage selfLineage = Creatures.GetLineageAt(index);
```

Change the `otherCandidates.Add(...)` call (currently line 765) to pass the candidate's lineage as a third argument:

```csharp
                        otherCandidates.Add(candidateObservation, Creatures.GetPhenotypeAt(candidateObservation.CreatureIndex), Creatures.GetLineageAt(candidateObservation.CreatureIndex));
```

Change the `DecisionSystem.DecideIntentUtilityV1(...)` call (currently lines 806-834) to pass four new trailing arguments after `Config.RestBehaviorEnabled`:

```csharp
                        Config.PredationEconomicsEnabled,
                        Config.ThreatFalloffDistance,
                        otherCandidates,
                        Config.MultiThreatPerceptionEnabled,
                        Config.RestBehaviorEnabled,
                        Creatures.GetIdAt(index),
                        selfLineage,
                        other.IsValid ? Creatures.GetLineageAt(other.CreatureIndex) : default,
                        Config.KinRecognitionEnabled);
```

- [ ] **Step 10: Write the failing integration tests**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`:

```csharp
        [Test]
        public void CreatureDoesNotFleeFromKinWhenKinRecognitionEnabledAndMultiThreatPerceptionOff()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 31,
                initialPopulation: 0,
                schedule: schedule,
                founderProfile: FounderProfile.PredationVariation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                predationEconomicsEnabled: true,
                kinRecognitionEnabled: true);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.SetCreaturePosition(firstParent, new SimVector2(1f, 0f));
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            world.Creatures.GetNeedsRefAt(childIndex).Age = ReproductionSystem.AdultAgeSeconds;
            world.Creatures.GetNeedsRefAt(childIndex).Energy = 1f;

            world.Step(config.FixedDeltaTime);

            CreatureDecision decision = world.Creatures.GetDecisionAt(childIndex);
            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.Flee));
        }

        [Test]
        public void CreatureDoesNotFleeFromKinWhenKinRecognitionEnabledAndMultiThreatPerceptionOn()
        {
            var schedule = new SimulationSchedule(1, 1, 1, 1, 1, 1, 1, 1);
            var config = new SimulationConfig(
                worldSeed: 32,
                initialPopulation: 0,
                schedule: schedule,
                founderProfile: FounderProfile.PredationVariation,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                predationEconomicsEnabled: true,
                multiThreatPerceptionEnabled: true,
                kinRecognitionEnabled: true);
            var world = new SimulationWorld(config);
            CreatureId firstParent = world.Spawn(Genome.Neutral);
            CreatureId secondParent = world.Spawn(Genome.Neutral);
            world.Creatures.TryGetIndex(firstParent, out int firstIndex);
            world.SetCreaturePosition(firstParent, new SimVector2(1f, 0f));
            CreatureId child = world.Creatures.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            world.Creatures.TryGetIndex(child, out int childIndex);
            world.Creatures.GetNeedsRefAt(childIndex).Age = ReproductionSystem.AdultAgeSeconds;
            world.Creatures.GetNeedsRefAt(childIndex).Energy = 1f;

            world.Step(config.FixedDeltaTime);

            CreatureDecision decision = world.Creatures.GetDecisionAt(childIndex);
            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.Flee));
        }
```

Check `world.SetCreaturePosition` exists (confirmed used in part 2's plan, `Assets/Scripts/Presentation`-adjacent test helper on `SimulationWorld` - if it isn't present, use `world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(1f, 0f);` instead, matching the pattern already used in part 2's tests).

- [ ] **Step 11: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "DoesNotFleeFromKin"`
Expected: PASS (2/2). If a test fails because the child creature decided something other than a predation-relevant action entirely (e.g. `SeekFood`), that's still a pass for this assertion (`Is.Not.EqualTo(CreatureAction.Flee)`) - the test only proves kin-flee is suppressed, not that any specific alternative action is chosen.

- [ ] **Step 12: Derive the hash-regression baseline**

From the repo root, record the current `main` tip before this task's changes (`git log --oneline -1 main`) as `<PRE_TASK_COMMIT>`. Create a throwaway worktree at that commit:

```bash
git worktree add /c/ls-work/kin-recognition-baseline <PRE_TASK_COMMIT>
cd /c/ls-work/kin-recognition-baseline/tools/HeadlessTests
```

Add a temporary test file (or temporary test method) running:

```csharp
SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
var config = new SimulationConfig(
    worldSeed: 99,
    initialPopulation: 2,
    schedule: schedule,
    founderProfile: FounderProfile.PredationVariation);
var world = new SimulationWorld(config);
for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }
Console.WriteLine(world.ComputeStateHash());
```

Run it, capture the printed `ulong` value, then remove the throwaway worktree:

```bash
cd /c/ls-work
git worktree remove /c/ls-work/kin-recognition-baseline
```

Expected: the printed hash equals `12050501592762519865UL` (every prior hash-regression test this session, using this exact scenario, has produced this identical value, since the scenario never exercises any of the newly flag-gated code paths - none of the two founders in this scenario are ever born via `AddChild`, so `KinRecognitionEnabled`'s branch condition is never reached even if it were `true`, let alone `false` as it is here).

- [ ] **Step 13: Write the failing hash-regression test**

Add to `Assets/Tests/EditMode/CoreSimulationTests.cs`, immediately after `ParentalFollowingDisabledProducesIdenticalHashToPreExistingBehavior` (the most recent hash-regression test in the file):

```csharp
        // Captured from the pre-Task-1 commit <PRE_TASK_COMMIT> (the commit this task's changes
        // were built on top of), by running this exact setup (with kinRecognitionEnabled omitted,
        // since that constructor parameter did not exist yet) for 50 ticks and reading
        // world.ComputeStateHash(). Pinning this value confirms that adding
        // Config.KinRecognitionEnabled and its call-site wiring in SimulationWorld.cs/
        // DecisionSystem.cs is byte-identical to prior behavior when the flag is left at its
        // default (false).
        private const ulong ExpectedKinRecognitionDisabledHash = 12050501592762519865UL;

        [Test]
        public void KinRecognitionDisabledProducesIdenticalHashToPreExistingBehavior()
        {
            SimulationSchedule schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);
            var config = new SimulationConfig(
                worldSeed: 99,
                initialPopulation: 2,
                schedule: schedule,
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++) { world.Step(config.FixedDeltaTime); }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(ExpectedKinRecognitionDisabledHash));
        }
```

Replace `<PRE_TASK_COMMIT>` in the comment with the actual commit hash recorded in Step 12.

- [ ] **Step 14: Run the full test suite**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass. This task adds 6 new tests (3 in `DecisionSystemTests.cs`, 2 integration tests + 1 hash-regression test in `CoreSimulationTests.cs`) on top of the 308 already passing as of the last merge to `main` - expect 314 passing. Adjust if the actual pre-task count differs (check `git log --oneline -1 main` and the prior commit's own reported test count first).

- [ ] **Step 15: Commit**

```bash
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/DecisionSystemTests.cs Assets/Tests/EditMode/CoreSimulationTests.cs
git commit -m "Add kin recognition: creatures don't flee from or hunt recognized parents/children/siblings"
```

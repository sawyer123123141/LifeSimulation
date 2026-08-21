# Ancestry-Aware Cluster History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build deterministic, analysis-only P5 history that follows genetic-cluster continuity through recorded ancestry and reports conservative split, merge, and lineage-extinction evidence.

**Architecture:** `GeneticClusterObservation` binds one immutable population snapshot to the exact explicit threshold and cluster result derived from it. `AncestryHistory` gains an overflow-aware completeness watermark. `GeneticClusterHistory` compares compatible observations through ancestry-supported relations and writes bounded immutable evidence events without affecting simulation state.

**Tech Stack:** C#, Unity EditMode tests through `tools/HeadlessTests`, no new packages.

**Spec:** `docs/superpowers/specs/2026-08-21-conservative-cluster-history-design.md`

## Global Constraints

- Work only in `Assets/Scripts/Simulation/Analysis/`, the focused EditMode tests, and the two lead-agent documents named below.
- Do not change `SimulationWorld`, behavior, config, RNG, hashes, or the hot tick path. Analysis is host-triggered and never feeds simulation decisions.
- All policy values are caller-supplied. Never add a default biological-species definition or a probability-like confidence score.
- Observation index and cluster ordinal determine output order; `Dictionary` and `HashSet` iteration never determine a track, relation, or event order.
- Full and sampled observations cannot share a segment. Overflowed or incomplete event data cannot produce confirmed ancestry-dependent results.
- Do not stage generated `.meta` files. Delete every `ZZZ*.cs` probe before committing and never use `git add -A`.

---

## File Structure

| File | Responsibility |
| --- | --- |
| `Analysis/PopulationGenomeSnapshot.cs` | Full/sample provenance on immutable genome captures. |
| `Analysis/GeneticClusterObservation.cs` | Owns one snapshot, threshold, and derived assignments. |
| `Analysis/ClusterHistoryPolicy.cs` | Validated explicit support, depth, and confirmation values. |
| `Analysis/AncestryHistory.cs` | Existing ancestry graph plus completeness watermark. |
| `Analysis/GeneticClusterRelation.cs` | Immutable direct/descendant evidence for one cluster pair. |
| `Analysis/ClusterHistoryEvent.cs` | Immutable event kind, status, tracks, and evidence. |
| `Analysis/ClusterHistoryEventBuffer.cs` | Fixed-capacity host-drained output. |
| `Analysis/GeneticClusterHistory.cs` | Segment validation, track allocation, classification, persistence, extinction. |
| `Assets/Tests/EditMode/*` named per task | Small deterministic fixtures for each contract. |

## Shared Contracts

| Member | Contract |
| --- | --- |
| `PopulationGenomeSnapshot.IsSampled` | False for `Capture`; true for every `CaptureSample`, even when all creatures fit. |
| `SourcePopulationCount` / `SampleLimit` | Source count at capture; `0` for full capture or requested positive limit. |
| `GeneticClusterObservation.Create(snapshot, threshold)` | Validates and privately creates `GeneticClusters.From(snapshot, threshold)`; callers cannot supply a separate cluster result. |
| `ClusterHistoryPolicy` | Positive counts/depth/windows, fractions in `(0, 1]`, value equality across every policy field. |
| `AncestryHistory.RecordCompleteBatch(events, throughTick)` | Reads but never clears host events; advances only after founders, monotonic tick, valid event ticks, and no overflow. |
| `GeneticClusterRelation.Create(...)` | Exposes direct/descendant support counts/fractions, contributor count, depth range, coverage, and `IsStrong`. |
| `GeneticClusterHistory.Record(observation, ancestry)` | Requires a later compatible observation and complete ancestry, then emits ordered analytical evidence into a fixed event buffer. |

## Behaviour Table

| Situation | Required result |
| --- | --- |
| Compatible cluster retains the same IDs | Existing external track continues. |
| Recorded descendants replace dead parents | Continuity if both explicit support sides pass. |
| One predecessor to several exclusive successors | Candidate split, then confirmation only after every child persists. |
| Several exclusive predecessors to one successor | Candidate merge, then confirmation only after successor persists. |
| Many-to-many strong component | `AmbiguousReorganisation`, never split or merge too. |
| Missing ancestry, cycle, overflow, incompatible stream | Unresolved/rejected evidence only. |
| Sampled disappearance | Never extinction. |
| Full complete history and no living descendant | `ConfirmedLineageExtinction` after absence window. |

### Task 1: Preserve Snapshot Provenance

**Files:** Modify `Assets/Scripts/Simulation/Analysis/PopulationGenomeSnapshot.cs`; modify `Assets/Tests/EditMode/PopulationGenomeSnapshotTests.cs`.

**Produces:** `IsSampled`, `SourcePopulationCount`, `SampleLimit`.

- [ ] **Step 1: Write failing capture-provenance tests.** Assert full capture yields `(false, source count, 0)`. Assert `CaptureSample(17, oneCreatureStore, 3)` yields `(true, 1, 3)` even though the sample contains every creature.
- [ ] **Step 2: Run `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~PopulationGenomeSnapshotTests"`.** Expected: compilation failure because provenance members are absent.
- [ ] **Step 3: Implement the immutable fields.** Extend the private constructor and both capture paths only; preserve current ID/genome copying and deterministic sample selection byte-for-byte.
- [ ] **Step 4: Run the focused test, then `dotnet test --filter "FullyQualifiedName!~LivenessTests"`.** Expected: pass.
- [ ] **Step 5: Commit.** Stage only those two files and commit `feat: retain genome snapshot provenance`.

### Task 2: Bind Observations and Define Explicit Policy

**Files:** Create `Assets/Scripts/Simulation/Analysis/GeneticClusterObservation.cs`; create `Assets/Scripts/Simulation/Analysis/ClusterHistoryPolicy.cs`; create `Assets/Tests/EditMode/GeneticClusterObservationTests.cs`.

**Consumes:** Task 1 and `GeneticClusters.From`.

**Produces:** observation-local cluster ordinals, exact threshold/provenance, policy values.

- [ ] **Step 1: Write failing tests.** Build a two-member snapshot at tick 10, call `GeneticClusterObservation.Create(snapshot, .5f)`, and assert tick plus matching cluster ordinals. Assert null snapshot, invalid threshold, non-positive counts/depth/windows, and fractions outside `(0, 1]` throw appropriate argument exceptions.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~GeneticClusterObservationTests"`.** Expected: compilation failure because the types do not exist.
- [ ] **Step 3: Implement both immutable analysis types.** Observation owns one private cluster result created in `Create`, delegates read access to its snapshot, and never accepts a caller-provided result. Policy is a `readonly struct` with explicit validation and value equality.
- [ ] **Step 4: Run `dotnet test --filter "FullyQualifiedName~GeneticClusterObservationTests|FullyQualifiedName~GeneticClusteringTests"`, then the fast shard.** Expected: pass with unchanged connected-component behavior.
- [ ] **Step 5: Commit.** Stage its two source files and test file; commit `feat: add cluster observations and history policy`.

### Task 3: Record Ancestry Completeness

**Files:** Modify `Assets/Scripts/Simulation/Analysis/AncestryHistory.cs`; modify `Assets/Tests/EditMode/AncestryHistoryTests.cs`.

**Produces:** `HasRecordedFounders`, `IsComplete`, `CompleteThroughTick`.

- [ ] **Step 1: Write failing tests.** Cover ordered empty-batch advancement, birth recording before advancement, decreasing `throughTick`, and a capacity-one buffer whose second write makes it overflow. Assert overflow leaves the host buffer untouched and permanently makes this history segment incomplete.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~AncestryHistoryTests"`.** Expected: failure because `RecordCompleteBatch` and watermark state are absent.
- [ ] **Step 3: Add a monotonic batch method.** Retain existing idempotent `Record` semantics; read every event in index order without clearing. Validate event order and tick bounds. Overflow permanently prevents later confirmation in this segment. Do not change event emission.
- [ ] **Step 4: Run ancestry and isolation tests, then the fast shard.** Expected: pass.
- [ ] **Step 5: Commit.** Stage source/test only; commit `feat: track ancestry event completeness`.

### Task 4: Measure Deterministic Ancestry Relations

**Files:** Create `Assets/Scripts/Simulation/Analysis/GeneticClusterRelation.cs`; create `Assets/Tests/EditMode/GeneticClusterRelationTests.cs`.

**Produces:** raw relation evidence and `IsStrong`; classification remains Task 5's responsibility.

- [ ] **Step 1: Write failing fixtures.** Use fixed stores plus explicit birth events to test direct survival, descendant-only replacement, maximum ancestor depth blocking a grandchild, missing parent evidence, and a prolific ancestor failing the previous-cluster support side.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~GeneticClusterRelationTests"`.** Expected: compilation failure because relation evidence is absent.
- [ ] **Step 3: Implement index-ordered construction.** Inspect current then previous members by index. Traverse first parent then second parent using explicit buffers and deterministic visited-ID membership checks. Expose direct/descendant counts, both fractions, contributing-old-member count, depth range, coverage state, and policy-derived `IsStrong`.
- [ ] **Step 4: Run relation and ancestry tests, then fast shard.** Expected: pass with no simulation-source diff.
- [ ] **Step 5: Commit.** Stage source/test only; commit `feat: measure ancestry-supported cluster relations`.

### Task 5: Add Bounded History Events and Classify Evidence

**Files:** Create `Assets/Scripts/Simulation/Analysis/ClusterHistoryEvent.cs`; create `Assets/Scripts/Simulation/Analysis/ClusterHistoryEventBuffer.cs`; create `Assets/Scripts/Simulation/Analysis/GeneticClusterHistory.cs`; create `Assets/Tests/EditMode/GeneticClusterHistoryTests.cs`.

**Consumes:** Tasks 2–4.

**Produces:** ordered candidate, confirmed, and unresolved evidence records.

- [ ] **Step 1: Write failing stream and classification fixtures.** Reject repeated/decreasing ticks, changed threshold, sample mode/limit, and policy. Require: exclusive split gives `CandidateSplit` then `ConfirmedSplit` only after every child uniquely continues; exclusive merge behaves symmetrically; many-to-many gives `AmbiguousReorganisation`; sampling never gives extinction; full complete absence gives `PendingDisappearance` then `ConfirmedLineageExtinction`; living weak descendants and overflow remain unresolved.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~GeneticClusterHistoryTests"`.** Expected: compilation failure because history/event types are absent.
- [ ] **Step 3: Implement bounded external output and state machine.** Model the event buffer on `SimulationEventBuffer`: capacity, indexed read, `TryWrite`, `Overflowed`, `Clear`. Validate segment compatibility before relation work. Build each bipartite component by ordinal, classify exclusively, retain only prior observation and candidates inside policy windows, and scan newest full observation for same-ID/recorded descendants before confirmed extinction.
- [ ] **Step 4: Run the required verification shards.** Build first; run focused history/relation tests; run `FullyQualifiedName!~LivenessTests`; run `FullyQualifiedName~PlantLivenessTests`; run `FullyQualifiedName~LivenessTests&FullyQualifiedName!~RiskAversionIsLiveOnlyWhenThreatsExist`. Run RiskAversion alone with a 20-minute timeout and report its actual outcome without claiming a full-suite pass unless observed.
- [ ] **Step 5: Commit.** Stage its three source files and test file; commit `feat: add ancestry-aware cluster history`.

### Task 6: Prove Isolation and Record the P5 Boundary

**Files:** Modify `Assets/Tests/EditMode/AnalysisIsolationTests.cs`; modify `docs/AGENT_FIELD_NOTES.md`; modify `docs/SESSION_HANDOFF.md`.

**Produces:** hash-equality proof and clear lead-agent status.

- [ ] **Step 1: Extend the isolation fixture.** Only beside the observed world: drain complete event batches, capture an observation at explicit threshold `.25f`, and record history. Keep the existing equality assertion on `ComputeStateHash`.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~AnalysisIsolationTests"`.** Expected: equal hashes. A mismatch blocks completion.
- [ ] **Step 3: Update lead-agent records.** Append one dated lesson only if the implementation revealed a reusable rule. Update standing/open work to say P5 history is analysis-only, threshold/provenance scoped, and requires complete ancestry for confirmation; state separately if UI or durable chunk storage remains unbuilt.
- [ ] **Step 4: Run Task 5's shard set and Unity batch compile.** Expected: every observed shard passes and Unity exits `0`.
- [ ] **Step 5: Commit.** Stage exactly the isolation test and two documents; commit `docs: record ancestry-aware cluster history`.

## Plan Review

| Spec requirement | Plan tasks |
| --- | --- |
| Provenance and no snapshot/cluster mismatch | 1–2 |
| Overflow-aware complete ancestry | 3 |
| Explicit deterministic descendant evidence | 4 |
| Bounded external events and compatible segments | 5 |
| Split, merge, ambiguity, strict extinction | 5 |
| Hash isolation, Unity compile, handoff | 6 |

The plan covers every specification section, defines types before later tasks consume them, and uses small deterministic fixtures rather than a full ecology run. It intentionally changes no simulation behavior.

# Decision Policy Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a versioned, allocation-free intent/decision contract and produce evidence showing whether Legacy resource decisions dither before changing policy behavior.

**Architecture:** Preserve the existing scorer as `Legacy` so historical experiment hashes and paired-seed results remain meaningful. Add fixed-capacity value-type candidate and trace records beside the existing behavior state; Legacy continues to choose and execute decisions exactly as before. A bounded opt-in trace recorder samples selected creatures without allocations during a run.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, pure simulation assembly.

## Global Constraints

- Do not change the Legacy selection/execution result for an identical seed/configuration.
- Do not introduce managed allocations, delegates, or new throwing validation into fixed-tick hot loops.
- Keep `Eat`, `Drink`, `FeedCarcass`, and `Attack` as deterministic execution states; keep reproduction pairwise.
- Stop after producing and reporting a Legacy decision trace; top-K, travel/risk scoring, stickiness, and heritable policy weights are out of this slice.

---

### Task 1: Define versioned intent and trace records

**Files:**
- Modify: `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs`
- Test: `Assets/Tests/EditMode/SpatialBehaviorTests.cs`

- [ ] Write failing tests proving a fixed candidate buffer selects the highest score and stable action/target tie-break, and that `SimulationConfig` defaults to `Legacy`.
- [ ] Add `DecisionPolicyVersion`, `CreatureIntent`, `DecisionCandidate`, and a fixed-capacity `DecisionCandidateBuffer` that uses fields/value types only.
- [ ] Add immutable policy version to `SimulationConfig`, defaulting every existing factory and constructor call to `Legacy`.
- [ ] Run EditMode tests and commit this contract-only change.

### Task 2: Preserve Legacy behavior while separating execution

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Test: `Assets/Tests/EditMode/CoreSimulationTests.cs`

- [ ] Write a failing deterministic regression test that runs a fixed Legacy scenario twice and requires identical final state hashes.
- [ ] Route current Legacy selection through an explicit selection method and retain the current seek-to-eat/drink/feed/attack transitions in a deterministic execution method.
- [ ] Run all EditMode tests and confirm the Legacy state-hash regression passes.
- [ ] Commit the behavior-preserving split.

### Task 3: Capture bounded sampled Legacy decision traces

**Files:**
- Create: `Assets/Scripts/Simulation/Experiments/DecisionTrace.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs`
- Modify: `Assets/Tests/EditMode/CoreSimulationTests.cs`
- Modify: `README.md`

- [ ] Write failing tests for bounded trace capture: record current/winning actions and targets, candidate score components, switch events, and invalidation reason without changing state hashes when tracing is disabled.
- [ ] Add an opt-in fixed-size recorder configured outside the hot loop; do not allocate per decision.
- [ ] Capture a deterministic Legacy trace for a deliberately close food/water scenario, report target-switch count and entries, and stop before changing stickiness.
- [ ] Clarify README: ground-plane simulation movement inside a 3D Unity scene.
- [ ] Run all EditMode tests, compile the simulation assembly through Unity batch mode, inspect the worktree, and commit.

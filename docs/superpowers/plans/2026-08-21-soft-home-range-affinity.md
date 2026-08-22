# Soft Home-Range Affinity Implementation Plan

> **SUPERSEDED — CLOSED AS A MEASURED NEGATIVE, 2026-08-22.** The implementation described here is
> correct, deterministic and byte-identical when disabled, and it stays in the tree with
> `HomeRangeAffinityEnabled` defaulting `false`. Its *ecological purpose* — recognisable repeatable
> routes — was measured across two experiments, five conditions and 240 fixed-seed runs and does
> not occur. In the shipped observation scenarios the route metric is saturated at 1.0000 with the
> flag off (delta +0.0000, and +0.0001 at a 10x bonus). In `ObservationRouteRing`, purpose-built so
> that a route can exist and delivering 90.6% decision opportunity at 0.88 familiarity, route
> repeatability **fell** (-0.0345, t -2.87, 8/30 seeds up) while same-site re-entry **rose**
> (+0.0594, t +4.93, 26/30 up). A proximity-to-recent-success bonus rewards clinging, not
> circuits. **Do not tune the constants and do not reopen this design** — the sign of the effect is
> wrong, not its size. Evidence:
> `docs/experiments/p4a-home-range-affinity-2026-08-22.md` and
> `docs/experiments/p4a-route-ring-home-range-2026-08-22.md`.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add flag-gated, deterministic, non-genetic familiarity preference for successful creatures without using place memory.

**Architecture:** `HomeRangeState` stores a compact centre and familiarity. A pure `HomeRangeSystem` records success, decays familiarity, and produces a bounded ordinary food/water candidate bonus; `SimulationWorld` wires it only behind a disabled-by-default flag.

**Tech Stack:** deterministic C#, Unity EditMode tests through `tools/HeadlessTests`.

**Spec:** `docs/superpowers/specs/2026-08-21-soft-home-range-affinity-design.md`

## Global Constraints

- Place-memory observation/decay and its liveness assertion remain inert and unchanged.
- Flag-off worlds remain byte-identical; no RNG, unordered iteration, or tick allocation.
- Affinity applies only to valid ordinary food/water candidates, never flee, mate, fallback movement, or unavailable targets.
- Newborns receive default state and inherit no territory.

---

### Task 1: Add isolated state and arithmetic

**Files:** Create `Assets/Scripts/Simulation/Behavior/HomeRangeSystem.cs`, `Assets/Tests/EditMode/HomeRangeSystemTests.cs`; modify `SimulationTypes.cs`, `CreatureStore.cs`, `SimulationConfig.cs`.

**Produces:** `HomeRangeState`, `HomeRangeSystem.RecordSuccess`, `TickDecay`, `GetCandidateBonus`, and false-by-default explicit config.

- [ ] **Step 1: Write failing pure tests.** Assert success moves centre toward position/raises familiarity, decay clamps to zero without moving centre, near candidate gets bounded bonus, and blank state gets zero.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~HomeRangeSystemTests"`.** Expect missing-type compile failure.
- [ ] **Step 3: Implement minimal pure operations and storage lifecycle.** Keep state centre/familiarity only; explicit fixed arithmetic; no place-memory reads. Test newborn/add/swap-remove state defaults.
- [ ] **Step 4: Run focused tests and commit.**

```bash
git add Assets/Scripts/Simulation/Core/SimulationTypes.cs Assets/Scripts/Simulation/Core/CreatureStore.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Scripts/Simulation/Behavior/HomeRangeSystem.cs Assets/Tests/EditMode/HomeRangeSystemTests.cs
git commit -m "feat: add soft home-range state"
```

### Task 2: Wire narrow flag-gated behavior

**Files:** Modify `SimulationWorld.cs`, `DecisionSystem.cs`; create `Assets/Tests/EditMode/HomeRangeAffinityTests.cs`.

**Consumes:** Task 1 state/system/config APIs.

- [ ] **Step 1: Write failing tests.** Assert enabled affinity prefers a nearer equal food candidate; disabled paired worlds retain equal hash; threat/flee/mating get no bonus; successful food/water/reproduction records state only when enabled.
- [ ] **Step 2: Run `dotnet test --filter "FullyQualifiedName~HomeRangeAffinityTests"`.** Expect failure before wires exist.
- [ ] **Step 3: Wire only existing success branches and ordinary candidate scoring.** Call `RecordSuccess` beside existing resource-outcome/reproduction success only when enabled; decay only when enabled; precompute/pass bonus into ordinary score path; extend hash inside enabled branch only.
- [ ] **Step 4: Verify.**

```bash
dotnet test --filter "FullyQualifiedName~HomeRange|FullyQualifiedName~LivenessTests.PlaceMemoryProbesRunButNeverTakeEffect|FullyQualifiedName~CoreSimulationTests"
dotnet test --filter "FullyQualifiedName!~LivenessTests"
```

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Tests/EditMode/HomeRangeAffinityTests.cs
git commit -m "feat: add soft home-range affinity"
```

### Task 3: Verify and record the P4a boundary

**Files:** Modify `docs/AGENT_FIELD_NOTES.md` only for a reusable lesson; modify `docs/SESSION_HANDOFF.md` only after verification.

- [ ] **Step 1: Run build, fast shard, PlantLiveness, and Liveness excluding RiskAversion; run RiskAversion alone with 20-minute limit.** Report actual outcomes only.
- [ ] **Step 2: Run Unity batch compile if project lock is released; otherwise report it as unobserved.**
- [ ] **Step 3: Record verified status only and commit docs.**

```bash
git add docs/AGENT_FIELD_NOTES.md docs/SESSION_HANDOFF.md
git commit -m "docs: record soft home-range verification"
```

## Plan Review

Task 1 creates testable state; Task 2 contains every behavior wire and the byte-identical flag-off proof; Task 3 contains final verification. The plan never wires protected inert place memory or builds territory, packs, navigation, inheritance, or UI.

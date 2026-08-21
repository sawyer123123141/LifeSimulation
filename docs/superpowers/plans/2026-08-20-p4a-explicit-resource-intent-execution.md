# Explicit Resource-Intent Execution Implementation Plan

> **WITHDRAWN 2026-08-20.** The premise was already satisfied: `SimulationWorld.ResolveResourceInteractions` requires a valid, active, non-empty, matching in-range resource index before creating intake. No implementation is needed.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure a remembered food or water destination cannot directly execute eating or drinking until perception resolves a matching visible resource.

**Architecture:** Keep remembered-place scoring and movement in the pure simulation layer. At action execution, validate a concrete, visible resource of the needed kind and interaction range; otherwise preserve movement/search semantics and clear an arrived-but-unresolved remembered target. Unity only reads the resulting decision state.

**Tech Stack:** Unity 6, C#, pure deterministic simulation, Unity Test Framework / headless NUnit.

**Spec:** `docs/ROADMAP.md` — P4a “explicit resource-intent execution”.

## Global Constraints

- Simulation truth remains independent of GameObjects and rendering.
- Use deterministic array/index iteration only; no allocation or LINQ in tick paths.
- Preserve byte-identical behavior for decisions that already resolve to a visible matching resource.
- Never change tests to accept altered output; add focused fixtures only.
- No Unity navigation, flocking, or MonoBehaviour state becomes simulation truth.

---

## Contracts

| Contract | Producer | Consumer | Required behavior |
|---|---|---|---|
| remembered target | `CreatureMemory.ActiveRememberedTarget` | `SimulationWorld` decision/action resolution | May guide travel; is not proof that a resource remains consumable. |
| visible resource | perception/resource query at the creature position | `SimulationWorld` intake request builder | Must match Food/Water kind and interaction range before `Eat`/`Drink` allocates intake. |
| unresolved arrival | action resolution | movement/decision state | Does not allocate intake; clears the stale active remembered target so the next decision can search normally. |

## Behavior table

| Intent source | Matching visible resource in range | Result |
|---|---|---|
| Current perception | yes | Existing `Eat`/`Drink` request behavior is unchanged. |
| Remembered target | yes | Execute the matching intake request. |
| Remembered target | no | Do not execute intake; clear the active remembered target and transition back to search/movement. |
| Remembered food with only water visible | no | Do not drink; retain resource-kind correctness. |

### Task 1: Define the resolved-resource seam

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs` in the resource-intake request path near the `Eat`/`Drink` action checks.
- Test: `Assets/Tests/EditMode/PlaceMemoryDecisionTests.cs`.

**Interfaces:**
- Consumes: `CreatureDecision.Action`, `CreatureMemory.HasActiveRememberedTarget`, `ResourceKind`, and the existing resource interaction-radius query.
- Produces: an intake request only when a matching concrete resource index is resolved; otherwise no request and a cleared remembered target.

- [ ] **Step 1: Add one failing remembered-food fixture**

  Create one deterministic fixture in `PlaceMemoryDecisionTests.cs`: place a creature at its remembered food coordinate, remove/deactivate that food before the action step, and assert no food intake occurs and `HasActiveRememberedTarget` becomes false.

- [ ] **Step 2: Run the focused fixture and verify the current failure**

  Run: `dotnet test --filter "FullyQualifiedName~PlaceMemoryDecisionTests"`

  Expected: the new assertion fails because remembered arrival is presently permitted to reach intake without a resolved resource.

- [ ] **Step 3: Implement the minimum resolution check**

  In the existing `SimulationWorld` intake-request construction branch, reuse its deterministic resource scan to require an active, in-range resource whose `ResourceKind` matches the `Eat`/`Drink` decision. On failed resolution of an active remembered target, clear only `HasActiveRememberedTarget`; do not alter unrelated memory slots or decision scoring.

- [ ] **Step 4: Run the focused fixture and existing decision tests**

  Run: `dotnet test --filter "FullyQualifiedName~PlaceMemoryDecisionTests|FullyQualifiedName~DecisionSystemTests"`

  Expected: all selected tests pass, including the new stale-target case and existing visible-resource behavior.

- [ ] **Step 5: Commit**

  Run: `git add Assets/Scripts/Simulation/Core/SimulationWorld.cs Assets/Tests/EditMode/PlaceMemoryDecisionTests.cs && git commit -m "fix: require visible resource for intake"`

### Task 2: Pin matching-kind and unchanged-visible contracts

**Files:**
- Modify: `Assets/Tests/EditMode/PlaceMemoryDecisionTests.cs`.

**Interfaces:**
- Consumes: the resolved-resource seam from Task 1.
- Produces: regression coverage that rejects a visible resource of the wrong kind and preserves existing concrete-resource intake.

- [ ] **Step 1: Add one table-driven template fixture in the existing test file**

  Add cases for remembered-food/visible-water and remembered-food/visible-food. Assert zero food intake and target clearing for the former; assert positive food intake for the latter.

- [ ] **Step 2: Run the fixture before any behavior change**

  Run: `dotnet test --filter "FullyQualifiedName~PlaceMemoryDecisionTests"`

  Expected: the wrong-kind case fails if the Task 1 predicate is incomplete.

- [ ] **Step 3: Restrict the predicate to exact resource kind**

  Keep the existing action-to-kind mapping: `Eat` resolves only `ResourceKind.Food`; `Drink` resolves only `ResourceKind.Water`. Do not add a fallback action.

- [ ] **Step 4: Run focused and shard verification**

  Run: `dotnet test --no-build --filter "FullyQualifiedName!~LivenessTests"`

  Expected: 365 passing tests (or the updated expected count if the two additions increase it).

- [ ] **Step 5: Commit**

  Run: `git add Assets/Tests/EditMode/PlaceMemoryDecisionTests.cs && git commit -m "test: pin resolved resource intent"`

## Self-review

- Spec coverage: the plan addresses the one selected P4a bullet and keeps resource verification in headless simulation; it intentionally does not claim home range, clustering, rendezvous, juvenile bias, presentation feedback, or scenario controls.
- Placeholder scan: no deferred implementation placeholders; each task names its sole implementation/test files and pass/fail command.
- Type consistency: all named state and action types exist in `SimulationWorld`/`SimulationTypes`; the plan adds no new public data type.

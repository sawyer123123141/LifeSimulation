# P5 History Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make P5 cluster-history evidence visible in Prototype 1 without changing simulation behavior or claiming species labels.

**Architecture:** A new Unity-facing `P5HistoryPanelSession` owns the analysis session, event draining, cadence, and bounded display history. `Prototype1Presenter` creates, advances, and draws that session; it remains the only Unity integration point. The simulation core is read-only from this feature’s perspective.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests through `tools/HeadlessTests`.

**Spec:** `docs/superpowers/specs/2026-08-21-p5-history-panel-design.md`

## Global Constraints

- Do not modify `SimulationWorld`, biology, config, RNG, event emission, hashes, or hot simulation paths.
- The panel must drain `world.Events` before `Prototype1Presenter` clears it, while never clearing it itself.
- All analysis settings are explicit and visibly labelled; tracks are not species and no probability-like confidence is rendered.
- Full snapshots only; session reset discards every observation, ancestry record, track, and displayed event.
- Preserve the existing presenter controls and generated Unity `.meta` rules; do not stage `.meta` files or use `git add -A`.

---

## File Structure

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Simulation/Analysis/P5HistoryPanelSession.cs` | Pure host-triggered session helper, owned only by the presenter; explicit settings, host-event draining, observation cadence, and bounded display-event reads. |
| `Assets/Scripts/Presentation/Prototype1Presenter.cs` | Constructs/resets/advances the session and renders its compact on-screen panel. |
| `Assets/Tests/EditMode/P5HistoryPanelSessionTests.cs` | Pure deterministic contracts for timing, event drain ordering, reset, and no simulation-state feedback. |

### Task 1: Build the presenter-side P5 session

**Files:**
- Create: `Assets/Scripts/Simulation/Analysis/P5HistoryPanelSession.cs`
- Create: `Assets/Tests/EditMode/P5HistoryPanelSessionTests.cs`

**Interfaces:**
- Consumes: `SimulationWorld`, `SimulationEventBuffer`, `PopulationGenomeSnapshot.Capture`, `AncestryHistory`, `GeneticClusterObservation.Create`, `GeneticClusterHistory.Record`, `ClusterHistoryEventBuffer`.
- Produces: `P5HistoryPanelSession.CreateForWorld(SimulationWorld world)`, `Advance(SimulationWorld world)`, `GetEventAt(int index)`, `DisplayEventCount`, `NextObservationTick`, `AncestryCompleteThroughTick`, `OutputOverflowed`, `StatusText`.

- [ ] **Step 1: Write failing session contracts.**

```csharp
[Test]
public void AdvanceRecordsEventsBeforeTheHostBufferIsCleared()
{
    SimulationWorld world = CreateWorld();
    P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);
    world.Step(world.Config.FixedDeltaTime);

    session.Advance(world);

    Assert.That(world.Events.Count, Is.GreaterThanOrEqualTo(0));
    Assert.That(session.AncestryCompleteThroughTick, Is.EqualTo(world.CurrentTick));
}

[Test]
public void ObservationCadenceCapturesAFullPopulationAtTheDeclaredTick()
{
    SimulationWorld world = CreateWorld();
    P5HistoryPanelSession session = P5HistoryPanelSession.CreateForWorld(world);

    StepAndAdvance(world, session, P5HistoryPanelSession.ObservationIntervalTicks);

    Assert.That(session.ObservationCount, Is.EqualTo(1));
    Assert.That(session.LastObservationWasSampled, Is.False);
}
```

- [ ] **Step 2: Run the focused test to verify it fails.**

Run: `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~P5HistoryPanelSessionTests"`

Expected: compilation failure because `P5HistoryPanelSession` is absent.

- [ ] **Step 3: Implement a narrow session owner.**

```csharp
public sealed class P5HistoryPanelSession
{
    public const int ObservationIntervalTicks = 300;
    public const float GeneticThreshold = .25f;

    public static P5HistoryPanelSession CreateForWorld(SimulationWorld world);
    public void Advance(SimulationWorld world);
    public int DisplayEventCount { get; }
    public ClusterHistoryEvent GetEventAt(int index);
}
```

`CreateForWorld` records founders and creates one explicit `ClusterHistoryPolicy`, `AncestryHistory`, `GeneticClusterHistory`, and fixed-size `ClusterHistoryEventBuffer`. `Advance` first calls `RecordCompleteBatch(world.Events, world.CurrentTick)`, then records a full observation only on the explicit cadence, then copies output records in index order into a fixed display ring/list. It never calls `world.Events.Clear`, `world.Step`, or any simulation setter. Convert P5 rejection/absence into non-authoritative `StatusText`; do not swallow unexpected programming errors.

- [ ] **Step 4: Add reset/isolation and wording tests.**

```csharp
[Test]
public void FreshSessionCannotCarryTracksOrDisplayedEventsAcrossAReset()
{
    SimulationWorld firstWorld = CreateWorld();
    P5HistoryPanelSession first = P5HistoryPanelSession.CreateForWorld(firstWorld);
    StepAndAdvance(firstWorld, first, P5HistoryPanelSession.ObservationIntervalTicks * 2);

    SimulationWorld resetWorld = CreateWorld();
    P5HistoryPanelSession reset = P5HistoryPanelSession.CreateForWorld(resetWorld);

    Assert.That(reset.DisplayEventCount, Is.EqualTo(0));
    Assert.That(reset.ObservationCount, Is.EqualTo(0));
    Assert.That(reset.StatusText, Does.Not.Contain("species"));
}
```

Use a paired identical-world fixture: advance only one session beside one world, then assert the two `ComputeStateHash` values match after equal steps.

- [ ] **Step 5: Run focused verification.**

Run: `dotnet test --filter "FullyQualifiedName~P5HistoryPanelSessionTests|FullyQualifiedName~AnalysisIsolationTests|FullyQualifiedName~GeneticClusterHistoryTests"`

Expected: all pass; equal state hashes prove no feedback to simulation truth.

- [ ] **Step 6: Commit.**

```bash
git add Assets/Scripts/Simulation/Analysis/P5HistoryPanelSession.cs Assets/Tests/EditMode/P5HistoryPanelSessionTests.cs
git commit -m "feat: add P5 history panel session"
```

### Task 2: Integrate and render the compact history panel

**Files:**
- Modify: `Assets/Scripts/Presentation/Prototype1Presenter.cs`
- Modify: `Assets/Tests/EditMode/P5HistoryPanelSessionTests.cs`

**Interfaces:**
- Consumes: `P5HistoryPanelSession.CreateForWorld`, `Advance`, status and event accessors from Task 1.
- Produces: presenter reset-safe P5 session lifecycle and a visible bounded evidence panel.

- [ ] **Step 1: Write a failing integration-oriented test.**

```csharp
[Test]
public void SessionStatusMakesOutputOverflowVisibleWithoutInventingHistory()
{
    P5HistoryPanelSession session = CreateSessionWithSmallOutputCapacity();
    FillOutputUntilOverflow(session);

    Assert.That(session.OutputOverflowed, Is.True);
    Assert.That(session.StatusText, Does.Contain("dropped"));
    Assert.That(session.StatusText, Does.Not.Contain("species"));
}
```

- [ ] **Step 2: Run the focused test to verify it fails.**

Run: `dotnet test --filter "FullyQualifiedName~P5HistoryPanelSessionTests"`

Expected: failure because the capacity/status contract is not yet exposed.

- [ ] **Step 3: Implement presenter lifecycle and drawing.**

```csharp
private P5HistoryPanelSession _p5HistorySession;

private void ResetSimulation(PrototypeScenario scenario)
{
    // Existing world reset work.
    _p5HistorySession = P5HistoryPanelSession.CreateForWorld(_world);
}

private void DrawP5HistoryPanel()
{
    // Draw status first, then at most eight newest-first immutable evidence rows.
}
```

Inside the existing `while` loop, immediately after each `_world.Step`, call `_p5HistorySession.Advance(_world)`, capture the recent event, then clear `_world.Events`. Remove the once-per-frame event capture/clear that would otherwise replay prior ticks. This preserves every cadence boundary and drains each tick's event batch exactly once. Draw a panel outside the existing inspector/population boxes. Each row must start with candidate/confirmed/unresolved status and call confirmed extinction “lineage extinction evidence.” Show threshold, cadence, full mode, ancestry completeness, explicit policy settings, and overflow/dropped-record notice. Do not create keys for this slice.

- [ ] **Step 4: Run focused and fast verification.**

Run: `dotnet test --filter "FullyQualifiedName~P5HistoryPanelSessionTests|FullyQualifiedName~AnalysisIsolationTests|FullyQualifiedName~GeneticClusterHistoryTests"`

Run: `dotnet test --filter "FullyQualifiedName!~LivenessTests"`

Expected: all pass with no hash change.

- [ ] **Step 5: Run Unity batch compile.**

Run the known Unity 6000.2.14f1 batch compile once the editor does not hold the project lock.

Expected: exit `0`; if the editor lock blocks it, report “not observed due to project lock,” not a compile pass/failure.

- [ ] **Step 6: Commit.**

```bash
git add Assets/Scripts/Presentation/Prototype1Presenter.cs Assets/Tests/EditMode/P5HistoryPanelSessionTests.cs
git commit -m "feat: show P5 history evidence"
```

## Plan Review

| Spec requirement | Task coverage |
| --- | --- |
| Presenter-only ownership and no simulation feedback | Task 1 session + paired hash test; Task 2 integration placement |
| Founders/event drain before host clear | Task 1 contract; Task 2 lifecycle order |
| Explicit cadence, threshold, policy, bounded output | Task 1 API/implementation; Task 2 labels |
| Auditable candidate/confirmed/unresolved display | Task 2 drawing contract |
| Reset isolation and no cross-scenario tracks | Task 1 reset test and Task 2 reset lifecycle |
| Overflow/incomplete honesty | Task 1 status and Task 2 display |
| No storage/tree/taxonomy scope expansion | Global constraints and Task 2 limits |

Self-review: the plan creates a single presentation helper instead of enlarging the already large history engine; all later interfaces are defined in Task 1; no placeholders, default biological threshold claims, simulation changes, or durable-storage work are included.

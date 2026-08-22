# P5 History Panel Design

**Status:** proposed for review  
**Date:** 2026-08-21

## Purpose

Make the existing P5 cluster-history evidence visible while a Prototype 1 simulation runs. The panel is a presentation-only window onto externally produced analysis records; it neither changes biology nor turns a genetic cluster into an authoritative species label.

## Scope

The first slice adds a compact history panel to `Prototype1Presenter`.

- It starts and owns a fresh, presentation-side P5 analysis session whenever a scenario resets.
- It records founders, drains simulation events into its bound `AncestryHistory` before clearing the host event buffer, and creates full-population observations on one explicit cadence.
- It uses one declared threshold and one explicit `ClusterHistoryPolicy` for the session.
- It displays a bounded newest-first list of P5 evidence records: event kind, candidate/confirmed/unresolved status, observed tick range, track IDs, and an uncertainty/provenance summary.
- It always labels sampled observations, incomplete ancestry, and unresolved evidence plainly. It never renders a probability or calls a track a species.

The panel is deliberately ephemeral. Resetting the scenario discards its observations and displayed events. Durable chunk storage, cross-run timelines, export, and a graphical lineage tree remain separate work.

## Boundaries

`Prototype1Presenter` is the only new P5 host in this slice. It may allocate and invoke P5 analysis after simulation stepping, because it is Unity-facing presentation code. `SimulationWorld` and every simulation system remain unchanged.

```
SimulationWorld.Step
        │
        ├─ Events (read by presenter before its existing Clear)
        │       └─ AncestryHistory.RecordCompleteBatch
        │
        └─ full PopulationGenomeSnapshot at declared cadence
                └─ GeneticClusterObservation
                        └─ GeneticClusterHistory
                                └─ bounded event buffer
                                        └─ presenter history panel
```

The presenter owns the entire session—founders, ancestry history, cluster history, output buffer, cadence counter, and declared settings. No result is written back to simulation state, creature state, configuration, RNG, or hashes.

## Session settings

The initial values are visible labels, not universal biology:

| Setting | Initial value | Reason |
| --- | --- | --- |
| Observation cadence | 300 simulation ticks | Avoids a per-frame clustering cost while leaving events inspectable. |
| Genetic threshold | 0.25 | Matches the existing P5 isolation fixture; panel labels it explicitly. |
| Policy | Explicit fixed presenter constants | Required by P5; every field is displayed in a short policy line. |
| P5 output capacity | 64 records | Bounds presentation memory and makes overflow visible. |
| Display rows | 8 newest records | Keeps the prototype HUD readable. |

If a session output buffer overflows, the panel says records were dropped. It must not reconstruct, sort, or infer missing history.

## Display contract

The panel appears beside the existing population-condition box and has four stable sections:

1. **Session line:** threshold, observation cadence, full/sampled mode, and ancestry completeness through tick.
2. **Policy line:** compact support/depth/persistence values, expressly described as analysis settings.
3. **Latest evidence:** newest-first bounded rows. Each row shows status first, then event kind, tick/range, and affected tracks.
4. **Evidence note:** incomplete ancestry, sampled mode, weak/living descendant, ambiguous reorganisation, or buffer overflow is shown as a plain limitation. A confirmed extinction row says “lineage extinction evidence,” never “species extinct.”

The panel does not display raw per-relation fractions in the first slice; the immutable event still retains them for later inspection. This avoids inventing a dense viewer before a durable history path exists.

## Failure handling

- A history analysis rejection caused by incompatible observations or incomplete ancestry becomes a visible unresolved note and does not interrupt the simulation.
- Analysis output overflow is visible but does not alter simulation events or the P5 analysis rules.
- Reset always constructs a new analysis session. Tracks never cross scenarios, cadence changes, or a new ancestry root.
- If no observation has occurred yet, the panel says when the first one will occur instead of implying no evolutionary change happened.

## Testing

EditMode tests cover a small presenter-extracted session helper rather than Unity GUI pixels:

- founder capture and event draining occur before the host event buffer is cleared;
- identical observed and unobserved worlds retain equal state hashes;
- the configured cadence produces a full observation at the expected tick;
- reset creates a new analysis session with no carried track/output records; and
- panel formatting preserves status/provenance wording and does not call tracks species.

The Unity batch compile verifies the presenter integration. The existing P5 synthetic tests remain the proof of classification correctness; presentation tests do not duplicate them.

## Non-goals

- durable chunk storage, export, or cross-run history;
- graphical trees, track picking, or a species taxonomy;
- automatic tuning of thresholds or policy;
- any readback from analysis to simulation behavior; and
- changes to default scenarios or to the deterministic simulation layer.

## Decision requested

Approve this constrained presenter-side history panel as the first P5 visibility slice. It makes evidence watchable now while preserving a clean boundary for later durable storage and richer visualization.

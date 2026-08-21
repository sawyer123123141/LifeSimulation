# Conservative Genetic-Cluster History Design

**Status:** proposed for review  
**Date:** 2026-08-21

## Purpose

P5 can already take deterministic population genome snapshots and form genetic clusters at an explicit distance threshold. This design adds a read-only analysis layer that compares successive cluster observations and records evidence for cluster continuity, split candidates, and merge candidates.

It must make uncertainty visible. A genetic cluster is an analytical grouping at one threshold and one observation time; it is not a biological species, and its disappearance does not by itself establish biological extinction.

## Constraints

- The feature lives outside `SimulationWorld` and never changes simulation decisions, configuration, RNG calls, ordering, or hashes.
- The caller supplies every interpretation policy value explicitly. There is no hidden genetic-distance threshold or universal species definition.
- Observations are compared only when they have the same genetic threshold and the same sampling mode.
- The analysis uses stable creature IDs only for continuity evidence. It does not infer ancestry from genetic similarity alone.
- It stores only the bounded pending state needed to confirm a relationship. The host owns durable experiment output and long-term storage.

## Terms

| Term | Meaning |
| --- | --- |
| Cluster observation | One connected component produced from one genome snapshot at one explicit threshold. |
| Strong overlap | Enough of two adjacent observations consists of the same live creature IDs to meet the caller's policy. |
| Cluster track | A read-only analytical identity carried forward through a unique strong-overlap relationship. It is not an entity in the simulation. |
| Candidate split / merge | A graph pattern observed once; useful evidence but not yet a confirmed event. |
| Confirmed split / merge | A candidate whose resulting tracks persist for the requested number of later observations. |
| Unresolved arrival / disappearance | A cluster that cannot be linked strongly to an adjacent observation. It is deliberately not labelled colonisation, speciation, or extinction. |

## Input contract

`GeneticClusterHistory.Record` receives, in chronological order:

- a `PopulationGenomeSnapshot`;
- `GeneticClusters` constructed from that exact snapshot;
- the explicit genetic-distance threshold used to construct the clusters; and
- a `ClusterHistoryPolicy` supplied by the host.

The snapshot needs provenance metadata before history is implemented:

- `IsSampled` — whether it came from `CaptureSample` rather than a complete capture;
- `SourcePopulationCount` — the live population count at capture; and
- `SampleLimit` — zero for a complete capture, otherwise the requested maximum sample size.

History rejects a stream when any of these change: threshold, sampling mode, sample limit, policy, or non-increasing tick. This prevents a visual/performance sampling change from silently becoming a population-history finding.

## Overlap rule

For every previous/current cluster pair, intersect their member IDs. A pair has a strong overlap when both conditions hold:

```text
sharedMemberCount >= policy.MinimumSharedMembers
sharedMemberCount / min(previousMemberCount, currentMemberCount)
    >= policy.MinimumOverlapFraction
```

Using the smaller cluster as the denominator lets a genuine subgroup retain continuity with its former larger cluster, which is necessary to observe a split. `MinimumSharedMembers` prevents one animal from creating a relationship on its own.

The policy is immutable and validates these inputs at its public constructor:

| Field | Meaning |
| --- | --- |
| `MinimumSharedMembers` | Positive count required in every strong overlap. |
| `MinimumOverlapFraction` | Fraction in `(0, 1]` required in every strong overlap. |
| `RequiredSuccessorObservations` | Positive number of subsequent observations that each resulting track must survive before an event is confirmed. |

The first tests will use explicit values chosen by the test, rather than a production default. A later experiment may calibrate a scenario-specific policy, but may not promote that calibration to a biological constant.

## Relationship classification

Build the bipartite graph of strong overlaps between adjacent observations.

| Previous-to-current pattern | Classification | What is recorded |
| --- | --- | --- |
| One previous cluster to one current cluster | Continuity | The current cluster continues the existing analytical track. |
| One previous cluster to two or more current clusters | Candidate split | A parent track and every child track; confirmation remains pending. |
| Two or more previous clusters to one current cluster | Candidate merge | Every parent track and the successor track; confirmation remains pending. |
| No strong predecessor for a current cluster | Unresolved arrival | The new cluster begins an unlinked track. |
| No strong successor for a previous cluster | Unresolved disappearance | The old track ends without a biological claim. |
| A component with both multiple predecessors and multiple successors | Ambiguous reorganisation | Preserve the graph and mark it unresolved; do not force it into a split or merge label. |

An unresolved disappearance is **not** biological extinction. It can mean extinction, dispersal outside a sample, rapid membership turnover, a changed operating point, or a threshold artefact. Biological lineage extinction needs separate ancestry-and-live-descendant evidence and is outside this feature.

## Persistence rule

When a candidate split is seen, the history creates pending child tracks. On each subsequent compatible observation, every child must have exactly one strong successor for the pending relation to advance by one. A new split, merge, loss of continuity, or ambiguous relation stops confirmation and records the candidate as unresolved.

A candidate merge is symmetric: its successor track must have exactly one strong successor on every subsequent compatible observation. Each input track is retained in the event record.

When the number of successful successor observations reaches `RequiredSuccessorObservations`, emit one immutable confirmed event. This means a relationship is reported only after it survives beyond the one snapshot where it was first seen. The pending object is then discarded.

The host can still inspect candidate and unresolved records; confirmation must never erase contrary evidence.

## Proposed analysis API

The names below describe the contract, not an implementation commitment:

```csharp
public readonly struct ClusterHistoryPolicy { ... }
public enum ClusterHistoryEventKind {
    Continuity,
    CandidateSplit,
    CandidateMerge,
    ConfirmedSplit,
    ConfirmedMerge,
    UnresolvedArrival,
    UnresolvedDisappearance,
    AmbiguousReorganisation
}
public sealed class GeneticClusterHistory {
    public void Record(
        in PopulationGenomeSnapshot snapshot,
        in GeneticClusters clusters,
        float geneticDistanceThreshold,
        in ClusterHistoryPolicy policy);
}
```

Events expose their observation ticks, participating read-only track IDs, member counts, and overlap counts. They do not expose or mutate simulation state.

## Test strategy

One focused test file will construct tiny fixed snapshots with stable IDs. It will cover:

1. a unique strong-overlap continuation;
2. a split candidate that becomes confirmed only after the configured later continuations;
3. a merge candidate with the same persistence requirement;
4. an ambiguous many-to-many reorganisation that never receives a split/merge claim;
5. an unlinked disappearance recorded as unresolved, never as extinction;
6. rejection of changing threshold, sampling provenance, policy, or tick order; and
7. analysis isolation: recording histories alongside a world leaves its deterministic state hash identical to an unobserved world.

No Unity presentation work is needed for this unit. A later display can render the records, but cannot feed them back to the simulation.

## Non-goals

- Naming species or assigning a permanent biological taxonomy.
- Declaring biological extinction, colonisation, speciation, predation, migration, or environmental causality.
- Selecting a universal cluster threshold, overlap threshold, or persistence duration.
- Persisting an unbounded timeline inside the simulation process.
- Changing any ecology, behaviour, or default scenario.

## Decision requested

Approve this conservative evidence model before implementation. In particular, approve the choice to label unmatched clusters as **unresolved** rather than incorrectly treating a cluster boundary as a biological lineage boundary.

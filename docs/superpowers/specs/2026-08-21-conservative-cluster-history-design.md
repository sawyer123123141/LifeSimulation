# Ancestry-Aware Genetic-Cluster History Design

**Status:** revised after architecture review; proposed for review
**Date:** 2026-08-21

## Review correction

The first version used only identical live creature IDs to connect adjacent genetic clusters. That was too weak for P5: a genuine lineage could appear to vanish as parents died and genetically similar offspring replaced them. It also accepted a snapshot and cluster result separately even though the API could not prove that they belonged together.

This revision makes ancestry/event history part of continuity evidence and introduces one cluster-observation aggregate that owns its snapshot, threshold, and assignments. It also defines the narrow conditions under which the analysis may report a lineage extinction rather than an unresolved disappearance.

## Purpose

P5 can already capture deterministic population genomes, build genetic clusters at an explicit distance threshold, and record ancestry outside active creature storage. This feature compares successive cluster observations and records evidence for continuity, split, merge, and lineage extinction without making any derived label authoritative.

A cluster is an analytical grouping at one threshold and one time. A cluster track is not a biological species and is never read by simulation behavior.

## Constraints

- The feature lives outside `SimulationWorld` and never changes simulation decisions, configuration, RNG calls, ordering, or hashes.
- The caller supplies every interpretation value explicitly. There is no universal species threshold or hidden confidence cutoff.
- Genetic clustering and ancestry are both evidence. Genetic similarity alone cannot prove historical continuity, while shared ancestry alone cannot prove that descendants remain one genetic group.
- Sampled and full-population observations are never mixed in one history stream.
- An overflowed or incomplete event stream cannot support a confirmed ancestry-dependent event.
- Pending state is bounded by the policy's confirmation window. The host owns durable event storage.

## Terms

| Term | Meaning |
| --- | --- |
| Cluster observation | One immutable snapshot plus the explicit threshold and cluster assignments derived from that exact snapshot. |
| Ancestry support | A current member is the same creature as, or a recorded descendant of, a member of an earlier cluster. |
| Strong relation | An ancestry-supported relation between adjacent cluster observations that meets the caller's explicit count and fraction policy. |
| Cluster track | A read-only analytical identity carried through strong relations. It is not an entity in the simulation. |
| Candidate split / merge | A qualifying graph pattern observed once. |
| Confirmed split / merge | A candidate whose resulting track or tracks persist for the requested later observations. |
| Unresolved arrival / disappearance | A cluster boundary that lacks enough evidence for a stronger historical claim. |
| Confirmed lineage extinction | A full-population, complete-history result showing that an observed track has no living recorded descendants. It is still a derived analytical claim, not an authoritative species flag. |

## Observation contract

`GeneticClusterObservation.Create(snapshot, threshold)` constructs `GeneticClusters` internally and retains the immutable inputs needed to audit every assignment. History never accepts an independently constructed `GeneticClusters`, so callers cannot accidentally pair clusters with the wrong snapshot or threshold.

`PopulationGenomeSnapshot` gains provenance metadata:

- `IsSampled` — whether the host requested sampling;
- `SourcePopulationCount` — live population count at capture; and
- `SampleLimit` — zero for a full capture, otherwise the requested maximum count.

`IsSampled` remains true when the population happens to fit under `SampleLimit`. This records the collection policy, not merely whether any creature was omitted on that tick.

Every observation exposes:

- tick and explicit genetic threshold;
- sampling provenance;
- cluster count;
- member IDs and genomes; and
- a stable observation-local cluster ordinal for every member.

The ordinal is not persisted as species identity. A track ID is allocated only by the external history analysis.

One history segment accepts observations only in strictly increasing tick order with the same threshold, sampling policy, sample limit, and `ClusterHistoryPolicy`. A change starts a new explicitly discontinuous segment; it is never interpreted as evolution.

## Event-history completeness

`AncestryHistory` already records founders, births, deaths, parents, and children. Cluster history additionally needs an explicit completeness watermark supplied when the host drains a tick's events.

The watermark advances only when:

- founders were recorded before the first drain;
- all events through that tick were recorded in order; and
- `SimulationEventBuffer.Overflowed` was false for every drained batch.

If overflow occurs, completeness becomes false from that tick onward for the current history segment. Later data remains inspectable, but ancestry-dependent confirmations are disabled because a missing birth can silently sever a lineage. The host may start a new explicitly discontinuous segment by recording the entire current population as new analytical roots; no track crosses that discontinuity.

Before accepting an observation, history requires ancestry records for all observed creature IDs and a completeness watermark at least as recent as the observation tick. Otherwise it records an incomplete-evidence result and does not classify a split, merge, or extinction.

## Ancestry relation

For every previous cluster `P` and current cluster `C`, inspect each member of `C`. A member supports `P` when it either:

1. has the same creature ID as a member of `P`; or
2. reaches a member of `P` by following recorded parent links backward within `policy.MaximumAncestorGenerations`.

Parent traversal is deterministic and cycle-checked. A cycle or missing ancestry record makes the affected relationship unresolved instead of being treated as negative evidence.

Record these raw values for every relation:

- current members supported by `P`;
- fraction of `C` supported by `P`;
- members of `P` with at least one descendant in `C`;
- fraction of `P` represented by at least one member of `C`;
- direct surviving-ID support;
- descendant-only support;
- minimum and maximum supporting ancestor depth; and
- whether ancestry coverage was complete.

A relation is strong when:

```text
supportedCurrentMemberCount >= policy.MinimumSupportedCurrentMembers
supportedCurrentMemberCount / currentClusterMemberCount
    >= policy.MinimumCurrentSupportFraction
supportingPreviousMemberCount >= policy.MinimumSupportingPreviousMembers
supportingPreviousMemberCount / previousClusterMemberCount
    >= policy.MinimumPreviousSupportFraction
ancestryCoverageIsComplete
```

Both sides must pass because a handful of prolific ancestors should not make an entire prior cluster appear to split or merge. This conservative rule can leave a real but highly asymmetric budding lineage unresolved; the evidence record makes that failure visible. The generation limit prevents ancient shared ancestry from eventually connecting every cluster to every other cluster.

## Policy

The immutable `ClusterHistoryPolicy` validates caller-supplied values:

| Field | Meaning |
| --- | --- |
| `MinimumSupportedCurrentMembers` | Positive count required for a strong relation. |
| `MinimumCurrentSupportFraction` | Fraction in `(0, 1]` required for a strong relation. |
| `MinimumSupportingPreviousMembers` | Positive number of distinct prior members that must contribute descendants. |
| `MinimumPreviousSupportFraction` | Fraction in `(0, 1]` of the prior cluster that must be represented. |
| `MaximumAncestorGenerations` | Positive ancestry depth searched between adjacent observations. |
| `RequiredSuccessorObservations` | Positive number of later compatible observations needed to confirm split or merge persistence. |
| `RequiredAbsentObservations` | Positive number of later full observations needed before testing lineage extinction. |

Tests choose explicit values. Later experiments may calibrate scenario-specific policies, but no calibration becomes a biological constant or a simulation default.

## Relationship classification

Build the bipartite graph of strong ancestry relations between adjacent observations.

| Pattern | Classification | Result |
| --- | --- | --- |
| One previous cluster to one current cluster | Continuity | Current cluster continues the track. |
| One previous cluster to two or more current clusters, with no child linked strongly to another predecessor | Candidate split | Parent and child tracks are retained pending confirmation. |
| Two or more previous clusters to one current cluster, with no parent linked strongly to another successor | Candidate merge | Parent and successor tracks are retained pending confirmation. |
| No strong predecessor for a current cluster | Unresolved arrival | Begin an unlinked track without claiming colonisation or speciation. |
| No strong successor for a previous cluster | Pending disappearance | Delay interpretation until descendant and persistence evidence is available. |
| Any many-to-many connected component | Ambiguous reorganisation | Preserve its evidence graph and make no split/merge claim. |

This exclusive classification prevents one many-to-many rearrangement from being reported simultaneously as several confident splits and merges.

## Persistence

When a split candidate appears, create pending child tracks. On each later compatible observation, every child must have exactly one strong successor for the candidate to advance. A new split, merge, loss of ancestry completeness, or ambiguous relation ends confirmation and records the candidate as unresolved.

A merge candidate is symmetric: its successor track must have exactly one strong successor on every required later observation.

When the configured number of successful successor observations is reached, emit one immutable confirmed event and discard its pending state. Candidate and failed-confirmation records remain in durable host output; confirmation never erases contrary evidence.

## Extinction rule

A pending disappearance may become `ConfirmedLineageExtinction` only when all of these hold:

1. every observation involved is full-population, not sampled;
2. event history is complete through the newest observation and has never overflowed since the track was observed;
3. the track has had no strong successor for `RequiredAbsentObservations` consecutive observations;
4. every member ID in the track's last observation has a complete ancestry record; and
5. no creature in the newest full-population observation is the same creature as, or a recorded descendant of, any member in that last cluster.

If a living descendant exists but falls below the strong-relation threshold, the result is an unresolved disappearance, not extinction. Sampled histories can never confirm extinction because absence from a sample is not absence from the population.

The event name deliberately says **lineage extinction**. It means the recorded descendant branch of this observed cluster has ended under complete evidence; it does not assert that an objective, threshold-independent species existed.

## Visible confidence

The system does not invent a probability such as “82% species confidence.” Every event instead exposes:

- `Candidate`, `Confirmed`, or `Unresolved` status;
- full versus sampled provenance;
- event-history completeness;
- the explicit genetic and ancestry thresholds;
- raw supporting counts and fractions;
- supporting ancestor-depth range;
- ticks observed and confirmation length; and
- the reason an unresolved candidate failed.

This is auditable confidence: viewers can see the evidence and uncertainty rather than a false-precision score.

## Proposed analysis boundaries

The names describe responsibilities, not implementation details:

```csharp
public readonly struct ClusterHistoryPolicy { ... }
public sealed class GeneticClusterObservation { ... }
public sealed class GeneticClusterHistory { ... }
public readonly struct ClusterHistoryEvent { ... }
```

- `GeneticClusterObservation` owns one snapshot/threshold/clustering result.
- `AncestryHistory` owns founder, birth, death, and parent/child evidence plus completeness state.
- `GeneticClusterHistory` owns only the previous observation, bounded pending confirmations, track allocation, and emitted analytical records.
- `ClusterHistoryEvent` is immutable host-facing evidence. It never exposes mutable simulation state.

No type in `SimulationWorld`, behavior, biology, or resource systems reads any of these results.

## Storage and determinism

- Observation construction and history analysis are host-triggered, not per-tick simulation work.
- Track allocation follows observation order, then observation-local cluster ordinal; dictionary iteration never decides output ordering.
- Parent traversal follows first parent then second parent and uses explicit buffers, so replaying the same observations and events produces identical records.
- In-memory history retains only the prior observation and candidates inside the maximum confirmation window.
- The host drains immutable records into the P5 long-history storage path; generated histories are not committed to Git.

## Test strategy

Focused synthetic fixtures cover:

1. unique continuity while some original creatures remain alive;
2. continuity after complete generational replacement through recorded parent links;
3. a split candidate that confirms only after both child tracks persist;
4. a merge candidate supported by descendants from both parent tracks;
5. a many-to-many reorganisation that remains ambiguous;
6. a full-population, complete-history lineage extinction;
7. sampled disappearance that can never become extinction;
8. event-buffer overflow or missing ancestry that blocks confirmation;
9. rejection of changing threshold, provenance, policy, or non-increasing ticks;
10. observation construction that makes snapshot/cluster mismatch unrepresentable; and
11. analysis isolation: an observed world retains the same deterministic state hash as an unobserved world.

The fixtures use known parent graphs and cluster assignments. They do not run a full ecology simulation merely to test graph classification.

## Non-goals

- Naming species or assigning permanent biological taxonomy.
- Feeding cluster or track IDs into mating, behavior, diet, or survival.
- Inferring ecological causality, migration, predation, or speciation from cluster history alone.
- Selecting universal thresholds or a probability-like confidence score.
- Persisting an unbounded history in active simulation memory.
- Adding presentation or changing any default scenario in this unit.

## Decision requested

Approve the ancestry-aware evidence model and its strict extinction rule before an implementation plan is written. The central decision is that incomplete or sampled evidence remains unresolved even when a cluster appears to disappear.

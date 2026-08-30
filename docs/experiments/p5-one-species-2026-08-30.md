# P5 has nothing to report, and the analysis is not why

**Date:** 2026-08-30
**Status:** finding. **No code changed.** `tools/HistoryProbe` is committed.
**Question:** the P5 history panel occupies 520 x 340 pixels of the screen at all times. What does it
actually say in the world the user watches?
**Method:** the shipped `Y` — the four-way split layout at `Y`'s own configuration — 12,000 ticks,
seeds 42-47, with `P5HistoryPanelSession.Advance` called every tick exactly as the presenter calls it.

## What the panel says: one species, forever

| seed | population | observations | events | notable | clusters (max/final) | ancestry complete |
|---|---|---|---|---|---|---|
| 42 | 96 | 40 | 41 | 2 | **1/1** | yes |
| 43 | 96 | 40 | 39 | 0 | **1/1** | yes |
| 44 | 96 | 40 | 39 | 0 | **1/1** | yes |
| 45 | 96 | 40 | 39 | 0 | **1/1** | yes |
| 46 | 96 | 40 | 41 | 2 | **1/1** | yes |
| 47 | 93 | 40 | 43 | 4 | **1/1** | yes |

**The cluster count is one, in every observation of every seed.** Of 242 events, **234 are
`Continuity`** — the routine "nothing happened" record the panel deliberately hides. The eight
remaining are three `PendingDisappearance`, three `UnresolvedDisappearance`, one `CandidateMerge` and
one `ConfirmedMerge`: bookkeeping about tracks, not biology.

**Zero splits. In any seed. Ever.** There has never been a second species to split into.

## The analysis is working. The population has no structure to find

The obvious suspicion is that the panel's threshold of 0.25 is simply too high. Clustering the final
population at five thresholds says otherwise:

| seed | mean pairwise distance | max pairwise | t=0.05 | t=0.10 | t=0.15 | **t=0.25** | t=0.40 |
|---|---|---|---|---|---|---|---|
| 42 | 0.171 | 0.323 | 89 | 23 | 2 | **1** | 1 |
| 43 | 0.133 | 0.258 | 83 | 6 | 2 | **1** | 1 |
| 44 | 0.113 | 0.207 | 72 | 2 | 1 | **1** | 1 |
| 45 | 0.196 | 0.346 | 93 | 54 | 2 | **1** | 1 |
| 46 | 0.189 | 0.331 | 96 | 27 | 3 | **1** | 1 |
| 47 | 0.178 | 0.333 | 86 | 16 | 2 | **1** | 1 |

Read the row for what it is. At **0.05** the population shatters into 72-96 clusters against a
population of 93-96 — nearly one cluster per animal. At **0.10** it is 2 to 54, swinging by a factor
of 27 across seeds of the same world. At **0.15** it is 1 to 3. At **0.25** it is always 1.

**There is no threshold at which this population separates stably.** The genotypes are a continuum,
not clumps: mean pairwise distance 0.11-0.20 with a maximum of 0.21-0.35, and no gap anywhere in
between for a boundary to sit in. A cluster count that moves from 96 to 1 as the knob turns, with no
plateau, is the signature of a single interbreeding population.

So the P5 machinery is not broken and its threshold is not miscalibrated. **It is correctly reporting
that there is one species**, and it would be wrong to report anything else.

## Why there is one species

Nothing in this world separates gene flow. One 50-unit arena, a population capped at 96, every animal
within reach of every other, and mate selection that scores partners without regard to how closely
related their genotypes are. Speciation needs a barrier — geographic, temporal or behavioural — and
the simulation has none.

**This is a missing piece of biology, not a missing piece of analysis.** P5's exit gate asks that
"species clusters provide useful separation with visible uncertainty". The separation cannot exist
until something splits the population, and the uncertainty measured above is total.

## What this means for the roadmap

P5's storage and analysis are built — ancestry, genetic distance, clustering, split/merge relations,
sensitivity, event policy, panel session, nine test files — and every one of those tests feeds it
**synthetic** ancestry, which is the right way to test clustering and says nothing about whether a
real population ever produces a split. That gap is what this probe closes.

Two routes to a second species, and they are not equally good:

1. **Isolation — the emergent route.** Separate the population geographically so gene flow is
   genuinely limited, and let divergence happen or not on its own. That is P6's world partitioning
   and regions, which the dependency chain already places after P5. The tiled-habitat transform
   (`SimulationScenario.Tiled`) already exists and was measured viable with founders scaled.
2. **Assortative mating — the forced route.** Make creatures prefer partners with similar genomes.
   It would produce clusters quickly and they would be an artefact of the rule that made them.

**Route 1 is the one that fits how this project has decided such things before** — the standing
direction is that behaviour must emerge rather than be imposed, and the soft home-range affinity and
safety-gated rendezvous closures are both cases of a mechanism being declined for forcing an effect
the ecology did not produce. Route 2 should not be built without an explicit decision that a
designed-in preference is wanted.

**Neither is small, and neither is started.** The finding here is only that the panel is honest and
the world is panmictic.

## What not to do next

- Do not lower the panel's threshold to make clusters appear. 0.15 gives 1 to 3 clusters across seeds
  of the same world and 0.05 gives one cluster per animal; both are threshold noise reported as
  species.
- Do not conclude the P5 analysis needs work from the fact that the panel looks empty. It is empty
  because the answer is one.
- Do not read the nine passing P5 test files as evidence that speciation works. They test the
  clustering of synthetic ancestry, which is a different claim.

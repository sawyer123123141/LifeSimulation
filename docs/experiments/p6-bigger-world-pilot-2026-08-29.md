# A bigger world — pilot

**Date:** 2026-08-29
**Status:** pilot, 4 seeds an arm. Enough to direct the next step, not enough to publish a number.
**Question:** does widening the arena give the herd room to spread out, and does the ecology survive it?

## Why the question exists

A human watching Play mode reported creatures clumping into a pile. That turned out not to be a
movement bug: the `Y` habitat has **six food sites of `InteractionRadius` 1.5 for 96 creatures**, so
sixteen animals share a disc three units across while a creature model is about one unit wide. They
overlap by construction. Spacing is a question about how much room the world has.

The arena was the literal `new ArenaBounds(-25f, 25f, -25f, 25f)` in `SimulationWorld`'s constructor.
It is now `SimulationConfig.ArenaHalfWidth`, hashed, defaulting to 25 — the question could not be
asked while the answer was a constant.

## Method

`Y`'s configuration, 12,000 ticks, seeds 42-45, matched across arms. The habitat is tiled rather than
stretched — `SimulationScenario.Tiled` repeats the whole layout at 50-unit spacing — so the spatial
density of resources and the travel distance between neighbouring sites both stay what the ecology was
calibrated against. **Widening the arena without placing resources into the new space measures
starvation, not space.**

Metrics: end population, mean nearest-neighbour distance, share of creatures with a neighbour inside
one unit, mean energy fraction, extinctions.

## Results

| arm | arena | founders | cap | extinct | surviving populations | mean nearest |
|---|---|---|---|---|---|---|
| today-50u | 50 u | 4 | 96 | **0 of 4** | 95, 96, 96, 96 | 0.726 |
| 200u-same-cap | 200 u | 4 | 96 | 2 of 4 | 96, 96 | 1.115 / 0.595 |
| 200u-scaled-cap | 200 u | 4 | 384 | **3 of 4**, survivor at population **1** | — | — |
| 200u-16founders | 200 u | 16 | 96 | **1 of 4** | 95, 96, 93 | 0.945 / 0.876 / 0.695 |
| 200u-16f-scaledcap | 200 u | 16 | 384 | 2 of 4, plus one world at population 1 | 100 | 0.881 |

## What it says

**1. The failure is establishment, not carrying capacity.** Every big-world failure is an early
extinction; the survivors reach a full 96, indistinguishable from baseline. Four founders in a
200-unit world with habitats 50 units apart do not reliably find the next site or each other.
Scaling founders with area takes extinctions from 2 of 4 to 1 of 4.

**2. Do not scale the population cap.** At 384 the population stops being cap-limited and becomes
resource-limited, and it collapses. This is not new: Phase I recorded that **the cap is the
stabiliser, not the ceiling**, and that survival at high caps depends on `gradedFertilityEnabled`,
which is **off for `Y` by the user's explicit choice**. Raising the cap removes the stabiliser from a
scenario with no replacement for it. Any future big-world work either keeps the cap or turns graded
fertility on and re-measures — and the second is blocked by a standing decision.

**3. Space buys less spacing than hoped.** The best big world reached 0.945 mean nearest-neighbour
against 0.726 at baseline, and the share with a neighbour inside one unit moved 74.9% to 66-77%.
A surviving population still fills to the same 96 and still eats at 1.5-unit sites, so more room puts
more empty grass *between* the plates without changing how many animals share one. **If the goal is
that the herd stops reading as a pile, the lever is feeding-site geometry or the cap, not arena
width.**

## What was wrong on the way here, and cost real time

**A hand-built copy of the habitat killed every world, including the control.** The first probe listed
the six active food and water sites and tiled those. The layout also carries **twenty dormant sites**
that plant dispersal re-establishes into — with `plantMortalityEnabled` on, plants die and never come
back without them — and a **founder placement**. Both were invisible to a reading that looked for
"where the food is". `SimulationScenario.Tiled` exists so this cannot happen again: it copies every
definition, whatever it is.

**Only the control arm distinguished "finding" from "bug".** The broken probe's result read exactly
like an ecology finding — twelve worlds, all extinct. The baseline arm's known answer was the only
thing that said otherwise.

**`Tiled` then carried founder placement through unchanged, which was also wrong.** The placement is a
point *on* a resource site, and tiling moves every site: founders spawned at (-12,-8) while their site
had moved to (-37,-33). Four animals in empty ground. That artefact produced 2 of 4 and 3 of 4
extinctions that looked like an ecology result. Fixed, with tests.

## Next, if this is taken further

- Confirm the establishment reading at more seeds, and find where founder count stops helping.
- Measure whether the extinctions are mate-finding or resource-finding. `SeekMate` targets a live
  creature position, so a dispersed founder population may simply never meet.
- Leave the cap alone unless graded fertility is being reopened.

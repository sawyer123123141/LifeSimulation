# P3 digestion: no strategy is favoured, in any cell measured

**Date:** 2026-08-30
**Status:** the P3 required experiment, run. **Negative.** No production code changed.
**Harness:** `tools/CreatureSweep`, which already reports `diet_specialization` drift with a
`neutral_marker` control column.

## What the docs asked for

`docs/superpowers/specs/2026-08-12-product-architecture.md`, P3 — *"Rich physiology and niche
formation"*:

> **Scientific question:** Can additional physiological trade-offs create multiple persistent
> survival strategies without manually assigning species roles?
>
> **Exit gate:** at least two distinct trait strategies persist because they exploit different
> conditions, not because of hardcoded category protection

Its candidate traits name **digestion specialization**, and its required experiments name
*"abundant versus scarce food with digestion specialists and generalists."* The program plan's P3
Digestion slice is *"plant/meat specialization, processing rate, yield, toxin/defense sensitivity,
and **generalist cost**."*

`docs/experiments/p3-physiology-checkpoint-2026-08-13.md` recorded this as outstanding:

> - Demonstrate two persistent strategies exploiting different conditions, rather than a single
>   higher-fitness condition.
>
> **P4 remains blocked until this evidence is recorded.**

No later document records it. This is that experiment.

## Result: diet specialization is unselected everywhere it was measured

24 seeds an arm, 12,000 ticks, predation on, gate 0.45, brake 1.5.

| cell | `diet_specialization` | t | control t | `defense` t | survived |
|---|---|---|---|---|---|
| regen 2.0, cap 48 | +0.0284 | **+0.59** | -0.44 | +7.10 | 18/24 |
| regen 1.0, cap 48 | -0.0102 | **-0.21** | -1.27 | +6.52 | 18/24 |
| regen 0.5, cap 48 | +0.0256 | **+0.52** | -0.11 | +3.62 | 19/24 |
| **regen 2.0, cap 500, proximity pairing** | +0.0481 | **+1.56** | **+1.62** | **+12.71** | **24/24** |

The last row is the cell `emergent-behaviour-fleeing-is-selected-against-2026-08-29.md` calls *"the
highest-combat cell available"*. **In it, the neutral control moves as much as diet does** — t 1.62
against 1.56. That is the honest reading: nothing.

**The instrument is not blind.** In the same runs `defense` reaches **t +12.71**, `attack` -3.60 and
`maneuverability` +3.17, reproducing the recorded +4.97 to +10.97 range for defense. Predation
selection is enormous in these cells. Diet is not part of it.

Sign is also non-monotone across the resource axis — +0.59, -0.21, +0.52 — which is what noise looks
like. **Scarcer plants did not make meat-eating pay.** That is the required experiment's own
comparison, and it returns nothing.

## The instrument gap was closed, and it corrects the section below

The drift table reports a **mean**, and a population half at 0.2 and half at 0.8 has a mean of 0.5
and no drift — the gate's own success condition would have been reported as nothing happening. So
`tools/CreatureSweep --diet` was added to print the distribution before any mechanism was changed on
the strength of the negative. Cap 500, proximity pairing, predation, gate 0.45:

| | brake 1.5 (16.2% starvation) | brake 1.0 (33.6% starvation) |
|---|---|---|
| creatures | 5,359 over 23 runs | 4,761 over 22 runs |
| mean / sd | 0.522 / **0.274** | 0.511 / **0.303** |
| middle two bins | 26.3% | **18.9%** |
| outer four bins | 33.1% | **42.4%** |
| hunt-capable (>= 0.58) | 41.5% | 39.9% |
| **per-run hunter share** | mean 45.3%, **min 0%, max 100%** | mean 57.2%, **min 1.5%, max 100%** |

**This is drift to fixation, not two strategies.** A uniform distribution on 0..1 has sd 0.289; the
observed 0.274 and 0.303 straddle it, and the U-shape — thin middle, mass piled at both clamped
boundaries — is what a neutral trait does under a random walk with clamping. The decisive number is
the **per-run** range: individual worlds finish anywhere from 0% to 100% hunt-capable. Worlds are
drifting to their own value independently, not maintaining two strategies within themselves.

**And the hunt-capable are not a phenotype.** Against everyone else they differ by attack +0.016,
aggression +0.003, movement speed -0.014, body size -0.006, vision +0.010. Only defense differs
appreciably (+0.088), and defense is under strong selection in these cells regardless. A carnivore
that is no faster, no more aggressive and no better armed than a grazer is a number, not a strategy.

**Even at 33.6% starvation nothing changes.** `p6-starvation-is-a-dial-2026-08-26.md` records
starvation running 49.6% to 0.0% of deaths on the brake alone; the brake-1.0 cell above is a
genuinely food-limited world, and diet is as neutral there as anywhere.

### Correction to this document

An earlier version of the section below said diet is **"pure cost"** below the hunting threshold,
reasoning from the linear yield curves in `GenomePhenotype`. **The distribution refutes that.** If a
30% plant-yield penalty were a realised cost, mass would pile at diet 0; instead 12.7% and 16.6% of
creatures sit in the top bin at 0.9-1.0 and the population is spread across the whole range. The
trade-off exists in the source and **does not reach fitness**. The claim was derived from code and
contradicted by measurement, which is the error this project keeps paying for.

## Why the trade-off does not reach fitness, from the death mix rather than from a story

- `PredationSystem.MinimumHuntingDiet = 0.58f`. **Below 0.58 a creature cannot hunt at all.**
- `GenomePhenotype`: `PlantFoodYieldMultiplier = 1 - 0.3 * diet` and
  `MeatYieldMultiplier = 0.5 + diet`, plus maintenance `+0.04 * diet`. Both **linear**.
- So from diet 0 to 0.58 the gene is **pure cost** — up to 17% of plant yield plus upkeep — and buys
  nothing, because the hunting threshold has not been crossed.
- Past the threshold the payoff is meat, and meat is small: measured at **1.1% of deaths** (14 of
  1,264) in the cap-48 predation cell, at 8.4 attack hits per run. The best figure on record
  anywhere is **8.4% of deaths** against 44.8% starvation, in the cap-500 cell
  (`emergent-behaviour-fleeing-is-selected-against-2026-08-29.md`).

**This is the shape the project has already closed once.** `MetabolicPace` was recorded as a pure
cost where *"the costs are continuous and no available benefit"* could rescue it. Diet has the same
profile with a threshold on top.

**The plan's "generalist cost" is not implemented.** Both yield curves are linear, so a generalist at
diet 0.5 is exactly the average of the two ends and pays no penalty for being in the middle. That is
a scoped item that was never built — but note it would not on its own produce two strategies: with
meat worth almost nothing, penalising the middle pushes the population to the herbivore end, not to
two ends.

## What this establishes and what it does not

**Establishes:** the P3 exit gate is not met, and the required digestion experiment now has a
recorded negative result instead of a gap. `diet_specialization` is **effectively neutral in every
configuration measured** — four cells by drift t-statistic, two cells by full distribution, including
a world where a third of all deaths are starvation. It drifts to fixation independently in each
world.

**Does not establish:** that raising the meat payoff would produce two strategies. That is a
hypothesis and it needs its own arm. Nothing here measures a world where predation is a material
energy flow, because no such configuration exists yet — 8.4% of deaths is the ceiling on record.

**Closed:** the distribution gap. `tools/CreatureSweep --diet <seeds> <cap>` prints the histogram,
the share above the hunting threshold, the per-run range, and whether the hunt-capable differ in any
other trait. Any future attempt at this gate should be judged on that output, not on a mean.

**Still unmeasured:** *why* a 30% plant-yield penalty does not reach fitness. Candidates exist — the
penalty is on yield per unit eaten while patches sit at capacity, so a creature may simply eat more —
but nothing here measures realised energy intake against the diet gene, and that is the next thing to
instrument if this gate is picked up again.

## What not to do next

- Do not implement "generalist cost" on its own and expect species. Measured above: the middle is not
  where the population is stuck, the carnivore end is where the food is missing.
- Do not read `defense` selecting at t +12.7 as evidence that the predator-prey loop is ecologically
  material. It selects on **surviving attacks**, at 1.1-8.4% of deaths; that is pressure on a
  defensive trait, not a food supply.
- Do not re-run this comparison at cap 48. The cap-500 proximity-pairing cell is the strongest
  available and it is the one that gives the control column something to say.

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

## Why, from the code and the death mix rather than from a story

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
recorded negative result instead of a gap. Two strategies cannot persist while one of them has
almost nothing to eat.

**Does not establish:** that raising the meat payoff would produce two strategies. That is a
hypothesis and it needs its own arm. Nothing here measures a world where predation is a material
energy flow, because no such configuration exists yet — 8.4% of deaths is the ceiling on record.

**Also unmeasured:** the *distribution* of the diet gene. `SimulationStatistics` exposes a mean, and
a mean cannot show two modes. If a later run does produce two strategies, the current instrument
would report their average and show nothing. That gap should be closed before, not after.

## What not to do next

- Do not implement "generalist cost" on its own and expect species. Measured above: the middle is not
  where the population is stuck, the carnivore end is where the food is missing.
- Do not read `defense` selecting at t +12.7 as evidence that the predator-prey loop is ecologically
  material. It selects on **surviving attacks**, at 1.1-8.4% of deaths; that is pressure on a
  defensive trait, not a food supply.
- Do not re-run this comparison at cap 48. The cap-500 proximity-pairing cell is the strongest
  available and it is the one that gives the control column something to say.

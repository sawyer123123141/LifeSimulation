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

## Realised intake: the trade-off works perfectly and still does not select

`tools/CreatureSweep --intake` follows every creature's energy tick by tick and attributes each
positive change to what it was eating at the time. An outside observer in the manner of
`CreatureActionHistory` — it reads the world and never writes to it. 8 seeds, cap 500, brake 1.0,
regen 2.0, predation, proximity pairing; 5,639 creatures that lived more than 200 ticks.

| diet | creatures | plant energy /1k ticks | meat energy /1k ticks | **total /1k** | **% of ticks eating plants** | plant energy per eating tick |
|---|---|---|---|---|---|---|
| 0.0-0.2 | 970 | 87.70 | 1.44 | **89.14** | 12.47 | 0.687 |
| 0.2-0.4 | 1353 | 79.78 | 4.54 | **84.32** | 12.23 | 0.632 |
| 0.4-0.6 | 1036 | 74.07 | 7.75 | **81.82** | 12.16 | 0.578 |
| 0.6-0.8 | 975 | 63.20 | 15.47 | **78.67** | 12.40 | 0.479 |
| 0.8-1.0 | 1305 | 63.00 | 22.44 | **85.45** | 12.38 | 0.483 |

**The trade-off is realised exactly as written.** Plant energy per feeding tick falls from 0.687 to
0.483 — a 30% drop, which is precisely `PlantFoodYieldMultiplier = 1 - 0.3 * diet`. Meat intake rises
**15.6-fold**, 1.44 to 22.44. The mechanism is not broken and it is not inert.

**The "they just eat more" hypothesis is refuted.** It was the leading explanation and it is wrong:
time spent eating is **flat at 12.16% to 12.47%** across the entire gene range. Creatures with poor
plant yield do not compensate by feeding longer.

**And a valley already exists.** Total intake is 89.1 at the herbivore end, sags to **78.7** in the
0.6-0.8 band, and recovers to 85.4 at the carnivore end — the generalist is the worst place to be, by
about 12%. That is the shape "guided emergence" would have set out to build, and it is already there.

**Meat is 9.69% of all energy ingested**, not the negligible flow the death mix suggested. A
carnivore takes 16 times the meat energy of a herbivore.

So the blocker is none of the things previously proposed. The trade-off exists, is correctly shaped,
has a valley in the middle, is fully realised in intake, and is **still not selected**.

## Intake does become fitness — it is simply swamped

The same probe now counts births per parent, and the hypothesis in the previous paragraph — that a
capped energy pool discards the surplus, so an intake advantage is not a fitness advantage — **is
refuted**:

| | correlation |
|---|---|
| lifetime intake vs offspring | **+0.880** |
| intake **rate** vs offspring | **+0.807** |
| ticks alive vs offspring | +0.786 |
| lifetime intake vs ticks alive | **+0.932** |

Energy very clearly becomes descendants. 71.9% of the 5,639 creatures left any, mean 1.99 offspring.

**But offspring is flat across the diet gene**, while intake is not:

| diet | intake /1k ticks | lifetime intake | **offspring** | ticks alive |
|---|---|---|---|---|
| 0.0-0.2 | 89.14 | 258.4 | **1.933** | 2446 |
| 0.2-0.4 | 84.32 | 245.9 | **2.014** | 2426 |
| 0.4-0.6 | 81.82 | 245.6 | **2.004** | 2533 |
| 0.6-0.8 | 78.67 | 229.3 | **2.023** | 2418 |
| 0.8-1.0 | 85.45 | 241.0 | **1.980** | 2367 |

An 11% spread in intake rate produces a **4% spread in offspring, in the wrong direction** — the band
with the *lowest* intake has the *highest* offspring.

**The resolution is in the last correlation.** Lifetime intake and ticks alive correlate at **+0.932**
— intake is very nearly a restatement of how long a creature lived. Fitness here is set by lifespan,
and lifespan is set by ageing: the recorded death mix is **92.7% age**. An 11% difference in feeding
efficiency is noise against that.

**So the finding is not about digestion at all.** For any metabolic trade-off to select, energy has to
gate survival or reproduction more tightly than age does. In this ecology it does not, which is why
the gene is neutral, why `MetabolicPace` was recorded as an unrescuable pure cost, and why the intake
valley in the middle of the diet range buys nothing. The lever is the death mix, not the trade-off.

## What this establishes and what it does not

**Establishes:** the P3 exit gate is not met, and the required digestion experiment now has a
recorded negative result instead of a gap. `diet_specialization` is **effectively neutral in every
configuration measured** — four cells by drift t-statistic, two by full distribution, one by realised
energy intake, including a world where a third of all deaths are starvation. It drifts to fixation
independently in each world.

**And it establishes that the usual suspects are all innocent.** The trade-off is not missing, not
mis-shaped, not inert, and not starved of prey: it delivers a 30% plant-yield penalty, a 15.6-fold
meat gain, a 12% intake valley in the middle, and 9.7% of all ingested energy coming from meat. The
failure is downstream of intake.

**Does not establish:** that raising the meat payoff would produce two strategies. That is a
hypothesis and it needs its own arm. Nothing here measures a world where predation is a material
energy flow, because no such configuration exists yet — 8.4% of deaths is the ceiling on record.

**Closed:** the distribution gap. `tools/CreatureSweep --diet <seeds> <cap>` prints the histogram,
the share above the hunting threshold, the per-run range, and whether the hunt-capable differ in any
other trait. Any future attempt at this gate should be judged on that output, not on a mean.

**Measured since, and both candidates were wrong:** the penalty *is* fully realised in intake,
creatures do **not** eat more to compensate, and intake *does* become fitness (r +0.88 with
offspring). The chain is now complete and the answer is that an 11% intake difference is swamped by
lifespan variance in a world where 92.7% of deaths are old age.

## What not to do next

- Do not implement "generalist cost". **A 12% intake valley in the middle already exists**, energy
  converts to offspring at r +0.88, and the valley still buys nothing — because 11% of intake is
  noise against a death mix that is 92.7% age. Deepening the valley does not change that.
- Do not attack this through digestion at all. **The lever is the death mix.** A trade-off in energy
  can only select where energy gates survival; `p6-starvation-is-a-dial-2026-08-26.md` records
  starvation running 49.6% to 0.0% of deaths on one configuration value, and that is the axis this
  question actually lives on.
- Do not act on "the carnivore end has nothing to eat". That claim appeared in an earlier version of
  this document and the intake measurement retired it: meat is 9.7% of ingested energy and a
  carnivore takes 16 times what a herbivore does.
- Do not read `defense` selecting at t +12.7 as evidence that the predator-prey loop is ecologically
  material for *diet*. It selects on surviving attacks; meat is still under a tenth of energy intake.
- Do not re-run this comparison at cap 48. The cap-500 proximity-pairing cell is the strongest
  available and it is the one that gives the control column something to say.

## The drift is visible on screen

`CreatureArenaCapture.CaptureFaunaAcrossSeeds` renders the pressured cell at four seeds and logs the
role mix. `CreatureModelRules.SelectRole` draws a predator when diet is at least 0.55 **and**
aggression at least 0.5, so a gene that fixes per world should change what the world looks like:

| seed | population | predators | large herbivores | small herbivores | predator share |
|---|---|---|---|---|---|
| 42 | 126 | 0 | 28 | 98 | **0.0%** |
| 43 | 72 | 0 | 43 | 29 | **0.0%** |
| 44 | 14 | 9 | 4 | 1 | **64.3%** |
| 45 | 0 | - | - | - | extinct |

**Same configuration, four worlds, and one of them is a predator world.** Seed 42 is a crowd of 126
herbivores; seed 44 is nine predators and five herbivores, nearly collapsed. This is what neutral
drift to fixation looks like from the outside, and it is the strongest visible consequence of the
finding above.

**Caveats, because the pictures overstate it.** Changing the seed changes the terrain as well as the
ecology, so the renders differ for two reasons at once; the role counts are the evidence, not the
images. Four seeds is not a distribution. And seed 44 is a world of fourteen animals, so "64.3%
predators" is nine of them.


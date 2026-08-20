# Where Plant Lineage Success Is Actually Decided — 2026-08-20

The P4 exit gate is "give plant selection a route that is not growth rate", and the three
candidate routes that skip the `(1 - Biomass/Capacity)` gate are **establishment**, **mortality**
and **seed production**. This decomposes the variance in per-patch lifetime offspring across
them, so that choosing one is an evidence-backed decision rather than a guess.

Purely observational: a probe reads public state before and after every `Step` and changes
nothing. Transcribed config from `ResourceExperimentTests.ConsumerDefenseCalibrationModerate`
`SurvivesPlantMortalityAcrossSeeds` — `maximumPopulation: 48`, cognition, physiology,
`IntentUtilityV1`, plant cohorts, **site competition and mortality on** — seeds 42-71,
12,000 ticks. 6,279 patch lifetimes recorded.

Data: `p4-plant-route-patches-2026-08-20.csv`, `p4-plant-route-occupancy-2026-08-20.csv`.

## Headline

**Half the variance in plant reproductive success is a coin flip that no gene can influence,
and it happens in the first two seconds of a patch's life.**

| | share of Var(lifetime offspring) |
|---|---:|
| survived infancy vs taken over as a newborn | **51.9%** |
| everything that happens afterwards | 48.1% |
| — of which realised lifespan explains | **2.4%** |

## The measurements

**Sites are the binding constraint.** 24 food sites exist; mean occupancy is **21.86 (91.1%)**,
peaking at 24/24. A patch looking for somewhere to seed is choosing from about 2.1 free sites.

**Per patch lifetime**, over 5,589 completed lives:

| class | n | mean lifespan | mean offspring | mean biomass/capacity | produced nothing |
|---|---:|---:|---:|---:|---:|
| died of age | 3,668 | 95.8 s | 1.520 | 0.913 | 10.0% |
| **taken over as a newborn** | **1,921** | **4.0 s** | **0.002** | **0.075** | **99.9%** |
| still alive at 12,000 ticks | 690 | 49.2 s | 0.752 | 0.775 | — |

Mean offspring across the cohort is 0.998 — exact replacement, as equilibrium requires — with
Var = 1.0021. **34% of every patch ever born is destroyed inside a median two seconds**, at 7.5%
of capacity, having produced nothing. 68% of victims are gone within three seconds.

This is `PlantReproductionSystem.FindSite` doing what it was written to do: with
`plantSiteCompetitionEnabled`, a seed may land on an occupied site whose occupant is below
`VulnerabilityFraction = .25f`. Newborns start at `SeedInvestmentFraction` of the parent's
biomass — 1.5% to 9% of capacity — so **newborns are the only class the mechanism can ever
reach.** Site competition is not established patches displacing weaker ones. It is infanticide.

## Route by route

### Mortality — a real genetic channel that does not convert

`LifespanSeconds = BaseLifespanSeconds * (1.5 - .75 * Growth)`, a genuine 2x span, and it is the
one plant phenotype term that is not multiplied by the capacity gate. Among patches that die of
age, `r(Growth, lifespan) = **-0.510**` — the channel is live, correctly signed, and strong.

It buys almost nothing. Among those same patches, `r(offspring, lifespan) = +0.156`,
**R² = 0.024**. Reproduction is site-limited, not time-limited: 95.8 seconds of life against a
20-second cooldown allows roughly four seedings, and the mean patch achieves 1.52. Extra
lifespan is extra time spent waiting for a free site that is not there.

Realised lifespan among survivors has SD 9.7 s on a mean of 95.8 — a 10% spread — while the
pooled SD is 44.4 s, and that whole difference is the takeover class. **The mortality route has
no headroom, and the 53% of pooled variance that appears to trace to lifespan is an artifact of
pooling two-second infants with hundred-second adults.**

### Seed production — capped by a constant, not by a gene

Births are gated by a maturity check at 75% of capacity and a hard-coded
`ReproductionCooldownSeconds = 20f`. Patches sit at 91% of capacity, so maturity is almost
always satisfied and the cooldown binds. There is no genetic channel on seeding *rate* at all.
`SeedInvestment` sets only the offspring's starting biomass, and growth from that start is fast
enough that the head start is nearly spent: `r(SeedInvestment, taken over) = -0.004`.

Time eligible to seed is not an opportunity count but a *failure* count —
`r(offspring, eligible ticks) = **-0.68**` among survivors. A patch that seeds immediately
spends its time on cooldown; a patch that cannot find a site accumulates eligible ticks. Median
31 eligible seconds per birth against a 20-second cooldown: about 11 seconds of failed search
per success.

### Establishment — all of the variance, almost none of it heritable

The 51.9% share is where selection could act, and today it does not. Correlations between a
newborn's own genome and whether it gets taken over:

| gene | r(gene, taken over) |
|---|---:|
| Dispersal | +0.102 |
| Growth | -0.047 |
| every other gene | \|r\| < 0.01 |

`Growth` carries the right sign — growth rate **is** ungated during the vulnerable window,
because `(1 - Biomass/Capacity)` is ~0.99 at 1.5% of capacity — but standing SD on `Growth` is
0.078, worth about ±9% on `GrowthRateMultiplier`, against a two-second exposure lottery. The
race exists and no gene is running it.

### The positive control

`r(Dispersal, offspring) = **+0.161**` among patches that survive infancy, versus \|r\| < 0.035
for every other gene. That is the per-patch fitness signature of the trait already known to move
at t 14-17, so the metric detects selection where selection exists.

## What this says about the design decision

1. **Do not wire a trait into mortality.** The channel is already live and already strong on
   lifespan, and it converts at R² = 0.024 because the ecology is site-limited. A second
   lifespan gene would land on the same dead end that three growth-rate sessions found.
2. **Do not wire a trait into seeding rate** without first removing the constant cooldown; a
   gene competing with a hard-coded 20 seconds inherits the cooldown's ceiling.
3. **Wire into the takeover contest.** It is 51.9% of reproductive variance and it is currently
   resolved by a coin flip. Making it heritable is the only change on the table that converts an
   existing dominant variance channel into a selectable one, rather than adding a small new one.

A second, independent reason to act here: a non-heritable lottery that doubles reproductive
variance is a **drift amplifier**. `docs/experiments/p4-defense-selection-demonstrated-2026-08-18.md`
records that this population is drift-dominated and needs ~230 seeds for 80% power at plausible
effect sizes. Site competition is a measurable part of why.

## Predictions made before the run, and how they did

Stated in advance, three of five wrong:

| | prediction | measured | |
|---|---|---|---|
| P1 | mean site occupancy > 0.75 | 0.911 | held |
| P2 | median eligible ticks per birth <= 3 | 31 | **refuted** |
| P3 | > 60% of variance from establishment | 51.9% | **refuted as stated** |
| P4 | \|r(Growth, offspring)\| < 0.10 | 0.053 | held |
| P5 | r(Dispersal, offspring) > 0.25 | 0.161 | **refuted** |

P2 was wrong about the *sign of the metric*, not just its size: "eligible ticks" was assumed to
count opportunities and in fact counts failures. That is the more useful half of the error, and
it is why the establishment share could be read off at all.

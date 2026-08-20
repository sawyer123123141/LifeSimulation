# Fertility Binds the Growth Limit, So Tolerance Adaptation Is Mostly Inert — 2026-08-19

Mechanism for the null in
`p4-plant-trait-selection-nonreplication-2026-08-19.md`, and a direct answer to whether
the elevation field (open item 2) addresses it.

**It does not.** The channel elevation would widen is the one that already matters least.

## Two hypotheses killed, one confirmed

### Killed: "the field does not vary where plants actually live"

Plants only occupy the six active sites and their dispersal targets, so a field that varies
across the arena could still be nearly constant at plant-reachable positions. Measured over
seeds 42-71, mean per-seed range at plant-reachable positions against the whole arena:

| channel | plant-reachable range | whole-arena range | plant-reachable SD | arena SD |
|---|---:|---:|---:|---:|
| Moisture | 0.7245 | 0.8417 | 0.2430 | 0.2462 |
| Fertility | 0.5796 | 0.7594 | 0.1809 | 0.1784 |
| Temperature | 0.5110 | 0.6519 | 0.1450 | 0.1754 |

Plants see most of the field's range and essentially all of its spread. **The null is not a
sampling artifact.**

### Confirmed: fertility is the binding channel, and adaptation makes that worse

`PlantGrowthSystem.Step` takes `limit = Min(moistureAdaptation, Fertility, temperatureLimit)`.
Fertility is the one channel with **no genome modulation at all** — recorded by
`PlantLivenessTests.FertilityIsPinnedAtOneOnEveryProductionPath`. Measured at plant-reachable
positions over 120 seeds, with `WaterEfficiency` held at .5:

| tolerance | fertility binds | moisture binds | temperature binds | mean growth limit |
|---:|---:|---:|---:|---:|
| 0.35 | 81.5% | 10.8% | 7.7% | 0.4972 |
| 0.50 | 86.3% | 10.2% | 3.5% | 0.5043 |
| 0.65 | 89.7% | 9.6% | 0.7% | 0.5083 |

**The structure is self-defeating.** Each adaptation term lifts its own channel *toward 1*,
which pushes that channel out of contention for the minimum. So the more tolerance a plant
carries, the less often tolerance is what binds: fertility binding rises 81.5% to 89.7%, and
temperature binding collapses from 7.7% to 0.7%, as tolerance goes .35 to .65. The `Min` has
diminishing returns built into it.

## What tolerance is actually worth

`growth = GrowthRate * GrowthRateMultiplier * sproutBiomass * (1 - Biomass/Capacity) * limit * deltaTime`,
so the limit and the phenotype multiplier combine multiplicatively. Raising **both**
tolerances from .35 to .65:

- growth limit **0.4972 to 0.5083**, **+2.23%**
- `GrowthRateMultiplier` **0.7730 to 0.7130** (measured, not derived), **-7.76%**
- net on growth rate: **-5.70%**

So tolerance is net costly, by a factor of about 3.5 to 1 — but only mildly, and only on the
growth *rate*, which is multiplied by `(1 - Biomass/Capacity)`. A patch sitting at capacity
pays approximately none of it. That is the same "cost unrealized at capacity" shape recorded
for defense on 2026-08-18.

**A -5.7% intermittently-expressed rate penalty, over ~15 plant generations, against a drift
SD of 0.08 to 0.14, is not detectable.** The null is fully explained without invoking any
selection on tolerance at all.

## Consequence for the elevation field (open item 2)

The elevation design couples elevation to temperature through a lapse rate, and optionally to
moisture through a rain shadow. Both are channels that **already vary** at plant positions
(per-seed ranges 0.51 and 0.72) and that **already lose the minimum to fertility** 82-90% of
the time. Widening them further mostly moves them further out of contention.

Elevation remains worth building for terrain, biomes and P6 groundwork. **It should not be
justified as making the tolerance genes meaningful, and it will not produce a selection
response on them.** The handoff bundled those two motivations; they come apart here.

What would actually give the tolerances a fitness channel, in rough order of directness:

1. **Give fertility an adaptation term**, mirroring moisture. It is the binding channel 82-90%
   of the time and the only one no gene can answer. This is the smallest change with the
   largest reach, and it is the same gap `plant-gene-liveness-2026-08-18.md` identified for
   temperature before the adaptation term landed.
2. **Stop the limit being a hard `Min`.** A product, or a soft minimum, would let every channel
   contribute instead of handing all selection pressure to whichever channel happens to be
   lowest. This is a larger design change and would invalidate every plant baseline.
3. **Raise fertility's mean** so it stops dominating. Cheapest, and the least principled — it
   tunes a symptom.

None of these should land before someone decides which; option 2 in particular is a behavior
change that invalidates baselines, and this document is not a mandate to make it.

## Method note

The two hypotheses above were both mine, formed from reading the growth path, and the first
was wrong. Measuring the field spread cost one probe and about a minute of compute, and it is
the reason the second hypothesis is stated with numbers rather than as another plausible
mechanism story. This is the third hypothesis this session refuted by measurement, after the
"generic realized growth cost" reading in the non-replication document.

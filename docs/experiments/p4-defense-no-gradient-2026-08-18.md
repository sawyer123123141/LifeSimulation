# P4 — Why Plant Defense Has No Gradient: the Mechanism, Not the Pressure — 2026-08-18

Resolves the blocker recorded in `p4-lifespan-derived-2026-08-17.md`, which observed that plant
defense ends at 0.3049 against a founder value of 0.3 and correctly refused to run the coevolution
experiment until a selection gradient could be demonstrated.

That document's diagnosis was that grazing pressure is too low. The diagnosis is wrong, or at least
incomplete. The gradient is absent for a structural reason that no amount of pressure tuning
reaches.

## The mechanism, read directly from the consumption path

`SimulationWorld.Step`, in the resource allocation loop:

- `Plants.ConsumeAt(plantPatchIndex, allocatedAmount)` removes biomass from the patch.
  `allocatedAmount` derives from `phenotype.IngestionRate` and the allocation resolver. **It
  contains no defense term.**
- The nutrition the consumer receives is
  `allocatedAmount * … * (1f - (resource.PlantDefense * (1f - genome.FoodEfficiency)))`.

So plant `Defense` scales only what the *animal extracts*. The plant loses exactly the same tissue
whether it is defended or not. Meanwhile `PlantPhenotype` charges defense against growth
(`-.15f * genome.Defense`) and against nutrition (`-.25f * genome.Defense`).

A patch carrying defense therefore pays a cost and receives no individual benefit. Defense's only
route to benefit is diffuse and population-level — hungrier consumers, eventually fewer of them —
which is a public good shared with every undefended neighbour. Individual-level selection cannot
build that.

## Measurement

Four arms differing only in founder plant defense. `ConsumerDefenseCalibrationModerate` geometry
(six spread active sites), seeds 42–71, 12,000 ticks, `maximumPopulation: 48`, cognition, site
competition and plant mortality enabled. `grazeFrac` is cumulative biomass consumed over final
standing biomass.

| founder defense | end defense | delta | extinctions | min plant gen | consumed | standing | grazeFrac |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0.00 | 0.0682 | **+0.0682** | 0/30 | 12 | 3308.7 | 1575.8 | 2.100 |
| 0.30 | 0.3049 | **+0.0049** | 0/30 | 12 | 4083.8 | 1529.5 | 2.670 |
| 0.60 | 0.5995 | **-0.0005** | 1/30 | 13 | 5266.1 | 1470.2 | 3.582 |
| 0.90 | 0.8813 | **-0.0187** | 0/30 | 13 | 7398.6 | 1436.0 | 5.152 |

### There is no directional selection in either direction

The prediction going in was that defense would be pushed *down*, since it is a cost that buys
nothing. It is not. Every delta is consistent with drift plus a clamp-boundary artifact: upward
against the floor at 0.0, downward against the ceiling at 1.0, and flat in between.

The reason the cost does not bite either is saturation. Defense is charged against growth *rate*,
but patches sit at 75–100% of capacity for the entire run (measured in
`p4-coevolution-null-2026-08-17.md`). A patch that grows more slowly still ends at capacity, so the
growth penalty is never realized. Defense at these settings is **both benefit-free and cost-free**.

### The feedback runs backwards

`grazeFrac` climbs monotonically with defense, 2.10 → 5.15, and total biomass consumed rises 2.2x
from the 0.0 arm to the 0.9 arm. Defended tissue yields less energy per unit eaten, so consumers eat
*more* of it to meet the same need.

In this model defense causes a plant to be grazed harder, not less. That is the opposite sign from
real herbivory, and it is a second, independent reason the coevolution experiment could not have
produced a positive result.

## Consequence for the recommendations in the lifespan document

Of the three options it proposed:

1. **Report realized grazing pressure** — done, and it should have been done first. Added as
   `SimulationStatistics.RealizedGrazingPressure` (cumulative biomass consumed per unit standing
   biomass per second, normalized by a standing-biomass time integral) and exposed as
   `ExperimentMetric.RealizedGrazingPressure`. The accumulators are read-only and excluded from
   `ComputeStateHash`.
2. **Establish a positive control** — cannot be done by tuning. No setting of lifespan, site count
   or population cap gives a defended patch an individual advantage, because the consumption path
   does not read defense when removing biomass.
3. **Raise grazing pressure** — would not have worked, and would have been read as the third
   consecutive failed calibration. The table above already spans a 2.5x range of realized grazing
   without producing a gradient.

## What was changed

`SimulationConfig.PlantDefenseDeterrenceEnabled` (default `false`, flag-off byte-identical, 354/354
tests green with it added). When set, defense reduces the biomass a grazer can strip per bite:

```
allocatedAmount *= 1f - (resource.PlantDefense * PlantDefenseDeterrenceStrength);
```

with nutrition computed from the reduced amount, so a defended patch loses less tissue and the
grazer carries away correspondingly less. `PlantDefenseDeterrenceStrength` defaults to 0.75 and is
an explicit placeholder — it has not been derived.

## Deterrence alone is not sufficient — measured, not assumed

Same four arms, one variable changed (`PlantDefenseDeterrenceEnabled`), seeds 42–71:

| deterrence | founder defense | end defense | delta | extinctions | grazing pressure |
| --- | ---: | ---: | ---: | ---: | ---: |
| off | 0.30 | 0.3049 | +0.0049 | 0/30 | 0.00480 |
| off | 0.90 | 0.8813 | -0.0187 | 0/30 | 0.00947 |
| **on** | 0.30 | 0.2974 | -0.0026 | 0/30 | 0.00480 |
| **on** | 0.90 | 0.8946 | -0.0054 | 0/30 | 0.00546 |

Deterrence works mechanically — realized grazing pressure at defense 0.9 falls from 0.00947 to
0.00546, so defended patches really are losing less tissue. It produces **no selection gradient**.
Retaining biomass buys nothing while patches sit at capacity, exactly as the saturation argument
predicts.

## The pressure lever, and the wall it hits

Regeneration is 12/second against a capacity of 24, so a patch refills in about two seconds while
grazing removes roughly 0.5% of standing biomass per second. Regrowth outpaces grazing by orders of
magnitude; that, not site count or lifespan, is what pins patches at capacity.

Deterrence on, sweeping `RegenerationPerSecond`, seeds 42–71:

| regeneration | founder defense | delta | extinctions | min plant gen | grazing pressure |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 12.0 | 0.60 | +0.0017 | 0/30 | 11 | 0.00622 |
| 6.0 | 0.60 | -0.0146 | 0/30 | 11 | 0.00757 |
| **3.0** | **0.60** | **+0.1157** | **30/30** | **0** | 0.01196 |
| 1.5 | 0.60 | -0.0764 | 30/30 | 0 | 0.01173 |

Defense finally moves at regeneration 3 — **+0.1157**, roughly twenty times any delta seen at 12.
But every seed in that arm lost its animal population, and minimum plant generations is 0.

**This is not a positive control.** A trait shift measured in a collapsing ecosystem cannot serve as
evidence that selection works in a functioning one, and it violates the zero-extinction constraint
the calibration exists to satisfy. Recorded here so that the +0.1157 is not cited later as a
demonstrated gradient.

## Status: blocker diagnosed and instrumented, not resolved

The tension the lifespan document identified is real and now has numbers on it. Along this
one-dimensional lever there is **no window** where defense responds and both populations survive:
pressure sufficient to select on defense is pressure sufficient to collapse the consumer layer.

Tuning regeneration further is unlikely to find that window, because the sweep moves the food supply
for the whole animal population at the same time as it moves per-patch depletability. The next
attempt should separate those two, for instance by holding total food adequate while making an
individual patch depletable — lowering `IngestionRate` or per-patch capacity while raising site
count — so grazing can bite locally without starving the population globally. That is a scenario
redesign, not a constant to sweep, and it should be specified deliberately rather than searched for.

`RealizedGrazingPressure` now exists to steer that search instead of guessing at it, and the four
tables above are the baseline any redesign must beat.

## Standing caution

Do not read the coevolution null in `p4-coevolution-null-2026-08-17.md` as evidence about
coevolution. It was measured under a consumption path in which plant defense could not affect plant
fitness. Both it and any run made before the deterrence flag exists are measurements of a model that
has no coevolutionary channel to detect.

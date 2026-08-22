# P4 — BaseLifespanSeconds Derived, and a Warning for Coevolution — 2026-08-17

> **AFFECTED EVIDENCE — 2026-08-22.** This derives plant lifespan itself, which the fix bears on most directly of all - a takeover no longer shortens the replacement's life. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


Follows `p4-calibration-unblocked-carrying-capacity-2026-08-17.md`, which raised
the calibration scenario to six spread plant sites. With the scenario fixed,
`BaseLifespanSeconds` could finally be derived instead of guessed.

## Derivation

`ConsumerDefenseCalibrationModerate` (six spread sites), seeds 42-71, 12,000
ticks, `maximumPopulation: 48`, cognition, site competition and plant mortality
enabled. Halving downward from the placeholder:

| BaseLifespanSeconds | animal extinctions | min plant generations | mean final population | mean plant defense gene |
| ---: | ---: | ---: | ---: | ---: |
| **90** | **0/30** | **12** | 48.0 | 0.3049 |
| 45 | 1/30 | 0 | 46.4 | 0.2772 |
| 22.5 | 30/30 | 0 | 0.0 | 0.0108 |
| 11.25 | 30/30 | 0 | 0.0 | 0.0000 |

**90 is the smallest value satisfying both calibration constraints** — at least 8
plant generations and zero animal extinctions across seeds 42-71. The
placeholder was correct; it is now measured, and the comment on the constant
records the derivation.

The failure mode below 90 is not the expected one. Shorter lifespans do not
trade animal survival for faster plant turnover — they destroy the plant layer
outright. `minPlantGen` is 0 at 45 and below, meaning at least one seed lost its
plants entirely: patches die faster than dispersal can recolonize, so the whole
layer unwinds rather than cycling faster.

## Warning: there may be no selection gradient to detect

The `mean plant defense gene` column is the reason this document exists.

Founder plant defense in this scenario is **0.3**. At the derived lifespan of 90
the population ends at **0.3049** — essentially unmoved after 12 plant
generations across 30 seeds. Plant defense is not under meaningful directional
selection at these settings.

This confirms, with a direct measurement rather than an inference, the caveat
recorded when the site count was raised: six sites feed the animal population to
the cap with mean energy above 90, so grazing pressure is low, and a trait that
costs something to carry and buys nothing under weak grazing will drift rather
than climb.

Consequence for the coevolution experiment: **run under these exact settings, a
null result would be uninformative.** It would not distinguish "plants and
consumers do not coevolve in this model" from "nothing was selecting on plant
defense in the first place". The two hypotheses predict the same observation.

Note the contrast further down the table: at lifespan 45 defense moves to 0.2772
and at 22.5 to 0.0108. The trait *can* move — it is not inert like `Commitment`
(see `halfway-wired-mechanism-audit-2026-08-17.md`). It simply is not being
pushed at the calibrated settings. The downward direction at shorter lifespans
is consistent with defense being costly and unrewarded.

## Recommendation before running coevolution

Do not run the paired coevolution experiment until the setup demonstrates a
selection gradient on plant defense. Options, in rough order of directness:

1. **Report realized grazing pressure** as a first-class experiment metric —
   biomass consumed per patch per unit time — so any null can be read against
   the pressure that actually existed. This is needed regardless of which other
   option is taken.
2. **Establish a positive control**: find settings where plant defense
   demonstrably rises across generations. Without one, the experiment cannot
   distinguish a true null from an absent gradient, which is exactly the trap
   the 2026-08-17 coevolution null already fell into once.
3. **Raise grazing pressure** — a higher population cap relative to site count,
   or fewer/poorer sites — accepting that this moves back toward the extinction
   regime and needs its own calibration.

Options 2 and 3 are in tension with the zero-extinction constraint: pressure
high enough to select on defense may be pressure high enough to kill seeds. That
tension is real and worth confronting deliberately rather than discovering again
by another null result.

## State

`BaseLifespanSeconds = 90f` is derived and documented. `PlantMortalityEnabled`
still defaults `false`, so any coevolution run must enable it explicitly or it
will reproduce the original generation-2 freeze.

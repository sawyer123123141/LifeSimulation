# Calibration Scenario Carrying Capacity — Design

## Problem

`CreateConsumerDefenseCalibrationScenario` builds two active plant sites against
a population cap of 48. Carrying capacity therefore sits below the engine's
population bound: the animal population grows exponentially while food is
abundant, saturates the cap, and collapses together. At two sites, 22 of 30
seeds go extinct and plant turnover never starts.

## What was measured

An initial site-count sweep suggested four sites sufficed. That sweep was
confounded: its sites were successive points on a tight grid near the founders,
so **count and spatial arrangement moved together**. A follow-up sweep separating
the two — all arms through `ExperimentRunner`, seeds 42-71, 12,000 ticks,
`maximumPopulation: 48`, cognition, site competition and plant mortality all
enabled:

| layout | animal extinctions | min plant generations | mean final population |
| --- | ---: | ---: | ---: |
| 2 sites (as shipped) | 22/30 | 0 | 10.8 |
| 4 sites, spread across the arena | **16/30** | 11 | 21.9 |
| 4 sites, clustered along one edge | 0/30 | 13 | 48.0 |
| **6 sites, spread with two central** | **0/30** | 12 | 48.0 |
| 8 sites, clustered | 0/30 | 13 | 48.0 |

Both count and arrangement matter. Four spread sites still lose half the seeds:
when a patch dies, animals in that corner of the arena cannot reach another one.
Four clustered sites survive because every alternative is close by.

## Design

Six active sites, spread, each with co-located water and three inactive
dispersal targets. Six spread sites are chosen over four clustered ones because
clustering satisfies the constraints only by collapsing the spatial structure
the prototype exists to exercise — with every site adjacent there is no travel
decision, no local depletion, and nothing for later migration work to build on.

### Site layout

The four corner sites keep the two original coordinates plus their mirror
positions. The two central sites at `(-1, 2)` and `(-1, -18)` are what make the
spread arrangement survivable: they bridge the corners, so a creature whose
patch dies has a reachable alternative.

| # | active site | co-located water | dispersal targets |
| --- | --- | --- | --- |
| 0 | `(-12, -8)` | same | `(-20, -8)`, `(-12, 0)`, `(-8, -4)` |
| 1 | `(10, 12)` | same | `(2, 12)`, `(10, 20)`, `(14, 16)` |
| 2 | `(10, -8)` | same | `(2, -8)`, `(10, 0)`, `(14, -4)` |
| 3 | `(-12, 12)` | same | `(-20, 12)`, `(-12, 20)`, `(-8, 16)` |
| 4 | `(-1, 2)` | same | `(-9, 2)`, `(-1, 10)`, `(3, 6)` |
| 5 | `(-1, -18)` | same | `(-9, -18)`, `(-1, -10)`, `(3, -14)` |

Dispersal targets follow a fixed rule per active site: `(x-8, y)`, `(x, y+8)`,
`(x+4, y+4)`. All thirty positions are distinct within their kind, and all lie
inside the fixed arena bounds `(-25, 25)`.

These are the exact coordinates the measured arm used. They are transcribed
rather than re-derived — substituting a "equivalent-looking" layout for the
measured one is what produced the 16/30 failure above.

### Resource ordering

Six food/water pairs first (indices 0-11, food then water per site), then the
eighteen inactive dispersal targets (indices 12-29). Total resource count rises
from 10 to 30. Pairing food and water at consecutive indices preserves the
invariant asserted by
`Prototype4DefenseCalibrationChangesOnlyPlantDefenseBetweenPairedConditions`.

Per-site properties are unchanged: amount and capacity 24, regeneration 12 for
food and 1.5 for water, interaction radius 1.5, same plant genome.

### Scope

Both `ConsumerDefenseCalibrationControl` (defense 0) and
`ConsumerDefenseCalibrationModerate` (defense 0.3) change together — they share
the factory, and the paired-experiment tests require them to differ only in
plant defense. No other scenario is touched.

## What this is not

A **scenario-data change**, not a simulation-behavior change. No system, no
decision path, and no `SimulationConfig` flag is modified, so the flag-gating
convention does not apply — there is no behavior to gate. Every other scenario
is bit-identical and the standard `PredationVariation`/`Legacy` hash baseline is
untouched.

Reproduction is already density-dependent (`ReproductionSystem.CanReproduce`
gates on `needs.Energy >= phenotype.EnergyCapacity * 0.7f`). That is unchanged.
A level gate simply cannot prevent overshoot when carrying capacity sits below
the population cap.

## Test impact

1. `PlantSiteCompetition...` asserts `PlantSites.Count` of 6 without competition
   and 8 with; these become **18** and **24** (18 dispersal targets, plus 6
   active sites when competition is on).
2. `Prototype4DefenseCalibrationChangesOnlyPlantDefenseBetweenPairedConditions`,
   `PlantPatchGrowthRateMatchesCorrectedFourTimesConversion`, and
   `ConsumerDefenseCalibrationControlSustainsNonzeroPopulationAcrossAllSeeds`
   all pass unchanged.

## Testing

1. Update the two `PlantSites.Count` assertions and their comment.
2. Scenario composition: six active food resources, six co-located water
   resources, eighteen inactive food resources, thirty total, with each
   food/water pair sharing a position.
3. Every resource position distinct within its kind and inside arena bounds.
4. Regression guard: `ConsumerDefenseCalibrationModerate` at
   `maximumPopulation: 48` with mortality and site competition enabled sustains
   a nonzero final population and more than two plant generations across seeds
   42-46 at 12,000 ticks. This fails against both the two-site and the
   four-spread-site layouts.
5. Full suite green: 354 tests.

## Follow-on, out of scope

- Re-deriving `BaseLifespanSeconds`. The existing table was measured at two
  sites and does not transfer.
- Re-running the coevolution experiment. Caveat carried forward: at six sites
  the population pins at the cap with mean energy above 90, so grazing pressure
  is low and a null coevolution result could indicate an absent selection
  gradient rather than absent coevolution. That run should report realized
  grazing pressure.

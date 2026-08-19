# Calibration Scenario Carrying Capacity — Design

## Problem

`CreateConsumerDefenseCalibrationScenario` builds two active plant sites against
a population cap of 48. Carrying capacity therefore sits below the engine's
population bound: the animal population grows exponentially while food is
abundant, saturates the cap, and collapses together.

Measured, seeds 42-71, 12,000 ticks, everything else held identical
(`docs/experiments/p4-calibration-unblocked-carrying-capacity-2026-08-17.md`):

| active plant sites | animal extinctions | min plant generations |
| ---: | ---: | ---: |
| 2 (as shipped) | 22/30 | 0 |
| 4 | **0/30** | 11 |
| 6 | 0/30 | 14 |
| 8 | 0/30 | 13 |

Both calibration constraints — at least 8 plant generations, and zero animal
extinctions across seeds 42-71 — hold simultaneously from four active sites up.
The smallest satisfying value is **4**, and that is what this design adopts,
following the project's derive-don't-guess convention and its preference for the
smallest satisfying value rather than the most comfortable one.

This is why the earlier `BaseLifespanSeconds` sweep (8x range) and
`maximumPopulation` sweep (3x range) both read as unsatisfiable: neither
parameter could reach the binding constraint.

## Design

Double the active site count from two to four, preserving every other property
of the scenario: per-site amount and capacity (24), regeneration (12 for food,
1.5 for water), interaction radius (1.5), co-located water at each active site,
the plant genome, and the ratio of three inactive dispersal targets per active
site.

### Site layout

Active sites form a rectangle at ±12 / ±8-to-12, with the two existing sites
kept first and at their current coordinates so the diff stays minimal and the
founder placement is unchanged:

| # | active site | co-located water | dispersal targets |
| --- | --- | --- | --- |
| 0 | `(-12, -8)` | `(-12, -8)` | `(-20, -8)`, `(-12, -20)`, `(-4, -8)` |
| 1 | `(10, 12)` | `(10, 12)` | `(18, 12)`, `(10, 22)`, `(2, 12)` |
| 2 | `(10, -8)` | `(10, -8)` | `(18, -8)`, `(10, -18)`, `(2, -8)` |
| 3 | `(-12, 12)` | `(-12, 12)` | `(-20, 12)`, `(-12, 22)`, `(-4, 12)` |

Sites 0 and 1 and their six targets are exactly the current scenario. Sites 2
and 3 replicate the same ±8-offset dispersal pattern.

All twenty positions are distinct, and every one lies inside the fixed arena
bounds `(-25, 25)` on both axes — the furthest are `(-12, 22)`, `(10, 22)` and
`(10, -18)`.

### Resource ordering

Emission order is: the four food/water pairs first (indices 0-7, food then water
per site), then the twelve inactive dispersal targets (indices 8-19). Total
resource count rises from 10 to 20.

Pairing food and water at consecutive indices preserves the invariant asserted
by `Prototype4DefenseCalibrationChangesOnlyPlantDefenseBetweenPairedConditions`,
which checks that resources 0/1 and 2/3 share a position.

### Scope

Both `ConsumerDefenseCalibrationControl` (defense 0) and
`ConsumerDefenseCalibrationModerate` (defense 0.3) change together, since both
are produced by the same factory and the paired-experiment tests require them to
differ only in plant defense.

No other scenario is touched. `PlantBackedBaseline`, `DefendedPlants`,
`UndefendedPlants` and `WatchableStarterHabitat` keep their current layouts.

## What this is not

This is a **scenario-data change**, not a simulation-behavior change. No system,
no decision path, and no `SimulationConfig` flag is modified, so the project's
flag-gating convention does not apply — there is no behavior to gate. Runs of
every other scenario are bit-identical, and the standard hash-regression
baseline (a `PredationVariation`/`Legacy` scenario) is untouched.

Reproduction is already density-dependent
(`ReproductionSystem.CanReproduce` gates on
`needs.Energy >= phenotype.EnergyCapacity * 0.7f`); nothing about that changes.
The gate simply cannot prevent overshoot when carrying capacity is below the
population cap, which is what this design fixes.

## Test impact

1. `ResourceExperimentTests.PlantSiteCompetition...` asserts
   `PlantSites.Count` of **6** without competition and **8** with. With four
   active sites and twelve dispersal targets these become **12** and **16**. The
   comment above them ("2 active founder Food sites + the scenario's existing 6
   inactive dispersal targets") must be updated to say four and twelve.
2. `Prototype4DefenseCalibrationChangesOnlyPlantDefenseBetweenPairedConditions`
   passes unchanged — it compares control against moderate, both of which move
   together, and its positional assertions cover indices 0-3 which remain
   food/water pairs.
3. `PlantPatchGrowthRateMatchesCorrectedFourTimesConversion` passes unchanged —
   it reads `Plants.GetAt(0).GrowthRate`, which depends on
   `InitialPopulation`, not on site count.
4. `ConsumerDefenseCalibrationControlSustainsNonzeroPopulationAcrossAllSeeds`
   passes unchanged and more robustly: it asserts nonzero final population for
   seeds 42-46, which more food can only help.

## Testing

1. Update the two `PlantSites.Count` assertions and their comment.
2. New test: the calibration scenario exposes four active food resources, four
   water resources co-located with them, and twelve inactive food resources, for
   twenty total.
3. New test: every resource position in the scenario is distinct and lies within
   the arena bounds.
4. Regression guard for this finding: `ConsumerDefenseCalibrationModerate` at
   `maximumPopulation: 48`, plant mortality and site competition enabled,
   sustains a nonzero final population across seeds 42-46 at 12,000 ticks. This
   fails against the two-site scenario and passes after. Five seeds rather than
   thirty keeps suite runtime acceptable; the full thirty-seed result is recorded
   in the experiment document.
5. Full existing suite stays green (`cd tools/HeadlessTests && dotnet test`),
   currently 351 tests.

## Follow-on, explicitly out of scope

- Re-deriving `BaseLifespanSeconds` against the restored constraints. The
  existing lifespan table was measured at two sites and does not transfer.
- Re-running the coevolution experiment. Note the caveat recorded in the
  experiment document: at four or more sites mean energy exceeds 93, so grazing
  pressure is low and a null coevolution result could indicate an absent
  selection gradient rather than absent coevolution. That run should report
  realized grazing pressure.

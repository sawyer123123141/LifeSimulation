# P4 Calibration Unblocked — the Scenario Was Too Small — 2026-08-17

> **AFFECTED EVIDENCE — 2026-08-22.** The site-count and arrangement calibration recorded here was measured through ExperimentRunner with mortality and site competition enabled, and downstream scenarios still depend on it. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


> **CORRECTED 2026-08-17.** The original sweep below varied site count and site
> *geometry* together — its "sites" were successive points on a tight grid near
> the founders — so attributing the effect to count alone was unjustified. A
> follow-up sweep separating the two, run through `ExperimentRunner` to match the
> regression test, gives: 4 sites **spread** across the arena 16/30 extinct; 4
> sites **clustered** 0/30; 6 sites spread 0/30; 8 clustered 0/30. Both count and
> arrangement matter. The shipped scenario uses **6 spread** sites, not 4. The
> two-site collapse and the overshoot mechanism are unaffected.

Third and final entry in the chain
`p4-coevolution-null` → `p4-plant-mortality-calibration-blocked` →
`p4-memory-root-cause-retracted` → this document.

The calibration was declared unsatisfiable after two sweeps failed to satisfy
both constraints at once: an 8x sweep of `BaseLifespanSeconds` and a 3x sweep of
`maximumPopulation`. Both conclusions were correct for the scenario as written
and wrong as general claims. The binding variable was neither lifespan nor the
population cap — it was **the number of active plant sites**.

## Measurement

Site-count sweep, everything else identical to
`ConsumerDefenseCalibrationModerate` (same plant genome, same per-site amount,
capacity and regeneration, same co-located water, same three-inactive-dispersal-
targets-per-active-site ratio), seeds 42-71, 12,000 ticks,
`maximumPopulation: 48`, cognition enabled, site competition and plant mortality
enabled:

| active plant sites | animal extinctions | min plant generations | mean final population | mean energy |
| ---: | ---: | ---: | ---: | ---: |
| 2 (as shipped) | 22/30 | 0 | 10.8 | 77.6 |
| 4 | **0/30** | 11 | 47.9 | 93.2 |
| 6 | **0/30** | 14 | 47.9 | 92.9 |
| 8 | **0/30** | 13 | 48.0 | 93.4 |

Both calibration constraints — at least 8 plant generations, and zero animal
extinctions across seeds 42-71 — are satisfied simultaneously at 4 active sites
and above. The smallest satisfying value is **4**.

Note that `ConsumerDefenseCalibrationModerate` ships with **2** active food
sites (plus 6 inactive dispersal targets). Earlier notes in this chain described
it as having 8 food sites, conflating active sites with the total resource
count; that error is part of why the capacity mismatch went unnoticed.

## Why the earlier sweeps read as unsatisfiable

Carrying capacity sat below the population cap. With 2 active sites feeding a
ceiling of 48, the engine's array bound bound before the ecology did, so
population grew exponentially to the cap and then collapsed together — the
overshoot recorded in `p4-memory-root-cause-retracted-2026-08-17.md`. Every
sweep along lifespan or cap was moving parameters that could not reach the
actual constraint, so all of them traded one failure for the other.

Reproduction is already density-dependent — `ReproductionSystem.CanReproduce`
gates on `needs.Energy >= phenotype.EnergyCapacity * 0.7f` — but a level gate
cannot prevent overshoot when food is abundant early: the population multiplies
while the signal is still green, and the gate closes only once everyone is
already starving.

## Caveat that matters for the coevolution experiment

At 4 or more sites the population still pins at the cap (mean final 47.9 of 48)
with mean energy above 93. Food is no longer scarce, so the cap is once again
the binding constraint — this time harmlessly, but it means grazing pressure is
low. Plant defense is selected on only when grazing actually costs the plant.

Running the coevolution experiment in a food-saturated world risks a null result
for the opposite reason to the original one: not famine, but plenty. The
site count for the coevolution run should be chosen as the smallest value that
keeps both populations alive, not the most comfortable one, and the run should
report realized grazing pressure so a null can be distinguished from an absent
selection gradient.

## Recommended change

Raise `CreateConsumerDefenseCalibrationScenario` to 4 active sites, keeping the
three-inactive-targets-per-active-site ratio, then re-derive
`BaseLifespanSeconds` against the restored constraints and re-run the
coevolution experiment.

This is a scenario-data change, not a simulation-behavior change: no system, no
flag, and no decision path is altered. It does affect committed tests, which
consume `ConsumerDefenseCalibrationControl` and
`ConsumerDefenseCalibrationModerate`
(`Assets/Tests/EditMode/ResourceExperimentTests.cs`,
`Assets/Editor/PrototypeBatchEntry.cs`), so it needs the normal spec-and-plan
pipeline rather than an in-place edit.

## Superseded claims

- "The calibration's constraint set is unsatisfiable" (from
  `p4-memory-root-cause-retracted-2026-08-17.md`) is **withdrawn**. It is
  satisfiable at 4 active sites.
- The proposal to restate the constraint as persistence-based is **withdrawn**;
  no constraint change is needed.
- `BaseLifespanSeconds = 90f` remains uncalibrated. The earlier lifespan table
  was measured at 2 sites and does not transfer.

# A2 is preserved on `run-length-triage` and is absent from `main`

**Date:** 2026-09-10. **Status:** integration note. **No experimental record is altered by this
document** — every predeclaration, result, raw artefact and commit hash on `main` is exactly as it was
written.

---

## What happened

`run-length-triage` was fast-forwarded into local `main` in full, all eighteen commits with their
original hashes, so every cross-reference inside the committed evidence still resolves. The graded
seeding mechanism — candidate **A2** — was then removed from `main`'s tree in a follow-up revert of
`cd20f1b`.

**The evidence stayed. The mechanism did not.**

## Why the mechanism was not kept

A2 replaced `PlantReproductionSystem`'s all-or-nothing seeding threshold with a ramp built from
`MaturityFraction`. It was tested against C3 at brake 1.5, 24 seeds, first at 24,000 ticks and then at
a frozen 72,000-tick horizon.

**It failed its own predeclared criterion.** At 72,000 ticks the arm is **16 of 24 alive** against a
threshold of `ceil(0.85 x 20) = 17` — COLLAPSE, and the alternative denominator of 24 gives 21 and
fails as well, so both readings agree.

**And the apparent stabilisation was the population cap, not the ecology.** Twelve of the sixteen
survivors touch the cap of 500, thirteen end at 475 or above, and excluding cap-contacting worlds
leaves **four survivors of twenty-four**. The survivor interquartile range is 496.5 to 500 — the
repository's own test is that *a carrying capacity produces a distribution and a cap produces a
constant*. Reaching the cap protected nothing: seeds 51 and 52 both hit 500 and were extinct within
8,000 ticks.

**The governing rule, applied.** A mechanism enters production because it is independently more
biologically correct and has a justified future role — **not** because it improved one cell that still
collapsed. A2 has not met that bar, and it is not merged on the strength of this experiment.

**The honest counter-argument, recorded rather than buried.** The all-or-nothing gate arguably *is* a
modelling defect on its own terms: a patch at 74% of capacity produces literally zero seed, and
`p6-what-limits-the-peak-2026-09-06.md` measured plant age mortality at **78% of gross growth in the
ungrazed phase**, which makes that gate the only valve on the only inflow opposing a large constant
outflow. That case may well be winnable. **It has not been made, and it must be made on biological
grounds and tested on its own before A2 returns.**

## Where A2 lives now

| | |
|---|---|
| branch | **`run-length-triage`**, not deleted, not renamed |
| branch tip | **`cfe3b32`** |
| the mechanism | **`cd20f1b`** — "Graded plant seeding, and the arm that lands 0.4 of a world from the threshold" |
| its tests | `Assets/Tests/EditMode/PlantGradedSeedingTests.cs`, at that commit |

`cd20f1b` is also an ancestor of `main`, so the code is readable from `main`'s history; it is simply
not in `main`'s tree. Restoring it is `git revert` of the removal commit, and it should not be done
without the biological argument above.

## What `main` kept

**All of the instrumentation**, none of which depends on A2:

- `SimulationStatistics.SeedEligiblePlantPatchCount` — live patches at or above the seeding threshold,
  the ratchet's state variable, which occupancy cannot see.
- `PlantReproductionSystem.MaturityFraction` stays **public**. It was promoted in `f048e15`, an
  instrumentation commit, so the statistic reads the same constant the gate does rather than
  duplicating the literal. **This is not a remnant of A2.**
- `Trajectory`'s plant table, the per-arm-per-seed-per-tick longitudinal CSV with plant columns, cap
  contact, run peak and a behaviour hash at every sample, configurable sample count defaulting to nine
  so every recorded artefact still reproduces, and both final-population distributions printed and
  labelled all-world against survivor-conditioned.
- `PlantSweep`'s `no_living_bred_lineage`, renamed from `frozen` because the name was a history word
  over a scan of the present.
- **17 reporting regression tests** in `TrajectoryReportingTests.cs`, covering extinction inclusion,
  per-seed identity, plant columns, sample spacing, final-sample inclusion and hash-inertness.

**All of the evidence**, unaltered: the Regime B triage, the production-versus-access measurement, the
graded-seeding arm at both horizons, the per-seed analysis, the frozen extension predeclaration with
its provenance, every raw `.txt` and `.csv` corpus, the persistence-threshold erratum, the lessons
log, the regulation design spec and its plan.

## One cosmetic residue, recorded rather than tidied

`TrajectoryReportingTests.cs` uses the string `"graded-seeding"` as an arm **label** in two tests, and
`Trajectory.cs` carries a comment citing the graded-seeding arm as the reason the recorded sample
spacing matters. Neither is the mechanism — the label is an arbitrary string whose round-trip through
the CSV's `arm` column is what is being tested, and the comment is a true historical justification.
They are left alone because renaming them would edit files outside the removal's scope.

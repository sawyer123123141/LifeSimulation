# The 168-site low-occupancy condition: recovery attempt and replication

**Date:** 2026-08-22
**Status:** recovery attempted and failed; a replication condition is committed in its place.

## Why this exists

Three standing documents rest on a 168-site "abundant sites / low occupancy" operating point:

- `p4-site-abundance-seed-production-rate-2026-08-20.md`
- `p4-low-occupancy-plant-route-audit-2026-08-20.md`
- `p4-low-occupancy-growth-trait-reaudit-2026-08-20.md`

All three were measured with `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled` on, which is
exactly the path the `ReplaceAt` takeover-age fix (`4cc9a47`) changes. They therefore need an impact
assessment — and could not receive one, because the scenario they ran on does not exist in the
repository.

## Recovery attempt

Searched, in this order, with results:

| source | result |
|---|---|
| `git log --diff-filter=D -- 'Assets/Tests/EditMode/ZZZ*'` | **nothing** — no ZZZ probe was ever committed, so none was ever deleted |
| `git log --all --name-only -- 'Assets/Tests/EditMode/ZZZ*'` | **nothing** |
| `git log --all -S"168" -- Assets/` | one unrelated hit in a predation-formula commit |
| the three writeups | **partial recovery — see below** |
| `p4-site-abundance-seed-production-rate-2026-08-20.csv` | per-seed results including a `state_hash` column, but no geometry |

### What was recovered

- **Site composition:** 168 food sites = **6 active plus 162 inactive dispersal targets**.
- **Active sites are unchanged** from the 24-site calibration (the writeups describe the abundant
  condition as adding targets, not moving active sites).
- **Config:** the calibration configuration transcribed from
  `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds`,
  `maximumPopulation = 48`.
- **Method:** 120 deterministic seeds, **42–161**, **12,000 ticks**, seed-production dispersal
  charge 0.
- **Resulting occupancy:** 0.332 (flag off) and 0.322 (flag on), against 0.908 / 0.904 at 24 sites.
- **Method note that matters:** a first **42-site** version was discarded after a preflight because
  it stayed ~0.88 occupied. The 168-site layout was itself *selected to achieve low occupancy*.

### What could not be recovered

**The coordinates of the 162 inactive targets.** The 24-site calibration places 3 targets per active
site at `(x-8, y)`, `(x, y+8)` and `(x+4, y+4)`. The abundant version needs 27 per active site and
no document records the rule used. Nothing in git, the CSVs or the writeups fixes those positions.

**Exact recovery is therefore impossible, and this is recorded rather than papered over.** Nothing
below should be read as a re-run of the original experiment.

## The replication condition

`Prototype4Scenarios.AbundantSiteReplicationModerate` (`p4-abundant-site-replication-moderate`) is
committed as scenario data — deliberately not as another throwaway probe, since that is the failure
this whole document exists because of.

Identical to `ConsumerDefenseCalibrationModerate` in active sites, co-located water, capacities,
regeneration, founder genome and founder placement. **Only the dispersal-target layout differs**, and
it is fully specified:

> a lattice over [-24, 24] on both axes in steps of 4 (13 x 13 = 169 points), excluding any point
> within 2.0 of an active food site, taking the first 162 remaining points in row-major order.

### Relationship to the original

- **Count: identical** — 6 active plus 162 inactive, 168 total.
- **Geometry: different, and knowably so.** The original fanned targets around each active site; this
  spreads them across the whole arena on a regular lattice. Both raise free-site availability, which
  is the mechanism the occupancy change runs through, but they are not the same arrangement.
- **Selection criterion: the same one the original used.** The original discarded a 42-site layout
  for staying ~0.88 occupied, i.e. the layout was chosen to hit a low-occupancy condition. Judging
  this replication on whether it reproduces occupancy near **0.32–0.33** is faithful to that method.

**No byte-equivalence is claimed and none is achievable.** State hashes will differ from the recorded
ones for both this reason and the takeover-age fix, and the recorded hashes cannot distinguish the
two causes. Any comparison between this condition and the recorded 168-site numbers is a comparison
of *conditions*, not of runs.

## Provenance for future re-runs

Everything needed to reproduce this condition after a future fix:

- **Scenario:** `Prototype4Scenarios.AbundantSiteReplicationModerate`, committed in
  `Assets/Scripts/Simulation/Experiments/SimulationScenario.cs`.
- **Config:** `CreatePrototype4Defaults(seed, 12)` schedule and founder profile, cognition on,
  physiology on, `IntentUtilityV1`, `plantCohortsEnabled`, `maximumPopulation: 48`,
  `plantSiteCompetitionEnabled: true`, `plantMortalityEnabled: true`; the establishment contest and
  the seed-production charge are the varied arms.
- **Seeds:** 42–161. **Ticks:** 12,000.
- **Founder variance:** plant selection requires varying founder traits; a uniform founder genome
  produces drift only. The revalidation sweep's rule is recorded alongside its results.

## Result

See `p4-postfix-revalidation-2026-08-22.md` for the measured occupancy this layout produces and
whether the low-occupancy conclusions replicate under it on fixed code.

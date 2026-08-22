# Evidence-impact audit for the 2026-08-22 correctness fixes

**Date:** 2026-08-22
**Raw data:** `evidence-impact-audit-2026-08-22.csv` (paired old/new rows)
**Fixes under audit:** `4cc9a47` (plant takeover age reset) and `9763374` (statistics sampled after
death commit; `CaptureStatistics` for end-of-run reporting).

## Method

The same probe file was run in two trees: a detached worktree at `15c7a5a` (pre-fix) and fixed
`main`. Identical seeds, identical configs, identical tick counts. 85 paired runs.

- **Battery A**, plant site competition **off**: `ObservationStable`, the same with home-range
  enabled, `ObservationRouteRing`, the same with home-range enabled, and
  `ObservationShiftingPatches` with plant mortality. 5 seeds each, 6,000 ticks.
- **Battery B**, plant site competition **on** with mortality: the canonical high-occupancy
  calibration copied from
  `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds`
  (12 founders, `maximumPopulation` 48, `ConsumerDefenseCalibrationModerate`, 12,000 ticks), with
  and without `PlantEstablishmentContestEnabled`. 30 seeds each.

`ReplaceAt` is only reached on the competition path, so Battery A is the falsification check: any
difference there would mean the fix has a reach nobody predicted.

## Result 1 — everything with competition off is bit-identical

| arm | seeds | state hashes differing |
|---|---|---|
| stable | 5 | **0/5** |
| stable + home-range | 5 | **0/5** |
| route ring | 5 | **0/5** |
| route ring + home-range | 5 | **0/5** |
| shifting patches + mortality | 5 | **0/5** |

Every trajectory metric matches exactly: population, births, plant count, plant births, plant
generations, and all seven mean plant traits.

**The 2026-08-22 home-range, route-ring and shifting-patch conclusions are untouched.** They were
measured with `PlantSiteCompetitionEnabled` off, so no takeover ever occurred in them. No banner,
no retraction, no re-run needed.

One informative exception proves the statistics fix behaves as designed: in `ring-homerange`, one
seed of five reports **one extra death** (mean +0.2) while its **state hash is identical**. That is
precisely the signature of a reporting-only change — the trajectory is bit-for-bit the same, and the
final sample now includes a death the old sample dropped. Reported mortality was previously
understated at run boundaries; the underlying simulation was not wrong.

## Result 2 — the competition path genuinely moves

| metric | competition arm (30 seeds) | contest arm (30 seeds) |
|---|---|---|
| state hashes differing | **30/30** | **30/30** |
| plant births | -5.63, t **-2.65**, 9 up / 21 down | -0.20, t -0.11 |
| highest plant generation | -0.20, t -0.52 | -1.00, t **-2.63**, 9 up / 16 down |
| mean SeedlingResilience | -0.0143, t -0.88 | **-0.0237, t -1.99**, 11 up / 19 down |
| mean Dispersal | -0.0049, t -0.39 | -0.0131, t -1.22 |
| mean Growth | +0.0019, t +0.16 | -0.0120, t -1.12 |
| mean SeedProductionRate | +0.0070, t +0.63 | -0.0131, t -0.84 |
| mean Defense | -0.0131, t -0.76 | -0.0095, t -0.79 |
| creature final population | 48.0 both | 47.87 to 47.90 |
| creature births | +0.80, t +1.02 | 0.00 |

Every seed's trajectory changed, which is expected and correct: takeovers used to install seedlings
that were already old and died almost immediately, and now they live a full lifespan. Consumer-side
survival is unaffected (population pinned at the 48 cap in both builds).

**The direction matters.** `SeedlingResilience` — the trait the establishment conclusion is about —
moves **downward** under the fix in the contest arm, at t -1.99 with 19 of 30 seeds down. Plant
generations fall by a full generation (t -2.63). These are not large effects, but they push against
the recorded establishment result rather than with it, and they are measured on the very mechanism
that conclusion concerns.

## What this does and does not license

**Not retracted.** The recorded establishment result (`SeedlingResilience` rising at t +4.03,
76/120 seeds up) was measured under a different design from Battery B: 120 seeds, varying plant
founder traits, and a specific dispersal charge. Battery B uses the calibration scenario's **uniform
founder genome**, so its trait means are drift, not selection. A drift-magnitude shift of t -1.99
does not by itself overturn a selection result measured at t +4.03 under standing variance.

**Not cleared either.** The fix demonstrably changes the exact mechanism that conclusion rests on,
in the unfavourable direction. Asserting the conclusion survives would be the "convincing mechanism
story" failure this project has been burned by repeatedly.

The honest status is therefore **"requires re-measurement"**, recorded as banners on the affected
experiment documents rather than retractions, and the original files preserved unchanged.

Affected, banner added:

- `p4-establishment-contest-2026-08-20.md`
- `p4-where-plant-fitness-is-decided-2026-08-20.md`
- `p4-invader-establishment-contest-2026-08-21.md`

Unaffected and explicitly cleared: every 2026-08-22 experiment, and any result measured with
`PlantSiteCompetitionEnabled` off.

## Not reproducible in this audit

The **168-site low-occupancy** operating point could not be re-run. Its geometry lived in a
throwaway probe that was deleted after use and never became scenario data, so it does not exist in
the repository. Re-auditing `p4-site-abundance-seed-production-rate-2026-08-20.md`,
`p4-low-occupancy-plant-route-audit-2026-08-20.md` and
`p4-low-occupancy-growth-trait-reaudit-2026-08-20.md` requires reconstructing that 168-site layout
first, from the description in those documents.

This is itself a finding: **an experiment whose scenario is not committed cannot be re-audited when
a correctness fix lands.** It is the strongest argument for the experiment manifest/provenance item
already queued from the review, and the reason a reconstructed 168-site geometry should be added as
committed scenario data rather than as another throwaway probe.

## Positive controls

`Dispersal` and `SeedInvestment` were measured in both builds and move by -0.0049/-0.0131 and
-0.0006/-0.0037 respectively, at |t| ≤ 1.22 — no material change under drift conditions. They remain
usable as positive controls, but the same caveat applies: their recorded *selection* strengths
(Dispersal t +14 to +19.6) were measured under varying founders and would need the same re-run to be
re-confirmed on the competition path.

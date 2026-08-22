# Reproducing the low-occupancy operating point: occupancy is a cliff

**Date:** 2026-08-22
**Raw data:** `p4-occupancy-calibration-2026-08-22.csv`, `p4-occupancy-calibration-fine-2026-08-22.csv`,
`p4-occupancy-calibration-final-2026-08-22.csv`
**Follows:** `p4-168-site-replication-2026-08-22.md` and `p4-postfix-revalidation-2026-08-22.md`,
where the first replication attempt produced occupancy 0.84 instead of the recorded 0.32.

## The question

The three low-occupancy documents could not be audited because their scenario was never committed.
A first replication kept the recorded site count exactly — 6 active plus 162 inactive — and laid the
targets on a lattice of spacing 4. It produced **occupancy 0.840**, while the 24-site arm in the same
sweep reproduced its recorded occupancy to within 0.006. The harness was right; the geometry was
wrong.

`PlantPhenotype.DispersalRange` is `4 + 20 * Dispersal`, and `Dispersal` evolves strongly upward, so
by late run a patch throws seeds 14–24 units. A lattice at spacing 4 is trivially reachable and
saturates. **Site count was never the lever. Target spacing is.**

## Method

Held fixed: the six active sites and their water, capacities, regeneration, founder genome, the
config (competition and mortality on, `maximumPopulation` 48, 12 founders), 12,000 ticks, 10 seeds
(42–51). Varied: the span of the 13 x 13 target lattice, which sets spacing. Measured: mean occupancy
sampled every 100 ticks, plus survival and plant generations. **Occupancy only — no trait conclusion
is drawn in this document.**

## Result: a cliff with a narrow viable window

| span | spacing | occupancy | plant generations | extinct |
|---|---|---|---|---|
| 48 | 4.00 | 0.833 | 20.0 | 0/10 |
| 96 | 8.00 | 0.528 | 17.2 | 0/10 |
| 108 | 9.00 | 0.406 | 17.6 | 0/10 |
| **112** | **9.33** | **0.350** | 16.1 | **0/10** |
| **114** | **9.50** | **0.311** | 16.7 | **0/10** |
| **116** | **9.67** | **0.286** | 16.3 | **0/10** |
| 120 | 10.00 | 0.262 | 15.1 | 0/10 |
| 132 | 11.00 | 0.085 | 14.9 | 3/10 |
| 144 | 12.00 | 0.047 | 6.8 | 8/10 |
| 160 | 13.33 | 0.023 | 3.0 | 9/10 |
| 208 | 17.33 | 0.012 | 0.0 | 10/10 |
| 320 | 26.67 | 0.007 | 0.0 | 10/10 |

Occupancy does not fall smoothly — it holds above 0.5 to spacing 8, passes through the target band
between spacing 9 and 10, and then collapses. By spacing 11 the ecosystem is already failing (3/10
extinct) and by 13.3 it is gone. **The window that reproduces the recorded 0.322–0.332 with clean
survival is roughly spacing 9.3 to 9.7 — about four percent of the range swept.**

That the window is this narrow is the reason the first attempt missed: any "spread the targets out a
bit" guess lands on one side of the cliff or the other.

## Committed condition

`Prototype4Scenarios.AbundantSiteReplicationModerate` now uses **span 114, spacing 9.5**, measuring
**occupancy 0.311** against the recorded 0.322–0.332, with **0/10 seeds extinct** and 16.7 plant
generations. Span 112 (0.350) brackets the recorded band from above and 116 (0.286) from below; 114
was chosen as the closest single value without tuning to a third decimal, which would be fitting
noise.

## The caveat that must travel with any result from this scenario

At spacing 9.5 the lattice spans ±57, while the creature arena is hard-coded to ±25. **Patches
establishing on the outer targets are never grazed.** That refugium does not exist in the 24-site
condition, and it is not known whether the original 168-site layout had one, because its coordinates
are unrecoverable.

This matters for exactly the conclusions this scenario exists to re-audit. Ungrazed refugia change
the selective environment for defense and for anything mediated by grazing pressure, and they change
where free sites are available. **A trait result measured here reproduces the recorded occupancy, not
necessarily the recorded ecology.** Any use of this scenario must report
`RealizedGrazingPressure` alongside its trait numbers and state this difference.

## Status of the low-occupancy conclusions

Still **not** re-audited. What has changed is that a condition now exists to re-audit them in:

- `p4-site-abundance-seed-production-rate-2026-08-20.md` (SeedProductionRate becoming selected)
- `p4-low-occupancy-plant-route-audit-2026-08-20.md` (mortality/lifespan headroom, establishment
  reversal)
- `p4-low-occupancy-growth-trait-reaudit-2026-08-20.md` (the six growth-rate nulls)

The next step is the 120-seed varying-founder sweep at span 114, with a matched disabled-trait drift
control — not the competition-off arm, which the previous sweep showed is not a null distribution —
and with grazing pressure reported.

# P4 — More Patches Cuts Drift, and Two Refuted Hypotheses — 2026-08-18

Follows `p4-dose-curve-2026-08-18.md`. Tests the one route that should improve **both** halves of a
coevolution measurement: raise the plant population instead of lowering the grazer cap.

## Confirmed: more patches reduces plant-side drift

Active sites varied at fixed grazer cap 48, mixed founders (alternating defended / undefended),
seeds 42-71, 12,000 ticks. Spatial **extent held constant** — every arm fills the same box — so only
density changes.

| active sites | live patches | n | mean Δ | **SD** | t | 95% CI | pop | plant gens |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| 6 | 22.7 | 30 | -0.0455 | **0.1568** | -1.59 | [-0.0997, +0.0116] | 48.0 | 15.1 |
| 12 | 45.9 | 30 | -0.0511 | **0.1219** | -2.30 | [-0.0924, -0.0090] | 48.0 | 16.0 |
| 24 | 92.3 | 30 | -0.0279 | **0.1248** | -1.23 | [-0.0703, +0.0169] | 48.0 | 16.8 |

**The prediction holds:** across-seed SD falls from 0.157 to 0.122 as live patches go 23 to 46, a
~22% reduction, then plateaus. Drift really is population-size driven, confirming the diagnosis in
`defense-drift-report.html`. Consumer population is unaffected at 48 throughout, so unlike lowering
the grazer cap this does not trade one side against the other.

**But the effect shrinks too**, so significance still peaks in the middle (12 sites) rather than at
the largest population. At fixed grazers, more patches means fewer grazers per patch — the same
inverted U as the cap sweep, approached along the other axis.

### Confound, stated

Generating sites procedurally moves count and geometry together — the trap recorded in
`AGENT_FIELD_NOTES` §5 from the 2026-08-17 site-count sweep. Holding extent constant and varying only
density mitigates it, but arrangement still differs between arms. This is a first look at direction,
not a clean single-variable result, and the SD trend is the part worth trusting.

## Refuted hypothesis 1 — grazing does not become uniform at high density

From the previous document: heavy grazing might hit every patch equally, shrinking the differential
selection needs. Prediction was that per-patch biomass dispersion falls with density. It rises
monotonically, 0.120 to 0.236 across caps 0 to 48. **Withdrawn.**

## Refuted hypothesis 2 — realized grazing pressure is not the controlling variable

If selection strength were a function of grazing pressure, the cap sweep and the patch-count sweep
should collapse onto one curve when plotted against it. Both sweeps, sorted by pressure:

| arm | grazing pressure | mean Δ | t |
| --- | ---: | ---: | ---: |
| cap 0 / 6 sites | 0.000000 | -0.0015 | -0.05 |
| cap 3 / 6 sites | 0.000518 | +0.0264 | 0.79 |
| **cap 12 / 6 sites** | **0.001026** | -0.0957 | **-3.25** |
| **cap 6 / 6 sites** | **0.001028** | -0.0169 | **-0.52** |
| **24 sites / cap 48** | **0.001049** | -0.0279 | **-1.23** |
| cap 18 / 6 sites | 0.001510 | -0.0776 | -2.33 |
| cap 24 / 6 sites | 0.002076 | -0.0804 | -2.64 |
| 12 sites / cap 48 | 0.002130 | -0.0511 | -2.30 |
| cap 36 / 6 sites | 0.003255 | -0.0434 | -1.19 |
| 6 sites / cap 48 | 0.004419 | -0.0455 | -1.59 |
| cap 48 / 6 sites | 0.004536 | -0.0548 | -1.44 |

Three arms sit at essentially identical pressure (~0.00103) and produce t of -3.25, -0.52 and -1.23.
**The collapse fails**, so grazing pressure alone does not determine selection strength.

The cap-6 arm has a final population of 5.4 — a failing consumer population — which plausibly
explains that outlier specifically. At n=30 this cannot be distinguished from "pressure is not the
controlling variable", and no preference between those readings is asserted here. The arms at
~0.0021 and ~0.0044 do agree well, so the disagreement is concentrated at low pressure where
populations are marginal.

## Standing position

Two mechanistic hypotheses have now been tested and refuted in succession. What survives is
empirical and worth stating plainly:

- Defense declines **only when grazers are present** (`p4-grazing-dose-response`).
- The strongest, correction-surviving signal is **cap 12 at 6 sites** (t = -3.25).
- **More patches genuinely reduces drift** (SD -22%) without costing the consumer side.
- Neither uniform-grazing nor realized-pressure explains the inverted-U shape.

The obvious combination — more patches *and* a grazer cap tuned to keep grazers-per-patch near the
cap-12 optimum — has not been run. That is the next experiment, and it should be specified before
running rather than searched: at 12 sites (~46 patches) the cap-12 grazers-per-patch ratio implies a
cap near 24.

Raw per-seed data: `patch-count.csv`.

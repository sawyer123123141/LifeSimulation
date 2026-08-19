# P4 — Dose Curve: Peak at Cap 12, and a Refuted Hypothesis — 2026-08-18

Follows `p4-grazing-dose-response-2026-08-18.md`, which established the defense decline is
grazing-driven but left one thing explicitly unexplained: the effect *weakened* as grazing rose
(t = -3.25 at cap 12, -2.64 at 24, -1.44 at 48).

## Hypothesis tested — and refuted

Proposed explanation: at high grazer density every patch is grazed near-maximally, so grazing
becomes uniform across patches, the between-patch differential shrinks, and selection has less to
act on. This is the averaging constraint from
`flag-liveness-and-the-averaging-constraint-2026-08-18.md` reappearing.

It predicts **per-patch biomass dispersion falls as density rises**. Measured (coefficient of
variation of biomass across live patches, sampled every 400 ticks):

| cap | 0 | 3 | 6 | 12 | 18 | 24 | 36 | 48 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dispersion | 0.120 | 0.118 | 0.139 | 0.201 | 0.209 | 0.216 | 0.219 | **0.236** |

Dispersion **rises** monotonically. Grazing becomes *less* uniform at high density, not more.
**The hypothesis is wrong and is withdrawn.** The weakening at high grazing remains unexplained;
no replacement story is offered here.

## The curve

Mixed founders (three sites at 0.60, three at 0.00), seeds 42-71, 12,000 ticks, quality preference
on, baseline sampled at tick 200.

| grazer cap | n | mean Δ | SD | t | 95% bootstrap CI | grazing pressure | final pop | plant gens |
| ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| 0 | 30 | -0.0015 | 0.1612 | -0.05 | [-0.0636, +0.0506] | 0.000000 | 0.0 | 16.5 |
| 3 | 30 | +0.0264 | 0.1831 | 0.79 | [-0.0397, +0.0942] | 0.000518 | 0.7 | 16.1 |
| 6 | 30 | -0.0169 | 0.1784 | -0.52 | [-0.0812, +0.0438] | 0.001028 | 5.4 | 16.0 |
| **12** | 30 | **-0.0957** | 0.1611 | **-3.25** | **[-0.1482, -0.0328]** | 0.001026 | 12.0 | 15.5 |
| 18 | 30 | -0.0776 | 0.1825 | -2.33 | [-0.1374, -0.0076] | 0.001510 | 18.0 | 15.2 |
| 24 | 30 | -0.0804 | 0.1669 | -2.64 | [-0.1338, -0.0203] | 0.002076 | 24.0 | 15.5 |
| 36 | 30 | -0.0434 | 0.2001 | -1.19 | [-0.1103, +0.0304] | 0.003255 | 36.0 | 15.4 |
| 48 | 30 | -0.0548 | 0.2083 | -1.44 | [-0.1233, +0.0303] | 0.004536 | 48.0 | 15.6 |

An inverted U: null with no grazers, peaking at cap 12, decaying as density rises.

### Multiple comparisons

Seven grazed arms puts the Bonferroni bar at |t| ~ 2.94. **Only cap 12 clears it.** Caps 18 and 24
have intervals excluding zero but do not survive correction, and should be described as suggestive
rather than established. The cap-0 control remains pre-specified rather than fished, which is what
carries the causal argument.

### Caps 3 and 6 are not "low grazing"

Final populations are 0.7 and 5.4 — those animal populations effectively failed. They read near the
cap-0 null because almost nothing is grazing, not because light grazing is uninteresting. Do not
cite them as evidence about weak grazing.

### Why significance dies above cap 24

Both terms move the wrong way at once: the effect shrinks (-0.096 at 12 to -0.055 at 48) *and* the
spread grows (SD 0.161 to 0.208). Whatever the mechanism, it is not the uniform-grazing story tested
above.

## Consequence: the operating point, and a tension

**Cap 12 is the setting for any plant-defense measurement.** Strongest effect, lowest SD of the
grazed arms, survives correction, healthy population and 15.5 plant generations.

But the coevolution experiment measures the **consumer** response (`FoodEfficiency`) as well, and
twelve animals is a drift-dominated population on that side. **The two halves of a coevolution
measurement want opposite population sizes**: plant-defense detection peaks at cap 12, consumer-trait
detection wants the largest population available.

This must be resolved before the coevolution run rather than discovered in its results. Options:

1. **Two runs at different caps**, each powered for one side. Honest, doubles the compute, and the
   two halves are then not strictly the same experiment.
2. **Raise plant population instead of lowering animals** — many more patches at cap 48 would cut
   plant-side drift without starving the consumer side. This is the same "more patches" route
   identified in `defense-drift-report.html` and is the only option that serves both.
3. **Accept cap 12 and treat the consumer side as underpowered**, reporting it as such.

Option 2 is the only one that improves both sides, and it is a scenario redesign rather than a
parameter choice.

Raw per-seed data: `dose-curve.csv`.

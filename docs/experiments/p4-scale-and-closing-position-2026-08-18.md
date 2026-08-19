# P4 — Scaling Does Not Buy Power; Closing Position on Plant Defense — 2026-08-18

Final entry of 2026-08-18. Follows `p4-patch-count-2026-08-18.md`.

## Hypothesis 3, refuted: fixed grazers-per-patch does not preserve the effect

Within the grid geometry at cap 48, effect strength peaked near **one grazer per live patch**
(t -1.59 at 2.11/patch, -2.30 at 1.05, -1.23 at 0.52). If that ratio sets effect size, holding it
while scaling both populations should preserve the effect and shrink SD, improving t monotonically.

Ratio held at 1.04 across all three arms, seeds 42-71:

| sites / cap | live patches | final pop | grazers/patch | mean Δ | SD | t | 95% CI |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 12 / 48 | 45.9 | 48.0 | 1.04 | -0.0511 | 0.1219 | -2.30 | [-0.0924, -0.0089] |
| 18 / 72 | 68.9 | 72.0 | 1.04 | -0.0596 | 0.1402 | -2.33 | [-0.1093, -0.0071] |
| **24 / 96** | 92.6 | 96.0 | 1.04 | **+0.0158** | 0.1112 | **+0.78** | [-0.0218, +0.0545] |

The effect holds at two scales and then **vanishes and flips sign** at the third. SD does not fall
monotonically either (0.122, 0.140, 0.111). **Withdrawn.** Scaling at fixed ratio does not buy power.

## Three refuted hypotheses, and what that means

| # | hypothesis | prediction | outcome |
| --- | --- | --- | --- |
| 1 | Heavy grazing is uniform across patches, shrinking the differential | dispersion falls with density | **rises** 0.120 to 0.236 |
| 2 | Realized grazing pressure sets effect size | both sweeps collapse onto one curve | three arms at ~0.00103 give t of -3.25, -0.52, -1.23 |
| 3 | Grazers-per-patch sets effect size | effect preserved while SD falls | effect vanishes and flips at the largest scale |

No configuration variable tested predicts where the effect appears. Hypothesis generation is stopped
here deliberately: a fourth story fitted to the same data would be unfalsified rather than supported.

## What is actually established

Stated plainly, separating what the data support from what they do not:

**Supported.**
- Plant defense declines **only when grazers are present**. The cap-0 control is flat
  (-0.0015, t -0.05) and pre-specified, and this is the causal claim the blocker required.
- The mechanism is coherent and independently measured: `ConsumeAt` ignores defense, so defense only
  lowers nutrition density, so grazers eat **more** biomass from defended patches (2.2x from defense
  0.0 to 0.9). Defense makes a plant get eaten harder.
- More patches reduces plant-side drift (SD -22%, 23 to 46 live patches) without costing the
  consumer side.

**Not supported.**
- Any account of *how strong* the effect should be at a given configuration.
- Any configuration that yields a decisively powered measurement. The largest |t| observed anywhere
  is **-3.25** (cap 12, 6 hand-placed sites), which is the only arm surviving Bonferroni correction
  in its own sweep.

## Closing position for P4

The blocker from `p4-lifespan-derived-2026-08-17.md` is **resolved**: the setup can detect selection
on plant defense, so a coevolution null is now interpretable. That was the gate.

P4's *exit gate* — a repeatable reciprocal plant/consumer response — is a different matter and is
**not** met. Defense has no route to rise under the current consumption model: it is costly to grow,
protects no tissue, and provokes compensatory feeding. Every mechanism tried (deterrence, patch
quality preference, raised pressure) leaves that unchanged. A reciprocal response needs defense to
have a positive fitness route that outweighs its growth cost, and building one is a **design
decision**, not a calibration.

The remaining route to a sharper number is brute force: at SD ~0.12-0.21 and an effect near -0.05,
roughly 230 seeds gives 80% power, about eight times the compute spent so far. That is a cost
decision rather than a research question, and it should be taken deliberately.

Raw per-seed data: `scale.csv`.

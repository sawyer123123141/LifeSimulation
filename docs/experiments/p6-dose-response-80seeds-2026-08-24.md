# The scarcity dose-response replicates at 80 seeds — and n = 30 was hiding half the selection

**2026-08-24. 80 paired seeds per level, 12,000 ticks, population cap 100, terrain join on.**
`tools/CreatureSweep --focused 80 100 --scenario={moderate,lean,scarce}`, baseline (slope-off) arm.
Corpora: `p6-dose-response-{moderate,lean,scarce}-80seeds-2026-08-24.{csv,txt}`.

The 30-seed run in `p6-body-size-shrinks-under-scarcity-2026-08-24.md` found body size shrinking
harder as resources fell, and closed by saying only the lean level had been replicated. This is the
whole ladder at 80.

## Body size: the ordering holds

| level | founder | drift | t | surviving | control t | 30-seed drift |
|---|---|---|---|---|---|---|
| moderate (1.0x) | 0.5010 | **−0.0133** | **−2.01** | 79 / 80 | +0.17 | −0.0160 |
| lean (0.6x) | 0.5006 | **−0.0252** | **−3.23** | 55 / 80 | +0.07 | −0.0394 |
| scarce (0.35x) | 0.5080 | **−0.0697** | **−2.73** | 12 / 80 | −1.13 | −0.0769 |

**Monotonic, and in the same order as the smaller run.** Magnitudes are 15–35% smaller than at 30
seeds for the two milder levels and essentially unchanged for the harshest. The lean figure
reproduces the recorded value to four decimals — the same seeds and the same code produce the same
number, which is the least a corpus should do and worth confirming rather than assuming.

**One correction to the earlier writeup.** It said that at full resources the effect "is not
distinguishable from the control". At 80 seeds it is: **t = −2.01 against a control at +0.17.** The
effect at moderate was real and n = 30 could not see it.

## Whether the dose itself is significant — and why not to lean on it

Moderate against scarce is a difference of 0.0564 with a combined standard error of 0.0263, so
**z ≈ 2.1**. Adjacent levels are not separable (moderate against lean, z ≈ 1.2).

**This comparison is exactly the one the earlier doc warned is unsound**, and nothing here fixes it.
Drift is computed over survivors; scarcity is what causes the deaths; 79, 55 and 12 worlds survived
the three levels. The samples being compared were selected by the treatment. The z is reported
because suppressing it would be worse, and it should be read as "consistent with a dose" rather than
as a test that passed.

## The bigger finding: n = 30 was under-powered for most of the table

`p6-selection-is-happening-2026-08-24.md` (40 seeds) named three traits under selection and put the
other ten in "no detectable selection here". At 80 seeds the list is at least **six**, consistent
across levels:

| gene | moderate | lean | scarce |
|---|---|---|---|
| temperature_tolerance | +0.2879 (26.0) | +0.2999 (24.3) | +0.3552 (16.5) |
| lifespan_tendency | +0.2752 (17.4) | +0.2274 (12.5) | +0.0536 (1.21) |
| fertility_investment | +0.0593 (3.89) | +0.0597 (3.34) | +0.0966 (2.02) |
| urgency_exponent | −0.0353 (−14.6) | −0.0474 (−13.3) | −0.0474 (−3.51) |
| movement_speed | +0.0279 (3.93) | +0.0303 (3.01) | +0.0243 (0.58) |
| body_size | −0.0133 (−2.01) | −0.0252 (−3.23) | −0.0697 (−2.73) |
| **neutral_marker (control)** | **+0.0005 (0.17)** | **+0.0002 (0.07)** | **−0.0103 (−1.13)** |

`metabolic_pace` (−0.0252, t = −2.99) and `vision_range` (+0.0228, t = 2.69) also cross at lean but
not at the other two levels, so they stay in the undecided column.

**"No detectable selection" was a statement about the sample size, not about the traits.** That is
the correction this run forces on the earlier writeup, and it is the reason the phrasing there was
deliberately "not here" rather than "inert".

## Two patterns worth naming

**`lifespan_tendency` collapses under scarcity** — +0.275 at moderate, +0.227 at lean, +0.054 and not
significant at scarce. Living longer is worth selecting for when there is something to live on.

**`fertility_investment` moves the other way**, strongest at the harshest level (+0.097). Fewer,
better-provisioned offspring when resources are thin is the textbook direction, and it is the one
trait whose effect *grows* as the world gets worse.

Neither is claimed as established. Both are consistent across three levels with a control that stays
under |t| = 1.13.

## Independent confirmation of the thermal ceiling

`p6-why-temperature-tolerance-2026-08-24.md` argues the gene saturates because the field deviates by
at most 8 degrees and tolerance is `2 + 8*gene`, so 0.75 covers the world. If that is right, the
**endpoint should not depend on the ecology at all**. Founder plus drift:

| level | founder | drift | **endpoint** |
|---|---|---|---|
| moderate | 0.4794 | +0.2879 | **0.767** |
| lean | 0.4629 | +0.2999 | **0.763** |
| scarce | 0.4275 | +0.3552 | **0.783** |

**Three resource levels spanning a factor of three, survival from 79 worlds down to 12, and the
endpoint moves by 0.02.** The drift differs because the founders differ; the destination does not.
That is what a ceiling set by the field rather than by the ecology looks like, and it was not the
measurement the saturation argument was built on.

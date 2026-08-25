# Temperature comes from the world now, and it makes the pressure differ between worlds instead of within one

**2026-08-24. 40 seeds per arm, 12,000 ticks, population cap 100, 0 extinct in either arm.**
`tools/CreatureSweep --thermal 40 100 [--terrain-temperature]`.
Corpora: `p6-thermal-terrain-40seeds-2026-08-24.txt`, `p6-thermal-plateau-40seeds-2026-08-24.txt`.

`p6-why-temperature-tolerance-2026-08-24.md` found the model's strongest selection pressure was a
creature adapting to `20 + 8*sin(0.18x + 0.11y)` — a decorative sine with no latitude, no altitude,
no seasons and no terrain, while every other environmental quantity came from the world. This is the
flag that fixes it, `terrainDrivenTemperatureEnabled`, and the first measurement of what it changes.

## What was built

`ClimateField` is where a creature's temperature in degrees comes from. A `default` instance **is**
the placeholder sine, which is what makes the flag-off path byte-identical without a branch at every
call site; the terrain instance carries the world's own `EnvironmentField` — latitude band, climate
noise, a local band across the arena, and a lapse rate that costs warmth with height — mapped onto
degrees.

**The degree span is held at 12 to 28 deliberately.** Tolerance is `2 + 8*gene`, so an 8-degree
half-span is what puts the saturation ceiling at gene 0.75. Holding it fixed means the flag changes
the field's *spatial structure* and nothing else, and a difference in the equilibrium is attributable
to that alone.

It is not an ambient static: the field belongs to the world, so two worlds with equal configuration
hashes cannot disagree about the climate (handoff decision 13).

## The prediction, and the half of it that was wrong

**Predicted:** the terrain field is nearly uniform over a 50-unit arena — the join measured terrain
moisture at a standard deviation of .005 against the procedural field's .283 — so realised deviations
should be much smaller than the sine's and the equilibrium much lower than 0.75.

**Wrong on the deviations.** They barely moved:

| | sine | terrain |
|---|---|---|
| realised mean \|T − 20\| | 4.277 | 3.922 |
| p90 | 7.644 | 7.680 |
| max | 8.000 | 8.000 |

The full span is reached in both. What changed was not how much of the range creatures see; it was
**which worlds see it**.

## The finding: the spread between worlds doubles

| | sine | **terrain** |
|---|---|---|
| endpoint, mean of 40 worlds | 0.7790 | **0.6713** |
| **standard deviation across worlds** | **0.0744** | **0.1454** |
| min / median / max | 0.634 / 0.765 / 0.980 | **0.339** / 0.669 / 0.921 |
| still rising at 12,000 ticks | no, +0.002 per 1,000 | **yes, +0.006 per 1,000** |
| control endpoint | 0.5061 | 0.5059 |

**Variance ratio 3.8 on 39 and 39 degrees of freedom** — far past the 2.1 that p = .01 asks for.

The sine gives every arena the same climate, because a 50-unit window spans several full periods of
it: every world contains the hot bands and the cold ones, so every world applies the same pressure
and every world lands in the same place. Terrain gives one arena a temperate continent and another a
cold one. **A world at 0.339 ended below its founders** — the first time the gene's maintenance cost
has been visible at all, because it is the only condition where a world exists with nothing to adapt
to.

That also explains the lower mean and the slower climb: averaged over worlds, most of them are under
weaker pressure than the sine applied universally, and 12,000 ticks is no longer long enough to
finish.

## What this does and does not say

**Does:** the flag is live, the flag off is byte-identical, and turning it on converts a uniform
pressure into one that varies by a factor a control gene does not touch (control endpoints 0.5061 and
0.5059).

**Does not:** on its own settle whether the flag is safe. That needed three conditions, and they are
below.

**Nor is the ceiling gone.** A world with a harsh continent still saturates at 0.75 for the same
arithmetic reason; what varies is how many worlds have a reason to get there.

## Three conditions, 80 seeds each — the safety question

Same discipline the slope cost got: measured at three resource levels before being switched on
anywhere. `--focused 80 100 --terrain-temperature [--scenario=lean|scarce]`, baseline arm, against
the identical seeds without the flag (`p6-dose-response-*-80seeds-2026-08-24.txt`).

| level | extinct, sine | extinct, terrain | surviving, sine | surviving, terrain |
|---|---|---|---|---|
| moderate | 1 / 80 | **0 / 80** | 79 | **80** |
| lean | 25 / 80 | 33 / 80 | 55 | 47 |
| scarce | 68 / 80 | 72 / 80 | 12 | 8 |
| **total** | **94 / 240** | **105 / 240** | | |

**No detected survival cost: z = 1.02 on the pooled counts.** Better at moderate, worse at both
scarcity levels, and the largest single gap — lean, 33 against 25 — is z = 1.32 on its own.
**Direction is consistently worse under scarcity and the test is underpowered to resolve it**, which
is the same honest position the slope cost ended in. It is not evidence of safety under scarcity; it
is an absence of evidence of harm.

## What it does to selection

| gene | moderate | lean | scarce (n = 8) |
|---|---|---|---|
| temperature_tolerance, sine | +0.2879 (26.0) | +0.2999 (24.3) | +0.3552 (16.5) |
| **temperature_tolerance, terrain** | **+0.1251 (9.04)** | **+0.1954 (10.0)** | +0.2046 (3.80) |
| lifespan_tendency, sine | +0.2752 (17.4) | +0.2274 (12.5) | +0.0536 (1.21) |
| **lifespan_tendency, terrain** | **+0.2956 (22.8)** | **+0.2575 (12.5)** | +0.1350 (1.88) |
| control, terrain | +0.0006 (0.37) | −0.0079 (−1.91) | −0.0122 (−0.51) |

**Selection on temperature tolerance roughly halves at every level**, and **lifespan tendency
strengthens at every level** — overtaking it to become the strongest trait in the model at moderate
and lean. That reordering is the substance of this change: the model's dominant adaptation is no
longer a response to a placeholder.

**The lean control is noisy at t = −1.91**, so the noise floor there is much higher than at moderate
and columns near |t| = 2 in that condition mean little. `metabolic_pace` is the one candidate worth
another look — it lands at −2.88 with the flag and −2.99 without it, so it is not caused by this
change.

## Switched on for the `Y` playtest, default still false

Same call as the slope cost, for the same reason: `Y` is the scenario whose purpose is that terrain
means something, and a creature that ignores altitude and latitude contradicts it. The configuration
default stays `false`, and **every recorded thermal result on this project was measured without it**.

## Consequence for `docs/creature-appearance.md`

The hue channel carries temperature tolerance, and that doc says every population ends the same
colour because the gene plateaus. **With this flag on, that stops being true** — endpoints spread
from 0.34 to 0.92, so two worlds would visibly differ. That makes the channel better, not worse, and
the doc's advice to tint per creature rather than by population mean holds either way.

# Natural selection is happening, on three traits out of thirteen

**2026-08-24. 40 runs, 12,000 ticks, population cap 100, terrain join on.**
`tools/CreatureSweep --focused 40 100`, baseline (slope-off) arm.

## The question the paired sweeps could not answer

Every creature measurement before this compared **arm against arm** — flag on against flag off. That
is a difference of differences, and it is blind to selection by construction: a trait under strong
selection in *both* arms cancels exactly. "The flag moved nothing" and "nothing is happening" produce
the same table.

This asks the other question. Over one run, how far did the population move from its founders, and
did it move further than `NeutralMarker` — a gene inherited and mutated exactly like the others that
affects nothing whatsoever?

## The answer

| gene | founder | drift | t | vs control |
|---|---|---|---|---|
| **temperature_tolerance** | 0.480 | **+0.277** | **11.03** | **29×** |
| **lifespan_tendency** | 0.517 | **+0.257** | **7.90** | **27×** |
| **urgency_exponent** | 0.500 | **−0.052** | **−4.34** | **5.4×** |
| fertility_investment | 0.512 | +0.052 | 1.88 | 5.5× |
| body_size | 0.501 | −0.023 | −1.46 | 2.4× |
| metabolic_pace | 0.509 | −0.017 | −1.16 | 1.8× |
| risk_aversion | 0.500 | −0.017 | −1.32 | 1.8× |
| travel_sensitivity | 0.500 | −0.015 | −1.16 | 1.6× |
| movement_speed | 0.500 | +0.014 | 0.80 | 1.5× |
| water_efficiency | 0.495 | −0.012 | −0.80 | 1.3× |
| vision_range | 0.490 | −0.007 | −0.53 | 0.8× |
| food_efficiency | 0.507 | +0.004 | 0.26 | 0.4× |
| **neutral_marker (control)** | 0.500 | −0.010 | −0.72 | 1.0× |

**Temperature tolerance and lifespan tendency move by more than a quarter of the trait range, at
t = 11 and t = 7.9, against a control that does not move at all.** That is directional selection, not
drift.

## Why the founder column decides it

A bounded gene that starts away from the middle moves toward the middle under symmetric mutation
alone. Regression to the centre and selection look identical unless you know where each gene started
— and this project has retracted a selection claim before, in
`p4-defense-selection-demonstrated-2026-08-18.md`, for comparing against a baseline whose spread was
small by construction.

**Every gene here starts at 0.50.** Founders range from 0.480 to 0.517. There is no room for
regression to the centre to explain a +0.277 shift away from it, and the control starting at exactly
0.500 and staying there confirms the machinery is not pushing anything on its own.

## A false start worth recording

The first run of this measurement reported all thirteen genes drifting +0.49 at t ≈ 30, control
included. Statistics are rebuilt every `BaseFrequencyHz / StatisticsHz` ticks rather than every tick,
so reading them after a single step returns a default-valued struct with every gene at zero — and the
"drift" was the population mean itself. **A uniform result across every column including the control
is a broken measurement, not a finding**; it was the control that made it obvious within seconds.

## What this does and does not say

**Does:** in this scenario, at this population, over 12,000 ticks, selection acts measurably on
thermal tolerance, lifespan and urgency.

**Does not:** say anything about the other ten traits, which sit inside the control's range — that is
"no detectable selection here", not "these traits are inert". It is one scenario
(`ConsumerDefenseCalibrationModerate`) with the terrain join on, 40 seeds, and no claim is made about
other configurations. Nor does it explain *why* these three: temperature tolerance rising alongside
the terrain join, which is what introduced a real temperature field, is a hypothesis this run does
not test.

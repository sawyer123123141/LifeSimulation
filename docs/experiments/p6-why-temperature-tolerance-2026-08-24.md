# Why temperature tolerance: a saturating gene against a fixed sine, and the terrain join has nothing to do with it

**2026-08-24. 20 seeds per arm, 12,000 ticks, population cap 100, 0 extinct in either arm.**
`tools/CreatureSweep --thermal 20 100 [--join=off] [--scenario=scarce]`.

Temperature tolerance is the strongest selection in the model by a distance — +0.2999 at t = 24.3
against a control at t = 0.07, **1664 times the control** (`p6-body-size-shrinks-under-scarcity`).
Nothing explained it. This does.

## The handoff's hypothesis was wrong, and the source says so before any run

The proposed test was **the terrain join on against off**, on the reasoning that the join is what
introduced a real temperature field. It does not reach this gene:

| path | reads |
|---|---|
| decision — `ThermoregulationSystem.cs:29,39,58` | `TemperatureField.Sample` |
| health — `SimulationWorld.Ticking.cs:151` | `TemperatureField.Sample` |
| what the join builds — `SimulationWorld.cs:62` | `EnvironmentField`, which feeds **plants** |

`TemperatureField.Sample` is `20 + 8*sin(0.18x + 0.11y)` — a **fixed spatial sine with no terrain
input and no tick term at all**. The join changes moisture, temperature and elevation for the plant
layer; creature thermoregulation never asks it anything.

## The mechanism: benefit with a hard ceiling, cost without one

- tolerance in degrees is `2 + 8*gene` (`GenomePhenotype.cs:422`)
- stress is `max(0, |T - 20| - tolerance)`, costing health at `0.35/s` (`NeedsSystem.cs:84`) and
  hijacking the decision at `discomfort/8 >= 0.15` (`ThermoregulationSystem.cs:32`)
- the field deviates by **at most 8 degrees**, so **`gene = 0.75` covers the entire world** and every
  point above it buys precisely nothing
- the price is `0.06*gene` in the maintenance multiplier (`GenomePhenotype.cs:396`) against a
  midpoint total of ≈1.54 — about **1% of upkeep** for the 0.27 of gene at issue

Enormous benefit, negligible price, hard ceiling. **The prediction, written before the run:** the
mean climbs steeply and then flattens near 0.75, rather than climbing for the whole run — and it does
so with the join off just as it does with it on.

## The measurement

Mean gene across surviving worlds, at twelve checkpoints:

| tick | join on | join off |
|---|---|---|
| 0 | 0.5066 | 0.5066 |
| 2000 | 0.5116 | 0.4993 |
| 4000 | 0.6379 | 0.6275 |
| 6000 | 0.7544 | 0.7297 |
| 8000 | 0.7876 | 0.7420 |
| 10000 | 0.7896 | 0.7423 |
| **12000** | **0.7916** | **0.7475** |
| control at 12000 | 0.5091 | 0.5090 |

**It plateaus.** With the join on the mean moves 0.281 over the first 8,000 ticks and **0.004 over
the last 4,000**. The control moves +0.009 across the whole run in both arms.

And the realised deviations say where the ceiling is. Sampling `|T - 20|` at every living creature's
position at every checkpoint — 19,565 samples with the join on, 19,479 with it off:

| | join on | join off |
|---|---|---|
| mean | 4.267 | 4.290 |
| p90 | 7.640 | 7.667 |
| max | **8.000** | **8.000** |
| **gene that covers the max** | **0.750** | **0.750** |

**Predicted 0.750, observed plateau 0.7475 with the join off.** The join-on arm settles 0.04 above
the ceiling, which is what an asymmetric landscape does: below 0.75 a creature loses health, above it
loses about 1% of upkeep, so selection trims the low tail harder than the high one and the mean
comes to rest above the point where the benefit ran out.

## Replicated at 40 seeds

`--thermal 40 100`, join on, 0 extinct. Raw output in
`p6-thermal-plateau-40seeds-2026-08-24.txt`.

| tick | 20 seeds | **40 seeds** |
|---|---|---|
| 0 | 0.5066 | 0.5059 |
| 6000 | 0.7544 | 0.7355 |
| 9000 | 0.7904 | 0.7744 |
| **12000** | **0.7916** | **0.7790** |
| control at 12000 | 0.5091 | 0.5061 |

Same shape, same plateau, and the larger run sits closer to the predicted 0.750 — 0.7790 against
0.7916. The realised maximum is 8.000 over 39,735 samples, giving the same covering gene of 0.750.

## What the join arm actually shows

Turning off the thing that supposedly created the temperature field removes **0.044 of 0.285** —
15% of the drift, and none of the shape. The residual is second-order and has an obvious route:
the join moves the plants, the plants move the creatures, and where a creature has to go decides how
much of the sine it is exposed to. **The join is not the cause; it is a small perturbation on where
animals stand.**

## Scarcity: underpowered, reported for completeness

`--scenario=scarce` killed 17 of 20 worlds. The three survivors plateau higher (0.87 at 12,000) and
their realised deviations are hotter (mean 4.95 against 4.27) — fewer resources means longer trips
across uncomfortable ground. **n = 3 is not a result** and this is recorded so it is not re-run in the
belief that it is one. The realised maximum is 8.000 exactly as in every other condition, because
that is field geometry and no ecology can change it.

## What this does not close

**The amplitude test has not been run.** The decisive confirmation is to change the sine's amplitude
and watch the plateau move to `(amplitude - 2) / 8`. It was not done because the amplitude is a
literal inside a static `TemperatureField`, and making it a world property means putting it in
`SimulationConfig`, hashing it, bumping `ConfigurationHashVersion`, and threading it through
`ThermoregulationSystem` and the decision path — production surgery for a confirmation of something
the formula already fixes algebraically. It is worth doing when temperature becomes a real climate
variable rather than a placeholder, and not before.

**This is a placeholder field, and that is the finding underneath the finding.** The strongest
selection pressure in the simulation is a creature adapting to a decorative sine wave that has no
seasons, no altitude, no latitude and no connection to the world the terrain work built. The gene is
behaving correctly. The environment it is adapting to is not an environment.

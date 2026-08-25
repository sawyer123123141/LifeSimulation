# Creatures get smaller when there is less to eat, and the effect scales with scarcity

> **Superseded in part by `p6-dose-response-80seeds-2026-08-24.md` (80 seeds per level).** The
> ordering and direction replicate. **One claim below is wrong at the larger n:** this doc says the
> moderate level is not distinguishable from the control, and at 80 seeds it is - t = −2.01 against a
> control at +0.17. Prefer the 80-seed magnitudes.

**2026-08-24. 30 paired seeds per level, 12,000 ticks, population cap 100, terrain join on.**
`tools/CreatureSweep --focused 30 100 --scenario={moderate,lean,scarce}`, baseline (slope-off) arm.

## The mechanism, before the measurement

`bodyMass = 0.6 * 4^BodySize` (`GenomePhenotype.cs:380`) — a **fourfold mass range** across the gene.
Mass is charged twice:

- energy: `movementDistance * BodyMass * 0.5` (`NeedsSystem.cs:47`)
- water: `BodyMass * DigestionRate * WaterLossMultiplier * 0.75 * dt` (`NeedsSystem.cs:48`)

**Nothing pays a creature for being large.** The only thing size buys is a bigger carcass —
`10f * BodyMass` (`SimulationWorld.Ticking.cs:104`) — which feeds whoever eats it. So the pressure is
downward and should bite hardest where there is least to eat and drink. That is a prediction, and it
is testable.

## The result

Drift from founders in the baseline arm, extinct runs excluded, against the `NeutralMarker` control:

| resource level | body_size drift | t | vs control | control t | extinct |
|---|---|---|---|---|---|
| moderate (1.0x) | −0.0160 | −1.20 | 2.6x | +1.22 | 1 / 30 |
| **lean (0.6x)** | **−0.0394** | **−2.19** | **21.8x** | +0.36 | 13 / 30 |
| **scarce (0.35x)** | **−0.0769** | **−2.34** | **20.9x** | −0.23 | 24 / 30 |

**Monotonic in the right direction.** Cut the food and water and the shrinking roughly doubles, then
doubles again. At full resources the effect is not distinguishable from the control; scarcity is what
makes mass expensive enough to matter.

The control is quiet at every level (|t| ≤ 1.22), which is what makes the table readable at all.

## Other traits, same runs

`temperature_tolerance` rises hard in every condition (+0.309, +0.319, +0.332 at t = 14.4, 12.0,
10.1) and is by far the strongest selection in the model — see
`p6-selection-is-happening-2026-08-24.md`. `urgency_exponent` falls consistently (−0.042 to −0.048).
`lifespan_tendency` rises at moderate (+0.255, t = 7.87) and weakens as resources fall (+0.187,
+0.085). Everything else stays inside the control's range.

## Two artefacts found and removed on the way

**Swapping scenario families is not a scarcity arm.** The first attempt used
`ObservationStable` against `ConsumerDefenseCalibrationModerate`. Every one of 30 runs went extinct in
both arms — those layouts are calibrated against different founder counts and flags. The fix is
`SimulationScenario.Scaled(id, factor)`, which multiplies every amount, capacity and regeneration rate
of an existing layout, so scarcity differs from abundance in exactly one thing.

**Extinct runs must be excluded from a drift statistic.** A dead world reports every gene mean as
zero, so its drift is minus the founder value on all thirteen columns at once. With them included,
the lean arm showed every column down ~0.21 — **control included, ratios all ≈ 1.0** — which is the
signature of a broken measurement rather than a finding.

## What this does not establish

**Survivor conditioning.** Drift is computed over surviving runs, and scarcity is what causes the
deaths, so the survivors are a selected sample. This is sound for "did the survivors change" and
unsound for comparing drift magnitudes *between* levels, which is why the extinction counts sit beside
every row. At 0.35x only 6 of 30 worlds survived and that row should be read as direction, not size.

## Replicated

The lean arm was re-run at **80 seeds, 55 surviving**:

| run | body_size drift | t | control drift | control t |
|---|---|---|---|---|
| lean, 30 seeds (17 surviving) | −0.0394 | −2.19 | +0.0018 | +0.36 |
| **lean, 80 seeds (55 surviving)** | **−0.0252** | **−3.23** | **+0.0002** | **+0.07** |

Direction and significance hold, and the control is essentially exactly zero at the larger n. The
magnitude is smaller in the larger run — −0.025 against −0.039 — which is what a modest effect
measured twice on different seed sets does, and it is the reason the single-run figures should be
read as direction and rough size rather than as a coefficient.

`temperature_tolerance` at 80 seeds is +0.2999, t = 24.3, against a control at t = 0.07: **1664
times the control**. That remains the largest single fact about selection in this model.

# The mating gate stops being a selection knob and becomes a survival mechanism once the population is ecology-limited

**2026-08-26.** `tools/CreatureSweep --focused 40 <cap> [--regen] [--brake] [--gate] [--health-recovery]`,
12,000 ticks. Nine cells. Console artefact: `p6-selection-in-the-pressured-cell-2026-08-26.txt`; nine
per-configuration CSVs alongside it (the filename fix from `ed0f54c` is what keeps them distinct
rather than nine overwrites of one path).

Asks the payoff question of the whole thread: **does the pressured cell from
`p6-the-pressured-cell-is-a-plateau-2026-08-26.md` open selective channels the default configuration
lacks?**

## A claim of mine that the dose-response killed

The two-point comparison looked clean. `fertility_investment` drift, default cap-100 cell against the
pressured cell: **t 3.44 → 9.48**, mean 0.0709 → 0.1497. "Mortality pressure opens the fertility
channel" writes itself.

Swept on the brake at fixed cap 500 and regen 2.0, with the starvation share of each cell already
measured:

| brake | starvation (from the death mix) | `fertility_investment` t |
|---:|---:|---:|
| 1.0 | 33.6% | 4.69 |
| 1.5 | 16.2% | **9.48** |
| 3.0 | 0.0% | 4.83 |

**Non-monotone, and it does not track starvation at all.** The cell with *no* starvation and the cell
with the *most* starvation give nearly the same figure. The elevation over the default cell is real
and is a property of **the configuration** — cap 500 with 2.0x regeneration, i.e. a population near
300 instead of 98 — **not of mortality pressure**. It also moves with the gate (3.92 at 0.55, 6.94 at
0.65), which no story about starvation predicts.

**Withdrawn before publication.** This is the project's recorded failure shape — a two-point
comparison manufacturing a channel — caught this time by running the third point first.

## What the health-recovery arm says about the sterility confound

Same cell, `--health-recovery` on, which removes the one-way health ratchet:

| gene | default (ratchet on) | health recovery | change |
|---|---:|---:|---|
| `fertility_investment` | 9.48 | **5.77** | -39% |
| `body_size` | -2.53 | -0.88 | gone |
| `urgency_exponent` | -14.19 | -12.76 | -10% |
| `neutral_marker` (control) | 0.60 | 1.87 | control noisier |

**Roughly two-fifths of the fertility signal in that cell is the sterility channel**, not fertility
selection: with health irrecoverable, who is *able* to breed is itself health-selected. `body_size`
crosses |t| = 2 with the ratchet and does not without it — it would have been reported as a finding.

**Any selection claim in these cells is inflated unless the flag is on.** That is the confound named
before the measurement, now with a number on it.

## The finding: the gate is load-bearing for survival, and only when the ecology limits the population

Survival across the mate-seeking gate, everything else fixed:

| gate | pressured cell (cap 500, regen 2.0, brake 1.5) | default cell (cap 100) |
|---:|---:|---:|
| 0.45 | **4 / 40** | **40 / 40** |
| 0.55 | 11 / 40 | — |
| 0.65 | 24 / 40 | — |
| 0.70 (default) | 38 / 40 | 39 / 40 |

**Monotone, and the same slack gate that costs nothing at cap 100 kills nine worlds in ten at cap
500.** Slackening the gate lets creatures breed while depleted; at cap 100 the cap absorbs the
overshoot, and with the population ecology-limited nothing does.

**So the gate is not only the dominant selection channel — it is the model's density brake, and the
recorded dose-response could not see that** because every one of its five points was measured at cap
100, where the cap was doing that job. `p6-gate-dose-response-2026-08-24.md` reports 80 of 80
surviving at every gate value; **that survival is a property of the cap, not of the gate.**

This is the same shape as `p6-the-cap-is-the-stabiliser-2026-08-24.md`: a mechanism looks harmless
while a cap is quietly supplying the regulation it would otherwise have to supply itself.

## The urgency dose-response replicates in the new cell

`urgency_exponent` drift against the gate, pressured cell: **-1.48 / -3.91 / -14.19** at gate
0.55 / 0.65 / 0.70. Same monotone accelerating shape as the recorded curve at cap 100
(-1.02 / -7.13 / -14.55 at 0.55 / 0.65 / 0.70). **A recorded result reproducing in a configuration it
was not measured in**, which is worth more than the third significant figure of either.

## Scope and cautions

- **The low-gate cells are conditioned on very few survivors** — 4 and 11 of 40. Use their *survival
  counts*, which is the finding; **their trait tables are not interpretable** and the control sits at
  |t| 1.9 in the 4-survivor cell, which is what that looks like from inside.
- 40 seeds, one scenario family, one build. The gate curve is four points in the pressured cell and
  **one** in the default cell — enough to show the contrast at 0.45, not to shape the default cell's
  curve.
- **Nothing here is a new behaviour**, and nothing here needed one: every result is a configuration
  moving an existing mechanism.

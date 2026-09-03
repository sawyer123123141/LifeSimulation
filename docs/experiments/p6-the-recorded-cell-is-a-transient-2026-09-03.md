# The cap-500 / brake-1.0 cell is a slow collapse, not a regulated ecology

**Date:** 2026-09-03
**Status:** an incidental finding from the Task 9 digestion re-adjudication, recorded separately
because it bears on far more than digestion. **No biological code changed.** Nothing was tuned to
produce it: the only thing that differed from the recorded configuration was **run length**.
**Raw output:** `p3-re-adjudicated-36000-health{off,on}-2026-09-03.txt`, beside this file.

## The finding

The cell recorded on 2026-08-30 as the project's strongest available comparison — cap 500,
`--regen=2.0`, `--brake=1.0`, `--predation`, `--gate=0.45`, proximity pairing — **does not persist**.
Run it three times as long and almost every world is empty.

| arm | 12,000 ticks (recorded 2026-08-30) | 36,000 ticks (measured here) |
|---|---|---|
| health recovery OFF | 22 of 24 surviving | **3 of 24 surviving** (seeds 48, 50, 52) |
| health recovery ON | — | **1 of 24 surviving** (seed 50) |

Final populations at 36,000 ticks, health recovery OFF, 24 seeds:

```
42:0  43:0  44:0  45:0  46:0  47:0  48:173  49:0  50:202  51:0  52:410  53:0
54:0  55:0  56:0  57:0  58:0  59:0  60:0  61:0  62:0  63:0  64:0  65:0
```

and ON:

```
42:0  43:0  44:0  45:0  46:0  47:0  48:0  49:0  50:202  51:0  52:0  53:0
54:0  55:0  56:0  57:0  58:0  59:0  60:0  61:0  62:0  63:0  64:0  65:0
```

Both readings are correct. The population is alive and apparently healthy at 12,000 ticks and gone by
36,000. **The recorded cell is the first third of a collapse.**

## Why this matters beyond digestion

**No 12,000-tick measurement in this cell family can distinguish a steady state from a transient.**
That is the finding, and it is a statement about the instrument's time base, not about any one gene.

Everything measured in this cell — the digestion negative, the intake table, the drift-to-fixation
distribution, `defense` at t +12.71, the diet histogram and the per-run hunter share — was measured
inside a decline that nobody could see, because 12,000 ticks was the longest anything had been run.
A conclusion drawn there is not thereby wrong. It is a conclusion about a population on its way down,
and it should be labelled that way until someone shows the cell has a persistent regime.

The two things this does **not** say:

- It does not say the recorded conclusions are wrong. The Task 9 re-adjudication reproduced the
  digestion negative and reproduced `r +0.88` under exactly these conditions
  (`p3-digestion-re-adjudicated-2026-09-03.md`), which is what an instrument with real power looks
  like even in a declining population.
- It does not say the cell is misconfigured. Nothing here proposes a fix, and finding a length at
  which the population persists would mean moving brake, regeneration or cap — a biological change,
  which the measurement-validity milestone forbids and which would invalidate every baseline measured
  before it.

## How this connects to what was already known

The project has already recorded that **the cap was supplying the regulation**:
`p6-the-cap-is-the-stabiliser-2026-08-24.md` measured the same ecology surviving 23 of 24 runs at a
cap of 250 and 3 of 20 at a cap of 500, starvation going from 0.1% of deaths to 64%. Graded fertility
was added precisely because "births are gated by step functions, so there is no signal that resources
are *tightening*, only that they are gone" — the comment on `ReproductionSystem.CooldownMultiplierFor`
says so directly.

**This result says the brake at 1.0 did not finish the job.** It slowed the collapse enough to hide it
inside a 12,000-tick window; it did not convert the cell into a regulated ecology. The standing field
note — *"a cap hides every mechanism that would otherwise have to regulate the population"* — now has a
companion: **a brake can hide the absence of regulation for exactly as long as your run is short.**

The relevant lesson from 2026-08-30 was already on the books: *"distinguish 'not yet' from 'not ever'
by running longer, not by arguing."* It was written about a trait that had not moved. It applies just
as well to a population that had not yet died.

## What the run does and does not pin down

**Established:**

- 21 of 24 (health off) and 23 of 24 (health on) worlds are extinct at 36,000 ticks in a cell recorded
  as 22 of 24 surviving at 12,000.
- The collapse is not an artefact of the new instrumentation. Both arms end with 2,000 ticks run with
  the ingestion recorder attached and detached and report an identical state hash, and no simulation
  behaviour was changed in this milestone.
- It is not the population cap: the cap binds on 1.5% of reproduction ticks in both arms. The worlds
  are nowhere near their ceiling.
- Health recovery does not rescue it. It is very slightly *worse* with recovery on (23 of 24 extinct
  against 21 of 24) — the opposite of what a health ratchet would predict, and consistent with
  recovery costing energy in an already energy-limited world.
- Energy is the binding need in about three-quarters of blocked adult samples in both arms.

**Not established, and worth measuring before anyone acts on this:**

- **When** the collapse happens. No per-world extinction tick was recorded; the sweep reports the
  final population only. A `--deaths` run at 36,000 ticks would give the death-cause mix over time and
  is the obvious next measurement.
- Whether the trajectory is a smooth decline or a late crash. "Alive at 12,000, gone by 36,000" is
  consistent with both, and the two have different causes.
- Whether the same is true of the cap-48 cells, which have their own recorded results.
- Whether any brake value in this cell family produces a persistent regime, which is a tuning question
  and belongs to a separate, explicitly biological piece of work.

## What not to do next

- **Do not re-run existing comparisons at 36,000 ticks expecting them to hold.** Several were measured
  where the population still existed. Re-running them is a real experiment with a real cost, not a
  formality, and it needs its own record.
- **Do not tune the brake, cap or regeneration to make this go away** as part of a measurement task.
  That is a biological change; it invalidates baselines and needs to be decided as such.
- **Do not read this as invalidating the recorded results.** Read it as putting a time bound on them:
  the recorded numbers describe roughly the first 12,000 ticks of this cell, and until somebody
  measures the collapse curve, that is the whole of what they describe.

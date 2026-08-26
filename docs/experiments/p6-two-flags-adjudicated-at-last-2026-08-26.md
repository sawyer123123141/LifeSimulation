# `multiThreatPerception` and `kinRecognition` are live — adjudicated in the first world that could test them

**2026-08-26.** `tools/CreatureSweep --focused 30 500 --regen=2.0 --brake=1.5 --predation --gate=0.45
[--multithreat=off] [--kin=off] [--health-recovery]`, 12,000 ticks, 60 runs per cell (slope arms x 30
seeds). Six CSVs in `docs/experiments/`, one per configuration.

`p4-inert-flags-readjudicated-2026-08-19.md` left both flags **unadjudicated on purpose**: every use
site sits inside `if (predationEnabled)`, and no survivable predator-prey scenario existed, so both
verdicts on record were "measured on a corpse". `p6-a-survivable-predator-prey-scenario-exists-2026-08-26.md`
produced one. This is the measurement it unblocked.

## Result

Flag on against flag off, paired by seed, state hash compared exactly:

| flag | health arm | hashes differing | population, on → off |
|---|---|---:|---:|
| `multiThreatPerceptionEnabled` | ratchet | **60 / 60** | 150.1 → **159.2** |
| `multiThreatPerceptionEnabled` | health recovery | **60 / 60** | 156.1 → **203.9** |
| `kinRecognitionEnabled` | ratchet | **56 / 60** | 150.1 → **107.2** |
| `kinRecognitionEnabled` | health recovery | **56 / 60** | 156.1 → **129.5** |

**Both flags are live.** Not marginally: turning kin recognition off costs **29% of the population**,
and turning multi-threat perception off *raises* it by 6% to 31%.

**Both health arms agree in sign and rough magnitude**, which is the standing practice and matters
here because health is one of the three mate-seeking gate conditions and could plausibly have carried
the whole effect.

The four kin-recognition runs whose hash is unchanged are the expected shape: a flag that gates `IsKin`
inside a predation scorer cannot diverge a run in which no creature ever scores a relative.

## Determinism check

The same configuration run twice produced a **byte-identical CSV**, so the hash differences above are
the flags and not run-to-run variation. Worth spending a run on: the entire comparison rests on it.

## Directions, which are the interesting part

- **Kin recognition is load-bearing for survival.** Off, creatures stop sparing relatives, attack
  more, and the population falls by nearly a third. That is the flag doing exactly what it was
  written to do, measured for the first time.
- **Multi-threat perception costs population.** Off, creatures see one threat instead of many, and
  there are *more* of them at the end. Seeing every nearby threat makes a forager more avoidant —
  more fleeing, less eating. **Plausible and not established**; the measurement here is liveness, not
  mechanism.

## What changes, and what deliberately does not

- **The ledger entry changes.** Both flags have sat in the "unexercised, not unwired — do not delete"
  bucket since 2026-08-19 with the note that adjudicating them needs a scenario that does not exist.
  That scenario exists and they are adjudicated: **live when exercised**.
- **`LivenessTests` and `KnownInertFlags` are NOT changed.** The pinned set is scoped to the widest
  *available* configuration — full ecosystem, herbivore founders, no threats — and under that
  configuration both flags remain bit-inert, which is still true and still worth pinning. **A verdict
  measured in another scenario does not make the pinned one wrong**; it makes its scope explicit,
  which the field notes already say out loud. Changing the pinned set would be asserting something
  this experiment did not measure.
- **This is not a claim that predation selects.** Predation is still 1–2% of deaths in this cell. The
  flags reach behaviour; whether the behaviour they reach produces selection is a separate question
  and is not answered here.

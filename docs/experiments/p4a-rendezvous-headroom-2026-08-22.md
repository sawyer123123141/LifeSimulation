# Safety-gated rendezvous, re-measured where survival can actually move — 2026-08-22

**Verdict:** the gate is **live and does exactly what it was designed to do** — it cuts fleeing and
cuts predation deaths — but that benefit **does not propagate** to survival or to birth rate. Flag
stays default `false`. This is *not* the home-range case: the effect has the right sign, it just
buys nothing at the population level.

Raw per-seed data with a full provenance manifest: `p4a-rendezvous-headroom-2026-08-22.csv`.

## Why this was re-run at all

`p4-safety-gated-rendezvous-2026-08-21.md` concluded "no evidence of a birth or survival benefit"
and reported both arms at **48.0 final population, 0/120 extinct**.

Checking that CSV: **all 240 runs ended at exactly 48**, the population cap, with zero variance.
Survival could not have differed in either direction. The survival half of that null was
**structurally unmeasurable**, not measured. Its birth null (285.93 vs 286.77, t +0.93) had real
variance and still stands as a result *at that operating point*.

## The cap is not a safety limit — it is load-bearing ecology

Raising the cap and changing nothing else does not free the population to grow. It kills it.

| population cap | extinct | mean final population |
|---:|---:|---:|
| 60 | 0/8 | 59.9 |
| 72 | 0/8 | 57.0 |
| **84** | **5/8** | **1.3** |
| 96 | 8/8 | 0 |
| 120 | 8/8 | 0 |
| 160 | 8/8 | 0 |
| 200–800 | 3/3 | 0 (≈293 births, ≈305 deaths, 58–72 starvations) |

The cap suppresses growth, which prevents the overshoot that strips the habitat and starves the
population. Remove it and every run booms and collapses. **The prior experiment's "0/120 extinct,
48/48 survivors" was an artefact of the cap, not an ecological finding**, and any other conclusion
drawn from a cap-pinned arm deserves the same check.

Cap **84** was chosen because it is the only point where extinction is partial, so survival is free
to move in both directions.

## Predictions, stated before the run

| Prediction | Held? |
| --- | --- |
| Per-seed state hashes differ between arms (flag reaches behavior) | **Held** — 120/120 |
| Gate-off extinction 60–65% | **Held** — 62.5% (75/120) |
| Gate-on extinction within ±10 points of gate-off | **Held** — 55.0% (66/120), −7.5 points |
| Births within ±3% of gate-off | **Refuted** — +4.58% raw (t +2.04) |
| Overall: another null | **Refuted in part** — predation deaths fall, robustly |

## Design

120 paired seeds (42–161), 12,000 ticks, `WatchableStarterHabitat`, `PredationVariation` founders,
`IntentUtilityV1`, cognition + physiology + plant cohorts + mate selection on, 12 founders,
population cap 84. The only toggle is `SafetyGatedMateRendezvousEnabled`. Full field set is in the
CSV manifest.

## Manipulation check, reported first

Raw decision-tick counts are **confounded by exposure**: gate-on populations accumulate more
creature-ticks (+19,464, t +1.65), so any raw count is partly a population measure. Normalised per
creature-tick:

| | mean paired delta | t | sign |
|---|---:|---:|---|
| flee rate | −0.0285 | **−5.07** | 80/120 down |
| mate-seek rate | +0.0090 | **+2.67** | 75/120 up |

The gate is live, safety-scoped, and moves behavior in the designed direction: less fleeing, more
mate-seeking. 120/120 state hashes differ.

## Results

| outcome | mean paired delta (on − off) | t | sign | verdict |
|---|---:|---:|---|---|
| **predation deaths** | **−2.275** | **−4.64** | 70/120 down | **real effect** |
| predation deaths per creature-tick | — | **−4.85** | 72/120 down | survives normalisation |
| births (raw) | +12.81 | +2.04 | 72/120 up | **exposure artefact** |
| births per creature-tick | +0.00001 | +1.24 | 70/120 up | not significant |
| births, both-survived seeds only (n=28) | +11.71 | +1.01 | 15/13 | not significant |
| total deaths | +4.05 | +0.72 | 62/58 | null |
| starvation deaths | +1.15 | +0.85 | 58/44 | null |

**Extinction, paired properly.** 75/120 vs 66/120 looks like a benefit, but the arms are paired and
the aggregate hides that. Discordant pairs: **26 extinct only with the gate off, 17 only with it
on**; 49 extinct in both, 28 in neither. McNemar with continuity correction **χ² = 1.488** against
3.84 for p .05 — **not significant**. The headline difference does not survive the paired test.

**The births result is exposure, not fertility.** Raw births rise (t +2.04), but the birth *rate*
per creature-tick does not (t +1.24), and among the 28 seeds where both arms survived the difference
vanishes (t +1.01). Gate-on populations do not breed faster; some of them simply exist longer.

## What this means

The gate does its job. Creatures that decline to rendezvous next to a live threat flee 2.9 percentage
points less often per creature-tick and die to predation measurably less (t −4.64, 70/120). That is
a clean, correctly-signed, well-powered mechanism effect.

It buys nothing at the population level. Predation is not what limits this population — **starvation
is**, and starvation is untouched (t +0.85). Saving roughly 2.3 creatures per run from predators does
not change whether the population survives, because the binding constraint is food.

**Distinguish this from home-range affinity**, which was closed because its effect had the *wrong
sign*. This effect has the right sign and simply does not reach the outcome that matters. Two
different verdicts, and they should not be recorded as the same thing.

## Decision

- **`SafetyGatedMateRendezvousEnabled` stays default `false`.** No demonstrated fitness benefit.
- **Do not build pack architecture, group cohesion or family structure to force an effect.** The
  mechanism already works; the ecology declines to reward it.
- **Do not tune the gate.** Its measured effect is in the intended direction and adequately powered;
  a bigger gate buys more of a thing that does not propagate.
- **Reopen only with a predation-limited habitat.** If a scenario is ever built where predation
  rather than starvation limits the population, this gate becomes a live hypothesis again and should
  be re-measured there. That is a scenario question, not a mechanism question.

## Provenance failure, recorded

The 2026-08-21 configuration **could not be recovered**. Its probe was never committed. 81
configurations were tried across 5 recipes and 18 founder counts, matched against both the CSV's
recorded per-seed state hash (`5536980505421044029` for seed 42 gate-off) and its recorded births
(285). Nothing reproduced either; births peak at 276 in the closest family and fall as founder count
rises.

**This run is therefore a new condition, not a rerun of that one**, and no claim here should be read
as contradicting the 2026-08-21 numbers on their own operating point. This is the second such loss
after the 168-site geometry. Both predate `ExperimentManifest`; this CSV carries one, which is
exactly what it was built for.

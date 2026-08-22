# Adjudicating the low-occupancy conclusions on fixed code

**Date:** 2026-08-22
**Raw data:** `p4-low-occupancy-adjudication-2026-08-22.csv`
**Depends on:** `p4-occupancy-calibration-2026-08-22.md` (the condition),
`p4-168-site-replication-2026-08-22.md` (why it is a replication, not a re-run)
**Status:** predictions registered before the run; results appended after.

## What is being settled

Three conclusions were measured at a 168-site low-occupancy operating point whose scenario was never
committed, then left un-auditable when the takeover-age fix landed:

1. **`SeedProductionRate` becomes selected at low occupancy** — recorded +0.02022, t +4.32, 79/120 up
   against a matched disabled arm at 66/120. (At 24 sites it is null: 68/120 against 70/120 drift.)
2. **Lifespan gains headroom at low occupancy** — `Growth`, which shortens lifespan, moves *down*:
   recorded −0.01131, t −2.65, 46/120 up, against a mortality-off drift arm at +0.00450, t +2.25,
   61/120 up.
3. **`SeedlingResilience` reverses at low occupancy** — recorded −0.01184, t −2.56, 44/120 up, where
   at 24 sites it is selected upward.

Plus the six growth-rate trait nulls recorded at the same operating point.

## Design, and the control that was wrong last time

120 seeds (42–161), 12,000 ticks, the calibrated low-occupancy condition (span 114, spacing 9.5,
occupancy ≈0.31), varying founders so there is standing variance to select on, competition and
mortality on.

Every arm is paired against a control **in which the trait's channel is disabled**, not merely a
different ecology:

| claim | manipulation arm | matched disabled control |
|---|---|---|
| SeedProductionRate selected | `plantSeedProductionRateEnabled: true`, charge 0 | `base` — flag off, so the gene has no reader |
| SeedlingResilience reverses | `plantEstablishmentContestEnabled: true` | `base` — no contest, so resilience decides nothing |
| Lifespan headroom | `base` (mortality on) | `mortality-off` — lifespan has no channel |

The previous sweep used a competition-**off** arm as its "drift" control. That was wrong and its
drift comparisons were discarded: turning competition off disables no trait, so dispersal and the
rest keep acting, and `Dispersal` read *larger* there than in the competition arms.

`RealizedGrazingPressure` is reported for every arm, because this condition has a known ecological
difference from the 24-site one (below).

## Predictions, registered before running

**Condition checks, reported first.**

1. Occupancy lands 0.28–0.35 in every arm. If it does not, the condition drifted and nothing
   downstream is interpretable.
2. Extinctions ≤ 6/120 and frozen ≤ 6/120 per arm. The calibration showed 0/10 at this spacing.
3. `RealizedGrazingPressure` is **materially lower** than the 24-site condition's, because the outer
   lattice sits beyond the ±25 creature arena and those patches are never grazed. I expect roughly
   half or less. This is the caveat made quantitative rather than asserted.

**The three claims.**

4. **SeedProductionRate:** `seedprod-on` minus `base`, paired, gives a positive delta with ≥ 70/120
   seeds up and t ≥ +2.5. Recorded 79/120 at t +4.32.
5. **Lifespan headroom:** `Growth` delta in `base` is negative, and paired against `mortality-off` it
   is more negative by a margin with ≥ 65/120 seeds down. Recorded −0.01131, t −2.65, 46/120 up.
6. **SeedlingResilience:** `contest-on` minus `base`, paired, is **negative or null** at this
   occupancy — ≤ 60/120 seeds up. This is the prediction I am least confident in and the most
   interesting: at 24 sites the same manipulation measured **+0.0362, t +3.22, 72/120 up** on fixed
   code, so a reversal here means the operating point genuinely flips the sign of an establishment
   advantage.
7. **The six growth-rate traits** (Nutrition, WaterEfficiency, MoistureTolerance,
   TemperatureTolerance, NutrientUptake, and the growth-rate component generally) stay null against
   their controls: no trait beats 65/120 up with |t| ≥ 2.5.

**Pre-run judgment.** Predictions 4 and 5 rest on recorded results measured in a *different*
geometry, and the grazing difference in prediction 3 is a real reason they might not transfer. If 4
or 5 fails, the honest reading is not automatically "the recorded conclusion was wrong" — it may be
"the conclusion is specific to a geometry that no longer exists". I will not resolve that ambiguity
by assertion; where it arises it gets recorded as a limit of what a replication can establish.

## Results

480 runs. Raw rows: `p4-low-occupancy-adjudication-2026-08-22.csv`.

### Condition checks, reported first — one failed

| arm | occupancy | grazing pressure | extinct | frozen | plant generations |
|---|---|---|---|---|---|
| base | 0.3518 | 0.0026 | 4/120 | 0 | 17.8 |
| seedprod-on | 0.3321 | 0.0029 | 5/120 | 1 | 18.0 |
| contest-on | 0.2814 | 0.0032 | **19/120** | 1 | 17.0 |
| mortality-off | 0.5395 | 0.0015 | 0/120 | 0 | 13.4 |

Prediction 1 holds: occupancy sits in the 0.28–0.35 band in the three mortality-on arms, so the
calibrated condition reproduces. `mortality-off` at 0.54 is expected — nothing dies, so sites
accumulate.

**Prediction 2 failed for `contest-on`: 19/120 extinct against a predicted ≤6.** The establishment
contest is markedly less survivable at low occupancy than at 24 sites, where the same manipulation
cost nothing. That is itself a result, and it means the contest comparison below carries
differential extinction as a confound.

### The three claims: none replicate

**SeedProductionRate — does not replicate.** Paired `seedprod-on` minus `base` (the disabled-flag
control):

| | delta | t | seeds up |
|---|---|---|---|
| measured here | +0.00424 | +0.72 | 64/120 |
| recorded at 168 sites | +0.02022 | +4.32 | 79/120 |

Prediction 4 failed. At this low-occupancy condition the gene is null, as it is at 24 sites.

**SeedlingResilience — the reversal is not demonstrated, but the advantage is abolished.** Paired
`contest-on` minus `base`: −0.00248, t −0.34, 53/120 up, against a recorded −0.01184, t −2.56,
44/120. So prediction 6 is half right: there is no significant *negative* selection, but the
strongly positive effect measured on fixed code at 24 sites — **+0.0362, t +3.22, 72/120** — is gone.
The establishment advantage does not survive the move to low occupancy; a reversal does not.

**Lifespan headroom — not adjudicated, and my control was poorly chosen.** `Growth` in `base` minus
`mortality-off` came out at −0.00942, t −1.51, **58/120 up**, against a recorded −0.01131, t −2.65,
46/120 up. The point estimate is close but the sign count points the wrong way.

More importantly, the same comparison moves traits that have nothing to do with lifespan, hard:
`Dispersal` +0.0834 (t +21.40, 118/120), `NutrientUptake` −0.0466 (t −7.62), `WaterEfficiency`
−0.0445 (t −8.61), `SeedProductionRate` +0.0223 (t +4.98), `Defense` +0.0183 (t +3.67). **Turning
plant mortality off does not isolate lifespan — it removes site turnover and rewrites the entire
selective regime.** It is a matched control in the narrow sense that lifespan has no channel without
it, and useless in the sense that everything else changes too. This claim needs a control that
disables the lifespan channel specifically, which does not currently exist.

### The six growth-rate nulls: hold

Against their disabled controls, no growth-rate trait clears |t| 2.5 with a decisive sign count.
The largest is `Nutrition` at +0.01382, t +2.29, 74/120 in the seedprod comparison — under the bar
and not the comparison it belongs to. The recorded nulls are consistent with what is measured here.

## Verdict: the low-occupancy conclusions are NOT confirmed, and NOT refuted

Three claims went in; none came out confirmed. But the honest reading is not "the recorded results
were wrong", for a reason registered before the run:

**This condition reproduces the recorded occupancy and not the recorded ecology.**
`RealizedGrazingPressure` is **0.0026** here. The lattice spans ±57 while the creature arena is
hard-coded to ±25, so most of the free-site pool sits in ungrazed refugia. Free sites that no
herbivore can reach are not the same resource as free sites inside a grazed arena, and every one of
these three claims is about how free-site abundance changes selection.

So the outcome is:

- The recorded low-occupancy conclusions **remain unverified on current code** and should not be
  relied on. Their banners stay.
- They are **not retracted**, because the replication differs ecologically in a way that plausibly
  explains a null.
- What *is* established on fixed code: `SeedProductionRate` is null in every condition reproducible
  here; the establishment advantage measured at 24 sites does not survive to low occupancy; the six
  growth-rate nulls hold; and the establishment contest costs 19/120 extinctions at low occupancy.

**What would settle it:** a low-occupancy condition whose free sites are *inside* the ±25 arena. That
is geometrically impossible with 162 targets at a spacing wide enough to avoid saturation — the
arena cannot hold them. So the real blocker is now precise and structural: **the recorded 168-site
operating point may not be reproducible in this arena at all**, and if the original achieved it
in-arena, its layout must have differed from anything a lattice can express. Recovering the original
coordinates is the only path to a true audit, and they are gone.

The next honest option, if these conclusions matter, is to stop treating them as recoverable and
re-derive the underlying question — does free-site abundance change which plant traits are
selected — as a **new** experiment with a committed scenario, rather than as an audit of an
unrecoverable one.

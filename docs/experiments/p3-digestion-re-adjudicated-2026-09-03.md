# P3 digestion, re-measured at the allocation site: the negative holds, and the cell does not survive the run

**Date:** 2026-09-03
**Status:** the Task 9 measurement experiment, run. **Negative, with one predeclared prediction
failed and one large caveat that is itself the most important finding.** No biological code changed.
**This document does not adjudicate the 2026-08-30 record.** That decision is the user's and is
pending; see "What this does not do" below.
**Harness:** `tools/CreatureSweep --life-history`, new in this milestone. Raw output is committed
beside this file as `p3-re-adjudicated-36000-health{off,on}-2026-09-03.txt`.

## What was run

The recorded digestion cell, held fixed: cap 500, `--regen=2.0`, `--brake=1.0`, `--predation`,
`--gate=0.45`, `--mate-selection=off` (proximity pairing). Nothing else changed — not digestion
coefficients, population size, mutation, brake strength, resource layout, the reproduction scheduler,
learning, or kin recognition.

**Arms:** health recovery OFF and ON, paired on seed. 24 seeds an arm, **36,000 ticks**, against the
recorded result's 8 seeds at 12,000.

**Cohort:** complete-life, admitting births in `[0, 30,600]` — 85% of the run. The horizon is
`MaximumLifespanTicks = 5,400`, derived from `GenomePhenotype`'s lifespan expression at the top of
`LifespanTendency`'s clamp, so a creature is admitted on its birth tick alone and its own lifespan
genotype is never consulted. 22,596 creatures (OFF) and 21,124 (ON) qualified. **No creature was
excluded for a missing genome in either arm**, and every world's pedigree was complete.

**The instrument checked itself.** Both arms end with 2,000 ticks run with the ingestion recorder
attached and detached: identical state hash. The measurement did not perturb its subject.

---

## The caveat first, because it conditions everything else

**The cell does not persist to 36,000 ticks.** Population at the end of the run:

| arm | worlds extinct | surviving |
|---|---|---|
| health recovery OFF | **21 of 24** | 3 |
| health recovery ON | **23 of 24** | 1 (seed 50, population 202) |

The 2026-08-30 record reports this cell surviving 24 of 24 — **at 12,000 ticks**. Both are true. The
recorded cell is not a steady state; it is the first third of a collapse, and nothing measured at
12,000 ticks could have shown that.

So every number below is measured **across a collapse**, on cohorts that are large precisely because
so many creatures lived and died inside it. That does not make the numbers wrong — the cohort rule is
genotype-independent and the pedigrees are complete — but it does mean they describe a declining
ecology rather than a persistent one. **Any conclusion drawn from them inherits that.**

This is the third time this project has been caught by a length assumption; the standing lesson is
"distinguish *not yet* from *not ever* by running longer". Running longer worked, and it answered a
question nobody asked.

---

## 1. Is there a diet-dependent difference in gross ingestion?

Gross energy per 1,000 ticks, measured at the allocation site — every bite, including the ones taken
under a stale `Seek*` action, and before the capacity clamp.

| diet | OFF: creatures | OFF: gross/1k | OFF: lifetime | ON: creatures | ON: gross/1k | ON: lifetime |
|---|---|---|---|---|---|---|
| 0.0-0.2 | 4292 | 103.20 | 319.8 | 3680 | 101.18 | 317.4 |
| 0.2-0.4 | 4276 | 103.75 | 318.7 | 3859 | 101.86 | 313.7 |
| 0.4-0.6 | 4487 | 104.47 | 315.5 | 5070 | 106.31 | 326.0 |
| 0.6-0.8 | 3777 | 104.44 | 307.9 | 3813 | 104.78 | 309.8 |
| 0.8-1.0 | 5764 | 105.69 | 299.5 | 4702 | 108.44 | 321.1 |

**Essentially flat, and very slightly rising.** Spread across the gene is 2.4% (OFF) and 7.2% (ON),
against the 11-13% the retired instrument reported.

### The predeclared sign failed

The prediction committed to the plan before these runs, and the only one predeclared:

> Gross ingestion falls with diet and then recovers. Total intake is highest at the herbivore end,
> lowest in the 0.6-0.8 band, and partially recovers at the carnivore end — the recorded 12% valley
> survives correction by the new instrument.

**It does not.** There is no valley in either arm. Rate rises monotonically with diet in the OFF arm
(103.20 → 105.69) and rises with one dip in the ON arm. The recorded "89.1 at the herbivore end,
sagging to 78.7 in the 0.6-0.8 band, recovering to 85.4" is not reproduced by a measurement taken at
the allocation site.

**A prediction that fails is still a measurement, and this one is reported as failed.** The three
other candidate predictions offered at the same time — offspring flat across diet, energy usually
binding, health recovery not changing the answer — were **deliberately not predeclared**, so what
follows on those is a first observation and must not be described as a confirmed prediction.

## 2. Does it reach reproductive fitness?

Complete-life cohort only.

| diet | OFF: offspring | OFF: surviving to adulthood | ON: offspring | ON: surviving to adulthood |
|---|---|---|---|---|
| 0.0-0.2 | 1.966 | 1.959 | 1.980 | 1.977 |
| 0.2-0.4 | 1.977 | 1.972 | 1.959 | 1.953 |
| 0.4-0.6 | 1.962 | 1.953 | 1.986 | 1.979 |
| 0.6-0.8 | 1.971 | 1.947 | 1.946 | 1.927 |
| 0.8-1.0 | **2.124** | 2.012 | 2.037 | 2.001 |

Both parents are credited for the same birth, so replacement is about two.

**No.** Offspring is flat to within 1% across four of the five bins in both arms. The top bin is
slightly higher — and note that the *surviving-to-adulthood* column erases most of that lift in the
OFF arm (2.124 → 2.012), which is the first time this project has had the two quantities side by side.

The per-world statistics say the same thing and say it with replication:

| relationship | OFF | ON |
|---|---|---|
| diet → gross ingestion rate | mean per-world r **+0.028**, 15 positive / 9 negative | **+0.043**, 20 / 4 |
| diet → offspring | **+0.009**, 10 / 14 | **+0.002**, 12 / 12 |
| diet → offspring surviving to adulthood | **+0.003**, 9 / 15 | **-0.001**, 11 / 13 |
| lifetime gross ingestion → offspring | **+0.872**, 24 / 0 | **+0.876**, 24 / 0 |

A twelve-to-twelve split on the sign is what nothing looks like.

**The `r +0.88` from the 2026-08-30 record survives re-measurement almost exactly**: +0.872 and
+0.876 as a mean of per-world correlations, 24 of 24 worlds positive in both arms, bootstrap intervals
[+0.858, +0.885] and [+0.865, +0.885]. Energy does become descendants. Diet does not become energy.

## 3. Which need is usually binding?

Sampled on reproduction ticks, adults only, using `ReproductionSystem.CanReproduce` itself rather
than a copy of the gate. Restricted to blocked creatures — the conditioning that says whether an
energy-side trait can reach fitness at all:

| need | OFF: lowest among blocked | ON: lowest among blocked |
|---|---|---|
| Energy | **74.6%** | **77.0%** |
| Hydration | 20.0% | 19.7% |
| Health | 5.4% | 3.3% |

Energy is the binding need three times out of four. **But the more interesting number is next to it:
of all blocked adult samples, 52.3% (OFF) and 49.8% (ON) are blocked by a running reproduction
cooldown, which is not a need at all.** About half of all reproductive blocking in this cell is the
cooldown timer, and no amount of energy advantage moves it.

7.5% (OFF) and 8.0% (ON) of adult samples pass the breeding gate and fail the higher mate-seeking
gate.

## 4. Does health recovery change the result?

**No.** Judged on whether the answers to 1-3 differ, not on whether the population differs:

- Ingestion by diet bin: flat in both arms.
- Offspring by diet bin: flat in both arms.
- diet → offspring: +0.009 (10/14) against +0.002 (12/12). Both nothing.
- lifetime intake → offspring: +0.872 against +0.876. Identical.
- Binding need among blocked: energy 74.6% against 77.0%.

The arms differ where you would expect a health ratchet to matter and nowhere else: health is the
binding need for 5.4% of blocked samples with recovery off and 3.3% with it on, and extinction is
slightly *more* common with recovery on (23/24 against 21/24) rather than less.

**This closes the standing 2026-08-26 rule** for this question — both health arms were run rather
than one being chosen, and they agree.

## 5. Is the cap binding?

**No.** Population saturated on 1.5% of reproduction ticks in both arms; cap-blocked on 1.5%.
`ReproductiveSkewInterpretationIsUnsafe` is **clear in all 24 worlds of both arms** at the
predeclared threshold of 0.25.

Instead, 44.5% (OFF) and 40.0% (ON) of reproduction ticks have two or more ready creatures **below**
the cap that did not pair — mate-finding, which is ecology, not the cap. Given the extinction result
that is unsurprising: a collapsing population is not pressed against its ceiling.

So reproductive-skew readings from this cell are interpretable, and the `CreatureId`-ordered
scheduler is not confounding them here. That is a conditional clearance: it holds for this cell, at
this cap, in these runs.

## 6. Do per-world effects agree, or was the pooled correlation misleading?

Both, mildly. The pooled and per-world figures agree in sign everywhere, so no Simpson's reversal
occurred — but pooling consistently overstates. `diet → offspring` pools to +0.023 (OFF) against a
per-world mean of +0.009 on a 10/14 sign split; `lifetime intake → offspring` pools to +0.848 against
a per-world mean of +0.872.

The pooled figure is reported in the sweep output and labelled **PSEUDO-REPLICATED** every time. The
per-world sign counts are the honest summary, and for diet they are a coin flip.

## 7. The surplus-lost fraction

Energy discarded by the capacity clamp, which the old proxy could not see at all:

| arm | gross | surplus lost | share |
|---|---|---|---|
| health recovery OFF | 7,271,584 | 22,697 | **0.31%** |
| health recovery ON | 6,798,624 | 2,319 | **0.03%** |

**Negligible.** Defect 2 — capacity clamping — is real in the source and does not matter in this
ecology. A bite is worth roughly one energy unit against roughly twenty-four of headroom at the
operating point, so the clamp almost never fires. The hypothesis that "a capped energy pool discards
the surplus, so an intake advantage is not a fitness advantage" was refuted by the 2026-08-30 record
using a correlation; it is now refuted directly, by measuring the discarded quantity.

## 8. What the retired instrument was missing

Measured in the same runs:

| arm | measured gross | delta-proxy estimate | ratio | taken under a stale `Seek*` action |
|---|---|---|---|---|
| health recovery OFF | 7,271,584 | 5,710,475 | **1.273** | **12.79%** of gross |
| health recovery ON | 6,798,624 | 5,347,551 | **1.271** | **12.73%** of gross |

The retired positive-energy-delta instrument sees about **79%** of the energy actually ingested. An
independent measurement during Task 6, at 8 seeds × 12,000 ticks under a different configuration, put
the ratio at 1.380 with a 19.55% stale share and a feeding-tick ratio of only 1.052 — which locates
most of the gap in **drain-tick erasure** rather than in stale actions: on a needs tick the
half-second drain usually exceeds the bite, the delta goes negative, and the whole tick's ingestion is
discarded rather than blurred.

So the four defects, measured rather than argued:

1. **Right-censoring** — removed by construction, not modelled. The genotype-independent horizon
   admits births in 85% of the run and consults no creature's lifespan gene.
2. **Capacity clamping** — real, and **0.03-0.31% of gross**. Negligible here.
3. **Drain-tick erasure** — the largest single loss. Most of the 27-38% gap.
4. **Stale-action erasure** — **12.7-19.6% of gross energy**, invisible to the old instrument by
   construction.

## What this establishes

- **The digestion negative holds under a corrected instrument, at three times the seeds and three
  times the run length, in both health arms.** `DietSpecialization` does not reach reproductive
  fitness. The sign of `diet → offspring` is a coin flip across worlds.
- **`r +0.88` was right.** Lifetime energy intake predicts offspring at +0.872 / +0.876 per world,
  24 of 24 worlds positive in both arms.
- **The 12% intake valley was not.** It does not appear in a measurement taken at the allocation
  site, and its disappearance is the predeclared prediction that failed.
- **The old instrument under-counts ingestion by 21-27% in this cell** and cannot see 12.7% of it at
  all, for a reason that is structural rather than incidental.
- **The capacity clamp is not the blocker.** It discards under a third of one percent.
- **About half of all reproductive blocking is a cooldown timer**, not a need.
- **The cap is not binding in this cell**, so skew readings from it are interpretable.

## What this does not establish, and what it does not do

- **It does not adjudicate the 2026-08-30 record.** The banner and the retraction-or-confirmation
  decision are Task 10 of the measurement-validity plan and are **pending a decision by the user**,
  because every number here is measured across an ecosystem collapse and whether that is grounds for
  retraction is a judgement, not a measurement.
- **It does not establish that this cell is a valid long-run ecology.** It is not. 21-23 of 24 worlds
  are extinct at 36,000 ticks. Whatever this cell is measuring after about tick 12,000, it is not a
  persistent population.
- **It does not say when the collapse happens.** No per-world extinction tick was recorded; that
  needs another run.
- **It does not say the trade-off is absent from the source.** `PlantFoodYieldMultiplier` is still
  `1 - 0.3 * diet`. The trade-off exists; it does not reach ingestion at the rate the old instrument
  reported, and it does not reach fitness at all.

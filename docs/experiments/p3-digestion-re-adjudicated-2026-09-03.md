# P3 digestion, re-measured at the allocation site: the negative holds, and the cell does not survive the run

**Date:** 2026-09-03
**Status:** the Task 9 measurement experiment, run. **Negative, with one predeclared prediction
failed and one large caveat that is itself the most important finding.** No biological code changed.
**The 2026-08-30 record is superseded on the instrument and confirmed on the conclusion** — see
`p3-digestion-strategies-2026-08-30.md`'s banner. The caveat is recorded separately as
[`p6-the-recorded-cell-is-a-transient-2026-09-03.md`](p6-the-recorded-cell-is-a-transient-2026-09-03.md),
because it bears on far more than digestion.
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

The 2026-08-30 record reports this cell — brake 1.0 — as 4,761 creatures over **22 surviving runs of
24**, at 12,000 ticks. Both are true. The
recorded cell is not a steady state; it is the first third of a collapse, and nothing measured at
12,000 ticks could have shown that.

So every number below is measured **across a collapse**, on cohorts that are large precisely because
so many creatures lived and died inside it. That does not make the numbers wrong — the cohort rule is
genotype-independent and the pedigrees are complete — but it does mean they describe a declining
ecology rather than a persistent one.

### Why "measured in dying worlds" is not fatal to the digestion result

**The same cohort, in the same worlds, in the same collapsing ecology, returns lifetime intake →
offspring at +0.872 and +0.876 with 24 of 24 worlds positive in both arms.** That is the load-bearing
sentence of this document.

The design therefore demonstrably has the power to detect a real, strong relationship **under exactly
the conditions the diet null was measured in**. The 12-of-24 / 12-of-24 sign split on
`diet → offspring` is not a floor effect, a variance ceiling, or a measurement that the collapse
washed out: an instrument that could not see a relationship here would not have returned +0.87 on a
different relationship from the identical creatures. The diet null is **centred on zero**, and the
collapse conditions how far the result generalises, not whether it was detectable.

The separate finding — that this cell is a transient — is recorded in
[`p6-the-recorded-cell-is-a-transient-2026-09-03.md`](p6-the-recorded-cell-is-a-transient-2026-09-03.md).

The standing lesson from 2026-08-30 — "distinguish *not yet* from *not ever* by running longer, not by
arguing" — was written about a trait that had not moved. Running longer worked, and it answered a
question nobody had asked.

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

**Where the valley went was settled by two follow-up runs on 2026-09-03, and the answer is run
length.** See "The valley, decomposed" below. In short: the valley is present at 12,000 ticks under
**both** instruments and absent at 36,000 under the same instrument and the same seeds, so the
variable that separates them is the run — not the instrument, which turns out to *attenuate* the
valley rather than manufacture it, and not the seed count, which only inflated its depth.

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

Energy is the binding need three times out of four.

**But the more interesting number is next to it: of all blocked adult samples, 52.3% (OFF) and 49.8%
(ON) are blocked by a running reproduction cooldown — roughly half of all reproductive blocking in
this cell is a refractory period rather than a need being below the gate.**

**That refractory period is itself condition-dependent, which matters.** Graded fertility is on at
brake 1.0, so `ReproductionSystem.CooldownFor` multiplies the base cooldown by
`CooldownMultiplierFor`, and that multiplier is computed from the **minimum of the three normalised
needs at the moment of the last birth**. A creature in poor condition when it bred waits
proportionally longer before it can breed again. So this is not "a timer, not a need" — it is a
refractory period whose *duration* is set by condition, which is exactly the smooth density brake
graded fertility was added to provide.

The consequence for digestion is unchanged and worth stating plainly: an energy advantage can shorten
the next refractory period, and it still produces no measurable difference in lifetime offspring.

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

### The erasure rate is not diet-dependent, so it did not manufacture the valley

Measured per diet bin over cohort members, in the same runs. This is the query that would have
explained the missing valley, and it comes back negative:

| diet | OFF: ratio | OFF: stale share | ON: ratio | ON: stale share |
|---|---|---|---|---|
| 0.0-0.2 | 1.266 | 12.25% | 1.273 | 12.74% |
| 0.2-0.4 | 1.260 | 11.77% | 1.264 | 12.06% |
| 0.4-0.6 | 1.262 | 12.05% | 1.257 | 11.68% |
| 0.6-0.8 | 1.259 | 11.99% | 1.266 | 12.55% |
| 0.8-1.0 | 1.279 | 13.47% | 1.277 | 13.57% |

**Flat.** The ratio spans 1.259 to 1.279 across the whole diet range in the OFF arm and 1.257 to
1.277 in the ON arm — a spread of 1.6%, non-monotone, and in the wrong direction to produce a valley
in the middle (if anything the erasure is very slightly *largest* at the carnivore end, which would
deepen a measured carnivore trough rather than a mid-range one).

**So the retired instrument under-counted ingestion uniformly across the gene, and uniform
under-counting cannot create a 12% mid-range valley in a rate.** The valley's origin is therefore
**unexplained by this experiment**. What differs between the two measurements, beyond the instrument:

- **Run length.** 12,000 against 36,000 ticks, and the cell is a transient — so the two measurements
  cover different parts of a declining trajectory.
- **Cohort rule.** The recorded table filtered on `AliveTicks > 200`, which admits creatures still
  alive at run end; this one admits on birth tick alone and requires a complete life.
- **Seeds.** 8 against 24. The field notes record an effect significant at n=5 vanishing at n=30 in
  this project before, and 8 seeds is within that range.
- **Ordinary sampling noise**, which remains a live candidate and cannot be excluded here.

**Distinguishing these would need the retired `--intake` mode re-run at 24 seeds and 12,000 ticks —
which is why `Intake.cs` was deliberately kept.** That measurement was not made; this record does not
claim it was.

### The valley, decomposed

Three variables differed between the recorded table and this one — instrument, 8 seeds against 24,
and 12,000 ticks against 36,000. Two further runs in the same cell separate them. **Compare the
shape, not the level**: the delta proxy reports a net energy delta and the recorder reports gross
energy at the allocation site, and the two modes use different cohorts (`AliveTicks > 200` against
complete-life), so only the profile across bins is comparable.

| measurement | instrument | seeds | ticks | intake per 1k ticks, by diet bin | valley |
|---|---|---|---|---|---|
| recorded 2026-08-30 | delta proxy | 8 | 12,000 | 89.14 · 84.32 · 81.82 · **78.67** · 85.45 | **11.7%** |
| run 1, this section | delta proxy | 24 | 12,000 | 86.45 · 86.62 · 84.29 · **84.13** · 87.87 | **2.9%** |
| run 2, this section | allocation site | 24 | 12,000 | 152.82 · 151.23 · 149.79 · **140.34** · 152.28 | **8.2%** |
| Task 9, above | allocation site | 24 | 36,000 | 103.20 · 103.75 · 104.47 · 104.44 · 105.69 | **none** |

Raw output: `p3-valley-intake-24seeds-12000-2026-09-03.txt` and
`p3-valley-lifehistory-24seeds-12000-2026-09-03.txt`.

**The valley is present in both 12,000-tick runs and absent at 36,000.** By the decomposition's own
reading rule that makes **run length** the variable that decides it. The minimum sits in the 0.6-0.8
band in all three 12,000-tick measurements, with recovery at the carnivore end, and the profile is
monotone and flat at 36,000.

Two things fall out that were not the point of the runs:

- **The old instrument attenuates the valley; it does not create one.** At 24 seeds and the same
  12,000 ticks the proxy shows a 2.9% dip where the allocation-site measurement shows 8.2%. The
  erasure-by-bin table for run 2 explains why, and unlike at 36,000 ticks it is **not** flat: the
  ratio falls monotonically with diet, 1.257 / 1.240 / 1.238 / 1.236 / **1.219**, so the proxy loses
  most at the herbivore end — exactly the end that makes the valley deep. Diet-dependence of the
  erasure is therefore itself length-dependent, which is worth knowing before quoting the flat
  36,000-tick table as a general property.
- **Eight seeds inflated the depth.** The same instrument at the same length gives 11.7% at n=8 and
  2.9% at n=24. The recorded figure was a real effect measured badly, not a phantom.

### The length/phase confound, separated

The two runs above left run length and *which phase of the cell the cohort samples* moving together.
One 36,000-tick run separates them **by reporting rather than by re-running**: the same complete-life
cohort, split on birth tick alone with `FitnessCohort`'s birth-window rule, into births in
`[0, 6,600]` and births after. Same horizon on both halves; only the birth tick differs. Raw output:
`p3-birth-window-split-36000-healthoff-2026-09-03.txt`.

| diet | early n | early gross/1k | early offspring | late n | late gross/1k | late offspring |
|---|---|---|---|---|---|---|
| 0.0-0.2 | 758 | 152.822 | 3.769 | 3534 | 92.562 | 1.580 |
| 0.2-0.4 | 769 | 151.233 | 3.670 | 3507 | 93.337 | 1.606 |
| 0.4-0.6 | 952 | 149.788 | 3.810 | 3535 | 92.271 | 1.465 |
| 0.6-0.8 | 852 | **140.342** | 3.459 | 2925 | 93.981 | 1.538 |
| 0.8-1.0 | 768 | 152.278 | 3.945 | 4996 | 98.527 | 1.843 |

Cohort sizes: early 4,099, late 18,497.

**The valley is in the early window and absent from the late one.** So the variable is **phase, not
length** — length was only the vehicle that decided which phase the pooled cohort was mostly made of.

The early window's numbers are identical to the separate 12,000-tick run, to every printed digit, for
both intake and offspring. That is not a coincidence and it is worth stating: at 12,000 ticks the
complete-life cohort *is* births in `[0, 6,600]`, so the two measurements are the same creatures. The
pooled 36,000-tick table averaged this window together with a late window four and a half times its
size, which is how an 8.2% valley became a flat line.

**The two windows are different ecologies, and the offspring column shows it.** 3.46-3.95 offspring per
creature early against 1.47-1.84 late — expansion against whatever the cell is doing on its way down.

**What this licenses saying, and what it does not.** A trait effect present during population expansion
and absent afterwards is **density-dependent**, and that is a different claim from "no effect": it
bears on the P3 gate, because a strategy difference that only exists away from carrying capacity is
still a strategy difference, and the gate asks about persistence.

**But the per-world sign counts do not corroborate it, and the reason is a statistical one that must
not be glossed:**

| window | relationship | mean per-world r | worlds + / - | 95% interval |
|---|---|---|---|---|
| early | diet → gross ingestion rate | +0.015 | **13 / 11** | [-0.042, +0.066] |
| early | diet → offspring | +0.042 | **15 / 9** | [-0.026, +0.106] |
| late | diet → gross ingestion rate | +0.072 | **21 / 3** | [+0.049, +0.095] |
| late | diet → offspring | +0.034 | **16 / 8** | [+0.012, +0.055] |

A correlation measures a **monotone** trend and a valley is **not monotone**, so the early window's
13-to-11 coin flip is exactly what a U-shape produces and exactly what noise produces. **It neither
supports the valley nor refutes it.** The statistic that would corroborate a density-dependent reading
is a per-world U-shape test — in how many of the 24 worlds does the 0.6-0.8 bin mean fall below both
end bins — and **that was not computed here**. Until it is, the density-dependent reading rests on
pooled bin means, which is a weaker footing than the sign counts elsewhere in this document.

### The control column, and what it withdraws

Run on 2026-09-03, same cell, same 24 seeds, 36,000 ticks, health recovery off. Raw output:
`p3-neutral-control-and-ushape-36000-healthoff-2026-09-03.txt`.

Two things were missing from the window split above, and the project's own standing methodology
required both: **`NeutralMarker` reported beside every drift column**, and results judged against
**`PairedEvolutionCriterion.MinimumDirectionConsistency = 0.75`** rather than against whether an
interval excludes zero. `NeutralMarker` is read by zero behaviour code and is pinned dead by
`LivenessTests`, so anything it shows is structure, not biology - most plausibly family-level, since
relatives share both a drifted marker value and a foraging neighbourhood.

| window | relationship | mean per-world r | worlds + / - | consistency | 0.75 | 95% interval |
|---|---|---|---|---|---|---|
| early | diet → intake rate | +0.015 | 13 / 11 | 0.542 | fails | [-0.042, +0.066] |
| early | **neutral** → intake rate | -0.004 | 10 / 14 | 0.583 | fails | [-0.039, +0.035] |
| early | diet → offspring | +0.042 | 15 / 9 | 0.625 | fails | [-0.026, +0.106] |
| early | **neutral** → offspring | -0.016 | 11 / 13 | 0.542 | fails | [-0.058, +0.026] |
| late | diet → intake rate | **+0.072** | **21 / 3** | **0.875** | **PASSES** | [+0.049, +0.095] |
| late | **neutral** → intake rate | +0.020 | 17 / 7 | 0.708 | fails | **[+0.008, +0.033]** |
| late | diet → offspring | +0.034 | 16 / 8 | **0.667** | **fails** | [+0.012, +0.055] |
| late | **neutral** → offspring | +0.010 | 17 / 7 | **0.708** | fails | [-0.007, +0.027] |

**One result survives, one does not, and they are not findings of the same kind.**

- **`diet → intake rate` in the late window passes the committed threshold** at 0.875 direction
  consistency, 21 of 24 worlds positive, and it stands well clear of its control: r +0.072 against the
  marker's +0.020, and the marker fails 0.75. This is the one positive relationship in this milestone
  that survives both its own null and the repository's own acceptance criterion.
- **`diet → offspring` in the late window is withdrawn.** It fails the threshold at 0.667 — and its
  control is **more** direction-consistent than it is (0.708, 17 of 24). An effect that its own inert
  channel outperforms on the criterion the project uses to accept effects is not an effect. The
  previous pass called it "the first diet-to-fitness relationship whose interval does not cross zero"
  and filed it as small but not nothing. **That was wrong, and the reason it was wrong is the next
  paragraph.**

**The interval criterion is not reliable at these widths, and the control proves it.**
`neutral → intake rate` in the late window has a 95% interval of **[+0.008, +0.033]**, which excludes
zero. `NeutralMarker` cannot influence anything; the simulation contains no reader for it. So an
interval excluding zero, here, is worth exactly nothing on its own. This milestone has now reported
somewhere upwards of thirty window x relationship x arm intervals; **at these widths some will exclude
zero by chance, and at least one demonstrably did.** Direction consistency against the committed 0.75,
with the marker reported beside it, is the criterion that should be read.

### The U-shape count: the density-dependent reading is not corroborated, and is withdrawn

The previous pass recorded the valley as a density-dependent effect resting on pooled bin means, and
named the statistic that would corroborate it. That statistic has now been run - in how many worlds
does the 0.6-0.8 bin mean fall below **both** end bins - and judged against the same 0.75:

| window | predictor | worlds with a valley | of | fraction | verdict |
|---|---|---|---|---|---|
| early | diet | 10 | 22 | 0.455 | **fails** |
| late | diet | 5 | 22 | 0.227 | fails |
| early | neutral marker | - | 0 | - | **not computable** |
| late | neutral marker | - | 0 | - | **not computable** |

Two worlds in each window are unjudgeable because an end bin was empty.

**Fewer than half the worlds show the valley.** The 8.2% dip in the early-window bin table is
therefore a **pooled-mean feature, not a per-world one** - which is precisely the failure mode this
document warns about everywhere else, and the pooled figure is labelled pseudo-replicated for exactly
this reason. **The density-dependent reading of the valley is withdrawn.** What remains true is
narrower and still worth having: the pooled early-window bin means show a dip that the pooled
late-window means do not, and no more than that.

### The missing null is a result in its own right

**The U-shape control could not be computed in a single world of 24, and that is a finding rather than
a blank cell.** Binning creatures by `NeutralMarker` needs both end bins populated; in no world were
they. The reason is the one this project already recorded from the other direction: **the marker
drifts to fixation independently per world**, so a world's creatures pile into one or two adjacent
bins and the 0.0-0.2 and 0.8-1.0 bins cannot both be occupied.

**That independently confirms the drift-to-fixation result of
`p3-digestion-strategies-2026-08-30.md`.** That document established it from the distribution across
worlds — mean 0.511-0.522 against a standard deviation of 0.274-0.303, a U-shaped histogram, and a
per-run hunter share running the whole 0% to 100% range. This is the same fact arriving as a
*mechanical failure of a binning operation*, in a different cell, at three times the run length, with
a different instrument. Two independent routes to the same conclusion is worth more than either.

**It is also a hard constraint on the Task 11 drift plan, and it retires one of the four options.**
The plan's **option 2 — "neutral-marker standing variation", tracking within-world variance of the
marker over time — has no standing variation to work with at this run length.** By 36,000 ticks each
world holds essentially one marker value. Within-world variance is not merely noisy there; the
quantity the method proposes to track has gone to approximately zero, and a method whose signal is
gone cannot be rescued by more seeds. The plan already rated option 2 weak on the grounds that one
locus is one realisation of a stochastic process. **This is a stronger and more concrete objection
than that one**, it is measured rather than argued, and it should be read as retiring the option for
this cell family rather than merely discounting it.

It leaves option 4 — synthetic markers replayed offline over the recorded pedigree — as the
recommendation it already was, and now for a second reason: a replayed locus can be given whatever
standing variation the question needs, precisely because it is not subject to the drift that erased
the real marker's.

**So the U-shape count is a fair statistic for the shape of the claim and an uncontrolled one**, and
its failure against 0.75 is being read without a null beside it. The reason no null exists is
biological, not an oversight.

### Calibration: what an interval excluding zero is worth in this design

Recorded here as a property of the analysis layer rather than as a caveat on two findings, because it
is reusable and the next person to report an interval from this harness should have it.

> **`NeutralMarker` → gross intake rate, late window, 24 worlds, 36,000 ticks: mean per-world
> r +0.020, 95% bootstrap interval [+0.008, +0.033], excluding zero.**

`NeutralMarker` is read by **no** behaviour code and is pinned dead by `LivenessTests` under the widest
available configuration. There is no causal path from it to intake. **So this is a measurement of the
false-positive behaviour of this design at this sample size** — 24 worlds, cohorts of a few hundred to
a few thousand creatures, 2,000 bootstrap resamples on per-world correlations — and the number it
returns is: **an interval excluding zero is compatible with an effect size of exactly nothing.**

Two things follow for anyone using this harness:

- **Do not accept a result on an interval alone.** Read direction consistency against the committed
  `PairedEvolutionCriterion.MinimumDirectionConsistency` of 0.75, with the neutral marker reported
  beside it. The marker fails 0.75 here (0.708) even while its interval excludes zero, which is
  exactly the discrimination the threshold exists to provide.
- **Count the intervals.** This milestone has reported upwards of thirty window x relationship x arm
  intervals. At these widths some will exclude zero by chance, and at least one demonstrably did.

The magnitude is worth keeping too: the inert channel's r is **+0.020**, so anything in this design of
that order should be treated as within the structural noise floor, whatever its interval says. The one
surviving positive result of this milestone, `diet -> intake rate` in the late window at **+0.072**
with 0.875 consistency, is three and a half times that floor and clears it — which is the comparison
that makes it a result rather than the interval that accompanies it.

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
- **The 12% intake valley was not** — at 36,000 ticks. Its disappearance is the predeclared
  prediction that failed. **Four follow-up runs settled why.** The valley is present at 12,000 ticks
  under both instruments (8.2% measured at the allocation site, 2.9% through the delta proxy) and
  absent from the pooled 36,000-tick table; splitting that same 36,000-tick cohort on birth tick then
  showed the valley **present in births before tick 6,600 and absent after**. So the variable is
  **the phase of the population, not the run length** — a density-dependent effect, present during
  expansion and gone afterwards. The proxy attenuates the valley rather than inventing it, and 8 seeds
  inflated its depth from a true 8.2% to a reported 11.7%. **The per-world sign counts do not yet
  corroborate the density-dependent reading**, because a correlation cannot see a non-monotone shape;
  the per-world U-shape test that would has not been run.
- **The old instrument under-counts ingestion by 21-27% in this cell** and cannot see 12.7% of it at
  all, for a reason that is structural rather than incidental.
- **The capacity clamp is not the blocker.** It discards under a third of one percent.
- **About half of all reproductive blocking is a refractory period**, whose duration is itself set by
  the creature's condition at its last birth rather than by a fixed timer.
- **The cap is not binding in this cell**, so skew readings from it are interpretable.

## What this does not establish

- **It does not establish that this cell is a valid long-run ecology.** It is not. 21-23 of 24 worlds
  are extinct at 36,000 ticks. Whatever this cell is measuring after about tick 12,000, it is not a
  persistent population.
- **It does not say when the collapse happens.** No per-world extinction tick was recorded; that
  needs another run. See the transient record for what would settle it.
- **It does not say the trade-off is absent from the source.** `PlantFoodYieldMultiplier` is still
  `1 - 0.3 * diet`. The trade-off exists; it does not reach ingestion at the rate the old instrument
  reported, and it does not reach fitness at all.

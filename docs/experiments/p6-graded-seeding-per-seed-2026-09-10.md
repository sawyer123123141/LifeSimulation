# Per-seed structure of the graded-seeding arm at 24,000 ticks

**Date:** 2026-09-10.
**Status: EXPLORATORY.** This is not a confirmatory result and no criterion was predeclared for it.
It exists to **design** the 72,000-tick confirmatory extension, and every number in it is therefore
hypothesis-generating. Nothing here may be cited as evidence that A2 works.

**Data:** the two committed longitudinal files, produced by the reporting change in `6edac7a`:

- `p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-24000ticks-9samples-2026-09-10.csv`
- `p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-gradedseeding-24000ticks-9samples-2026-09-10.csv`

**Reproduction check, first.** Both arms were re-run under the reporting build. The population and
plant trajectory tables are **byte-identical** to
`p6-graded-seeding-control-24seeds-24000-2026-09-09.txt` and
`p6-graded-seeding-arm-24seeds-24000-2026-09-09.txt`, and the legacy survivor-conditioned line
reproduces exactly (`mean 255.7 min 32 median 236 max 500 sd 165.34`). The reporting change moved
nothing.

**The verdict is unchanged and is not revisited here.** `alive(9) = 20 of 24` against
`ceil(0.85 x 24) = 21`: **COLLAPSING**, per the 2026-09-09 ruling.

---

## 1. The distributions, both of them

| | n | mean | min | q1 | median | q3 | max | sd |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| **arm, ALL WORLDS** (extinct = 0) | 24 | 213.0 | 0 | 51 | 215 | 361 | 500 | 179.0 |
| **arm, SURVIVORS ONLY** | 20 | 255.7 | 32 | 99 | 235.5 | 408 | 500 | 165.3 |
| **control, ALL WORLDS** | 24 | 0.0 | 0 | 0 | 0 | 0 | 0 | 0.00 |

The two arm rows differ by 43 in the mean and by 51 in the minimum reported as non-zero. Quoting the
survivor row alone is what made this cell look healthier than it is, and it is the reading every
artefact before today offered.

## 2. Paired per-seed differences

The control is extinct in **all 24** worlds, so the paired difference is simply the arm's final
population.

| | value |
|---|---|
| improved | **20 of 24** |
| unchanged (both zero) | 4 of 24 — seeds 42, 50, 55, 63 |
| **worse** | **0 of 24** |
| paired difference, all worlds | mean **+213.0**, sd 179.0, min +0, q1 +51, median **+215**, q3 +361, max +500 |

**No world was harmed.** The "actively harmful" falsifier from the arm's predeclaration is refuted
per seed, not merely on the aggregate.

## 3. Which seeds survived

**Survivors (20):** 43, 44, 45, 46, 47, 48, 49, 51, 52, 53, 54, 56, 57, 58, 59, 60, 61, 62, 64, 65.
**Casualties (4):** 42, 50, 55, 63.

Seed 55 is an **establishment failure**, not a collapse: its series is `32 18 3 0 0 0 0 0 0` — it never
grew. The other three peaked and then crashed. That distinction matters for the extension, because an
establishment failure cannot recur later in a longer run and a crash can.

## 4. Cap contact is small and does not carry the result

**Three worlds of 24 touch the cap of 500 at any sample** — seed 48 at two samples, seeds 52 and 56 at
one each. That is **3 of 20 survivors, 15%**, under the quarter that the draft extension names as
confounding.

| | all-world mean | survivors |
|---|---:|---:|
| with cap-contacting worlds | 213.0 | 20 of 24 |
| excluding them | 172.0 | 17 of 24 |

**Excluding them does not move the verdict**: 17 also fails the threshold of 21. The near-miss at 20
is therefore **not** a cap artefact. Removing three worlds lowers the mean by 19%, which is worth
carrying as a caveat on any *level*, and changes nothing about the *outcome*.

## 5. The finding that reframes everything: results consistent with similar oscillatory dynamics sampled at different phases

Three independent readings, all pointing the same way. **They are consistent with similar
oscillatory dynamics sampled at different phases. They do not establish that every world runs the same
oscillator**, and nothing below should be read as a claim that the worlds share a period, an amplitude
or a mechanism - only that each of them cycles, and that at any one instant they are not in step.

**Every surviving world oscillates.** Each has between two and four turning points in nine samples.
Representative series:

```
seed 48   37   86  288  499  101  108  474  500  500
seed 52   42   98  181  323  286   32   48  172  500
seed 62   40   54  103  201  419  494   45   69  276
seed 58   34   46  121  279  194  174  499   53   32
```

**13 of the 20 survivors have already fallen to a quarter of their peak or below and recovered.** The
crash A2 was predicted not to prevent does happen, in most worlds, and is then survived.

**At the horizon the worlds split evenly by phase: 11 rising, 8 falling, 1 flat.** A population
settling toward an equilibrium does not do that; a set of worlds each cycling on its own schedule
does. That is evidence against a common approach to equilibrium, not evidence for a common period.

**Final population and final plant biomass are inversely correlated, r = -0.716 over 20 worlds.** The
worlds holding the most animals are the ones that have just eaten their plants — a predator-prey phase
relationship, read across worlds instead of across time.

| survivors grouped by final population | n | mean biomass | mean seed-eligible patches |
|---|---:|---:|---:|
| small, under 100 | 5 | 1560 | 21.4 |
| middle, 100-350 | 9 | 836 | 11.3 |
| large, over 350 | 6 | 369 | 4.5 |

**These are more readily explained as phases than as regimes**, though the data cannot exclude
genuinely different regimes that happen to order the same way. Patch count is ~23 in every world,
survivors and casualties alike; what differs is how much of it has just been eaten.

**Observed peak-to-peak spacing: 8,000 to 10,667 ticks, mean 8,333** (n = 8 spacings, pooled
across worlds - a per-world period is not established and the spread here may be between-world
variation rather than measurement noise). This is
measured on a 2,666-tick grid over a nine-sample window, so it is biased toward the short end — long
periods cannot appear inside the run — but it is direct evidence that the period is of order 8,000 to
11,000 ticks rather than 20,000.

## 6. This resolves the anomaly the arm document recorded and could not explain

The arm reported interval starvation of 33-49% in **every** sample of its last third and never falling
back — the signature the design document assigns to contest allocation and territoriality, not to A2,
and the falsifier the Regime B triage predeclared that no cell had ever met.

Per seed it is not chronic at all. It is **episodic and phase-locked**:

| phase at the horizon | n | mean starvation share, final interval | range |
|---|---:|---:|---|
| rising | 11 | **3.8%** | 0.0% - 41.5% |
| falling | 8 | **58.2%** | 0.0% - 78.9% |

Worlds in recovery are barely starving; worlds in crash are starving heavily. The aggregate showed a
constant third-to-a-half because at every sample **some** subset of worlds is crashing. **The anomaly
was an artefact of pooling worlds at different phases**, and no new mechanism is needed to explain it.

That also withdraws the speculation in the arm document that this might be emergent density-dependent
mortality. It is not. It is a boom-bust cycle, sampled across worlds.

## 7. Is the 20 of 24 broadly distributed or driven by outliers?

**Broadly distributed on survival, moderately concentrated on level.** 20 of 24 worlds surviving cannot
be produced by outliers. The top three worlds hold 29.3% of the total final population against 12.5%
under a uniform split, and six worlds are needed to reach half the total, so the *level* has a right
tail — largely the three cap-contacting worlds.

## 8. What this changes about the extension, and what it does not

**Changes:** the extension's central question is no longer "does it persist" in isolation. It is
**whether the cycling is sustained, damped, or divergent**, because a 24,000-tick snapshot catches each
world at whatever phase it happens to be in and cannot answer it. The period estimate of 8,000-11,000 ticks makes 72,000 ticks roughly
seven to nine cycles, which is ample — and, honestly, means the 48,000 originally proposed would also
have sufficed. The earlier lower-bound estimate of 11,000 ticks was derived from the all-world mean,
which smears worlds at different phases together and biases the apparent period upward.

**Does not change:** the verdict, the threshold, or the need for the food-supply control. And it
sharpens one warning. **Eight of the twenty survivors are in steep decline at the horizon**, five of
them with starvation above 69% of deaths in the final interval. Those are the next casualties, and any
reading of 20 of 24 as a standing population ignores that half the survivors are mid-crash.

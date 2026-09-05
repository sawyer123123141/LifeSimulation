# The contest and join nulls survive an unpinned population — re-run at cap 250, brake 1.0

> **RUN-LENGTH BANNER, 2026-09-05 - the nulls hold; "unpinned, healthy, and stable" does not.** This
> cell (cap 250, brake 1.0) was re-run at **24,000 ticks and 24 seeds** and keeps **3, 7, 8 and 6 of 24
> worlds** across its four contest x terrain arms, from 22-24 at one third against thresholds of
> 18.7-20.4, with starvation reaching 41-64% of deaths in the crash intervals. Some seeds lose the
> plant community outright (occupancy 0.0). The run had the power to return PERSISTENT for any arm and
> returned it for none.
> **The contest null and the join null are unaffected**: they are paired arm differences, and a shared
> downward trajectory is common to both arms. What is withdrawn is the description of the cell - the
> population settling at 63-67 with 10-15% extinction is the top of a boom, not an unpinned
> equilibrium, so the qualification this document claimed to remove has changed shape rather than
> gone. A null measured in a collapsing cell is a weaker null than one measured in a persistent cell,
> and this document carries no positive control through it. `p6-regime-b-triage-2026-09-05.md`,
> ledger row C5.

**2026-08-26.** `dotnet run --project tools/PlantSweep -c Release -- 60 --cap=250 --brake=1.0`,
12,000 ticks, 240 runs (4 cells x 60 seeds). Raw: `p6-plant-cap250-brake1.0-60seeds-2026-08-26.csv`,
console `…-2026-08-26.txt`.

`p6-graded-fertility-is-scenario-specific-2026-08-24.md` closed with the work this doc does: every
plant result on record was measured with the herbivore population **pinned** at the cap — the scope
qualification the whole plant corpus carries — and brake 1.0 at cap 250 is the first configuration in
which the population is unpinned, healthy, and stable. The earlier unpinned check used the
**confounded** arm and was explicitly not a re-validation. This is the re-run.

**Scope: the establishment-contest and terrain-join comparisons only.** Those are what `PlantSweep`
measures. The other corpora are untouched and still carry the qualification.

## The four cells

| cell | extinct | frozen | occupancy | population |
|---|---|---|---|---|
| contest-off / flat | 8 / 60 | **0 / 60** | 0.872 | 66.5 |
| contest-off / terrain | 9 / 60 | **0 / 60** | 0.903 | 63.0 |
| contest-on / flat | 7 / 60 | **0 / 60** | 0.887 | 63.8 |
| contest-on / terrain | 6 / 60 | **0 / 60** | 0.878 | 67.2 |

Population 63–67 under a cap of 250, no frozen worlds anywhere, extinction 10–15%. That reproduces
the 40-seed tuning result (70.9, 0 frozen, 5/40) at 60 seeds in all four cells rather than one.

## The instrument can still see selection

The reason the nulls below are readable at all. Drift from founders is large and consistent in every
cell — Dispersal **t +8.18 to +11.28**, SeedInvestment **+6.10 to +7.49**, Growth **+5.38 to +6.52**,
Defense **+4.27 to +5.66**, and against them TemperatureTolerance **-3.41 to -4.49** and
NutrientUptake **-2.18 to -3.93**. **A null from an instrument reading zero everywhere would mean
nothing; this one is reading ±11 in the same runs.**

## The comparisons

**Establishment contest, paired on-off.** Twenty-two columns, **every |t| ≤ 1.64**; flat tops out at
1.01 (Dispersal), terrain at 1.64 (NutrientUptake). Null.

**The join, paired terrain minus flat.** Twenty-two columns, **every |t| ≤ 1.99**. Null.

Against the record: pinned was all |t| < 1.3, the confounded unpinned arm all |t| < 2.4. **Brake 1.0
lands between them and is null on the same reading.**

## What this licenses, and what it does not

- **The contest null and the join null are no longer conditional on a pinned population.** They hold
  with the population settling at a quarter of the cap with real variance. That is the qualification
  lifted for these two comparisons.
- **It is not lifted for the corpus at large.** Nine other corpora were measured pinned and are not
  re-run here.

## One thing to test rather than claim

`MoistureTolerance` is the only trait where the join looks like anything: **+0.0454 (t +1.87)**
contest-off and **+0.0482 (t +1.99)** contest-on, and the within-cell drift agrees — selection against
it is **-0.058 / -0.071 (t -3.6 / -3.9) on the flat field and -0.012 / -0.023 (t -0.64 / -1.42) under
terrain.** The terrain field appears to relax the cost of moisture tolerance, which is the one
mechanism a moisture-carrying field ought to move.

**This is one observation, not two.** The contest-on and contest-off arms share seeds and the same
terrain field, so their agreement is correlation, not replication — a single column at |t| ≈ 1.9 out of
eleven is what chance produces, which is exactly the reading given to the 2.35 in the confounded arm.
**Deciding it needs a fresh seed block**, not a re-reading of this one.

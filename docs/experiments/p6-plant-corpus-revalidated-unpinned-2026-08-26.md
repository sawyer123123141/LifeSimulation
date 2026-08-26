# The contest and join nulls survive an unpinned population — re-run at cap 250, brake 1.0

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

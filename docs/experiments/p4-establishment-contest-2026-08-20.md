# The Establishment Contest: A Plant Trait Selected Off the Growth-Rate Gate — 2026-08-20

> **REQUIRES RE-MEASUREMENT — 2026-08-22.** This result was measured with
> `PlantSiteCompetitionEnabled` on, before the `PlantPatchStore.ReplaceAt` takeover-age defect was
> fixed in `4cc9a47`. Until that fix, a seedling installed by a takeover inherited the incumbent's
> accumulated age and was frequently aged out within a tick or two of establishing — on the exact
> mechanism this document draws conclusions about. A paired old-versus-fixed audit over 30 seeds
> found every state hash on the competition path changed, `SeedlingResilience` moving **downward**
> under the fix (t -1.99, 19 of 30 seeds down) and plant generations falling by one (t -2.63).
>
> Those figures are drift magnitudes measured on the uniform-founder calibration, so they do **not**
> overturn a selection result measured at t +4.03 under standing variance — but they push against it
> on the mechanism it concerns, so the conclusion is neither retracted nor confirmed. **Re-run this
> experiment's original design before relying on it.** Nothing here has been edited or recomputed;
> the numbers below are the original measurements.
> Audit: `evidence-impact-audit-2026-08-22.md`.


> **SUPERSEDED IN PART 2026-08-20.** The positive result is specific to the scarce 24-site operating point. At 168 sites, `SeedlingResilience` declines (t −2.56, 44/120 up) as its dispersal charge remains while contests become less valuable. See `p4-low-occupancy-plant-route-audit-2026-08-20.md`.

Follows directly from `p4-where-plant-fitness-is-decided-2026-08-20.md`, which measured that
51.9% of the variance in per-patch lifetime offspring is the single binary of whether a newborn
survives site-competition takeover, and that **no gene influenced that outcome above |r| = 0.11**.

This wires one gene into that channel and answers the P4 exit-gate question:
**yes — plant selection has a route that is not growth rate, and it is establishment.**

## What was built

`SimulationConfig.PlantEstablishmentContestEnabled`, defaulting `false`. With it set, a takeover
attempt against a seedling below `VulnerabilityFraction` becomes a contest the incumbent can win:

```
if (establishmentContestEnabled)
{
    float contestRoll = DeterministicRandom.Float01(seed, RandomDomain.PlantEstablishmentContest, ...);
    if (contestRoll < occupant.Genome.SeedlingResilience) continue;
}
```

`PlantGenome.SeedlingResilience` is the tenth plant trait, given the full tenth-parameter
treatment — constructor, `CloneMutated`, `ToTraits`/`FromTraits`, `TraitNames`, `TraitCount`,
`ComputeStateHash`, and the transmission test that pins all ten.

Its own random domain (`RandomDomain.PlantEstablishmentContest`) rather than a reuse of
`PlantEstablishment`, so the contest roll cannot correlate with the distance roll beside it.

**The charge is on dispersal range, not growth rate.** A growth-rate charge is multiplied by
`(1 - Biomass/Capacity)`, measured mean 0.1711, and is therefore almost free — which is why six
plant traits routed through `GrowthRateMultiplier` have measured null. `DispersalRange` is the
strongest measured fitness channel in the model, so a charge there is a trade-off that bites.

## It reaches the seedling

Same 30-seed decomposition probe, contest on, one variable changed:

| | contest off | contest on |
|---|---:|---:|
| takeover rate among non-founders | 0.355 | **0.205** |
| plant births per run | 218.9 | 166.6 |
| takeovers per run | 64.0 | 28.7 |
| mean site occupancy of 24 | 21.86 | 21.05 |
| extinctions | 0/30 | 0/30 |

`r(SeedlingResilience, taken over) = **-0.108**` in the contest arm — correctly signed, and the
gene did not exist in the control arm so there is nothing to compare it against there. It is not
the only trait correlated with infant death: `Dispersal` reads +0.116 and `Growth` -0.083 in the
same arm, both incidental and both present before the contest existed. What changed is that the
takeover rate itself fell by 42%.

## Pricing the trade-off

120 seeds per arm, 12,000 ticks, founders varied with a per-seed centre plus per-site jitter.
`Dispersal` is the positive control in every arm.

| dispersal charge | delta | t | seeds up | Dispersal control | extinct |
|---:|---:|---:|---:|---:|---:|
| 0 | +0.0455 | **+6.24** | 85/120 | t +17.14, 112/120 | 0/120 |
| **2** | **+0.0257** | **+4.03** | **76/120** | t +19.64, 119/120 | 0/120 |
| 6 | -0.0147 | -2.10 | **48/120** | t +17.41, 114/120 | 0/120 |

**Charge 2 ships.** At 0 the gene rises with no trade-off at all, which is a gene that sweeps
rather than a gene that is selected. At 6 the dispersal loss overwhelms the benefit and the trait
declines — and note the sign test, 48/120 up, which is the exact shape of the magnitude-driven
null retracted on 2026-08-18: a `t` of -2.10 there means nothing. At 2 the trait rises at t +4.03
with 76/120 seeds up, comparable to `SeedInvestment` in the same arm (t +3.83, 72/120), while
paying a real price.

For the same arm, every trait routed through `GrowthRateMultiplier` stays null as always:
`Growth` t -0.99, `Nutrition` t +0.63, `Defense` t -0.24, `WaterEfficiency` t +0.61,
both tolerances \|t\| < 1.3, `NutrientUptake` t -0.16.

Data: `p4-establishment-contest-cost0-2026-08-20.csv`, `-cost2-`, and
`-cost6-and-control-` (that last file carries the flag-off control arm alongside charge 6).

## Predictions made before implementing, and how they did

| | prediction | measured | |
|---|---|---|---|
| W1 | takeover rate falls from 0.355 to 0.15-0.20 | 0.205 | held |
| W2 | delta > +0.05, t > +6, > 85/120 up | -0.0147, t -2.10, 48/120 | **refuted at the charge predicted** |
| W3 | Dispersal control stays t 14-17 | t 17.4-19.6 | held |
| W4 | flag-off byte-identical | 395/395 green, pinned hashes unmoved | held |

W2 was wrong because the *price* was wrong, not because the channel was. The value predicted for
t at charge 6 turned out to be roughly the value measured at charge 0 — the whole prediction was
displaced by one calibration step. That is worth recording precisely because the diagnosis
"benefit channel is fine, cost is mispriced" is only available when the cost is swept; a single
arm at charge 6 would have been written up as another plant-trait null, and it would have been
the fourth in a row and wrong.

## What this does and does not establish

**Does:** a plant trait can be selected in this model without touching growth rate, at an effect
size between `SeedInvestment` and `Dispersal`, with a real cost, at zero extinctions and ~15
plant generations. The P4 blocker is answered.

**Does not:** make the establishment channel large. The 51.9% variance share is not 51.9% of
*selective opportunity* — most of being taken over stays luck. Resilience shifts your odds by
about 0.1 in correlation, not by a lot more, because a doomed seedling faces repeated attempts
and the invader simply retries other sites.

**Do not treat "share of variance" as "available selection" again.** That was the error inside
W2: the variance share was read as if the whole 51.9% were up for grabs, and the cost was priced
against that imagined payoff.

## Not done

- The contest is one-sided. Only the incumbent's genome enters; the invader's does not. An
  invader-side term is the obvious next question and was left out to keep this one variable.
- `ReproductionCooldownSeconds = 20f` is still a hard-coded constant, so seeding *rate* still has
  no genetic channel. That is the untouched third route.

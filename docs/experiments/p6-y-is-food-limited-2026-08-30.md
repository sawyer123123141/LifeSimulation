# `Y` is now limited by its food instead of by a number

**Date:** 2026-08-30
**Status:** applied to `Y`. `maximumPopulation` 96 → 500, `gradedFertilityEnabled` on at strength
**0.75**. Scenario id `p6-terrain-playtest-split4-braked`.
**Harness:** `tools/SitePilot`, whose control arm is fingerprint-identical to the old `Y`.

## Why

The goal is a world that does not need a population cap, because a world does not have one. The cap
was doing that job: `docs/ROADMAP.md` records *"the population cap is load-bearing ecology, not a
safety limit"*, and `gradedFertilityEnabled` was built as its replacement — commit `d663ac7`,
*"a carrying capacity at last; the oldest debt closes."*

It also unblocks a P4a acceptance item. The gate asks that a player can see *"resource
depletion/recovery"*, and at cap 96 nothing was ever hungry — Phase I records the death mix at cap
100 as **96.9% age, 8 starvations of 5,619**, and patches sat at 88% full with nothing to watch.

## The sweep, 24 seeds, cap 500

Cap 500 is effectively uncapped once the brake binds — the recorded dial has cap 500 and cap 1000 as
the same ecology, and the populations below settle far under 500.

| brake | alive | population | **starvation** | **mean patch fill** | mean energy | clump index |
|---|---|---|---|---|---|---|
| *shipped `Y`, cap 96* | 21/24 | 95.7 | 0.0% | 0.876 | 0.794 | 0.488 |
| none | 16/24 | 96.4 | **43.1%** | 0.359 | 0.435 | 0.355 |
| 0.5 | 17/24 | 138.4 | 15.9% | 0.458 | 0.475 | 0.475 |
| **0.75** | **20/24** | **154.1** | **5.4%** | **0.563** | 0.602 | 0.516 |
| 1.0 | 20/24 | 133.4 | 0.7% | 0.647 | 0.651 | 0.521 |
| 1.5 | 16/24 | 106.3 | 0.3% | 0.789 | 0.770 | 0.552 |

**Starvation is a clean monotone dial** — 43.1 / 15.9 / 5.4 / 0.7 / 0.3 — reproducing the recorded
finding in a configuration it had never been measured in.

**Survival is not monotone: 16 / 17 / 20 / 20 / 16.** Strength 1.5 is both safer-sounding and worse
than 0.75, which is the recorded *"brake strength does not transfer"*. That is why 0.75 was chosen by
measurement rather than by picking a round number.

**0.75 is the point that meets the goal.** The population self-limits at **154 under a cap of 500 that
never binds**, survival is 20 of 24 against the shipped 21 of 24, and 5.4% of deaths are starvation.
Clumping is unaffected. It is harsher: mean energy 0.794 → 0.602.

## What the render actually showed, which is less than the average

Rendered at seed 42 through `CaptureSitePilot`: population **56**, mean energy **0.803**, and food
sites **median fill 1.00 with 34 of 39 above 75%**.

**That is a well-fed world, not a hungry one.** Seed 42 is not representative of the 24-seed mean —
the aggregate says fill 0.563 and this seed says nearly full. The change is supported by the sweep,
**not by this picture**, and anyone wanting to look at a food-limited world should render a seed
picked for that rather than assume seed 42 shows it.

This is recorded rather than quietly re-rendered because the discrepancy is the interesting part: the
brake's effect has high between-world variance, and a single capture can show the opposite of the
mean.

## How to undo it

Two values in `Prototype1Presenter.ResetTerrainPlaytest`: `maximumPopulation` back to 96, and remove
`gradedFertilityEnabled` / `gradedFertilityStrength`. Nothing else depends on them.

## What this does not claim

- Not that `Y` is uncapped. Cap 500 is still a number; it simply never binds at a settling population
  of 154. A genuinely capless run has not been measured.
- Not that 0.75 is optimal. It is the best of five points at 24 seeds, and survival differences of
  20 vs 21 of 24 are not separable at that count.
- Not that starvation at 5.4% makes food-related genes selectable. The digestion work
  (`p3-digestion-strategies-2026-08-30.md`) found intake swamped by lifespan variance at 92.7% age
  deaths; this moves age deaths to about 94% of a different mix and nothing here re-measures diet.

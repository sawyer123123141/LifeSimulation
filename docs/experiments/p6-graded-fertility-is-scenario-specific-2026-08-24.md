# The brake works, but its strength is a tuning parameter — 3 stabilises one ecology and destroys another

**2026-08-24.** `tools/PlantSweep <seeds> [--cap=N] [--brake=X]`, 12,000 ticks.

`p6-graded-fertility-closes-the-cap-debt-2026-08-24.md` showed a graded fertility brake turning
boom-and-collapse into a carrying capacity, and said so *"at every cap tried"*. **That was one
scenario family.** This is what happened on the second one, and it qualifies the claim.

## First, an error of mine

The first attempt compared the recorded plant condition (cap 48, no brake) against **cap 250 with the
brake**, changing two things at once. Separating them was necessary and is the only reason the rest of
this is readable.

| plant sweep, contest-on / terrain-driven | population | extinct | frozen | occupancy |
|---|---|---|---|---|
| cap 48, no brake — **the recorded condition** | 40.8 | 6 / 60 | 0 / 60 | 0.933 |
| cap 250, **no brake** | 80.7 | 11 / 60 | 7 / 60 | 0.696 |
| cap 250, **brake 3.0** | **10.0** | **21 / 60** | 0 / 60 | 0.945 |

**Raising the cap alone raises the population**, as it should. **The brake at strength 3 is what
collapses it** — to a tenth of the unbraked population, with a third of worlds dying. The confound
was mine; the result underneath it is real.

## The dose-response

`GradedFertilityStrength` was an untuned first guess. Made configurable and swept, at cap 250:

| brake strength | extinct | frozen | occupancy | population |
|---|---|---|---|---|
| 0 (none) | 11 / 60 | 7 / 60 | 0.696 | 80.7 |
| 0.5 | 6 / 40 | 1 / 40 | 0.822 | 76.5 |
| **1.0** | **5 / 40** | **0 / 40** | **0.871** | **70.9** |
| 1.5 | 9 / 40 | 0 / 40 | 0.939 | 42.3 |
| 3.0 | 21 / 60 | 0 / 60 | 0.945 | 10.0 |

**Strength 1.0 is the best point in this ecology on every axis at once:** the lowest extinction of any
condition including no brake (5 of 40 against 11 of 60), **no frozen worlds** against 7 without it,
and a population of 70.9 sitting comfortably under a cap of 250 rather than pinned at 48.

Occupancy rising with strength is the mechanism visible from the plant side — fewer grazers, more
standing forage.

## What this means for the earlier claim

**The mechanism generalises. The constant does not.**

- **Resource-backed calibration scenario:** strength 3 produces a carrying capacity — survival 3 of
  20 to 19 of 20, starvation to exactly zero.
- **Plant-backed full ecosystem:** strength 3 is catastrophic; **strength 1 is the equivalent
  result** — lower extinction than no brake at all, no frozen worlds, unpinned population.

A factor of three in the strength is the difference between the best and the worst condition tested.
**That makes it a parameter, not a constant**, which is why it is now in `SimulationConfig` and hashed
rather than a `const` — the same argument that applied to the reproduction gate.

## Why the two ecologies want different strengths — a hypothesis

Plants are patchy and their quality varies, so condition in the plant-backed world likely sits lower
and more variably than against regenerating point resources. The brake keys off exactly that, so the
same curve bites much harder there. **Untested** — the measurement would be the distribution of the
binding-need condition in each scenario, which nothing currently reports.

## Status

**Default strength stays 3.0** and `gradedFertilityEnabled` stays false, so nothing recorded moves.
**Anyone enabling the brake must choose a strength for their scenario and measure it** — that is the
finding, and taking the default into a new ecology is exactly the mistake this doc exists to prevent.

## Consequence for the plant corpus

The scope qualification — every plant result measured with the herbivore population pinned — **now has
a working configuration to be re-tested in**: cap 250 with brake 1.0, where the population is
unpinned, healthier than the unbraked alternative, and stable.

**The contest and join comparisons at 60 seeds are null in both the pinned and the unpinned arms**
(all |t| < 1.3 pinned, all |t| < 2.4 unpinned with one column of eleven at 2.35, which is what chance
produces). That is consistent with the recorded null and is **not yet a re-validation** — those runs
used the confounded arm, and the corpus should be re-run at brake 1.0 before anything is claimed.

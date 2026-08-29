# Fleeing is selected against, and making it work does not fix that

2026-08-29. A mechanism built, verified, measured, and **found not to achieve its purpose** - with
the reason established. Also **a correction to
`emergent-behaviour-the-gradient-is-on-armour-2026-08-26.md`, which named the wrong gene.**

## The correction first

That document claimed: `FearResponse` is the gene scaling the Flee decision, it crosses |t| = 2 in
1 of 18 cells against the control's 2, therefore **the behavioural knob is unselected** and selection
took the armour route because behaviour had no route.

**The gene was wrong, and so was the conclusion.**

Grep of every live reader:

| gene | read by | on which path |
|---|---|---|
| `RiskAversion` | flee score (`DecisionSystem.Scoring.cs:96`, `:143`), foraging danger penalty (`:287`) | **`IntentUtilityV1` - the live path** |
| `FearResponse` | `PredationSystem.Decide:43` | **`Legacy` only** |
| `FearResponse` | `ForagingEconomics:154`, remembered-threat penalty | place memory, **inert by standing decision** |

Every cell in that document ran `IntentUtilityV1`. **Under that controller `FearResponse` is not read
by any code that executes** - it is a second neutral marker, the same situation as
`urgency_exponent` under `Legacy`. Its 1-of-18 was not evidence about fleeing; it was evidence that
nothing reads it.

**The flee knob on the live path is `risk_aversion`, and it is under strong selection - negative.**
Across the same twenty-two powered cells:

| gene | role | \|t\| > 2 in | range |
|---|---|---|---|
| `defense` | passive armour, no decision reads it | **22 / 22** | +2.48 to +12.81 |
| `risk_aversion` | **the flee knob** | **20 / 22** | **-5.92 to +0.16** |
| `fear` | nothing, on this path | 1 / 22 | -0.36 to +2.13 |
| `neutral_marker` | the control | 3 / 22 | +0.04 to +2.68 |

**The corrected finding is sharper than the wrong one.** It is not that the world ignores behaviour.
**The world has a strong opinion about defensive behaviour and the opinion is "don't".** Caution is
being bred out at t = -3 to -6 while armour is bred in at t = +11.

## The mechanism that was built

`evasiveFleeingEnabled`, default false, flag-off byte-identical, 619 tests green (8 new).

Combat resolution read the defender's stats and never its decision - a creature grazing obliviously
was hit exactly as often as one running for its life. The flag adds the one place a defender's
*choice* reaches combat:

```
hitChance = 0.20 + 0.70 * Threat(attacker, defender, distance)
if (evasiveFleeing && defender's action == Flee)
    hitChance *= 1 / (1 + strength * defender.Maneuverability)
```

Applied at resolution and deliberately **not** inside `PredationSystem.Threat`, which also feeds the
decision path - folding it in there would make a fleeing creature perceive less threat and stop
fleeing, a feedback loop rather than a gradient.

**A calibration bug the unit test caught:** `Phenotype.Maneuverability` is `1 + 2 * gene` and runs
**1.0 to 3.0**, not 0 to 1. The first draft added a `0.5f` floor term on the assumption of a 0-1
gene and cut hit chance by about 70% instead of the intended 50%. No floor is needed - the phenotype
minimum is already 1, so the least agile creature alive still benefits from running.

## It does not work, and not for want of strength

Cell: cap 500, regen 2.0, brake 1.5, gate 0.45, predation, proximity pairing, `IntentUtilityV1` -
the highest-combat cell available at 276 attacks per run. Both health arms.

| arm | extinct/60 | `risk_aversion` t | `defense` t |
|---|---|---|---|
| no evasion (baseline) | 2 / 2 | -3.44 / -5.92 | +12.70 / +12.81 |
| evasion 0.5 (hit chance x0.51) | 3 / 3 | -5.64 / -4.22 | +11.73 / +11.24 |
| evasion 4.0 (hit chance x0.12) | 4 / 3 | **-6.44 / -5.09** | +11.12 / +10.23 |

**At strength 4.0 a fleeing creature is very nearly untouchable, and the flee knob is selected
against just as hard as before.** This is not a tuning problem. Eight-fold better evasion moved
nothing.

## Why - one gene doing two jobs with opposite signs

`RiskAversion` has two live roles on the intent path:

1. **`Scoring.cs:96`** - it scales the flee score. Higher means more likely to flee. **The evasion
   flag makes this pay.**
2. **`Scoring.cs:287`** - `dangerPenalty = threatIntensity * RiskAversion * (distance / maxSpeed)`,
   subtracted from every food candidate's score. Higher means avoiding food near any perceived
   threat. **This costs energy and the flag does nothing about it.**

In this cell the death mix is **44.8% starvation against 8.4% predation.** The cost of job 2 outruns
the benefit of job 1 by roughly five to one, so the gene is selected out through *caution* regardless
of how good *fleeing* becomes. **No change to combat can fix a gene that is being killed by hunger.**

The ratio is the claim's basis and it is measured; the "roughly five to one" is an order-of-magnitude
statement from the death mix, not a fitted coefficient.

## What this means for evolved behaviour

**Two routes, and they are now specific rather than hand-waving:**

1. **Split the gene.** Flee propensity and foraging caution are one number today, and the two have
   opposite fitness signs. While they share a gene, selection resolves them jointly and the larger
   pressure wins - which is hunger. Two genes would let the population evolve "flees when attacked,
   still forages boldly", which is what a real prey animal does and is currently unrepresentable.
2. **Change the mortality mix.** Caution can only pay where predation is a larger share of death
   than the foraging opportunity cost of avoiding it. That is a scenario-calibration question and the
   predation cell is already characterised, so it is measurable rather than speculative.

**Route 1 is the interesting one** because it is a *representation* limit, not a tuning limit: there
is no value of `risk_aversion` that expresses the strategy, so evolution cannot find it however long
it runs. That is a concrete instance of the general question - a behaviour that cannot emerge because
the genome cannot say it.

## Status of the flag

**Kept, default false, off in every recorded run.** It is a necessary precondition that turned out to
be insufficient, and it is now measured as such rather than assumed either way. Turning it on without
addressing `RiskAversion`'s double duty buys nothing - that is the finding, and it is written on the
flag itself in `SimulationConfig`.

## The instrument, built the same day - and it upgrades the story from inferred to measured

`FleeDecisionCount`, `DecisionCount` and `FleeingFraction` on `SimulationStatistics`. Counting only,
**deliberately not hashed**, so every number recorded before they existed stays comparable. 623 tests
green with four new, including a negative control: herbivore founders, where nothing is ever a
threat, must report **exactly zero** flee decisions.

Same cell, both arms folded in:

| arm | flee share of all decisions | predation deaths | starvation deaths | population | `risk_aversion` t |
|---|---|---|---|---|---|
| no evasion | **38.2%** | 8.4% | 44.8% | 234 | -3.44 |
| evasion 0.5 | **37.0%** | 6.0% | 45.2% | 245 | -5.64 |
| evasion 4.0 | **39.3%** | **2.3%** | 48.9% | 259 | -6.44 |

**Fleeing is not rare - it is 38% of every decision taken.** That is the cost, and it is enormous:
roughly two decisions in five are spent running instead of eating.

**The flag works exactly as designed and it does not help.** Evasion cuts predation deaths by 73%,
from 8.4% to 2.3%. Starvation rises to take up the slack, 44.8% to 48.9%. Population improves only
234 to 259, and selection against `risk_aversion` gets *stronger*, not weaker.

**The five-to-one account is confirmed and sharpened.** The cost of caution is **foraging time**, and
making flight safer does nothing about time. The benefit of caution is **bounded by how much
predation there is** - and with evasion on, that ceiling drops to 2.3% of deaths, which makes the
trade worse rather than better. **A safer flight is a cheaper flight only if flight costs lives; here
it costs meals.**

### This corrects the recommendation above

The section above calls splitting `RiskAversion` "the interesting one" of the two routes. **The
instrument says route 1 alone is insufficient.** Splitting flee propensity from foraging caution
would let the caution half evolve down while the flee half stays - but the flee half carries its own
time cost, at 38% of decisions, and its benefit is still capped by the predation share. Selection
would push both halves down for the same reason.

**Route 2 - the mortality mix - is the necessary one.** Defensive behaviour cannot pay in a
starvation-limited world, because its cost is measured in meals and its benefit is capped by a
predation rate that is currently 8% and falling. **Evolved fleeing requires predation to be a much
larger share of death than it is**, and that is a scenario-calibration question the predation cell is
already characterised for.

## Not claimed

- Flee *frequency* is still not instrumented. Everything above is inferred from gene drift and the
  death mix; no statistic reports how often creatures actually flee. **That instrument is the obvious
  next build** and it would test the five-to-one story directly.
- One cell, one controller, one scenario family.
- The two-jobs mechanism is read from the source and is consistent with the measured death mix. It
  has not been isolated by splitting the gene - which is exactly what route 1 would do.

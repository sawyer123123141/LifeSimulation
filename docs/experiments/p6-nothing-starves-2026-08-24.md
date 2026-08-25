# Nothing starves, and the population is pinned to the mating gate

**2026-08-24. 30 seeds, 12,000 ticks, population cap 100, terrain join on.**
`tools/CreatureSweep --deaths 30 100`. One run, no configuration change.

`UrgencyExponent` is under the most reproducible selection in the model
(`p6-urgency-exponent-is-monotone-2026-08-24.md`) and two explanations both predicted that sign:

1. **The reproduction gates** — a sluggish eater sits below the threshold and never breeds.
   Selection through *fertility*.
2. **Starvation** — a creature that waits until it is nearly empty does not reach food in time.
   Selection through *survival*.

The instrument that would separate them properly costs a configuration value, a hash version bump and
three guard updates. **This asked first whether the survival channel is even open.**

## It is not open

5,619 deaths across 30 surviving runs:

| cause | deaths | share |
|---|---|---|
| **age** | 5,443 | **96.9%** |
| health | 161 | 2.9% |
| **starvation** | **8** | **0.1%** |
| **dehydration** | **7** | **0.1%** |
| predation | 0 | 0.0% |

**Fifteen creatures out of 5,619 died of hunger or thirst.** The survival channel cannot be carrying a
t of −19.4. **Hypothesis 2 is retired**, and the instrument that would have tested it was not built.

## And the population is sitting exactly on the gate

| | value | gate |
|---|---|---|
| mean energy fraction | **0.8058** | 0.80 to seek a mate |
| mean hydration fraction | 0.8593 | 0.80 |
| mean final population | 98.2 | cap 100 |

**Mean energy is 0.806 against a mate-seeking threshold of 0.800.** That is not a coincidental
equilibrium; it is a homeostat. A creature at or above 0.80 becomes eligible to seek a mate, spends
time doing that instead of eating, drifts back down, and returns to feeding. The population is held
against the threshold from below.

Which is precisely what hypothesis 1 looks like from outside, and it makes the selection mechanism
concrete: **time spent above 0.80 is breeding opportunity, and a lower `UrgencyExponent` buys more of
it.**

## The cap sharpens it rather than confounding it

Final population is 98.2 of a cap of 100, and 96.9% of deaths are old age. So new slots open almost
exclusively when somebody dies of old age, and **whoever happens to be above the gate at that moment
takes the slot**. At saturation, absolute fecundity stops mattering and *relative* readiness is
everything — which is the strongest possible version of the fertility channel.

**It is also a scoping limit, stated plainly:** this is one scenario at one cap, and a population not
at its ceiling might select differently. The nine corpora the urgency result rests on are all at cap
100.

## What this does not yet establish

**That the gates are causal.** Everything above is consistent with hypothesis 1 and inconsistent with
hypothesis 2, which is not the same as demonstrating the first. The causal test is to make the
threshold configurable and re-measure the urgency drift at a slacker gate:

- pressure **weakens** → the gates are the driver, and `UrgencyExponent` is a healthy gene reporting
  the truth about a strict world
- pressure **persists** → something else again, and the search continues

That test is now worth building, because it is aimed at the one surviving hypothesis rather than at
two.

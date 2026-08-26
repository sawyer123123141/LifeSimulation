# A graded fertility brake produces a carrying-capacity-limited habitat, at every cap tried

**2026-08-24.** `tools/CreatureSweep --deaths <seeds> <cap> [--regen=X] [--graded-fertility]`,
moderate layout, 12,000 ticks.

`p6-the-cap-is-the-stabiliser-2026-08-24.md` ended with a hypothesis: the model has no
density-dependent brake, because births are gated by **step functions** — 70% and 80% of three needs
— so the population breeds at full rate until the forage is stripped and then starves together. This
is that hypothesis built and tested.

## What was built

`gradedFertilityEnabled` scales the **reproduction cooldown** by condition:

```
condition = the BINDING need, min(energy, hydration, health) as fractions of capacity
headroom  = (condition - gate) / (1 - gate),  clamped to [0, 1]
cooldown *= 1 + GradedFertilityStrength * (1 - headroom)        // strength = 3
```

So a creature at full condition is not slowed at all, and one sitting exactly on the gate waits four
times as long.

**Deterministic on purpose.** The obvious graded gate is a breeding *probability*, which needs a
random source inside the tick. Scaling the cooldown is the same negative feedback with none.

**Measured against the gate, not against zero.** A creature is not "half fed" — it is some fraction
of the way from the threshold that lets it breed at all up to full. Measuring from zero would leave
the brake almost fully applied everywhere, since nothing breeds below the gate anyway. A test pins
this, and another pins that the curve stretches with the gate rather than moving, so the two knobs
stay separable.

## The result

| condition | surviving | **starvation share** | population mean / median / max | **sd** |
|---|---|---|---|---|
| cap 500, regen 1.5x, step gate | 3 / 20 | **55.1%** | 46.0 / 27 / 109 | 56.0 |
| **cap 500, regen 1.5x, graded** | **19 / 20** | **0.0%** | 100.8 / 73 / 264 | 76.5 |
| cap 500, regen 2.0x, step gate | 3 / 20 | **64.2%** | 131.3 / 2 / 390 | 224.0 |
| **cap 500, regen 2.0x, graded** | **19 / 20** | **0.0%** | 109.0 / 117 / 325 | 74.1 |
| **cap 500, regen 1.0x, graded** | **19 / 20** | **0.0%** | 75.7 / 54 / 177 | 49.9 |
| **cap 100, moderate, graded** | 28 / 30 | **0.0%** | **63.1** / 72 / 100 | **33.6** |

**Survival 3 of 20 becomes 19 of 20. Starvation goes from 55–64% of deaths to exactly zero.** Not
"low" — no creature starved in any run.

**And the population settles.** At a cap of 500 it holds around 75–110 with a standard deviation near
50–75 and a maximum of 177–325. It is nowhere near the ceiling and it varies between worlds. **That
is a carrying capacity.**

## The debt this closes

`p4-cap-pinning-audit-2026-08-22.md` recorded **4,080 runs with the population column pinned at 48**,
and section 9 of the handoff has carried "a carrying-capacity-limited habitat" as the most important
outstanding measurement debt ever since.

**It needed no scenario redesign at all.** The last row above is the standard layout at the standard
cap of 100: population **63.1 with sd 33.6**, against 98.2 pinned with essentially no variance under
the step gate. The habitat was always capable of limiting itself; nothing was ever telling it to
slow down.

The regeneration axis (`WithRegeneration`) was necessary to *diagnose* the problem and turns out to
be unnecessary to *fix* it — the brake works at baseline regrowth.

## The cost, stated plainly

**Two extra extinctions at the standard cap** — 28 of 30 against 30 of 30. A population that regulates
itself sits lower, and a lower population is closer to zero. That is the honest price of the trade and
it is what a real carrying capacity looks like; a habitat that cannot fail is a habitat with a floor
as well as a ceiling.

Mean energy also falls slightly, 0.8058 to 0.7847, consistent with the population no longer being
pressed against the mating gate.

## What this does not say

**It is not proof the step gate was the only cause.** It shows that adding a graded brake removes the
collapse, which is strong, but the collapse might have had more than one contributor and this one
dominates. Two mechanisms tonight looked sufficient and turned out to be partial.

**Default false**, and every recorded result predates it. **Not switched on for the `Y` playtest
either** — it changes population dynamics at the root, which is a much larger claim on a scenario
than a slope cost or a temperature field, and it deserves a deliberate decision rather than being
folded into a playtest.

**What it does mean is that the whole plant corpus's scope qualification now has a route out.** Every
plant trait result on record was measured with the herbivore population pinned. There is now a
configuration in which it is not.

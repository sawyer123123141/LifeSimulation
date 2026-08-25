# Giving `MetabolicPace` a benefit halves the bleed and does not make it a trade-off

**2026-08-24. 80 seeds per level, 12,000 ticks, population cap 100, terrain join on, fixed sine
temperature.** `tools/CreatureSweep --focused 80 100 --metabolic-ingestion --scenario={moderate,lean,scarce}`,
baseline arm, against the identical seeds without the flag (`p6-dose-response-*-80seeds`).

`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md` found `MetabolicPace` raises two drains 2.14x
across its range and has no third reader at all. This is the flag that gives it one —
`metabolicIngestionEnabled` — and the measurement of whether that is enough.

## What the flag does

`IngestionRate` is scaled by `0.7 + 0.8 * pace`, **the same factor the two drains already use**, so a
creature with twice the metabolism burns twice as fast and eats twice as fast. A different curve
would have made the trade-off arbitrary.

Ingestion is a real rate limit — a creature requests `IngestionRate * dt` per tick from a site
(`SimulationWorld.cs:606`) and contested sites are divided between requesters — so a faster eater
finishes sooner **and takes a larger share of a crowded patch**. The costs are paid every second; the
intake only pays while standing at food that still has some left.

Tests pin that the flag moves only the benefit: `BasalEnergyCostMultiplier`, `DigestionRate`,
`BodyMass` and `FoodYield` are unchanged, and `FoodEfficiency` keeps its own existing trade-off.

## The prediction, and it was wrong

**Predicted:** the gene stops falling uniformly and becomes condition-dependent — flat or upward at
moderate where there is food to exploit, still downward at scarce. **A sign flip across the
ladder.**

**No level shows upward selection.** There is no sign flip.

| level | `metabolic_pace`, pure cost | **with the benefit** | control t (benefit on) | surviving |
|---|---|---|---|---|
| moderate (1.0x) | +0.0055 (t +0.86) | **−0.0013 (t −0.21)** | +0.94 | 80 / 80 |
| lean (0.6x) | **−0.0252 (t −2.99)** | **−0.0129 (t −1.55)** | +0.81 | 49 / 80 |
| scarce (0.35x) | −0.0329 (t −1.25) | −0.1267 (t −3.12) | **+3.31** | **4 / 80** |

## What did happen, and it is a real if smaller result

**At lean the downward pressure halves and drops below significance** — −0.0252 at t = −2.99 becomes
−0.0129 at t = −1.55, against a quiet control at +0.81. That is the flag working: the benefit is
real and it pays for about half of what the gene costs. **At moderate the gene is now flat**
(t = −0.21) rather than merely un-selected.

**It is not enough to make fast metabolism worth having.** The gene went from being sold to being
held, not to being bought. As a design outcome that is a partial success: `MetabolicPace` is no
longer a pure downside, but it is also not the trade-off the name promises.

## The scarce row is unreadable, and the control says so

**`neutral_marker` is at t = +3.31 in the scarce condition.** The control responds to nothing by
construction; when it is the third-largest mover in the table, the table is measuring composition
rather than selection. Four surviving worlds of eighty is why.

So the −0.1267 at t = −3.12 in that row is **not** evidence that the benefit backfires under
scarcity. It is 3.67 times a control that is itself moving hugely. **Discarded**, and recorded here
so it is not quoted later.

## Survival is slightly worse, and not significantly

Baseline-arm extinctions, benefit off against benefit on: moderate 1 → 0, lean 25 → 31, scarce
68 → 76. **Total 94 → 107 of 240, z = 1.20.** Direction is consistently worse at both scarcity
levels; the test cannot resolve it, exactly as with the terrain temperature flag.

**A hypothesis this run does not test:** everyone gets the faster ingestion, and the resource is
shared, so faster eating may deplete contested sites sooner and partly cancel its own benefit — a
commons effect. That would explain both the missing sign flip and the direction of the extinction
counts. **It is untested.** The measurement that would settle it is site depletion and mean energy
compared between the two configurations, which the drift table does not carry.

## Verdict: default stays false, and not switched on for `Y` either

The slope cost and the terrain temperature both earned their place in the `Y` playtest by doing what
they were built to do. **This did not.** It makes an unwanted gene cheaper rather than making it a
decision, and it costs a possible survival penalty that the corpus is too small to rule out.

Two honest options remain, both design decisions rather than fixes:

1. **A stronger or different benefit.** Ingestion may be the wrong channel precisely because it is
   shared. A private benefit — faster recovery, shorter handling time, quicker reproduction cooldown
   — would not be diluted by competitors.
2. **Accept `MetabolicPace` as a cost gene** and rename it and `DigestionRate` so the names stop
   promising a trade-off that does not exist.

The flag, the tests and this corpus stay committed either way, so whichever is chosen starts from a
measurement rather than from an argument.

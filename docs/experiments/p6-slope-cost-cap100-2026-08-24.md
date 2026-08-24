# Slope cost changes behaviour, not genes

**2026-08-24. 240 runs, 120 paired seeds, 12,000 ticks, population cap 100.**
`tools/CreatureSweep --focused 120 100`. Corpus: `p6-slope-cost-focused-cap100-2026-08-24.csv`.

Third condition for the same question. The first was inherited from the plant corpus and could not
answer it; the second removed the wrong limitation and broke the ecology. This one holds.

## The condition is finally healthy

| | cap 48 (first) | cap 200 (focused) | **cap 100 (this)** |
|---|---|---|---|
| extinct, slope-off / on | 3 / 2 of 120 | 38 / 46 of 60 | **2 / 2 of 120** |
| pairs that diverged | 62 of 120 | 60 of 60 | **120 of 120** |
| both arms at population cap | 96 of 120 | — | 67 of 120 |

Cap 200 let populations boom and crash, so 33 of 60 pairs died in both arms and every gene column
moved together through differential extinction. At 100 the worlds live, every pair diverges, and the
table is quiet enough to read.

## The finding: creatures that pay to climb stand on flatter ground

| column | mean | t | sign test |
|---|---|---|---|
| **occupied_slope** | **−0.0345** | **−2.09** | **71 neg / 45 pos, z = 2.41** |
| energy | −0.0080 | −0.59 | 72 / 48, z = 2.19 |
| occupied_elevation | −0.136 | −0.85 | 65 / 51, z = 1.30 |
| neutral_marker (control) | +0.0041 | +0.47 | 51 / 69, z = −1.64 |

`occupied_slope` is the steepness of the ground under the survivors. **This is the mechanism's
a-priori prediction**, not a column that happened to cross: charge for climbing and animals should
end up on flatter ground. It is also not a gene — it is where they are standing, so it needs no
selection to have happened.

Two tests agree on it, a paired t at −2.09 and a sign test at z = 2.41 over 116 pairs. Energy points
the same way, lower with the cost, which is what paying more for the same journey means.

## What stops this being a stronger claim

**The control is not silent.** `neutral_marker` responds to nothing by construction and still comes
in at sign z = −1.64. That is the noise floor of these tests, and `occupied_slope` at 2.41 clears it
without towering over it. One column of fourteen past |t| = 2 is also what chance produces.

The honest position: **the direction is predicted, two tests agree, and the size is modest.** It is
evidence that the mechanism does what it says, not proof of a large effect.

**No gene moved at all.** Every gene column sits at |t| ≤ 1.36. Charging for climbs changes where
animals are within a lifetime; it does not, over 12,000 ticks at this population, change what they
are. That is a coherent result rather than a disappointing one — behaviour responds first, and
selection would need either a stronger cost or far longer runs.

**Population remains saturated**: 67 of 120 pairs finished at the cap in both arms, so the population
column (+1.1, t = 0.60) says little. Uncapping is what broke the previous run, so it stays.

## Consequence

`slopeMovementCostEnabled` is now measured across three conditions: it does not destabilise a healthy
ecology, it moves no gene, and it makes creatures occupy flatter ground. **Enabled for the `Y`
terrain playtest scenario**, which is the scenario whose purpose is that terrain means something. The
configuration default stays `false`, and every recorded plant result remains scoped to runs without
it.

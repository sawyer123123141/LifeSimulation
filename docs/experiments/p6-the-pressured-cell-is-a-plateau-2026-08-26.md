# The survivable-with-starvation cell is a plateau on brake and regeneration, and a cliff on the cap

**2026-08-26.** `tools/CreatureSweep --deaths 30 <cap> --regen=<r> --brake=<b>`, 12,000 ticks,
consumer-defense calibration layout. Twelve cells. Console artefact:
`p6-brake-regen-cap-grid-2026-08-26.txt`. Follows
`p6-starvation-is-a-dial-2026-08-26.md`, which found brake 1.5 / regen 2.0 / cap 500 surviving 30 of
30 with 16.2% starvation **by moving one axis**.

**Why this exists:** a cell found on one axis can be a knife-edge, and adding predation to a marginal
substrate reads out as "predation is unviable" when the truth is "the substrate was marginal". This is
the same objection this session raised against a single-arm conclusion, pointed at its own result.

## Brake x regeneration, cap 500 — a plateau, not an island

| regen \ brake | 1.4 | 1.5 | 1.6 |
|---|---|---|---|
| **1.75** | 30/30, **27.2%** starv, energy 0.498 | 30/30, **19.3%**, 0.502 | 30/30, **4.2%**, 0.657 |
| **2.00** | 29/30, **10.8%**, 0.621 | 30/30, **16.2%**, 0.583 | 30/30, **10.8%**, 0.650 |
| **2.25** | 30/30, **15.5%**, 0.561 | 29/30, **11.5%**, 0.639 | 30/30, **6.5%**, 0.679 |

**Nine of nine cells survive 29 or 30 of 30, and every one keeps starvation between 4.2% and 27.2%.**
No cell collapses, none goes to zero. The trend is the expected one — more brake and more
regeneration each reduce starvation — and it is smooth. **The 2026-08-24 collapse cliff is not nearby
in either direction.**

**The cell is robust. Predation can be attempted here without the substrate being the confound.**

## The cap is the cliff, and it is a floor not a ceiling

Same brake 1.5, same regen 2.0:

| cap | surviving | starvation | mean energy | sterile | population mean / sd / max |
|---:|---:|---:|---:|---:|---|
| **250** | 30/30 | **0.0%** | 0.780 | 7.0% | 224.6 / **55.6** / — |
| 500 | 30/30 | **16.2%** | 0.583 | 20.9% | 299.4 / 146.8 / 500 |
| 1000 | 30/30 | **16.2%** | 0.579 | 21.0% | 302.4 / 151.1 / **532** |

**Cap 500 and cap 1000 are the same ecology** — starvation identical to the tenth of a percent,
population 299 against 302, and at cap 1000 the highest population any seed reaches is **532**. The
cap is not binding; the population is limiting itself at about 300. **The starvation is ecological,
not a ceiling effect.**

**Cap 250 removes the pressure entirely.** It clamps below where crowding begins, starvation goes to
zero, and the population sd falls from 147 to 55.6 — a ceiling again, which is what
`p6-the-cap-is-the-stabiliser-2026-08-24.md` describes.

**So the requirement is a cap of 500 or more — and above 500 the number stops mattering.** That is a
floor to clear, not an edge to balance on.

## The sterility confound, stated before anything is measured here

**Health never regenerates unless `healthRecoveryEnabled` is on, and it is off by default.**
`NeedsSystem` subtracts from health in five places; the only addition is `RecoverHealth`, called at
`SimulationWorld.Ticking.cs:160` behind that flag. Health is one of the three conditions on the
mate-seeking gate.

**At brake 1.5 / regen 2.0 / cap 500, 20.9% of the living are permanently below the health gate.** Any
drift-from-founders measured in that configuration is therefore conditioned on a **breeding
subpopulation that health has already selected**, and the selected fraction rises with the pressure —
**27.1% at regen 1.75 / brake 1.4, 8.9% at regen 2.25 / brake 1.6**. **The confound scales with the
very pressure the cell exists to provide.** No selection claim in this configuration should be
written without either this sentence or the flag below.

### With health recovery on, same cell

| | starvation | sterile | mean energy | population | surviving |
|---|---:|---:|---:|---:|---:|
| default | 16.2% | **20.9%** | 0.583 | 299.4 | 30/30 |
| `--health-recovery` | **13.2%** | **15.2%** | 0.574 | 330.6 | 30/30 |

**The pressure survives the fix.** Starvation stays in the teens and every world lives. Sterility drops
by a quarter **and changes kind**: with recovery on, being under the gate is a recoverable state rather
than a permanent one — recovery needs energy and hydration both above 50%, so the starving tail still
cannot climb out.

**Recommendation, not a decision:** run selection experiments in this cell **with
`healthRecoveryEnabled` on**. The handoff records that flag as the first to flip on a deliberate
re-baseline, and this is a deliberate re-baseline — but it re-baselines everything measured without
it, so it is the human's call, not a fold-in.

## Scope

- One scenario family, 30 seeds, 12,000 ticks, single build.
- Every figure conditions on surviving runs; here that is 29–30 of 30 in every cell, so unlike the
  brake-off cells in the previous document these means **are** comparable across arms.
- Nothing here measures selection. It establishes that the substrate is not marginal and names the
  confound that would otherwise contaminate the first selection measurement taken in it.

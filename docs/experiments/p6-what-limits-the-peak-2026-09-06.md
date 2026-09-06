# What limits the peak: production or access, and does the plant community go sterile before the crash

**Date:** 2026-09-06. **Status:** PREDECLARATION — written and committed **before any run started**.
Results are appended in a later commit; nothing in this section is edited afterwards.

**Plan:** `docs/superpowers/plans/2026-09-05-regulation-first-arms.md`, Task 3.
**Design:** `docs/superpowers/specs/2026-09-05-population-regulation-design.md`.

**No configuration value moves.** The eleven cells are re-run at their recorded brake, cap,
regeneration, gate and seeds, at the recorded length. The only difference from
`p6-regime-b-triage-2026-09-05.md` is that the tools now print the plant side (`f048e15`), which
changes no simulation behaviour and no hash. **No parameter value is proposed here.**

---

## What is being asked

Two questions the recorded corpus cannot answer, because no artefact contains a plant column.

### Q1 — is the peak population limited by production or by access?

The design document (§1.4) leaves this open deliberately. The realised plant growth `limit` is
`min(moisture, fertility, temperature)`, recorded as fertility-bound for 82-90% of plant-reachable
positions but never quoted as a number, and the answer decides whether the producer-side candidates
are aimed at the binding constraint at all.

It is read off `offtake/growth` — realised offtake over realised growth, both per unit
biomass-second, within the interval containing the population peak:

- **Production-limited:** `offtake/growth` reaches or exceeds ~1 in that interval, and standing
  biomass falls through it. Consumers are taking everything the producers make.
- **Access-limited:** `offtake/growth` stays well below 1 while starvation rises and standing biomass
  stays high. Forage is present and not being reached.

### Q2 — does the seed-eligible patch count reach zero, and does it do so before the crash?

This is the ratchet claim (§1.4): grazing holds patches below `MaturityFraction * Capacity`, the
community stops recruiting, and it then ages out on a 34-135 second lifespan.

**Ordering is the whole content of the claim.** Seed-eligible reaching zero *after* the population
peak makes the ratchet a consequence of the collapse. Only reaching zero **at or before** the peak
makes it a cause.

---

## Predeclared answers

Stated so they can fail.

**Q1 — PRODUCTION-LIMITED.** `offtake/growth` reaches 1.0 or above in the interval containing the
population peak in a majority of the eleven cells, with standing biomass falling in the same
interval.

*Why.* The recorded death mix is 42-56% starvation with mean energy fraction at 0.63 at the peak. An
access-limited world starves its creatures beside standing forage, so biomass would hold up while
starvation rose. The recorded plant-community extinction at 24,000 ticks in `PlantSweep` seed 45 —
occupancy 0.0 with 259 plant births — is what a stripped producer looks like, not an unreached one.

*The honest counter-argument, recorded so a failure is informative rather than a surprise.* Six
active food sites of interaction radius 1.5 is a very small feeding surface for a population of ~300,
and `PerceptionSystem` makes an emptied patch **invisible** rather than poor, so a hungry creature
whose target has just been stripped has no target at all. That is an access mechanism and it is
present in the source. If Q1 returns access-limited, this is why, and it is a better explanation than
the one being predicted.

**Q2 — YES, AND BEFORE THE PEAK.** Seed-eligible patch count falls to zero, or within rounding of it,
at or before the sample at which the all-world mean population peaks, in a majority of the eleven
cells.

*Why.* The seeding gate is at 0.75 of capacity while the population is still growing toward its peak,
so patches cross below it well before biomass is exhausted. The gate is crossed long before the
larder is empty.

**Q3 — the instrument check, and it comes first.** Population, alive count, mean energy and the five
death shares must reproduce `p6-regime-b-triage-24seeds-24000-2026-09-05.txt` **exactly**. If any of
them moves, Task 2 perturbed the simulation and nothing else in this document may be read. Predeclared
outcome: they reproduce, because statistics are absent from `ComputeStateHash` and the full suite
passed 747/747 unchanged.

---

## The falsifiers, named in advance

- **Q1 falsified** by `offtake/growth` staying below 1 at the peak in a majority of cells while
  starvation is above 40% of deaths in the following interval. That would mean the ratchet is real but
  is not what is killing the population, it reopens the candidate set, **and under the plan's gate it
  stops execution before Task 4.**
- **Q2 falsified** by seed-eligible staying materially above zero through the collapse. Recruitment was
  available and the community died for some other reason — most likely age mortality outrunning a
  recruitment rate that was never high enough, which is a different mechanism with a different fix.
- **Q2 weakened, not falsified,** by seed-eligible reaching zero only *after* the peak. The ratchet
  would then be a consequence of the crash rather than its cause, and A2 would be worth doing for the
  recovery phase alone rather than as a way to prevent the crash.

## The cells

The eleven rows of `p6-regime-b-triage-2026-09-05.md`, unchanged, at 24 seeds and 24,000 ticks.

| cell | command |
|---|---|
| C2 | `CreatureSweep --deaths 24 500 --regen=2.0 --brake=1.5 --predation --gate=0.45 --ticks=24000` |
| C3 | the same without `--predation --gate=`, at brake **1.4, 1.5, 1.6** |
| C4 | the same without predation, plus `--mate-selection=off`, at brake **3.0, 4.0, 5.0** |
| C5 | `PlantSweep -- 24 --cap=250 --brake=1.0 --ticks=24000` (four arms: contest on/off x flat/terrain) |

---

# Results

**Run 2026-09-06**, after the predeclaration above, which is unedited. Console artefacts:
`p6-what-limits-the-peak-creatures-24seeds-24000-2026-09-06.txt` and
`p6-what-limits-the-peak-plants-24seeds-24000-2026-09-06.txt`, beside this file. Build `f048e15`.

## Q3 first: the instrument check passes in all eleven cells

Every population trajectory reproduces `p6-regime-b-triage-24seeds-24000-2026-09-05.txt`. The peak
sample, `alive(3)` and `alive(9)` agree cell for cell:

| cell | peak sample | `alive(9)` recorded | `alive(9)` here |
|---|---:|---:|---:|
| C2 predation 1.5 | 13,333 | 7 | **7** |
| C3 herbivore 1.4 | 13,333 | 2 | **2** |
| C3 herbivore 1.5 | 10,666 | 0 | **0** |
| C3 herbivore 1.6 | 13,333 | 1 | **1** |
| C4 proximity 3.0 | 10,666 | 2 | **2** |
| C4 proximity 4.0 | 13,333 | 1 | **1** |
| C4 proximity 5.0 | 16,000 | 11 | **11** |
| C5 off/flat | 13,333 | 3 | **3** |
| C5 off/terrain | 16,000 | 7 | **7** |
| C5 on/flat | 13,333 | 8 | **8** |
| C5 on/terrain | 13,333 | 6 | **6** |

So the plant columns are a continuation of the recorded measurement, not a different one.

## Q1 — the predeclared criterion was ill-posed, and it fails in all eleven cells

**Say this before the answer, because it is the more important finding about the method.** The
predeclared threshold was `offtake/growth >= ~1` at the peak. **That threshold is unreachable by any
system with a second loss term**, and this one has a large one: patches die of age, and
`CumulativePlantBiomassLostToMortality` runs at **0.0100-0.0126 per biomass-second in every cell at
every sample** — a near-constant ~1%/s, which is exactly what a 34-135 second patch lifespan produces.
A community in decline satisfies `offtake + mortality > growth`; it need not satisfy
`offtake > growth`, and here it never does. **Measured `offtake/growth` at the peak is 0.48 to 0.88 and
does not reach 1 in any of the eleven cells, so Q1 as literally written is falsified everywhere.**

This is recorded as a criterion error rather than repaired quietly, because the repository's own rule
is that a verdict from an instrument that can only return one answer is not a verdict. The other half
of the predeclared criterion — *"and standing biomass falls through it"* — is the half that carries the
claim, and it is the same statement as `drain > growth`.

**On the corrected reading the answer is unambiguous, and it is PRODUCTION-LIMITED in all eleven.**

| cell | peak | `offtake/growth` | `(offtake+mortality)/growth` | grazing share of loss | biomass at peak, % of its own max |
|---|---:|---:|---:|---:|---:|
| C2 predation 1.5 | 13,333 | 0.60 | **1.04** | 58% | 77% |
| C3 herbivore 1.4 | 13,333 | 0.88 | **1.12** | 78% | 31% |
| C3 herbivore 1.5 | 10,666 | 0.70 | **1.06** | 66% | 74% |
| C3 herbivore 1.6 | 13,333 | 0.88 | **1.12** | 79% | 35% |
| C4 proximity 3.0 | 10,666 | 0.80 | **1.12** | 72% | 49% |
| C4 proximity 4.0 | 13,333 | 0.78 | **1.09** | 71% | 54% |
| C4 proximity 5.0 | 16,000 | 0.66 | **1.05** | 62% | 69% |
| C5 off/flat | 13,333 | 0.50 | **1.08** | 46% | 78% |
| C5 off/terrain | 16,000 | 0.62 | **1.09** | 57% | 56% |
| C5 on/flat | 13,333 | 0.48 | **1.10** | 44% | 74% |
| C5 on/terrain | 13,333 | 0.50 | **1.09** | 46% | 78% |

Total drain exceeds gross production at the population peak in every cell, standing biomass is falling
through that sample in every cell, and grazing is **44-79% of the loss**. The access branch required
biomass to stay high while creatures starved; instead biomass ends at **3-82 units in C3 and C4**, from
a maximum near 1,500 — a 94-99.8% loss, with the patch count down to 0.6-2.6 of 22. Forage is not
standing unreached in those cells. It is gone.

**The gate in the plan does not fire.** Execution continues to Task 4.

## The finding neither branch anticipated: most of primary production is spent on replacement

The mortality column is the one nobody had looked at, and it is the largest single number in the early
run. In the ungrazed phase — sample 2/9 of C2, offtake 0.0015 against growth 0.0124 — **age mortality
is 0.0097, or 78% of gross growth.** Before a single meaningful bite is taken, the plant community is
spending roughly four fifths of what it grows on replacing patches that died of age.

So the production available to consumers without shrinking the standing crop is not `growth`. It is
`growth - mortality`, which in that phase is **about 22% of gross**. At the peak the consumers are
taking 0.0090 to 0.0435 per biomass-second against a net production of that order or less. The
overshoot is not marginal.

**This is a producer-side structural fact and no candidate in the design document addresses it.**
Patch lifespan is `BaseLifespanSeconds * (1.5 - .75 * Growth)`, 34-135 seconds, and every patch pays
it whether or not anything eats it.

## Q2 — falsified as written, and the ordering it was testing holds

**Predeclared: seed-eligible patch count reaches zero at or before the population peak in a majority of
cells. It does not, in any cell.** The all-world mean seed-eligible count at the peak is 6.3 to 16.8,
and it reaches within rounding of zero in exactly one cell (C4 at brake 4.0) at tick 21,333, which is
**three samples after** that cell's peak. This is the "weakened, not falsified" branch that was named
in advance: **the ratchet runs after the peak, not before it, so it is a consequence of the crash
rather than its cause.**

Two qualifications, in opposite directions.

**Against the prediction, beyond the falsification:** the all-world mean is pooled over 24 worlds that
die at different times, so it cannot reach zero while any world holds one patch. The literal
zero-crossing was a badly chosen statistic as well as a wrong one. A per-world crossing count would
have been the right form and was not computed.

**For the mechanism the prediction was testing:** *sterilisation leads patch loss at every sample in
every cell*. In C3 at brake 1.4, against that cell's own maximum, patch count runs 100% -> 92% -> 60%
across the samples up to the peak while seed-eligible runs 100% -> 76% -> 30%. The seeding fraction
falls roughly twice as fast as the patch count and one sample ahead of it, which is the causal order
the ratchet claims. It is visible; it is simply not fast enough to precede the population peak.

## The split the eleven cells fall into, which was not predicted at all

The cap-500 herbivore and proximity families (C3, C4) and the cap-250 plant family (C5) behave
differently at the same verdict:

- **C3 and C4 strip the community.** Grazing is 66-79% of plant loss, biomass ends at 0.2-6% of its
  maximum, patch count ends at 0.6-2.6.
- **C5 does not.** Grazing is 44-57% of loss, age mortality is the other half, and the community ends
  with **6.6-10.4 seed-eligible patches and 500-800 standing biomass** — and the population still dies,
  3 to 8 worlds of 24 alive, with 41-64% starvation in the last interval.

**A community that is not stripped still fails to hold the population up.** In C5 the consumers are
taking half of production at the peak and the other half is going to age mortality, so the standing
crop falls anyway, and what is left at the end is standing forage beside a population that starved.
Whether that residual is genuinely unreachable — the access mechanism the predeclaration named, with
`PerceptionSystem` making an emptied patch invisible rather than poor — is **not settled by these
columns**, and saying so is the honest end of this document rather than a conclusion drawn past its
evidence.

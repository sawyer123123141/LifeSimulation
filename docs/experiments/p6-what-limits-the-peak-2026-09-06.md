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

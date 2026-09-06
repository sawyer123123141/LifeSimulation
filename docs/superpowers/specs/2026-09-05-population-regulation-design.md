# Population regulation: what the brake actually computes, why no value of it can work, and how to tell a real regulator from a slower collapse

**Date:** 2026-09-05. **Status:** DESIGN. No code changed, no run executed, no sweep started.
**No parameter value is proposed anywhere in this document.** The frozen spec is untouched.

> **§4's ordering was overruled the same day, and correctly. The order is A2 first, then E.**
> Testing E with the ratchet live is a confounded test: any regulator that lets the population reach
> the forage holds patches below `0.75 * Capacity` some of the time, which under the current seed gate
> is sterilisation, so E would be measured against a resource base that fails under exactly the
> conditions E exists to handle. §4's argument for E as the strongest candidate stands; its argument
> for E *first* does not. Execution, ledger and predeclarations:
> `plans/2026-09-05-regulation-first-arms.md`. **§1.2 also carries a correction from the free check
> recorded there.**

**Where this lives, and why.** `docs/superpowers/specs/`, not `docs/superpowers/plans/`.
`2026-08-30-what-finished-means-design.md` §10 reserves implementation for "separate plans and specs
derived from this frozen governing document", and every file in `plans/` in this repository is a
numbered task list with a progress ledger. This document has no tasks. It states a failure, enumerates
a candidate space, and predeclares the trajectory signature of each candidate **before anything is
built**, which is the sequence this project's own field notes demand. A plan follows once a candidate
is chosen, and only for that candidate.

**Inputs.** `AGENTS.md`; `docs/AGENT_FIELD_NOTES.md` §1-4 and §5 from 2026-09-02;
`2026-08-30-what-finished-means-design.md` §5, §6, §9; `p6-regime-b-triage-2026-09-05.md`;
`p6-the-shipped-world-does-not-persist-2026-09-03.md`; the cell-family ledger in
`plans/2026-09-03-run-length-validity-audit.md` §1; and the simulation source.

**Not re-litigated.** No value on the brake axis regulates this population. Eleven cells, eight
strengths from 0.75 to 5.0, four scenario families, all COLLAPSING at 24 seeds. Hunger onset and
collapse onset are the same event at 2,666-tick resolution. This document starts after that.

---

## 1. The failure, from the source and the recorded trajectories

### 1.1 What the brake computes, exactly

`ReproductionSystem.CooldownMultiplier` is the whole mechanism:

```
condition   = min(Energy/EnergyCapacity, Hydration/HydrationCapacity, Health/HealthCapacity)
headroom    = clamp01((condition - needFraction) / (1 - needFraction))     // needFraction = 0.70
multiplier  = 1 + strength * (1 - headroom)
```

Three facts about where it is evaluated decide everything that follows.

1. It is evaluated **only inside `CreateChild`**, once per birth, for both parents
   (`ReproductionSystem.cs`, `CreateChild` -> `CooldownFor` -> `CooldownMultiplierFor`).
2. It is evaluated **after `ChargeCost`**, which has just subtracted
   `ReproductionEnergyCostFraction` of energy capacity and 0.10 of hydration capacity from that same
   creature, on the two lines above.
3. `CanReproduce` has already required all three needs at or above `needFraction` = 0.70 for the
   birth to happen at all.

### 1.2 The brake's dynamic range is at most 8-16%, and in the recorded cells it is zero

> **CORRECTED 2026-09-05, by the free check in `plans/2026-09-05-regulation-first-arms.md` §2.** Two
> changes, in opposite directions. The 8-16% figure below is an **upper bound** that requires a parent
> at 0.95-0.99 of energy capacity; recorded breeder energy sits near the 0.80 mate-seeking gate, so the
> realised range is **zero** and the brake is exactly `1 + strength` for effectively every birth. The
> section's conclusion is therefore stronger than it claims. And the second bullet at the end -
> "the brake's own authority is under selection" - is **WITHDRAWN**: `fertility_investment` rises in one
> scenario family, does not rise in the predation family, shows **no dose-response across a 4x brake
> range**, and rises without a brake at all. The rise is real and the brake is not its cause.

Put (2) and (3) together and the input to the curve is bounded by the charge, not by the ecology.

`ReproductionEnergyCostFraction = 0.15 + 0.20 * FertilityInvestment` (`GenomePhenotype.FromGenome`).
`PhysiologyFounderFactory` centres every founder gene at 0.5, so the founder-average cost is 0.25 of
energy capacity. A parent enters the birth with energy fraction at most 1.00 and at least 0.70; it
leaves with at most 0.75. `headroom` is therefore at most `(0.75 - 0.70)/0.30 = 0.167`, and the
multiplier lies in `[1 + 0.833*strength, 1 + strength]`.

The ratio between the brake's output for a **perfectly fed** founder-average parent and one **exactly
at the gate**:

| strength | multiplier, full parent | multiplier, parent at the gate | total dynamic range |
|---:|---:|---:|---:|
| 0.75 | 1.625 | 1.75 | **1.08x** |
| 1.5 | 2.25 | 2.5 | **1.11x** |
| 5.0 | 5.167 | 6.0 | **1.16x** |

**That is the whole authority of the regulator.** Across the entire span from satiation to the
breeding gate, at every strength anyone has measured, the birth interval moves by roughly a tenth.
Meanwhile the population it is supposed to regulate moves from 12 founders to a peak near 300 - a
factor of 25. A controller with 1.1x authority cannot answer a 25x perturbation, and no choice of
`strength` changes that, because `strength` multiplies both ends of the range together.

This is the arithmetic behind the recorded observation that nothing was left to find on the brake
axis. `p6-regime-b-triage` reports that C3 at 1.4, 1.5 and 1.6 - the one clean single-variable axis in
the corpus - peak at the same sample, lose their first worlds at the same sample and are all gone by
tick 18,666: **"Across a 14% change in brake strength the collapse does not move by a single sample."**
It does not move because the brake is not a feedback. It is a near-constant scalar on the intrinsic
birth rate, and rescaling `r` in a system with a fixed food supply relocates the boom without changing
its shape.

Two further consequences of *where* it is evaluated, both of which make it worse:

- **The stored cooldown is stale.** It reflects the parent's condition at its *last* birth, not now.
  A parent that bred while the forage was intact carries a short cooldown into the famine.
- ~~**The brake's own authority is under selection, in the direction that removes it.**~~
  **WITHDRAWN 2026-09-05 - the check was run and did not support it.** The claim was that
  `ReproductionCooldownSeconds = 16 - 8 * FertilityInvestment` makes selection favour the genotype the
  brake cannot modulate. `fertility_investment` does rise, hard, in the mate-selection-off family
  (+0.17 to +0.22 at 0.84-0.92 direction consistency against a `neutral_marker` null at +0.01), but it
  **does not rise at all** in the predation family, shows **no dose-response across brake 3.0 to 12.0**,
  and rises in an **unbraked** cap-100 cell at +0.100. The gene is under directional selection for the
  obvious brake-independent reason - it directly shortens the base birth interval - and the brake is
  not what drives it. Full table in `plans/2026-09-05-regulation-first-arms.md` §2.
- **What the check established instead, and it is stronger than the bound above.** `headroom > 0`
  requires pre-charge energy above `0.70 + ReproductionEnergyCostFraction`: **above 0.950 of capacity at
  the founder mean, above 0.986 at the evolved mean.** `p6-nothing-starves-2026-08-24` records the
  population homeostatting at 0.806 mean energy against the 0.80 mate-seeking gate, and the 24-seed
  triage artefact reads 0.6295 in C2. **Breeders are nowhere near the threshold at which the brake
  varies at all, so the multiplier is exactly `1 + strength` for effectively every birth, in every
  recorded cell, from the founders onward.** The brake was never a feedback, at any strength.

### 1.3 It acts on recruitment; the crash is caused by standing consumers

`AdultAgeSeconds = 20`. `MaximumAgeSeconds = 90 + 180 * LifespanTendency`, so 90-270 seconds at
20 Hz - 1,800 to 5,400 ticks. Cutting births to zero at the instant of overshoot removes **zero**
consumers. The overshoot is standing adult biomass, and the only mechanism in this model that removes
standing adult biomass is death. So the brake's control authority over the quantity that causes the
crash is not small; on the timescale of the crash it is zero. A fertility-only regulator has to act
long before the peak, and this one first reaches its maximum *at* the peak, because that is when
condition first falls.

### 1.4 The food side is not a passive stock. It has a ratchet.

**Every one of the eleven collapsing cells is plant-backed.** `tools/CreatureSweep/Program.cs`
sets `plantCohortsEnabled: true` unconditionally, and `PlantSweep` is plant-backed by construction.
Under that flag `SimulationWorld.Step` calls `Resources.RegenerateNonFood`, which skips
`ResourceKind.Food` entirely, and `PlantGrowthSystem.ProjectFoodResources` overwrites each food
resource's amount from patch biomass every resource tick. Food comes from logistic plant growth in all
eleven cells; the flat `RegenerationPerSecond` path is dead for food. `--regen=X`, which appears on
every recorded command line, reaches food only indirectly, through
`SimulationScenario.ApplyTo`: `growthRate = 4 * RegenerationPerSecond / capacity`. It is the logistic
rate constant, not a refill rate, and on the same flag it also scales water regeneration.

The plant side then contains a positive feedback in the crash direction, in three source lines:

- `PlantGrowthSystem.Step`: `growth = growthRate * mult * (B + 0.01*K) * (1 - B/K) * limit * dt`.
  The 1% sprout floor stops a patch locking at zero, but growth is still proportional to remaining
  biomass, so a heavily grazed patch regrows slowly in absolute terms.
- `PlantReproductionSystem.Step:36`: `if (parent.Biomass < parent.Capacity * MaturityFraction) continue;`
  with `MaturityFraction = .75f`. **A patch held below three quarters of capacity produces no seeds at
  all.** Not fewer seeds - none.
- `PlantMortalitySystem.Step`: patches die of **age only**, at
  `BaseLifespanSeconds * (1.5 - .75 * Growth)` = 34-135 seconds, i.e. 675-2,700 ticks.

Chain them: grazing pressure sufficient to hold patches below 0.75K sterilises the plant community;
the standing community then decays on its 34-135 second age clock; each patch lost raises grazing
pressure on the rest; and re-establishing a patch requires a *parent* above 0.75K, which the same
grazing prevents. **This is a ratchet, not a regulator.** Consumer pressure destroys the resource's
capacity to recover, and the destruction is self-reinforcing.

It is observed. `p6-regime-b-triage` records, in the section it labels an instrument caveat, that seed
45 at 24,000 ticks reports **259 plant births and occupancy 0.0** - the plant community is extinct,
not sterile. That is the far end of this ratchet.

And the timescale matches. The plant community's decay clock is its age clock, 675-2,700 ticks. The
recorded collapse takes one to two trajectory samples, 2,666-5,333 ticks. **The clock the collapse
runs on is the plant age structure, not the consumer birth rate** - which is exactly why changing the
brake by 14% does not move it by a single sample.

One quantity in that chain is *not* established and must not be asserted: the realised growth `limit`,
`min(moisture, fertility, temperature)`, is under 1 with procedural fields on, and
`p4-fertility-binds-the-growth-limit-2026-08-19` records fertility as the binding minimum for 82-90%
of plant-reachable positions. Whether the peak population is limited by plant *production* or by
*access* to six point sites of interaction radius 1.5 follows from that number, and §3.1 says how to
read it off accumulators that already exist rather than estimate it here.

### 1.5 Scramble allocation makes the shortage uniform, so the mortality is synchronous

`ResourceAllocationSystem.Resolve` computes, per resource, one number:

```
scale = totalRequested <= availableAmount ? 1 : availableAmount / totalRequested
```

and applies it to **every** requester at that resource. Nobody is excluded; nobody is fed in full.
This is pure scramble competition, and it is the reason the collapse has no floor.

Under scramble, a shortage that halves the supply halves *everyone's* intake. Per-capita intake falls
as `1/N` for all `N` competitors simultaneously, so every energy buffer at a patch drains in parallel
and the cohort crosses the starvation threshold together. Under contest - some individuals fed in
full, the rest excluded - the same shortage feeds a subset and starves the remainder, and the
survivors are still there when the plants recover. **The model contains no mechanism by which one
individual out-competes another for a scarce patch.** Differences in `IngestionRate` do not help,
because the scale factor multiplies them all identically; `GenomePhenotype.cs` says so in its own
words about `MetabolicPace`: faster ingestion "is **shared**, so competitors cancel it".

The recorded death mix is what synchronous starvation looks like:
`p6-the-shipped-world-does-not-persist` reports interval starvation shares of 0.0%, 0.0%, 22.9%,
65.6%, 69.0% and all 24 worlds dead.

### 1.6 What the data can and cannot say about the timing

The triage's per-cell gap table is the strongest statement available: gap 0 in four cells, 1 in six,
2 in one, and `hunger` coincides exactly with `peak` in seven of eleven, differing by one sample **in
both directions** in the rest. The both-directions detail is the project's own evidence that the
residual gap is the 2,666-tick sampling grid rather than a mechanism.

It should be read together with the crash arithmetic. Once energy reaches zero, `NeedsSystem.Tick`
drains health at `4f * deltaTime`; `HealthCapacity = BodyMass^0.67 * 100`, so a founder-mass creature
dies roughly 18 seconds - about 360 ticks - after its energy runs out. **The entire crash, from the
forage being stripped to the cohort being dead, is shorter than one trajectory sample.** The
instrument cannot resolve hunger onset from collapse onset because the model puts nothing between
them, and no finer sampling of the *population* would change that. What finer sampling of the *plant
community* would show is where the separation actually exists, and nothing there is currently
reported (§3.1).

### 1.7 What a working regulator has to do differently

Four requirements, one from each failure above. They are the acceptance criteria for §2, and no
candidate is credible on ease of implementation if it misses R2 or R4.

- **R1 - the input must not be an individual need.** Any signal read off `CreatureNeeds` inherits the
  energy-buffer lag and is bounded below by the 0.70 breeding gate, which is where the current brake's
  8-16% range comes from.
- **R2 - the authority must scale with density across the range the population traverses**, roughly 12
  to 500, not by a bounded constant factor.
- **R3 - it must act on standing consumers, or act on recruitment early enough that the 20-second
  maturation delay and the 90-270-second adult residence still leave it authority.** Regulating births
  at the peak is regulating nothing.
- **R4 - it must break the synchrony of scramble allocation**, so that a shortage produces differential
  survival rather than a uniform decline. Without R4 there is no floor under any crash, however
  shallow, and a population that dips below its replacement rate has nothing to recover from.

One control fact worth carrying through all of §2: the same layout at cap 96 is **level from 12,000 to
36,000 ticks**, 21-22 of 24 worlds alive, starvation 0.0-0.1% of every interval
(`p6-the-shipped-world-does-not-persist`). A survivable equilibrium exists in this model at low
density. What is missing is anything that steers the population towards it - and the one thing that
has ever held it there is a hard clamp on `N` in `ReproductionSystem.Step`.

---

## 2. Candidate mechanisms

Each entry: what it changes, where in the source, what it costs, and its determinism risk under
`AGENTS.md` §3. Two axes recur - **producer-side vs consumer-side**, and **does it meet R4** - and they
are what the ranking in §4 turns on.

### Rejected first, with reasons from the source

- **"Give it more food" via `--regen`.** Under `plantCohortsEnabled` this scales the logistic rate
  constant and water regeneration, not a refill rate, and every recorded cell already sits at 2.0. It
  is a variant of A1, not a separate option, and reading the recorded command lines as if `--regen`
  were the food knob is a mistake worth naming.
- **Scaling all resource levels together.** Already measured and recorded in
  `SimulationScenario.WithRegeneration`'s own docstring: at a cap of 250, levels from 0.40x to 1.00x
  **all** collapse, 21 to 23 of 24 worlds extinct at every one. "Scarcity is not what causes
  boom-and-collapse - the ratio of regrowth to standing stock is."
- **Searching the brake axis further.** The triage places the burden explicitly: a proposal that some
  unmeasured strength behaves differently must first say why, given that both ends and the middle
  behave identically. §1.2 answers that: it would not, because `strength` scales both ends together.

### A - Resource regrowth dynamics

Two genuinely different mechanisms that must not be conflated.

**A1 - growth form and rate.** `PlantGrowthSystem.Step`, one expression; or the scenario's
`RegenerationPerSecond`, which becomes `growthRate`. Changes how fast a grazed patch returns towards
capacity.
*Cost:* trivial. *Determinism risk:* low in kind - no new randomness, no ordering - but it changes
every hash in every plant-backed cell, so `ComputeStateHash` tests fail by design and the entire plant
corpus is re-baselined. That is the real price and it is large.
*What it does not do:* nothing on the consumer side. It cannot meet R2, R3 or R4.

**A2 - the recruitment ratchet.** `PlantReproductionSystem.Step:36`, the `MaturityFraction` step, plus
the `seedBiomass` expression beside it. Replacing an all-or-nothing seeding threshold with output
graded on biomass removes the positive feedback identified in §1.4 without adding any regulator.
*Cost:* small - one condition and one expression. *Determinism risk:* moderate. `seedBiomass` feeds
`ConsumeAt` and site competition, so this moves plant *genetics* as well as plant demography and
touches every recorded plant-gene result, including the establishment-contest and
seed-production-rate calibrations.
*What it does:* removes a destabiliser rather than adding a stabiliser. Under it a grazed community can
still recruit, so patch count stops being a one-way function of grazing history.

### B - Density-dependent mortality

A health or mortality term rising with local conspecific density, independent of energy.
*Where:* a new per-creature neighbour count computed at `PerceptionHz` from the existing
`CombatGrid` (`RebuildCombatGrid` already runs there), consumed as an extra drain in `TickNeeds`.
*Cost:* moderate - a new buffer on the `EnsureCapacity` pattern and a new system file, which
`AGENTS.md` §6 requires the task to name explicitly.
*Determinism risk:* low **if** it is a deterministic drain over a neighbour *count*, which is
order-independent. It becomes high the moment it selects *which* neighbours, or rolls a die - the
latter needs a new `RandomDomain` member, permitted only as a new number. The graded-fertility
precedent already chose scaling a continuous quantity over a probability for exactly this reason;
follow it.
*Requirements met:* R2 and R3 directly, R1 partially - density is available before individual buffers
deplete. Not R4.
*The reason it is not recommended first is in §4:* it regulates by construction, which is the same
objection already recorded against the population cap.

### C - Reproduction gated on local resource availability

Replace or augment the `CanReproduce` step on individual needs with a gate on the forage state near
the parent.
*Where:* `ReproductionSystem.IsReady`/`CanReproduce`, plus a resource query. `SimulationWorld` already
rebuilds `ResourceGrid` at `PerceptionHz`, so the query exists; passing it in changes
`ReproductionSystem`'s constructor and `Step` signature and every test that constructs one.
*Cost:* moderate, mostly signature churn. *Determinism risk:* low if the query resolves by index order
through the existing perception helper.
*Requirements met:* R1 squarely - it replaces a lagging individual stock with a resource-side signal.
**R3 is untouched:** it still acts only on recruitment, and §1.3 applies to it unchanged.

### D - Spacing or territoriality

Two very different implementations hide under one word.
**D1 - repulsion:** a conspecific-avoidance term in `GetMovementTarget`. Cheap, but with six point
sites of `interactionRadius` 1.5 it mostly pushes creatures *off* food, which is a handling-time
change wearing a spacing costume.
**D2 - exclusive claims:** a claim on a food resource index, so a held patch is invisible to
non-holders in `PerceptionSystem`. `HomeRangeSystem` is the nearest existing machinery and is
deliberately *soft*.
*Cost:* D1 small, D2 large - claim state, release on death, and a change to perception.
*Determinism risk:* **D2 is the highest of any candidate.** Claim resolution is a selection among
competitors, the exact class `AGENTS.md` §3 warns about, and must resolve on a stable key such as
creature id, never on grid or array order.
*Requirements met:* D2 meets R4 through space. D1 meets nothing reliably.
*Constraint:* a territoriality implementation must **not** be built on place memory.
`MemorySystem.ObservePlace` is pinned inert by `LivenessTests`, and the frozen spec §10 keeps it inert
"unless and until an experiment implementing the section 3 hypothesis justifies activating it".

### E - Contest allocation (from the source, not the brief)

Replace the single proportional `scale` in `ResourceAllocationSystem.Resolve` with an ordered fill:
sort the requests at a resource by a stable, heritable key - phenotype, tie-broken by creature id -
and satisfy them in order until the pool is empty. Late requesters get nothing.
*Where:* **one file**, `Assets/Scripts/Simulation/Resources/ResourceAllocationSystem.cs`, behind a
config flag so flag-off is byte-identical.
*Cost:* the smallest of any candidate that meets R4. No new per-creature state, no new system; an
`Array.Sort` over an index buffer with a total-order comparer, which is the pattern
`ReproductionSystem.CreatureIndexComparer` already establishes.
*Determinism risk:* low, and materially lower than D2, **provided** the ordering key is a phenotype or
id and never array or grid order. No randomness. It does change every hash in every scenario that has
resource contention, which is the real cost.
*Requirements met:* R4 squarely. It does not by itself meet R2 or R3 - it changes what a shortage
*does*, not when one arrives.

### F - Consumer functional response

Today `requestedAmount = IngestionRate * FixedDeltaTime` every tick, with no handling cost and no
saturation other than the energy clamp in `ConsumeFood` - a Holling type I response, linear in
availability. A type III response, in which per-capita offtake falls faster than linearly as patch
biomass falls, is the classical stabiliser: grazing pressure releases before a patch is stripped, and
the patch can climb back past the maturity gate.
*Where:* the `requestedAmount` expression in `SimulationWorld.ResolveResourceInteractions`, and
possibly `PerceptionSystem`'s `candidate.Amount <= 0f` filter, which today makes an emptied patch
invisible rather than poor.
*Cost:* small. *Determinism risk:* low - one expression, no ordering, no randomness.
*Worth noting:* `foragingEconomicsEnabled` already maintains `AdvanceForagingActionTime` and
`UpdateForagingIntakeRate` every tick on the live path, and the field-notes ledger records that
nothing on that path consumes the result. A functional response is the natural consumer of that
already-executing state.
*Requirements met:* it attacks the ratchet from the consumer side. Not R4.

### G - Refugia

An ungrazable fraction of the plant community that keeps seeding the grazed area.
*Where:* **no simulation code at all** - scenario layout plus `ArenaHalfWidth`.
`AbundantSiteReplicationModerate`'s docstring already records that this exists by accident: "at this
spacing the outer targets sit beyond the hard-coded creature arena of +/-25, so patches establishing
there are never grazed. **That is a refugium.**"
*Cost:* lowest of all. *Determinism risk:* none beyond being a different scenario.
*Caveat, from `SimulationConfig`:* "Widening the arena without also placing resources into the new
space is a measurement of starvation, not of space."
*Requirements met:* none of the four. It changes the *floor* of a crash, not the interaction. It is the
cheapest available probe of whether the collapse is recoverable at all, and it belongs in §3 as a
control rather than in §4 as a regulator.

---

## 3. Predeclared trajectory signatures

Written before anything is built, so that a candidate can fail. The reference cell is **C3 at brake
1.5, herbivore, 24 seeds, 24,000 ticks** - the one clean single-variable axis in the corpus, currently
**0 of 24 alive**, and the only genuine zero in the triage. Verdicts use the persistence criterion
unchanged: COLLAPSING if `mean(7) > mean(8) > mean(9)` or `alive(9) < 0.85 * alive(3)`.

### 3.1 The instrument gap, which blocks all of this

**Every signature below is read off plant-side columns that the trajectory does not currently print.**
`Trajectory` reports population, alive count, mean energy and the death mix. It reports nothing about
the plant community, and §1.4 says the plant community is where the clock lives.

`SimulationStatistics` already carries `Plants.Count`, standing plant biomass,
`_cumulativePlantGrowth`, `_cumulativePlantBiomassConsumed` and
`_cumulativePlantBiomassLostToMortality`. The only missing quantity is **the fraction of patches at or
above `MaturityFraction * Capacity`** - the seed-eligible fraction, which is the ratchet's state
variable - and that is one accumulator in the existing loop in `SimulationWorld.Statistics.cs`.

**No candidate can be adjudicated before the trajectory carries these columns, and adding them is a
reporting change, not a mechanism change.** It is the first thing to build regardless of which
candidate wins, and it also settles the production-versus-access question left open in §1.4, because
`_cumulativePlantGrowth` against `_plantBiomassSeconds` gives the realised growth rate directly rather
than by estimate. While doing it, the `frozen` column's watermark-versus-scan defect - recorded
2026-09-05 and deliberately not fixed because nothing depended on it - stops being harmless, because
plant columns are about to become load-bearing.

### 3.2 The signatures

| candidate | population shape | starvation by interval | plant patch count / seed-eligible fraction | verdict |
|---|---|---|---|---|
| **A1** faster regrowth | same peak, same crash, **later** | same shape, shifted right | still ratchets monotonically to zero | COLLAPSING |
| **A2** graded seeding | peak unchanged; crash **arrests partway**; V-shaped recovery | spike at the peak, falls back towards zero, **recurs** | stops being monotone - it recovers | PERSISTENT, oscillating |
| **B** density mortality | rises then **flattens, no crash** | **under 5% in every interval** | biomass and patch count both flat | PERSISTENT, flat |
| **C** local-forage gate | peak **earlier and lower**; births fall while mean energy is still high | under 5% throughout; **extinction risk moves into the first third** | biomass never drawn below the maturity gate | PERSISTENT, flat to oscillating; establishment losses up |
| **D2** territoriality | plateau below the cap | **chronic non-zero in every interval including the last third** | held patches above the gate; unheld patches idle | PERSISTENT |
| **E** contest allocation | plateau; the crash is replaced by a **step down to a persisting remnant** | **chronic non-zero in every interval including the last third** | trough shallower - fewer effective grazers | PERSISTENT |
| **F** type III response | peak unchanged; **oscillation** | periodic | **biomass minimum rises sharply**; seed-eligible fraction never reaches zero | PERSISTENT, oscillating |
| **G** refugium | crash still happens; **recovery follows** - a visible V | spike, then decline | in-arena count to zero while out-arena stays flat | PERSISTENT via recovery |

Note what D2 and E predict: **a cell with materially non-zero starvation in every interval including
the last third, while `alive(9) >= 0.85 * alive(3)`.** That is verbatim the falsifier the triage
predeclared for the hunger-equals-collapse claim and that no cell met. It is not a target - §6 forbids
targeting a starvation share - it is the signature that distinguishes a population regulated by
differential access from one regulated by anything else in this table.

### 3.3 The discriminators, where two signatures collide

- **D2 versus E.** Both predict chronic starvation with survival. They separate on the **selection**
  statistics, not the population ones: E ties access to a heritable key by construction, so the
  intake-to-offspring slope should steepen and the variance in lifetime reproductive success should
  rise. D2 ties access to space, and moves the selection statistics only if the claim contest itself
  resolves on phenotype. A secondary separator: E's excluded individuals die *at* the patch, D2's die
  *away* from it.
- **A2 versus F.** Both predict oscillation with a raised biomass trough. They separate on grazing
  pressure at low biomass: F reduces per-capita offtake as biomass falls, A2 does not change offtake at
  all. The ratio `_cumulativePlantBiomassConsumed / _plantBiomassSeconds` falls under F and is
  unchanged under A2. Both accumulators already exist.
- **Any candidate versus a disguised cap.** This is the control that matters most, and it must be
  predeclared with the arm it tests. **Run the surviving arm again with the plant capacity budget or
  the active site count raised.** A real ecological regulator moves its plateau up with the food
  supply; an imposed one - the population cap, and B if B is doing all the work - does not. Without
  this control, "the population is stable" is not evidence that the ecology is regulating it, which is
  precisely the error `p6-the-cap-is-the-stabiliser-2026-08-24` already recorded once.

### 3.4 Free checks, on data already recorded

- ~~**The `fertility_investment` drift sign in the braked cells**~~ - **RUN 2026-09-05, result in
  `plans/2026-09-05-regulation-first-arms.md` §2.** The mechanism it tested is withdrawn; the headroom
  arithmetic it produced is folded back into §1.2.
- **The intake-to-offspring flatness under scramble** (§1.5). The frozen spec §6 records intake rate
  spanning 78.7-89.1 against offspring flat at 1.93-2.02. That is the shape proportional-share
  allocation predicts, and it is already measured.

---

## 4. Recommendation: E first, A2 second, run singly

**E - contest allocation - first. A2 - the plant recruitment ratchet - second.** Run each as its own
arm against the same unchanged control before running the pair, per field notes §3: prefer one sweep
that varies one variable to one that varies several and hopes the effect is attributable.

### Why E, on merits

1. **It is the only candidate that addresses why the collapse has no floor.** A1, C, F and G lower or
   delay the peak; B flattens it. None of them changes what happens when the shortage does arrive, and
   the recorded death mix - 0% starvation to 50-76% in one sample, in every cell, with every world
   lost - is a statement about the shortage, not about the peak. R4 is the requirement no other
   low-risk candidate meets.
2. **It is the only candidate that also attacks the frozen spec's recorded blocker on the §5 causal
   loop.** §6 records the loop failing at the performance-to-fitness step: intake rate spans 78.7-89.1
   while offspring is flat at 1.93-2.02. Proportional-share allocation is a *mechanical* explanation
   for that flatness - every competitor's bite is scaled by the same factor, so an intake advantage is
   invariant to density, and the 0.70 step gate then discretises what survives of it. The source says
   this out loud about `MetabolicPace`. So E is not complexity layered on top of an unclosed loop; it
   is work on the link the spec says the loop breaks at. **No other candidate here buys both a
   regulator and a §5 answer.**
3. **It is a named §9 mechanism** - "competition for resources and space" - so it needs no invented
   rationale, which is what §9 exists to prevent.
4. **Its determinism surface is the smallest of the R4-meeting candidates**: one file, one flag, a
   total-order comparer on a stable key, no new state, no randomness. This is stated last deliberately;
   it is not the reason to prefer it, and E's true cost - re-baselining every scenario with resource
   contention - is not small.

### Why A2 second

The 0.75 maturity step plus age-only patch mortality is the one place in the model where consumer
pressure destroys the resource's ability to recover, and no ecological justification for the threshold
is recorded anywhere in the source. Removing it is not adding a regulator; it is removing a
destabiliser - which makes it the candidate most likely to be **necessary regardless of which
regulator wins**, because any regulator that lets the population actually reach the forage will hold
patches below 0.75K some of the time, and under the current gate that is sterilisation.

### Why not the others first

- **B - density-dependent mortality.** It is the most direct route to a flat population and the least
  informative one: it regulates *by construction*. A density death term is a population cap with a
  smooth edge, and this project has already recorded once that a stable population held up by a clamp
  taught nothing about the ecology. **Hold B as the control, not the candidate:** if E or A2 produces
  persistence, B run alongside shows what an imposed regulator's trajectory looks like, and §3.3's
  food-supply control distinguishes them.
- **C - local-forage gating.** It fixes R1 and leaves R3 exactly as it is. The recorded evidence is
  specifically that fertility-side control does not move this collapse - eight strengths, four
  families, 24 seeds. C is a better-signalled fertility brake, and the burden the triage names applies
  to it unchanged: say why a differently-signalled brake on the same channel would separate hunger from
  collapse when the existing one did not at either end or the middle.
- **D2 - territoriality.** It meets R4 as well, but through space rather than phenotype, at the highest
  determinism risk in the set and the largest amount of new state. It is the right second-generation
  move once contest exists and the §5 loop has something to measure.
- **F - functional response.** Genuinely attractive, cheap, and it would give an already-executing
  half-wired mechanism a reader. It is third on merits only because it shares A2's target - the plant
  ratchet - and A2 removes the threshold directly where F works around it.
- **G - refugium.** Not a regulator. Run it as the cheapest available probe of whether the collapse is
  recoverable at all, and read it as a control.

---

## 5. Interaction with the frozen spec

### What the spec already anticipates

- **§9** lists "competition for resources and space", "spatial population structure",
  "frequency-dependent selection" and "environmental variability across space and time" as
  underrepresented, and says the list exists "so that proposals can point at one rather than inventing
  a rationale". E points at the first, D2 and G at the second, and all three are the kind of mechanism
  §6 names as required for the P3 gate.
- **§6** already demotes the death-mix justification the 0.75 brake was shipped on and marks it
  **PROVISIONAL**, to be decided by matched brake-on/brake-off experiments on population stability and
  reproductive-fitness variance. The triage supplies half of that for free: no brake strength anywhere
  on the axis produces a persisting population. This document removes one of the two reasons to keep
  the brake and **does not resolve its status**, because §6 rests that decision on reproductive-fitness
  instrumentation that does not yet exist. Nothing here proposes reverting it or changing its value.
- **§10** reserves implementation for plans and specs derived from the frozen document, which is why
  this is a spec with no tasks.

### The one real tension, and how it resolves

**§5 says: "Only after this loop works cleanly should further complexity be layered on."** Adding a
regulation mechanism is, on its face, exactly the complexity §5 defers.

It resolves against deferral, on the spec's own terms. **The §5 gate is currently unmeasurable.** It
requires a measurable difference in *lifetime reproductive success* responding to ecology across
replicated seeds. Every configuration in which the ecology is doing the regulating is extinct by
24,000 ticks in 24 of 24 worlds; every configuration that persists is cap-pinned, and in a cap-pinned
world the cap is deciding reproductive success rather than the ecology. There is no third kind of
world in this repository. A population that cannot be kept alive long enough to accumulate lifetimes
is not a substrate the §5 experiment can run on, so this work is a prerequisite for the gate, not a
detour around it. E in particular is work on the link §6 identifies as the broken one.

### Conflicts and build interactions to expect

- **Every candidate is a behaviour change.** `ComputeStateHash` tests will fail by design, and
  `AGENTS.md` §7 makes that acceptable **only** when the task explicitly says behaviour changes. Each
  must land behind a config flag whose off state is byte-identical, which is the pattern
  `plantFertilityAdaptationEnabled` and `establishmentContestEnabled` already establish for exactly
  this reason.
- **`LivenessTests.InertFlagsAreExactlyTheKnownSetUnderTheWidestConfiguration` will fail on F**, and on
  G if it lands via terrain. F gives `foragingEconomicsEnabled` a live consumer on the
  `IntentUtilityV1` path; terrain fields make `plantTemperatureAdaptationEnabled` live, which the
  ledger already flags as the correct signal rather than a regression. Both are real behaviour changes
  invalidating baselines measured before them, and both must be authorised rather than absorbed.
- **`LivenessTests.PlaceMemoryProbesRunButNeverTakeEffect` must keep passing.** D2 is the candidate
  most likely to tempt someone into wiring `MemorySystem.ObservePlace`; the frozen spec §10 keeps place
  memory inert pending a §3 experiment, and territoriality is not that experiment.
- **`DeterministicRandom.cs` and `TemperatureField.cs` stay untouched.** No candidate here needs
  either. If B is ever built probabilistically it needs a new `RandomDomain` member with a new number -
  permitted, never a renumber - but the deterministic form is preferred for the reason
  `GradedFertilityEnabled` records.
- **Baseline invalidation is the dominant cost of E and A2**, not implementation effort, and it should
  be sequenced against any queued measurement per field notes §2.

### What this document does not decide

- Any parameter value, for any mechanism, including the brake's. None is proposed and none was searched
  for.
- Whether the shipped brake is reverted. That remains §6's decision on §6's evidence.
- Whether more than one candidate is eventually needed. §4 recommends running them singly first
  precisely because that question cannot be answered from a factorial run.
- Whether the peak population is production-limited or access-limited (§1.4). That is the first thing
  §3.1's reporting change answers, and it is deliberately left as a measurement rather than an estimate.

# The controller comparison - richer decision machinery, in a world that now has pressure

> **PARTLY SUPERSEDED THE SAME DAY** by
> `p6-the-clean-controller-comparison-2026-08-26.md`. The claim below that the two controllers
> **cannot** be given identical machinery is true of the *pairing rule* and **false as a limit on the
> comparison**: raising `gradedFertilityStrength` from 1.5 to 4.0 makes proximity pairing work for
> the intent controller (0 of 60 extinct), so both controllers can be run on identical machinery
> after all. **The "on survival it is a tie" conclusion in Cell 1 is withdrawn** - it compared each
> controller on a different pairing rule at a brake tuned for neither. Matched, intent wins 0 of 60
> against 4 of 60 and carries **ten times the population**. The "its advantage is a brake" finding
> survives, reframed: the brake is a *precondition* for the controller paying, not the thing it was
> buying. Everything else here - the zero-births-by-construction mechanism, cell 2, the
> `urgency_exponent` result, the combat-gene floor trap - stands.

2026-08-26. `CreatureSweep`, 30 relief-selected seeds x two slope arms = 60 runs per cell per arm,
plus `--deaths` at 30 contiguous seeds (slope-off only) for composition. **Every arm below was run
with `healthRecoveryEnabled` off and on**, per the standing decision in section 4 of the handoff.
Both health arms agree on every direction reported here; where they differ in size it is said so.

## The question

`DecisionPolicyVersion.IntentUtilityV1` is the controller every P4 scenario and both playtest hotkeys
use. `Legacy` with cognition on is `DecisionSystem.DecideFromLearnedOutcomes`, a far thinner thing.
The comparison had never been run in either direction. The recorded reason to expect nothing was
`docs/emergent-behaviour-constraints-2026-08-24.md`: 96.9% of deaths were old age, one threshold
carried nearly all selection, and `ComputeNeedGain` saturated - so a richer controller had nothing to
exploit. **The world has pressure now** (graded fertility, starvation at 16%, a survivable
predator-prey cell), so the question is live.

## The first answer was wrong, and here is why

The first arm run was `--policy=legacy` in the pressured cell: **8 of 8 extinct, in 1.5 seconds.**
That is not a controller result. `mateSelectionEnabled` routes reproduction through
`ReproductionSystem.FindSeekMateTarget`, which requires the creature's decision to *be*
`CreatureAction.SeekMate`. `DecideFromLearnedOutcomes` emits only `SeekFood`, `SeekWater` and
`Wander` (`DecisionSystem.Legacy.cs`) - it cannot emit `SeekMate` at all. **A Legacy world with mate
selection on has zero births by construction** and dies of old age no matter how well it forages.

`--mate-selection=off` was added for this reason. It falls back to `FindNearestReadyMate`, proximity
pairing, which both controllers can reach. With it, Legacy survives 55 of 60.

**The two controllers cannot be given identical reproduction machinery.** That is the finding under
the finding, and everything below is shaped by it.

## Cell 1 - the pressured herbivore cell (cap 500, regen 2.0, brake 1.5, gate 0.70)

Extinct of 60 from the sweep. Population, energy, births and death composition from `--deaths`,
30 seeds. Format throughout: `health off / health on`.

| arm | extinct/60 | population | energy | births/run | starvation | dehydration |
|---|---|---|---|---|---|---|
| intent + mate selection (its home) | **3 / 1** | 299 / 331 | 0.583 / 0.574 | 492 / 541 | 16.2 / 13.2% | 0.0 / 0.0% |
| legacy + proximity (its home) | **5 / 1** | 144 / 173 | 0.650 / 0.652 | 268 / 293 | 24.7 / 24.4% | 2.4 / 2.6% |
| intent + proximity (matched pairing) | **50 / 50** | 11 / 17 | 0.030 / 0.033 | 623 / 626 | 69.0 / 68.5% | 0.1 / 0.1% |

**On survival the two home configurations are a tie** - 3 against 5 with health off, 1 against 1 with
it on. The richer controller is not buying survival here. It carries **roughly twice the population**
(299 against 144) at slightly lower energy per creature, and it is the only one of the two that
essentially never loses a creature to thirst: 0.0% dehydration against 2.4%. Multi-need arbitration
is visible there and nowhere else in this table.

**The third row is the important one.** Give the rich controller the thin controller's pairing and it
**starves itself to death in 50 of 60 runs** - 623 births per run, 69% of deaths from starvation,
mean energy 0.03, 98.5% of the living below the health gate. It is not a degradation, it is an
overshoot: the population breeds past what the habitat can feed and collapses.

So the rich controller's advantage in this cell **is not foraging skill. It is that it owns a brake.**
`SeekMate` is an intent only it can express, and requiring a creature to choose and reach a mate is
what limits its birth rate. This is the same mechanism recorded in
`p6-the-gate-is-a-survival-mechanism-2026-08-26.md`, seen from the other side: there the gate value
was varied, here the gate is removed entirely.

## Cell 2 - the predator-prey cell (the same, plus `--predation --gate=0.45`)

| arm | extinct/60 | population | energy | births/run | attacks/run | predation deaths | starvation |
|---|---|---|---|---|---|---|---|
| intent + mate selection | 10 / 10 | 129 / 126 | 0.635 / 0.631 | 146 / 149 | **6.7 / 9.3** | 1.0 / 1.5% | 4.0 / 2.6% |
| legacy + proximity | **38 / 37** | 98 / 126 | 0.537 / 0.527 | 111 / 160 | 230 / 296 | 33.3 / 26.0% | 24.5 / 34.8% |
| intent + proximity | **2 / 2** | 234 / 267 | 0.431 / 0.407 | 535 / 542 | 276 / 279 | 8.4 / 7.9% | 44.8 / 36.1% |

**The ordering inverts.** Legacy is now the worst arm by a wide margin - 38 of 60 extinct against 10
- and the configuration that was catastrophic in cell 1 is the best here, 2 of 60. Predation supplies
the mortality that starvation had to supply before, so the overshoot that killed the third row of
cell 1 is cropped from outside and the population settles at 234 instead of 11.

**Legacy dies of its own predation.** 230 attacks per run and 33% of deaths from predation, against
1.0% for intent with mate selection. `PredationSystem.Decide` is a bolt-on that runs *after* the
Legacy decision and overrides it; the intent controller scores hunting against every other intent
instead of overriding them.

**Why intent-with-mate-selection barely hunts at all is mechanism, not mystery.** Both paths compute
`hunt = HuntCapability(...) * hunger` and drop it below a hard `>= 0.10` floor
(`PredationSystem.cs:44`, `DecisionSystem.Scoring.cs:96`). At mean energy 0.635 there is not enough
hunger to clear the floor; at 0.43 there is. Across all six predation arms attack rate is monotone in
mean energy. **The direction is predicted by the code. The 40x magnitude is not derived and is not
claimed.**

## What the controller changes about selection

Drift from founders, herbivore cell, baseline arm. `t`, `health off / health on`:

| gene | intent | legacy |
|---|---|---|
| `urgency_exponent` | **-11.53 / -10.11** | **-1.34 / +1.23** |
| `temperature_tolerance` | +9.97 / +13.46 | +6.59 / +6.18 |
| `fertility_investment` | +9.64 / +4.87 | +5.57 / +3.85 |
| `lifespan_tendency` | +8.39 / +8.49 | +6.82 / +8.93 |
| `food_efficiency` | +2.17 / +2.76 | +4.28 / +2.78 |
| `neutral_marker` (control) | +1.13 / +1.63 | +0.99 / +0.45 |

**The single most reproducible selection signal in this project exists only under one controller.**
`urgency_exponent` is under t = -11 with intent and reads as a *second control* under Legacy.

This is not a surprise once checked: `genome.UrgencyExponent`'s only behavioural readers in the whole
simulation are two lines in `DecisionSystem.Scoring.cs`, which is the `IntentUtilityV1` path. **Under
Legacy the gene is not read by any code that runs.** A gene nothing reads should drift like the
neutral marker, and it does. The prediction was made from the source before the table was looked at.

The other three selected traits are selected under both controllers, more strongly under intent.
`food_efficiency` is the one trait Legacy may select harder, and the two health arms disagree about
its size (4.28 against 2.78), so **nothing is claimed for it.**

**A trap in the same table, recorded so nobody reads it as a result:** all six combat genes drift
+0.040 to +0.048 at t = 13 to 27, *in both controllers*. Founder value is exactly `0.0000` under
`PhysiologyVariation` - this is mutation off a floor, not selection. Combat questions need
`--predation`'s founder profile, as `p6-predation-selects-on-defense-2026-08-26.md` used.

## The answer

**Does richer decision machinery pay now that the world has pressure? Yes, but not for the reason the
question implies, and not by the same amount in both cells.**

- It does not pay by foraging better. In the herbivore cell the two controllers, each on its home
  configuration, survive equally often (3 against 5, and 1 against 1).
- It pays by **expressing intents the thin controller cannot express** - a mate-seeking gate that
  brakes reproduction, and a hunt score that competes with other needs instead of overriding them.
  In the predation cell the second of those is worth 28 runs of 60.
- **What it buys is contingent on what else is limiting the world.** The mate gate is worth
  everything when starvation is the only brake, and worth less than nothing - 50 of 60 extinct
  becomes 2 of 60 - once predation supplies mortality from outside.

## Limits, stated

- **The arms are not a single-variable comparison and cannot be made into one.** Legacy cannot use
  mate selection. Every row pairs a controller with the only pairing mechanism it can run.
- Two instruments, two seed sets: `--focused` selects seeds by relief and runs both slope arms;
  `--deaths` uses contiguous seeds, slope-off only. **Extinction counts from the two are not the same
  measurement** and are never mixed in one column above.
- One cell each. Cap, regeneration and brake were not varied.
- `Urgency()` under intent against a linear `1 - energy/capacity` under Legacy is a real difference in
  the hunger term that this comparison does not separate from the rest of the controller.

## What this opens

- **Is the mate gate separable from the controller?** Proximity pairing plus an explicit birth-rate
  brake would test whether intent's advantage in cell 1 survives without `SeekMate`. Nothing
  currently offers that.
- **Attack rate against mean energy** is monotone across six arms and has one hard code floor behind
  it. A resource-level sweep with the controller held fixed would turn it from consistent-with into
  measured.
- **Replication, incidentally:**
  `p6-slope-cost-focused-cap500-regen2.00-30seeds-predation-brake1.5-gate0.45-2026-08-26.csv`
  reproduced byte-for-byte at `9631800` against the copy written at `a065905` - every hash, every
  column. The `-healthrecovery` companion was rewritten from `d99b854`; every shared column and every
  hash is identical, and it gains the `maneuverability` and `fear` columns that did not exist then.

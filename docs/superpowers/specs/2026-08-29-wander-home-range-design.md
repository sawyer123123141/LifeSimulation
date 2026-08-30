# Wander home range — remove the ring, keep the memory

**Date:** 2026-08-29
**Status:** **REFUTED BY MEASUREMENT.** The design below - remove the ring - was measured and kills
the world. Kept in full because the reasoning that produced it is worth not repeating, and because
what it got wrong is the useful part. The shipped fix is hysteresis, recorded at the end.
**Supersedes in intent:** the uncommitted `WanderHomeHysteresisEnabled` work in the tree

## The problem, as observed

A human watched Play mode on the `Y` terrain playtest and reported creatures "randomly spin in a
circle 20 times super fast", later refined to a repeated 180-degree reversal rather than a full
rotation. The second description is the accurate one.

## Root cause, measured

`SimulationWorld.GetMovementTarget`'s wander branch sends a creature that holds a learned home to a
point **on** a ring of radius 3 while it is inside that radius, and to the home **centre** once it is
outside:

```
const float homeRadius = 3f;
if (Distance(position, home) > homeRadius) return home;      // outside: to the centre
...                                        return ringPoint;  // inside:  to a point ON the ring
```

The ring point sits at exactly the distance that flips the test. Arriving at it makes the creature
"too far", which sends it to the centre; walking in makes it "too close", which sends it back to the
ring. It chatters across its own boundary indefinitely.

Measured on `Y`, 12,000 ticks, using the presenter's own heading rule:

| metric | value |
|---|---|
| Wander heading updates reversing >150 deg in one tick | **13.1%** (n = 220,025) |
| Reversals belonging to creatures **with** a memory home | **28,752 of 28,753** |
| Reversals occurring within 0.25 of the 3.0 radius | **85.6%** |
| Non-reversing wander samples that near the radius | 12.1% |
| Distance-to-home at reversal, 90th percentile | **exactly 3.000** |

The presenter turns the drawn model toward its target at `TurnDegreesPerSecond` (540) scaled by
`_speedMultiplier`, which defaults to **4** — **2,160 deg/s, six revolutions a second**. That is why a
simulation-side reversal reads as spinning.

**The renderer is not at fault.** The creature genuinely ping-pongs. `28963c3`'s interpolation changed
the artefact from a snap to a spin; it did not create it.

## Where the ring came from

Introduced by `ab5bc83 feat: add watchable starter habitat checkpoint` — built to make creatures look
settled in a watchable demo. It has no spec, no experiment and no field note. It is undocumented
scaffolding that became permanent behaviour.

## Three things that are already built

Checked before designing, because the first version of this design proposed building all three.

1. **Memory already decays.** `MemorySystem.TickDecay` reduces `FoodConfidence` / `WaterConfidence`
   linearly every tick, and `MemorySystem` resets them to 1 on sighting and multiplies them by 0.35
   after a failed search.
2. **Homing already exists.** `DecisionSystem.ScoreRememberedResource` scores remembered food and
   water by confidence, age, outcome value and experience count. Creatures already return to places
   that paid off, whenever a need makes them want to.
3. **The plain wander fallback is already smooth.** It picks an angle per five-second epoch and walks
   that way. In the same 12,000-tick run, creatures with **no** memory home produced **one** violent
   reversal in total.

## Why the obvious fix is the wrong fix

The tempting design — "make home a soft pull whose strength falls off with distance" — is
[decision 1 in section 4 of the handoff](../../SESSION_HANDOFF.md), closed as a measured negative.
`HomeRangeAffinityEnabled` stays off, and the rule is explicit that the **sign** of the effect is
wrong, not its size. Two experiments back it: `p4a-home-range-affinity-2026-08-22.md` and
`p4a-route-ring-home-range-2026-08-22.md`.

One of the two stated causes of that null applies directly here: the affinity centre chased the
creature's own recent position, making distance-to-centre nearly collinear with distance-to-candidate,
which `ResourceUtility` already charged as travel cost. **A wander drift toward the best-remembered
place would be the same redundancy.** Do not build it.

## REFUTED: what the measurement said

Three arms on the `Y` configuration, 8 seeds, 12,000 ticks, matched on seed:

| arm | survived | end population | wander reversals >150 deg |
|---|---|---|---|
| ring (today) | 7 of 8 | 95-96 | **~15%** |
| hysteresis | 7 of 8 | 95-96 | **~1.6%** |
| **no ring** | **1 of 8** | 51 | 0.5% |

**Seven of eight worlds went extinct with the ring removed.** Seed 48 dies in all three arms, so that
one extinction is pre-existing and not caused by any of this.

**The ring is load-bearing.** It is an accidental tether, and the tether is what keeps a creature
close enough to remembered resources to survive. The claim below - that
`DecisionSystem.ScoreRememberedResource` already provides homing and the movement rule is redundant -
**is false**. Homing on need is not the same as staying in range: a creature that wanders freely
between needs disperses far enough that the need, when it arrives, cannot be met in time.

**The lesson.** "This mechanism is redundant with one that already exists" was reasoned from the call
paths and was wrong. The measurement took eight minutes. It should have come before the spec, not
after it - the risk section below named this exact failure and the spec was still written as a
recommendation rather than a hypothesis.

## The shipped fix: hysteresis

Recall to the centre only past 4/3 of the ring radius, so reaching the ring is an arrival rather than
a trigger. Applied to the memory-home branch and to the parental-following branch, which has the
identical defect at `followRadius = 2`.

Measured survival-neutral against today (7 of 8 in both arms, same seeds, population 95-96, mean
energy 0.79-0.82 in both) while cutting wander reversals about ninefold. Behind
`WanderHomeHysteresisEnabled`, default false so every recorded result stays reproducible, and **on for
the `Y` playtest**, which is the scenario the spinning was reported in.

## What is still wrong, and is not fixed

The user's design objection stands and is not answered by this fix: a home that is a geometric fence
is not how an animal uses ground. Hysteresis widens the fence from 3 to 4; it does not remove it.

**But the fence cannot simply be deleted** - that is what the measurement above proves. Any future
home model has to *replace the tether's ecological function*, not just remove the tether. That means a
mechanism that keeps creatures within reach of remembered resources between needs, arrived at
honestly: a rest or den behaviour they choose, a foraging range that emerges from the decision system
rather than from geometry, or a needs model that makes drifting out of range costly before it is
fatal. Each of those is a measured experiment, not a refactor.

## REFUTED DESIGN, kept for the record

**Take the home branch out of the default path in `GetMovementTarget`.** A wandering creature falls
through to the existing exploration fallback: hold a heading for five seconds, then pick a new one.
The branch is not deleted outright — it stays behind the flag below so recorded runs remain
reproducible — but it stops running in any new work.

Staying near home stops being a movement rule and becomes what it should be — a consequence of the
decision system, which already pulls a creature back to remembered food and water when a need rises.
Leaving happens when the memory decays or a better place is found. There is no boundary, so there is
nothing to chatter across.

The same defect exists in the parental-following branch immediately above (`followRadius = 2`, ring at
radius 2, identical structure). **Same treatment, behind the same flag**: a juvenile beyond the radius still walks to its parent;
inside it, it wanders. The recall is one-directional, so it has no limit cycle.

### Scope

| file | change |
|---|---|
| `SimulationWorld.cs` | put the memory-home ring block behind the flag, off by default; make the parental-following block recall-only on the default path |
| `SimulationConfig.cs` | one flag, `WanderHomeRingEnabled`, **default false** (= new behaviour), so the old behaviour stays reachable for reproducing recorded runs |
| `ExperimentManifest.cs` | the flag, or the manifest guard test fails |
| `StateFingerprintTests.cs` | pinned property count |

### Why a flag rather than a clean deletion

Every recorded result was produced with the ring in place. A flag keeps those runs reproducible.
Unusually, its default is the **new** behaviour, because the old one is a defect rather than a
scenario choice — an inversion that must be stated in the flag's own documentation.

### The uncommitted hysteresis work

Revert it. It widens the fence from 3 to 4; this design removes the fence. Both together is two
mechanisms fighting. Its measurements are preserved in this document.

## Testing

1. **The existing test, retargeted.** `WanderHomeHysteresisTests` becomes `WanderHomeRangeTests` and
   asserts the reversal rate on `Y` collapses. Threshold: **under 2%**, against 13.1% today. The plain
   fallback measured ~0%, so this has real headroom.
2. **A unit test on the target rule** — a creature at the ring radius with a home must not be handed a
   target that reverses its heading.
3. **Parental following** — a juvenile inside the follow radius wanders; beyond it, it walks to the
   parent.
4. **Reproducibility** — with the flag set to the old behaviour, the config hash and a fixed-seed run
   match the recorded values.

## What this changes, and the risk

**It moves creatures, so it moves every number.** Recorded results are not comparable across this
flag. That is the price and it should be paid deliberately.

**The real risk is dispersal.** Today the ring is an accidental tether. Removing it lets wandering
creatures drift further from resources, and the honest possibility is more starvation, lower
population, or extinction in marginal cells. This is not speculation to wave away: the handoff records
that the population is pinned to the mating gate and that the cap is the stabiliser, so a change to
where creatures spend their time can move survival.

**Therefore this is measured before it is switched on**, both arms, the way section 4 requires for
selection work: survival, mean population, starvation share, and the reversal rate, across seeds, on
the `Y` configuration and one pressured cell.

## Open questions for the user

1. **Does `Y` get the new behaviour once measured?** The playtest scenarios are under a standing
   "do not fold in silently" rule.
2. **If dispersal measurably hurts survival, which way do we go** — accept a worse ecology for honest
   movement, or keep a tether and make it a real one (a rested/den behaviour rather than a geometric
   ring)?

## Explicitly not in scope

- `SeekMate` reversals, measured at 6.7%, and `SeekThermalComfort` at 5.8%. Different mechanism
  (creatures in contact chasing each other's jitter). Separate work.
- The presenter's 2,160 deg/s turn rate. It amplifies reversals but does not cause them. Worth
  revisiting only after the simulation stops producing them.

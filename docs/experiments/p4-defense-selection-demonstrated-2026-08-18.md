# P4 — Selection on Plant Defense Demonstrated (Negative Direction) — 2026-08-18

> Third and final entry of 2026-08-18. Follows `p4-defense-no-gradient-2026-08-18.md` and
> `flag-liveness-and-the-averaging-constraint-2026-08-18.md`.
>
> **The blocker recorded in `p4-lifespan-derived-2026-08-17.md` is resolved.** A coevolution null is
> now interpretable, because the setup has been shown capable of moving plant defense.

## The design flaw that made every earlier sweep flat

Every arm from 2026-08-17 onward gave plant defense a **uniform founder value across all patches**.
Defense varied *between* arms, never *within* a run.

Selection response is proportional to standing variance. With every patch equally defended there is
nothing for grazer choice to discriminate on and nothing for selection to act on, so mutation drift
dominated and every delta read as noise. Four sweeps and two mechanism changes were spent before
this was noticed. It is not a property of the model — it is an experiment-design error.

## The clamp that made patch quality invisible

Before the variance fix, a second defect had to be cleared. `DecisionSystem.ComputeNeedGain`
(`DecisionSystem.cs:742`) ends in `Math.Min(1f, (resource.Amount * perUnitGain) / missing)`.

Measured against real patches in FULL ecosystem mode: **88 of 88 patch-and-hunger combinations
returned exactly 1.0000**, roughly 10x over the clamp, at every hunger level down to 5% energy.

So `ResourceUtility` reduced to `urgency - travelBurden - dangerPenalty`. Patch quality played no
role in foraging choice at all under `IntentUtilityV1`; grazers chose by hunger and distance only.
Since plant defense acts on a patch's nutrition density, defense could not make a patch less
attractive, and grazing was uniform by construction.

Addressed by `SimulationConfig.PlantQualityPreferenceEnabled` (default `false`, flag-off path is the
original expression verbatim, 365 tests green). When set, a patch's utility is weighted by its
nutrition density, so a richer patch is preferred even when both would fully satisfy the need.

Note this is a *different* defect from the dead `learnedResourceQualityEnabled` flag: that one gates
`DecideFromLearnedOutcomes` on the Legacy path and is inert
(`flag-liveness-and-the-averaging-constraint-2026-08-18.md`).

## Measurement

Six active sites as shipped, but **three seeded undefended and three seeded defended**, so the plant
population starts with real between-patch variance. Seeds 42–71, 12,000 ticks, cognition, site
competition, plant mortality, quality preference on.

| defended-site defense | mean start | mean end | delta | deterrence | extinctions | min plant gen |
| ---: | ---: | ---: | ---: | --- | ---: | ---: |
| 0.00 | 0.000 | 0.0715 | +0.0715 | off | 0/30 | 13 |
| 0.30 | 0.150 | 0.1665 | +0.0165 | off | 0/30 | 12 |
| **0.60** | 0.300 | 0.2463 | **-0.0537** | off | 0/30 | 14 |
| **0.90** | 0.450 | 0.4004 | **-0.0496** | off | 0/30 | 12 |
| 0.00 | 0.000 | 0.0700 | +0.0700 | on | 0/30 | 13 |
| 0.30 | 0.150 | 0.1796 | +0.0296 | on | 0/30 | 13 |
| **0.60** | 0.300 | 0.2436 | **-0.0564** | on | 0/30 | 12 |
| **0.90** | 0.450 | 0.3896 | **-0.0604** | on | 0/30 | 13 |

The 0.00 rows are the clamp-boundary artifact: symmetric mutation against a floor at 0 truncates
negative draws. They are not a response.

### This is a real response, not drift

The -0.05 to -0.06 deltas are **ten to thirty times** the drift-level deltas measured in every
uniform-founder sweep (+0.0049, -0.0026, +0.0017, -0.0054, …), consistent in sign across both
deterrence arms and both nonzero variance levels, with zero extinctions and healthy plant turnover
in all 30 seeds. Populations are not collapsing — unlike the regeneration-3 arm, this is a
functioning ecosystem.

**Plant defense is under measurable directional selection here, and the direction is downward.**

## What this does and does not establish

**Establishes:** the plant selection pipeline works. Defense is heritable, variable, and responds to
selection at a magnitude far above noise, in a stable ecosystem. Any future null on plant defense
can now be read against this — the machinery is not inert. That was precisely what
`p4-lifespan-derived-2026-08-17.md` demanded before the coevolution experiment could mean anything.

**Does not establish:** that the decline is *grazing-mediated*. Turning deterrence on barely changes
it (-0.0537 to -0.0564 at 0.60; -0.0496 to -0.0604 at 0.90). If grazing pressure were driving the
loss, protecting a defended patch's biomass should have slowed it measurably. It did not.

The likely driver is the **growth-rate cost**: `PlantPhenotype` charges `-.15f * Defense` against
growth and `-.25f * Defense` against nutrition. Under site competition and mortality, a slower-growing
patch loses colonisation races to its undefended neighbours regardless of who is being eaten.

### The attribution test that has not been run

Run the same mixed-founder scenario with grazing removed or heavily suppressed. If defense still
declines by roughly -0.05, the growth cost fully explains it and grazing contributes nothing. If the
decline flattens, grazing carries part of it. **Do not describe this result as grazing-driven
selection until that arm exists.**

## Standing conclusion for P4's exit gate

The exit gate asks for a repeatable reciprocal plant/consumer trait response. On this evidence
plant defense will not rise under any mechanism tried — deterrence, patch avoidance, or raised
pressure — because it is net-costly to the individual patch in every one of them. A reciprocal
response requires defense to have a positive fitness route that outweighs its growth cost, and no
such route currently exists in the model. That is a design question, not a calibration one.

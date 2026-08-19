# P4 — Selection on Plant Defense Demonstrated (Negative Direction) — 2026-08-18

> ## RETRACTED 2026-08-18 (same day)
>
> **The central claim of this document is withdrawn.** It reported the -0.05 decline as
> "measurable directional selection". That rested on comparing the delta against the *uniform*
> arms' deltas, which have small spread **by construction** — no standing variance means no
> lineage lottery, so their SD is small for reasons unrelated to selection.
>
> Compared against its own sampling error over the same 30 seeds:
>
> | arm | mean Δ | SD | SE | t | 95% bootstrap CI | direction |
> | --- | ---: | ---: | ---: | ---: | --- | ---: |
> | variance | -0.0548 | 0.2083 | 0.0380 | **-1.44** | **[-0.1222, +0.0319]** | 20 down / 10 up |
> | variance + deterrence | -0.0575 | 0.1942 | 0.0355 | **-1.62** | **[-0.1212, +0.0139]** | 19 down / 11 up |
> | uniform | +0.0002 | 0.0694 | 0.0127 | 0.02 | [-0.0224, +0.0242] | 17 down / 13 up |
> | uniform + deterrence | -0.0025 | 0.0649 | 0.0118 | -0.21 | [-0.0244, +0.0209] | 18 down / 12 up |
>
> **Every interval contains zero.** Direction consistency for the variance arm is 0.67, against
> the 0.57 that `p4-coevolution-null-2026-08-17.md` correctly read as a null. This is the
> project's recorded "n=5 looked real, vanished at n=30" failure in a subtler form: the error was
> not sample size but choosing a comparison baseline whose spread was small for structural reasons.
>
> What survives: standing variance roughly **triples the outcome spread** (SD 0.208 vs 0.069).
> That is drift being given room to operate in a population of six to thirty patches, not a
> directional signal.
>
> **The blocker in `p4-lifespan-derived-2026-08-17.md` is therefore still open.** A coevolution
> null remains uninterpretable, because the setup has *not* been shown capable of moving plant
> defense. Charts and full statistics: `docs/experiments/defense-drift-report.html`.
>
> The mechanism findings below (the `ComputeNeedGain` clamp, the standing-variance design error)
> stand and are unaffected. Only the significance claim is withdrawn.

> Third and final entry of 2026-08-18. Follows `p4-defense-no-gradient-2026-08-18.md` and
> `flag-liveness-and-the-averaging-constraint-2026-08-18.md`.

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

### ~~This is a real response, not drift~~ — WITHDRAWN

> The paragraph below is the retracted claim, kept verbatim so the reasoning error stays visible
> rather than being quietly edited away.

~~The -0.05 to -0.06 deltas are **ten to thirty times** the drift-level deltas measured in every
uniform-founder sweep (+0.0049, -0.0026, +0.0017, -0.0054, …), consistent in sign across both
deterrence arms and both nonzero variance levels, with zero extinctions and healthy plant turnover
in all 30 seeds.~~

The error: those uniform-founder deltas are small because uniform founders have **no standing
variance**, so no lineage can fix and the across-seed SD is structurally tiny. Comparing against
them measures the presence of variance, not the presence of selection. The correct comparison is
each delta against its own SE — done in the retraction banner above, where every interval
contains zero.

The parts that do hold: zero extinctions across all 120 runs, population at the cap of 48, and
about fifteen plant generations in every arm. Unlike the regeneration-3 arm, this is a functioning
ecosystem — so the null is a null from a working system rather than a collapsed one.

## What this does and does not establish

**~~Establishes: the plant selection pipeline works.~~ WITHDRAWN** — see the retraction banner. At
n=30 this design cannot distinguish selection on plant defense from drift, so the positive control
`p4-lifespan-derived-2026-08-17.md` demanded does **not** exist yet.

**Establishes instead:** the obstacle is statistical power, and its cause is identified. With six
active sites (up to about thirty patches once dispersal targets are colonised) the plant population
is small enough that lineage fixation by drift moves the mean further than selection plausibly
could. Standing variance triples the outcome SD, which is exactly what drift-with-more-room looks
like.

Routes to an answer, in order of directness: **more patches** (drift scales inversely with
population size, so this shrinks the spread far more efficiently than more seeds); **more seeds**
(at SD 0.21 and an effect near -0.055, ~230 seeds for 80% power — roughly eight times the compute,
worth spending only if the effect is believed real); or **remove the confound first** via the
grazing-suppressed arm below.

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

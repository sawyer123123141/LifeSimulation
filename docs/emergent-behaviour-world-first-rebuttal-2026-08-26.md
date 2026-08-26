# Against "enrich the world before enriching the brain"

**2026-08-26.** Argues against §1 of `emergent-behaviour-constraints-2026-08-24.md`, at that
document's own invitation ("This is a hypothesis, not a conclusion — argue against it"). It does not
argue for a controller architecture. It argues that **the ordering claim is unsupported, that two of
its three supporting measurements are properties of the configuration rather than of the world, and
that its cheapest step has a recorded failure behind it while a controller-side step is a flag flip.**

## What is conceded

A world with **zero** predation cannot select for fleeing or hunting; a world where nothing starves
cannot select for caching. That much is arithmetic and stands. The disagreement is about what follows.

## 1. "No predation" is the scenario's scope, not the world's poverty

Predation reads 0 because the measured runs are a **herbivore calibration**. This is already recorded
in the field-notes ledger: `RiskAversion` "reads dead under `CreatePrototype4Defaults` because the
herbivore calibration produces no threats", and `multiThreatPerceptionEnabled` and
`kinRecognitionEnabled` are inert for exactly the same reason — their use sites sit inside
`if (predationEnabled)`.

**The predation machinery is built, wired and unexercised.** So "enrich the world" here does not mean
adding a mechanism; it means producing a **survivable predator-prey scenario** — and
`p4-inert-flags-readjudicated-2026-08-19.md` records that attempt failing:
`FounderProfile.PredationVariation` goes extinct before 3,000 ticks with zero births, so every verdict
taken there was "measured on a corpse". **The rival proposal's cheap first step is the step this
project has already tried and not landed.** Calling it cheaper than a controller is an estimate with
a counterexample attached.

## 2. "Nothing starves" is a cap setting, not a world property

15 starvations in 5,619 deaths was measured at **cap 100**, where — per
`p6-the-cap-is-the-stabiliser-2026-08-24.md` — the cap, not the ecology, supplies the regulation. The
same model at **cap 500** starves **35–64%** of its deaths.

**The starvation gradient the proposal wants to build already exists and is reachable by changing one
number.** What removed it was a configuration choice. A fact that inverts under a cap change cannot
carry an argument about which of the world or the controller is impoverished.

## 3. The strongest of the three measurements points at the controller, not the world

`ComputeNeedGain` saturating at 1.0 for every patch is **not an environment fact**. The environment
already differentiates patches: plant `NutritionMultiplier` varies, and the plant genome is under
measured selection on exactly those traits — from the 2026-08-26 re-run
(`p6-plant-corpus-revalidated-unpinned-2026-08-26.md`), Defense **t +4.27 to +5.66** and Nutrition
**t +2.10 to +2.73** across four cells of 60 seeds.

**The variation is there and the decision function discards it**, in one clamp, at
`DecisionSystem.Scoring.cs:269`.

And **the channel is already written**: `plantQualityPreferenceEnabled` weights a candidate patch by
nutrition density, is threaded from `SimulationConfig` through both scorers, and is hashed.

> **CORRECTED 2026-08-26, same day, my error.** This paragraph continued "and defaults **false**. The
> presenter turns it on; the sweeps do not." **The default is false and the conclusion is wrong** —
> that is the *constructor parameter* default. Both sweeps pass `true` explicitly
> (`tools/PlantSweep/Program.cs:176`, `tools/CreatureSweep/Program.cs:300`). **I read a declaration
> instead of the call sites, which is the error this section accuses the constraints doc of making.**
>
> **The section's conclusion survives, and is now measured rather than argued.** Because the channel
> is on everywhere, the test is to turn it **off**:
> `experiments/p6-patch-quality-is-not-a-free-parameter-2026-08-26.md`. Across 240 paired runs,
> **population -31.9 (t -6.42)**, **occupancy +0.201 (t +10.69)**, **0 of 240 hashes matching**;
> Defense drift **+2.39** with the channel on and Nutrition drift **-3.71**. **"Nothing to be smarter
> about" is false where patch quality varies.** What is corrected is the claim that the recorded
> corpora were run blind to quality — they were not.

So the doc's flagship evidence for an impoverished world is, read against the source and then
measured, a statement about **one term of the score** rather than about the information available to
a forager. The channel that reads patch quality is on in every recorded run and decides a third of the
population. **This is the inversion the whole argument turns on**, and it cost nothing to test —
against predators that have never survived 3,000 ticks.

## 4. "One threshold carries the selection" is not evidence for ordering

The 0.80 gate dominates because it is the only place the creature's entire state is tested against a
criterion. Adding predation adds a **second** such place; it does not change the shape.

Worse for the proposal: the dose-response found the population sits **0.006** above the gate and that
**raising the gate raises the population's energy** — a feedback equilibrium, which is why the
predicted cliff never appeared. A population that re-equilibrates against a moved threshold will
re-equilibrate against added mortality too. **"Add pressure and a gradient appears" is the same
prediction the cliff was**, and the cliff was wrong for a documented reason.

## 5. The ordering claim is a 2x2 asserted as a sequence

"Controller richness cannot pay until world richness increases" is a claim about **interaction**:
controller poor/rich × world poor/rich. This project decides claims of that shape with paired seeds
and a t-statistic, and the machinery is sitting there — `CreatureSweep --focused`, `PlantSweep`,
flag-gated arms that are byte-identical when off.

**Accepting a sequence on assertion abandons the method on the one question where overstatement is
easiest** — which §3 of the source document says out loud about behaviour claims, then §1 does not
apply to itself.

## 6. The shape of this claim is the shape of the ones that failed

The session behind it produced, by its own record: a two-point comparison that manufactured a
five-trait claim five points deleted; a mechanism read from source that explained 19% of an effect and
was written up as the cause; a cap/brake confound that made a collapse look harmless; and a predicted
cliff that measurement turned into a smooth curve. **Every one is a single sufficient-looking
mechanism adopted before it was measured against an arm.** "Enrich the world first" has one session,
no arm, and a mechanism story. Its own numbers do not make it different in kind — they make it easier
to adopt.

## 7. And world enrichment cannot deliver three of the target behaviours anyway

Caching, territory and shelter need **place memory**, which is deliberately inert and pinned inert by
`LivenessTests`. No amount of predation or scarcity reaches them. That half of the goal is blocked on
a **state and controller** decision, which §2 of the source document identifies and which §1's
ordering does not address.

## What to do instead of choosing an order

1. ~~**Flip `plantQualityPreferenceEnabled` in a sweep arm**~~ **DONE, 2026-08-26** — inverted, since
   the channel was already on: `--quality=off` on `PlantSweep`. It changes a third of the population
   and a fifth of plant occupancy, and every hash. §1's "no information to exploit" does not survive
   it. Details and scope: `experiments/p6-patch-quality-is-not-a-free-parameter-2026-08-26.md`.
2. ~~**Re-measure the death mix at cap 250–500 with the brake at a scenario-appropriate strength.**~~
   **DONE, 2026-08-26** — and the naive form of it was confounded, since suppressing starvation is what
   the brake is for. Measured with the brake **on and off**, ten cells:
   `experiments/p6-starvation-is-a-dial-2026-08-26.md`. Starvation runs **49.6% to 0.0%** of deaths
   under one ecology across brake strength, so **"nothing kills a creature" is the default
   configuration, not the model**. At **brake 1.5, 30 of 30 worlds survive with 16.2% starvation** —
   a survivable world with real mortality pressure, no predation and no new mechanism.
3. **Then, and only then, price predation** — against the recorded failure of the last attempt, not
   against an estimate. **Now with a survivable pressured configuration to attempt it in**, which the
   2026-08-19 attempt did not have.

**None of these is "build a neural controller".** The claim being rejected is the *ordering*, not the
value of enriching the world.

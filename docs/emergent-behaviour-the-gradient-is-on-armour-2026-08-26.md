# The gradient is on armour, not on behaviour - and the world/brain ordering is a crossover

> **THE GENE IN SECTION 1 IS WRONG - CORRECTED 2026-08-29** by
> `emergent-behaviour-fleeing-is-selected-against-2026-08-29.md`. This document names `FearResponse`
> as "the Flee decision" knob. **It is not, on the path every cell here ran.** Under
> `IntentUtilityV1` the flee score is `threatIntensity * genome.RiskAversion`
> (`DecisionSystem.Scoring.cs:96`); `FearResponse` is read only by `PredationSystem.Decide`, which is
> **`Legacy`-only**, and by the place-memory penalty, which is **inert by standing decision**. So
> `fear`'s 1-of-18 was not evidence about fleeing - it was evidence that nothing reads it, the same
> situation as `urgency_exponent` under `Legacy`.
>
> **The conclusion inverts.** The real flee knob, `risk_aversion`, crosses |t| = 2 in **20 of 22**
> cells and is **negative, -5.92 to +0.16**. It is not that behaviour has no gradient; **the gradient
> on defensive behaviour is strong and points the wrong way.** Caution is bred out at t = -3 to -6
> while armour is bred in at t = +11, because `RiskAversion` also governs avoiding food near threats
> and this cell loses 44.8% of deaths to starvation against 8.4% to predation.
>
> **What survives unchanged:** the `defense`-is-passive observation (22 of 22, and `Defense` still
> enters no decision), the hunt knobs being selected against, and **all of section 2** - the
> world/brain crossover 2x2 and its cap-pinning caveat are untouched.


2026-08-26, latest. Two results bearing directly on "can evolution invent behaviours nobody
programmed". Both were produced while trying to *falsify* the last surviving claim of
`emergent-behaviour-constraints-2026-08-24.md` §1. **One of them sharpened it instead.**

## Where this picks up

§1 of the constraints document was contested on 2026-08-26 and three of its four supports were
withdrawn (`emergent-behaviour-world-first-rebuttal-2026-08-26.md`). **One support was left
standing:** that predation is zero and *the named target behaviours have no fitness gradient*. The
predation work since then has made the first half of that obsolete - predation now runs at 1% to 33%
of deaths depending on the cell. So the surviving claim looked ready to fall.

**It did not fall. It got more specific and more damaging.**

## 1. The world now selects hard - on a trait that no decision reads

Drift-from-founders `t` across the **eighteen adequately-powered predation cells** from
`p6-the-predation-cell-is-robust-2026-08-26.md` (gate, cap, regeneration and brake axes, both health
arms; the two near-extinct gate-0.65 cells are excluded):

| gene | what reads it | \|t\| > 2 in | range |
|---|---|---|---|
| `defense` | `PredationSystem` success formula **only** | **18 / 18** | +2.48 to +10.97 |
| `maneuverability` | same success formula only | 5 / 18 | +0.87 to +4.43 |
| `fear` (`FearResponse`) | **the Flee decision** | **1 / 18** | -0.36 to +2.13 |
| `attack` (`AttackPower`) | success formula **and** hunt score | 15 / 18 | **all negative**, -1.50 to -4.21 |
| `aggression` | multiplies the hunt score | 4 / 18 | negative where it crosses |
| `neutral_marker` | nothing - the control | 2 / 18 | +0.04 to +2.68 |

**`defense` and `maneuverability` are passive.** Grep confirms it: outside genome/phenotype plumbing,
`Defense` appears only inside
`successChance = AttackPower / (AttackPower + Defense + 0.25*Maneuverability + 0.01)` and in the
plant system. **Neither changes a single decision.** A creature with high `defense` does not behave
differently; it is simply harder to kill.

**`FearResponse` is the behavioural knob** - it scales the threat term that decides whether a creature
flees (`PredationSystem.cs:43`) and penalises remembered-threat places in `ForagingEconomics`. It
crosses |t| = 2 in **one cell of eighteen. The control crosses in two.** By the project's own
standard, `fear` is indistinguishable from a gene nothing reads.

### What that means for the question

**The named behaviours still have no gradient, and now we know why.** It is no longer "nothing kills
you" - things kill you. It is that **the cheapest answer to being killed is armour, and armour is
free of behaviour.** Selection took the passive route and left the behavioural knob at control level.

- **Flee:** the trait that would make it evolve is unselected. No gradient.
- **Hunt:** the two knobs that would make it evolve are selected **against** (`attack` negative in 15
  of 18, `aggression` in 4 of 18). Not merely absent - **actively maladaptive** in this cell. Evolved
  hunting cannot appear here.
- **Cache, territory, shelter:** unchanged, and still blocked by the standing decision that
  `MemorySystem.ObservePlace` stays inert (§2 of the constraints document).

**Five named behaviours, zero positive gradients.** Adding predation created a strong selective
channel and it flowed entirely into a stat.

**This is a design constraint, not a defect.** If the goal is evolved *behaviour*, the success formula
is the thing to look at: while a scalar on the defender cancels the attack directly, nothing a
creature *does* can compete with simply having more of that scalar. That is a statement about the
combat model, not about the controller.

## 2. World versus brain is a crossover interaction, measured

The rebuttal asked for this explicitly - *"controller poor/rich x world poor/rich is a 2x2, and this
project decides those with paired arms"*. Here it is, **fully matched**: brake 1.5, proximity pairing,
gate 0.45, cap 500, regen 2.0. Only the controller and the presence of predation move. Extinct of 60,
`health off / health on`:

| | poor brain (`Legacy`) | rich brain (`IntentUtilityV1`) |
|---|---|---|
| **poor world** (no predation) | **0 / 0** | **59 / 57** |
| **rich world** (predation) | **38 / 37** | **2 / 2** |

**Enriching either one alone is worse than enriching neither.** The brain alone goes 0 to 59; the
world alone goes 0 to 38; both together land at 2. Both health arms agree to within two runs on every
corner. **Neither "world first" nor "brain first" is right, and the ordering question was
malformed** - it is an interaction, exactly as the rebuttal argued and as this now measures.

### The caveat that survival counts hide, and which nearly cost me the reading

**The poor/poor corner is not a healthy world; it survives by pinning against the cap.** Its
composition: **70.4% of deaths from starvation**, population **403 with a median of 468 under a cap
of 500**, mean energy 0.470, 1,141 births per run. That is the cap supplying the regulation - the
precise pathology `p6-the-cap-is-the-stabiliser-2026-08-24.md` identified and the graded-fertility
work was built to remove. It reads as "0 of 60 extinct" and it is the *least* self-regulating cell in
the table.

The rich/rich corner, by contrast, holds **population 234 at 44.8% starvation** with the cap never
binding.

**So the honest form of the result:** enriching one side alone is catastrophic; enriching both
produces the only cell here that is both survivable and not cap-regulated. **A 2x2 read on
extinctions alone would have called the poorest world the best one.**

## 3. What this changes for a design

- **Do not build a richer controller expecting evolved flee or hunt.** Neither has a positive
  gradient in the world as it stands, and hunting has a negative one. The controller is not the
  binding constraint on those two.
- **The binding constraint is the combat model's shape.** Resistance is a scalar in a denominator, so
  armour dominates behaviour by construction. If evolved defensive *behaviour* is wanted, that
  formula is where the design has to bite - not the decision system.
- **§1's rival proposal is now settled and both halves of it were wrong.** Not "world first", not
  "brain first". The pair, or neither.
- **§2, §3 and §4 of the constraints document are untouched.** The place-memory contradiction still
  blocks three of the five behaviours; the ablation / null-controller / cross-seed criteria are still
  the right bar; the determinism and flag-gating constraints still hold.

## Not claimed

- `fear` being unselected is **not** evidence that fleeing never happens - only that its intensity is
  not under measurable selection. Flee frequency was not instrumented and no statistic reports it.
- The armour-versus-behaviour split is measured in the **predation cell only**, on
  `PredationVariation` founders. It is a statement about this combat model, not a general one.
- The 2x2 is one brake value, one gate, one scenario family. Brake strength is scenario-specific by
  prior measurement, so the corner values will move elsewhere; **the crossover sign is the claim, not
  the magnitudes.**
- I set out to falsify §1's surviving claim and confirmed it by a different route. That is recorded
  because the direction of the attempt is part of the evidence.

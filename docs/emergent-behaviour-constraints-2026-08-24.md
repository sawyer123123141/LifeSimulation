# Before designing for emergent behaviour: what was measured on 2026-08-24 that bears on it

> ## SECTION 1'S LAST SURVIVING SUPPORT: CONFIRMED BY A DIFFERENT ROUTE — 2026-08-26 (later)
>
> `emergent-behaviour-the-gradient-is-on-armour-2026-08-26.md`. The banner below leaves §1's opening
> observation standing - *predation is 0 and the named behaviours have no gradient in the measured
> scenario*. **Predation is no longer 0** (1% to 33% of deaths), so that support was expected to fall.
> **It did not. It sharpened.** Across eighteen powered predation cells, `defense` - which is read
> **only** in the attack success formula and changes no decision - crosses |t| = 2 in **18 of 18**,
> while `fear`, the actual Flee knob, crosses in **1 of 18 against the control's 2**, and the hunt
> knobs `attack` and `aggression` are selected **negatively**. **The world now selects hard, and
> entirely on armour.** Five named behaviours, zero positive gradients. The binding constraint is the
> combat model's shape, not the controller.
>
> **And the ordering question is settled as a crossover**, matched 2x2, both health arms: poor
> world/poor brain **0 of 60** extinct, brain alone **59**, world alone **38**, both **2**. Enriching
> either side alone is worse than enriching neither. **Neither "world first" nor "brain first".** But
> read the caveat: the 0-of-60 corner survives by pinning against the cap at 70.4% starvation, so
> extinction counts alone would call the poorest world the best.

> ## SECTION 1 CONTESTED AND PARTLY WITHDRAWN — 2026-08-26
>
> **The rival proposal in §1 — "enrich the world first" — was argued against as this document asked,
> and three of its four supports do not hold.** Full argument:
> `emergent-behaviour-world-first-rebuttal-2026-08-26.md`. Measurement:
> `experiments/p6-patch-quality-is-not-a-free-parameter-2026-08-26.md`. **Nothing below is edited** —
> the reasoning stays visible, because the way it failed is the reusable part.
>
> - **"Almost nothing kills a creature" is a cap-100 figure.** The same model starves **35–64%** of its
>   deaths at cap 500 (`p6-the-cap-is-the-stabiliser-2026-08-24.md`). Both numbers were mine and I
>   quoted the one that suited the argument. **A fact that inverts on one integer cannot carry a claim
>   about world poverty.**
> - **"The foraging decision carries no information to exploit" is measured false.** I found
>   `ComputeNeedGain` saturating and did not check what else reads the same score.
>   `plantQualityPreferenceEnabled` weights a patch by nutrition density, is hashed, and is passed
>   **true** by both sweeps. Turned off across 240 paired runs it moves **population -31.9 (t -6.42)**
>   and **plant occupancy +0.201 (t +10.69)**, with **0 of 240 hashes matching**. Defense drift rises
>   with the channel on (**t +2.39**) and Nutrition drift falls (**t -3.71**) — a two-sided pressure
>   that does not exist under uniform grazing.
> - **"Enriching the world is the cheaper step" has a counterexample I did not know.**
>   `p4-inert-flags-readjudicated-2026-08-19.md` records the predator-prey attempt extinct before
>   3,000 ticks with zero births.
> - **The ordering is an interaction asserted as a sequence.** Controller poor/rich x world poor/rich
>   is a 2x2, and this project decides those with paired arms.
>
> **What survives:** §1's opening observation that predation is 0 and that the named target behaviours
> have no gradient in the *measured scenario*; §2 (the place-memory contradiction), §3 (the ablation /
> null-controller / cross-seed criteria) and §4 (the architectural constraints) are untouched.
>
> **One correction in the other direction**, for the rebuttal's §4: the margin above the mating gate is
> not constant at 0.006. It runs **0.167 / 0.089 / 0.064 / 0.041 / 0.006** across the five gate values —
> the population re-equilibrates relative to the gate, but the margin **shrinks**.


**Written for whoever takes the "can evolution invent behaviours I did not program" question.**
This is not a design. It is the set of facts from the 2026-08-24 session that a design would be wrong
to ignore, plus one contradiction in the existing docs and one ready-made success criterion.

The question, as posed: give organisms primitive capabilities and sensory inputs, do **not** program
"hunt", "flee", "shelter", "cache food", "territory", and see whether mutation, inheritance, learning
and selection discover strategies that were not encoded — **measurably and reproducibly**, not just
interestingly.

## 1. The likeliest obstacle is the environment, not the controller

Every option usually proposed — evolved utility weights, recurrent controllers, behaviour trees,
neuroevolution, evolvable topology — enriches **the thing being selected**. The measurements below
say the problem may be **what is doing the selecting**.

**Almost nothing kills a creature.** Death mix over 5,619 deaths at the standard configuration
(`p6-nothing-starves-2026-08-24.md`):

| cause | share |
|---|---|
| old age | **96.9%** |
| health | 2.9% |
| starvation | 8 deaths |
| dehydration | 7 deaths |
| **predation** | **0** |

A strategy for fleeing, hunting, sheltering or caching cannot be selected for in a world where
nothing eats you and nobody starves. **The named end-goal behaviours have no fitness gradient to
climb.**

**One threshold carries nearly all measurable selection.** The mate-seeking gate — energy, hydration
**and** health each at 80% — is the dominant selective channel
(`p6-the-mating-gate-is-the-selection-2026-08-24.md`, `p6-gate-dose-response-2026-08-24.md`). Slacken
it and selection on the strongest behavioural trait collapses:

| gate | 0.45 | 0.55 | 0.60 | 0.65 | **0.70** |
|---|---|---|---|---|---|
| `urgency_exponent` | −0.44 | −1.02 | −2.01 | −7.13 | **−14.55** |

The population sits **0.006** above the threshold that decides whether it can breed. If essentially
every fitness route runs through "clear the gate", a richer controller gets **more parameters and the
same behaviour**.

**The foraging decision carries no information to exploit.** `ComputeNeedGain` returns exactly 1.0
for every active patch at every hunger level down to 5% energy — the source says so in a comment at
`DecisionSystem.Scoring.cs:270`. Foraging reduces to `urgency − travel − danger`. **A smarter
controller has nothing to be smarter about**, because the input that would distinguish patches is
constant.

**So the rival proposal a design should have to beat: enrich the world first.** Real predation
pressure, real starvation risk, differentiated patches, and a fitness landscape with more than one
route. That is cheaper than a new controller architecture and it is a precondition for one to show
anything. **This is a hypothesis, not a conclusion** — argue against it rather than adopting it.

## 2. A contradiction between the roadmap and the goal

**`MemorySystem.ObservePlace` is on the do-not-touch list.** Place memory is deliberately inert
(handoff section 4, "Place memory stays inert. Never wire it").

**Caching food, territoriality and shelter all require place memory.** Three of the named target
behaviours are impossible while that decision stands.

That is not an argument for overturning it — it was closed for reasons — but **the roadmap and the
stated end goal currently contradict each other**, and somebody has to resolve it on purpose rather
than discover it halfway through an implementation.

## 3. A success criterion this project already has the machinery for

The hardest part of the question is separating **"a new behaviour emerged"** from **"a behaviour I
programmed produced an output I did not expect"**. This project already solved the analogous problem
for *genes*: `NeutralMarker` is inherited, mutated and hashed like any gene and read by no behaviour
system, so it is the noise floor every selection claim is measured against.

The behavioural analogues:

- **Ablation.** Remove the primitive the strategy supposedly composes from. If the strategy survives,
  it was not composed of that. If it disappears, the composition is real. This is the same shape as
  the flag-liveness harness.
- **A null controller.** Identical parameter count, identical mutation, **no selection**. If a
  "discovered" strategy appears there too, it is drift or an artefact of the representation — exactly
  the role the control gene plays.
- **Reproducibility across seeds.** A strategy that appears in one world is an anecdote. The project's
  standard is paired seeds and a t-statistic against a control.

**Beware the failure this project keeps catching:** a two-point comparison manufactured a five-trait
claim that five points deleted; a mechanism found in the source explained 19% of an effect and was
written up as the cause. **An emergent-behaviour claim is far easier to overstate than a gene claim**,
because it has no obvious control unless one is built deliberately.

## 4. Constraints any architecture has to survive here

- **Determinism is load-bearing.** `ComputeBehaviorHash`, `ComputeStateFingerprint`, paired-seed
  sweeps and `FlagLivenessAnalysis` all assume bit-reproducible runs. A recurrent or neural controller
  is compatible, but the whole verification method has to extend to it — that is a real cost, and it
  is the reason the graded fertility brake scales a **cooldown** rather than a **probability**.
- **`Assets/Scripts/Simulation/` contains no Unity types**, which is what lets everything run
  headlessly in `tools/HeadlessTests`. Any controller must keep that.
- **Flag-gated, default false, flag-off byte-identical.** Five flags landed this way on 2026-08-24
  and nothing recorded moved. A new controller should arrive the same way, running **alongside** the
  utility system rather than replacing it, so the two can be compared on the same seeds.
- **Performance is not currently a constraint.** 1,090 renderers and 566,272 triangles run at 354 fps
  (`p6-play-mode-profiled-2026-08-24.md`). There is headroom for a more expensive controller — but
  **creature rendering at full population has never been profiled**; the readings had 9 to 18
  creatures against a cap of 100.

## 5. What this document is not

It does not pick an architecture, and it does not say the question is premature. It says that **three
of the measurements taken on 2026-08-24 bear directly on the answer and were not available when the
question was framed**, and that a design which does not account for a world with no predation, no
starvation, one dominant threshold and a saturated foraging signal will be answering a different
question than the one asked.

# Before designing for emergent behaviour: what was measured on 2026-08-24 that bears on it

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

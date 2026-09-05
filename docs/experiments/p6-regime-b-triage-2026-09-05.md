# Regime B triage: do C2, C3, C4 and C5 persist past 12,000 ticks?

**Date:** 2026-09-05. **Status:** PREDECLARATION — written and committed **before any run started**.
Results are appended below in a later commit; nothing in this section is edited afterwards.

**Plan:** `docs/superpowers/plans/2026-09-03-run-length-validity-audit.md`, section 5 (deferred
triage). **Ledger:** `docs/experiments/cell-family-persistence-ledger.md`.

**No biological value is tuned here.** Brake, cap, regeneration and gate are held at each cell's
recorded values. No brake value is proposed. The frozen spec is untouched.

## What is being screened, and at what length

The four Regime B cells the ledger records as INDETERMINATE, at **24,000 ticks** — the length derived
in section 5 of the plan from the only two measured collapse curves (peak at tick 8,000, crash
12,000–20,000, outcome settled by 24,000).

| cell | command |
|---|---|
| C2 | `CreatureSweep --deaths 6 500 --regen=2.0 --brake=1.5 --predation --gate=0.45 --ticks=24000` |
| C3 | the same without `--predation --gate=`, at **brake 1.4, 1.5 and 1.6** (the ledger's range) |
| C4 | the same without predation, `--mate-selection=off`, at **brake 3.0 and 5.0** (the ledger's range) |
| C5 | `PlantSweep -- 6 --cap=250 --brake=1.0 --ticks=24000` |

**Six seeds, 42–47.** That is the maximum the plan's screen rule allows, and the rule governs how the
output may be read:

> A 3–6 seed screen may flag a cell for follow-up. **It may never clear one.** At that seed count the
> screen cannot distinguish 22 of 24 from 18 of 24, so "not obviously dying" is an absence of evidence
> and must be recorded as exactly that.

So the only verdicts available here are **COLLAPSING** and **INDETERMINATE**. PERSISTENT requires 20+
seeds and is out of reach by construction. A cell that survives this screen has shown one thing only:
it does not crash on C1's clock.

## The criterion, unchanged

From the ledger, predeclared 2026-09-03. Nine samples; `alive(k)` is worlds with a living population
at sample `k`; `mean(k)` is the **all-world** mean population, extinct worlds counted as zero.

- **COLLAPSING** — `mean(7) > mean(8) > mean(9)`, or `alive(9) < 0.85 × alive(3)`.
- **INDETERMINATE** — anything else at fewer than 20 seeds, which is every cell here.

At six seeds `0.85 × alive(3)` is not an integer for most values of `alive(3)`; the clause is applied
as written, so from 6 alive at one third, 5 at the end passes (5 ≥ 5.1 is false — **5 fails**) and
from 5 alive at one third, 4 passes (4 ≥ 4.25 is false — **4 fails**). Stated here before the numbers
exist: at six seeds the survival clause condemns on the loss of **one** world after the one-third
mark. That is a strict screen, and it is strict in the direction the screen is allowed to be.

## Predeclared verdicts

Stated so they can fail. The reasoning is given so that a failure is informative rather than a coin
landing the other way.

| cell | prediction | why |
|---|---|---|
| **C2** — brake 1.5, predation | **COLLAPSING** | C1 is this cell at brake 1.0 and it collapses. The brake acts on fertility only, and at 12,000 ticks brake 1.5 carries a **larger** mean population than brake 1.0 (299.4 vs 262.4, `p6-starvation-is-a-dial`) because more of its worlds are still growing. A stronger brake that permits a bigger standing crop has not removed the overshoot; the plan's own condition 1 says it may only postpone it. |
| **C3** — brake 1.4 / 1.5 / 1.6, herbivore | **COLLAPSING at all three** | Same mechanism, and predation is the one thing that removes consumers before they eat the food out, so the herbivore cell should if anything be worse than C2. The 12,000-tick reading for this family is 29–30 of 30 surviving, which is exactly the reading C1 and C7 both had at 12,000. |
| **C4** — brake 3.0 / 5.0, proximity pairing | **INDETERMINATE — not obviously dying** | This is a different regime, not a stronger version of the same one: at brake 3.0 the population self-limits at **100 under a cap of 500** with **0.0% starvation** and 98.9% age deaths. It never reaches its food supply, so there is no overshoot to unwind. The failure mode to watch for is not a crash but a **slow bleed** — a fertility brake strong enough to suppress recruitment below the age-mortality rate would show as a level, gently falling all-world mean rather than a step. If C4 collapses, that is what it will look like, and the trend clause rather than the survival clause will catch it. |
| **C5** — cap 250, brake 1.0, plants | **INDETERMINATE — not obviously dying** | Cap 250 takes starvation to 0.0% (`p6-starvation-is-a-dial`) and the population settles at 63–67, a quarter of its cap, with 10–15% of worlds extinct by 12,000. Nothing is food-limited, so the C1 mechanism is absent. Note before the fact: **seeds 42 and 43 are already extinct at 12,000 in the recorded corpus**, reproduced exactly by this build, so this screen starts with two dead worlds and the survival clause is judged from `alive(3)`, not from 6. |

**Two of four predicted to collapse and two to survive the screen.** If all four collapse the screen
has learned less than it looks like it has — that is the outcome a screen this strict produces by
being strict — and the write-up must say so rather than read four condemnations as four findings.

## The mechanism claim being checked at the same time

`docs/p4a-acceptance-window-2026-09-03.md` rules that resource recovery cannot be verified in any
measured configuration, on this claim:

> hunger in this configuration arrives **with** the collapse, not before it … the brake produced
> hunger by letting the population outgrow its food, which is also how it produced the collapse.

The triage samples the death mix at all nine trajectory points in every cell, so the brake axis
0.75 / 1.0 / 1.4 / 1.5 / 1.6 / 3.0 / 5.0 comes with a starvation share per interval at no extra run.

**Predeclared: they move together.** The brake acts only on fertility, and the only channel this model
has for producing chronic hunger is a population above its food supply — a state with no negative
feedback except death. So the prediction is that every cell showing a sustained non-zero starvation
share also fails the criterion, and every cell that survives the screen shows a starvation share at or
near zero throughout.

**The falsifier, named in advance:** a cell whose starvation share is materially above zero in *every*
interval including the last third, while `alive(9) ≥ 0.85 × alive(3)`. **Brake 1.5 is the candidate**
— it is recorded at 16.2% starvation with 30 of 30 surviving at 12,000, which is the "starvation and
survival coexist" claim, and it is a 12,000-tick reading. C3 is therefore the decisive cell for this
question as well as for its own row.

If they move together, the deferred brake experiment is not a matter of picking a value and the p4a
note should say so. If they separate at some strength, the p4a claim is wrong and this document says
that plainly.

# The A2 arm: graded plant seeding, against the all-or-nothing threshold

**Date:** 2026-09-06. **Status:** PREDECLARATION — written and committed **before the arm was built or
run**. Results are appended in a later commit; nothing in this section is edited afterwards.

**Plan:** `docs/superpowers/plans/2026-09-05-regulation-first-arms.md`, Task 4.
**Design:** `docs/superpowers/specs/2026-09-05-population-regulation-design.md`, candidate A2.
**Preceded by:** `p6-what-limits-the-peak-2026-09-06.md`, which narrows this arm's rationale before it
runs — see "What Task 3 changed about why this is worth running".

---

## What A2 changes, exactly

`PlantReproductionSystem.Step` today:

```csharp
if (parent.Biomass < parent.Capacity * MaturityFraction) continue;   // MaturityFraction = .75f
float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction;
```

A patch below three quarters of capacity produces **no seed at all** — not less seed, none. With it:

```csharp
float maturity = Math.Min(1f, (parent.Biomass / parent.Capacity) / MaturityFraction);
float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction * maturity;
```

**This arm introduces no new parameter, and that is deliberate.** The plan forbids proposing values;
the ramp is built from `MaturityFraction` itself, the constant that already defines the step, and runs
from zero biomass to that fraction. Choosing any other floor would be choosing a number, so the floor
is zero.

**At and above `MaturityFraction` the behaviour is identical**, so the arm changes exactly one thing:
patches between empty and three quarters full can now seed, in proportion to how full they are, on the
biomass they actually have. A patch at half the threshold makes a quarter of the seed — half the rate
on half the stock — and pays for it out of its own biomass through the same `ConsumeAt` call, so
seeding while depleted is not free.

**Flag-gated, off by default, byte-identical when off.** Following
`plantFertilityAdaptationEnabled` and `establishmentContestEnabled`. **No recorded result is
invalidated**, because every recorded run has the flag off; what the flag changes is the arm, and the
arm is the comparison.

---

## What Task 3 changed about why this is worth running

The design document justified A2 as removing the thing that prevents the plant community recovering.
Task 3 measured the ordering and **the ratchet runs after the population peak, not before it**: the
seed-eligible count at the peak is still 6.3 to 16.8 patches of about 21, and reaches zero in one cell
only, three samples late. **So A2 cannot be expected to prevent the crash**, and this document does not
predict that it will. Its predeclared "weakened, not falsified" branch already said what it is worth
instead: the recovery phase.

Task 3 also produced a number that bears directly on this arm and was anticipated by nobody: **age
mortality is 78% of gross plant growth in the ungrazed phase.** Patches die on a 34-135 second clock
whether or not anything eats them, and recruitment is the only thing that replaces them. That makes
the seeding gate more load-bearing than the design document argued, not less — it is the one valve on
the only inflow that opposes a large constant outflow — while also capping how much A2 can achieve,
because A2 opens the valve without touching the outflow.

---

## Predeclared signature

Reference cell: **C3 at brake 1.5, herbivore, 24 seeds, 24,000 ticks** — currently **0 of 24 alive**,
the only genuine zero in the triage, and the one clean single-variable axis in the corpus. One variable
moves: the flag. Same seeds, same length, same everything else.

Verdict by the unchanged persistence criterion: COLLAPSING if `mean(7) > mean(8) > mean(9)` or
`alive(9) < 0.85 * alive(3)`; PERSISTENT needs neither and at least 20 seeds.

**Predicted verdict: COLLAPSING, but materially less so.** Specifically:

1. **`alive(9)` rises from 0 of 24 to a non-zero count**, and stays well below the 20.4 the criterion
   needs.
2. **Plant patch count and seed-eligible count stop falling monotonically.** In the control they run
   22.2 -> 21.0 -> 12.0 -> 5.4 -> 3.1 -> 1.8 -> 1.0 across the last six samples. Under A2 they should
   turn upward at some sample after the crash rather than continuing down.
3. **Standing biomass at the final sample is materially above the control's 70.2.**
4. **The population peak is unchanged or slightly higher**, because A2 only adds seeding below three
   quarters of capacity and only a few patches are below it before the peak.

**Why COLLAPSING rather than PERSISTENT.** A2 opens recruitment; it changes nothing about the consumer
side, nothing about the ~1%/s age-mortality outflow, and nothing about the fact that at the peak total
drain exceeds gross production in every measured cell. A recovering plant community that is still
grazed by a population with no negative feedback except death should be expected to be grazed down
again. Predicting a rescue here would be predicting that a producer-side change fixes a consumer-side
overshoot, which is exactly what the design document argues it cannot do.

---

## The falsifiers, named in advance

- **PERSISTENT.** `alive(9) >= 20` with no monotone decline over the last third. That falsifies the
  prediction above and is the headline if it happens: it would mean the ratchet was load-bearing for
  survival after all, and it would promote producer-side work ahead of contest allocation.
- **Nothing moves.** `alive(9)` still 0 of 24 and the plant trajectory within noise of the control.
  That falsifies A2 as having any effect at all in this cell and says the sub-threshold patches were
  never numerous enough to matter — in which case the seeding gate is a real defect that is not worth
  fixing for its own sake, and E becomes the next arm without a post-A2 rebaseline.
- **The peak moves materially DOWN**, or `alive(9)` falls below the control. A2 would then be actively
  harmful, most plausibly because seeding while depleted drains parents through `ConsumeAt` faster than
  the offspring repay — a real possibility given the ramp scales output but not the cost's dependence
  on it. Recorded here so it cannot be presented afterwards as expected.
- **Population plateaus flat with starvation under 5% throughout.** That is candidate B's signature,
  not A2's. It would mean A2 is regulating rather than de-ratcheting, and it must not be believed
  without the food-supply control below.

## The control that runs if the arm survives

From the design document §3.3, and it is not optional. **Any arm that comes back PERSISTENT is re-run
with the plant capacity budget or the active site count raised.** A real ecological regulator moves its
plateau with the food supply; an imposed one does not. Without it, "the population is stable" is not
evidence that the ecology is regulating it — the error `p6-the-cap-is-the-stabiliser-2026-08-24`
recorded once already.

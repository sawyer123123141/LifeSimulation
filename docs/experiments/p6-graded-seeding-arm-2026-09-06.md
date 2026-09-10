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

---

# Results

**Run 2026-09-09**, after the predeclaration above, which is unedited. Console artefacts:
`p6-graded-seeding-control-24seeds-24000-2026-09-09.txt` and
`p6-graded-seeding-arm-24seeds-24000-2026-09-09.txt`, beside this file. Full suite before the run:
**752 passed, 0 failed.**

Both arms are the same build, the same 24 seeds and the same 24,000 ticks. One variable moves:
`--graded-seeding`.

## The control reproduces the recorded cell exactly

Flag off, the plant trajectory matches `p6-what-limits-the-peak-2026-09-06.md`'s C3 brake-1.5 row to
every printed digit — 20.2 / 17.9 / 1308.6 at the first sample, 1.0 / 1.0 / 70.2 at the last — and
`alive(9)` is **0 of 24**. So the flag-off path is unchanged by the whole diff, and the arm below is a
comparison rather than a different measurement.

## The headline: the population survives, and the verdict lands 0.4 of a world from the threshold

| | control | arm |
|---|---:|---:|
| `alive(3)` | 24 / 24 | 24 / 24 |
| `alive(9)` | **0 / 24** | **20 / 24** |
| threshold `0.85 x alive(3)` | 20.4 | 20.4 |
| last third, all-world mean | 49.3 -> 29.6 -> 0.0 | 127.5 -> 198.3 -> 213.0 |
| trend clause | **YES** (monotone decline) | no |
| population peak | 261.6 at 10,666 | 277.4 at 13,333 |
| final population, surviving worlds | — | mean 255.7, min 32, median 236, max 500 |
| whole-run starvation share | 49.8% | 36.4% |
| births per run | 696.2 | 1029.4 |

**The verdict depends on a contradiction inside the criterion's own predeclaration, and this run lands
exactly in it.** `p6-regime-b-triage-2026-09-05.md` states the rule twice formally — COLLAPSING if
`alive(9) < 0.85 x alive(3)`, PERSISTENT needs `alive(9) >= 0.85 x alive(3)` — which makes 20 < 20.4
**COLLAPSING**. Its six-seed worked example applies exactly that reading ("5 >= 5.1 is false — **5
fails**"). But its 24-seed gloss says "20 of 24 passes and 20 is the first passing value", which makes
this **PERSISTENT**. The falsifier written into *this* document repeated the gloss as `alive(9) >= 20`,
and that is met.

**Read on the formal rule, which is the one applied to all eleven triage cells: COLLAPSING, by 0.4 of
a world.** No threshold is changed here to resolve it, and none should be. **The honest reading is that
a 24-seed run cannot separate these two verdicts**, and the resolution is more seeds, not a different
number.

Two things are unambiguous whichever way that falls. **The trend clause does not fire**, and not for
the survivorship reason the clause was written against: the all-world mean rises 67% across the last
third while 20 to 23 worlds stay alive, so the sample is not shrinking. And **`alive(9)` moved from 0
to 20**, which no configuration in this project has done before.

## Against the four predeclared specifics: three held, one failed

1. **"`alive(9)` rises off zero and stays *well below* 20.4" — FAILED.** It rose to 20. The direction
   was right and the magnitude was wrong by the entire margin that matters. This is the clause the
   prediction should be scored on, and it is the one it got wrong.
2. **"Patch and seed-eligible counts stop falling monotonically" — HELD, emphatically.** Control patch
   count runs 22.2 -> 21.0 -> 12.0 -> 5.4 -> 3.1 -> 1.8 -> 1.0. Arm runs 22.7 -> 21.8 -> 22.8 -> 22.9
   -> 23.2 -> 22.8 -> **23.0**: essentially flat. Seed-eligible dips to 7.2 at the peak and recovers to
   19.1, ending at 13.3 against the control's 1.0.
3. **"Final biomass materially above the control's 70.2" — HELD.** 985.4, a factor of 14.
4. **"Peak unchanged or slightly higher" — HELD.** 277.4 against 261.6, one sample later.

## Against the four falsifiers

- **PERSISTENT** — met on the predeclaration's own `>= 20` wording, not met on the formal `>= 20.4`.
  See above. Unresolved by this run.
- **Nothing moves** — decisively not met.
- **Actively harmful** — not met. Nothing moved down.
- **B's flat-plateau signature, starvation under 5% throughout** — not met, and the reason matters
  (below).

## The signature does not match A2's predicted shape. It matches a different candidate's.

The design document's §3.2 row for A2 predicted starvation that "spikes at the peak, falls back
towards zero, and **recurs**". Measured, the arm's interval starvation share over its last five
samples is **33.4%, 49.3%, 40.3%, 33.9%, 49.4%** — it never falls back. That is **chronic non-zero
starvation in every interval including the last third, while worlds survive**, which is the row the
design document assigns to **D2 (territoriality) and E (contest allocation)**, and it is verbatim the
falsifier the Regime B triage predeclared for the hunger-equals-collapse claim and that **no cell had
ever met**. This arm meets it.

Beside it: mean energy among the living over those same samples is **0.448, 0.721, 0.792, 0.715,
0.622** — recovering and high — while a third to a half of deaths are starvation. A well-fed standing
population that continuously loses its marginal individuals to starvation is what density-dependent
mortality looks like when it arises from the ecology rather than being imposed.

**That reading is not established here and this document does not claim it.** No per-individual data
was collected in this run, the alternative — that the worlds are oscillating and the samples catch
different phases — is not excluded, and one world is pinned at the population cap of 500 (median 236),
so the cap binds again in at least part of the arm. Saying which of those it is needs the
reproductive-fitness instrumentation, not another sweep.

## What this does and does not establish

**Established:**

- The flag-off path is bit-unchanged; the control reproduces the recorded cell to the digit.
- Graded seeding takes C3 at brake 1.5 from **0 of 24 alive to 20 of 24** at 24,000 ticks.
- The plant community stops collapsing: patch count flat at ~23, final biomass 14x the control.
- The result sits **0.4 of a world** from the persistence threshold and the predeclaration contains
  two contradictory statements of that threshold, so the verdict is not resolved by this run.

**Not established, and worth saying:**

- **Whether the population persists past 24,000 ticks.** This project has now been surprised twice by
  exactly this question. A cell that survives to the end of the run is not a cell at equilibrium, and
  the arm's last third is *rising*, which is a boom, not a plateau.
- **Whether the regulation is ecological or the cap.** One world of 20 sits at 500.
- **Why the starvation signature is D2/E's rather than A2's.** It is recorded as an anomaly, not
  explained.

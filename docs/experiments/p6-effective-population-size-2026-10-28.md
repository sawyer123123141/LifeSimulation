# Effective population size: drift is weak at the peak and takes over in the crash

**Date:** 2026-10-28
**Status:** the Ne diagnostic the frozen spec marks MISSING, now built and run. **No production code
changed.** New tool: `tools/CreatureSweep --effective`.
**Spec:** `2026-08-30-what-finished-means-design.md` section 8 lists effective population size as a
**current diagnostic**, not a future concern, and is explicit that the existing observations
*motivate* measuring it and **do not establish that Ne is small**. This answers it.

---

## What was asked

Section 8's reasoning: a neutral gene has been measured drifting to fixation independently per world,
which is what makes Ne worth measuring — while absent clustering follows from panmixia and a weak
trait-to-fitness signal can come from environmental variance, so neither is evidence of small Ne by
itself. If Ne were far below census size, drift would be the hidden reason selection looks weak, and
the digestion result would be a statement about population size rather than about digestion.

Two routes, both specified there, both implemented here:

- **Route A, demographic** — lifetime reproductive success and its variance over the complete-life
  cohort, with generation length measured rather than assumed.
- **Route B, realised drift** — the spread of `NeutralMarker` **among replicate worlds**. The marker
  is read by no behaviour system and is pinned inert by `LivenessTests`, so divergence across
  identically-configured worlds is drift and mutation alone.

**No definitive Ne is produced.** Effective size has several non-coinciding definitions under
overlapping generations; the spec asks for a diagnostic of drift strength, not false precision.

---

## Two cells, 24 seeds each

**Cell A — the cell the digestion work was done in.** Cap 500, predation, gate 0.45, brake 1.0,
regen 2.0, proximity pairing.
**Cell B — the plant-corpus default.** Cap 48, no predation, regen 2.0.

### Route A, demographic

| cell | ticks | mean k | var k | **var/mean** | share breeding | generation (ticks) | **Ne/N proxy** | end state |
|---|---|---|---|---|---|---|---|---|
| A | 12,000 | 3.690 | 3.305 | **0.90** | 0.924 | 1,507 | **1.015** | 24/24 alive |
| A | 24,000 | 1.983 | 3.699 | **1.87** | 0.718 | 1,519 | **0.698** | **19/24 extinct** |
| B | 12,000 | 2.454 | 4.456 | **1.82** | 0.799 | 2,345 | **0.751** | 23/24 alive |
| B | 24,000 | 2.206 | 3.159 | **1.43** | 0.833 | 2,919 | **0.836** | 23/24 alive, census pinned 47.9 |

Both parents are credited for each birth, so mean k near 2 is replacement.

The proxy is `k / (k - 1 + var k / k)` — the variance-effective ratio in its general form. It assumes
constant size and discrete generations and **this population is neither**, so it is an index of
reproductive evenness rather than Ne/N, and an **upper bound**: age structure reduces variance-Ne
further.

**Reproductive skew is not dramatic anywhere.** The proxy sits between **0.70 and 1.02**. It is not
0.1. Whatever is limiting selection in these cells, it is not a population where a handful of
individuals produce everything — between 72% and 92% of each complete-life cohort leaves offspring.

### Route B, realised drift — cell A through its collapse

| tick | worlds | mean census | marker among | marker within | **marker STRUCTURE** | defense STRUCTURE |
|---|---|---|---|---|---|---|
| 3,000 | 24 | 32.7 | 0.000085 | 0.000872 | **0.088** | 0.182 |
| 6,000 | 24 | 99.3 | 0.000259 | 0.002667 | **0.089** | 0.361 |
| 9,000 | 24 | 306.9 | 0.000371 | 0.004447 | **0.077** | 0.422 |
| 12,000 | 22 | 216.4 | 0.000630 | 0.006099 | **0.094** | 0.476 |
| 15,000 | 16 | 123.6 | 0.001593 | 0.004479 | **0.262** | 0.538 |
| 18,000 | 11 | 55.5 | 0.004521 | 0.004195 | **0.519** | 0.800 |
| 21,000 | 6 | 29.7 | 0.008134 | 0.003889 | **0.677** | 0.862 |
| 24,000 | 5 | 68.8 | 0.007353 | 0.003638 | **0.669** | 0.875 |

STRUCTURE is `among / (among + within)` — the share of a gene's total variation sitting *between*
worlds rather than inside them.

---

## What this establishes

**1. At boom census, drift is weak — and census size is not what sets it.**

The neutral marker's structure share sits at **0.077–0.094** throughout cell A's growth from 33 to
307 animals. It does not rise as the population grows, and it does not fall either. Matched for
generations, **cell B shows the same thing at a sixth of the census**: 0.096 at 24,000 ticks
(≈8 generations at 2,919 ticks each) against cell A's ~0.08 at 12,000 (≈8 generations at 1,507).

**Six times the census, the same between-world divergence.** That is not what census-dependent drift
produces, and the reason is caution 2 in the spec: **mutation dominates both components.** The
inheritance model adds Gaussian noise at every birth, births scale with population, so the among and
within parts grow together and the ratio is insensitive to N. The caution was not cosmetic — at boom
census it is fatal to Route B.

**2. Selection is clearly visible above that floor.** In cell A, where predation is on and `defense`
reaches t +12.7, defense's structure share climbs to **0.476** by tick 12,000 while its *within*-world
variance falls from 0.0796 to 0.0222. Selection purges variation inside each world while the worlds
settle at different values. In cell B, where predation is off, defense behaves like a neutral gene —
structure **0.070**, below the marker's own 0.096. **The instrument reads selection, and in the cell
where selection is strong it is five times the neutral floor.**

**3. In the crash, drift takes over completely.** As cell A falls from 307 animals to 30, the marker's
structure share goes **0.077 → 0.669** and within-world variance is purged (0.0061 → 0.0036). Among-
world variance rises **thirteen-fold**. Route B is uninformative during the boom and strongly
informative during the bottleneck, exactly as theory says: mutation dominates at large N, drift
dominates at small N.

**4. The same cell gives two different Route A answers depending on run length, and the 12,000-tick
one is the boom generation.** Cell A reads var/mean **0.90** and 92% breeding at 12,000 ticks, and
var/mean **1.87** with 72% breeding at 24,000. The complete-life cohort admits only individuals whose
whole life fits inside the run, so **at 12,000 ticks that cohort is by construction the animals who
lived and died during the boom.** This is the project's own recorded error — a reading taken at the
peak of a boom — reappearing in a new instrument, and it applies to **every 12,000-tick fitness
number this project holds**, including the ones that motivated this measurement.

---

## The answer to section 8's question

**Ne is not dramatically below census size, and drift is not the hidden reason selection looks weak
at the population sizes the recorded experiments ran at.** Reproductive evenness is 0.70–1.02 by the
demographic route, and the neutral between-world floor is ~8–9% while a selected gene reaches 48% in
the same runs.

So the digestion finding stands as a statement about digestion. **The tension flagged when the spec
was frozen — that a small Ne might make the P3 gate unreachable at ~150 animals — is not supported.**
Low Ne is not the constraint at the peak.

**But the crash changes the verdict for anything measured across a full run.** Once a cell collapses,
drift dominates (structure 0.67) and effective size falls (proxy 0.698). A world that does not persist
cannot support a selection claim measured over its whole life, and **every Regime B cell is recorded
as collapsing**.

---

## What this does NOT establish

- **Not a value for Ne.** The proxy is an index of reproductive evenness under assumptions this
  population violates, and an upper bound.
- **Not that drift is irrelevant.** It is weak relative to mutation at boom census and dominant in
  the bottleneck. Which regime a result sits in depends on when it was measured.
- **Not separable drift and mutation.** No configuration knob disables mutation, so Route B's boom-
  phase numbers are drift **plus** mutation and bound drift from above. A mutation-rate knob would
  settle it and would be a production change; none was made.
- **Nothing about cell B's persistence being healthy.** Cell B holds 23 of 24 worlds at 24,000 ticks
  with census pinned at 47.9 — it persists because **the cap holds it**, which is the mechanism the
  project has already recorded as the stabiliser rather than the ceiling.

## Two bugs in this instrument, found and fixed during the run

1. **The Ne/N proxy used the replacement-only form** `4 / (Vk/k + 2)`, which assumes mean k = 2. Mean
   k here is near 4, and it returned **1.400** — a ratio above one, which is not a possible value. A
   bug in the instrument, not a finding. Replaced with the general form.
2. **A stale caption** kept printing the old formula after the code was corrected. Fixed.

Both are recorded because this project's standing failure mode is a plausible number trusted before
the instrument was checked.

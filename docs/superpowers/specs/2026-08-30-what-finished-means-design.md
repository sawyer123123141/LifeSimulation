# What "finished" means, and which gates matter

**Date:** 2026-08-30
**Status:** DESIGN, revision 2, awaiting the user's review. Nothing implemented against it.
**Revision 2** rewrites sections 1, 3, 4, 5 and 6 of revision 1 after a critical external review.
Revision 1's two worst errors are corrected here and named as errors in section 11, because the
mistakes are instructive and this project keeps paying for the same class of them.

**Why this document exists:** the project has 624 commits in 18 days, eight roadmap phases, and **no
definition of done anywhere**. `docs/ROADMAP.md` ends at P7 plus an art milestone;
`2026-08-12-product-architecture.md` frames every phase as a *scientific question* rather than a
deliverable. "Are we on track" had no answer because there was no target.

---

## 1. What the project is for

> **A realistic simulation.** Something to watch, and something you could do research with — where
> being *able* to research with it is the proof that it is realistic, not a separate goal.

That is the user's framing and it stands. But "realistic" on its own is not a usable standard, and
revision 1 leant on it too heavily. A 2D, 50-unit, ~150-creature world is **necessarily an
abstraction**. It is not a small Earth and it never will be.

### Three different things that "realistic" gets used for

They are related and they are not the same, and conflating them is how a project ends up polishing
one while claiming another:

- **Visual plausibility** — it looks like animals in a landscape.
- **Mechanistic plausibility** — outcomes are produced by local causes that resemble real ones, not
  by rules that impose the outcome.
- **Scientific validity** — a question asked of the simulation gets an answer you could defend.

### The definition of done

> **LifeSimulation is finished when ecological and evolutionary outcomes emerge from plausible local
> mechanisms, and those outcomes are measurable, reproducible, and explainable from the mechanisms
> that produced them.**

Short form: **mechanistically plausible, observable emergence.**

Not "all eight phases complete". Phases are means.

### The test for any simplification

Abstraction is not a failure of realism; unexamined abstraction is. The test:

> **Does this simplification preserve the causal relationship relevant to the question being
> studied?**

Simplifying a mechanism *outside* the causal question is legitimate and should be done freely.
Simplifying one *inside* it invalidates the answer. A flat 50-unit arena is fine for asking whether a
digestion trade-off pays; it is not fine for asking how dispersal distance shapes gene flow. **State
the assumption where it matters.**

**This is a judgement standard, not a metric, and that is deliberate.** It is operationalised by the
gate table in section 4 and the causal-loop gate in section 5: each is a specific, checkable claim,
recorded once rather than re-argued every session.

---

## 2. The constitution: information provenance

The user's rule, and the one thing every other decision defers to:

> "Animals should adapt and learn by themselves and definitely not [be given] information for free."

Restated as a test applicable to any line of code:

> **A creature may be given a body, senses, instincts, and costs. It may never be given knowledge it
> did not perceive or learn.**

The line is not *where the designer intervenes* — world versus body versus decision weights. It runs
through **what the animal knows**. Guiding the world is legitimate; guiding the animal's beliefs is
not.

### Allowed, so this does not paralyse engineering

- **Internal physiological state** — hunger, thirst, fatigue, pain, temperature, injury. An animal
  knows how it feels.
- **Locally perceived cues** — sight, smell, hearing, touch, within a sensory range that is itself a
  trait.
- **Inherited instincts and priors** — innate attraction or aversion to a cue, reflexes, mating
  responses. Being born knowing something is not cheating; it is genetics.
- **Learned and remembered information** — whatever the animal gathered and retained.

### Forbidden

- Exact coordinates of unperceived resources.
- Another creature's raw genome.
- True pedigree identifiers used *as knowledge*.
- Perfect resource quality without sensing or sampling it.
- Arbitrary world state the animal has no channel to.

### The distinction that makes it workable

**The simulation's internals may know everything.** Determinism, hashing and the analysis layer all
require global state. The restriction is on **what reaches decision-making**. Preferred shape:

```
World  →  perception/sensing  →  Observation  →  decision + learning
```

Behaviour code should consume an `Observation`, not query ground truth.

### The modelling exception

A shortcut is acceptable when **both** hold:

1. it approximates information the animal could realistically obtain, **and**
2. the omitted mechanism is not part of the question being studied.

**The assumption must be written down where the shortcut lives.** An undocumented shortcut is
indistinguishable from a bug.

### Audit status

**One confirmed finding.** `DecisionSystem.IsKin` compares parent identifiers directly, so kin
recognition is perfect, error-free and impossible to deceive. Real kin recognition is cue-based —
scent, familiarity from co-rearing — which is why brood parasitism works.

**Revision 1 called this a flat constitutional violation. Under the modelling exception it is
narrower than that:** pedigree lookup is a defensible *approximation* of a cue an animal could
plausibly obtain, and it is only a violation if kin recognition is inside the question being studied
or if its perfection matters. What it is not, currently, is **written down as an assumption**. It is
also load-bearing — kin recognition off costs 29% of the population — so it cannot be casually
deleted. The correct next action is to document the assumption, and to replace it with a foolable cue
only if kin dynamics become part of a question.

**Not yet audited:** mate selection, multi-threat perception, learned resource quality, carcass
detection, and the perception system's own range rules.

---

## 3. Evolution and learning

Revision 1 said in-lifetime learning should be a "thin layer" because most animals do simple
associative learning. **That was too broad and is withdrawn.**

The better framing:

- **Evolution remains the primary source of inherited adaptation.**
- **In-lifetime learning matters whenever information gathered during life has predictive value.**
  Where resources persist, remembering them pays. Where they move constantly, it does not. That is an
  empirical question about the world, not a fixed weighting to assert in a spec.
- **Learning does not mean human-like reasoning or internal world models.** It means associative
  learning, learned resource value, place and route memory, predator-cue learning, avoidance after a
  bad experience.
- **Genes control the priors:** exploration tendency, learning rate, memory duration, innate cue
  attraction or aversion, sensory acuity.
- **Experience modifies behaviour inside those inherited constraints.**
- **Learning should carry limits or costs** where that is biologically sensible — memory is not free.

Target model:

```
genes       →  inherited body, senses, priors, learning capacity
experience  →  learned information
both        →  behaviour  →  fitness
```

**Observable individual learning is part of watchability.** A player should sometimes be able to
watch *one animal* get better at exploiting its environment within its own life — not only watch a
population shift across generations. That is a distinct and currently absent thing to see.

**Current state, honestly.** `learnedResourceQualityEnabled` is one narrow channel. Place memory is
built and **pinned inert by a standing decision** ("Place memory stays inert. Never wire
`MemorySystem.ObservePlace`"). Under this section that decision is **reopened, not silently
preserved** — it was taken under the old framing and it blocks a now-explicit requirement. Re-taking
it is the user's call. See section 10.

---

## 4. Gate triage

Judged against biology **at the timescale the simulation runs** — **29–37 generations** in a
60,000-tick run, measured 2026-08-30.

| phase | gate | verdict | reason |
|---|---|---|---|
| **P0** evolution proof | heritable traits create measurable selection | **MET** | `urgency_exponent` to \|t\| 19.4, `defense` to t +12.7 against a flat control |
| **P1** predator/prey | niches from biology, no hardcoded flags; population cycles | **KEEP, unmet** | Predation fires and selects `defense`, but is 1–8% of deaths and cycles were never shown |
| **P2** cognition | learning with a biological cost | **REFRAME, unmet** | Re-gate on section 3: learning must be earned, must pay where information is predictive, and must be visible in an individual |
| **P3** niche formation | two trait strategies persist by exploiting different conditions | **KEEP, unmet** | Ecotypes arise fast under disruptive selection — stickleback armour, finch beaks, peppered moth are tens-of-generations cases. Realistic and reachable. Measured not met on 2026-08-30 |
| **P4** plants | plant genetics, competition, coevolution | **MET** | Heritable genome, defence, dispersal, mortality, competition all real |
| **P4a** watchable | distinguish foraging, drinking, mating, fleeing, resting, **and resource recovery** | **UNVERIFIED** | Resource recovery was blocked because nothing was ever hungry; the food-limited `Y` removes that block, but nobody has watched and confirmed the rest |
| **P5** species | *(reframed — see below)* | **KEEP, reframed, unmet** | Revision 1 dropped this. That was wrong |
| **P5** history | ancestry and timelines reconcile with events | **KEEP, built** | Unaffected by the reframe |
| **P6** world scale | partitioning, LOD, far populations | **DEFER** | Presentation half shipped early. Simulation half is worth building for scale, biomes, or **spatial structure as a divergence mechanism** — not as a species generator on its own |
| **P7** planet | zoom from organism to planet | **PARTIAL** | View exists; simulation stays 2D by standing decision |
| **art** | custom body plans | **OPEN** | Twelve CC0 models are placeholders by design |

### P5, reframed

**Revision 1 argued that speciation needs thousands of generations, therefore one interbreeding
population is the biologically correct result, therefore drop the gate. That reasoning was too
categorical and it was doing the thing this document is supposed to prevent — turning an inability
into a principle.**

The corrected position:

- **Do not require completed speciation in a normal 29–37 generation run.** That part stands.
- **Do keep population divergence and incipient speciation as a capability**, because rapid
  divergence genuinely happens over short evolutionary timescales when there is strong divergent
  selection, habitat or host preference, assortative mating, reduced gene flow, or spatial structure.
- **Apple maggot flies** are the useful example: rapid ecological divergence and *partial*
  reproductive isolation. They are not proof that clean species should routinely appear in 30
  generations.
- **Cichlids** are extraordinarily fast and still operate over far longer than one run, with strong
  ecological and sexual structure doing the work.
- **So one interbreeding population may well be correct for the current world configuration** — the
  arena has no divergent selection, no habitat preference, no assortative mating and no reduced gene
  flow. **That must not become a rationalisation for a simulation that cannot diverge when conditions
  should favour it.** The measurements to date were taken in a world built to be uniform; they say
  nothing about a world built to be divergent.

**Reframed gate:**

> Under deliberately divergent ecological conditions, populations develop persistent ecotypes and
> genetic clustering with reduced gene flow, **without any hardcoded species labels**.

**Candidate measurements** — several, not one, because a single moving metric is not validation:
phenotype bimodality; genetic distance between users of different niches; habitat and resource
preference; within-group versus between-group mating; migration and gene flow; intermediate/hybrid
fitness.

Full reproductive isolation stays a long-duration or extreme-condition outcome, not a normal-run
requirement.

---

## 5. The immediate priority: one complete causal loop

**The next major milestone is not "make species".** It is proving that one full evolutionary loop
works, end to end, for at least one trait:

```
genetic variation
  → phenotype
  → environmental interaction
  → measurable difference in lifetime reproductive success
  → predictable allele-frequency change
  → visible population-level change
```

**And then the effect must change or reverse when the ecological conditions change.** A trait that
only ever goes one direction has not been shown to be responsive; it may just be drifting or be
under a constant artefact.

Digestion is the natural candidate, because most of the chain is already instrumented and the loop
demonstrably breaks at a known link (section 6). A version of the experiment that supplies genuinely
different resources or spatial niches, and shows different phenotypes winning under each, would close
it.

**Only after this loop works cleanly should further complexity be layered on.** Every mechanism in
section 9 is easier to justify, and easier to debug, once one loop is known to work.

---

## 6. How selection actually works here

**Revision 1 concluded that the death mix is the lever rather than the trade-off curves. That was
too simplistic and is the most important correction in this revision.**

Natural selection acts through **differential reproductive success**, not through death percentages.
That 92.7% of deaths are old age is evidence the world may be too forgiving — it is **not itself the
optimisation target**, and no desired death percentage should be specified.

An 11% energy advantage matters evolutionarily only if it reliably changes something like: surviving
offspring; reproductive success; age at first reproduction; reproduction frequency; mate acquisition;
survival to reproductive age; or offspring investment and survival.

**The chain to measure and reason about:**

```
trait / genotype
  → ecological performance
  → lifetime reproductive success
  → allele-frequency change
```

### Rejecting the false choice

It is not trade-offs *or* mortality. **All three are required:**

1. **Trade-offs** create alternative viable strategies.
2. **Ecology** makes those strategies perform differently under different conditions.
3. **Life history and reproduction** convert performance differences into fitness.

### Re-reading the two standing findings under this frame

- **Digestion.** The physiological trade-off exists and is fully realised — plant yield per feeding
  tick falls 0.687 → 0.483, meat intake rises 15.6-fold, and lifetime intake predicts offspring at
  r +0.88. What fails is link 3: the ecological advantage is **not producing sufficient differential
  reproductive success**, because offspring is flat across the gene (1.93–2.02) while intake rate
  spans 78.7–89.1. Ingredient 2 is also weak — one uniform arena gives the strategies nowhere to
  perform differently.
- **Temperature tolerance saturation.** Most likely means the current fitness landscape has **one
  broadly dominant solution**, because broad tolerance costs too little and the environment is
  insufficiently differentiated. That is a statement about the landscape, not about the trait.

### Consequence for a change already shipped

On 2026-08-30 `Y` was given a density brake at 0.75, taking starvation from 0.0% to 5.4% of deaths.
**That was justified on death-mix grounds, which this section demotes.** It is not thereby wrong —
a less forgiving world is a plausible precondition for ingredient 3 — but it is **no longer
self-justifying**. It needs re-testing against reproductive success, not against a starvation
percentage.

---

## 7. Visible adaptation

Requested directly: *"there should be eventual visual changes when adapting, like fur getting whiter
in cold."* First-class requirement, not decoration — and the bridge between watching and researching.

### Four distinct sources of visible phenotype

1. **Genetic evolution across generations** — the population's appearance shifts as alleles shift.
2. **Developmental plasticity** — conditions during growth produce a different adult.
3. **Seasonal or reversible plasticity** — a coat that changes and changes back.
4. **Learned behaviour** — an individual visibly doing something better than it used to.

All four are legitimate. They are different mechanisms and should not be implemented as one.

### Every visual adaptation needs a causal mechanism

**Do not equate cold with white fur.** That is environment-X-directly-sets-cosmetic-trait-Y, which is
exactly the hardcoding the constitution forbids, wearing a biological costume. The real chain is:

```
snow cover  →  camouflage advantage  →  survival / reproductive difference  →  coat colour evolution
```

Cold *itself* plausibly drives insulation, coat density, metabolism, body proportions — different
traits, different chain. **Pick the mechanism first, then the visible consequence follows from it.**

### Two constraints on record

1. **Gene vision stays a separate picture.** `CreatureAppearance` records that genome colour must not
   silently replace the action colours: *"two pictures of the same population answer two different
   questions."* A single biological channel in the normal view is narrower than a gene rainbow and
   can coexist — but the spec for it must say how.
2. **Temperature tolerance is a poor first channel.** It saturates: gene 0.75 covers the world and
   the endpoint is a property of the field, measured 0.767 / 0.763 / 0.783 across three resource
   levels. Under section 6 this is a landscape problem, not a trait problem — but until the landscape
   changes, this channel would show almost nothing.

---

## 8. Scientific infrastructure

"Research-capable" has to mean more than having graphs. **Much of this already exists and is good;
this section records what is built so it is not rebuilt, and what is missing so it is not assumed.**

| requirement | status |
|---|---|
| **Replication across many seeds** | **BUILT.** `CreatureSweep` runs 24–120 seeds; `SitePilot` and `HistoryProbe` run seed sets. Single-world conclusions are already treated as invalid |
| **Controls / null models** | **BUILT.** `neutral_marker` is a gene that responds to nothing and is reported beside every drift column; `SitePilot`'s control arm is layout-fingerprint-identical to the shipped scenario. Flag-on/flag-off pairing is standard |
| **Headless batch** | **BUILT.** All three tools run without Unity |
| **Determinism** | **BUILT.** Three hashes, pinned by tests |
| **Fitness measurement** | **PARTIAL, and this is the important gap.** Lifetime offspring was measured for the first time on 2026-08-30, inside one probe. It is not a first-class tracked quantity. Missing entirely: surviving descendants, reproductive timing, mating success, survival to reproductive age |
| **Trait-versus-fitness relationships** | **MISSING as a standing capability.** Done once, ad hoc |
| **Sensitivity analysis** | **PARTIAL and inconsistent.** Some cells were checked for being knife-edges; most conclusions rest on one parameter setting |
| **Explicit hypotheses** | **MISSING as a habit.** Features are proposed as "add memory" rather than as a falsifiable statement |
| **Pattern validation** | **MISSING as a habit.** Several conclusions rest on one metric moving |

**Explicit hypotheses** should look like:

> Memory should improve reproductive fitness when resource locations persist, and its advantage
> should shrink or reverse when locations change rapidly.

not:

> Add memory.

The first is testable and can fail. The second cannot.

**Pattern validation:** prefer several predicted patterns over one moving metric. The project has
already been burned by a single number — mean nearest-neighbour spacing rewarded arms for killing
creatures until a density-normalised index replaced it.

---

## 9. Mechanisms currently underrepresented

Acknowledged as important and **not** demanded immediately. Listed so that proposals can point at one
rather than inventing a rationale:

spatial population structure · dispersal · gene flow · habitat preference · local and assortative
mating · effective versus census population size · developmental plasticity · costs of generalism ·
frequency-dependent selection · sexual selection and mate choice · juvenile survival and age-specific
selection · competition for resources and space · environmental variability across space **and** time

Several of these are exactly the ingredients the reframed P5 gate needs, which is not a coincidence:
divergence requires structure, and this world currently has almost none.

---

## 10. What this document does not decide

- **The provenance audit.** One finding documented, five systems unaudited.
- **Whether to replace pedigree kin recognition with a foolable cue**, or simply to document the
  assumption. Load-bearing either way.
- **Whether to reopen place memory.** Section 3 requires learning that place memory is the mechanism
  for; a standing decision pins it inert. **These cannot both hold.** The decision was taken under
  the old framing and is now formally reopened, but re-taking it is the user's call.
- **Whether the shipped brake at 0.75 survives re-testing** against reproductive success rather than
  starvation share.
- **Whether P1's population cycles are worth pursuing**, or P6's simulation half is worth building.
- **Any implementation.** No plan follows until the user has reviewed this.

### A contradiction this revision creates

The reframed **P5** gate — persistent ecotypes, genetic clustering, reduced gene flow — now overlaps
heavily with the **P3** gate — two trait strategies persisting by exploiting different conditions.
They are close to the same claim at two resolutions. A reasonable split:

- **P3** — the strategies exist and persist. Phenotypic.
- **P5** — the strategies become genetically structured, with measurably reduced gene flow between
  them. Population-genetic.

That split is a proposal, not a decision, and the two gates should not be pursued as separate work
streams until it is settled.

---

## 11. Errors in revision 1, recorded

Kept because the class of mistake recurs in this project and naming it is cheaper than repeating it.

1. **"Speciation needs thousands of generations, therefore one population is correct, therefore drop
   the gate."** Categorical, and it converted a measured inability into a principle. Every
   measurement behind it was taken in a world with no divergent selection, no habitat preference and
   no reduced gene flow — a world built to be uniform cannot tell you whether the simulation can
   diverge.
2. **"The lever is the death mix, not the trade-off curves."** Wrong link in the chain, and the data
   to see it was already in hand: offspring counts had been measured that same day and were flat
   across the gene while intake was not. Selection acts through reproductive success; a death
   percentage is a symptom.

Both are the same underlying error: **a mechanism that explained the data was adopted before an arm
that could have contradicted it was run.** That sentence is already in the field notes from
2026-08-26, about a different session.

---

## 12. How to use this

When work is proposed, ask in order:

1. **Does it serve mechanistically plausible, observable emergence?**
2. **Does it respect the constitution?** Does it hand a creature information it did not earn — and if
   it takes the modelling exception, is the assumption written down?
3. **Which link in the section 6 chain does it strengthen?** Trade-off, ecology, or reproduction.
4. **Which gate does it serve, and is that gate kept?** If no gate covers it, say so — that is itself
   a finding.
5. **What would falsify it?** If nothing, it is not a hypothesis.

And when a gate is met, dropped or reframed, **write it in the table in section 4.** This document
exists because P3's checkpoint said *"P4 remains blocked until this evidence is recorded"* on
2026-08-13, the evidence was never recorded, and four phases shipped over the top of it. That was not
a decision anyone made. It was the absence of a place to write it down.

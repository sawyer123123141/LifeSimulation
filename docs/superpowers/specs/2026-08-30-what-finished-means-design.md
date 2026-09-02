# What "finished" means, and which gates matter

**Date:** 2026-08-30
**Status:** DESIGN, awaiting the user's review. Nothing implemented against it.
**Why it exists:** the project has 624 commits in 18 days, eight roadmap phases, and **no definition
of done anywhere**. `docs/ROADMAP.md` ends at P7 plus an art milestone;
`docs/superpowers/specs/2026-08-12-product-architecture.md` frames every phase as a *scientific
question* rather than a deliverable. "Are we on track" had no answer because there was no target.

This document supplies one, states the rule the project is built on, and triages the existing gates
against it.

---

## 1. What the project is for

Settled with the user on 2026-08-30, and it is more precise than the docs had it:

> **A realistic simulation.** Something to watch, and something you could do research with — where
> being *able* to research with it is the proof that it is realistic, not a separate goal.

The three sit in a chain, and the order matters:

**realism** → makes it **researchable** → which makes it **worth watching**

Realism is the standard. Research-capability is the *test* that realism was achieved. Watchability is
the payoff. This is why "make it look better" and "make it measure properly" are not competing
priorities here: they are the same priority seen from two ends.

### The definition of done

> **LifeSimulation is finished when an ecologist watching it would recognise the outcomes as
> plausible for the timescale it actually runs, and could use it to ask a real question and trust the
> answer.**

Not "all eight phases complete". Phases are means. A phase whose gate is biologically unrealistic is
not a thing to achieve — it is a thing to drop, and section 4 drops one.

**This is a judgement standard, not a metric, and that is deliberate.** "An ecologist would find it
plausible" cannot be computed. It is operationalised by the gate table in section 4: each gate is a
specific, checkable claim, and the table is where the judgement gets recorded once rather than
re-argued every session. When a gate's verdict is disputed, the argument is about that row, with
evidence — not about whether the project is "on track".

---

## 2. The constitution: information provenance

The user's own words, and the single rule everything else defers to:

> "Animals should adapt and learn by themselves and definitely not [be given] information for free."

Restated as a test that can be applied to any line of code:

> **A creature may be given a body, senses, instincts, and costs. It may never be given knowledge it
> did not perceive or learn.**

**What this permits.** Shaping terrain, food, water, climate. Shaping physiology — what a body can do,
what it costs, how a trade-off curves. Giving an animal an instinct it is born with. Making the world
harder or easier. All of this is physics and biology, and none of it is cheating.

**What this forbids.** Handing a creature ground truth. Knowing where food is without perceiving it.
Knowing another animal's genome. Knowing a predator is dangerous without ever having met one. Knowing
who its relatives are without a cue.

**This is not the line I assumed.** Through most of 2026-08-30 the working assumption was that the
line ran between *where* the designer intervenes — world versus body versus decision weights. It does
not. It runs through *what the animal knows*. That is a better rule because it is auditable, and
because it explains why "guided emergence" is coherent rather than a fudge: **guiding the world is
legitimate; guiding the animal's beliefs is not.**

### The audit this implies, and its first finding

Every system that tells a creature something must be asked: *did the creature perceive this, learn
it, or was it handed over?*

**First finding, already confirmed.** `DecisionSystem.IsKin` reads the **pedigree**: it compares
parent identifiers directly, so a creature knows its parents, offspring and siblings with perfect
accuracy, no cue, no error, and no possibility of being deceived. Real kin recognition is cue-based —
smell, and familiarity from having been raised together — which is exactly why brood parasitism
works. This is a constitutional violation and it is **load-bearing**: the record shows kin
recognition off costs 29% of the population, so it cannot simply be deleted.

**Not yet audited:** mate selection, multi-threat perception, learned resource quality, carcass
detection, and the perception system's own range rules. The audit is work, not a finding, and it is
listed in section 7.

**Note the audit is not a demand to fix everything.** A cue can be cheap: kin recognition could read a
heritable scent-like marker plus proximity-at-birth, and would then be foolable, which is more
realistic *and* more interesting. The point of the audit is to know where the project stands, not to
force a rewrite.

---

## 3. Both timescales are required

The user asked for adaptation **across generations** and learning **within a lifetime**, and was
explicit that their own judgement should be checked against how biology actually works. Checked:

- **Evolution across generations does the heavy lifting.** For small-to-medium animals this is where
  most adaptation lives. The project already does this well and it is measurable.
- **In-lifetime learning is real but thin.** Most animals at this scale do simple associative
  learning — where food was, what hurt — not model-building. Making learning the centre of the
  project would be *less* realistic, not more.

So: both, weighted. Evolution primary, learning a thin and honest layer. The current state matches
that shape only partly — `learnedResourceQualityEnabled` is one narrow channel and place memory is
built but deliberately pinned inert, so the learning layer is thinner than "thin" and is genuinely
incomplete.

---

## 4. Gate triage

Each roadmap gate judged against real biology **at the timescale the simulation runs**, which is
**29–37 generations** in a 60,000-tick run (measured 2026-08-30).

| phase | gate | verdict | reason |
|---|---|---|---|
| **P0** evolution proof | heritable traits create measurable selection | **MET** | `urgency_exponent` to \|t\| 19.4, `defense` to t +12.7 against a flat control |
| **P1** predator/prey | niches from biology, no hardcoded flags; population cycles | **KEEP, unmet** | Predation fires and selects `defense`, but is 1–8% of deaths and cycles were never shown. Predator-prey cycles are real biology; the gate is fair |
| **P2** cognition | learning with a biological cost | **REFRAME** | Partly built. Re-gate on the constitution: learning must be *earned* information, and `IsKin` currently is not |
| **P3** niche formation | two trait strategies persist by exploiting different conditions | **KEEP, unmet — the live gate** | Ecotypes and polymorphisms arise fast under disruptive selection, well inside tens of generations. Realistic and reachable. Measured not met on 2026-08-30 |
| **P4** plants | plant genetics, competition, coevolution | **MET** | Heritable genome, defence, dispersal, mortality, competition all real |
| **P4a** watchable | a player can distinguish foraging, drinking, mating, fleeing, resting, **and resource recovery** | **UNVERIFIED** | The resource-recovery clause was blocked because nothing was ever hungry, and the food-limited `Y` removes that block — but the seed-42 render still showed full patches, and **nobody has checked that a watcher can actually tell fleeing from resting from mating.** Claiming this met needs someone to watch and say so |
| **P5** species | clusters describe meaningful genetic separation | **DROP the speciation clause** | Even the fastest documented speciation runs to thousands of generations, and typical cases far more, against this sim's 29–37. **One interbreeding population is the biologically correct result**, and the panel reporting it is the simulation being right, not failing |
| **P5** history | ancestry and timelines reconcile with events | **KEEP** | Built, and unaffected by the clause above |
| **P6** world scale | partitioning, LOD, far populations | **DEFER** | The presentation half shipped early. The simulation half is only worth building for scale or biomes — **measured 2026-08-30 that it will not produce species**, which was its main claimed motivation |
| **P7** planet | zoom from organism to planet | **PARTIAL** | The view exists; the simulation stays 2D by a standing decision |
| **art** | custom body plans | **OPEN** | Untouched; twelve CC0 models are placeholders by design |

**The important line in that table is P5.** It is the first gate this project drops on the grounds
that achieving it would make the simulation *less* realistic. That is the triage working.

---

## 5. Visible adaptation

Requested by the user directly: *"there should be eventual visual changes when adapting, like fur
getting whiter in cold."*

This is the bridge between the two halves of section 1 — adaptation you can *watch* is also
adaptation a researcher would *check*. It is a first-class requirement, not decoration.

**Both timescales, again:**
- **Evolutionary:** a population's appearance shifts over generations as its genes shift.
- **Plastic:** an individual changes with its conditions, the way a seasonal coat does.

**Both must be legible in the normal view**, not only in gene vision.

**Two constraints this has to respect, both on record:**

1. **Gene vision stays a separate picture.** `CreatureAppearance` records the decision that genome
   colour must not silently replace the action colours, because *"two pictures of the same population
   answer two different questions."* This request is narrower than a gene rainbow — one biological
   channel, in the normal view — so the two can coexist, but the spec for it must say how.
2. **Temperature tolerance is the wrong first channel.** It **saturates**: gene 0.75 already covers
   the whole world, and the endpoint is a property of the field rather than the ecology — measured
   0.767 / 0.763 / 0.783 across three very different resource levels. A population adapting to cold
   converges to nearly the same value everywhere, so the canonical "whiter in the cold" example would
   show almost no variation. **The trait exists; the divergence does not.**

---

## 6. The shared prerequisite

Sections 4 and 5 have the same blocker, and this is the most useful thing in this document:

> **Nothing can be seen changing, and no two strategies can persist, unless traits actually diverge.**

Measured on 2026-08-30: `diet_specialization` is neutral in every configuration tested and drifts to
fixation independently per world; genetic distance plateaus at 29–37 generations; the digestion
trade-off is realised exactly as designed and still selects nothing, because an 11% intake difference
is swamped by lifespan variance in a world that is **92.7% age deaths**.

**So the P3 gate and the visible-adaptation feature are one problem, not two.** Both need selection
pressure that is strong enough and *differential* enough to pull a trait apart. The lever identified
by measurement is the **death mix** — what kills creatures — not the trade-off curves. The
food-limited `Y` shipped on 2026-08-30 is the first step on that lever.

---

## 7. What this document does not decide

- **The audit itself.** Section 2 names one confirmed violation and five unaudited systems. Running
  that audit is work.
- **How to fix kin recognition**, or whether to. A cue-based version is more realistic and foolable;
  it is also a behaviour change to a load-bearing flag.
- **What the death mix should be.** Section 6 identifies it as the lever; it does not pick a value.
- **Whether P1's population cycles are worth pursuing**, or P6's simulation half is worth building
  for scale alone.
- **A conflict this document creates, and does not resolve.** Section 3 requires in-lifetime learning
  to be real. Place memory is the mechanism for it and is **pinned inert by a standing decision**
  ("Place memory stays inert. Never wire `MemorySystem.ObservePlace`"). Those cannot both hold. The
  standing decision was made for good reasons on the old framing; under this one it needs re-taking,
  and that is the user's call, not a thing to quietly reverse.
- **Any implementation.** No plan follows from this document until the user has reviewed it.

---

## 8. How to use this

When a piece of work is proposed, ask three questions in order:

1. **Does it serve realism?** If not, it is decoration or scope creep.
2. **Does it respect the constitution?** Does it hand a creature information it did not earn?
3. **Which gate does it serve, and is that gate kept?** If the gate was dropped, the work is not
   needed. If no gate covers it, say so — that is itself a finding.

And when a gate is met or dropped, **write it in the table in section 4.** The reason this document
exists is that P3's checkpoint said *"P4 remains blocked until this evidence is recorded"* on
2026-08-13, the evidence was never recorded, and four phases of work shipped over the top of it. That
was not a decision anyone made. It was the absence of a place to write it down.

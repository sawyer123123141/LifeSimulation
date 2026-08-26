# The predator-prey scenario has never been tested — its founders are demographically non-viable

**2026-08-26.** `tools/CreatureSweep --deaths 30 <cap> --predation [...]`, 12,000 ticks, seven cells.
Console artefact: `p6-predation-founder-defect-2026-08-26.txt`.

`p4-inert-flags-readjudicated-2026-08-19.md` records the predator-prey attempt **extinct before 3,000
ticks with zero births**, and every verdict on `multiThreatPerceptionEnabled` and
`kinRecognitionEnabled` has been "measured on a corpse" ever since. This session cited that as a
counterexample to "enriching the world is the cheap step". **The counterexample stands and the reason
for it is not predation.**

## The result, and why the first run of it said nothing

`FounderProfile.PredationVariation` in the pressured cell — the substrate shown non-marginal in
`p6-the-pressured-cell-is-a-plateau-2026-08-26.md`, where the herbivore profile survives 30 of 30:

| cell | surviving | births / run | predation share of deaths |
|---|---:|---:|---:|
| predation, cap 100 default | **0 / 30** | **0.0** | 4.4% |
| predation, pressured, health ratchet | **0 / 30** | **0.0** | 3.9% |
| predation, pressured, health recovery | **0 / 30** | **0.0** | 4.4% |
| *herbivore, pressured (reference)* | *30 / 30* | *492.1* | *0.0%* |

**The first attempt at this table was blank.** `--deaths` conditioned every figure on surviving runs,
so a fully-extinct arm reported "0 deaths of 0 causes over 0 runs" — the instrument was silent about a
result it had completely measured. It now reports the death mix over **all** runs, extinct included,
plus births per run. **That change is the only reason anything below is knowable**, and it is committed
with this document.

## What the numbers say

- **Zero births in 90 runs**, across three different worlds, while the same worlds give the herbivore
  profile 492 births per run.
- **Predation is 4% of deaths and age is 82%.** The founders are not being eaten. They live out a full
  lifespan and die of old age without reproducing.
- **The three predation cells are numerically near-identical** — 360 deaths in each, the same 4%
  predation share, at cap 100 and at cap 500 with and without health recovery. **The outcome does not
  depend on the world at all**, which is itself the diagnosis: nothing about the environment is
  causing it.
- Relaxing the mate-seeking gate barely helps: **0.9 births per run at gate 0.45 and 1.8 at gate
  0.20**, against 492. The gate is not the blocker either.

## The cause, read from the source and confirmed by probe

`Genome`'s constructor takes twenty-four positional traits and defaults **`fertilityInvestment = 0f`
and `lifespanTendency = 0f`**. `Genome.Neutral` passes **six** of them.
`PredationFounderFactory.Create` starts from `Genome.Neutral` and sets the six combat traits, so it
never reaches the two reproductive ones.

Measured directly (throwaway probe, deleted):

| founder | fertility investment | lifespan tendency | maximum age |
|---|---:|---:|---:|
| predation, ordinals 0–2 | **0, 0, 0** | **0, 0, 0** | **90 s, 90 s, 90 s** |
| physiology, ordinals 0–2 | 0.732, 0.023, 0.454 | 0.139, 0.771, 0.601 | 115 s, 229 s, 198 s |

Maximum age is `90 + 180 * lifespanTendency`, so **every predator founder gets the floor**, and the
reproduction interval is `16 - 8 * fertilityInvestment`, so **every one gets the maximum**. Adulthood
is 20 s. `PhysiologyFounderFactory` sets both traits explicitly; the predation factory was written
without them.

**This is the positional-trait hazard the field notes already warn about**, in a constructor rather
than in `InheritTrait`.

## Consequences

- **"Predator-prey is unviable here" has never been measured.** What was measured is a founder cohort
  with the shortest possible life and the slowest possible reproduction, in three different worlds.
- **The 2026-08-19 verdicts on `multiThreatPerceptionEnabled` and `kinRecognitionEnabled` remain
  unadjudicated**, and now for a reason that is fixable rather than ecological.
- **My own use of it as a counterexample was right about the record and wrong about the mechanism.**
  Enriching the world may still be expensive; this particular failure is not evidence for that.

## The fix, proposed and deliberately not applied

Pass `fertilityInvestment` and `lifespanTendency` in `PredationFounderFactory` the way
`PhysiologyFounderFactory` does — two lines and two more trait indices.

**Not applied here, on purpose.** It changes what `FounderProfile.PredationVariation` means, and the
presenter uses that profile for play mode (`Prototype1Presenter.cs:632, :671`). No committed corpus
uses it — every recorded sweep is `PhysiologyVariation` — so the blast radius is small, but it is a
production behaviour change and the sequencing is the human's call, per the "before you fix a bug"
contract. **`Genome.Neutral` has the same hole** and should be looked at in the same change rather
than separately.

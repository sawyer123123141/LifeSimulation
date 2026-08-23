# Cap-pinning audit: which recorded conclusions were ceilings?

**Date:** 2026-08-22
**Trigger:** `p4a-rendezvous-headroom-2026-08-22.md` found that a reported survival null was a
population-cap ceiling with zero variance, not a measurement.
**Outcome:** the *pattern* is systemic; the *damage* is not. **One conclusion was a ceiling
artefact, and it is already corrected.** No other conclusion materially changes, so no new supersede
banners were added. What does change is the **scope** of the plant corpus, stated below.

## The scan

Every experiment CSV in `docs/experiments/`, checked for population, survival or extinction columns
with zero variance across all runs.

| file | column | constant | runs |
|---|---|---|---:|
| `p4-establishment-contest-cost0-2026-08-20` | population | 48 | 120 |
| `p4-establishment-contest-cost2-2026-08-20` | population | 48 | 120 |
| `p4-establishment-contest-cost6-and-control-2026-08-20` | population | 48 | 240 |
| `p4-invader-establishment-contest-2026-08-21` | final_population | 48 | 480 |
| `p4-low-occupancy-growth-trait-reaudit-2026-08-20` | final_population | 48 | 840 |
| `p4-low-occupancy-plant-route-audit-2026-08-20` | final_population | 48 | 720 |
| `p4-occupancy-calibration-final-2026-08-22` | extinct | 0 | 30 |
| `p4-postfix-revalidation-2026-08-22` | extinct | false | 720 |
| `p4-safety-gated-rendezvous-2026-08-21` | final_population | 48 | 240 |
| `p4-site-abundance-seed-production-rate-2026-08-20` | final_population | 48 | 480 |
| `patch-count` | finalPop | 48 | 90 |

Eleven files, **4,080 runs**, every creature population pinned at the cap.

## The distinction that decides the damage

A zero-variance outcome column invalidates a conclusion **only if that column was the outcome under
test.**

**Case 1 — the outcome was survival. One file.** `p4-safety-gated-rendezvous-2026-08-21` asked
whether the gate improves survival and answered from a column that could not move. That is the
ceiling artefact, corrected in `p4a-rendezvous-headroom-2026-08-22.md`, which re-measured at a cap
where extinction is partial and found the gate cuts predation deaths (t −4.64) without changing
survival or birth rate.

**Case 2 — survival was a control. Ten files.** Every other entry reports "0/120 extinct" as a
*control against differential survival*, alongside outcomes that are plant trait gradients:
`Dispersal` t +17.41, `SeedlingResilience` +0.02156 / t +4.16, and so on. **That control is valid
exactly as stated.** Zero extinction really does mean no arm lost creatures the other kept, so the
trait comparison is not confounded by differential survival. The cap does not weaken that claim — if
anything it guarantees it.

**No trait conclusion in the plant corpus is invalidated by cap pinning.** The scan looks alarming
and mostly is not.

## What must not be read from those files

"0/120 extinct" was in places used as evidence of *run quality* — that these are healthy,
sustainable worlds. **It is not.** These populations are cap-stabilised, and the cap is not a guard
rail; it is what prevents overshoot.

Verified on both harnesses, not assumed from one:

| configuration | cap 48 | cap 84 | cap 96 | cap 200 | cap 600–800 |
|---|---|---|---|---|---|
| predation (`PredationVariation`, rendezvous harness) | 0/8 extinct | 5/8 | 8/8 | 8/8 | 3/3 |
| herbivore (plant-experiment config, `ConsumerDefenseCalibrationModerate`) | 0/6, pop 48.0 | — | 0/6, pop **95.8** | 5/6 | 6/6 |

The herbivore configuration is still pinned at cap 96 (mean 95.8), so its free-growth ceiling is
higher than the predation harness's — but it collapses the same way once growth is allowed to run,
with runs booming and then starving. **The cliff exists in both; only its position differs.**

## Scope qualification for the plant corpus

Every plant trait result on record was measured under a creature population **pinned at 48**, i.e.
under approximately constant grazing pressure. That is a reasonable controlled variable and is
arguably cleaner than a free-running herbivore population for isolating plant selection.

But it is an **assumption, not a demonstrated generalisation**: whether `Dispersal` is still
selected upward under a freely fluctuating herbivore population is untested. And it **cannot be
tested by simply raising the cap**, because raising the cap does not produce a free-running
population — it produces a boom and a collapse. Testing it needs a habitat whose carrying capacity,
rather than a cap, limits the population. No such scenario exists yet.

This is a scope note, not a retraction. The results stand on the condition they were measured on.

## Method lesson

Before believing any result, check the **spread** of its outcome variable. A column with zero
variance answered a different question than the writeup claims, and "0/n extinct" reads as a strong
control right up until you notice n was never free to be anything else.

## What was and was not done

Done: full CSV scan; classification of all eleven files; verification that the collapse-without-cap
finding generalises across two different configurations rather than being predation-specific.

Not done: no supersede banners were added, because no conclusion materially changes — the standing
rule is that banners mark changed conclusions, not changed context. Not done: building a
carrying-capacity-limited habitat, which is the only way to lift the scope qualification above and
is a scenario design task rather than an audit task.

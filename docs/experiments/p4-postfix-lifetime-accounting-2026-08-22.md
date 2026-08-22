# Plant lifetime accounting after the takeover-age fix

**Date:** 2026-08-22
**Raw data:** `p4-postfix-lifetime-accounting-2026-08-22.csv` (one row per patch incarnation)
**Question:** do the four recorded plant-lifetime numbers survive `4cc9a47`?

The age fix changes when a taken-over patch dies, so these four figures could not be cleared by a
trait sweep and were measured directly:

- "site competition destroys **34%** of every patch ever born"
- "inside a median **two seconds**"
- "that binary is **51.9%** of the variance in per-patch lifetime offspring"
- "realised lifespan explains only **R² = 0.024** of offspring variance among survivors"

## Method, and one correction to the measurement itself

30 seeds (42-71), 12,000 ticks, the 24-site calibration layout with **varying** founder genomes,
`PlantSiteCompetitionEnabled` and `PlantMortalityEnabled` on, establishment contest off. Every patch
is tracked from birth to end, with its offspring attributed by lineage parent.

**A takeover does not remove a patch id.** `ReplaceAt` overwrites the occupant in place and leaves
`_ids[index]` alone; only `PlantMortalitySystem` removes an id. So one numeric id hosts a sequence
of distinct lives, and each is tracked as a separate incarnation.

**The first detector was wrong and its numbers are discarded.** It identified a takeover by the age
resetting to zero — which only happens *after* the fix, making it useless for comparing builds, and
which also undercounted takeovers post-fix (0.275 instead of 0.347). The detector used here is
version-independent: `ReplaceAt` always installs a new lineage whose `ParentId` is the invading
parent, on both sides of the fix, so a lineage-parent change at a persisting id is the signal.

## Result: the fix moves none of these figures

The same probe, same seeds, run on a pre-fix worktree at `15c7a5a` and on fixed `main`:

| figure | pre-fix | post-fix | recorded original |
|---|---|---|---|
| fraction of patches ending in takeover | 0.3409 | 0.3471 | **34%** ✓ |
| median takeover lifetime | 1.95 s | 1.95 s | **~2 s** ✓ |
| R²(takeover binary, offspring) | 0.5013 | 0.5164 | **51.9%** ✓ |
| R²(realised lifetime, offspring), pooled | 0.1546 | 0.1407 | 0.024 — see below |

Three of the four reproduce the recorded values closely, and **the fix shifts every one of them by
less than two percentage points**. Takeover remains the dominant single explainer of per-patch
lifetime offspring.

## The fourth figure: a censoring artefact in my probe, not a changed conclusion

The pooled R² of 0.14 does not match the recorded 0.024, and it does not match on *either* build —
so it cannot be an effect of the fix. It is a population-definition difference. Splitting the
post-fix patches by how they ended:

| population | R²(realised lifetime, offspring) | n |
|---|---|---|
| died of age (mortality) | **0.0039** | 3,590 |
| still alive at the end of the run | 0.3000 | 685 |
| pooled (what my probe first reported) | 0.1407 | 4,275 |

Patches still alive when the run stops have a lifetime **truncated by the run ending rather than by
biology**, and among them "lifetime" is largely a proxy for "born early", which correlates strongly
with offspring count. Pooling those right-censored patches with completed lives inflates the
statistic.

On the comparable population — patches that lived a full life and died of age — **R² = 0.0039**,
against the recorded 0.024. Both are "realised lifespan explains essentially nothing", which is the
claim that matters. The recorded conclusion **stands**.

## Verdict

All four lifetime-accounting figures are **confirmed on current code**. The takeover-age fix does
not disturb them: it changes when a *taken-over* patch dies, and by construction those patches are
the ones whose lives were already ending. The banners on
`p4-where-plant-fitness-is-decided-2026-08-20.md` and
`p4-seed-production-rate-is-not-the-constraint-2026-08-20.md` can be narrowed to their trait-
selection content; their lifetime decompositions are cleared.

Two lessons worth carrying:

- **A detector that depends on the fix under test cannot compare builds.** The age-reset detector
  would have reported a 7-point drop in takeover fraction that was entirely an artefact of
  detection.
- **Right-censored lifetimes must be excluded from any lifespan-versus-fitness regression.** Pooling
  them inflated R² by 36x here (0.0039 to 0.14).

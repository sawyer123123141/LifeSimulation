# P4 invader-side establishment contest — 2026-08-21

> **REQUIRES RE-MEASUREMENT — 2026-08-22.** This result was measured with
> `PlantSiteCompetitionEnabled` on, before the `PlantPatchStore.ReplaceAt` takeover-age defect was
> fixed in `4cc9a47`. Until that fix, a seedling installed by a takeover inherited the incumbent's
> accumulated age and was frequently aged out within a tick or two of establishing — on the exact
> mechanism this document draws conclusions about. A paired old-versus-fixed audit over 30 seeds
> found every state hash on the competition path changed, `SeedlingResilience` moving **downward**
> under the fix (t -1.99, 19 of 30 seeds down) and plant generations falling by one (t -2.63).
>
> Those figures are drift magnitudes measured on the uniform-founder calibration, so they do **not**
> overturn a selection result measured at t +4.03 under standing variance — but they push against it
> on the mechanism it concerns, so the conclusion is neither retracted nor confirmed. **Re-run this
> experiment's original design before relying on it.** Nothing here has been edited or recomputed;
> the numbers below are the original measurements.
> Audit: `evidence-impact-audit-2026-08-22.md`.


## Question

Does letting an invader's inherited `SeedlingResilience` offset the vulnerable incumbent's
resilience create a selectable plant route?

## Predictions made before the run

| Prediction | Held? |
| --- | --- |
| The invader term may weaken incumbent-side selection at 24 sites. | Held: t +4.16 to +1.95. |
| The 168-site condition will disagree with 24 sites. | Held: it became more negative, t −3.46. |

## Design

120 seeds per arm (42–161), 12,000 ticks, existing establishment contest enabled, varying
only `PlantInvaderEstablishmentContestEnabled`. When enabled, the incumbent block threshold is
`max(0, incumbent resilience − invader-parent resilience)`; false retains the prior roll exactly.
The 24-site and 168-site conditions retain the known count/geometry confound.

Raw data: `p4-invader-establishment-contest-2026-08-21.csv`.

## Results

| Sites / invader term | Mean occupancy | Resilience delta / t / up | Survival |
| --- | ---: | --- | --- |
| 24 / off | 0.900 | +0.02156 / +4.16 / 75/120 | 0 extinct, 0 frozen |
| 24 / on | 0.897 | +0.01133 / +1.95 / 74/120 | 0 extinct, 0 frozen |
| 168 / off | 0.275 | −0.00525 / −1.24 / 55/120 | 0 extinct, 0 frozen |
| 168 / on | 0.271 | −0.01770 / −3.46 / 41/120 | 0 extinct, 0 frozen |

The 24-site on arm is less directionally consistent than its off arm (74 versus 75 up), so it
is a null despite positive t. At low occupancy the invader term makes the existing negative
selection stronger. This route is closed: symmetrical contest mechanics remove the incumbent
advantage rather than producing a new resilient-invader advantage.

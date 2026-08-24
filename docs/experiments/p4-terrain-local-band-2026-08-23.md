# A local band on top of the planetary climate

**Date:** 2026-08-23
**Raw data:** `p4-terrain-local-band-2026-08-23.csv` (480 runs)
**Instrument:** `tools/PlantSweep`, same design as
`docs/experiments/p4-terrain-join-2026-08-23.md` — 120 seeds, 12,000 ticks, flat versus
terrain-driven, crossed with the establishment contest.

## What changed

`EnvironmentField.SampleTerrain` now treats the terrain climate as the **regional mean** and adds a
zero-centred local band to moisture and temperature, sampled at the same scale as the procedural
field's own noise. Which continent the arena sits on still decides whether it is wet or dry; the
band decides which end of the valley is wetter.

This is option 2 of the two the previous measurement left open. Option 1 — widening metres-per-unit
so the arena crosses real climate — is **not** taken: it changes what every recorded distance means.

**Elevation is untouched.** It still equals the generator's own sample to the last decimal, which is
what makes the ground drawn and the ground simulated the same ground.

**No new config flag.** The band lives inside the terrain path, which is already gated by
`terrainDrivenEnvironmentEnabled`. Flag-off is still byte-identical: 503 / 19 / 33 / 1 green,
including every pinned hash literal.

## The band restores the spread the join had removed

Sampled at 1,681 positions across the ±25 arena:

| | flat sd | terrain sd, before | terrain sd, with band |
|---|---:|---:|---:|
| moisture, seed 42 | 0.240 | 0.050 | **0.207** |
| moisture, seed 71 | 0.240 | 0.037 | **0.192** |
| moisture, seed 161 | 0.283 | 0.005 | **0.178** |
| temperature, seed 42 | 0.182 | 0.166 | 0.154 |
| temperature, seed 71 | 0.201 | 0.099 | **0.134** |
| temperature, seed 161 | 0.189 | 0.014 | **0.108** |

Moisture lands within about 20% of the procedural field's spread; temperature stays lower, because
the window sits at a low latitude where the regional value is already high (mean 0.66–0.78) and the
upward half of the band clips. The flat field clips at both ends too, so this is a difference of
degree.

Strengths are `LocalMoistureStrength = .40` and `LocalTemperatureStrength = .32`, chosen to match
the procedural spread rather than to maximise it. Higher values do not make the world more varied;
they make the regional signal irrelevant.

## The join still moves no plant conclusion — and now that means something

Paired terrain-minus-flat per seed, contest-on:

| trait | mean | t | up |
|---|---:|---:|---:|
| **WaterEfficiency** | −0.0221 | −2.33 | 51/120 |
| **NutrientUptake** | +0.0277 | +2.06 | 67/120 |
| MoistureTolerance | +0.0221 | +1.41 | 61/120 |
| Nutrition | −0.0109 | −1.25 | 56/120 |
| everything else | — | under \|0.9\| | — |

Contest-off is flatter still — nothing past |t| = 1.6. Two marginal cells out of twenty-two
comparisons is what twenty-two comparisons produce, and neither is claimed.

**Before the band, "no difference" and "no signal" were indistinguishable.** Now the terrain field
carries comparable structure to the one it replaces, and selection is still indifferent to which
field supplies it. That is a real null: the plant corpus is robust to where its environment comes
from.

`NutrientUptake` is the one worth watching if anything is followed up — its selection is weaker
under terrain (contest-on: flat −0.0536, t −5.39; terrain −0.0259, t −2.37), which is consistent
with fertility being the channel whose shape changed most. Not a claim on this evidence.

Survival is unchanged: extinct 8–10 of 120 per cell, occupancy 0.92–0.94, in both fields.

## The establishment contest replicates better under terrain

Paired on−off on `SeedlingResilience`:

| field | mean | t | up |
|---|---:|---:|---:|
| recorded (2026-08-22) | +0.0362 | +3.22 | 72/120 |
| flat, this harness | +0.0220 | +1.88 | 70/120 |
| terrain, before band | +0.0330 | +2.97 | 70/120 |
| **terrain, with band** | **+0.0340** | **+3.04** | 67/120 |

Both terrain arms sit closer to the recorded result than this harness's own flat arm. Stated as an
observation, not a conclusion — the flat arm is the control for terrain claims, not the reverse, and
this harness is not the recorded one (see the limitations in the previous document, which all still
apply, including its 8–14 extinctions per 120 against a recorded 0/120).

## Now enabled in the terrain playtest

`p6-terrain-playtest` in `Prototype1Presenter` sets `terrainDrivenEnvironmentEnabled: true`. It is
the one scenario where the ground being looked at and the ground being simulated ought to be the
same ground. **This was only safe once the band existed** — without it the temperature heatmap in
that scenario would have shown one flat colour.

No experiment configuration is touched. `CreateFullEcosystemDefaults` still has the flag off, so
every recorded baseline stands.

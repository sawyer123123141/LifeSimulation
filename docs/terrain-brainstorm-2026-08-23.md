# Terrain: why five rounds of changes produced no visible difference

**Date:** 2026-08-23
**Status:** diagnosis and plan. Nothing here is implemented.

Five rounds of changes — scale separation, Nyquist-aware octaves, plate tectonics, relief rework —
and the render "basically looks the same". That is not five failed fixes. It is one symptom: **the
terms I keep adjusting are not the terms that dominate the output.** This document does the
arithmetic instead of guessing again.

---

## 1. The arithmetic

### 1.1 fBm never spans 0..1, and I never corrected for it

`EnvironmentNoise` documents this in its own source:

> a raw 4-octave field spans roughly **.37…82** rather than 0..1

`EnvironmentNoise.Contrast(value, strength)` exists precisely to fix it, and `EnvironmentField` applies
it to moisture (2.4) and fertility (2.0). **`PlanetTerrain` applies it to nothing.** So every noise
band I wrote — coast perturbation, hills, ridges, detail, climate — delivers roughly **45% of the
range I assumed**, centred on 0.5.

This alone makes every amplitude I chose wrong by about half, in the same direction, everywhere.

### 1.2 Elevation is dominated by a per-plate constant

Current composition on land:

| term | typical contribution |
|---|---:|
| plate base elevation | **0.52 – 0.68** |
| coast perturbation `0.22 × (coastNoise − 0.5)` | −0.03 … +0.07 |
| hills `~0.136 × (hills − 0.42)` | −0.01 … +0.05 |
| ridged × boundary relief | 0 away from boundaries |
| detail `0.05 × (detail − 0.5)` | ±0.02 |

**The plate constant is 5–10× everything else combined.** Each continental plate is a flat plateau at
its own height, with a ripple on top. Changing hill amplitude or ridge weighting cannot alter that,
which is exactly what the last five rounds discovered empirically.

### 1.3 Only a third of the height range is ever used

Sea level is 0.38. Land sits at 0.52–0.73. Through the height mapping
`(elevation − 0.38) / 0.62 × scale`, typical land uses **0.23 → 0.56 of the scale**, and nothing ever
approaches 1.0. Peaks are structurally impossible.

Concretely, on the 400-unit wide patch at `PatchHeightScale ≈ 30`:

- typical land height ≈ **9.7 units**
- total hill variation ≈ **2.4 units across 400 units of world** — a 0.6% slope, invisible
- a creature is 1 unit

So the terrain is, correctly, being drawn as almost flat. The renderer is fine. The field is flat.

### 1.4 Coastlines are plate polygons

Land/sea is decided by `continent` crossing 0.38. The plate step from oceanic (0.12–0.26) to
continental (0.52–0.68) is about **0.4**, while the coast perturbation is at most **±0.07**. The
threshold therefore falls almost exactly on the Voronoi edge, and coastlines trace plate boundaries
as polygons. This is why boundaries are the most legible thing on the sphere: they are the coast.

---

## 2. What this says about method

The renders were the only instrument, and they cannot distinguish "the change had no effect" from
"the change had an effect that is 3% of the dominant term". Every round I inferred a cause from a
picture and adjusted a coefficient, and the numbers above say most of those coefficients could not
have mattered.

**The simulation half of this project does not work this way** — it has 499 tests and refuses to
accept a conclusion without a manipulation check. The terrain half has none, because it lives in
Presentation, and it has been proceeding on vibes. That asymmetry is the actual bug.

---

## 3. Plan: measure the generator before changing it again

### 3.1 Instrument first

`Assets/Editor/PrototypeBatchEntry.cs` already provides a headless `-executeMethod` entry point.
Add a terrain statistics dump that samples the generator over the sphere and reports:

- elevation histogram: min, max, and deciles
- **land fraction** (target ~30%, per the integration design)
- fraction of land within each 0.1 elevation band — reveals plateauing directly
- distribution of boundary distance, and elevation conditional on it — does a range actually rise?
- moisture and temperature histograms, and the **joint** distribution against elevation
- biome counts under the current classifier — "2 biomes" becomes a number, not an impression

**Acceptance for any future terrain change: state the predicted shift in these numbers first, then
measure.** Same discipline as the ecology work.

### 3.2 Fixes the arithmetic already justifies

Ranked by expected effect, all currently unimplemented:

1. **Apply `Contrast` to every noise band.** Restores the range the amplitudes were chosen for.
   Strength ~2.0–2.4, as `EnvironmentField` uses.
2. **Make the plate constant a weak bias, not the elevation.** Plate type should decide *land or sea*
   and a modest offset; **relief should come from the process terms.** Something like: continental
   plates start just above sea level, and hills and boundaries build upward from there — rather than
   starting at 0.6 and adding 0.05.
3. **Normalise so peaks reach 1.0.** If the tallest ground on the planet is 0.73, the top third of the
   height range is dead. Either scale the composed elevation to its observed range, or choose
   amplitudes that sum to it.
4. **Widen the coast transition** so it is comparable to the plate step — a warp applied to the
   *sampling position* rather than added to the value, which moves the boundary rather than nudging
   the threshold. This is what makes coastlines irregular rather than polygonal.

### 3.3 Research directions worth reading before implementing

- **Domain warping at continental scale** (Quílez). Warping the *position* rather than adding to the
  value is the standard way to make boundaries wander; the repo already has `WarpedFbm` but uses it
  at feature scale, not at continent scale.
- **Hydraulic erosion / flow accumulation.** This is T2 in the existing spec and it is the single
  biggest legibility win in most terrain generators — valleys carved by water read as *caused*.
  Rivers, lakes and coastlines all fall out of the same accumulation pass.
- **Whittaker biome diagram** — biome from the temperature/moisture *pair* rather than a cascade of
  thresholds. The current classifier is an ordered if-chain, which is why one variable dominates and
  biomes track elevation.
- **Distance-to-coast** as a first-class field. Cheap once land/sea exists, and it drives beaches,
  continental drying, and coastal ranges far better than the current continentality proxy.
- The existing **`docs/terrain-visual-direction-brief.md`** and
  **`docs/superpowers/specs/2026-08-14-world-generation-design.md`** (T1–T2) — both were written before
  this session and both are ahead of where the implementation is.

---

## 4. Standing decisions, unchanged

- **Do not grow the arena.** 1 unit = 1 metre is settled; at planet scale the world holds ~40,000
  creatures against a kernel that runs ~1,000. Regions and level of detail (T5) are the answer.
- **`PlanetTerrain` stays in Presentation** until it looks right. It moves no hash and needs no
  re-measure there; promotion is a deliberate step behind a flag.
- **Caves** need T3 (layered world model and position representation) — a kernel change to how
  position is represented, scheduled independently and explicitly not bundled with generation.

---

## 5. Honest status

The plate structure from T0 is, as far as the arithmetic goes, correct and worth keeping — boundary
classification, drift, falloff widths. It is being **swamped** by a per-plate elevation constant and
by noise bands running at half amplitude. The structure is there and invisible, which is consistent
with what the renders show: boundaries legible, everything else flat.

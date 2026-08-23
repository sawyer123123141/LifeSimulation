# Terrain, second brainstorm: the normalisation is the bug

**Date:** 2026-08-23
**Status:** proposal. Nothing here is implemented.
**Supersedes the approach in:** `terrain-brainstorm-2026-08-23.md` (its diagnosis was right, its plan was to keep tuning).

## 1. Why tuning kept failing

Roughly fifteen rounds of coefficient changes produced terrain that still shows terraces, combs
and hard cutoffs. Six diagnoses were wrong. That is not bad luck with parameters — it is a
structural problem, and comparing against a working implementation makes it obvious.

## 2. What a working implementation does differently

Sebastian Lague's `Procedural-Planets` (source read directly, not summarised):

```csharp
// ShapeGenerator.CalculatePointOnPlanet
float mask = layer.useFirstLayerAsMask ? firstLayerValue : 1;
elevation += noiseFilters[i].Evaluate(point) * mask;
return pointOnUnitSphere * settings.planetRadius * (1 + elevation);
```

```csharp
// RidgidNoiseFilter.Evaluate
float v = 1 - Mathf.Abs(noise.Evaluate(point * frequency + centre));
v *= v;
v *= weight;
weight = Mathf.Clamp01(v * settings.weightMultiplier);
noiseValue += v * amplitude;
...
noiseValue = Mathf.Max(0, noiseValue - settings.minValue);
return noiseValue * settings.strength;
```

Three things matter, and we do none of them:

1. **Elevation is an unbounded signed displacement, not a 0..1 field.** Geometry is
   `radius * (1 + elevation)`. There is no upper bound to clamp against and no interior threshold.
2. **Sea level is a separate mesh at `radius`, not a value inside the elevation range.** Ocean is
   "where the terrain sphere is inside the water sphere". Nothing normalises against it.
3. **The only clamp is `Max(0, v - minValue)` per layer**, which creates flat basins *deliberately*
   and at the layer's own scale — not a global squash applied to the composed result.

## 3. What we do, and why every artefact follows from it

Our pipeline is:

```
compose bands  ->  SoftSaturate into 0..1  ->  subtract SeaLevel 0.38  ->  divide by 0.62
               ->  branch on sign, compress the sea bed by 0.35  ->  multiply by height scale
```

Every reported artefact is a fingerprint of one of those steps:

| artefact | cause |
|---|---|
| flat mesas with cliff rims | `Clamp01`, then `SoftSaturate`'s knee — both flatten the top of the range |
| terraces along contour lines | slope discontinuities where the piecewise mapping changes: at the knee, at sea level, at the taper |
| cliff and stair-step at the waterline | the sign branch changes gradient discontinuously at exactly `elevation = SeaLevel` |
| coastline tracing plate polygons | land/sea decided by a *threshold* on a field whose plate step (0.4) dwarfs the coastal noise |
| whole flat views ocean, then all land | sea level being an interior constant makes "where is the coast" a tuning problem |

**The 0..1 range with an interior sea level is the mistake.** It forces a bounded field, which forces
clamping, which forces a knee, and every one of those is a place where slope jumps.

## 4. Proposal

### 4.1 Elevation becomes signed displacement

```
elevation(direction) -> metres above (positive) or below (negative) sea level
```

- No `Clamp01`, no `SoftSaturate`, no knee, no `SeaLevel` constant inside the field.
- Land is `elevation > 0`. The coast is the zero crossing, which needs no threshold tuning.
- Height is `elevation * exaggeration`, one multiply, no branch and no piecewise mapping.

### 4.2 Ocean becomes a separate surface

A water plane at height 0 for the flat views, a sphere of `radius` for the planet. The sea floor is
just terrain that happens to be below it, so no depth compression and no sign branch.

### 4.3 Layers compose with masks, and each shapes itself

Keep the plate structure — it is the part that works, and boundary lift is measurably correct
(subduction +0.346, collision +0.164, rift −0.193). Feed it as one signed contribution among several:

```
continentShelf  = plateIsContinental ?  +baseHeight : -oceanDepth      (signed, no threshold)
mountainBelts   = ridged(...) * max(0, boundaryLift)                    (masked by the boundary)
hills           = fbm(...) * landMask                                   (masked by being above water)
detail          = fbm(...) * smallAmplitude
elevation       = sum
```

`Max(0, v - minValue)` per layer where a flat basin is wanted, rather than one global squash.

### 4.4 Slope, not frequency, is the sampling limit

The measured lesson worth keeping: the mesh samples every 2.5 units, and the ridged band produced
~17 units of rise across ~4 units of ground — a 76° face that renders as a staircase. Doubling mesh
resolution doubled the stripe count without removing them.

So the octave cap must be derived from **representable slope**, not only from Nyquist on position:
an octave may only contribute amplitude up to roughly `sampleSpacing × maxSlope`.

## 5. What to keep

- `PlateStructure` — T0, measurably working.
- `IcoSphere` — removed the polar singularity.
- The Whittaker palette and per-corner vertex colour.
- **Both instruments**: the statistics dump and, especially, the PNG render loop. The render is what
  finally separated shading from geometry, and it did so in one image after six wrong guesses.

## 6. What to delete

`SoftSaturate`, the `SeaLevel` constant inside `PlanetTerrain`, the sign branch and 0.35 sea-bed
compression, the edge taper, and the vertex jitter. Each was added to compensate for the bounded
range, and none of them is needed once elevation is signed.

## 7. Honest note on process

The user asked three times for research before it was done. Fifteen rounds of first-principles
tuning produced six wrong diagnoses; twenty minutes reading a working implementation produced the
architectural answer. **Read a reference implementation before writing a generator, not after.**

## Sources

- https://github.com/SebLague/Procedural-Planets — `ShapeGenerator`, `RidgidNoiseFilter`,
  `SimpleNoiseFilter` read directly from the repository.
- https://www.world-creator.com/en/learn/guides/digital-terrain-creation/digital-terrain-creation.phtml
  — terracing as a quantisation artefact of bounded heightmaps.

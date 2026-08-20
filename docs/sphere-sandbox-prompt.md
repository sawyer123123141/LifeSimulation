# Sphere + Heightmap Visual Sandbox — Handoff Prompt

**Scope:** a standalone visual prototype, in a throwaway sandbox. **Not** code for this repository.

**Why it is worth doing anyway:** the elevation field can be designed in the *same shape* as the
moisture, fertility and temperature fields already shipped (`EnvironmentNoise`, `EnvironmentField`).
If the sandbox uses that shape, integrating later is "add a fourth channel", not "rewrite the noise".
If it does not, the work is decorative only.

**What integration would actually require, so nobody is surprised:** every position in the simulation
is a 2D `SimVector2`, the arena is hardcoded `(-25, 25)`, and perception uses a uniform 2D grid.
Movement, distance, plant dispersal, site placement and spatial hashing all assume a flat plane. A
real spherical world is a spatial-model refactor threaded through all of that while preserving
determinism and every hash baseline. It is P6/P7 work, gated behind P4 and P5. The sandbox does not
change that — it informs the design.

---

```
I want a STANDALONE VISUAL PROTOTYPE of a small procedurally generated planet — a sphere
with elevation — that I can spin and look at. Use three.js (or p5/WebGL if you prefer);
it is a throwaway sandbox, not production code.

I also want the elevation SPECIFIED AS NUMBERS, because I will reimplement it in C# later
against an existing system. Parameters transfer; implementations do not.

=== THE PROJECT THIS FEEDS ===

A deterministic artificial-life ecology simulation. Plants and animals have real genomes,
mutate, and are selected on. The current world is a flat 50x50 arena. The roadmap ends at a
small spherical planet, and I am designing toward that now.

I have ALREADY built three environment fields — moisture, fertility, temperature — as 3D
noise sampled on a sphere. Elevation should be a FOURTH field in the same family, so please
match the existing scheme rather than inventing a parallel one.

=== THE EXISTING SCHEME YOU MUST MATCH ===

Noise primitive: 3D VALUE noise on an integer lattice, quintic fade
t*t*t*(t*(t*6-15)+10), trilinear interpolation of the 8 corners. Lattice corners are hashed
deterministically. Value noise rather than Perlin/simplex specifically because a gradient
table would have to be specified exactly to stay reproducible across platforms.

fBm: sum octaves, each amplitude *= gain and frequency *= lacunarity, then divide by the
total amplitude so output stays 0..1 regardless of octave count.

Domain warp: offset the sample point by another noise field before sampling, warp strength
in noise units.

Contrast expansion: clamp01(0.5 + (v - 0.5) * strength). Needed because fBm concentrates
near 0.5 — raw 4-octave output only spanned about 0.37..0.82, which was too flat to matter.

Sphere mapping: sphere radius 500, arena 50 units wide, treated as a small patch near the
equator. Position maps to a unit-sphere point, then scales into noise space by
NoiseScale = FeaturesAcrossArena * SphereRadius / ArenaSize = 3 * 500 / 50 = 30.
So features are sized independently of the radius, and shrinking the radius later turns the
same functions into a full planet.

The three existing fields, for reference — match this level of specificity:

  MOISTURE     warped fBm, 4 octaves, lacunarity 2.0, gain 0.5, warp strength 0.35,
               contrast 2.4, remapped to 0.15..1.00
  TEMPERATURE  0.7 * latitude band + 0.3 * fBm(3 octaves, lacunarity 2.1, gain 0.45),
               remapped to 0.20..1.00
  FERTILITY    warped fBm, 3 octaves, lacunarity 2.0, gain 0.5, warp strength 0.2,
               contrast 2.0, then multiplied by a moisture-balance term that penalises
               BOTH extremes (waterlogged and arid are both poor soil), remapped to
               0.20..1.00

=== WHAT I WANT FROM YOU ===

1. A spinning sphere with visible elevation — mountains, plains, coastlines, whatever the
   scheme produces. Sea level as a threshold on the elevation field.
2. Elevation specified in the same format as the three above: noise type, octaves,
   lacunarity, gain, base frequency, warp strength, contrast, remap range.
3. Your opinion on the design questions below.
4. A biome colouring that combines elevation with moisture and temperature, so I can see
   whether the four fields together read as a believable world.

=== DESIGN QUESTIONS I WANT YOUR OPINION ON ===

1. Plain fBm gives rolling blobs. What gets RIDGES and mountain chains instead — ridged
   multifractal (1 - |noise|, accumulated), erosion approximations, something else? Give me
   the actual formula, not just the name.
2. Should temperature be modified by elevation (a lapse rate), so high ground is cold
   independently of latitude? I think yes; tell me the rate and argue for it.
3. Should moisture be modified by elevation — rain shadows, orographic lift? This is the
   one that most makes a world feel causal rather than layered. Is it worth the complexity
   at this scale?
4. What sea-level threshold gives a good land/water ratio, and should sea level be a
   constant or derived from the elevation histogram?
5. At planet scale, how many octaves before the detail is below what a viewer can see? I
   would rather not pay for octaves that never show.

=== HARD CONSTRAINTS ON THE SPEC (not on the sandbox code) ===

The eventual C# implementation must be a PURE FUNCTION of position and an integer seed:
identical results on every platform and every run, no call-order dependence, no cached
state, no time or threading. Implementable with only System.Math.

So the SPEC must avoid: engine-specific noise, external noise libraries, GPU-only
techniques, and any hash you do not specify exactly. The sandbox itself can use whatever is
convenient to look at — just make sure the spec you hand me does not depend on it.

=== DELIVERABLE ===

1. The running sandbox (code is fine here, it is throwaway).
2. An ELEVATION FIELD SPEC in the same format as the three reference fields above.
3. Answers to the five design questions, with numbers.
4. Screenshots at planet scale and at "small patch" scale — the second matters because a
   creature is 1 unit and the current arena is 50 across, so I need to know what the terrain
   looks like when you are standing on it, not just from orbit.

Ask me anything that would change your answer before you start.
```

---

## Notes for whoever integrates this later

- Elevation slots in as a fourth channel on `EnvironmentSample` alongside `Moisture`, `Fertility`,
  `Temperature`, and a fourth noise channel offset in `EnvironmentField.SampleProcedural`.
- If the spec answers "yes" to the lapse rate or rain shadow, those become modifiers applied *after*
  the base fields, and they will make `TemperatureTolerance` and `MoistureTolerance` more meaningful
  by widening spatial variation — the same lever measured in
  `docs/experiments/plant-gene-liveness-2026-08-18.md`.
- Adding elevation to `EnvironmentSample` changes no hash by itself, but anything that *reads* it
  does. Land it behind a flag defaulting false, like every other behavior change here.
- Terrain geometry, sphere movement and the camera are separate work from the elevation field, and
  much larger. The field can land and be measured long before the world is round.

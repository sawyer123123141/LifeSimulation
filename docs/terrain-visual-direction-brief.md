# Terrain Visual Direction — External Exploration Brief

**Status:** P6 is deliberately last, gated behind P4 and P5. This brief covers **visual direction
only** — look, palette, biome vocabulary, field shapes. It does **not** authorise building a terrain
system, and no code produced from it should enter this repository.

Purpose: let art direction be explored in parallel (in ChatGPT or any sandbox) and come back as a
**specification we can implement**, rather than as code that has to be thrown away.

---

## 1. What we want back

**A spec, not code.** Concretely:

- Named field layers and what each one means ecologically.
- **Numeric parameters** — octaves, lacunarity, gain, warp strength, threshold values, blend weights.
  These transfer perfectly. Implementation language does not.
- Reference images or a live sandbox showing the look at our zoom level.
- A biome table: which combinations of moisture / temperature / fertility produce which biome, with
  the cutoffs.

**Why not code:** anything written against Unity's `Mathf.PerlinNoise`, `System.Random`,
`UnityEngine.Random`, or a noise library will be rewritten from scratch (§3). Handing us parameters
skips that entirely.

---

## 2. Where terrain actually plugs in

There is already a seam, and it is small. Terrain generation replaces the body of one method:

```csharp
// Assets/Scripts/Simulation/Environment/EnvironmentField.cs
public readonly struct EnvironmentSample
{
    public float Moisture { get; }      // 0..1
    public float Fertility { get; }     // 0..1
    public float Temperature { get; }   // 0..1
}

public sealed class EnvironmentField
{
    public EnvironmentSample Sample(SimVector2 position);
}
```

Today `Sample` returns a hardcoded linear moisture gradient:
`moisture = .25f + .75f * clamp01((x + 25) / 50)`, fertility and temperature pinned at 1.

A separate `TemperatureField.Sample(position, tick)` returns `20 + 8*sin(0.18x + 0.11y)` °C and its
comment already anticipates being replaced by "a future world-generation pass".

**So the first terrain deliverable is: better field functions.** Not meshes, not rendering, not
chunks. Three scalar fields over 2D position.

### What already consumes these fields

- `PlantGrowthSystem.Step(Plants, Environment, dt)` — growth responds to moisture.
- `PlantGenome` carries `MoistureTolerance` and `TemperatureTolerance` genes, already inherited and
  mutated, so plants can already adapt to spatial variation.
- `ThermoregulationSystem.ScoreThermalComfort` — animal movement responds to temperature.
- `PlantSiteRegistry` — which sites exist and can be colonised.

This matters: **spatial field variation is already wired to selection.** Richer fields immediately
create real biome adaptation, without any new mechanism.

---

## 3. Hard constraints (non-negotiable — these are what make code untransferable)

1. **Determinism.** Identical seed must produce an identical world, bit-for-bit, on every platform
   and every run. All randomness goes through `DeterministicRandom` (a file that is never edited).
   **Banned:** `System.Random`, `UnityEngine.Random`, `Mathf.PerlinNoise` (platform-variable),
   anything time- or thread-dependent, and iteration over a `Dictionary`/`HashSet` whose order is
   not defined.
2. **Pure function of position and seed.** `Sample(position)` must not depend on call order, on what
   was sampled before, or on cached mutable state. Two calls with the same argument return the same
   value forever.
3. **No Unity types in the simulation assembly.** No `Vector2`, `Mathf`, `MonoBehaviour`,
   `ScriptableObject`. We use our own `SimVector2` and `System.Math`. The simulation must run headless
   under `dotnet test` with no Unity present.
4. **State-hashable.** Anything that becomes part of world state must fold into
   `ComputeStateHash`. Fields derived purely from a seed need not be hashed; generated *state* does.
5. **Behind a flag.** New behavior lands as a `SimulationConfig` bool defaulting `false`, and
   flag-off must be **byte-identical** to today, proven by hash regression tests.
6. **Arena is currently `(-25, 25)` on both axes**, hardcoded in `SimulationWorld`. Design fields as
   continuous functions so they work at any extent — do not bake in the number 50.

### The sphere caveat — read this before choosing a noise scheme

The roadmap ends at **P7: a small spherical / planet-like world**, and the paused terrain brainstorm
(2026-08-13) chose **sphere-sampled fields** for exactly that reason.

So: **do not design around 2D-plane noise that cannot be evaluated on a sphere.** A flat tileable
Perlin plane looks fine now and has to be discarded at P7. Prefer schemes that are defined in 3D
space and sampled on a surface — 3D simplex/value noise evaluated at a point on a sphere, or domain
warping in 3D — so the same field function works for the flat prototype *and* the eventual planet.

The current flat arena can be treated as a small patch of the sphere's surface.

---

## 4. Design questions worth exploring

- What does a moisture field look like that creates **recognisable regions** — a wet side, a dry
  side, and interesting boundaries — rather than a smooth ramp?
- Should fertility correlate with moisture, anti-correlate, or be independent? This decides whether
  biomes feel like real ecosystems or like noise.
- How many distinct biomes read clearly at our zoom, where a creature is roughly one unit and the
  arena is 50 across?
- What temperature structure gives thermoregulation something meaningful to do — latitude bands,
  altitude, or both?
- Where should plant sites cluster? `PlantSiteRegistry` currently holds hand-placed sites; terrain
  should eventually place them, and site geometry has already been shown to matter a lot
  (`p4-calibration-unblocked-carrying-capacity-2026-08-17.md`).

---

## 5. Deliverable format

A short document containing:

1. **Field definitions** — for each of moisture, fertility, temperature: the noise type, octave
   count, lacunarity, gain, frequency, any domain warp and its strength, and the final remap to 0..1.
2. **Biome table** — cutoffs on the three fields, with a name and a colour per biome.
3. **Reference images** at arena scale.
4. **A note on sphere behaviour** — confirmation the scheme evaluates on a sphere surface.

That maps directly onto an implementation task here. Anything beyond it is likely to be discarded.

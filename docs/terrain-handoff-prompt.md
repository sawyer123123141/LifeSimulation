# Terrain Field Spec — Handoff Prompt

Paste the block below into ChatGPT (or any assistant without repo access). It is written to be
**self-contained**: it assumes zero knowledge of this project.

**Scope discipline:** an assistant without the repository can produce an excellent *specification*.
It cannot safely produce code that lands here — the determinism and no-Unity-types rules are
unusual, and violations are invisible until they break reproducibility. Ask for parameters; bring
them back; implementation happens against the real code.

Bring back: the field definitions, the biome table, and the sphere confirmation. Those map directly
onto an implementation task. Anything else is likely to be discarded.

---

```
I'm designing procedural environment fields for a deterministic artificial-life ecology
simulation written in C#. I need a SPECIFICATION WITH CONCRETE NUMBERS — not code. I will
implement it myself. Parameters transfer between languages; implementations do not.

=== WHAT THE SIMULATION IS ===

A headless, fully deterministic ecology sim. Plants grow in patches, reproduce, mutate, and
die. Animals forage, drink, thermoregulate, reproduce, and evolve. Both have real genomes
that are inherited, mutated, and selected on. Same seed must reproduce bit-identically.

The current world is a flat 50x50 arena, coordinates -25..25 on both axes. A creature is
roughly 1 unit across. The long-term roadmap ends at a small SPHERICAL planet-scale world.

=== WHAT I NEED FROM YOU ===

Three scalar fields over position, each normalized to 0..1:

  Moisture      - how wet a location is
  Fertility     - soil quality / nutrient availability
  Temperature   - local climate

These are not decoration. They already drive live selection:
  - Plants carry heritable MoistureTolerance and TemperatureTolerance genes that mutate
    and are selected on. Both cost growth to carry, and pay off only where the field is
    limiting. With a flat environment they are pure taxes; with real variation they become
    genuine adaptation.
  - Plant growth rate is limited by all three fields.
  - Animals thermoregulate and prefer comfortable temperature.
  - Plant colonization sites should eventually be placed by terrain rather than by hand.

NOTE: Fertility is currently an entirely unused channel — no plant gene adapts to it yet.
So you have design freedom there, and I'd like your opinion on what it should mean
ecologically and whether it deserves its own plant trait.

=== HARD CONSTRAINT 1: MUST WORK ON A SPHERE ===

The roadmap ends at a spherical planet. The flat 50x50 arena should be treated as a small
patch of that sphere's surface. So the field functions must be defined in 3D and evaluated
at a point on a sphere — 3D simplex/value noise, 3D domain warping, and so on.

Do NOT propose a tileable 2D-plane scheme. It would look fine now and be thrown away later.
Confirm explicitly that your scheme has no seams or pole artifacts on a sphere.

=== HARD CONSTRAINT 2: DETERMINISM ===

Sample(position) must be a PURE FUNCTION of position and a world seed:
  - Identical results on every platform, every run, bit-for-bit.
  - No dependence on call order, cached state, time, or threading.
  - Implementable in plain C# using only System.Math.

Therefore unusable: any engine noise function (Unity's Mathf.PerlinNoise is
platform-variable), external noise libraries, GPU-only techniques, hash functions whose
output isn't specified exactly. If your scheme needs a hash or gradient table, specify it
precisely enough that two people would implement the same thing.

=== SCALE ===

Arena is 50 units across; a creature is ~1 unit. Regions should read clearly at that zoom —
a creature should cross between biomes in a meaningful but not trivial journey. Fields must
be continuous functions that work at any extent; don't bake in the number 50.

=== QUESTIONS I WANT YOUR OPINION ON ===

1. What moisture field creates recognizable regions — a wet side, a dry side, interesting
   boundaries — rather than a boring smooth gradient?
2. Should fertility correlate with moisture, anti-correlate, or be independent? This
   decides whether biomes feel like real ecosystems or like layered noise. Argue for one.
3. How many distinct biomes read clearly at this zoom? I'd rather have 4 legible ones than
   9 that mush together.
4. What temperature structure gives thermoregulation something meaningful to do — latitude
   bands, altitude, both, something else? Remember it must work on a sphere.
5. Where should plant sites cluster? Site geometry has been measured to matter enormously
   here: in one experiment, clustered versus spread site layouts changed population
   extinction rates from 0% to 53% with everything else held constant.

=== DELIVERABLE FORMAT ===

1. FIELD DEFINITIONS — for EACH of moisture, fertility, temperature: noise type, octave
   count, lacunarity, gain, base frequency, any domain warp and its strength, and the final
   remap to 0..1. Actual numbers, not "tune to taste".
2. BIOME TABLE — cutoffs on the three fields, with a name and a color per biome.
3. REFERENCE IMAGES at arena scale, and one at planet scale if you can.
4. SPHERE CONFIRMATION — a note on how the scheme behaves at the poles and across the
   whole surface.

Start by asking me anything that would change your answer, then give the spec.
```

---

## Current state, for whoever picks this up next

- Both checkouts at commit `a4304bd`. 377 tests green. Tree clean.
- Tests run headless: `cd tools/HeadlessTests && dotnet test`.

### The terrain task is two halves; one is already done

| half | status |
| --- | --- |
| `temperatureAdaptation` term in `PlantGrowthSystem` | **Done.** Behind `plantTemperatureAdaptationEnabled`, default false, flag-off byte-identical. |
| Varying temperature and fertility fields in `EnvironmentField` | **This is what the spec above is for.** |

Doing the field half *without* the adaptation half would have made `TemperatureTolerance` more
costly rather than more meaningful, because temperature entered the growth limit as a raw value no
gene could improve against. That is now fixed.

### Two markers that will fail on purpose when the fields land

Both are deliberate, and both failing is the signal that the work succeeded:

1. `LivenessTests.InertFlagsAreExactlyTheKnownSetUnderTheWidestConfiguration` — currently lists
   `plantTemperatureAdaptationEnabled` as inert, because `EnvironmentField` returns
   `Temperature = 1` everywhere and the adaptation expression collapses to the raw value at 1. When
   temperature really varies, the flag goes live and this test fails. **Remove it from
   `KnownInertFlags` at that point.**
2. `PlantLivenessTests.TemperatureAdaptationIsByteIdenticalWhereTemperatureIsUnlimiting` — pins the
   reason for the above.

### After the fields land, the measurement to run

Same method that worked for plant defense: mixed founders so standing variance exists, then check
whether `TemperatureTolerance` responds. **Seed founders with *varying* tolerance** — a uniform
founder value gives zero standing variance and the result will be drift no matter what. That error
cost four sweeps on 2026-08-18; see `AGENT_FIELD_NOTES` §5.

Compare any delta against **its own** sampling error (SD, SE, bootstrap CI), never against another
arm whose spread is small for structural reasons. That error produced a retracted claim the same day.

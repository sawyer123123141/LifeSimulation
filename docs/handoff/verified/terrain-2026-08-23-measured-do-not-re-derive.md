### Terrain (2026-08-23) — measured, do not re-derive

**Scale, settled:** elevation 1.0 is about **30 metres**; one radian is **500 metres**; 1 unit = 1
metre. A slope value `s` in elevation-per-radian is a real grade of `s * 30 / 500`.

**The Voronoi step — the defect fifteen rounds of tuning missed.** Adjacent-sample elevation along a
meridian, 1-unit spacing:

| | median | p90 | max | ratio |
|---|---:|---:|---:|---:|
| nearest-plate only | 0.00093 | 0.00213 | **0.825** | **885** |
| blended across the seam | 0.00122 | 0.01463 | 0.0417 | 34 |

Every plate boundary was a vertical cliff in the field. That is what the terraces tracing closed
contours were.

**Boundary lift, per kind** (continental only, near minus far): subduction **+0.346**, continental
collision **+0.164**, divergent **−0.193**, transform **+0.050**, island arc never continental.
Measured only after breaking out by kind — averaging across kinds let rifts cancel collisions and
read as +0.09.

**Climate and biomes**, before → after:

| | before | after |
|---|---:|---:|
| ice (fraction of surface) | 0.234 | **0.074** |
| grassland | 0.025 | **0.139** |
| desert | absent | 0.007 |
| scrub | absent | 0.023 |
| temperature median | 0.310 | 0.648 |
| moisture minimum | 0.476 | 0.232 |
| moisture saturated at 1.0 | 0.0356 | 0.0039 |
| elevation pinned at 1.000 | 0.0081 | 0.0000 |

Land fraction **0.296–0.298** against a 0.30 target throughout.

**The whole palette exists; the view could not reach it (2026-08-23).** Reported as "never really
saw ice, just green and some sand and water" - which is an accurate description of latitude -15
degrees, and not a description of the generator. The same 400-unit window, walked along one meridian:

| latitude | biome mix |
|---:|---|
| **-15 (the shipped centre)** | Ocean 61.1%, **Grassland 34.9%**, Scrub 1.6%, Beach 1.2%, Marsh 0.8%, Desert 0.5% |
| +23 | Ocean 75.8%, Grassland 10.7%, **Scrub 7.5%**, **Desert 4.7%** |
| +40 | Ocean 42.1%, Grassland 18.2%, Scrub 14.5%, **Tundra 14.2%**, Desert 4.9%, **Ice 4.6%** |
| +57 | **Ice 41.5%**, Grassland 17.4%, Tundra 14.6%, Scrub 12.5%, Ocean 11.6% |
| +75 | **Ice 78.8%**, Tundra 13.6%, Scrub 3.5%, Grassland 2.7% |

All seven biomes appear. **A biome that exists globally and appears in no view is absent for every
purpose anyone has**, so the flat-view centre is now a control (`J`, View tab) with a live biome
readout, and the statistics instrument names biomes per window instead of counting them.

Land is also strongly asymmetric - the south pole window is 100% ocean and the north 99.3% land -
which is plate placement, not climate. High-latitude land is nearly flat: median land grade 0.015 at
+75 against 0.088 at the coast, because it is plate interior far from any boundary.

**Correction: ice is 0.116 of the surface, not the 0.074 recorded earlier.** Land is 0.241, so ice is
close to half of all land. Not caused by the creature-scale retune: the globe samples at 40.7
cycles/radian, so the local and micro bands are gated off there and cannot move a global count. The
earlier figure is stale against a later terrain commit.

**Flat-view windows** — the measurement that showed the views were parked in the wrong place:

| centre | land | biomes |
|---|---:|---:|
| origin (as shipped for days) | **0.001** | 2 |
| continental plate centre | **1.000** | **1** |
| coastline (current) | 0.451 / 0.514 / 0.503 | 6 / 4 / 3 |

**Resolvable frequency:** planet **13.3** cycles/radian (icosphere subdivision 5, ~167 triangles
around the equator); patch **120.6**. That 9x gap is real level of detail, not a defect.

**Creature-scale relief, retuned (2026-08-23).** Reported bumpy in the 200-unit view and acceptable
in the 400-unit one. The difference is which bands the view can resolve: patch resolution is fixed at
193 samples, so the resolvable frequency is **120.6** at 400 units and **241.2** at 200 units, and the
micro band at 150 cycles/radian switches on between them.

Both fine bands were **clipped by the slope ceiling rather than chosen**: `min(0.16, 6/55) = 0.109`
and `min(0.08, 6/150) = 0.040`, so two bands rode the ceiling and summed. Adjacent-sample grade over
land, in metres per metre:

| local / micro amplitude | 400u median | 200u median | 200u p90 | 50u median |
|---|---:|---:|---:|---:|
| 0.16 / 0.08 (was clipped to 0.109 / 0.040) | 0.169 | **0.243** | 0.611 | 0.283 |
| 0.060 / 0.020 | 0.113 | 0.155 | 0.388 | 0.208 |
| **0.036 / 0.012 (now)** | **0.088** | **0.119** | **0.306** | **0.172** |
| 0.024 / 0.008 | 0.077 | 0.103 | 0.278 | 0.156 |
| planet-scale bands only | 0.063 | 0.085 | 0.253 | 0.160 |

The chosen values are both **under** the ceiling, so they are now decisions rather than clamps.
Nothing else in the field moved: at these three views the new band fade evaluates to weight 1 or 0
exactly as the old `if` did, and the retuned numbers reproduce the sweep row for row.

**A hard band gate is a pop.** `if (maximumFrequency >= MicroFrequency)` gives a band **full**
amplitude the instant the camera crosses the threshold, so zooming changed the character of the
ground rather than its detail. Now faded across half an octave (`BandWeight`).

**The 82 degree wall (FIXED, `ce71fcb`).** Reported as "big cut offs" when jumping to another
continent. Not colour - geometry. At latitude 48.7 the field stepped **0.277 to 0.528 between samples
1.04 metres apart, a grade of 7.24**, with *identical* shelf and *identical* seam distance on both
sides, reading **Divergent** on one and **ContinentalCollision** on the other.

Cause: boundary kind and intensity belong to a **pair** of plates, so they change the instant a
different plate becomes second-nearest - along a line through the cell interior, far from any seam,
where the seam blend has already saturated to **1.000** and smooths nothing. Blending the shelf fixed
the seam and could never have fixed this.

Fix: carry **both** candidate neighbours and crossfade on how close they are to swapping
(`SwapTransition = 0.12` radians = 60 m). Where they change places their distances are equal, so both
sides evaluate the same half-and-half mixture.

| | before | after |
|---|---:|---:|
| worst step, lat 48.7 | **7.24** | **2.80** |
| max grade, lat 40.1 | 5.92 | 2.80 |
| max grade, lat 22.9 | 5.32 | 1.05 |
| medians and biome mix | — | unchanged to within a point |

**First hypothesis was WRONG and cost nothing because it was measured, not argued:** a suspected
off-by-one in the seam smoothstep (`Smooth01(0)` returning 0.5, which would have made every seam
discontinuous). It returns 0. The plate-state print on either side of the worst step named the real
cause in two minutes.

**Three scale errors, each worth remembering:**
- `MaximumSlope` 0.55 was a **3% grade** — wrong by 20x, and it crushed every band above ~10
  cycles/radian to centimetres. Now 6 (a 36% grade).
- Height scale was proportional to view width: 28 units per elevation unit at 400, **3.5 at 50** —
  the same ground **eight times flatter the closer you looked**. Now a constant 30.
- The hill band is a **77 m wavelength**, so **less than one hill spanned the 50 m arena**. Local
  (9 m) and micro (3 m) bands added.

---

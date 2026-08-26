# Review: the terrain design brief, checked against this codebase

**2026-08-24.** Reviewing an external design brief on terrain storage and rendering. Verified by
reading the code it describes, not by agreeing with it.

> **MEASURED 2026-08-24, after this review was written** -
> `p6-play-mode-profiled-2026-08-24.md`. **1,090 renderers and 566,272 triangles run at a median of
> 2.83 ms, 354 fps.** The brief's own top item - profile first, terrain is probably not the
> bottleneck - **was right**, and it is the only item in it that mattered. Everything else in this
> review, including my own finding that vertex data is 6.7x duplicated and costs about 53 MB, is
> real and **does not matter at this scale.** The 908 / 232k figures used below were derived from
> reading the quadtree and are superseded by the measured ones.

## Verdict

**One item is right and important. Most of the rest is written for a different program.**

The brief's own §9 asks four questions and says the first "needs answering before any work starts".
It is right that it does — answering them deletes about half the document:

| §9 question | answer in this codebase | consequence |
|---|---|---|
| Built-in Unity `Terrain`, or custom mesh? | **Custom**, and not a heightmap grid at all — an icosphere quadtree over 20 icosahedron faces, spherical triangles (`PlanetChunkedSurface`, `PlanetChunkMesh`) | §2's "x,z implicit in the grid index" and §3's "one flat NxN grid shared by all chunks" do not map without redesigning the LOD scheme |
| Heightmap authored, or generated? | **Generated** from noise and plates (`PlanetTerrain.Sample`) | **§2 and §5 are moot by the brief's own logic** — it says so itself: "if generated, storage is a non-issue entirely" |
| Chunk dimensions and view distance? | `Segments = 16`, `MaximumDepth = 6`, 20 root faces | see the arithmetic below |
| Does the sim modify terrain at runtime? | **No.** Read-only, regenerated freely | the sparse-delta representation §9 contemplates is unnecessary |

## Section by section

**§1 — sim core never touches a vertex.** *Already true and structurally enforced.*
`Assets/Scripts/Simulation/` contains no Unity types at all; that is what lets `tools/HeadlessTests`
compile it against plain .NET. Ground height comes from `EnvironmentField.Sample` /
`PlanetTerrain.Sample`. There is no layering bug to fix.

**§2 — per-vertex storage.** *Right conclusion, wrong reasoning, and it misses the actual cost.* See
below.

**§3 — GPU heightmap displacement.** *The one substantive suggestion, and the one that does not port
cleanly.* Displacing a shared square grid is the geometry-clipmap approach for a flat heightmapped
world. This world is a sphere subdivided as spherical triangles, chosen deliberately — a lat/long
grid has poles and a cube-sphere has seams. Adopting §3 means replacing the LOD scheme, not
optimising it. That may still be right eventually; it is not the "small change" §7 sequences it as.

**§4 — seams.** *Already implemented, and the brief's diagnosis has already been measured wrong here.*
Skirts exist (`PlanetChunkMesh` builds a double-sided skirt, `PlanetChunkLod.SkirtDepth`). More
usefully: **the visible seam in this project is not a crack.** It was investigated and the cause is a
flat-shaded coastline quantised to the triangle grid — shrinking the skirts fourfold changed the
rendered image by **zero bytes**. CDLOD would fix cracks this project does not have, and would not
fix the artefact it does. It would still kill LOD popping, which is a real if smaller prize.

**§5 — things not worth trying.** *Correct, and aimed at nobody.* No compression scheme was being
invented and no billion chunks were being chased. Harmless.

**§6 — access patterns.** *Half applies.* Struct-of-arrays and zero per-frame allocation are already
the house style (`CreatureStore`, `PlantPatchStore`). **Morton ordering does not apply**: the
simulation's spatial index is a `UniformGrid` over a 50-unit arena at cell size 5 — **100 cells
total**. Z-ordering 100 cells buys nothing.

**§7 — sequencing.** *Item 1 is the best thing in the document* (below). Items 3 and 4 are sequenced
wrongly — see the index-buffer correction.

**§10 — multi-resolution simulation.** *Describes a problem this codebase does not have, and misstates
where it is.* The simulation is a single 50-unit arena; there are no off-screen regions to freeze or
reconstitute. It also says "the roadmap is at P1" — **this project is at P6**.

## The one item that is right, and the most valuable line in the brief

> **§7.1 Profile first. It is likely that terrain is *not* the bottleneck.**

This is correct and it is exactly what is missing. The handoff already records that **nothing in Play
mode has been observed** — the camera, the planet view and the tuning-drag fix are all verified by
compile and offline capture only, and **908 chunks means 908 renderers, never profiled**. Every
suggestion in this document, mine included, is unranked until that number exists.

## What the brief missed, with the arithmetic

`TerrainMeshBuilder.FlatShaded` **de-indexes every chunk**: each triangle gets three unique vertices
and the index buffer is written as `flatTriangles[i] = i` — the identity permutation.

Per chunk at `Segments = 16`: 256 surface triangles plus 192 skirt triangles = **448 triangles, 1,344
indices**. Indexed, the same geometry needs **201 vertices**.

| channel | stored | per chunk | note |
|---|---|---|---|
| position | 1,344 × 12 B | 16.1 KB | **6.7× duplicated** by de-indexing |
| normal | 1,344 × 12 B | 16.1 KB | `RecalculateNormals` |
| color | 1,344 × 16 B | 21.5 KB | `Color` is 4 floats; `Color32` is 4 bytes |
| index | 1,344 × 4 B | 5.4 KB | **the identity sequence — carries no information** |
| | | **≈ 59 KB** | |

At 908 chunks that is roughly **53 MB of mesh data, of which about 4.9 MB is an index buffer that
says `0, 1, 2, 3, …`**.

**But the de-indexing is deliberate, not an oversight**, and the brief would not have caught that
either. Flat shading requires per-face normals, and `Mesh.RecalculateNormals` averages across shared
vertices — so splitting them is the standard way to get the look. The real fix is the one neither
document proposes: **compute the face normal in the shader from screen-space derivatives of world
position**, which gives flat shading on an indexed mesh. That is a ~6.7× reduction on positions and
normals and removes the identity index buffer, and it is invisible in the rendered image if done
right.

`Color` → `Color32` is a separate 4× on the largest channel and is trivially safe.

**So §2's conclusion survives and its reasoning does not.** It says don't store normals and UVs and
share an index buffer. UVs are already not stored. Normals are stored for a reason. And **a shared
index buffer is impossible here**: every chunk is its own `Mesh` object with its own `MeshFilter`, and
Unity `Mesh` objects cannot share an index buffer. Sharing one requires instancing a single mesh —
which is §3. **§7 sequences §2 as a cheap step-3 independent of the step-4 GPU work; for this
codebase it is not independent of it at all.**

## An inconsistency in this project's own recorded numbers

Two figures are on record and they do not reconcile: **232k triangles drawn**, and **908 chunks at
ground level**. At 448 triangles per chunk, 908 chunks is ~407k triangles and 232k is ~518 chunks.
They were almost certainly captured from different viewpoints. **Neither should be quoted as "the"
triangle count until one measurement produces both numbers at once.**

## What I would actually do, in order

1. **Profile Play mode.** Frame time, draw calls, and where the time goes at ground level. Nothing
   below is worth ranking without it. This is the brief's §7.1 and it is right.
2. **Check whether draw calls dominate before touching vertex data.** 908 renderers is the loudest
   suspect and it is a completely different fix — merging finished chunks into fewer renderers —
   from anything in §2.
3. `Color` → `Color32`. Small, safe, independent of everything else.
4. Shader-computed face normals, if and only if vertex memory or upload cost shows up in the profile.
5. Treat §3 and §4 as a redesign proposal to be argued on its merits, not as an optimisation queue.

## What is not verified here

**No measurement was taken for this review.** Every claim above is from reading the code or from
numbers already recorded in `docs/`. In particular **the 53 MB figure is arithmetic, not an observed
allocation**, and whether any of it matters is precisely the open question. That is the brief's own
closing instruction — *verify, don't assume* — and it applies to this review as much as to the brief.

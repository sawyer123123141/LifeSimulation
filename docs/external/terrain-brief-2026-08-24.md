# Terrain System — Design Brief

Context: LifeSimulation (Unity 6, C#). Heightmap terrain rendered as a mesh.
Architecture rule already in place: simulation core is data-oriented and independent
of Unity GameObjects; Unity is presentation only. This brief respects that split.

---

## 1. The central conclusion

**Terrain data is not a mesh. The mesh is a derived render cache.**

The source of truth is a 2D scalar field: `height(x, z)`, plus whatever biome /
moisture / resource layers the sim needs. The mesh is an output of that field,
regenerable at any time.

This splits the work cleanly into two problems that should not be conflated:

| | Simulation core | Rendering |
|---|---|---|
| Data | 2D scalar arrays, or noise functions | Vertex/index buffers, textures |
| Access | `O(1)` array index or noise eval | Streamed by camera distance |
| Cares about | Throughput | Frame time |
| Needs chunks? | **No** | Yes |

**Action:** the sim core should never touch a vertex. If any simulation system
currently reads mesh data to answer "how high is the ground here," that is a bug
in the layering — it should read the scalar field directly.

---

## 2. Heightmap storage — how small it actually gets

For a regular grid, most per-vertex data is implicit and should not be stored:

- **x, z** — implicit in the grid index. Do not store.
- **y** — the only real payload. `uint16` normalized to a chunk-local min/max
  range is enough for terrain (65536 steps across a chunk's height range).
  4 bytes → 2.
- **Normals** — derive via central differences on neighbouring heights, in the
  shader or in a Burst job. Do not store. 12 bytes → 0.
- **UVs** — derive from x,z (or triplanar for cliffs). Do not store. 8 bytes → 0.
- **Index buffer** — for a regular grid, topology is *identical for every chunk at
  the same LOD*. Build one shared index buffer per LOD level and reuse it for
  every chunk. Per-chunk index cost is zero.

A 256×256 heightmap chunk is ~128 KB as uint16. That is already near the entropy
floor for stored data; see §5.

---

## 3. Recommended rendering approach

**Don't build per-chunk meshes on the CPU. Displace a shared grid on the GPU.**

1. Upload each chunk's heightmap as an `R16` (or `R16_UNorm`) texture.
2. Keep one flat NxN grid mesh in memory, shared by all chunks.
3. In the vertex shader, sample the heightmap and displace `y`.
4. Per-chunk streaming cost becomes a single texture upload — no mesh
   generation, no per-chunk vertex/index buffers, no CPU vertex work.

This is the geometry-clipmap / GPU-terrain approach. It is what makes large
worlds cheap, and it removes mesh generation from the critical path entirely.

If GPU displacement is too large a change right now, the fallback is CPU mesh
generation in a Burst job writing directly into a `NativeArray`, with the shared
index buffer from §2. Still avoid per-chunk index buffers.

---

## 4. LOD and the seam problem

Adjacent chunks at different LOD levels produce **cracks** — visible gaps where a
low-resolution edge doesn't line up with the higher-resolution edge beside it.
This *will* happen; plan for it rather than discovering it.

Two standard fixes:

- **Skirts** — drop a vertical apron around each chunk's border. Cheap, trivially
  correct, slightly wasteful, occasionally visible from below. Fine as a first pass.
- **CDLOD** (Continuous Distance-Dependent LOD) — morph vertex positions
  continuously toward the coarser level as distance increases, so seams match
  exactly. Also eliminates LOD popping. **This is the recommended target.**

CDLOD composes naturally with the GPU displacement approach in §3, since the morph
is a vertex-shader operation.

---

## 5. Things that are NOT worth trying (and why)

These were investigated and ruled out. Recording them so they don't get
re-attempted.

- **Inventing a novel compression scheme.** By the counting argument, no scheme
  compresses every input — 2^n strings of length n, fewer than 2^n shorter ones.
  Shannon entropy is a proved floor, not an engineering frontier. Use standard
  approaches (quantization, delta encoding, RLE, LZ4 for cold storage) and stop.
- **Chasing "a billion chunks."** No system stores a billion chunks. The trick is
  always that chunks are generated on demand from a seed and most are never
  materialized. The real budget is *chunks materialized per second*, which is
  bounded by view distance and agent count — a number in the thousands, not
  billions.
- **Optimizing vertex layout for voxel terrain** (octahedral normals, meshlets,
  SVO-DAGs, Transvoxel). All correct for voxels; all irrelevant here. A heightmap
  is a strictly simpler case and most of that machinery collapses to nothing.

---

## 6. Where speed actually comes from

Storage size is not the bottleneck; **memory access patterns are.** Identical
bytes laid out differently can differ by 5–10× in throughput.

- Struct-of-arrays, not array-of-structs, for anything iterated in bulk.
- Morton / Z-order indexing for 2D fields so spatial neighbours are adjacent in
  memory — matters for any system doing neighbourhood queries.
- Burst + Jobs for hot loops, as already planned — but **after** profiling.
- Zero allocation in per-frame paths (watch for GC spikes in the profiler).

---

## 7. Sequencing — do this in order

1. **Profile first.** Do not optimize anything until there is a number. Capture:
   current simulation throughput (ticks/sec), frame time, and the top three
   entries in the Unity profiler. It is likely that terrain is *not* the
   bottleneck and agent decision-making dominates.
2. Enforce the §1 split — sim core reads scalar fields, never meshes.
3. Shared index buffer + derived normals/UVs (§2). Small change, immediate win.
4. GPU heightmap displacement (§3).
5. CDLOD (§4).
6. Re-profile after each step and compare against the baseline.

---

## 8. How to prove any of it

Use the existing paired-seed benchmark harness. For every change:

- Same seed, baseline vs. modified, multiple runs.
- Report median and variance, not a single run.
- Metric is simulation throughput (the project's stated key metric), plus frame
  time separately for rendering changes.

Complexity analysis is near-useless here — constants and cache behaviour decide
real performance. Measured numbers are the only proof that counts. This applies
to suggestions in this document as much as anything else: **verify, don't assume.**

---

## 9. Open questions for whoever implements this

- Is this Unity's built-in `Terrain` component, or a custom mesh? The built-in
  Terrain already implements much of §3 and §4 internally, and if it's in use,
  most of this brief is redundant — the answer becomes "configure it correctly."
  **This needs answering before any work starts.**
- Is the heightmap authored/stored, or generated from noise? If generated, storage
  is a non-issue entirely and only the render path matters.
- Current chunk dimensions and view distance?
- Does the sim modify terrain at runtime? If yes, the modified deltas are the only
  irreducible storage cost and need a sparse representation. If no, terrain is
  read-only and can be regenerated freely.

---

## 10. Not now, but noted

The genuinely unsolved problem in this project is **not** terrain — it's
multi-resolution simulation: what happens to populations, genetics, and ecology in
regions no observer is near. Options are freeze (world feels dead, evolution
stops off-screen), simulate fully (defeats the purpose), or run coarse aggregate
statistics and reconstitute plausible individuals on approach. The hard part is
consistency — repeated zoom in/out must not drift or leak. This is testable with
the same paired-seed harness (region A fully simulated vs. region B
coarse-then-reconstituted, compare distributions).

This is a P6-scale problem. The roadmap is at P1. **Do not start it now** — it is
recorded here only so it isn't lost.

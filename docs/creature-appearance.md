# Making adaptation visible — design, not yet built

**Status: step 2 built, steps 1/3/4 outstanding (2026-08-24).** The pure mapping is committed as
`Assets/Scripts/Presentation/CreatureAppearance.cs` and `CreatureAppearanceRules.cs`, with eight
headless tests. Nothing is applied to any renderer yet - that is the half a model swap would redo.

**Original status: planned, deliberately unbuilt (2026-08-24).** Real creature models are expected soon and
this must not be work that has to be undone. What follows is written so that the part which survives
a model swap is separated from the part which does not.

## The problem

Selection is measurably running — `docs/experiments/p6-selection-is-happening-2026-08-24.md` has
temperature tolerance moving +0.277 and lifespan tendency +0.257 over a run, against a control that
does not move. **None of it is visible.** What the view currently shows:

| channel | driven by | file |
|---|---|---|
| size | action x age x **body-size gene** (0.7 to 1.35) | `Prototype1Presenter.Views.cs:104` |
| colour | **current action** — wander, food, water, mate, flee, hunt | `Views.cs:105`, `GetActionColor` |
| mesh | `GameObject.CreatePrimitive(Capsule)` | `Views.cs:79` |

So one gene is expressed, and it is one of the **ten traits with no detectable selection** — it
varies between individuals and trends nowhere. The two traits that are adapting hard have no visual
expression whatsoever. A run where the population shifts a quarter of the thermal range looks
identical to one where nothing happens.

**P5 does not fix this.** Its question is "can we summarise species and history without faking it" —
genetic distance, lineage ids, cluster history, the panel. It is an analysis layer. It can tell you a
population has split; it draws nothing.

## What survives real models, and what does not

The seam is: **deciding what a creature should look like** against **applying it to whatever a
creature is made of**.

- **Survives.** A pure function from genome to appearance: `(Genome) -> CreatureAppearance`, where
  `CreatureAppearance` is a small struct of tint, scale multiplier, and later whatever a real model
  can vary. No Unity types beyond `Color`. Testable headlessly, exactly like `FreeCameraMotion` and
  `PlanetChunkLod`. **This is the whole of the design work and none of it is thrown away.**
- **Does not survive.** Anything that assumes a capsule, one renderer, or one material — setting
  `GetComponent<Renderer>().material.color` directly, and `Vector3.one * scale` on a primitive.
  Confine it to the existing few lines in `Views.cs` so the model swap edits one place.

**Do not** bake appearance into creature creation, and do not add per-creature materials at spawn: a
real model will bring its own material setup, and appearance has to be re-applied every frame anyway
because it tracks genes that change between generations.

## The mapping

Two channels, because there are two questions a viewer asks — *what is it doing* and *what is it*.

| channel | shows | why |
|---|---|---|
| hue | **temperature tolerance** (cold-adapted → heat-adapted) | the trait with the strongest measured selection - but see the saturation warning below |
| brightness or outline | current action | keeps the behaviour read the HUD legend already documents |
| size | body-size gene, unchanged | already correct |

**Colour must not silently replace the action colours.** The HUD legend documents them and they are
how anyone reads behaviour at a glance. Put genome tinting behind a toggle — `U` is unbound (checked
against every binding in `HandleInput`; note `WASDQE` are camera keys while the right mouse is held).
Toggling is also the honest presentation: two pictures of the same population answer two questions.

## The hue channel saturates, and that changes what to watch

`docs/experiments/p6-why-temperature-tolerance-2026-08-24.md` explains why thermal tolerance is the
strongest selection in the model, and the explanation has a consequence for this design. The gene
**plateaus**: the temperature field deviates by at most 8 degrees, tolerance is `2 + 8*gene`, so 0.75
covers the whole world and buys nothing above it. Measured means climb 0.28 over the first 8,000
ticks and **0.004 over the last 4,000**, resting near 0.78.

So the colour changes for two thirds of a run and then stops, and **every population ends roughly the
same colour**. That is not a reason to drop the channel. It relocates what the picture is of:

- **The mean is the boring half.** It goes cream to orange, once, and freezes.
- **The spread is the finding.** Founders scatter across the ramp around 0.50 - blue, cream and
  orange animals mixed together - and selection kills the cold tail until the crowd is uniform. **A
  mottled population becoming a monochrome one is the clearest picture of directional selection this
  model can produce.**

Which also means the applied step should not average anything or tint by the population mean. Per
creature, or the whole point is lost.

## What would make it worth building

A run of the `Y` playtest where the founder crowd is visibly mottled and, by around tick 8,000, is
uniformly heat-adapted. That is the same finding as the CSV, in a form that needs no CSV.

## The genuinely P5-shaped version, later

Once clustering is trustworthy, colour each creature by its **cluster** rather than by a single gene:
a population visibly separating into two groups that stop interbreeding is the thing the whole P5
line exists to describe. It needs the same `(Genome) -> CreatureAppearance` seam, with cluster
identity as the input instead of one trait — which is another reason to put the seam in first and the
mapping behind it.

## Order

1. Real models land.
2. ~~`CreatureAppearance` as a pure function, with tests.~~ **Done.** `CreatureAppearance` is the
   struct - three colour channels and a scale multiplier, no `UnityEngine` types at all -
   and `CreatureAppearanceRules.FromGenome` is the mapping. The scale ramp reproduces the view's
   existing `0.7 → 1.35` on `BodySize` **exactly**, pinned by a test, so adopting it cannot silently
   resize anything. `NeutralMarker` reaching no channel is also pinned by a test: giving the drift
   control a visible channel would make drift and selection look alike.
3. Apply it at the one call site, behind the `U` toggle.
4. Cluster colouring when P5 clustering is trusted.

Doing 2 before 1 is safe — it touches no rendering. Doing 3 before 1 is the part that would be
redone, and is the reason step 3 is still a document rather than a commit.

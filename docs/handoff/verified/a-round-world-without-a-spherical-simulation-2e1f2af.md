### A round world, without a spherical simulation (`2e1f2af`)

**`O`** draws the arena curved onto the planet it is a window on. **Presentation only** - positions
stay `SimVector2` on a flat 50-unit square with Euclidean distances, mapped by `ArenaProjection`
after the tick. No hash moves, no flag, nothing re-measured.

- The planet's centre sits at **(0, -500, 0)**, so the arena's centre lands on the origin with its
  normal up. There the mapping is the **identity**, which is why the camera rig kept working.
- **True scale was free.** The globe preview draws at radius 60 with relief fraction 0.06 - 3.6 units
  per elevation unit; at radius 500 the same fraction is **30**, exactly the arena's own figure. The
  two views were always the same shape at different sizes. **I predicted the mountains would shrink;
  they do not.**
- The patch is curved by remapping the flat builder's output, never by a second spherical builder.
- Ground heights are cached from the **flat** vertices, before curving: "how high is the ground here"
  is a question in simulation coordinates.

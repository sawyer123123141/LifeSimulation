### The join (DONE behind a flag — `6c35905`)

`terrainDrivenEnvironmentEnabled`, **default false, last optional constructor parameter**. On, the
simulation's moisture, temperature and elevation come from `PlanetTerrain` - the same function, seed,
window centre and detail limit the arena mesh is built from.

- **Flag-off is byte-identical.** The suite pins V1 state hashes as literals; 499 of them pass
  unchanged.
- **Detail limit is derived, not chosen:** the simulation samples at the frequency the 193-sample
  arena mesh resolves. Reading a sharper field would mean a creature climbing a bump nobody drew.
- **Output ranges deliberately held** where the plant systems were calibrated - moisture .15 to 1,
  temperature .20 to 1 before lapse, fertility .20 to 1, elevation 0 to 1 - so any difference the
  re-measure finds is the **shape** of the field changing, not its scale.
- Sea bed reads as elevation **zero**, not negative: elevation is a lapse-rate input, and ground below
  the waterline being warmer than the shore is not a claim worth making.
- Four tests (`TerrainDrivenEnvironmentTests`). The load-bearing one asserts the field's elevation
  **equals the generator's own sample at five spread positions** - one point agrees even with a wrong
  window centre or swapped axes. A manipulation check pins that the field actually varies.
- The **manifest drift guard fired on the first run**: a flag the manifest does not name makes a
  result irreproducible. That is the guard working, not a failure.

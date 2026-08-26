## 6. Test commands

### The creature instruments (2026-08-24)

```powershell
dotnet test tools/HeadlessTests            # 603 green
dotnet run --project tools/CreatureSweep -c Release -- --focused 80 100 --scenario=lean
dotnet run --project tools/CreatureSweep -c Release -- --focused 120 100
dotnet run --project tools/CreatureSweep -c Release -- --relief
dotnet run --project tools/CreatureSweep -c Release -- --thermal 40 100 [--join=off] [--terrain-temperature]
dotnet run --project tools/TerrainProbe -c Release -- --ice
```

`--focused <seeds> <cap> [--scenario=moderate|lean|scarce]`. Seeds are filtered to at least 5 m of
climb per traverse. **`--scenario=stable` and `=scarcity` exist but are traps** - those are
observation-family layouts calibrated for different founder counts, and they kill every run in both
arms. Use `lean` and `scarce`, which are `Scaled` copies of the calibrated layout.

Every run prints two tables: the paired arm-against-arm comparison, and **drift from founders**, which
is the one that can see selection.

`--thermal <seeds> <cap>` is a third instrument and a different question: **one arm, twelve
checkpoints**, so it shows the *shape* of a drift rather than its endpoint - which is how the
temperature plateau became visible. It also samples the realised `|T - 20|` under every living
creature, which is what fixes where the saturation ceiling is. `--join=off` turns the terrain join
into an arm; it defaults on and every other mode is unchanged.

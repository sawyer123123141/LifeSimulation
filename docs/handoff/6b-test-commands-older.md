## 6b. Test commands (older)

From `tools/HeadlessTests`:

```powershell
dotnet build
dotnet test --no-build --filter "FullyQualifiedName!~LivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~PlantLivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~LivenessTests&FullyQualifiedName!~RiskAversionIsLiveOnlyWhenThreatsExist"
dotnet test --no-build --filter "FullyQualifiedName~RiskAversionIsLiveOnlyWhenThreatsExist"
```

**Green at handoff: 503 / 19 / 33 / 1.** RiskAversion alone takes ~16 s; silence is not a hang.

Presentation changes additionally need a Unity compile — the headless project excludes
`Assets/Scripts/Presentation`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -logFile '.\Logs\compile.log'
```

Then check `grep -c "error CS"` on the log and confirm `Exiting batchmode successfully`.

---

### Terrain instruments

Unity menu, or headless `-executeMethod`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -executeMethod LifeSimulation.EditorTools.TerrainStatisticsEntry.Dump -logFile '.\Logs\stats.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -executeMethod LifeSimulation.EditorTools.TerrainRenderEntry.Render -logFile '.\Logs\render.log'
```

Statistics land in `Logs/terrain-statistics.txt`; PNGs in `Logs/terrain/`. **The render needs
graphics — `-nographics` disables it.** Both fail while the Unity editor holds the project lock;
either close the editor or run the menu items.

**Neither needs Unity closed if the question is about the field itself:**

```powershell
dotnet run --project tools\TerrainProbe -c Release
```

The probe lives at `tools/TerrainProbe` and compiles the generator directly - it is pure C#, which
is why it works without Unity - reporting **grade** (median, p90, max), **named biome mix**, and the
**worst single step with the plate state on either side of it**. That last one is what named the
82-degree wall; a median cannot see a wall.

That compiles `PlanetTerrain`, `PlateStructure` and `TerrainSettings` directly - they are pure C# -
and prints the adjacent-sample grade for each flat view at **its own** resolvable frequency, with and
without the creature-scale bands.

**A field statistic cannot see a rendering defect and a render cannot see a field discontinuity.**
Both exist because each missed something the other caught.

---

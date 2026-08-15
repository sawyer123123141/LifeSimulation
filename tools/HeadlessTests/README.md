# Headless Test Harness

Runs the EditMode test suite without Unity. Same source files, same tests — just a plain `dotnet` project pointed at `Assets/Scripts/Simulation` and `Assets/Tests/EditMode`.

```bash
cd tools/HeadlessTests
dotnet test
```

Takes well under a second. Useful for:

- Any agent (human, local model, CI) checking its work without opening Unity
- Proving a plan's referenced types/signatures actually compile before implementing it

## Why this exists

`Assets/Scripts/Simulation/` has zero `UnityEngine` references by design (see `docs/ARCHITECTURE.md`), and the EditMode tests are plain NUnit. Nothing here is Unity-specific except the folder Unity happens to also compile.

## Limits

- Runs on .NET 8 (CoreCLR). Unity runs on Mono. Tests involving accumulated floating-point simulation steps can show tiny differences between the two runtimes — a failure here that involves float comparisons after several `world.Step()` calls should be cross-checked in Unity's own Test Runner before treating it as real.
- `GlobalUsings.cs` exists because Unity's `asmdef` system implicitly resolves some `using` directives that a plain csproj does not. If a new test file fails to compile only here, check whether it's missing a `using` that Unity was resolving for it, and add it to `GlobalUsings.cs` rather than editing the test file.
- This project does not run PlayMode tests or anything touching `Assets/Scripts/Presentation/`.

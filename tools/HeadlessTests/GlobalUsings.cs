// Intentionally empty — do not add global usings here.
//
// This file previously carried `global using System;` and
// `global using LifeSimulation.Simulation.Environment;`. Unity's compiler has no equivalent, so
// any source file relying on them compiled cleanly under `dotnet test` and then failed in the
// Unity editor. That happened on 2026-08-18: EnvironmentField.cs was edited to use System.Math
// without a `using System;`, passed 385 headless tests, and broke the editor build.
//
// Emptying it produced zero errors — nothing actually depended on it — and it restores
// `dotnet test` as a faithful proxy for "does this compile in Unity".
//
// If a file needs a namespace, add the using to that file.

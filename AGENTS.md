# Instructions for AI Coding Agents

Read this file completely before editing anything in this repository.

These rules exist because this project is a **deterministic scientific simulation**. Recorded experiment results must stay reproducible. A change that looks harmless can silently invalidate months of recorded evidence.

If a rule here conflicts with the task you were given, **stop and say so**. Do not guess.

---

## 1. What this project is

A 3D artificial-life simulation about genetics, natural selection, and evolution, built in Unity 6 with C#.

Two layers, strictly separated:

- `Assets/Scripts/Simulation/` — pure C# simulation. The authoritative biology. **No Unity code here, ever.**
- `Assets/Scripts/Presentation/` — Unity rendering, camera, UI. Reads simulation state. Never owns biology.

---

## 2. Never modify these files

Do not edit, rename, move, or delete any of these unless your task names the file explicitly and says to change it:

| File | Why |
|---|---|
| `Assets/Scripts/Simulation/Core/DeterministicRandom.cs` | Every recorded experiment depends on its exact output. |
| `Assets/Scripts/Simulation/Environment/TemperatureField.cs` | Frozen P3 physiology evidence depends on its exact values. A replacement is planned on a separate code path. |
| Anything in `Assets/Tests/` | See rule 7. |
| Anything in `docs/` | Documentation is written by humans and by planning agents, not by implementation agents. |
| `.gitignore`, `Packages/`, `ProjectSettings/` | Project configuration. |

Existing values in the `RandomDomain` enum (`Assets/Scripts/Simulation/Core/SimulationTypes.cs`) must never change. You may add a new member with a new number. You may not renumber existing ones.

---

## 3. Determinism rules (most important section)

The simulation must produce **bit-identical results** given the same seed, configuration, and tick count.

**Never use any of these anywhere in `Assets/Scripts/Simulation/`:**

- `System.Random`
- `UnityEngine.Random`
- `DateTime.Now`, `DateTime.UtcNow`, `Environment.TickCount`, `Stopwatch`
- `Guid.NewGuid()`
- Anything reading the system clock, thread ID, machine state, or environment variables
- `Dictionary` or `HashSet` iteration order to drive simulation logic
- Parallel loops, threads, `async`, or `Task`

**For all randomness, use `DeterministicRandom`:**

```csharp
float value = DeterministicRandom.Float01(
    worldSeed,
    RandomDomain.SomeDomain,
    tickOrOrdinal,
    entityA,
    entityB,
    purpose);
```

It takes no state and returns the same value for the same arguments forever. That is the point.

**Do not reorder floating-point arithmetic.** `(a + b) + c` and `a + (b + c)` can produce different results. If you are refactoring math, keep the exact operation order unless your task says to change it.

**Iterate arrays by index**, not by anything whose order could vary.

---

## 4. Performance rules

Simulation code runs every tick for thousands of creatures.

- **No allocation in per-tick code.** No `new` for arrays, lists, or classes inside anything called from `SimulationWorld.Step`. Grow buffers with the existing `EnsureCapacity` pattern instead.
- **No LINQ** anywhere in `Assets/Scripts/Simulation/`.
- **No string operations, string formatting, or logging** in per-tick paths.
- **No `foreach` over anything that allocates an enumerator** in per-tick paths.
- Pass large structs with `in` or `ref` rather than by value.
- Use `ref` accessors (`GetNeedsRefAt`, `GetMemoryRefAt`) when mutating creature state; do not copy, mutate, and write back.

Do not claim something is faster without a measurement. If your task asks for an optimization, add or update a benchmark.

---

## 5. Code style

Match the code that already exists. Read a neighbouring file before writing a new one.

- **Full words in names.** `movementDistance`, not `moveDist`. `phenotype`, not `pt`. No invented abbreviations.
- `readonly struct` for values that do not change. `struct` for mutable per-creature state. `static class` for stateless systems.
- Validate arguments at public entry points with `ArgumentOutOfRangeException`, following the existing pattern. Do not validate inside hot loops.
- Comments explain **why**, not what. Do not comment obvious syntax.
- One responsibility per file. If a file passes roughly 300 lines, that is a signal it is doing too much — mention it, do not silently restructure it.

---

## 6. Scope rules

- **Edit only the files your task names.** If the task says "modify `NeedsSystem.cs`", do not also modify `SimulationWorld.cs` because it looked related.
- **Do not refactor unrelated code.** Not formatting, not renaming, not reorganizing. Even if it is clearly worse than it could be.
- **Do not add dependencies.** No NuGet packages, no new Unity packages, no vendored code.
- **Do not add abstractions that were not requested.** No interfaces, no base classes, no generics, no dependency injection, no event systems.
- **Do not create new files** unless the task says to.

If the task cannot be completed without breaking one of these, stop and explain why.

---

## 7. Testing rules

Tests live in `Assets/Tests/EditMode/` and run through the Unity Test Framework.

**If a test fails:**

1. Assume your change is wrong. It usually is.
2. Read the test to understand what behavior it protects.
3. Fix your code.

**Never** delete a test, comment out a test, weaken an assertion, add `[Ignore]`, or change an expected value to match your output. If you believe a test is genuinely wrong, **stop and report it**. Changing a test to pass is the single worst thing you can do in this repository.

Tests calling `ComputeStateHash` are checking that simulation results have not changed. If one of those fails, you have altered simulation behavior. That is only acceptable if your task explicitly said to change behavior.

---

## 8. When to stop and ask

Stop and report instead of proceeding if:

- A test fails and you do not understand why
- The task requires editing a file listed in rule 2
- The task requires breaking a determinism or performance rule
- The task description conflicts with what the code actually does
- You need an API that does not exist yet
- You are unsure whether a change affects simulation results

A stopped task with a clear explanation is a good outcome. A finished task that silently broke determinism is not.

---

## 9. Before you finish

- The project compiles.
- You edited only the files the task named.
- You added no allocations to per-tick code.
- You used `DeterministicRandom` for all randomness.
- You changed no tests.
- You state plainly what you changed, and anything you were unsure about.

Do not describe work as complete, verified, or passing unless you actually ran it. If you could not run the tests, say that.

---

## 10. Further reading

Read these before non-trivial work:

- `docs/AGENT_FIELD_NOTES.md` — **for the lead/planning agent, not implementation
  subagents.** A file map so the repository does not need re-reading, a ledger of
  which mechanisms actually execute, when a found bug should *not* be fixed, and
  an accumulating lessons log. Append to it when a session ends with a lesson.
- `README.md` — project overview and current status
- `docs/ARCHITECTURE.md` — layer separation and creature representation
- `docs/PERFORMANCE.md` — performance strategy and metrics
- `docs/superpowers/specs/2026-08-12-product-architecture.md` — the permanent architectural principles
- `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md` — known defects, and which are safe to fix

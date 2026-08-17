# Persistence Inheritance Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the `Persistence` gene actually heritable and give it a non-degenerate starting value, so B-4's foraging-economics behavior (commitment bonus + marginal-value abandonment) runs on a live, evolvable input instead of a hardcoded zero.

**Architecture:** Two one-line production changes — add the missing 24th trait to `GenomeInheritance.CreateChild`, and change the `Genome` constructor's `persistence` default from `0f` to `0.5f` to match its four sibling behavioral genes. Plus one new structural regression test that breeds a genome and asserts every gene transmits, so this class of bug cannot silently recur. This is a deliberate, global behavior change: all hash baselines shift and must be rederived from real runs.

**Tech Stack:** C# (.NET), NUnit EditMode tests, `dotnet test` under `tools/HeadlessTests`.

## Background (why this exists)

`GenomeInheritance.CreateChild` passes **23** arguments into a **24**-parameter `Genome` constructor. The omitted parameter is `persistence`, which therefore takes its constructor default. Separately, no code anywhere in the project assigns a nonzero `persistence` — not founder generation, not any variation profile. Net effect: **every creature in every run has `Persistence = 0`, and always has.**

`Persistence` is not dead code. It feeds:
- `GenomePhenotype`: `bodyMass += 0.05f * genome.Persistence`
- `Phenotype.Persistence` → `ForagingEconomics.CommitmentBonus` (`DecisionSystem.cs:865,869`)
- `Phenotype.Persistence` → `ForagingEconomics.ShouldAbandon` (`SimulationWorld.cs:892`)
- `SimulationWorld.ComputeStateHash` (`:364`)

So the B-4 foraging-economics mechanic — recorded as **resolved** on the defect list — has never run with a varying input.

Empirically confirmed before writing this plan: a parent with `Persistence = 0.9` produces children with `Persistence = 0`, while the adjacent gene `Commitment` inherits and mutates normally.

## Global Constraints

- `Genome`'s constructor parameter `persistence` changes its default from `0f` to `0.5f`. Rationale: its four sibling behavioral-tuning genes (`urgencyExponent`, `travelSensitivity`, `riskAversion`, `commitment`) all already default to `0.5f`; `persistence` defaulting to `0f` is the original slip. Starting at `0.5f` also avoids a clamp-boundary artifact — a gene pinned at exactly `0.0` under symmetric mutation drifts upward purely because negative mutations truncate at the clamp and positive ones do not.
- `GenomeInheritance.CreateChild` must pass `persistence` as the 24th argument, using `traitIndex: 23` — the next unused index, so no existing trait's RNG stream shifts.
- This is an intended, global behavior change. Hash baselines WILL change. Every changed baseline must be **rederived by running the actual scenario and reading the produced value** — never hand-edited to whatever makes a test green.
- When any pre-existing test fails after the change, determine whether the new result is a legitimate consequence of `Persistence` now being `0.5` and heritable, or a genuine regression. Record that judgment per failing test in the report. Do not blanket-update expected values.
- No other gene's semantics, no `ForagingEconomics` formula, and no `SimulationConfig` flag may change in this task.

---

## File Structure

- `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs` — `Genome` constructor `persistence` default (`0f` → `0.5f`), at line 31 as read.
- `Assets/Scripts/Simulation/Biology/GenomeInheritance.cs` — add the 24th `InheritTrait` call.
- `Assets/Tests/EditMode/` — new structural regression test (place in whichever existing file holds genome/inheritance tests; find it with `grep -rln "GenomeInheritance" Assets/Tests/EditMode`).
- `Assets/Tests/EditMode/CoreSimulationTests.cs` — 10 hash baseline constants at lines 19, 854, 1103, 1125, 1171, 1430, 1453, 1535, 1599, 1623.

## Task 1: Make Persistence heritable with a non-degenerate default

**Files:**
- Modify: `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs:31`
- Modify: `Assets/Scripts/Simulation/Biology/GenomeInheritance.cs:22-45`
- Test: new test in the genome/inheritance test file; update `Assets/Tests/EditMode/CoreSimulationTests.cs` baselines

**Interfaces:**
- Consumes: `Genome(float bodySize, …, float commitment = 0.5f, float persistence = 0f)` — 24 parameters, `persistence` last. `GenomeInheritance.InheritTrait(float firstParentTrait, float secondParentTrait, int worldSeed, long birthOrdinal, int traitIndex, float mutationStandardDeviation)` — existing private helper, unchanged.
- Produces: heritable `Persistence`; `Genome`'s `persistence` default becomes `0.5f`.

- [ ] **Step 1: Write the failing structural regression test**

Find the right file: `grep -rln "GenomeInheritance" Assets/Tests/EditMode`. Add this test there (it is deliberately structural — it fails for ANY gene that stops transmitting, not just `Persistence`, so it guards the whole class of bug):

```csharp
        [Test]
        public void EveryGeneTransmitsFromParentsToOffspring()
        {
            // Both parents share a distinctive non-default value in every gene. Any gene that
            // CreateChild forgets to pass will fall back to its constructor default instead,
            // which this test detects. Mutation sigma is 0 so inheritance is exact.
            var parent = new Genome(
                .81f, .82f, .83f, .84f, .85f, .86f, .87f, .88f, .89f, .90f, .91f, .92f,
                .93f, .94f, .95f, .96f, .97f, .98f, .99f, .80f, .79f, .78f, .77f,
                persistence: .76f);

            Genome child = GenomeInheritance.CreateChild(parent, parent, worldSeed: 42, birthOrdinal: 0, mutationStandardDeviation: 0f);

            Assert.That(child.BodySize, Is.EqualTo(.81f).Within(.0001f));
            Assert.That(child.MovementSpeed, Is.EqualTo(.82f).Within(.0001f));
            Assert.That(child.MetabolicPace, Is.EqualTo(.83f).Within(.0001f));
            Assert.That(child.VisionRange, Is.EqualTo(.84f).Within(.0001f));
            Assert.That(child.WaterEfficiency, Is.EqualTo(.85f).Within(.0001f));
            Assert.That(child.FoodEfficiency, Is.EqualTo(.86f).Within(.0001f));
            Assert.That(child.Attack, Is.EqualTo(.87f).Within(.0001f));
            Assert.That(child.Defense, Is.EqualTo(.88f).Within(.0001f));
            Assert.That(child.Maneuverability, Is.EqualTo(.89f).Within(.0001f));
            Assert.That(child.Fear, Is.EqualTo(.90f).Within(.0001f));
            Assert.That(child.Aggression, Is.EqualTo(.91f).Within(.0001f));
            Assert.That(child.DietSpecialization, Is.EqualTo(.92f).Within(.0001f));
            Assert.That(child.MemoryCapacity, Is.EqualTo(.93f).Within(.0001f));
            Assert.That(child.MemoryRetention, Is.EqualTo(.94f).Within(.0001f));
            Assert.That(child.LearningRate, Is.EqualTo(.95f).Within(.0001f));
            Assert.That(child.Exploration, Is.EqualTo(.96f).Within(.0001f));
            Assert.That(child.TemperatureTolerance, Is.EqualTo(.97f).Within(.0001f));
            Assert.That(child.FertilityInvestment, Is.EqualTo(.98f).Within(.0001f));
            Assert.That(child.LifespanTendency, Is.EqualTo(.99f).Within(.0001f));
            Assert.That(child.UrgencyExponent, Is.EqualTo(.80f).Within(.0001f));
            Assert.That(child.TravelSensitivity, Is.EqualTo(.79f).Within(.0001f));
            Assert.That(child.RiskAversion, Is.EqualTo(.78f).Within(.0001f));
            Assert.That(child.Commitment, Is.EqualTo(.77f).Within(.0001f));
            Assert.That(child.Persistence, Is.EqualTo(.76f).Within(.0001f));
        }

        [Test]
        public void PersistenceMutatesAcrossOffspringRatherThanStayingConstant()
        {
            var parent = new Genome(.5f, .5f, .5f, .5f, .5f, .5f, persistence: .5f);

            float first = GenomeInheritance.CreateChild(parent, parent, 42, 0, .03f).Persistence;
            float second = GenomeInheritance.CreateChild(parent, parent, 42, 1, .03f).Persistence;

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first, Is.Not.EqualTo(0f));
        }
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "EveryGeneTransmitsFromParentsToOffspring|PersistenceMutatesAcrossOffspringRatherThanStayingConstant"`
Expected: FAIL — `EveryGeneTransmitsFromParentsToOffspring` fails on the `child.Persistence` assertion (`Expected: 0.76f, But was: 0.0f`); `PersistenceMutatesAcrossOffspringRatherThanStayingConstant` fails because both values are `0f`.

- [ ] **Step 3: Change the constructor default**

In `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs`, line 31, change:

```csharp
            float persistence = 0f)
```

to:

```csharp
            float persistence = 0.5f)
```

- [ ] **Step 4: Add the missing trait to inheritance**

In `Assets/Scripts/Simulation/Biology/GenomeInheritance.cs`, the `CreateChild` return currently ends with the `Commitment` line at traitIndex 22:

```csharp
                InheritTrait(firstParent.Commitment, secondParent.Commitment, worldSeed, birthOrdinal, 22, mutationStandardDeviation));
```

Change it to add the 24th trait at traitIndex 23:

```csharp
                InheritTrait(firstParent.Commitment, secondParent.Commitment, worldSeed, birthOrdinal, 22, mutationStandardDeviation),
                InheritTrait(firstParent.Persistence, secondParent.Persistence, worldSeed, birthOrdinal, 23, mutationStandardDeviation));
```

- [ ] **Step 5: Run the new tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "EveryGeneTransmitsFromParentsToOffspring|PersistenceMutatesAcrossOffspringRatherThanStayingConstant"`
Expected: PASS (2/2)

- [ ] **Step 6: Run the full suite and catalogue every failure**

Run: `cd tools/HeadlessTests && dotnet test`

Expect failures. Every creature's `bodyMass` now includes `+0.05 * 0.5 = +0.025` that it did not before, and `Persistence` now varies, so any test asserting exact energy/health capacities, populations, or state hashes may shift.

For EACH failing test, write down in the report: the test name, the old and new values, and an explicit judgment — **legitimate consequence** (the change is explained by `Persistence` now being `0.5` and heritable) or **genuine regression** (something else broke). Do not edit any expected value until you have written this judgment.

If you find a genuine regression, STOP and report BLOCKED rather than papering over it.

- [ ] **Step 7: Rederive the hash baselines from real runs**

The 10 baselines in `Assets/Tests/EditMode/CoreSimulationTests.cs` (lines 19, 854, 1103, 1125, 1171, 1430, 1453, 1535, 1599, 1623) will change. Nine currently hold `12050501592762519865UL` (same scenario, same value); `ExpectedDecisionStaggerDisabledHash` at line 1103 holds `12400869477994959903UL`.

Do NOT guess these. For each distinct scenario, read the produced value from the actual failing-test output (NUnit prints `But was: <value>`), confirm the scenario configs that previously shared a value still share the new one, and update each constant to its rederived value.

Update each constant's explanatory comment to note that the baseline was rederived when `Persistence` became heritable (defaulting `0.5f`), so the shift is a recorded intentional change rather than an unexplained edit.

- [ ] **Step 8: Run the full suite to confirm green**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/Simulation/Biology/GenomeInheritance.cs Assets/Scripts/Simulation/Biology/GenomePhenotype.cs Assets/Tests/EditMode/
git commit -m "fix: make Persistence heritable and default it to 0.5

GenomeInheritance.CreateChild passed only 23 of the Genome
constructor's 24 parameters, so every offspring silently took the
persistence default. Combined with no code anywhere assigning a
nonzero persistence, every creature in every run had Persistence = 0
permanently - which meant B-4's foraging economics (commitment bonus
and marginal-value abandonment) never ran on a live input despite
being marked resolved.

Persistence now inherits at traitIndex 23, and its constructor default
moves 0f -> 0.5f to match its four sibling behavioural genes and to
avoid the clamp-boundary drift a gene pinned at exactly 0 would show.
Hash baselines rederived from real runs."
```

---

## Self-Review Notes

- **Spec coverage:** Heritability (Step 4) ✅. Non-degenerate default (Step 3) ✅. Structural guard against recurrence (Step 1's `EveryGeneTransmitsFromParentsToOffspring`, which covers all 24 genes, not just this one) ✅. Honest baseline rederivation (Steps 6–7, with an explicit BLOCKED path rather than a green-at-all-costs path) ✅.
- **Placeholder scan:** None. Step 1's test file location is a `grep` command with a deterministic answer, not a TBD.
- **Type consistency:** `InheritTrait`'s signature in Step 4 matches its existing declaration exactly. `traitIndex: 23` is the next unused index, so no existing trait's RNG stream shifts — only new draws are added.
- **Known non-goal:** the observation that 9 of the 10 hash baselines pin the same value from the same scenario (i.e. they are one test repeated under ten names) is real and worth addressing, but it is a separate coverage problem and explicitly out of scope here.

# Session Handoff — 2026-08-22

**Head at handoff: `e6ce068`** (`experiment: reassess safety-gated rendezvous where survival can
actually move`), pushed to `origin/main`.

Two documentation commits sit between that and the previous handoff state (`c197061`): `d40f7ea`
rewrote this file, and the docs commit that follows `7343653` records the fingerprint work below.

Read this first, then `docs/CLAUDE_HANDOFF_2026-08-22.md` for architecture, scientific context,
testing rules and user preferences. `docs/ROADMAP.md` is the backlog. `docs/superpowers/plans/` is
an archive, not a backlog.

---

## 1. What was completed this session

Twenty commits, `f0a691d` through `c197061`. Three phases.

### Phase A — P4a feature work

| commit | what |
|---|---|
| `f0a691d` | key `R` home-range playtest; P5 panel hides routine continuity rows |
| `a817ccb` | home-range measured null for route formation |
| `173f3a3` | `ObservationRouteRing` scenario; home-range **closed as a measured negative** |
| `c528ced` | `ObservationShiftingPatches` scenario; map-turnover measurement |
| `6d64df0` | key `V` shifting-patches playtest at world seed 45 |
| `0b0387c` | founder mortality diagnosis: separation **sterilises**, does not kill |
| `15c7a5a` | inspector shows *why* a creature cannot breed; "ready to breed" count |

### Phase B — evidence-integrity audit (triggered by an external code review)

| commit | what |
|---|---|
| `4cc9a47` | **fix:** `PlantPatchStore.ReplaceAt` did not reset plant age |
| `9763374` | **fix:** statistics sampled before deaths committed; `CaptureStatistics()` added |
| `c97efe9` | paired old-vs-fixed blast-radius audit |
| `0fbb7f8` | `CaptureStatistics` guarded to a settled step boundary; corrected an overclaim |
| `1eb801c` | affected-evidence ledger widened to 9 docs; 168-site replication committed |
| `3f3b77c` | plant lifetime accounting **survives** the fix |
| `c19c1a5` | plant corpus revalidated on fixed code |
| `8f55b6e` | low-occupancy replication calibrated (occupancy is a cliff) |
| `06d80a8` | low-occupancy conclusions are **unverifiable** |
| `bbd7a76` | grazing deficit quantified |

### Phase C — P1 queue (all four items complete)

| commit | what |
|---|---|
| `c751a13` | finite/range validation at config and scenario boundaries |
| `b8a61a7` | mandatory experiment manifest + scenario layout fingerprint |
| `d3fac12` | P5 clustering allocation made linear in population |
| `c197061` | resource allocation benchmarked and **deliberately not optimised** |

### Phase D — state fingerprint V2 (the item the last handoff queued)

| commit | what |
|---|---|
| `7343653` | `ComputeStateFingerprint` (V2) + `SimulationConfig.ComputeConfigurationHash`; `BehaviorHash` extended with plant age/cooldown after measuring it changes no verdict |
| `32900de` | selected-creature action history — an outside observer, testable headlessly |
| `e6ce068` | safety-gated rendezvous reassessed at an operating point with survival headroom |

---

## 2. Verified numeric results — do not re-derive these

### Home range (CLOSED — do not reopen, do not tune)

- `ObservationRouteRing` gives **90.6%** of creature-ticks a genuine equidistant choice at **0.88**
  mean familiarity, with an unsaturated off-arm route metric of **0.7955**.
- Flag on: route repeatability **fell −0.0345 (t −2.87, 8/30 up)**; same-site clinging **rose
  +0.0594 (t +4.93, 26/30 up)**.
- In shipped scenarios the route metric is saturated at **1.0000** flag-off; delta **+0.0000**, and
  **+0.0001** at a 10x bonus. The 10x arm cost **2.7%** food intake for no births.

### Shifting patches (`V`, world seed 45)

- ~**29** patch deaths and ~**33** establishments per 6,000-tick run; equilibrium **11.96** active
  food sites.
- Route permanence **−0.0935 (t −6.47)**; distinct routes per creature **+0.628 (t +4.48, 22/30)**;
  cross-kind legs unchanged (445 vs 441). **No survival cost.**
- Honest extinction rate **6/30**. Seed 42 dies; seed 45 chosen from the 24/30 that establish.

### The reproduction gate (DECIDED — keep as-is)

- `ReproductionSystem.CanReproduce` needs energy AND hydration AND health each **≥70%** of capacity.
- Adults satisfy it **95.0%** of adult-ticks with co-located resources, **56.8%** on the route ring,
  **33.5%** when food sits 7 units from water.
- Marginals collapse (energy above 0.7: 95.1% → 46.3%) **plus** a simultaneity penalty of **8.6–12.8
  points**.
- **Nothing starves or dehydrates**: 0 dehydration deaths, 0.07 starvations per run; all four
  founders die of **age** at tick ~2500 in every arm. Minimum hydration reached averages **0.445**.

### Plant lifetime accounting (CONFIRMED on fixed code)

Pre-fix → post-fix, same probe, version-independent detector:

- takeover fraction **0.3409 → 0.3471** (recorded 34%)
- median takeover lifetime **1.95 s → 1.95 s** (recorded ~2 s)
- R²(takeover, offspring) **0.5013 → 0.5164** (recorded 51.9%)
- R²(realised lifespan, offspring) among patches that **died of age**: **0.0039** (recorded 0.024 —
  same claim). Pooled with right-censored survivors it reads 0.14; **that is an artefact.**

### Plant corpus revalidation (120 seeds, varying founders, fixed code)

- `Dispersal` **+0.1119, t +15.63, 110/120** (recorded t +14→+19.6, 105–119/120) — confirmed
- `SeedInvestment` **+0.0872, t +7.10, 91/120** (recorded t +4.8→+6.8) — confirmed
- Establishment contest, paired on−off: **+0.0362, t +3.22, 72/120** (recorded t +4.03, 76/120) —
  **replicates**
- `SeedProductionRate` at 24 sites: **t −2.80, 43/120 up** — null, as recorded
- Survival: **0/120 extinct, 0/120 frozen** in all six combinations

### Occupancy is a cliff in target spacing

| spacing | occupancy | extinct |
|---|---|---|
| 4 | 0.833 | 0/10 |
| 8 | 0.528 | 0/10 |
| **9.5** | **0.311** | **0/10** |
| 11 | 0.085 | 3/10 |
| 13.3 | 0.023 | 9/10 |

`DispersalRange = 4 + 20 × Dispersal` and Dispersal evolves upward, so a mature patch throws seeds
14–24 units; any tighter lattice saturates. Viable window ≈ spacing 9.3–9.7, ~4% of the swept range.

### P1 measurements

- Genetic distance: **240 bytes/pair** before → 187 KB / 4.8 MB / **120 MB and 126 ms** at 40 / 200 /
  1,000 creatures. After: 4.3 KB / 21 KB / **104 KB and 50 ms**. **1,151x** less, **2.5x** faster.
- Resource allocation: cost is **O(requests × distinct resources)**, not O(requests²). 1,000 requests
  on 1 resource = **16.9 µs**; on 24 resources = **165 µs**. Full 12,000-tick runs: **0.012 /
  0.090 / 0.227 ms per tick** at peak populations 38 / 48 / 523.

---

### State fingerprint V2 (DONE — `7343653`)

Three hashes, three jobs, and they must stay separate:

| | job | includes configuration? |
|---|---|---|
| `ComputeStateHash` (V1) | frozen historical identifier; tests pin its literals | only `WorldSeed` |
| `ComputeStateFingerprint` (V2) | "will these two worlds evolve identically from here?" | **yes, all of it** |
| `ComputeBehaviorHash` | did this gene/flag reach behavior? | **no, and never** |

V2 adds, over V1: the config hash, `_birthOrdinal`, `_plantSeedOrdinal`, the three store id
counters, `PlantSiteRegistry` contents and order, plant `Age` and `ReproductionCooldownRemaining`,
and home-range state **unconditionally** rather than behind its flag. Guarded to a settled step
boundary, like `CaptureStatistics`. Excluded on purpose: reporting accumulators, liveness counters,
derived caches.

`BehaviorHash` also gained plant `Age` and `ReproductionCooldownRemaining` — decided by measurement,
not argument. Prediction stated first (the inert set would not move, since all four inert flags are
inert for a *reachability* reason); measured with the lines in and out: **identical inert set,
identical plant gene verdicts, 33 / 19 / 1 either way**. No `BehaviorHash` value is pinned as a
literal anywhere, so extending it invalidated no baseline.

Config hash covers **44 of 46** public `SimulationConfig` properties; `FixedDeltaTime` and
`MaximumMemorySlots` are derived from inputs already hashed. Two drift guards: every `bool`
constructor parameter must move the hash, and the property count is pinned.

**Green: 489 / 19 / 33 / 1**, up from 480 / 19 / 33 / 1. The three liveness counts being unchanged
*was* the acceptance criterion.

---

### Selected-creature history (DONE — `32900de`)

`CreatureActionHistory` records, for one creature at a time, a bounded list of action episodes plus
a lifetime budget of ticks per action. Each episode carries the needs it started and finished on,
which is the whole point: a `SeekFood` episode ending with **less** energy than it started is a
failed trip, and that is invisible from an instantaneous inspector reading.

**It lives outside `SimulationWorld` on purpose.** It samples the world; the world never reads it.
So it adds no simulation state, appears in no hash, and cannot change a tick. A per-creature history
held *inside* the world would be future-determining state by the letter of the fingerprint design
and would need re-arguing every time a fingerprint changed. Not config-flag-gated either — a
diagnostics flag has to be behavior-inert to be correct, and `FlagLivenessAnalysis` would then
report it inert and fail the known-inert-flag assertion. Same reasoning as `SimulationWorld.Liveness`.

Ten tests. The load-bearing one: an observed world and an unobserved world have **identical V2
fingerprints** after 400 ticks — the first real use of the fingerprint from `7343653`. Both that
test and the determinism test assert the observer actually recorded something, and a third asserts
the run produced more than one kind of episode, since a single unbroken `Wander` would satisfy
determinism while showing the player nothing.

Sampled once per simulated step, not per frame, so resolution is independent of frame rate and of
the speed multiplier. Drawn in its own panel at (464, 300) rather than lengthening the inspector,
which is already at full height with all optional trait rows showing.

---

### Safety-gated rendezvous (CLOSED — "works, buys nothing")

The 2026-08-21 null was partly unmeasurable: **all 240 of its runs ended at exactly 48**, the
population cap, zero variance. Its birth null stands; its survival null was a ceiling.

**The population cap is load-bearing ecology, not a guard rail.** Extinct 0/8 at cap 72, **5/8 at
84**, 8/8 at 96 and above, where runs boom to ~293 births and collapse on starvation. Cap 84 is the
only point where survival is free to move, so the rerun used it.

Re-measured, 120 paired seeds, cap 84 (`p4a-rendezvous-headroom-2026-08-22.md`):

| | delta | t | sign |
|---|---:|---:|---|
| flee rate per creature-tick | −0.0285 | **−5.07** | 80/120 down |
| **predation deaths** | **−2.275** | **−4.64** | 70/120 down |
| births, raw | +12.81 | +2.04 | 72/120 up |
| births per creature-tick | +0.00001 | +1.24 | not significant |
| births, both-survived seeds (n=28) | +11.71 | +1.01 | not significant |
| starvation deaths | +1.15 | +0.85 | null |

Extinction 75/120 vs 66/120 **does not survive pairing**: discordant 26 vs 17, McNemar χ² 1.49.
The raw birth gain is **exposure, not fertility**.

**Verdict: the mechanism works and the ecology declines to reward it.** Starvation, not predation,
limits this population. Flag stays default `false`. Do not build pack architecture to force an
effect; do not tune the gate. Reopen only in a predation-limited habitat — a scenario question, not
a mechanism question. **This is not the home-range case**: home range was closed for the wrong sign,
this for a right-signed effect that reaches no outcome that matters.

**Provenance:** the 2026-08-21 configuration could not be recovered — 81 candidates tried against its
recorded state hash and births, none matched. The rerun is a **new condition**, not a rerun, and its
CSV carries an `ExperimentManifest`.

---

## 3. Unresolved findings

### The three low-occupancy plant conclusions are UNVERIFIABLE

`p4-site-abundance-seed-production-rate-2026-08-20.md`,
`p4-low-occupancy-plant-route-audit-2026-08-20.md`,
`p4-low-occupancy-growth-trait-reaudit-2026-08-20.md` — banners stay.

Their scenario was never committed and cannot be recovered (no ZZZ probe was ever committed, so
none was ever deleted; the CSV and writeups give count, config, seeds, ticks and occupancy but never
coordinates). The calibrated replication reproduces occupancy **0.311** but grazes at
**0.00261 vs 0.00699 — a ratio of 0.373** — because its free-site pool sits outside the ±25 creature
arena and is never grazed. Placing 162 targets at non-saturating spacing *inside* the arena is
geometrically impossible.

Measured at that condition, for the record: `SeedProductionRate` **+0.00424, t +0.72, 64/120** (does
not replicate); `SeedlingResilience` contest-on−off **−0.00248, t −0.34, 53/120** (reversal not
demonstrated, but the +0.0362 advantage seen at 24 sites is **abolished**); the six growth-rate nulls
**hold**. `PlantEstablishmentContestEnabled` costs **19/120 extinctions** at low occupancy against
4/120 base.

**Do not attempt a fourth reconstruction.** If free-site abundance matters, re-derive it as a NEW
experiment with a committed scenario, in a geometry that fits inside the grazed arena.

### Lifespan-headroom claim: not adjudicated, control was confounded

`mortality-off` gives lifespan no channel but also removes site turnover and rewrites the regime:
the same comparison moved `Dispersal` **+0.0834 (t +21.40, 118/120)**, `NutrientUptake` **−0.0466
(t −7.62)**, `WaterEfficiency` **−0.0445 (t −8.61)**. Needs a lifespan-specific control that does
not exist.

### Unverified by me

The breeding-readiness inspector UI (`15c7a5a`) compiles and passes tests but was **never seen in
Play mode**. Layout at 324px with all optional trait rows showing is untested. The same applies to
the selected-creature history panel (`32900de`): its *model* is covered by ten headless tests, but
the panel itself has never been seen rendered. It was placed at (464, 300) in free space beside the
population-condition box specifically to avoid stacking more onto the untested inspector.

### Not measured

Per-tick resource request counts were never instrumented. The do-not-optimise decision uses
population as an upper bound — sound for that decision, but it is a bound, not an attribution.

---

## 4. Decisions that must NOT be reopened

1. **Soft home-range affinity is closed as a measured negative.** Flag stays default `false`; code,
   tests and key `R` stay; spec and plan carry SUPERSEDED banners. Do not tune
   `DefaultHomeRangeBonusMaximum`, the falloff distance or the learning fraction — the **sign** of
   the effect is wrong, not its size.
2. **The joint 70%/70% reproduction gate stays** (user decision). Reduced fertility while commuting
   is accepted as real ecology. Do not change `ReproductionSystem.CanReproduce`. No re-baseline is
   needed and every result on record stands. Separated scenarios must be calibrated to be viable
   *under* the gate.
3. **Resource allocation is not to be optimised** at current scales. Revisit only if populations in
   the thousands and site counts in the hundreds coincide.
4. **`ObservationShiftingPatches` needs no further placement or productivity calibration** — six
   variants are recorded and the joint gate explains why all failed.
5. **Do not build the P4a juvenile local-area bias as a fix for separated-resource extinction.**
   Juveniles are not the failing class and mortality is not the failure mode.
6. **Place memory stays inert.** Never wire `MemorySystem.ObservePlace`.
7. **Do not use the competition-off arm as a drift control.** It disables no trait.
8. **Safety-gated rendezvous is closed as "works, buys nothing."** Flag stays default `false`. Its
   effect is real and correctly signed; the ecology is starvation-limited, so it does not propagate.
   No pack architecture, no tuning. Reopen only in a predation-limited habitat.
9. **The three hashes stay three hashes.** V1 stays frozen and incomplete; V2 carries configuration;
   `BehaviorHash` never carries configuration. Merging any two of them breaks either a recorded
   baseline or `FlagLivenessAnalysis`, which would then report every flag as live.

---

## 5. Next task

1. **Audit other cap-pinned conclusions.** The rendezvous survival null was a ceiling artefact.
   Any recorded result whose outcome variable sat against a clamp deserves the same check — look for
   arms reporting an identical final population across every seed.
2. **Finish the P4a visible-feedback item**: resource depletion/recovery feedback and lineage
   display are the two parts still open.
3. **Then P5** (species and history), which is the large remaining phase before P6 terrain.
4. Treat dense-index scheduling, stale grids, defense projection and Legacy predation as measured or
   design questions, not automatic fixes.

**Use `ComputeStateFingerprint()` for "do these two worlds evolve identically" questions.** Never
`ComputeStateHash` — V1 is a frozen historical identifier and is deliberately incomplete. Never
recompute or overwrite a recorded V1 value.

**Use `ExperimentManifest` + `ExperimentCsv` for every new experiment CSV.** `ExperimentCsv.Compose`
refuses without provenance; that is deliberate.

---

## 6. Test commands

From `tools/HeadlessTests`:

```powershell
dotnet build
dotnet test --no-build --filter "FullyQualifiedName!~LivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~PlantLivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~LivenessTests&FullyQualifiedName!~RiskAversionIsLiveOnlyWhenThreatsExist"
dotnet test --no-build --filter "FullyQualifiedName~RiskAversionIsLiveOnlyWhenThreatsExist"
```

**Green at handoff: 499 / 19 / 33 / 1.** RiskAversion alone takes ~16 s; silence is not a hang.

Presentation changes additionally need a Unity compile — the headless project excludes
`Assets/Scripts/Presentation`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -logFile '.\Logs\compile.log'
```

Then check `grep -c "error CS"` on the log and confirm `Exiting batchmode successfully`.

---

## 7. Play-mode keys

`Space` pause · `1`/`2`/`4`/`8` speed · `H` overlay · left-click select, drag resources.

Scenarios: **`V` shifting patches (best — the map changes as you watch)** · `5` stable · `6` scarcity
· `7` migration · `9` mating · `E` starter habitat · `R` home range (looks identical to `5` — that
is the measured result, not a bug) · `N` all-flags playtest · `B`/`D`/`F`/`P`/`C`/`T`/`G`/`M` older
demos.

---

## 8. Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. **Never stage or delete them. Never `git add -A`** —
add named files only. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing; none exist at
handoff.

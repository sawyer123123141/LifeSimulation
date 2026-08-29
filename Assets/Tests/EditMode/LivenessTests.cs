using System.Collections.Generic;
using System.Linq;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Turns the live/dead mechanism ledger in docs/AGENT_FIELD_NOTES.md §4 from documentation
    /// into something the build enforces.
    ///
    /// The project has twice built work on a wrong belief about what runs: place memory was blamed
    /// for extinctions while never executing, and Persistence was reported as behaviorally inert
    /// under P4 when in fact it feeds body mass. Both slipped through a caller-search. These tests
    /// use perturbation instead, so they cannot go stale silently — wiring a dead gene or breaking
    /// a live one fails the build.
    /// </summary>
    public sealed class LivenessTests
    {
        private const int LivenessTicks = 3000;

        private static SimulationConfig FullEcosystem() =>
            SimulationConfig.CreateFullEcosystemDefaults(42, 12);

        private static GeneLivenessResult[] AnalyzeFullEcosystem() =>
            GeneLivenessAnalysis.Analyze(
                FullEcosystem,
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                LivenessTicks);

        // ---- Genome integrity -------------------------------------------------------------

        [Test]
        public void EveryTraitSurvivesTheTraitArrayRoundTrip()
        {
            // Pairwise-distinct values so a duplicated or dropped index cannot pass by coincidence.
            float[] traits = Enumerable.Range(0, Genome.TraitCount)
                .Select(index => (index + 1) / (float)(Genome.TraitCount + 1))
                .ToArray();

            float[] roundTripped = Genome.FromTraits(traits).ToTraits();

            Assert.That(roundTripped, Is.EqualTo(traits));
        }

        [Test]
        public void TraitNamesAreDistinctAndCoverEveryTrait()
        {
            var names = Enumerable.Range(0, Genome.TraitCount).Select(Genome.TraitName).ToArray();

            Assert.That(names.Distinct().Count(), Is.EqualTo(Genome.TraitCount));
            Assert.That(names, Has.None.Null.Or.Empty);
        }

        [Test]
        public void WithTraitChangesOnlyTheNamedTrait()
        {
            Genome original = Genome.FromTraits(
                Enumerable.Repeat(0.5f, Genome.TraitCount).ToArray());

            for (int traitIndex = 0; traitIndex < Genome.TraitCount; traitIndex++)
            {
                float[] changed = original.WithTrait(traitIndex, 0.25f).ToTraits();
                for (int other = 0; other < Genome.TraitCount; other++)
                {
                    float expected = other == traitIndex ? 0.25f : 0.5f;
                    Assert.That(changed[other], Is.EqualTo(expected),
                        $"WithTrait({traitIndex}) altered {Genome.TraitName(other)}");
                }
            }
        }

        [Test]
        public void EveryTraitTransmitsThroughInheritance()
        {
            // The positional-argument bug: CreateChild once passed 23 of 24 parameters, so
            // Persistence silently took its default for every creature ever born.
            Genome firstParent = Genome.FromTraits(
                Enumerable.Range(0, Genome.TraitCount)
                    .Select(index => (index + 1) / (float)(Genome.TraitCount + 1))
                    .ToArray());
            Genome secondParent = firstParent;

            Genome child = GenomeInheritance.CreateChild(
                firstParent, secondParent, worldSeed: 42, birthOrdinal: 1, mutationStandardDeviation: 0f);

            for (int traitIndex = 0; traitIndex < Genome.TraitCount; traitIndex++)
            {
                Assert.That(child.GetTrait(traitIndex), Is.EqualTo(firstParent.GetTrait(traitIndex)).Within(1e-5f),
                    $"{Genome.TraitName(traitIndex)} did not transmit from identical parents");
            }
        }

        // ---- Behavior hash validity -------------------------------------------------------

        [Test]
        public void BehaviorHashIgnoresGenomeWhileStateHashDoesNot()
        {
            // The whole perturbation method rests on this asymmetry. If the behavior hash ever
            // starts including genome fields, every liveness verdict below becomes meaningless.
            SimulationConfig config = FullEcosystem();
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            ulong behaviorBefore = world.ComputeBehaviorHash();
            ulong stateBefore = world.ComputeStateHash();

            // NeutralMarker is inert, so overwriting it must move the state hash and nothing else.
            world.OverwriteTraitForAllCreatures(22, 0.123f);

            Assert.That(world.ComputeBehaviorHash(), Is.EqualTo(behaviorBefore));
            Assert.That(world.ComputeStateHash(), Is.Not.EqualTo(stateBefore));
        }

        [Test]
        public void LivenessRecorderCountersDoNotReachTheStateHash()
        {
            SimulationConfig config = FullEcosystem();
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int step = 0; step < 200; step++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            ulong stateHash = world.ComputeStateHash();
            ulong behaviorHash = world.ComputeBehaviorHash();

            var recorder = new LivenessRecorder();
            recorder.RecordReached(LivenessProbe.PlaceMemoryObservation);
            recorder.RecordOutput(LivenessProbe.CommitmentBonus, 1f, 0f);
            recorder.RecordOutcome(LivenessProbe.ShouldAbandon, true);

            Assert.That(world.ComputeStateHash(), Is.EqualTo(stateHash));
            Assert.That(world.ComputeBehaviorHash(), Is.EqualTo(behaviorHash));
        }

        [Test]
        public void LivenessRecorderSeparatesInertExecutionFromRealEffect()
        {
            var recorder = new LivenessRecorder();

            recorder.RecordOutput(LivenessProbe.PlaceMemoryScoring, produced: 0f, noOpValue: 0f);
            recorder.RecordOutput(LivenessProbe.PlaceMemoryScoring, produced: 0f, noOpValue: 0f);
            recorder.RecordOutput(LivenessProbe.ThreatAvoidance, produced: 0.4f, noOpValue: 0f);

            Assert.That(recorder.IsUnreached(LivenessProbe.PlaceMemoryDecay), Is.True);
            Assert.That(recorder.IsInertlyExecuting(LivenessProbe.PlaceMemoryScoring), Is.True);
            Assert.That(recorder.IsLive(LivenessProbe.PlaceMemoryScoring), Is.False);
            Assert.That(recorder.IsLive(LivenessProbe.ThreatAvoidance), Is.True);
        }

        // ---- The ledger itself ------------------------------------------------------------

        [Test]
        public void NeutralMarkerReachesNoBehaviorUnderTheWidestConfiguration()
        {
            // Ledger entry: Genome.NeutralMarker is inherited, mutated, hashed and reported as an
            // ExperimentMetric while no behavior system reads it. It is retained deliberately as a
            // drift-control channel: the 2026-08-17 coevolution run used its inertness to show the
            // bootstrap pipeline does not manufacture false positives. That argument only holds
            // while it stays inert, which is what this test pins.
            //
            // If this fails, someone wired NeutralMarker to behavior. That invalidates its use as a
            // placebo in every paired experiment, and those results must be re-read.
            GeneLivenessResult neutralMarker = AnalyzeFullEcosystem()[22];

            Assert.That(neutralMarker.TraitName, Is.EqualTo(nameof(Genome.NeutralMarker)));
            Assert.That(neutralMarker.ReachesBehavior, Is.False,
                "NeutralMarker is documented as the inert drift-control channel but now reaches behavior.");
        }

        [Test]
        public void EveryTraitExceptCommitmentReachesBehaviourUnderTheWidestConfiguration()
        {
            // Under narrower configurations this is not true, and that is the point: RiskAversion
            // reads dead against CreatePrototype4Defaults because the herbivore calibration never
            // produces a threat for its three call sites to fire on. A "does not reach behavior"
            // verdict is always scoped to the scenario it was measured in, so the ledger is pinned
            // against the widest surface available.
            GeneLivenessResult[] results = AnalyzeFullEcosystem();

            List<string> dead = results
                .Where(result => !result.ReachesBehavior)
                .Select(result => result.TraitName)
                .ToList();

            Assert.That(dead, Is.EqualTo(new[] { nameof(Genome.NeutralMarker) }),
                $"Gene liveness changed.\n{GeneLivenessAnalysis.Report(results)}");
        }

        // ---- Class B: executes, but always on empty data --------------------------------------

        [Test]
        public void PlaceMemoryProbesRunButNeverTakeEffect()
        {
            // Enforces the §4 "executes but always on empty data" entry, which neither perturbation
            // harness can reach: there is no gene and no flag to flip for place memory, so its
            // deadness was documented and nothing checked it. Place memory reading as live is what
            // produced the retracted root cause in
            // docs/experiments/p4-memory-root-cause-retracted-2026-08-17.md.
            //
            // If either probe reports live, someone wired ObservePlace. That is a real behavior
            // change and every baseline measured before it is suspect.
            SimulationConfig config = FullEcosystem();
            var world = new SimulationWorld(config) { Liveness = new LivenessRecorder() };
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            for (int step = 0; step < 4000; step++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            Assert.That(world.Liveness.IsLive(LivenessProbe.PlaceMemoryScoring), Is.False,
                $"place-memory scoring produced a result, so slots are now populated.\n{world.Liveness.Report()}");
            Assert.That(world.Liveness.IsLive(LivenessProbe.FailedPlaceSearch), Is.False,
                $"failed-place-search altered confidence, so slots are now populated.\n{world.Liveness.Report()}");
        }

        [Test]
        public void DeterrenceProbeIsLiveSoTheRecorderIsNotSilentlyBroken()
        {
            // Control for the test above. A recorder that only ever reports INERT is
            // indistinguishable from one whose probes never fire at all, so at least one probe must
            // demonstrably register a real effect.
            SimulationConfig config = FullEcosystem();
            var world = new SimulationWorld(config) { Liveness = new LivenessRecorder() };
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            for (int step = 0; step < 4000; step++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            Assert.That(world.Liveness.IsLive(LivenessProbe.PlantDefenseDeterrence), Is.True,
                $"deterrence probe never registered an effect; the recorder may not be wired.\n{world.Liveness.Report()}");
        }

        [Test]
        public void AttachingARecorderDoesNotChangeTheSimulation()
        {
            // The recorder observes; it must never perturb. Same seed, same scenario, one world
            // instrumented and one not — both hashes must match at the end.
            SimulationConfig bare = FullEcosystem();
            var without = new SimulationWorld(bare);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(without);

            SimulationConfig instrumentedConfig = FullEcosystem();
            var with = new SimulationWorld(instrumentedConfig) { Liveness = new LivenessRecorder() };
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(with);

            for (int step = 0; step < 1500; step++)
            {
                without.Step(bare.FixedDeltaTime);
                with.Step(instrumentedConfig.FixedDeltaTime);
                without.Events.Clear();
                with.Events.Clear();
            }

            Assert.That(with.ComputeStateHash(), Is.EqualTo(without.ComputeStateHash()));
            Assert.That(with.ComputeBehaviorHash(), Is.EqualTo(without.ComputeBehaviorHash()));
        }

        // ---- Config flags -------------------------------------------------------------------

        /// <summary>
        /// Flags that have a production reader but whose reader sits on a path
        /// <c>IntentUtilityV1</c> never takes. Flipping any of them produces bit-identical runs.
        ///
        /// This list is not a wish list — it is a measurement. The 2026-08-17 audit cleared all of
        /// these on the grounds that each had "at least one production reader", which is true and
        /// insufficient. Turning one of them on is currently a no-op, so do not reach for one
        /// expecting an effect, and do not "verify" one by grepping for its name.
        /// </summary>
        private static readonly string[] KnownInertFlags =
        {
            "evasiveFleeingEnabled",
            "foragingEconomicsEnabled",
            "kinRecognitionEnabled",
            "learnedResourceQualityEnabled",
            "metabolicHealingEnabled",
            "multiThreatPerceptionEnabled",

            // plantTemperatureAdaptationEnabled was listed here between a4304bd and the procedural
            // environment fields landing. It was inert for a different reason than the four above:
            // fully wired on the live path, but EnvironmentField returned Temperature = 1 everywhere,
            // and at 1 the adaptation expression collapses to the raw value. Once the fields varied
            // it diverged at tick 40 and this test failed - which was the designed signal - so it was
            // removed. The four that remain are inert because their readers sit on the Legacy path,
            // which no configuration reaches; those will not resolve themselves.
            //
            // evasiveFleeingEnabled is inert here for the SAME benign reason as metabolicHealingEnabled
            // below, one step further out: it only fires when a defender that is actively fleeing is
            // struck, and this configuration seeds PhysiologyVariation herbivores, so no creature
            // ever attacks another and combat resolution is never reached at all. The harness reports
            // it "never" reached, not "reached and made no difference". It becomes live the moment
            // the founders hunt, which EvasiveFleeingTests pins from both directions on
            // PredationVariation - byte-identical off, divergent on. If it ever reports live *here*,
            // herbivores have started attacking each other.
            //
            // metabolicHealingEnabled is inert here for a THIRD reason, and a benign one: it scales
            // health recovery by metabolic pace, and this configuration has healthRecoveryEnabled
            // off, so there is no healing to scale. Same shape as slopeMovementCostEnabled needing
            // elevation. It becomes live the moment recovery is on, which HealthRecoveryTests pins
            // from both directions - inert without healing, live with it. If it ever reports live
            // *here*, something has started healing creatures without being asked to.
        };

        [Test]
        public void InertFlagsAreExactlyTheKnownSetUnderTheWidestConfiguration()
        {
            // FULL ecosystem mode has all sixteen flags on, so an "inert" verdict here cannot be
            // blamed on the flag simply having no occasion to fire.
            FlagLivenessResult[] results = FlagLivenessAnalysis.Analyze(
                FullEcosystem,
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                LivenessTicks);

            string[] inert = results
                .Where(result => !result.ChangesBehavior)
                .Select(result => result.FlagName)
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(inert, Is.EqualTo(KnownInertFlags),
                "Config flag liveness changed. If a flag became live, that is a real behavior "
                + "change and every baseline measured before it is suspect.\n"
                + FlagLivenessAnalysis.Report(results));
        }

        [Test]
        public void EveryConfigFlagIsCoveredByTheLivenessSweep()
        {
            // Guards the reflection: if the constructor convention changes and bool parameters stop
            // being discovered, the test above would silently pass on an empty sweep.
            FlagLivenessResult[] results = FlagLivenessAnalysis.Analyze(
                FullEcosystem,
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                ticks: 5);

            int flagCount = typeof(SimulationConfig)
                .GetConstructors()
                .OrderByDescending(candidate => candidate.GetParameters().Length)
                .First()
                .GetParameters()
                .Count(parameter => parameter.ParameterType == typeof(bool));

            Assert.That(flagCount, Is.GreaterThan(0));
            Assert.That(results.Length, Is.EqualTo(flagCount));
        }

        [Test]
        public void RiskAversionIsLiveOnlyWhenThreatsExist()
        {
            // Pins the scenario-scoping caveat itself, so that a future reader cannot cite the
            // narrow-configuration result as evidence that RiskAversion is dead code.
            GeneLivenessResult narrow = GeneLivenessAnalysis.Analyze(
                () => SimulationConfig.CreatePrototype4Defaults(42, 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                LivenessTicks)[21];

            GeneLivenessResult wide = AnalyzeFullEcosystem()[21];

            Assert.That(narrow.TraitName, Is.EqualTo(nameof(Genome.RiskAversion)));
            Assert.That(narrow.ReachesBehavior, Is.False, "expected no threats under the P4 herbivore calibration");
            Assert.That(wide.ReachesBehavior, Is.True, "expected FULL ecosystem mode to produce threats");
        }
    }
}

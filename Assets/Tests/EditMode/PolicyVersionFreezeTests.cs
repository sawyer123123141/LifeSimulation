using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// P0 of the V2 seam (docs/superpowers/specs/2026-09-11-v2-ruleset-seam-design.md §6.1):
    /// literal pins of the live <c>IntentUtilityV1</c> path, captured at commit 65bae69.
    ///
    /// <para><b>Four configurations are pinned, not the 98 construction sites.</b> A, the P4
    /// baseline every one-flag arm varies against; B, the widest surface the inert-flag set is
    /// pinned on; C, the representative recorded predation configuration; D, the C3 control
    /// whose value reproduces a committed artefact rather than a fresh capture. Legacy is pinned
    /// separately in <c>CoreSimulationTests</c> at 50 ticks.</para>
    ///
    /// <para>Pins C and D are hand replicas of <c>tools/CreatureSweep/Program.cs</c>
    /// <c>CreateConfig(seed, slope: false)</c> as of 65bae69, cross-checked once at capture
    /// time against the real tool at the same horizon: the emitted seed, the emitted behaviour
    /// hash, and the emitted manifest normalised by removing exactly <c>code_revision</c> and
    /// <c>SlopeMovementCostEnabled</c>. A later change to the tool silently desynchronises the
    /// replica; this file does not track it.</para>
    ///
    /// <para>The <c>internal</c> members exist for the P1 seam tests, which must not
    /// re-replicate anything pinned here. Nothing else is shared.</para>
    /// </summary>
    public sealed class PolicyVersionFreezeTests
    {
        internal const int FreezeSeed = 42;
        internal const int PinCTicks = 2000;            // C_HORIZON from the capture
        internal const int PinCSeedCount = 1;
        internal const string PinCRevision = "p0-pin-c";
        private const int FreezeFounders = 12;
        private const int PinDTicks = 2666;             // sample 1 of a 24,000-tick, 9-sample trajectory
        private const int DefaultTicks = 2000;

        // --- captured at 65bae69 by the transient probe (plan Task 2); A/B/C cross-checked ---
        private const ulong CapturedAState = 17811950795457374630UL;
        private const ulong CapturedABehavior = 4903546012323466459UL;
        private const ulong CapturedAFingerprint = 11456860755430468762UL;
        private const ulong CapturedAConfiguration = 9998238842416875608UL;
        private const ulong CapturedBState = 363660116393324261UL;
        private const ulong CapturedBBehavior = 1444606364382886198UL;
        private const ulong CapturedBFingerprint = 5945803090718552361UL;
        private const ulong CapturedBConfiguration = 4960884631795113949UL;
        private const ulong CapturedCState = 4695850742587819928UL;
        private const ulong CapturedCBehavior = 4733920025962675787UL;
        private const ulong CapturedCFingerprint = 9852847996031763401UL;
        private const ulong CapturedCConfiguration = 9676919953325805535UL;
        private const ulong CapturedDState = 117900750459664081UL;
        private const ulong CapturedDFingerprint = 18230787517724414463UL;
        private const ulong CapturedDConfiguration = 16323614469258713575UL;
        internal const string PinCCanonicalManifest =
            "schema=1\n"
            + "code_revision=p0-pin-c\n"
            + "scenario_id=p6-defense-calibration-regen2.00\n"
            + "scenario_layout_fingerprint=13576515063262381010\n"
            + "scenario_resource_count=30\n"
            + "first_seed=42\n"
            + "seed_count=1\n"
            + "ticks=2000\n"
            + "WorldSeed=42\n"
            + "InitialPopulation=12\n"
            + "MaximumPopulation=500\n"
            + "FounderProfile=PredationVariation\n"
            + "DecisionPolicyVersion=IntentUtilityV1\n"
            + "BaseFrequencyHz=20\n"
            + "CognitionEnabled=true\n"
            + "PhysiologyEnabled=true\n"
            + "PlantCohortsEnabled=true\n"
            + "PredationEconomicsEnabled=true\n"
            + "ForagingEconomicsEnabled=true\n"
            + "DecisionStaggerEnabled=true\n"
            + "MultiThreatPerceptionEnabled=true\n"
            + "RestBehaviorEnabled=true\n"
            + "JuvenileCapabilityEnabled=true\n"
            + "ParentalFollowingEnabled=true\n"
            + "KinRecognitionEnabled=true\n"
            + "LearnedResourceQualityEnabled=true\n"
            + "MateSelectionEnabled=false\n"
            + "PlantSiteCompetitionEnabled=true\n"
            + "PlantMortalityEnabled=true\n"
            + "PlantDefenseDeterrenceEnabled=true\n"
            + "PlantQualityPreferenceEnabled=true\n"
            + "PlantTemperatureAdaptationEnabled=true\n"
            + "PlantFertilityAdaptationEnabled=true\n"
            + "ProceduralEnvironmentFieldsEnabled=true\n"
            + "ElevationFieldEnabled=true\n"
            + "TerrainDrivenEnvironmentEnabled=true\n"
            + "TerrainDrivenTemperatureEnabled=false\n"
            + "MetabolicIngestionEnabled=false\n"
            + "HealthRecoveryEnabled=false\n"
            + "MetabolicHealingEnabled=false\n"
            + "GradedFertilityEnabled=true\n"
            + "GradedFertilityStrength=1\n"
            + "EvasiveFleeingEnabled=false\n"
            + "WanderHomeHysteresisEnabled=false\n"
            + "FeedInPlaceEnabled=false\n"
            + "GeneratedPlantSitesEnabled=false\n"
            + "GeneratedPlantSiteSpacing=5\n"
            + "GeneratedPlantSiteJitterFraction=0.35\n"
            + "GeneratedPlantSiteFertilityThreshold=0.45\n"
            + "GeneratedPlantSiteFixedCapacity=0\n"
            + "GeneratedPlantSiteMaximumWaterDistance=0\n"
            + "GeneratedPlantSiteAnchorRingRadius=0\n"
            + "GeneratedPlantSiteAnchorCount=4\n"
            + "ArenaHalfWidth=25\n"
            + "EvasiveFleeingStrength=0.5\n"
            + "ReproductionNeedFraction=0.45\n"
            + "SlopeMovementCostEnabled=false\n"
            + "PlantEstablishmentContestEnabled=true\n"
            + "PlantInvaderEstablishmentContestEnabled=true\n"
            + "PlantSeedProductionRateEnabled=true\n"
            + "SafetyGatedMateRendezvousEnabled=false\n"
            + "HomeRangeAffinityEnabled=false\n"
            + "PlantSeedProductionRateDispersalCharge=2\n"
            + "PlantDefenseDeterrenceStrength=0.75\n"
            + "ThreatFalloffDistance=10\n";

        /// <summary>Not captured: the committed artefact's value, arm control, seed 42, tick 2,666.</summary>
        private const ulong CommittedC3ControlBehaviorHash = 663693199115149672UL;

        internal static SimulationScenario RecordedPredationScenario =>
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.WithRegeneration("p6-defense-calibration-regen2.00", 2f);

        /// <summary>Mirror of the sweep tool's CreateConfig(seed, slope: false) for
        /// <c>--focused 1 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --mate-selection=off</c>.</summary>
        internal static SimulationConfig RecordedPredationCell(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, FreezeFounders);
            return new SimulationConfig(
                worldSeed,
                FreezeFounders,
                defaults.Schedule,
                500,
                FounderProfile.PredationVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: false,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false,
                metabolicIngestionEnabled: false,
                reproductionNeedFraction: 0.45f,
                healthRecoveryEnabled: false,
                metabolicHealingEnabled: false,
                gradedFertilityEnabled: true,
                gradedFertilityStrength: 1.0f,
                evasiveFleeingEnabled: false,
                evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        /// <summary>Mirror of the sweep tool's CreateConfig(seed, slope: false) for
        /// <c>--deaths 24 500 --regen=2.0 --brake=1.5 --ticks=24000</c>.</summary>
        private static SimulationConfig C3Control(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, FreezeFounders);
            return new SimulationConfig(
                worldSeed,
                FreezeFounders,
                defaults.Schedule,
                500,
                FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false,
                metabolicIngestionEnabled: false,
                reproductionNeedFraction: SimulationConfig.DefaultReproductionNeedFraction,
                healthRecoveryEnabled: false,
                metabolicHealingEnabled: false,
                gradedFertilityEnabled: true,
                gradedFertilityStrength: 1.5f,
                evasiveFleeingEnabled: false,
                evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        /// <summary>The sweep tools' loop, exactly: Step, then clear the event buffer, per tick.</summary>
        private static SimulationWorld RunSettled(SimulationConfig config, SimulationScenario scenario, int ticks)
        {
            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);
            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            return world;
        }

        /// <summary>All four comparisons are reported together, so a sentinel run shows every
        /// mismatch rather than stopping at the first.</summary>
        private static void AssertPinned(SimulationWorld world, SimulationConfig config, ulong state, ulong behavior, ulong fingerprint, ulong configuration)
        {
            Assert.Multiple(() =>
            {
                Assert.That(world.ComputeStateHash(), Is.EqualTo(state), "state hash");
                Assert.That(world.ComputeBehaviorHash(), Is.EqualTo(behavior), "behavior hash");
                Assert.That(world.ComputeStateFingerprint(), Is.EqualTo(fingerprint), "state fingerprint");
                Assert.That(config.ComputeConfigurationHash(), Is.EqualTo(configuration), "configuration hash");
            });
        }

        [Test]
        public void PinA_Prototype4DefaultsOnTheModerateCalibration()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(FreezeSeed, FreezeFounders);
            SimulationWorld world = RunSettled(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, DefaultTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(DefaultTicks));
            Assert.That(world.CaptureStatistics().PredationDeathCount, Is.EqualTo(0), "A is not a predation pin");
            AssertPinned(world, config, CapturedAState, CapturedABehavior, CapturedAFingerprint, CapturedAConfiguration);
        }

        [Test]
        public void PinB_FullEcosystemDefaultsOnTheModerateCalibration()
        {
            SimulationConfig config = SimulationConfig.CreateFullEcosystemDefaults(FreezeSeed, FreezeFounders);
            SimulationWorld world = RunSettled(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, DefaultTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(DefaultTicks));
            Assert.That(world.CaptureStatistics().PredationDeathCount, Is.EqualTo(0), "B is not a predation pin");
            AssertPinned(world, config, CapturedBState, CapturedBBehavior, CapturedBFingerprint, CapturedBConfiguration);
        }

        [Test]
        public void PinC_RecordedPredationCell()
        {
            SimulationConfig config = RecordedPredationCell(FreezeSeed);
            SimulationWorld world = RunSettled(config, RecordedPredationScenario, PinCTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(PinCTicks));
            Assert.That(world.CaptureStatistics().AttackHitCount, Is.GreaterThan(0), "a predation pin in which no attack landed pins nothing about predation");
            AssertPinned(world, config, CapturedCState, CapturedCBehavior, CapturedCFingerprint, CapturedCConfiguration);
        }

        [Test]
        public void PinC_CanonicalManifestUnderTheFixedRevision()
        {
            string manifest = ExperimentManifest.Describe(
                PinCRevision, RecordedPredationScenario, RecordedPredationCell(FreezeSeed), FreezeSeed, PinCSeedCount, PinCTicks);

            Assert.That(manifest, Is.EqualTo(PinCCanonicalManifest));
        }

        [Test]
        public void PinD_C3ControlReproducesTheCommittedArtefact()
        {
            // docs/experiments/p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-24000ticks-9samples-2026-09-10.csv,
            // arm control, seed 42, sample 1 of 9, tick 2,666, column behavior_hash. The behaviour
            // hash is the committed number; the other three are captured beside it.
            SimulationConfig config = C3Control(FreezeSeed);
            SimulationWorld world = RunSettled(config, RecordedPredationScenario, PinDTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(PinDTicks));
            AssertPinned(world, config, CapturedDState, CommittedC3ControlBehaviorHash, CapturedDFingerprint, CapturedDConfiguration);
        }
    }
}

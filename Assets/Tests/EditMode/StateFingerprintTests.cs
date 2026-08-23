using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="SimulationWorld.ComputeStateFingerprint"/> (V2) and
    /// <see cref="SimulationConfig.ComputeConfigurationHash"/>, per
    /// docs/superpowers/specs/2026-08-22-state-fingerprint-design.md.
    ///
    /// Does not touch <see cref="SimulationWorld.ComputeStateHash"/> (V1), which is frozen.
    /// </summary>
    public sealed class StateFingerprintTests
    {
        [Test]
        public void TwoIdenticalWorldsStayFingerprintEqualFor2000TicksAcrossBirthsDispersalDeathAndTakeover()
        {
            SimulationWorld worldA = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            SimulationWorld worldB = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);

            Assert.That(worldA.ComputeStateFingerprint(), Is.EqualTo(worldB.ComputeStateFingerprint()));

            // Manipulation checks, gathered as the run proceeds. A takeover overwrites a patch in
            // place (PlantPatchStore.ReplaceAt), so the id survives and only the lineage parent
            // changes; a mortality removal drops the patch count. Neither is visible after the fact,
            // so both are observed tick by tick.
            var lineageParentById = new Dictionary<long, long>();
            int takeoverCount = 0;
            int patchRemovalCount = 0;
            int previousPatchCount = worldA.Plants.Count;
            RecordLineageParents(worldA, lineageParentById);

            for (long tick = 1; tick <= 2000; tick++)
            {
                worldA.Step(worldA.Config.FixedDeltaTime);
                worldA.Events.Clear();
                worldB.Step(worldB.Config.FixedDeltaTime);
                worldB.Events.Clear();

                Assert.That(worldA.ComputeStateFingerprint(), Is.EqualTo(worldB.ComputeStateFingerprint()),
                    $"fingerprints diverged at tick {tick}");

                takeoverCount += CountLineageParentChanges(worldA, lineageParentById);
                if (worldA.Plants.Count < previousPatchCount)
                {
                    patchRemovalCount += previousPatchCount - worldA.Plants.Count;
                }

                previousPatchCount = worldA.Plants.Count;
            }

            // Confirm the run actually exercised the ordinal-sensitive paths rather than passing
            // vacuously: a birth advances _birthOrdinal and a successful dispersal advances
            // _plantSeedOrdinal.
            long birthOrdinal = (long)GetPrivateField(worldA, "_birthOrdinal");
            long plantSeedOrdinal = (long)GetPrivateField(worldA, "_plantSeedOrdinal");
            Assert.That(birthOrdinal, Is.GreaterThan(0), "expected at least one birth over 2000 ticks");
            Assert.That(plantSeedOrdinal, Is.GreaterThan(0), "expected at least one plant dispersal over 2000 ticks");

            // The test name claims four paths. Without these two the death and takeover halves of
            // that claim would be unevidenced, and the test would pass just as happily in a run
            // where no patch ever died.
            Assert.That(patchRemovalCount, Is.GreaterThan(0), "expected at least one plant death over 2000 ticks");
            Assert.That(takeoverCount, Is.GreaterThan(0), "expected at least one plant takeover over 2000 ticks");
        }

        private static void RecordLineageParents(SimulationWorld world, Dictionary<long, long> lineageParentById)
        {
            for (int index = 0; index < world.Plants.Count; index++)
            {
                PlantPatchState patch = world.Plants.GetAt(index);
                lineageParentById[patch.Id.Value] = patch.Lineage.ParentId.Value;
            }
        }

        /// <summary>
        /// A surviving patch id whose lineage parent changed was overwritten in place by a
        /// competing seedling. Keyed on the lineage parent rather than on age, which would key the
        /// detector on the very reset that the 2026-08-22 ReplaceAt fix introduced.
        /// </summary>
        private static int CountLineageParentChanges(SimulationWorld world, Dictionary<long, long> lineageParentById)
        {
            int changes = 0;
            for (int index = 0; index < world.Plants.Count; index++)
            {
                PlantPatchState patch = world.Plants.GetAt(index);
                if (lineageParentById.TryGetValue(patch.Id.Value, out long previousParent)
                    && previousParent != patch.Lineage.ParentId.Value)
                {
                    changes++;
                }

                lineageParentById[patch.Id.Value] = patch.Lineage.ParentId.Value;
            }

            return changes;
        }

        [Test]
        public void FingerprintChangesWhenOnlyBirthOrdinalDiffers()
        {
            // ReproductionSystem only advances _birthOrdinal inside CreateChild, which also grows
            // CreatureCount and CreatureStore.NextIdPeek in the same call — so a real birth can
            // never isolate this field through simulation alone. Test-only reflection is used to
            // perturb exactly this field and nothing else; no production setter is added.
            SimulationWorld worldA = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            SimulationWorld worldB = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            StepTo(worldA, 50);
            StepTo(worldB, 50);
            Assert.That(worldA.ComputeStateFingerprint(), Is.EqualTo(worldB.ComputeStateFingerprint()),
                "precondition: both worlds must be identical before the isolated perturbation");

            long birthOrdinal = (long)GetPrivateField(worldB, "_birthOrdinal");
            SetPrivateField(worldB, "_birthOrdinal", birthOrdinal + 1);

            Assert.That(worldA.ComputeStateFingerprint(), Is.Not.EqualTo(worldB.ComputeStateFingerprint()));
        }

        [Test]
        public void FingerprintChangesWhenOnlyPlantSeedOrdinalDiffers()
        {
            // Same reasoning as the birth-ordinal test: PlantReproductionSystem only advances
            // _plantSeedOrdinal on a real dispersal draw, which also changes plant/site state in
            // the same call. Test-only reflection isolates the field.
            SimulationWorld worldA = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            SimulationWorld worldB = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            StepTo(worldA, 50);
            StepTo(worldB, 50);
            Assert.That(worldA.ComputeStateFingerprint(), Is.EqualTo(worldB.ComputeStateFingerprint()),
                "precondition: both worlds must be identical before the isolated perturbation");

            long plantSeedOrdinal = (long)GetPrivateField(worldB, "_plantSeedOrdinal");
            SetPrivateField(worldB, "_plantSeedOrdinal", plantSeedOrdinal + 1);

            Assert.That(worldA.ComputeStateFingerprint(), Is.Not.EqualTo(worldB.ComputeStateFingerprint()));
        }

        [Test]
        public void FingerprintChangesWhenOnlyOnePlantsAgeDiffers()
        {
            // PlantPatchStore.AdvanceAge is a real production API, so this needs no test hook.
            SimulationWorld worldA = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            SimulationWorld worldB = BuildWorld(
                SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            Assert.That(worldB.Plants.Count, Is.GreaterThan(0), "scenario must seed at least one active plant patch");
            Assert.That(worldA.ComputeStateFingerprint(), Is.EqualTo(worldB.ComputeStateFingerprint()));

            worldB.Plants.AdvanceAge(0, 1f);

            Assert.That(worldA.ComputeStateFingerprint(), Is.Not.EqualTo(worldB.ComputeStateFingerprint()));
        }

        [Test]
        public void FingerprintChangesWhenOnlyOneConfigFlagDiffers()
        {
            SimulationWorld worldA = BuildWorld(
                CreateFullEcosystemDefaultsWithPlantMortality(worldSeed: 42, initialPopulation: 12, plantMortalityEnabled: true),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);
            SimulationWorld worldB = BuildWorld(
                CreateFullEcosystemDefaultsWithPlantMortality(worldSeed: 42, initialPopulation: 12, plantMortalityEnabled: false),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate);

            Assert.That(worldA.ComputeStateFingerprint(), Is.Not.EqualTo(worldB.ComputeStateFingerprint()));
        }

        [Test]
        public void ComputeStateFingerprintRefusesToSampleWhileADeathIsQueued()
        {
            SimulationWorld world = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4));
            StepTo(world, 5);
            world.RequestDeath(world.GetCreatureIdAt(0), DeathCause.Starvation);

            Assert.That(
                () => world.ComputeStateFingerprint(),
                Throws.InstanceOf<InvalidOperationException>(),
                "sampling between RequestDeath and the next Step would report the pending state this method exists to exclude");
        }

        [Test]
        public void ComputeStateFingerprintSucceedsOnceTheQueuedDeathIsCommitted()
        {
            SimulationWorld world = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4));
            StepTo(world, 5);
            world.RequestDeath(world.GetCreatureIdAt(0), DeathCause.Starvation);

            world.Step(world.Config.FixedDeltaTime);

            Assert.That(() => world.ComputeStateFingerprint(), Throws.Nothing);
        }

        [Test]
        public void EveryConfigurationBoolConstructorParameterChangesTheConfigurationHash()
        {
            ConstructorInfo ctor = typeof(SimulationConfig).GetConstructors().Single();
            ParameterInfo[] parameters = ctor.GetParameters();
            List<int> boolParameterIndices = new List<int>();
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].ParameterType == typeof(bool))
                {
                    boolParameterIndices.Add(index);
                }
            }

            Assert.That(boolParameterIndices, Is.Not.Empty, "expected SimulationConfig's constructor to have bool parameters");

            object[] BuildArgs(int flippedIndex)
            {
                var args = new object[parameters.Length];
                for (int index = 0; index < parameters.Length; index++)
                {
                    ParameterInfo parameter = parameters[index];
                    if (parameter.Name == "worldSeed") { args[index] = 7; continue; }
                    if (parameter.Name == "initialPopulation") { args[index] = 4; continue; }
                    if (parameter.Name == "schedule") { args[index] = new SimulationSchedule(20, 20, 4, 2, 2, 1, 1, 1); continue; }

                    object defaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : false;
                    if (index == flippedIndex)
                    {
                        args[index] = !(bool)defaultValue;
                    }
                    else
                    {
                        args[index] = defaultValue;
                    }
                }

                return args;
            }

            var baseline = (SimulationConfig)ctor.Invoke(BuildArgs(-1));
            ulong baselineHash = baseline.ComputeConfigurationHash();

            foreach (int index in boolParameterIndices)
            {
                var flipped = (SimulationConfig)ctor.Invoke(BuildArgs(index));
                Assert.That(flipped.ComputeConfigurationHash(), Is.Not.EqualTo(baselineHash),
                    $"flipping constructor bool parameter '{parameters[index].Name}' from its default did not change "
                    + "ComputeConfigurationHash — a flag was added without being wired into the hash.");
            }
        }

        [Test]
        public void ConfigurationHashCoverageMatchesThePinnedPropertyCount()
        {
            // Update this constant, and ComputeConfigurationHash above it, whenever
            // SimulationConfig gains or loses a public instance property. FixedDeltaTime and
            // MaximumMemorySlots are excluded because both are derived from already-hashed
            // properties (BaseFrequencyHz, and MinimumMemorySlots + AdditionalMemorySlots,
            // respectively) rather than independent configuration.
            const int PinnedConfigurationPropertyCount = 45;

            PropertyInfo[] properties = typeof(SimulationConfig)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != nameof(SimulationConfig.FixedDeltaTime)
                    && property.Name != nameof(SimulationConfig.MaximumMemorySlots))
                .ToArray();

            Assert.That(properties.Length, Is.EqualTo(PinnedConfigurationPropertyCount),
                "SimulationConfig's property set changed. Update ComputeConfigurationHash to cover the new/removed "
                + "property, then update PinnedConfigurationPropertyCount to match.");
        }

        private static SimulationConfig CreateFullEcosystemDefaultsWithPlantMortality(int worldSeed, int initialPopulation, bool plantMortalityEnabled)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                defaults.Schedule,
                defaults.MaximumPopulation,
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
                plantMortalityEnabled: plantMortalityEnabled,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true);
        }

        private static SimulationWorld BuildWorld(SimulationConfig config, SimulationScenario scenario)
        {
            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);
            return world;
        }

        private static void StepTo(SimulationWorld world, long tick)
        {
            while (world.CurrentTick < tick)
            {
                world.Step(world.Config.FixedDeltaTime);
                world.Events.Clear();
            }
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"expected private field '{fieldName}' on {target.GetType().Name}");
            return field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"expected private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}

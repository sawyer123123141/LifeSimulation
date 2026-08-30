using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// A wandering creature that has learned a home must not chatter across its own home radius.
    ///
    /// <para><b>The bug.</b> <c>GetMovementTarget</c>'s wander branch aims at a point <b>on</b> a ring
    /// of radius <c>homeRadius</c> while the creature is inside that radius, and at the home centre
    /// once it is outside. The ring point is at exactly the distance that flips the test, so a
    /// creature walks out to the ring, crosses it, turns around, walks back in, crosses it again, and
    /// repeats - a limit cycle on its own boundary. Measured on the `Y` playtest before the fix:
    /// 13.1% of all wander heading updates were a reversal of more than 150 degrees in a single tick,
    /// 28,752 of 28,753 of them belonging to creatures with a memory home, 85.6% occurring within 0.25
    /// of the 3.0 radius. On screen the presenter turns the model through that reversal at 2,160
    /// degrees a second, which reads as the animal spinning on the spot.</para>
    ///
    /// <para>The fix is hysteresis: the creature only heads back to the centre once it has strayed
    /// past a band beyond the ring, so arriving at the ring is an arrival rather than a trigger.</para>
    /// </summary>
    [TestFixture]
    public sealed class WanderHomeHysteresisTests
    {
        private const int Ticks = 12000;

        /// <summary>A reversal this sharp in one tick is the signature; a real turn is far smaller.</summary>
        private const float ReversalDegrees = 150f;

        [Test]
        public void WanderReversalsCollapseWhenHysteresisIsEnabled()
        {
            float withoutFix = MeasureWanderReversalRate(hysteresisEnabled: false);
            float withFix = MeasureWanderReversalRate(hysteresisEnabled: true);

            Assert.That(
                withoutFix,
                Is.GreaterThan(0.05f),
                "The bug should still be reproducible with the flag off - if this fails the measurement changed, not the fix.");

            Assert.That(
                withFix,
                Is.LessThan(withoutFix / 4f),
                $"Hysteresis should remove most wander reversals. Without: {withoutFix:P2}, with: {withFix:P2}.");
        }

        /// <summary>
        /// The flag must stay off by default: every recorded result predates it.
        /// </summary>
        [Test]
        public void HysteresisIsOffByDefault()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            Assert.That(defaults.WanderHomeHysteresisEnabled, Is.False);
        }

        /// <summary>
        /// The share of wander heading updates that reverse by more than <see cref="ReversalDegrees"/>
        /// in a single tick, computed with the presenter's own heading rule.
        /// </summary>
        private static float MeasureWanderReversalRate(bool hysteresisEnabled)
        {
            SimulationConfig config = BuildTerrainPlaytestConfig(hysteresisEnabled);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);

            var lastHeading = new System.Collections.Generic.Dictionary<CreatureId, float>();
            long samples = 0;
            long reversals = 0;

            for (int tick = 0; tick < Ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);

                for (int index = 0; index < world.Creatures.Count; index++)
                {
                    CreatureId id = world.Creatures.GetIdAt(index);
                    MovementState movement = world.Creatures.GetMovementAt(index);
                    bool isWandering = world.Creatures.GetDecisionAt(index).Action == CreatureAction.Wander;

                    float deltaX = movement.Position.X - movement.PreviousPosition.X;
                    float deltaY = movement.Position.Y - movement.PreviousPosition.Y;

                    // The presenter's threshold, verbatim: below it the drawn heading is held.
                    if ((deltaX * deltaX) + (deltaY * deltaY) <= 1e-8f) continue;

                    float yaw = (float)Math.Atan2(deltaX, deltaY) * (180f / (float)Math.PI);

                    if (isWandering && lastHeading.TryGetValue(id, out float previous))
                    {
                        samples++;
                        if (Math.Abs(DeltaAngle(previous, yaw)) > ReversalDegrees) reversals++;
                    }

                    lastHeading[id] = yaw;
                }
            }

            Assert.That(samples, Is.GreaterThan(1000), "Too few wander samples to measure.");
            return (float)reversals / samples;
        }

        /// <summary>Mathf.DeltaAngle, which this project cannot reference outside Unity.</summary>
        private static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }

        /// <summary>
        /// The `Y` terrain playtest, matching <c>Prototype1Presenter.ResetTerrainPlaytest</c>. That is
        /// the scenario the spinning was reported in, and the lesson log is explicit that measuring a
        /// different world than the one being watched is worse than not measuring.
        /// </summary>
        private static SimulationConfig BuildTerrainPlaytestConfig(bool hysteresisEnabled)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            return new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 96,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
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
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: true,
                terrainDrivenTemperatureEnabled: true,
                healthRecoveryEnabled: true,
                wanderHomeHysteresisEnabled: hysteresisEnabled);
        }
    }
}

using System.Linq;
using System.Reflection;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class DecisionSystemTests
    {
        // Mirrors PredationSystemTests.MakePhenotype: Phenotype's constructor is private and
        // Phenotype.FromGenome(Genome) derives every field from a shared bodyMass term, so there
        // is no public way to set independent, exact field values for these tests. This helper
        // invokes the real private constructor via reflection so each test can pin the exact
        // attacker/defender numbers used in the behavior table.
        private static readonly ConstructorInfo PhenotypeConstructor =
            typeof(Phenotype)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(constructor => constructor.GetParameters().Length == 27);

        private static Phenotype MakePhenotype(
            float attackPower, float defense, float maneuverability, float aggression = 0.5f,
            float meatYieldMultiplier = 1f, float energyCapacity = 100f)
        {
            var namedValues = new System.Collections.Generic.Dictionary<string, float>
            {
                ["bodyMass"] = 1f,
                ["energyCapacity"] = energyCapacity,
                ["hydrationCapacity"] = 100f,
                ["healthCapacity"] = 100f,
                ["maximumSpeed"] = 2f,
                ["visionRange"] = 8f,
                ["foodYield"] = 1f,
                ["ingestionRate"] = 1f,
                ["digestionRate"] = 1f,
                ["waterLossMultiplier"] = 1f,
                ["basalEnergyCostMultiplier"] = 1f,
                ["attackPower"] = attackPower,
                ["defense"] = defense,
                ["maneuverability"] = maneuverability,
                ["fearResponse"] = 0.5f,
                ["aggression"] = aggression,
                ["plantFoodYieldMultiplier"] = 1f,
                ["meatYieldMultiplier"] = meatYieldMultiplier,
                ["memoryConfidenceDecayPerSecond"] = 0.05f,
                ["cognitionRestCostMultiplier"] = 1f,
                ["temperatureTolerance"] = 5f,
                ["learningRate"] = 0.5f,
                ["exploration"] = 0.5f,
                ["reproductionCooldownSeconds"] = 10f,
                ["reproductionEnergyCostFraction"] = 0.2f,
                ["maximumAgeSeconds"] = 180f,
                ["persistence"] = 0.5f,
            };

            ParameterInfo[] parameters = PhenotypeConstructor.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                string parameterName = parameters[i].Name;
                if (!namedValues.TryGetValue(parameterName, out float value))
                {
                    throw new System.InvalidOperationException(
                        $"MakePhenotype has no known value for Phenotype constructor parameter " +
                        $"'{parameterName}'. Add it to the namedValues map in MakePhenotype " +
                        "instead of relying on positional argument order.");
                }

                args[i] = value;
            }

            return (Phenotype)PhenotypeConstructor.Invoke(args);
        }

        // Behavior table row 2: economics-enabled, strongly favorable attacker/defender pair at
        // close distance -> a SeekPrey candidate should win with Score > 0.
        [Test]
        public void IntentUtilityWithEconomicsEnabledSeeksPreyForStronglyFavorableMatchup()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            CreatureNeeds needs = CreatureNeeds.Full(attacker);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var threatObservation = new CreatureObservation(new CreatureId(2), 1, 1f);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, attacker, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threatObservation,
                threatIntensity: 0f, otherPhenotype: defender, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: true, tick: 0,
                diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
            Assert.That(decision.Score, Is.GreaterThan(0f));
        }

        // Behavior table row 3: economics-enabled, unfavorable attacker/defender pair at long
        // distance -> huntScore is 0, so no SeekPrey candidate is added.
        [Test]
        public void IntentUtilityWithEconomicsEnabledDoesNotSeekPreyForUnfavorableMatchup()
        {
            Phenotype attacker = MakePhenotype(attackPower: 0.3f, defense: 1.8f, maneuverability: 2.5f, aggression: 0.8f, meatYieldMultiplier: 0.6f);
            Phenotype defender = MakePhenotype(attackPower: 1.7f, defense: 1.8f, maneuverability: 2.5f, energyCapacity: 150f);
            CreatureNeeds needs = CreatureNeeds.Full(attacker);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var threatObservation = new CreatureObservation(new CreatureId(2), 14, 1f);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, attacker, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threatObservation,
                threatIntensity: 0f, otherPhenotype: defender, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: true, tick: 0,
                diagnostics: out _);

            Assert.That(decision.Action, Is.Not.EqualTo(CreatureAction.SeekPrey));
        }

        // Bug report: the inspector's "Also:" line (flee/hunt/carcass/warmth) always reads 0 under
        // IntentUtilityV1 because DecideIntentUtilityV1 built its DecisionDiagnostics from only
        // bestFoodScore/bestWaterScore and never called .WithPredationScores/.WithCarcassScore/
        // .WithThermalScore, even though ScorePredation/ScoreCarcass/the thermal block all compute
        // real values. Reuses row 2's exact favorable-matchup setup, but checks the diagnostics
        // output instead of ignoring it.
        [Test]
        public void IntentUtilityDiagnosticsReportsNonZeroHuntScoreForAFavorableMatchup()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            CreatureNeeds needs = CreatureNeeds.Full(attacker);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var threatObservation = new CreatureObservation(new CreatureId(2), 1, 1f);

            DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, attacker, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: threatObservation,
                threatIntensity: 0f, otherPhenotype: defender, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, economicsEnabled: true, tick: 0,
                diagnostics: out DecisionDiagnostics diagnostics);

            Assert.That(diagnostics.HuntScore, Is.GreaterThan(0f));
        }

        // Behavior table row 1: no remembered threat -> remembered-food score is
        // exactly today's pre-existing formula (avoidance term is 0).
        [Test]
        public void RememberedFoodScoreIsUnaffectedWhenNoThreatIsRemembered()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var memory = new MemoryState
            {
                FoodPosition = new SimVector2(10f, 0f),
                FoodConfidence = 1f,
                FoodAge = 0f,
                FoodOutcomeValue = 0f,
                FoodExperienceCount = 0,
            };

            DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: memory, cognitionEnabled: true, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, tick: 0,
                diagnostics: out DecisionDiagnostics diagnostics);

            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.6640625f).Within(0.0001f));
        }

        // Behavior table row 2: a remembered threat sitting at the remembered food's
        // exact location applies the same avoidance penalty
        // TryScoreBestRememberedPlace already applies under Legacy, lowering the
        // remembered-food score.
        [Test]
        public void RememberedFoodScoreIsLoweredByARememberedThreatAtTheSameLocation()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var memory = new MemoryState
            {
                FoodPosition = new SimVector2(10f, 0f),
                FoodConfidence = 1f,
                FoodAge = 0f,
                FoodOutcomeValue = 0f,
                FoodExperienceCount = 0,
                ThreatPosition = new SimVector2(10f, 0f),
                ThreatConfidence = 1f,
            };

            DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: memory, cognitionEnabled: true, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, tick: 0,
                diagnostics: out DecisionDiagnostics diagnostics,
                threatFalloffDistance: SimulationConfig.DefaultThreatFalloffDistance);

            Assert.That(diagnostics.FoodScore, Is.EqualTo(0.1640625f).Within(0.0001f));
        }

        // Proves best-of-K selection: with two visible creatures, the winning SeekPrey candidate
        // targets the favorable-to-hunt one even though it is farther away than the dangerous one,
        // and the Flee candidate (if present) targets the dangerous one - not just "the nearest".
        [Test]
        public void MultiThreatPerceptionSelectsBestHuntTargetAcrossMultipleVisibleCreatures()
        {
            Phenotype self = MakePhenotype(attackPower: 1f, defense: 0.5f, maneuverability: 1f, aggression: 0.8f);
            Phenotype weakFavorableTarget = MakePhenotype(attackPower: 0.1f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            Phenotype strongDangerousTarget = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f);
            CreatureNeeds needs = CreatureNeeds.Full(self);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 0);
            var otherCandidates = new PredationCandidateBuffer();
            var weakObservation = new CreatureObservation(new CreatureId(2), 1, 5f);
            var strongObservation = new CreatureObservation(new CreatureId(3), 2, 1f);
            otherCandidates.Add(strongObservation, strongDangerousTarget);
            otherCandidates.Add(weakObservation, weakFavorableTarget);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, self, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: true, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, tick: 0,
                diagnostics: out _, economicsEnabled: true,
                threatFalloffDistance: SimulationConfig.DefaultThreatFalloffDistance,
                otherCandidates: otherCandidates, multiThreatPerceptionEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(weakObservation.CreatureId));
        }

        [Test]
        public void IntentUtilitySelectsRestWhenRestNeedIsLowAndNothingElseIsUrgent()
        {
            Phenotype phenotype = MakePhenotype(attackPower: 1f, defense: 1f, maneuverability: 1f);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Rest = 10f;
            var resources = new ResourceStore(initialCapacity: 0);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default,
                threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
                reproduction: default, mate: default, mateNeeds: default, matePhenotype: default,
                mateReproduction: default, reproductionEnabled: false, tick: 0,
                diagnostics: out _, restBehaviorEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Rest));
        }
    }
}

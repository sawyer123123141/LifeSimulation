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
    }
}

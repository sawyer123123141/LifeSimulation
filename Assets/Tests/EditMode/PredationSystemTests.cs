using System.Linq;
using System.Reflection;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PredationSystemTests
    {
        // Phenotype's constructor is private and Phenotype.FromGenome(Genome) derives every
        // field (including AttackPower/Defense/EnergyCapacity) from a shared bodyMass term, so
        // there is no public way to set independent, exact field values for these tests. This
        // helper invokes the real private constructor via reflection so each test can pin the
        // exact attacker/defender numbers used in the behavior table.
        private static readonly ConstructorInfo PhenotypeConstructor =
            typeof(Phenotype)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(constructor => constructor.GetParameters().Length == 27);

        private static Phenotype MakePhenotype(
            float attackPower, float defense, float maneuverability, float aggression = 0.5f,
            float meatYieldMultiplier = 1f, float energyCapacity = 100f)
        {
            // Named per constructor parameter (not positional) so a future reordering of
            // Phenotype's private constructor cannot silently shuffle values into the wrong
            // fields. Any parameter this helper doesn't recognize by name throws immediately.
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

        // Row 1: legacy formula, diet above threshold.
        // NOTE: the brief's worked example stated advantage ~= 0.6725 and result ~= 0.3766, but
        // 1.5 / (1.5+0.5+0.25+0.01) = 1.5/2.26 = 0.663717 (not 0.6725), giving a result of
        // 0.663717 * 0.8 * 0.7 = 0.371681. Verified against the untouched legacy code in the
        // false branch, so the test below uses the corrected value.
        [Test]
        public void LegacyHuntCapabilityMatchesTodaysFormulaWhenDietAboveThreshold()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.5f, defense: 999f, maneuverability: 999f, aggression: 0.8f, meatYieldMultiplier: 1.2f);
            Phenotype defender = MakePhenotype(attackPower: 999f, defense: 0.5f, maneuverability: 1f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 5f, economicsEnabled: false);

            Assert.That(result, Is.EqualTo(0.371681f).Within(0.001f));
        }

        // Row 2: legacy formula, diet below threshold -> gated to zero regardless of distance.
        [Test]
        public void LegacyHuntCapabilityReturnsZeroWhenDietBelowThreshold()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.5f, defense: 0.5f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 0.9f);
            Phenotype defender = MakePhenotype(attackPower: 1f, defense: 0.5f, maneuverability: 1f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 5f, economicsEnabled: false);

            Assert.That(result, Is.EqualTo(0f));
        }

        // Row 3: economics-enabled, strongly favorable matchup (big weak close prey).
        [Test]
        public void EconomicsHuntCapabilityIsStronglyFavorableForBigWeakClosePrey()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 1f, economicsEnabled: true);

            Assert.That(result, Is.GreaterThan(0.5f));
        }

        // Row 4: economics-enabled, unfavorable matchup -> net EV goes negative -> clamped to zero.
        [Test]
        public void EconomicsHuntCapabilityIsZeroWhenNetExpectedValueIsNegative()
        {
            Phenotype attacker = MakePhenotype(attackPower: 0.3f, defense: 1.8f, maneuverability: 2.5f, aggression: 0.8f, meatYieldMultiplier: 0.6f);
            Phenotype defender = MakePhenotype(attackPower: 1.7f, defense: 1.8f, maneuverability: 2.5f, energyCapacity: 150f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 14f, economicsEnabled: true);

            Assert.That(result, Is.EqualTo(0f));
        }

        // Row 5: same favorable matchup as row 3, but zero aggression zeroes the final multiplier.
        [Test]
        public void EconomicsHuntCapabilityIsZeroWhenAggressionIsZero()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);

            float result = PredationSystem.HuntCapability(attacker, defender, distance: 1f, economicsEnabled: true);

            Assert.That(result, Is.EqualTo(0f));
        }

        // Row 6: pursuit cost increases with distance, so result should strictly decrease.
        // NOTE: the brief suggested comparing distance=1 vs distance=10 using row 3's
        // attacker/defender, but at both of those distances the net EV / NormalizingEnergyScale
        // term exceeds 1 and gets Clamp01'd to the same saturated value (0.8 == 0.8), which would
        // make the "strictly greater" assertion fail. Using distance=200 for the second sample
        // pushes the net EV below the clamp ceiling so the two results are genuinely distinct.
        [Test]
        public void EconomicsHuntCapabilityDecreasesWithPursuitDistance()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);

            float resultAtDistance1 = PredationSystem.HuntCapability(attacker, defender, distance: 1f, economicsEnabled: true);
            float resultAtDistance200 = PredationSystem.HuntCapability(attacker, defender, distance: 200f, economicsEnabled: true);

            Assert.That(resultAtDistance1, Is.GreaterThan(resultAtDistance200));
        }

        // Row 7: legacy Threat formula, same attacker/defender as row 1.
        [Test]
        public void LegacyThreatMatchesTodaysFormula()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.5f, defense: 999f, maneuverability: 999f, aggression: 0.8f, meatYieldMultiplier: 1.2f);
            Phenotype defender = MakePhenotype(attackPower: 999f, defense: 0.5f, maneuverability: 1f);

            float result = PredationSystem.Threat(attacker, defender, distance: 5f, economicsEnabled: false);

            Assert.That(result, Is.EqualTo(0.7196f).Within(0.001f));
        }

        // Row 8: economics-enabled, huntScore == 0 (row 4's matchup) -> Threat gated to zero.
        [Test]
        public void EconomicsThreatIsZeroWhenHuntScoreIsZero()
        {
            Phenotype attacker = MakePhenotype(attackPower: 0.3f, defense: 1.8f, maneuverability: 2.5f, aggression: 0.8f, meatYieldMultiplier: 0.6f);
            Phenotype defender = MakePhenotype(attackPower: 1.7f, defense: 1.8f, maneuverability: 2.5f, energyCapacity: 150f);

            float result = PredationSystem.Threat(attacker, defender, distance: 14f, economicsEnabled: true);

            Assert.That(result, Is.EqualTo(0f));
        }

        // Row 9: economics-enabled, huntScore > 0 (row 3's matchup) -> Threat computed normally.
        [Test]
        public void EconomicsThreatIsPositiveWhenHuntScoreIsPositive()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);

            float result = PredationSystem.Threat(attacker, defender, distance: 1f, economicsEnabled: true);

            float pressure = attacker.AttackPower * (0.5f + attacker.Aggression);
            float resistance = defender.Defense + (0.25f * defender.Maneuverability) + 0.01f;
            float expected = pressure / (pressure + resistance);

            Assert.That(result, Is.GreaterThan(0f));
            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        // Decide row 2: economics-enabled, favorable matchup (Task 2 row 3) -> SeekPrey chosen.
        [Test]
        public void EconomicsEnabledDecideChoosesSeekPreyForAFavorableMatchup()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0.8f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, energyCapacity: 200f);
            CreatureNeeds needs = new CreatureNeeds { Energy = 10f, Hydration = 100f };
            CreatureObservation observation = new CreatureObservation(new CreatureId(2), 1, 1f);
            CreatureDecision survival = new CreatureDecision(CreatureAction.Wander, -1, 0.05f);

            CreatureDecision decision = PredationSystem.Decide(needs, attacker, defender, observation, survival, economicsEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(observation.CreatureId));
        }

        // Decide row 3: economics-enabled, unfavorable matchup -> unchanged.
        // NOTE: Decide's threat term is Threat(other, self, ...), which swaps the attacker/defender
        // roles relative to hunt's HuntCapability(self, other, ...). Task 2's row-4 phenotypes only
        // guarantee HuntCapability(attacker, defender) == 0 in that fixed order; the swapped-role
        // Threat(other, self) call is a *different* attacker/defender pairing and is not guaranteed
        // zero by that row. To keep both hunt and threat at zero regardless of role order, this test
        // instead zeroes aggression on both phenotypes: HuntCapability's economics formula always
        // multiplies by attacker.Aggression, so a zero aggression on either phenotype zeroes
        // HuntCapability (and therefore Threat, which gates on huntScore <= 0) in both directions.
        [Test]
        public void EconomicsEnabledDecideReturnsSurvivalDecisionForAnUnfavorableMatchup()
        {
            Phenotype attacker = MakePhenotype(attackPower: 1.9f, defense: 0.1f, maneuverability: 1f, aggression: 0f, meatYieldMultiplier: 1.3f);
            Phenotype defender = MakePhenotype(attackPower: 0.2f, defense: 0.1f, maneuverability: 1f, aggression: 0f, energyCapacity: 200f);
            CreatureNeeds needs = new CreatureNeeds { Energy = 10f, Hydration = 100f };
            CreatureObservation observation = new CreatureObservation(new CreatureId(2), 1, 1f);
            CreatureDecision survival = new CreatureDecision(CreatureAction.Wander, -1, 0.05f);

            CreatureDecision decision = PredationSystem.Decide(needs, attacker, defender, observation, survival, economicsEnabled: true);

            Assert.That(decision.Action, Is.EqualTo(survival.Action));
            Assert.That(decision.Score, Is.EqualTo(survival.Score));
        }
    }
}

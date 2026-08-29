using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Behavior
{
    public static class PredationSystem
    {
        private const float MinimumHuntingDiet = 0.58f;
        private const float MinimumHuntingAggression = 0.35f;
        private const float InjuryCostScale = 20f;
        private const float PursuitCostPerDistance = 0.5f;
        private const float NormalizingEnergyScale = 150f;

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision,
            bool economicsEnabled = false)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return Decide(needs, self, other, otherObservation, survivalDecision, ref ignoredDiagnostics, economicsEnabled);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision,
            ref DecisionDiagnostics diagnostics,
            bool economicsEnabled = false)
        {
            if (!otherObservation.IsValid)
            {
                return survivalDecision;
            }

            float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + otherObservation.Distance);
            float hunger = 1f - (needs.Energy / self.EnergyCapacity);
            float threat = Threat(other, self, otherObservation.Distance, economicsEnabled) * self.FearResponse * distanceAvailability;
            float hunt = HuntCapability(self, other, otherObservation.Distance, economicsEnabled) * hunger * distanceAvailability;
            diagnostics = diagnostics.WithPredationScores(threat, hunt);

            if (threat > Math.Max(0.10f, hunt) && threat > survivalDecision.Score)
            {
                return new CreatureDecision(CreatureAction.Flee, -1, threat, targetCreatureId: otherObservation.CreatureId);
            }

            if (hunt > survivalDecision.Score && hunt >= 0.10f)
            {
                return new CreatureDecision(CreatureAction.SeekPrey, -1, hunt, targetCreatureId: otherObservation.CreatureId);
            }

            return survivalDecision;
        }

        // Legacy-only entry point retained for DecisionSystem.ScorePredation, SimulationWorld.cs,
        // and pre-existing tests in SpatialBehaviorTests.cs. Distance is ignored by the legacy
        // (non-economics) branch, so passing 0f here preserves byte-identical legacy behavior.
        public static float Threat(Phenotype attacker, Phenotype defender)
        {
            return Threat(attacker, defender, distance: 0f, economicsEnabled: false);
        }

        public static float Threat(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
        {
            float huntScore = HuntCapability(attacker, defender, distance, economicsEnabled);
            if (huntScore <= 0f)
            {
                return 0f;
            }

            float pressure = attacker.AttackPower * (0.5f + attacker.Aggression);
            float resistance = defender.Defense + (0.25f * defender.Maneuverability) + 0.01f;
            return Clamp01(pressure / (pressure + resistance));
        }

        public static CreatureDecision PreferCarcassWhenUseful(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation carcass,
            CreatureDecision currentDecision)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return PreferCarcassWhenUseful(needs, phenotype, carcass, currentDecision, ref ignoredDiagnostics);
        }

        public static CreatureDecision PreferCarcassWhenUseful(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation carcass,
            CreatureDecision currentDecision,
            ref DecisionDiagnostics diagnostics)
        {
            if (!carcass.IsValid)
            {
                return currentDecision;
            }

            float hunger = 1f - (needs.Energy / phenotype.EnergyCapacity);
            float score = hunger * phenotype.MeatYieldMultiplier / (1f + carcass.Distance);
            diagnostics = diagnostics.WithCarcassScore(score);
            return score > currentDecision.Score && score >= 0.10f
                ? new CreatureDecision(CreatureAction.SeekCarcass, carcass.ResourceIndex, score)
                : currentDecision;
        }

        // Compatibility shim: see comment on the two-parameter Threat overload above.
        public static float HuntCapability(Phenotype attacker, Phenotype defender)
        {
            return HuntCapability(attacker, defender, distance: 0f, economicsEnabled: false);
        }

        public static float HuntCapability(Phenotype attacker, Phenotype defender, float distance, bool economicsEnabled)
        {
            if (!economicsEnabled)
            {
                float legacyDiet = Clamp01((attacker.MeatYieldMultiplier - 0.5f) / 1f);
                if (!HasViableHuntingStrategy(attacker))
                {
                    return 0f;
                }

                float legacyAdvantage = attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f);
                return Clamp01(legacyAdvantage * attacker.Aggression * legacyDiet);
            }

            float successChance = Clamp01(attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f));
            float expectedGain = defender.EnergyCapacity * attacker.MeatYieldMultiplier * successChance;
            float expectedInjuryCost = defender.AttackPower * (1f - successChance) * InjuryCostScale;
            float expectedPursuitCost = PursuitCostPerDistance * distance;
            float netEnergyValue = expectedGain - expectedInjuryCost - expectedPursuitCost;
            return Clamp01(netEnergyValue / NormalizingEnergyScale) * attacker.Aggression;
        }

        /// <summary>
        /// How much a defender's hit chance is reduced by actually fleeing, as a multiplier on the
        /// attacker's chance to land the blow. Returns 1 when <paramref name="strength"/> is 0, so
        /// the flag-off path is exactly the old arithmetic.
        ///
        /// <para>Scaled by the defender's <c>Maneuverability</c> so that agility only pays if it is
        /// <i>used</i>: the same gene already sits passively in the resistance denominator, and this
        /// is the term that requires a decision behind it.</para>
        ///
        /// <para><b>Note the scale.</b> <c>Phenotype.Maneuverability</c> is <c>1 + 2 * gene</c>, so
        /// it runs <b>1.0 to 3.0</b>, not 0 to 1. No floor term is needed for a creature with no
        /// agility to still gain from running - the phenotype minimum is already 1. The first draft
        /// of this method added a <c>0.5f</c> offset on the assumption of a 0-1 gene and cut hit
        /// chance by about 70% instead of the intended 50%; the unit test caught it.</para>
        /// </summary>
        public static float FleeEvasionMultiplier(Phenotype defender, float strength)
        {
            if (strength <= 0f)
            {
                return 1f;
            }

            return 1f / (1f + (strength * defender.Maneuverability));
        }

        public static bool HasViableHuntingStrategy(Phenotype phenotype)
        {
            float diet = Clamp01((phenotype.MeatYieldMultiplier - 0.5f) / 1f);
            return diet >= MinimumHuntingDiet && phenotype.Aggression >= MinimumHuntingAggression;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}

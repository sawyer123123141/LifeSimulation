using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Behavior
{
    public static class PredationSystem
    {
        private const float MinimumHuntingDiet = 0.58f;
        private const float MinimumHuntingAggression = 0.35f;

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return Decide(needs, self, other, otherObservation, survivalDecision, ref ignoredDiagnostics);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype self,
            Phenotype other,
            CreatureObservation otherObservation,
            CreatureDecision survivalDecision,
            ref DecisionDiagnostics diagnostics)
        {
            if (!otherObservation.IsValid)
            {
                return survivalDecision;
            }

            float distanceAvailability = 1f / (1f + otherObservation.Distance);
            float hunger = 1f - (needs.Energy / self.EnergyCapacity);
            float threat = Threat(other, self) * self.FearResponse * distanceAvailability;
            float hunt = HuntCapability(self, other) * hunger * distanceAvailability;
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

        public static float Threat(Phenotype attacker, Phenotype defender)
        {
            if (HuntCapability(attacker, defender) <= 0f)
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

        public static float HuntCapability(Phenotype attacker, Phenotype defender)
        {
            float diet = Clamp01((attacker.MeatYieldMultiplier - 0.5f) / 1f);
            if (!HasViableHuntingStrategy(attacker))
            {
                return 0f;
            }

            float advantage = attacker.AttackPower / (attacker.AttackPower + defender.Defense + (0.25f * defender.Maneuverability) + 0.01f);
            return Clamp01(advantage * attacker.Aggression * diet);
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

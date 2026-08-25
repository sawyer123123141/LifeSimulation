using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Simulation.Behavior
{
    public static class ThermoregulationSystem
    {
        private const float ComfortableTemperature = 20f;

        public static CreatureDecision PreferThermalComfort(
            Phenotype phenotype,
            SimVector2 position,
            long tick,
            CreatureDecision currentDecision,
            ClimateField climate = default)
        {
            DecisionDiagnostics ignoredDiagnostics = default;
            return PreferThermalComfort(phenotype, position, tick, currentDecision, ref ignoredDiagnostics, climate);
        }

        public static CreatureDecision PreferThermalComfort(
            Phenotype phenotype,
            SimVector2 position,
            long tick,
            CreatureDecision currentDecision,
            ref DecisionDiagnostics diagnostics,
            ClimateField climate = default)
        {
            float discomfort = Math.Max(0f, Math.Abs(climate.Celsius(position, tick) - ComfortableTemperature) - phenotype.TemperatureTolerance);
            float score = discomfort / 8f;
            diagnostics = diagnostics.WithThermalScore(score);
            return score > currentDecision.Score && score >= 0.15f
                ? new CreatureDecision(CreatureAction.SeekThermalComfort, -1, score)
                : currentDecision;
        }

        public static float ScoreThermalComfort(Phenotype phenotype, SimVector2 position, long tick, ClimateField climate = default)
        {
            float discomfort = Math.Max(0f, Math.Abs(climate.Celsius(position, tick) - ComfortableTemperature) - phenotype.TemperatureTolerance);
            return discomfort / 8f;
        }

        public static SimVector2 FindNearbyComfortTarget(SimVector2 position, long tick, ArenaBounds arena, ClimateField climate = default)
        {
            SimVector2 best = position;
            float bestDiscomfort = DiscomfortAt(position, tick, climate);
            const float sampleDistance = 5f;
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X + sampleDistance, position.Y)), tick, climate, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X - sampleDistance, position.Y)), tick, climate, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X, position.Y + sampleDistance)), tick, climate, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X, position.Y - sampleDistance)), tick, climate, ref best, ref bestDiscomfort);

            return best;
        }

        private static float DiscomfortAt(SimVector2 position, long tick, ClimateField climate)
        {
            return Math.Abs(climate.Celsius(position, tick) - ComfortableTemperature);
        }

        private static void EvaluateCandidate(SimVector2 candidate, long tick, ClimateField climate, ref SimVector2 best, ref float bestDiscomfort)
        {
            float discomfort = DiscomfortAt(candidate, tick, climate);
            if (discomfort < bestDiscomfort)
            {
                best = candidate;
                bestDiscomfort = discomfort;
            }
        }
    }
}

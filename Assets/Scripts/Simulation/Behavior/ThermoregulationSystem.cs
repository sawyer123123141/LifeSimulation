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
            CreatureDecision currentDecision)
        {
            float score = ScoreThermalComfort(phenotype, position, tick);
            return score > currentDecision.Score && score >= 0.15f
                ? new CreatureDecision(CreatureAction.SeekThermalComfort, -1, score)
                : currentDecision;
        }

        public static float ScoreThermalComfort(Phenotype phenotype, SimVector2 position, long tick)
        {
            float discomfort = Math.Max(0f, Math.Abs(TemperatureField.Sample(position, tick) - ComfortableTemperature) - phenotype.TemperatureTolerance);
            return discomfort / 8f;
        }

        public static SimVector2 FindNearbyComfortTarget(SimVector2 position, long tick, ArenaBounds arena)
        {
            SimVector2 best = position;
            float bestDiscomfort = DiscomfortAt(position, tick);
            const float sampleDistance = 5f;
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X + sampleDistance, position.Y)), tick, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X - sampleDistance, position.Y)), tick, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X, position.Y + sampleDistance)), tick, ref best, ref bestDiscomfort);
            EvaluateCandidate(arena.Clamp(new SimVector2(position.X, position.Y - sampleDistance)), tick, ref best, ref bestDiscomfort);

            return best;
        }

        private static float DiscomfortAt(SimVector2 position, long tick)
        {
            return Math.Abs(TemperatureField.Sample(position, tick) - ComfortableTemperature);
        }

        private static void EvaluateCandidate(SimVector2 candidate, long tick, ref SimVector2 best, ref float bestDiscomfort)
        {
            float discomfort = DiscomfortAt(candidate, tick);
            if (discomfort < bestDiscomfort)
            {
                best = candidate;
                bestDiscomfort = discomfort;
            }
        }
    }
}

using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Behavior
{
    public static class HomeRangeSystem
    {
        public static void RecordSuccess(ref HomeRangeState state, SimVector2 position)
        {
            float centreX = state.Centre.X + ((position.X - state.Centre.X) * SimulationConfig.DefaultHomeRangeLearningFraction);
            float centreY = state.Centre.Y + ((position.Y - state.Centre.Y) * SimulationConfig.DefaultHomeRangeLearningFraction);
            state.Centre = new SimVector2(centreX, centreY);
            state.Familiarity = Math.Min(1f, state.Familiarity + SimulationConfig.DefaultHomeRangeFamiliarityGain);
        }

        public static void TickDecay(ref HomeRangeState state, float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            float familiarityLoss = deltaTime * SimulationConfig.DefaultHomeRangeFamiliarityDecayPerSecond;
            state.Familiarity = Math.Max(0f, state.Familiarity - familiarityLoss);
        }

        public static float GetCandidateBonus(HomeRangeState state, SimVector2 candidatePosition)
        {
            if (state.Familiarity <= 0f)
            {
                return 0f;
            }

            float distance = SimVector2.Distance(state.Centre, candidatePosition);
            float proximity = Math.Max(0f, 1f - (distance / SimulationConfig.DefaultHomeRangeBonusFalloffDistance));
            float bonus = SimulationConfig.DefaultHomeRangeBonusMaximum * state.Familiarity * proximity;
            return Math.Min(SimulationConfig.DefaultHomeRangeBonusMaximum, bonus);
        }
    }
}

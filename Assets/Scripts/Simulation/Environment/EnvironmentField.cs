using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public readonly struct EnvironmentSample
    {
        public EnvironmentSample(float moisture, float fertility, float temperature)
        {
            Moisture = moisture;
            Fertility = fertility;
            Temperature = temperature;
        }

        public float Moisture { get; }
        public float Fertility { get; }
        public float Temperature { get; }
    }

    public sealed class EnvironmentField
    {
        private readonly EnvironmentSample _constantSample;
        private readonly bool _usesMoistureGradient;

        public EnvironmentField(float moisture = 1f, float fertility = 1f, float temperature = 1f)
        {
            _constantSample = new EnvironmentSample(moisture, fertility, temperature);
        }

        private EnvironmentField(bool usesMoistureGradient)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesMoistureGradient = usesMoistureGradient;
        }

        public static EnvironmentField CreateMoistureGradient() { return new EnvironmentField(true); }

        public EnvironmentSample Sample(SimVector2 position)
        {
            if (!_usesMoistureGradient) return _constantSample;
            float moisture = .25f + (.75f * Clamp01((position.X + 25f) / 50f));
            return new EnvironmentSample(moisture, 1f, 1f);
        }

        private static float Clamp01(float value) { return value < 0f ? 0f : value > 1f ? 1f : value; }
    }
}

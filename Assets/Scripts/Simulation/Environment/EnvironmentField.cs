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

        public EnvironmentField(float moisture = 1f, float fertility = 1f, float temperature = 1f)
        {
            _constantSample = new EnvironmentSample(moisture, fertility, temperature);
        }

        public EnvironmentSample Sample(SimVector2 position) { return _constantSample; }
    }
}

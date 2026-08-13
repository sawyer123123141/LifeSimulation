using System;

namespace LifeSimulation.Simulation.Core
{
    public readonly struct SimulationSchedule
    {
        public SimulationSchedule(
            int baseFrequencyHz,
            int movementHz,
            int perceptionHz,
            int needsHz,
            int decisionsHz,
            int resourcesHz,
            int reproductionHz,
            int statisticsHz)
        {
            BaseFrequencyHz = baseFrequencyHz;
            MovementHz = movementHz;
            PerceptionHz = perceptionHz;
            NeedsHz = needsHz;
            DecisionsHz = decisionsHz;
            ResourcesHz = resourcesHz;
            ReproductionHz = reproductionHz;
            StatisticsHz = statisticsHz;
        }

        public int BaseFrequencyHz { get; }
        public int MovementHz { get; }
        public int PerceptionHz { get; }
        public int NeedsHz { get; }
        public int DecisionsHz { get; }
        public int ResourcesHz { get; }
        public int ReproductionHz { get; }
        public int StatisticsHz { get; }
    }

    public sealed class SimulationConfig
    {
        public SimulationConfig(int worldSeed, int initialPopulation, SimulationSchedule schedule)
        {
            WorldSeed = worldSeed;
            InitialPopulation = initialPopulation;
            Schedule = schedule;
        }

        public int WorldSeed { get; }
        public int InitialPopulation { get; }
        public SimulationSchedule Schedule { get; }
        public float FixedDeltaTime => 1f / Schedule.BaseFrequencyHz;

        public static SimulationConfig CreatePrototype1Defaults(int worldSeed, int initialPopulation)
        {
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                new SimulationSchedule(20, 20, 4, 2, 2, 1, 1, 1));
        }

        public void Validate()
        {
            if (InitialPopulation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(InitialPopulation), "Founder population cannot be negative.");
            }

            if (Schedule.BaseFrequencyHz <= 0)
            {
                throw new ArgumentException("Base simulation frequency must be positive.", nameof(Schedule));
            }

            ValidateScheduledFrequency(Schedule.MovementHz, nameof(Schedule.MovementHz));
            ValidateScheduledFrequency(Schedule.PerceptionHz, nameof(Schedule.PerceptionHz));
            ValidateScheduledFrequency(Schedule.NeedsHz, nameof(Schedule.NeedsHz));
            ValidateScheduledFrequency(Schedule.DecisionsHz, nameof(Schedule.DecisionsHz));
            ValidateScheduledFrequency(Schedule.ResourcesHz, nameof(Schedule.ResourcesHz));
            ValidateScheduledFrequency(Schedule.ReproductionHz, nameof(Schedule.ReproductionHz));
            ValidateScheduledFrequency(Schedule.StatisticsHz, nameof(Schedule.StatisticsHz));
        }

        private void ValidateScheduledFrequency(int frequencyHz, string name)
        {
            if (frequencyHz <= 0 || frequencyHz > Schedule.BaseFrequencyHz || Schedule.BaseFrequencyHz % frequencyHz != 0)
            {
                throw new ArgumentException(
                    $"{name} must be a positive integer divisor of the base frequency.",
                    nameof(Schedule));
            }
        }
    }
}

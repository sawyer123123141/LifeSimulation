using System;

namespace LifeSimulation.Simulation.Core
{
    public enum FounderProfile : byte
    {
        Prototype1 = 0,
        PredationVariation = 1,
        CognitionVariation = 2,
        PhysiologyVariation = 3,
    }

    public enum DecisionPolicyVersion : byte
    {
        Legacy = 0,
        IntentUtilityV1 = 1,
    }

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
        public SimulationConfig(
            int worldSeed,
            int initialPopulation,
            SimulationSchedule schedule,
            int maximumPopulation = 1000,
            FounderProfile founderProfile = FounderProfile.Prototype1,
            bool cognitionEnabled = false,
            bool physiologyEnabled = false,
            DecisionPolicyVersion decisionPolicyVersion = DecisionPolicyVersion.Legacy)
        {
            WorldSeed = worldSeed;
            InitialPopulation = initialPopulation;
            Schedule = schedule;
            MaximumPopulation = maximumPopulation;
            FounderProfile = founderProfile;
            CognitionEnabled = cognitionEnabled;
            PhysiologyEnabled = physiologyEnabled;
            DecisionPolicyVersion = decisionPolicyVersion;
        }

        public int WorldSeed { get; }
        public int InitialPopulation { get; }
        public int MaximumPopulation { get; }
        public FounderProfile FounderProfile { get; }
        public bool CognitionEnabled { get; }
        public bool PhysiologyEnabled { get; }
        public DecisionPolicyVersion DecisionPolicyVersion { get; }
        public SimulationSchedule Schedule { get; }
        public float FixedDeltaTime => 1f / Schedule.BaseFrequencyHz;

        public static SimulationConfig CreatePrototype1Defaults(int worldSeed, int initialPopulation)
        {
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                new SimulationSchedule(20, 20, 4, 2, 2, 1, 1, 1),
                maximumPopulation: 1000);
        }

        public static SimulationConfig CreatePrototype2Defaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype1Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                defaults.Schedule,
                defaults.MaximumPopulation,
                FounderProfile.CognitionVariation,
                cognitionEnabled: true);
        }

        public static SimulationConfig CreatePrototype3Defaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype2Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(worldSeed, initialPopulation, defaults.Schedule, defaults.MaximumPopulation, FounderProfile.PhysiologyVariation, cognitionEnabled: true, physiologyEnabled: true);
        }

        public void Validate()
        {
            if (InitialPopulation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(InitialPopulation), "Founder population cannot be negative.");
            }

            if (MaximumPopulation < InitialPopulation)
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumPopulation), "Population limit cannot be lower than the founder population.");
            }

            if (!Enum.IsDefined(typeof(FounderProfile), FounderProfile))
            {
                throw new ArgumentOutOfRangeException(nameof(FounderProfile));
            }

            if (!Enum.IsDefined(typeof(DecisionPolicyVersion), DecisionPolicyVersion))
            {
                throw new ArgumentOutOfRangeException(nameof(DecisionPolicyVersion));
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

using System;

namespace LifeSimulation.Simulation.Experiments
{
    public readonly struct ExperimentBatchOptions
    {
        public ExperimentBatchOptions(int firstSeed, int seedCount, int founderPopulation, int ticks)
        {
            if (seedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seedCount));
            }

            if (founderPopulation <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(founderPopulation));
            }

            if (ticks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks));
            }

            FirstSeed = firstSeed;
            SeedCount = seedCount;
            FounderPopulation = founderPopulation;
            Ticks = ticks;
        }

        public int FirstSeed { get; }
        public int SeedCount { get; }
        public int FounderPopulation { get; }
        public int Ticks { get; }

        public static ExperimentBatchOptions Default => new ExperimentBatchOptions(
            firstSeed: 42,
            seedCount: 5,
            founderPopulation: 50,
            ticks: 20000);

        public static ExperimentBatchOptions Parse(string[] arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            ExperimentBatchOptions defaults = Default;
            int firstSeed = defaults.FirstSeed;
            int seedCount = defaults.SeedCount;
            int founderPopulation = defaults.FounderPopulation;
            int ticks = defaults.Ticks;
            for (int index = 0; index < arguments.Length; index++)
            {
                switch (arguments[index])
                {
                    case "-lifeSimFirstSeed":
                        firstSeed = ReadValue(arguments, ref index, "-lifeSimFirstSeed");
                        break;
                    case "-lifeSimSeedCount":
                        seedCount = ReadValue(arguments, ref index, "-lifeSimSeedCount");
                        break;
                    case "-lifeSimFounders":
                        founderPopulation = ReadValue(arguments, ref index, "-lifeSimFounders");
                        break;
                    case "-lifeSimTicks":
                        ticks = ReadValue(arguments, ref index, "-lifeSimTicks");
                        break;
                }
            }

            return new ExperimentBatchOptions(firstSeed, seedCount, founderPopulation, ticks);
        }

        private static int ReadValue(string[] arguments, ref int optionIndex, string optionName)
        {
            int valueIndex = optionIndex + 1;
            if (valueIndex >= arguments.Length || !int.TryParse(arguments[valueIndex], out int value))
            {
                throw new ArgumentException($"{optionName} requires an integer value.", nameof(arguments));
            }

            optionIndex = valueIndex;
            return value;
        }
    }
}

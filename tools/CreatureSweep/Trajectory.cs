using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Tools
{
    /// <summary>
    /// The population over the run, not only at the end of it.
    ///
    /// <para><b>Why this exists.</b> The cap-500 / brake-1.0 cell was recorded on 2026-08-30 as 22 of
    /// 24 worlds surviving at 12,000 ticks and was found on 2026-09-03 to be extinct in 21 of 24 at
    /// 36,000. Nothing biological changed between those readings and nothing was tuned: the run was
    /// simply three times longer. The reason a whole session's conclusions were labelled a steady
    /// state is that <b>the sweeps print the final population and nothing else</b>, so a population
    /// falling steeply and a population sitting still produce the same artefact.</para>
    ///
    /// <para>A falling world announces itself in three numbers. This samples nine evenly spaced
    /// points and reports them, so the distinction costs no extra run at all.</para>
    ///
    /// <para><b>Nine points, not three.</b> The thirds are what a reader wants; the ninths are what
    /// the persistence criterion needs, because "no monotone downward trend over the last third" is
    /// not a statement two samples can carry.</para>
    ///
    /// <para>Read-only. It calls <see cref="SimulationWorld.CaptureStatistics"/> between steps and
    /// writes nothing, so an arm measured with it is bit-identical to one measured without it.</para>
    /// </summary>
    internal sealed class Trajectory
    {
        public const int SampleCount = 9;

        // Column indices into _plant. Patches, seed-eligible patches and biomass are levels; growth,
        // offtake, mortality loss and biomass-seconds are cumulative and are differenced per interval.
        private const int PlantPatches = 0;
        private const int PlantSeedEligible = 1;
        private const int PlantBiomass = 2;
        private const int PlantGrowth = 3;
        private const int PlantOfftake = 4;
        private const int PlantMortality = 5;
        private const int PlantBiomassSeconds = 6;
        private const int PlantColumnCount = 7;

        private readonly int[] _boundaries = new int[SampleCount];
        private readonly int[] _population = new int[SampleCount];
        private readonly double[] _energy = new double[SampleCount];
        private readonly long[,] _deaths = new long[SampleCount, 5];
        private readonly double[,] _plant = new double[SampleCount, PlantColumnCount];
        private int _taken;

        public Trajectory(int ticks)
        {
            if (ticks < SampleCount) throw new ArgumentOutOfRangeException(nameof(ticks));
            for (int index = 0; index < SampleCount; index++)
            {
                // Long division deliberately: the last boundary must land exactly on the final tick,
                // or the end-of-run row disagrees with every other number the sweep prints.
                _boundaries[index] = (int)((long)ticks * (index + 1) / SampleCount);
            }
        }

        /// <summary>Call once after every <c>Step</c>, with the index of the step just taken.</summary>
        public void Observe(int tick, SimulationWorld world)
        {
            if (_taken >= SampleCount || tick + 1 != _boundaries[_taken]) return;

            SimulationStatistics statistics = world.CaptureStatistics();
            _population[_taken] = world.CreatureCount;
            _energy[_taken] = world.CreatureCount == 0 ? double.NaN : statistics.MeanEnergyFraction;
            _deaths[_taken, 0] = statistics.StarvationDeathCount;
            _deaths[_taken, 1] = statistics.DehydrationDeathCount;
            _deaths[_taken, 2] = statistics.AgeDeathCount;
            _deaths[_taken, 3] = statistics.HealthDeathCount;
            _deaths[_taken, 4] = statistics.PredationDeathCount;
            _plant[_taken, PlantPatches] = statistics.ActivePlantPatchCount;
            _plant[_taken, PlantSeedEligible] = statistics.SeedEligiblePlantPatchCount;
            _plant[_taken, PlantBiomass] = statistics.TotalPlantBiomass;
            _plant[_taken, PlantGrowth] = statistics.CumulativePlantGrowth;
            _plant[_taken, PlantOfftake] = statistics.CumulativePlantBiomassConsumed;
            _plant[_taken, PlantMortality] = statistics.CumulativePlantBiomassLostToMortality;
            _plant[_taken, PlantBiomassSeconds] = statistics.PlantBiomassSeconds;
            _taken++;
        }

        public static void Report(IReadOnlyList<Trajectory> runs, int ticks)
        {
            if (runs.Count == 0) return;

            Console.WriteLine();
            Console.WriteLine("population trajectory over " + ticks + " ticks, " + runs.Count + " runs");
            Console.WriteLine("  point     tick    alive    pop(all)  pop(alive)  energy | deaths within the interval");
            Console.WriteLine("                                                          |  starv  dehyd    age health   pred");

            var allWorldMeans = new double[SampleCount];
            for (int point = 0; point < SampleCount; point++)
            {
                int alive = 0;
                double populationTotal = 0d;
                double aliveTotal = 0d;
                double energyTotal = 0d;
                int energyCount = 0;
                var interval = new long[5];

                foreach (Trajectory run in runs)
                {
                    populationTotal += run._population[point];
                    if (run._population[point] > 0)
                    {
                        alive++;
                        aliveTotal += run._population[point];
                    }

                    if (!double.IsNaN(run._energy[point]))
                    {
                        energyTotal += run._energy[point];
                        energyCount++;
                    }

                    for (int cause = 0; cause < 5; cause++)
                    {
                        long before = point == 0 ? 0L : run._deaths[point - 1, cause];
                        interval[cause] += run._deaths[point, cause] - before;
                    }
                }

                allWorldMeans[point] = populationTotal / runs.Count;
                long intervalTotal = 0L;
                foreach (long count in interval) intervalTotal += count;

                string label = (point + 1) + "/" + SampleCount;
                if ((point + 1) % 3 == 0) label = ((point + 1) / 3) + "/3 " + label;

                Console.WriteLine("  " + label.PadRight(9)
                    + ((long)ticks * (point + 1) / SampleCount).ToString().PadLeft(6)
                    + (alive + "/" + runs.Count).PadLeft(9)
                    + allWorldMeans[point].ToString("0.0").PadLeft(12)
                    + (alive == 0 ? "-" : (aliveTotal / alive).ToString("0.0")).PadLeft(12)
                    + (energyCount == 0 ? "-" : (energyTotal / energyCount).ToString("0.000")).PadLeft(8)
                    + " |" + Share(interval[0], intervalTotal) + Share(interval[1], intervalTotal)
                    + Share(interval[2], intervalTotal) + Share(interval[3], intervalTotal)
                    + Share(interval[4], intervalTotal));
            }

            // The verdict line. `pop(alive)` rises as a cell collapses - the worlds that die stop
            // contributing to it - so the trend is read off the all-world mean, which is the honest
            // aggregate when extinction is the thing being looked for.
            bool falling = true;
            for (int point = SampleCount - 3; point < SampleCount - 1; point++)
            {
                if (allWorldMeans[point + 1] < allWorldMeans[point]) continue;
                falling = false;
                break;
            }

            double first = allWorldMeans[SampleCount - 3];
            double last = allWorldMeans[SampleCount - 1];
            string change = first <= 0d ? "n/a" : (100d * (last - first) / first).ToString("+0.0;-0.0") + "%";
            Console.WriteLine("  last third: all-world mean population " + first.ToString("0.0") + " -> "
                + last.ToString("0.0") + " (" + change + "), monotone decline "
                + (falling ? "YES" : "no"));
            Console.WriteLine("  a rising pop(alive) beside a falling pop(all) is survivorship, not recovery.");

            ReportPlants(runs, ticks);
        }

        /// <summary>
        /// The producer side of the same nine samples.
        ///
        /// <para><b>Why this is a second table.</b> Every trajectory recorded before 2026-09-06
        /// reports the consumer alone, so a plant community being grazed to sterility and one sitting
        /// untouched produce the same artefact - the same shape of blindness the population table was
        /// added to close. The population table above is unchanged so that older artefacts still diff
        /// against it; this is appended.</para>
        ///
        /// <para><b>`seed-elig` is the one to read.</b> Seeding is an all-or-nothing threshold, so a
        /// community at full occupancy every patch of which is grazed below it is reproductively dead
        /// while `patches` still reads full.</para>
        ///
        /// <para><b>`growth` and `offtake` are rates, not totals</b>, each normalized by the biomass
        /// integral accumulated over the same interval, so they are per unit standing biomass per
        /// second and are comparable to each other across intervals of different size. `offtake/growth`
        /// is the ratio of the two: at or above 1 the consumers are taking everything the producers
        /// make, which is production limitation. Well below 1 while creatures starve is not.</para>
        /// </summary>
        private static void ReportPlants(IReadOnlyList<Trajectory> runs, int ticks)
        {
            bool anyPlants = false;
            foreach (Trajectory run in runs)
            {
                for (int point = 0; point < SampleCount && !anyPlants; point++)
                {
                    if (run._plant[point, PlantPatches] > 0d) anyPlants = true;
                }
            }

            if (!anyPlants) return;

            Console.WriteLine();
            Console.WriteLine("plant trajectory over " + ticks + " ticks, " + runs.Count + " runs (all-world means)");
            Console.WriteLine("  point     tick  patches seed-elig    biomass | within the interval, per biomass-second");
            Console.WriteLine("                                              |   growth  offtake   mortal  offtake/growth");

            for (int point = 0; point < SampleCount; point++)
            {
                double patches = 0d;
                double seedEligible = 0d;
                double biomass = 0d;
                double growth = 0d;
                double offtake = 0d;
                double mortality = 0d;
                double biomassSeconds = 0d;

                foreach (Trajectory run in runs)
                {
                    patches += run._plant[point, PlantPatches];
                    seedEligible += run._plant[point, PlantSeedEligible];
                    biomass += run._plant[point, PlantBiomass];
                    growth += Interval(run, point, PlantGrowth);
                    offtake += Interval(run, point, PlantOfftake);
                    mortality += Interval(run, point, PlantMortality);
                    biomassSeconds += Interval(run, point, PlantBiomassSeconds);
                }

                string label = (point + 1) + "/" + SampleCount;
                if ((point + 1) % 3 == 0) label = ((point + 1) / 3) + "/3 " + label;

                Console.WriteLine("  " + label.PadRight(9)
                    + ((long)ticks * (point + 1) / SampleCount).ToString().PadLeft(6)
                    + (patches / runs.Count).ToString("0.0").PadLeft(9)
                    + (seedEligible / runs.Count).ToString("0.0").PadLeft(10)
                    + (biomass / runs.Count).ToString("0.0").PadLeft(11)
                    + " |" + Rate(growth, biomassSeconds) + Rate(offtake, biomassSeconds)
                    + Rate(mortality, biomassSeconds)
                    + (growth <= 0d ? "-" : (offtake / growth).ToString("0.000")).PadLeft(16));
            }

            Console.WriteLine("  seed-elig is patches at or above the seeding threshold; at zero the community cannot recruit.");
        }

        private static double Interval(Trajectory run, int point, int column)
        {
            double before = point == 0 ? 0d : run._plant[point - 1, column];
            return run._plant[point, column] - before;
        }

        private static string Rate(double amount, double biomassSeconds)
        {
            string text = biomassSeconds <= 0d ? "-" : (amount / biomassSeconds).ToString("0.0000");
            return text.PadLeft(9);
        }

        private static string Share(long count, long total)
        {
            string text = total == 0L ? "-" : (100d * count / total).ToString("0.0") + "%";
            return text.PadLeft(7);
        }
    }
}

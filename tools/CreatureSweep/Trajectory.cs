using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        /// <summary>
        /// Nine, which is what every artefact recorded before 2026-09-10 used at every run length.
        /// **It is the default and not a rule.** Every existing call site omits the sample count and
        /// therefore reproduces byte-identically; the extension passes one explicitly.
        /// </summary>
        public const int DefaultSampleCount = 9;

        /// <summary>
        /// Sample spacing, in ticks, of the 24,000-tick runs the recorded corpus is written against:
        /// 24,000 / 9. Quoted so a longer run can be given a sample count that holds spacing rather
        /// than holding count, which is the difference between resolving a crash and averaging over
        /// it - the graded-seeding arm's entire crash occupies one sample at this spacing.
        /// </summary>
        public const double RecordedSampleSpacingTicks = 24000d / DefaultSampleCount;

        /// <summary>Sample count that holds <see cref="RecordedSampleSpacingTicks"/> over <paramref name="ticks"/>.</summary>
        public static int SampleCountForRecordedSpacing(int ticks)
        {
            if (ticks < 1) throw new ArgumentOutOfRangeException(nameof(ticks));
            return Math.Max(1, (int)Math.Round(ticks / RecordedSampleSpacingTicks, MidpointRounding.AwayFromZero));
        }

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

        private readonly int[] _boundaries;
        private readonly int[] _population;
        private readonly double[] _energy;
        private readonly long[,] _deaths;
        private readonly double[,] _plant;
        private readonly ulong[] _behaviorHash;
        private readonly bool[] _atCap;
        private int _taken;

        /// <summary>How many samples this run takes. Instance state, not a constant, since 2026-09-10.</summary>
        public int SampleCount { get; }

        /// <summary>Which arm this run belongs to, for the per-seed longitudinal file. Empty when unset.</summary>
        public string Arm { get; }

        /// <summary>The world seed, so a row identifies the run that produced it.</summary>
        public int Seed { get; }

        /// <summary>Total ticks the run was constructed for.</summary>
        public int Ticks { get; }

        public Trajectory(int ticks, int sampleCount = DefaultSampleCount, string arm = "", int seed = 0)
        {
            if (sampleCount < 1) throw new ArgumentOutOfRangeException(nameof(sampleCount));
            if (ticks < sampleCount) throw new ArgumentOutOfRangeException(nameof(ticks));

            SampleCount = sampleCount;
            Ticks = ticks;
            Arm = arm ?? string.Empty;
            Seed = seed;

            _boundaries = new int[sampleCount];
            _population = new int[sampleCount];
            _energy = new double[sampleCount];
            _deaths = new long[sampleCount, 5];
            _plant = new double[sampleCount, PlantColumnCount];
            _behaviorHash = new ulong[sampleCount];
            _atCap = new bool[sampleCount];

            for (int index = 0; index < sampleCount; index++)
            {
                // Long division deliberately: the last boundary must land exactly on the final tick,
                // or the end-of-run row disagrees with every other number the sweep prints.
                _boundaries[index] = (int)((long)ticks * (index + 1) / sampleCount);
            }
        }

        /// <summary>
        /// The tick the given sample closes on. Public so the sample grid can be checked without
        /// running the ticks - a 72,000-tick run is not something a unit test should have to do to
        /// find out where its boundaries fall.
        /// </summary>
        public int BoundaryAt(int index)
        {
            if (index < 0 || index >= SampleCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _boundaries[index];
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

            // Recorded per sample rather than once at the end, so a longitudinal file proves
            // determinism at every point of the run instead of only at its last tick.
            _behaviorHash[_taken] = world.ComputeBehaviorHash();

            // Cap contact is a property of the sample, not of the ending: a world that sat on the
            // ceiling for half the run and fell off it before the horizon reports no contact at the
            // end and is exactly the world a persistence claim must not rest on.
            _atCap[_taken] = world.CreatureCount >= world.Config.MaximumPopulation;
            _taken++;
        }

        public static void Report(IReadOnlyList<Trajectory> runs, int ticks)
        {
            if (runs.Count == 0) return;
            int sampleCount = RequireUniformSampleCount(runs);

            Console.WriteLine();
            Console.WriteLine("population trajectory over " + ticks + " ticks, " + runs.Count + " runs");
            Console.WriteLine("  point     tick    alive    pop(all)  pop(alive)  energy | deaths within the interval");
            Console.WriteLine("                                                          |  starv  dehyd    age health   pred");

            var allWorldMeans = new double[sampleCount];
            for (int point = 0; point < sampleCount; point++)
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

                string label = ThirdsLabel(point, sampleCount);

                Console.WriteLine("  " + label.PadRight(9)
                    + ((long)ticks * (point + 1) / sampleCount).ToString().PadLeft(6)
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
            for (int point = sampleCount - 3; point < sampleCount - 1; point++)
            {
                if (allWorldMeans[point + 1] < allWorldMeans[point]) continue;
                falling = false;
                break;
            }

            double first = allWorldMeans[sampleCount - 3];
            double last = allWorldMeans[sampleCount - 1];
            string change = first <= 0d ? "n/a" : (100d * (last - first) / first).ToString("+0.0;-0.0") + "%";

            // DELIBERATELY the last three SAMPLES, at every sample count, which is the arithmetic the
            // persistence criterion was declared on and every recorded verdict was read with. At nine
            // samples that is the last third and the line says so, byte-identically to every artefact
            // recorded before 2026-09-10. At any other sample count it is NOT a third, and calling it
            // one would silently redefine the criterion - a monotone decline over nine points is a far
            // stricter test than over three, and stricter in the direction that clears cells. The span
            // is printed instead, and a longer run must declare in writing which reading governs.
            string span = sampleCount == DefaultSampleCount
                ? "last third"
                : "last 3 samples (tick " + ((long)ticks * (sampleCount - 2) / sampleCount) + "-" + ticks + ")";
            Console.WriteLine("  " + span + ": all-world mean population " + first.ToString("0.0") + " -> "
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
            int sampleCount = RequireUniformSampleCount(runs);
            bool anyPlants = false;
            foreach (Trajectory run in runs)
            {
                for (int point = 0; point < sampleCount && !anyPlants; point++)
                {
                    if (run._plant[point, PlantPatches] > 0d) anyPlants = true;
                }
            }

            if (!anyPlants) return;

            Console.WriteLine();
            Console.WriteLine("plant trajectory over " + ticks + " ticks, " + runs.Count + " runs (all-world means)");
            Console.WriteLine("  point     tick  patches seed-elig    biomass | within the interval, per biomass-second");
            Console.WriteLine("                                              |   growth  offtake   mortal  offtake/growth");

            for (int point = 0; point < sampleCount; point++)
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

                string label = ThirdsLabel(point, sampleCount);

                Console.WriteLine("  " + label.PadRight(9)
                    + ((long)ticks * (point + 1) / sampleCount).ToString().PadLeft(6)
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

        /// <summary>
        /// Every run in one report must have been sampled the same way, or the columns are not
        /// comparable and the thirds do not line up. Fails loudly rather than reporting a mixture.
        /// </summary>
        private static int RequireUniformSampleCount(IReadOnlyList<Trajectory> runs)
        {
            int sampleCount = runs[0].SampleCount;
            for (int index = 1; index < runs.Count; index++)
            {
                if (runs[index].SampleCount == sampleCount) continue;
                throw new ArgumentException(
                    "trajectories in one report must share a sample count; found "
                    + sampleCount + " and " + runs[index].SampleCount, nameof(runs));
            }

            return sampleCount;
        }

        /// <summary>
        /// <c>k/n</c>, with a <c>t/3</c> prefix on the samples that close each third. At
        /// <see cref="DefaultSampleCount"/> this is identical to the labelling of every recorded
        /// artefact; at any other count it still marks thirds rather than every third sample.
        /// </summary>
        private static string ThirdsLabel(int point, int sampleCount)
        {
            string label = (point + 1) + "/" + sampleCount;
            long scaled = (long)(point + 1) * 3;
            if (scaled % sampleCount == 0) label = (scaled / sampleCount) + "/3 " + label;
            return label;
        }

        /// <summary>
        /// Final population of every run, <b>extinct worlds included as zero</b>.
        ///
        /// <para><b>Why this exists.</b> <c>Deaths.Report</c> built its five-number summary from
        /// surviving worlds only - it skips an extinct run before adding to the sample - so a cell
        /// that lost four of twenty-four worlds reported a minimum of 32 and no zeros at all, and the
        /// more of it died the healthier its distribution read. That is the survivorship failure this
        /// repository has recorded twice. All-world and survivor-conditioned are both legitimate and
        /// they are different statistics; the caller prints both, labelled.</para>
        /// </summary>
        public static double[] FinalPopulationsAllWorlds(IReadOnlyList<Trajectory> runs)
        {
            var values = new double[runs.Count];
            for (int index = 0; index < runs.Count; index++) values[index] = runs[index].FinalPopulation;
            return values;
        }

        /// <summary>Final population of the runs that still had one. Shorter than <paramref name="runs"/> when any died.</summary>
        public static double[] FinalPopulationsSurvivorsOnly(IReadOnlyList<Trajectory> runs)
        {
            var values = new List<double>(runs.Count);
            foreach (Trajectory run in runs)
            {
                if (run.FinalPopulation > 0d) values.Add(run.FinalPopulation);
            }

            return values.ToArray();
        }

        /// <summary>Population at the last sample, which lands exactly on the final tick.</summary>
        public double FinalPopulation => _taken == 0 ? 0d : _population[SampleCount - 1];

        /// <summary>Highest population this run reached at any sample.</summary>
        public double PeakPopulation
        {
            get
            {
                double peak = 0d;
                for (int point = 0; point < _taken; point++)
                {
                    if (_population[point] > peak) peak = _population[point];
                }

                return peak;
            }
        }

        /// <summary>Samples at which this run sat at or above the population cap.</summary>
        public int CapContactSamples
        {
            get
            {
                int count = 0;
                for (int point = 0; point < _taken; point++)
                {
                    if (_atCap[point]) count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Count, mean, minimum, quartiles, median, maximum and standard deviation over a sample,
        /// formatted for one console line. Quartiles are the medians of the lower and upper halves,
        /// excluding the overall median when the count is odd - the definition is stated because a
        /// quartile reported without one is not reproducible.
        /// </summary>
        public static string FiveNumberSummary(double[] values)
        {
            if (values.Length == 0) return "no runs";

            var sorted = new double[values.Length];
            Array.Copy(values, sorted, values.Length);
            Array.Sort(sorted);

            double total = 0d;
            foreach (double value in sorted) total += value;
            double mean = total / sorted.Length;

            double variance = 0d;
            foreach (double value in sorted) variance += (value - mean) * (value - mean);
            double deviation = sorted.Length < 2 ? 0d : Math.Sqrt(variance / (sorted.Length - 1));

            return "n " + sorted.Length
                + "  mean " + mean.ToString("0.0", CultureInfo.InvariantCulture)
                + "  min " + sorted[0].ToString("0.0", CultureInfo.InvariantCulture)
                + "  q1 " + Median(sorted, 0, sorted.Length / 2).ToString("0.0", CultureInfo.InvariantCulture)
                + "  median " + Median(sorted, 0, sorted.Length).ToString("0.0", CultureInfo.InvariantCulture)
                + "  q3 " + Median(sorted, (sorted.Length + 1) / 2, sorted.Length).ToString("0.0", CultureInfo.InvariantCulture)
                + "  max " + sorted[sorted.Length - 1].ToString("0.0", CultureInfo.InvariantCulture)
                + "  sd " + deviation.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static double Median(double[] sorted, int start, int end)
        {
            int count = end - start;
            if (count <= 0) return double.NaN;
            int middle = start + (count / 2);
            return count % 2 == 1 ? sorted[middle] : 0.5d * (sorted[middle - 1] + sorted[middle]);
        }

        /// <summary>
        /// One row per arm, seed and sample tick - the longitudinal file the aggregates cannot
        /// substitute for.
        ///
        /// <para><b>Why.</b> Every artefact before 2026-09-10 reports all-world means and a
        /// survivor-conditioned five-number line, from which it is impossible to say which seeds
        /// survived, how a survivor's plant community differed from a casualty's, whether the
        /// survivors follow one trajectory or several, or how many worlds touched the cap. Those are
        /// the questions a persistence claim turns on.</para>
        ///
        /// <para><c>extinct_at_end</c> marks the run rather than the row, so a distribution can
        /// include the dead without re-deriving which they were. <c>behavior_hash</c> is recorded at
        /// every sample rather than once at the end, so determinism is provable at every point of the
        /// run instead of only at its last tick.</para>
        /// </summary>
        public static string ToCsv(IReadOnlyList<Trajectory> runs)
        {
            var builder = new StringBuilder();
            builder.Append("arm,seed,sample,sample_count,tick,population,extinct_at_end,at_cap,")
                .Append("peak_population,starvation_deaths,dehydration_deaths,age_deaths,health_deaths,")
                .Append("predation_deaths,starvation_deaths_interval,total_deaths_interval,")
                .Append("plant_patches,seed_eligible_patches,plant_biomass,plant_growth_cumulative,")
                .Append("plant_offtake_cumulative,plant_mortality_cumulative,mean_energy_fraction,")
                .Append("behavior_hash")
                .AppendLine();

            foreach (Trajectory run in runs)
            {
                bool extinctAtEnd = run.FinalPopulation <= 0d;
                for (int point = 0; point < run._taken; point++)
                {
                    long starvationBefore = point == 0 ? 0L : run._deaths[point - 1, 0];
                    long intervalStarvation = run._deaths[point, 0] - starvationBefore;
                    long intervalTotal = 0L;
                    for (int cause = 0; cause < 5; cause++)
                    {
                        long before = point == 0 ? 0L : run._deaths[point - 1, cause];
                        intervalTotal += run._deaths[point, cause] - before;
                    }

                    builder.Append(run.Arm).Append(',')
                        .Append(run.Seed.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append((point + 1).ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run.SampleCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._boundaries[point].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._population[point].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(extinctAtEnd ? "1" : "0").Append(',')
                        .Append(run._atCap[point] ? "1" : "0").Append(',')
                        .Append(run.PeakPopulation.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._deaths[point, 0].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._deaths[point, 1].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._deaths[point, 2].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._deaths[point, 3].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(run._deaths[point, 4].ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(intervalStarvation.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(intervalTotal.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(Number(run._plant[point, PlantPatches])).Append(',')
                        .Append(Number(run._plant[point, PlantSeedEligible])).Append(',')
                        .Append(Number(run._plant[point, PlantBiomass])).Append(',')
                        .Append(Number(run._plant[point, PlantGrowth])).Append(',')
                        .Append(Number(run._plant[point, PlantOfftake])).Append(',')
                        .Append(Number(run._plant[point, PlantMortality])).Append(',')
                        .Append(Number(run._energy[point])).Append(',')
                        .Append(run._behaviorHash[point].ToString(CultureInfo.InvariantCulture))
                        .AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string Number(double value)
        {
            return double.IsNaN(value) ? "" : value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static string Share(long count, long total)
        {
            string text = total == 0L ? "-" : (100d * count / total).ToString("0.0") + "%";
            return text.PadLeft(7);
        }
    }
}

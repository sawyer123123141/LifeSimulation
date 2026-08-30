using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    /// <summary>One generated site: where a plant may establish, and how much it may hold there.</summary>
    public readonly struct GeneratedPlantSite
    {
        public GeneratedPlantSite(SimVector2 position, float fertility, float capacity)
        {
            Position = position;
            Fertility = fertility;
            Capacity = capacity;
        }

        public SimVector2 Position { get; }
        public float Fertility { get; }
        public float Capacity { get; }
    }

    /// <summary>
    /// Where plants are allowed to grow, decided by the fertility field instead of by a human typing
    /// coordinates.
    ///
    /// <para><b>The seam this closes.</b> The system integration design named it directly - terrain
    /// produces fertility values nobody reads, and creatures eat point resources nobody generates.
    /// Fertility <i>was</i> read, but only as a limit on how fast an already-placed plant grows;
    /// nothing decided <b>where</b> a plant could be. `Y` allowed plants at 24 hand-typed
    /// coordinates and nowhere else.</para>
    ///
    /// <para><b>A jittered lattice, not a Poisson-disc sampler.</b> Occupancy is a cliff in site
    /// spacing rather than a gradient - measured 0.833 at spacing 4, 0.311 at 9.5, and the ecosystem
    /// collapsing entirely at 13.3 (<c>p4-occupancy-calibration-2026-08-22.md</c>). Spacing has to be
    /// a number that can be read off the source and swept, not an emergent property of a sampler.
    /// The jitter exists so the result does not read as a grid on screen.</para>
    ///
    /// <para><b>Total capacity is a budget, not a per-site constant.</b> Every site's share is its
    /// own fertility over the sum of all accepted fertilities, so the arena holds exactly the budget
    /// it is given however many sites pass the filter, and fertile ground genuinely holds more.
    /// Without that, lowering the threshold would quietly make the world richer and any measured
    /// difference would be a food change wearing a placement change's clothes.</para>
    ///
    /// <para><b>Regeneration is deliberately not set here.</b> With plant cohorts on the world calls
    /// <c>RegenerateNonFood</c>, so a food resource's own <c>RegenerationPerSecond</c> never runs -
    /// the plant's growth drives its amount, and that growth is already limited by local fertility,
    /// moisture and temperature in <see cref="PlantGrowthSystem"/>. The integration design's
    /// "regeneration set by local fertility and moisture" row is satisfied there, and setting the
    /// resource field too would be a second, dead copy of it.</para>
    /// </summary>
    public static class PlantSiteGenerator
    {
        /// <summary>
        /// Sites on a jittered lattice over the arena, keeping those whose local fertility clears
        /// <paramref name="fertilityThreshold"/>, with <paramref name="capacityBudget"/> divided
        /// between them in proportion to that fertility.
        /// </summary>
        public static List<GeneratedPlantSite> Generate(
            int worldSeed,
            EnvironmentField field,
            float arenaHalfWidth,
            float spacing,
            float jitterFraction,
            float fertilityThreshold,
            float capacityBudget,
            float fixedCapacity = 0f)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (arenaHalfWidth <= 0f || !IsFinite(arenaHalfWidth)) throw new ArgumentOutOfRangeException(nameof(arenaHalfWidth));
            if (spacing <= 0f || !IsFinite(spacing)) throw new ArgumentOutOfRangeException(nameof(spacing));
            if (jitterFraction < 0f || jitterFraction > 0.5f || !IsFinite(jitterFraction)) throw new ArgumentOutOfRangeException(nameof(jitterFraction));
            if (capacityBudget < 0f || !IsFinite(capacityBudget)) throw new ArgumentOutOfRangeException(nameof(capacityBudget));

            int steps = (int)Math.Floor((2d * arenaHalfWidth) / spacing);
            var accepted = new List<GeneratedPlantSite>();
            double fertilitySum = 0d;

            // Centred: the lattice covers the arena symmetrically whatever the spacing divides into,
            // so a spacing sweep does not also slide every site toward one corner.
            float origin = -((steps - 1) * spacing * 0.5f);
            for (int row = 0; row < steps; row++)
            {
                for (int column = 0; column < steps; column++)
                {
                    long ordinal = ((long)row * steps) + column;
                    float offsetX = (DeterministicRandom.Float01(worldSeed, RandomDomain.PlantSiteGeneration, 0L, ordinal, 0L, 0) - .5f) * 2f * jitterFraction * spacing;
                    float offsetY = (DeterministicRandom.Float01(worldSeed, RandomDomain.PlantSiteGeneration, 0L, ordinal, 0L, 1) - .5f) * 2f * jitterFraction * spacing;
                    var position = new SimVector2(
                        Clamp(origin + (column * spacing) + offsetX, -arenaHalfWidth, arenaHalfWidth),
                        Clamp(origin + (row * spacing) + offsetY, -arenaHalfWidth, arenaHalfWidth));

                    float fertility = field.Sample(position).Fertility;
                    if (fertility < fertilityThreshold) continue;

                    accepted.Add(new GeneratedPlantSite(position, fertility, 0f));
                    fertilitySum += fertility;
                }
            }

            if (accepted.Count == 0 || fertilitySum <= 0d) return accepted;

            // Fixed mode: every site holds the same capacity the authored dormant sites held, so
            // the arena's total food grows with the site count instead of being divided by it. It
            // exists because the two are different claims about what a site IS - a share of a fixed
            // landscape, or a place with its own productivity - and the ecology answers which one it
            // survives.
            if (fixedCapacity > 0f)
            {
                var fixedSites = new List<GeneratedPlantSite>(accepted.Count);
                foreach (GeneratedPlantSite site in accepted)
                {
                    fixedSites.Add(new GeneratedPlantSite(site.Position, site.Fertility, fixedCapacity));
                }

                return fixedSites;
            }

            var budgeted = new List<GeneratedPlantSite>(accepted.Count);
            foreach (GeneratedPlantSite site in accepted)
            {
                float capacity = (float)(capacityBudget * (site.Fertility / fertilitySum));
                budgeted.Add(new GeneratedPlantSite(site.Position, site.Fertility, capacity));
            }

            return budgeted;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}

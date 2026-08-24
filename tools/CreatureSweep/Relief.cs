using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Tools.CreatureSweep
{
    /// <summary>
    /// The ground a run happens on, and the ground the creatures chose to stand on.
    ///
    /// <para>The first sweep of the slope cost returned a clean null that half the corpus could not
    /// have contributed to, because half the arenas are flat - the window is 0.1 radian on a coastal
    /// centre and what lands in it ranges from 25 m of relief to a perfectly level ocean floor. This
    /// picks seeds that have a hill in them.</para>
    ///
    /// <para><b>And it measures where creatures ended up.</b> That was the other gap: the first sweep
    /// could not tell a behavioural response from indifference, because nothing recorded movement.
    /// Occupied elevation and occupied slope need no new simulation state at all - creature positions
    /// and the environment field are both already there, and asking where an animal is standing is a
    /// more direct question than how far it walked.</para>
    /// </summary>
    internal static class Relief
    {
        private const int Steps = 41;
        private const float Half = 25f;

        /// <summary>Metres climbed crossing the arena, averaged over rows. The quantity the cost bills.</summary>
        public static double ClimbPerTraverse(int seed)
        {
            EnvironmentField field = EnvironmentField.CreateTerrainDriven(seed);
            double climb = 0d;
            for (int row = 0; row < Steps; row++)
            {
                double previous = 0d;
                for (int column = 0; column < Steps; column++)
                {
                    double metres = Metres(field, Position(column), Position(row));
                    if (column > 0 && metres > previous) climb += metres - previous;
                    previous = metres;
                }
            }

            return climb / Steps;
        }

        /// <summary>
        /// Seeds whose arena has enough relief for the flag to be able to do anything.
        ///
        /// <para>Selecting seeds on a property of the <i>world</i> rather than on a property of the
        /// <i>result</i> - the terrain is identical in both arms, so this cannot favour either one.
        /// Choosing seeds by outcome would be the thing this is careful not to be.</para>
        /// </summary>
        public static int[] WithRelief(int firstSeed, int wanted, double minimumClimb)
        {
            var seeds = new List<int>();
            int seed = firstSeed;
            while (seeds.Count < wanted && seed < firstSeed + 5000)
            {
                if (ClimbPerTraverse(seed) >= minimumClimb) seeds.Add(seed);
                seed++;
            }

            return seeds.ToArray();
        }

        /// <summary>
        /// Where the survivors are standing: mean elevation in metres, and mean ground steepness.
        ///
        /// <para>If charging for climbs does anything behavioural, this is where it shows: creatures
        /// that pay to go uphill should end up lower down, on flatter ground, or both. If the
        /// population is indifferent, these sit on top of each other.</para>
        /// </summary>
        public static void Occupancy(SimulationWorld world, out double elevation, out double slope)
        {
            EnvironmentField field = world.Environment;
            double elevationTotal = 0d;
            double slopeTotal = 0d;
            int counted = 0;

            for (int index = 0; index < world.CreatureCount; index++)
            {
                MovementState movement = world.GetCreatureMovementAt(index);
                float x = movement.Position.X;
                float y = movement.Position.Y;

                elevationTotal += Metres(field, x, y);

                // Central differences over a metre: the steepness of the ground underfoot.
                double alongX = Metres(field, x + 0.5f, y) - Metres(field, x - 0.5f, y);
                double alongY = Metres(field, x, y + 0.5f) - Metres(field, x, y - 0.5f);
                slopeTotal += Math.Sqrt((alongX * alongX) + (alongY * alongY));
                counted++;
            }

            elevation = counted == 0 ? double.NaN : elevationTotal / counted;
            slope = counted == 0 ? double.NaN : slopeTotal / counted;
        }

        private static float Position(int step)
        {
            return -Half + (2f * Half * step / (Steps - 1));
        }

        private static double Metres(EnvironmentField field, float x, float y)
        {
            return field.Sample(new SimVector2(x, y)).Elevation * PlanetTerrain.MetresPerElevationUnit;
        }
    }
}

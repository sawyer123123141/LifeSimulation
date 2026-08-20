using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Procedural environment fields: determinism, useful range, and sphere behaviour.
    ///
    /// Range is tested as hard as determinism because a field that barely varies is the failure mode
    /// this work exists to fix — before it, fertility and temperature were pinned at 1 and two plant
    /// genes could only ever be taxes.
    /// </summary>
    public sealed class EnvironmentFieldTests
    {
        private static EnvironmentSample[] SampleArena(EnvironmentField field, float step)
        {
            int side = (int)(50f / step) + 1;
            var samples = new EnvironmentSample[side * side];
            int index = 0;
            for (int xi = 0; xi < side; xi++)
            {
                for (int yi = 0; yi < side; yi++)
                {
                    samples[index++] = field.Sample(new SimVector2(-25f + (xi * step), -25f + (yi * step)));
                }
            }

            return samples;
        }

        [Test]
        public void ProceduralFieldsAreDeterministicForAGivenSeed()
        {
            var first = EnvironmentField.CreateProcedural(42);
            var second = EnvironmentField.CreateProcedural(42);

            for (float x = -25f; x <= 25f; x += 3.7f)
            {
                for (float y = -25f; y <= 25f; y += 4.3f)
                {
                    EnvironmentSample a = first.Sample(new SimVector2(x, y));
                    EnvironmentSample b = second.Sample(new SimVector2(x, y));
                    Assert.That(b.Moisture, Is.EqualTo(a.Moisture));
                    Assert.That(b.Fertility, Is.EqualTo(a.Fertility));
                    Assert.That(b.Temperature, Is.EqualTo(a.Temperature));
                }
            }
        }

        [Test]
        public void SamplingIsOrderIndependent()
        {
            // Sample must be a pure function of position: no caching, no call-order dependence.
            var field = EnvironmentField.CreateProcedural(7);
            var probe = new SimVector2(3.5f, -7.25f);

            EnvironmentSample before = field.Sample(probe);
            for (float x = -25f; x <= 25f; x += 1.3f)
            {
                field.Sample(new SimVector2(x, x * 0.4f));
            }

            Assert.That(field.Sample(probe).Moisture, Is.EqualTo(before.Moisture));
        }

        [Test]
        public void DifferentSeedsProduceDifferentWorlds()
        {
            var probe = new SimVector2(3.5f, -7.25f);
            float a = EnvironmentField.CreateProcedural(42).Sample(probe).Moisture;
            float b = EnvironmentField.CreateProcedural(43).Sample(probe).Moisture;

            Assert.That(b, Is.Not.EqualTo(a));
        }

        [Test]
        public void EveryFieldStaysInRange()
        {
            foreach (int seed in new[] { 1, 42, 4242 })
            {
                foreach (EnvironmentSample s in SampleArena(EnvironmentField.CreateProcedural(seed), 2f))
                {
                    Assert.That(s.Moisture, Is.InRange(0f, 1f));
                    Assert.That(s.Fertility, Is.InRange(0f, 1f));
                    Assert.That(s.Temperature, Is.InRange(0f, 1f));
                }
            }
        }

        [Test]
        public void EveryFieldActuallyVariesAcrossTheArena()
        {
            // The point of the whole exercise. A field spanning less than half the range gives
            // adaptation genes too little to select on, which is how MoistureTolerance and
            // TemperatureTolerance ended up as pure costs.
            foreach (int seed in new[] { 1, 42, 4242 })
            {
                EnvironmentSample[] samples = SampleArena(EnvironmentField.CreateProcedural(seed), 2f);

                AssertSpread(samples, s => s.Moisture, "Moisture", seed);
                AssertSpread(samples, s => s.Fertility, "Fertility", seed);
                AssertSpread(samples, s => s.Temperature, "Temperature", seed);
            }
        }

        private static void AssertSpread(EnvironmentSample[] samples, Func<EnvironmentSample, float> select, string name, int seed)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (EnvironmentSample s in samples)
            {
                float v = select(s);
                if (v < min) min = v;
                if (v > max) max = v;
            }

            Assert.That(max - min, Is.GreaterThan(0.45f),
                name + " spans only " + (max - min).ToString("0.000") + " at seed " + seed
                + " (min " + min.ToString("0.000") + ", max " + max.ToString("0.000") + "); too flat to select on");
        }

        [Test]
        public void FieldIsContinuousSoNeighbouringPositionsAreSimilar()
        {
            // Guards against lattice seams and aliasing: a small step in position must not produce a
            // large jump in any field.
            var field = EnvironmentField.CreateProcedural(42);
            for (float x = -24f; x <= 24f; x += 1.1f)
            {
                for (float y = -24f; y <= 24f; y += 1.3f)
                {
                    EnvironmentSample a = field.Sample(new SimVector2(x, y));
                    EnvironmentSample b = field.Sample(new SimVector2(x + 0.25f, y));
                    Assert.That(Math.Abs(b.Moisture - a.Moisture), Is.LessThan(0.15f),
                        "moisture jumped near x=" + x + " y=" + y);
                }
            }
        }

        [Test]
        public void NoiseIsEvaluatedOnASphereSoItRemainsFiniteFarFromTheArena()
        {
            // The fields are sampled from 3D noise at a point on a sphere, which is seamless by
            // construction. Positions far outside the arena still map onto the sphere and must stay
            // well-defined — this is what makes the scheme survive the move to a planet at P7.
            var field = EnvironmentField.CreateProcedural(42);
            double quarterCircumference = EnvironmentField.SphereRadius * Math.PI * 0.5d;

            foreach (float y in new[] { 0f, (float)quarterCircumference, (float)(-quarterCircumference) })
            {
                EnvironmentSample s = field.Sample(new SimVector2(0f, y));
                Assert.That(float.IsNaN(s.Moisture), Is.False);
                Assert.That(float.IsNaN(s.Fertility), Is.False);
                Assert.That(float.IsNaN(s.Temperature), Is.False);
                Assert.That(s.Moisture, Is.InRange(0f, 1f));
                Assert.That(s.Temperature, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void ProceduralFieldsAreOffByDefaultAndTheOldGradientIsUnchanged()
        {
            // Flag-off regression: P4 defaults must still see the original linear moisture ramp with
            // fertility and temperature pinned at 1.
            var world = new SimulationWorld(SimulationConfig.CreatePrototype4Defaults(42, 12));

            EnvironmentSample west = world.Environment.Sample(new SimVector2(-25f, 0f));
            EnvironmentSample east = world.Environment.Sample(new SimVector2(25f, 0f));

            Assert.That(west.Moisture, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(east.Moisture, Is.EqualTo(1.0f).Within(1e-5f));
            Assert.That(west.Fertility, Is.EqualTo(1f));
            Assert.That(west.Temperature, Is.EqualTo(1f));
        }

        // ---- Elevation ---------------------------------------------------------------------

        private static SimVector2[] ArenaGrid(float step)
        {
            var positions = new System.Collections.Generic.List<SimVector2>();
            for (float x = -25f; x <= 25f; x += step)
            for (float y = -25f; y <= 25f; y += step)
            {
                positions.Add(new SimVector2(x, y));
            }

            return positions.ToArray();
        }

        [Test]
        public void ElevationIsZeroAndTheOtherChannelsAreUntouchedWhenTheFlagIsOff()
        {
            // Flag-off must be byte-identical to the field before elevation existed, so it is not
            // enough that Elevation reads 0 - moisture and fertility have to match exactly too.
            EnvironmentField without = EnvironmentField.CreateProcedural(42);
            EnvironmentField with = EnvironmentField.CreateProcedural(42, elevationEnabled: true);

            foreach (SimVector2 position in ArenaGrid(5f))
            {
                EnvironmentSample flat = without.Sample(position);
                Assert.That(flat.Elevation, Is.EqualTo(0f), $"elevation leaked at {position.X},{position.Y}");

                EnvironmentSample raised = with.Sample(position);
                Assert.That(raised.Moisture, Is.EqualTo(flat.Moisture), "elevation must not touch moisture");
                Assert.That(raised.Fertility, Is.EqualTo(flat.Fertility), "elevation must not touch fertility");
            }
        }

        [Test]
        public void ElevationSpansARealRangeAndStaysInBounds()
        {
            EnvironmentField field = EnvironmentField.CreateProcedural(42, elevationEnabled: true);

            float lowest = float.MaxValue;
            float highest = float.MinValue;
            foreach (SimVector2 position in ArenaGrid(2f))
            {
                float elevation = field.Sample(position).Elevation;
                Assert.That(float.IsNaN(elevation), Is.False);
                Assert.That(elevation, Is.InRange(0f, 1f));
                if (elevation < lowest) lowest = elevation;
                if (elevation > highest) highest = elevation;
            }

            // A field that barely varies is the failure mode this line of work exists to avoid: it
            // would make the lapse rate a constant offset rather than a gradient.
            Assert.That(highest - lowest, Is.GreaterThan(.35f),
                $"elevation spans only {lowest:F3}..{highest:F3}");
        }

        [Test]
        public void HighGroundIsColderThanTheSameGroundUnraised()
        {
            // The lapse rate is elevation's only route into the simulation. Pin it against the same
            // field with elevation off, not against an absolute temperature.
            EnvironmentField flat = EnvironmentField.CreateProcedural(42);
            EnvironmentField raised = EnvironmentField.CreateProcedural(42, elevationEnabled: true);

            int checkedPositions = 0;
            foreach (SimVector2 position in ArenaGrid(2f))
            {
                EnvironmentSample before = flat.Sample(position);
                EnvironmentSample after = raised.Sample(position);
                if (after.Elevation <= .05f) continue;

                checkedPositions++;
                Assert.That(after.Temperature, Is.LessThanOrEqualTo(before.Temperature + 1e-6f),
                    $"raising the ground warmed {position.X},{position.Y}");
            }

            Assert.That(checkedPositions, Is.GreaterThan(50), "too little high ground to test the lapse rate");
        }

        [Test]
        public void RidgedNoiseIsRightSkewedAndUsesMoreOfTheRangeThanPlainFbm()
        {
            // Characterises the choice of ridged multifractal over plain fBm: folding about the
            // peaks makes crests creases rather than domes, so most ground is low and the high
            // ground is sparse.
            //
            // An earlier version of this test asserted that under 45% of the arena sits above its
            // own mean. That measure is nearly blind here - it read 48.1% ridged against 49.6% fBm -
            // because the mean shifts down with the distribution, so the fraction above it barely
            // moves. Skewness measures the asymmetry directly and separates the two decisively
            // (+0.249 against +0.019 when this was written). The threshold is comparative rather
            // than absolute so it tests the difference, not a magic number.
            const int Side = 100;
            var ridged = new double[Side * Side];
            var plain = new double[Side * Side];

            int index = 0;
            for (int ix = 0; ix < Side; ix++)
            for (int iy = 0; iy < Side; iy++)
            {
                double x = ix * .35d;
                double y = iy * .35d;
                ridged[index] = EnvironmentNoise.RidgedFbm(42, 160, x, y, 0d, 5, 2.15d, .5d, 1.8d);
                plain[index] = EnvironmentNoise.Fbm(42, 160, x, y, 0d, 5, 2.15d, .5d);
                index++;
            }

            Shape(ridged, out double ridgedSkew, out double ridgedSd);
            Shape(plain, out double plainSkew, out double plainSd);

            Assert.That(ridgedSkew, Is.GreaterThan(plainSkew + .12d),
                $"ridged skew {ridgedSkew:F3} is not meaningfully above plain fBm's {plainSkew:F3}; "
                + "the fold has stopped concentrating high ground");
            Assert.That(ridgedSd, Is.GreaterThan(plainSd),
                $"ridged sd {ridgedSd:F3} should exceed plain fBm's {plainSd:F3}");
        }

        private static void Shape(double[] values, out double skew, out double standardDeviation)
        {
            double mean = 0d;
            foreach (double value in values) mean += value;
            mean /= values.Length;

            double variance = 0d;
            double thirdMoment = 0d;
            foreach (double value in values)
            {
                double d = value - mean;
                variance += d * d;
                thirdMoment += d * d * d;
            }

            variance /= values.Length;
            thirdMoment /= values.Length;
            standardDeviation = System.Math.Sqrt(variance);
            skew = standardDeviation <= 0d ? 0d : thirdMoment / (standardDeviation * standardDeviation * standardDeviation);
        }
    }
}

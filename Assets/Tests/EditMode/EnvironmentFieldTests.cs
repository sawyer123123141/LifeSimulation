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
    }
}

using System;
using System.Linq;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// <see cref="CreatureActionHistory"/> is the selected-creature history behind the P4a
    /// "clear visible action feedback" item. It is an outside observer, so the tests that matter
    /// most are the two that pin it as one: observing must not perturb the simulation, and the
    /// history must be reproducible from a fixed seed.
    /// </summary>
    public sealed class CreatureActionHistoryTests
    {
        private const int ObservationTicks = 400;

        [Test]
        public void ObservingDoesNotPerturbTheSimulation()
        {
            // The whole architectural claim in one test: a world that is watched and a world that
            // is not must remain bit-identical. If this ever fails, the observer has stopped being
            // an observer and every measurement taken while it was attached is suspect.
            SimulationWorld observed = BuildWorld();
            SimulationWorld unobserved = BuildWorld();

            var history = new CreatureActionHistory();
            history.Track(observed.GetCreatureIdAt(0));

            for (int step = 0; step < ObservationTicks; step++)
            {
                observed.Step(observed.Config.FixedDeltaTime);
                history.Observe(observed);
                observed.Events.Clear();

                unobserved.Step(unobserved.Config.FixedDeltaTime);
                unobserved.Events.Clear();
            }

            Assert.That(observed.ComputeStateFingerprint(), Is.EqualTo(unobserved.ComputeStateFingerprint()),
                "attaching a CreatureActionHistory changed the simulation");
            Assert.That(history.ObservedTicks, Is.EqualTo(ObservationTicks),
                "the observer recorded nothing, so the equality above proves nothing");
        }

        [Test]
        public void TheSameSeedProducesTheSameHistory()
        {
            CreatureActionHistory first = RunAndObserve();
            CreatureActionHistory second = RunAndObserve();

            Assert.That(second.EpisodeCount, Is.EqualTo(first.EpisodeCount));
            Assert.That(second.ObservedTicks, Is.EqualTo(first.ObservedTicks));

            for (int index = 0; index < first.EpisodeCount; index++)
            {
                CreatureActionEpisode expected = first.GetEpisodeAt(index);
                CreatureActionEpisode actual = second.GetEpisodeAt(index);
                Assert.That(actual.Action, Is.EqualTo(expected.Action), $"episode {index} action");
                Assert.That(actual.StartTick, Is.EqualTo(expected.StartTick), $"episode {index} start");
                Assert.That(actual.EndTick, Is.EqualTo(expected.EndTick), $"episode {index} end");
                Assert.That(actual.EnergyDelta, Is.EqualTo(expected.EnergyDelta), $"episode {index} energy delta");
            }
        }

        [Test]
        public void ARealRunProducesMoreThanOneKindOfEpisode()
        {
            // Manipulation check for the two tests above: a history of one unbroken Wander episode
            // would satisfy determinism and non-perturbation while showing the player nothing.
            CreatureActionHistory history = RunAndObserve();

            Assert.That(history.EpisodeCount, Is.GreaterThan(0), "no completed episode in 400 ticks");

            var actions = Enumerable.Range(0, history.EpisodeCount)
                .Select(index => history.GetEpisodeAt(index).Action)
                .Distinct()
                .ToArray();

            Assert.That(actions.Length, Is.GreaterThan(1),
                "every episode held the same action, so the history distinguishes nothing");
        }

        [Test]
        public void EpisodesAreContiguousAndOrderedNewestFirst()
        {
            CreatureActionHistory history = RunAndObserve();

            for (int index = 0; index + 1 < history.EpisodeCount; index++)
            {
                CreatureActionEpisode newer = history.GetEpisodeAt(index);
                CreatureActionEpisode older = history.GetEpisodeAt(index + 1);

                Assert.That(newer.StartTick, Is.GreaterThan(older.StartTick), "episodes are not newest-first");
                Assert.That(newer.StartTick, Is.EqualTo(older.EndTick + 1),
                    "episodes must tile the observed span with no gap and no overlap");
                Assert.That(newer.Action, Is.Not.EqualTo(older.Action),
                    "adjacent episodes share an action, so a run was split instead of extended");
            }
        }

        [Test]
        public void ObservedTicksAreFullyAccountedForByTheActionBudget()
        {
            CreatureActionHistory history = RunAndObserve();

            long budgeted = Enum.GetValues(typeof(CreatureAction))
                .Cast<CreatureAction>()
                .Sum(action => history.GetObservedTicksFor(action));

            Assert.That(budgeted, Is.EqualTo(history.ObservedTicks),
                "the per-action budget does not add up to the observed ticks");
        }

        [Test]
        public void TheActionBudgetCoversEveryDeclaredAction()
        {
            // CreatureActionHistory sizes its budget array with a constant. If an action is added
            // to the enum and the constant is not bumped, that action's ticks would be silently
            // dropped rather than counted.
            int declared = Enum.GetValues(typeof(CreatureAction)).Length;

            Assert.That(CreatureActionHistory.ActionCount, Is.EqualTo(declared),
                "CreatureAction gained or lost a value; update CreatureActionHistory.ActionCount");
        }

        [Test]
        public void ADeadCreatureSealsItsLastEpisodeInsteadOfLosingIt()
        {
            SimulationWorld world = BuildWorld();
            CreatureId doomed = world.GetCreatureIdAt(0);

            var history = new CreatureActionHistory();
            history.Track(doomed);

            world.Step(world.Config.FixedDeltaTime);
            history.Observe(world);

            int episodesWhileAlive = history.EpisodeCount;
            Assert.That(history.TryGetOpenEpisode(out CreatureActionEpisode open), Is.True);
            Assert.That(history.IsAlive, Is.True);

            world.RequestDeath(doomed, DeathCause.Health);
            world.Step(world.Config.FixedDeltaTime);
            history.Observe(world);

            Assert.That(history.IsAlive, Is.False, "the observer did not notice the creature leaving");
            Assert.That(history.EpisodeCount, Is.EqualTo(episodesWhileAlive + 1),
                "the open episode was discarded rather than sealed");
            Assert.That(history.GetEpisodeAt(0).Action, Is.EqualTo(open.Action));
        }

        [Test]
        public void TrackingTheSameCreatureAgainKeepsTheHistory()
        {
            // The presenter calls Track every frame from its selection. If that reset the history,
            // the panel would always show one episode.
            SimulationWorld world = BuildWorld();
            CreatureId tracked = world.GetCreatureIdAt(0);

            var history = new CreatureActionHistory();
            history.Track(tracked);
            for (int step = 0; step < 50; step++)
            {
                world.Step(world.Config.FixedDeltaTime);
                history.Track(tracked);
                history.Observe(world);
                world.Events.Clear();
            }

            Assert.That(history.ObservedTicks, Is.EqualTo(50));

            history.Track(world.GetCreatureIdAt(1));
            Assert.That(history.ObservedTicks, Is.EqualTo(0), "selecting a different creature must reset");
            Assert.That(history.EpisodeCount, Is.EqualTo(0));
        }

        [Test]
        public void ObservingTheSameTickTwiceCountsOnce()
        {
            // A paused presenter redraws without stepping. Sampling then must not inflate the budget.
            SimulationWorld world = BuildWorld();
            var history = new CreatureActionHistory();
            history.Track(world.GetCreatureIdAt(0));

            world.Step(world.Config.FixedDeltaTime);
            history.Observe(world);
            history.Observe(world);
            history.Observe(world);

            Assert.That(history.ObservedTicks, Is.EqualTo(1));
        }

        [Test]
        public void TheEpisodeListIsBoundedAndEvictsOldestFirst()
        {
            SimulationWorld world = BuildWorld();
            var history = new CreatureActionHistory(capacity: 3);
            history.Track(world.GetCreatureIdAt(0));

            for (int step = 0; step < ObservationTicks; step++)
            {
                world.Step(world.Config.FixedDeltaTime);
                history.Observe(world);
                world.Events.Clear();
            }

            Assert.That(history.EpisodeCount, Is.LessThanOrEqualTo(3));

            if (history.EpisodeCount == 3)
            {
                Assert.That(history.GetEpisodeAt(0).StartTick, Is.GreaterThan(history.GetEpisodeAt(2).StartTick),
                    "the retained episodes are not the most recent three");
            }
        }

        private static CreatureActionHistory RunAndObserve()
        {
            SimulationWorld world = BuildWorld();
            var history = new CreatureActionHistory();
            history.Track(world.GetCreatureIdAt(0));

            for (int step = 0; step < ObservationTicks; step++)
            {
                world.Step(world.Config.FixedDeltaTime);
                history.Observe(world);
                world.Events.Clear();
            }

            return history;
        }

        private static SimulationWorld BuildWorld()
        {
            var config = SimulationConfig.CreateFullEcosystemDefaults(worldSeed: 42, initialPopulation: 12);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            return world;
        }
    }
}

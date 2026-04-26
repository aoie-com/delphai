using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class BehaviorTests
    {
        static (int, TilePos)? Food(int idx, int x, int y) => (idx, new TilePos((short)x, (short)y));
        static (int, TilePos)? Water(int idx, int x, int y) => (idx, new TilePos((short)x, (short)y));

        // ---------- Idle ---------------------------------------------------

        [Test]
        public void Idle_With_All_Needs_High_Stays_Idle()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.9f, 0.9f), new TilePos(0, 0),
                Food(0, 5, 5), Water(1, 6, 6));
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
            Assert.That(action, Is.InstanceOf<BehaviorAction.Idle>());
        }

        [Test]
        public void Idle_With_Low_Fed_And_Berry_Seeks_Food()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.3f, 0.9f), new TilePos(0, 0),
                Food(2, 5, 5), null);
            Assert.That(state, Is.EqualTo(BehaviorState.SeekingFood));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Seek(new TilePos(5, 5))));
        }

        [Test]
        public void Idle_With_Low_Hydration_And_Water_Seeks_Water()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.9f, 0.2f), new TilePos(0, 0),
                null, Water(3, 4, 4));
            Assert.That(state, Is.EqualTo(BehaviorState.SeekingWater));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Seek(new TilePos(4, 4))));
        }

        [Test]
        public void Hydration_Priority_Trumps_Food_When_Both_Low()
        {
            // hydration<0.3 が fed<0.4 より優先 (migration.md 仕様)
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.2f, 0.2f), new TilePos(0, 0),
                Food(7, 5, 5), Water(11, 8, 8));
            Assert.That(state, Is.EqualTo(BehaviorState.SeekingWater), "hydration wins");
            Assert.That(action, Is.EqualTo(new BehaviorAction.Seek(new TilePos(8, 8))));
        }

        [Test]
        public void Idle_With_Low_Fed_But_No_Berry_Stays_Idle()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.2f, 0.9f), new TilePos(0, 0),
                null, null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
            Assert.That(action, Is.InstanceOf<BehaviorAction.Idle>());
        }

        [Test]
        public void Idle_With_Low_Hydration_But_No_Water_Falls_Through_To_Food()
        {
            // Hyd<HydLow but no water → check fed branch.
            var (state, action) = Behavior.Decide(
                BehaviorState.Idle, new Vitals(0.2f, 0.2f), new TilePos(0, 0),
                Food(5, 9, 9), null);
            Assert.That(state, Is.EqualTo(BehaviorState.SeekingFood), "no water, fall to fed branch");
            Assert.That(action, Is.EqualTo(new BehaviorAction.Seek(new TilePos(9, 9))));
        }

        // ---------- SeekingFood --------------------------------------------

        [Test]
        public void Seeking_Food_Adjacent_Transitions_To_Gathering()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingFood, new Vitals(0.3f, 0.9f), new TilePos(1, 0),
                Food(7, 0, 0), null);
            Assert.That(state, Is.EqualTo(BehaviorState.Gathering));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Consume(7)));
        }

        [Test]
        public void Seeking_Food_Same_Tile_Counts_As_Adjacent()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingFood, new Vitals(0.3f, 0.9f), new TilePos(4, 4),
                Food(3, 4, 4), null);
            Assert.That(state, Is.EqualTo(BehaviorState.Gathering));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Consume(3)));
        }

        [Test]
        public void Seeking_Food_Far_Keeps_Seeking()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingFood, new Vitals(0.3f, 0.9f), new TilePos(0, 0),
                Food(1, 5, 5), null);
            Assert.That(state, Is.EqualTo(BehaviorState.SeekingFood));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Seek(new TilePos(5, 5))));
        }

        [Test]
        public void Seeking_Food_With_Berry_Gone_Returns_To_Idle()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingFood, new Vitals(0.3f, 0.9f), new TilePos(0, 0),
                null, null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
            Assert.That(action, Is.InstanceOf<BehaviorAction.Idle>());
        }

        // ---------- SeekingWater -------------------------------------------

        [Test]
        public void Seeking_Water_Adjacent_Transitions_To_Drinking()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingWater, new Vitals(0.9f, 0.2f), new TilePos(2, 2),
                null, Water(11, 1, 2));
            Assert.That(state, Is.EqualTo(BehaviorState.Drinking));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Consume(11)));
        }

        [Test]
        public void Seeking_Water_With_Water_Gone_Returns_To_Idle()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.SeekingWater, new Vitals(0.9f, 0.2f), new TilePos(0, 0),
                null, null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
            Assert.That(action, Is.InstanceOf<BehaviorAction.Idle>());
        }

        // ---------- Gathering ----------------------------------------------

        [Test]
        public void Gathering_Stays_Gathering_When_Adjacent_And_Hungry()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Gathering, new Vitals(0.5f, 0.5f), new TilePos(4, 4),
                Food(3, 4, 4), null);
            Assert.That(state, Is.EqualTo(BehaviorState.Gathering));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Consume(3)));
        }

        [Test]
        public void Gathering_Returns_To_Idle_When_Full()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Gathering, new Vitals(1f, 0.5f), new TilePos(4, 4),
                Food(3, 4, 4), null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
        }

        [Test]
        public void Gathering_Returns_To_Idle_When_Resource_Gone()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Gathering, new Vitals(0.5f, 0.5f), new TilePos(4, 4),
                null, null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
        }

        [Test]
        public void Gathering_Returns_To_Idle_If_Wandered_Out_Of_Range()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Gathering, new Vitals(0.5f, 0.5f), new TilePos(0, 0),
                Food(3, 5, 5), null);
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
        }

        // ---------- Drinking -----------------------------------------------

        [Test]
        public void Drinking_Stays_Drinking_When_Adjacent_And_Thirsty()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Drinking, new Vitals(0.5f, 0.5f), new TilePos(2, 2),
                null, Water(11, 2, 3));
            Assert.That(state, Is.EqualTo(BehaviorState.Drinking));
            Assert.That(action, Is.EqualTo(new BehaviorAction.Consume(11)));
        }

        [Test]
        public void Drinking_Returns_To_Idle_When_Full_Hydration()
        {
            var (state, action) = Behavior.Decide(
                BehaviorState.Drinking, new Vitals(0.5f, 1f), new TilePos(2, 2),
                null, Water(11, 2, 3));
            Assert.That(state, Is.EqualTo(BehaviorState.Idle));
        }

        // ---------- Chebyshev ----------------------------------------------

        [Test]
        public void Chebyshev_Diagonal_Counts_As_One()
        {
            Assert.That(Behavior.Chebyshev(new TilePos(0, 0), new TilePos(1, 1)), Is.EqualTo(1));
            Assert.That(Behavior.Chebyshev(new TilePos(0, 0), new TilePos(-1, -1)), Is.EqualTo(1));
            Assert.That(Behavior.Chebyshev(new TilePos(3, 7), new TilePos(0, 5)), Is.EqualTo(3));
        }
    }
}

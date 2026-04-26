using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class MoveStateTests
    {
        const float Eps = 1e-6f;

        static void AssertCloseV((float X, float Y) actual, float expectedX, float expectedY, string label = null)
        {
            Assert.That(actual.X, Is.EqualTo(expectedX).Within(Eps), $"{label} X");
            Assert.That(actual.Y, Is.EqualTo(expectedY).Within(Eps), $"{label} Y");
        }

        [Test]
        public void New_Initializes_Prev_Equal_To_Current_And_No_Target()
        {
            var m = new MoveState(new TilePos(2, 3));
            AssertCloseV(m.Pos, 2, 3, "pos");
            AssertCloseV(m.PrevPos, 2, 3, "prev");
            Assert.That(m.MoveTarget.HasValue, Is.False);
            Assert.That(m.TilePos, Is.EqualTo(new TilePos(2, 3)));
            Assert.That(m.PrevTilePos, Is.EqualTo(new TilePos(2, 3)));
        }

        [Test]
        public void Step_With_No_Target_Is_Idle_And_Resets_Prev()
        {
            var m = new MoveState(new TilePos(5, 5));
            m.Step();
            AssertCloseV(m.Pos, 5, 5);
            AssertCloseV(m.PrevPos, 5, 5);
        }

        [Test]
        public void Step_Moves_Unit_Vector_At_Zero_Degrees()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(3, 0) };
            m.Step();
            AssertCloseV(m.Pos, 1, 0);
        }

        [Test]
        public void Step_Moves_Unit_Vector_At_Ninety_Degrees()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(0, 3) };
            m.Step();
            AssertCloseV(m.Pos, 0, 1);
        }

        [Test]
        public void Step_Moves_Unit_Vector_At_Two_Seventy_Degrees()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(0, -3) };
            m.Step();
            AssertCloseV(m.Pos, 0, -1);
        }

        [Test]
        public void Step_Moves_Unit_Vector_At_One_Eighty_Degrees()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(-3, 0) };
            m.Step();
            AssertCloseV(m.Pos, -1, 0);
        }

        [Test]
        public void Step_Moves_Unit_Vector_Along_Three_Four_Five_Triple()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(3, 4) };
            m.Step();
            AssertCloseV(m.Pos, 0.6f, 0.8f);
        }

        [Test]
        public void Step_Moves_Unit_Vector_Toward_Negative_Diagonal()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(-3, -4) };
            m.Step();
            AssertCloseV(m.Pos, -0.6f, -0.8f);
        }

        [Test]
        public void Step_Snaps_To_Target_When_Within_Speed()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(1, 0) };
            m.Step();
            AssertCloseV(m.Pos, 1, 0);
            Assert.That(m.MoveTarget.HasValue, Is.False, "target cleared on arrival");
        }

        [Test]
        public void Step_Reaches_Three_Four_Five_Target_In_Five_Ticks()
        {
            var m = new MoveState(new TilePos(0, 0)) { MoveTarget = new TilePos(3, 4) };
            for (int i = 0; i < 4; i++)
            {
                m.Step();
                Assert.That(m.MoveTarget.HasValue, Is.True, $"target should still be set at tick {i}");
            }
            m.Step();
            AssertCloseV(m.Pos, 3, 4);
            Assert.That(m.MoveTarget.HasValue, Is.False);
        }

        [Test]
        public void World_Pos_Interpolates_Float_Prev_To_Current()
        {
            var m = new MoveState((2f, 0.8f), (1.2f, 0.5f), null);
            AssertCloseV(m.WorldPos(0f), 1.2f, 0.5f, "alpha=0");
            AssertCloseV(m.WorldPos(1f), 2f, 0.8f, "alpha=1");
            AssertCloseV(m.WorldPos(0.5f), 1.6f, 0.65f, "alpha=0.5");
        }

        [Test]
        public void World_Pos_Clamps_Alpha()
        {
            var m = new MoveState((1f, 0f), (0f, 0f), null);
            AssertCloseV(m.WorldPos(-1f), 0f, 0f, "alpha=-1 clamped to 0");
            AssertCloseV(m.WorldPos(2f), 1f, 0f, "alpha=2 clamped to 1");
        }

        [Test]
        public void Tile_Pos_Derived_From_Round_Of_Pos()
        {
            var m = new MoveState((2.4f, 1.6f), (0f, 0f), null);
            Assert.That(m.TilePos, Is.EqualTo(new TilePos(2, 2)));
            var m2 = new MoveState((-0.3f, 0.7f), (0f, 0f), null);
            Assert.That(m2.TilePos, Is.EqualTo(new TilePos(0, 1)));
        }
    }
}

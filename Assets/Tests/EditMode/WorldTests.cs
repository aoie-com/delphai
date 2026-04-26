using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class WorldTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void Spawn_Citizen_Stores_Name_And_Position()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Kael", new TilePos(3, 7));
            Assert.That(idx, Is.EqualTo(0));
            Assert.That(w.Citizens[idx].Name, Is.EqualTo("Kael"));
            Assert.That(w.CitizenMoves[idx].TilePos, Is.EqualTo(new TilePos(3, 7)));
        }

        [Test]
        public void Spawn_Citizen_Assigns_Sequential_Indices()
        {
            var w = new World();
            int a = w.SpawnCitizen("A", new TilePos(0, 0));
            int b = w.SpawnCitizen("B", new TilePos(1, 2));
            Assert.That(a, Is.EqualTo(0));
            Assert.That(b, Is.EqualTo(1));
            Assert.That(w.Citizens.Count, Is.EqualTo(2));
        }

        [Test]
        public void World_Pos_At_Alpha_Boundaries_Match_Prev_And_Current()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Mover", new TilePos(0, 0));
            w.CitizenMoves[idx].MoveTarget = new TilePos(3, 0);
            w.Tick(); // moves (0,0) → (1,0); prev_pos = (0,0).
            var pPrev = w.GetCitizenWorldPos(idx, 0f);
            var pCurr = w.GetCitizenWorldPos(idx, 1f);
            var pHalf = w.GetCitizenWorldPos(idx, 0.5f);
            Assert.That(pPrev.X, Is.EqualTo(0f).Within(Eps));
            Assert.That(pPrev.Y, Is.EqualTo(0f).Within(Eps));
            Assert.That(pCurr.X, Is.EqualTo(1f).Within(Eps));
            Assert.That(pCurr.Y, Is.EqualTo(0f).Within(Eps));
            Assert.That(pHalf.X, Is.EqualTo(0.5f).Within(Eps));
            Assert.That(pHalf.Y, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void Tick_Increments_Tick_Count()
        {
            var w = new World();
            Assert.That(w.TickCount, Is.EqualTo(0u));
            w.Tick();
            w.Tick();
            w.Tick();
            Assert.That(w.TickCount, Is.EqualTo(3u));
        }
    }
}

using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class WorldDecayTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void Spawn_Citizen_Initializes_Vitals_To_Default()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Alice", new TilePos(0, 0));
            Assert.That(w.CitizenVitals[idx].Fed, Is.EqualTo(1f).Within(Eps));
            Assert.That(w.CitizenVitals[idx].Hydration, Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void Tick_Decays_Fed_By_Fed_Decay_Per_Tick()
        {
            var w = new World();
            int idx = w.SpawnCitizen("A", new TilePos(0, 0));
            w.Tick();
            Assert.That(w.CitizenVitals[idx].Fed, Is.EqualTo(1f - World.FedDecay).Within(Eps));
        }

        [Test]
        public void Tick_Decays_Hydration_By_Hydration_Decay_Per_Tick()
        {
            var w = new World();
            int idx = w.SpawnCitizen("A", new TilePos(0, 0));
            w.Tick();
            Assert.That(w.CitizenVitals[idx].Hydration, Is.EqualTo(1f - World.HydrationDecay).Within(Eps));
        }

        [Test]
        public void Tick_Clamps_Vitals_To_Zero_Never_Negative()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Starving", new TilePos(0, 0));
            // Run enough ticks that both fed and hydration would go negative
            // without clamping. 1.0 / 0.004 = 250 ticks → fed=0; hydration
            // hits 0 first at ~143 ticks. 500 ticks ensures both clamp.
            for (int i = 0; i < 500; i++) w.Tick();
            Assert.That(w.CitizenVitals[idx].Fed, Is.EqualTo(0f).Within(Eps));
            Assert.That(w.CitizenVitals[idx].Hydration, Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void Set_Resources_Stores_Berries_And_Water()
        {
            var w = new World();
            w.SetResources(new[]
            {
                Resource.NewBerry(new TilePos(1, 1)),
                Resource.NewBerry(new TilePos(5, 5)),
                Resource.NewWater(new TilePos(3, 3)),
            });
            Assert.That(w.Resources.Count, Is.EqualTo(3));
            Assert.That(w.Resources[0].Kind, Is.EqualTo(ResourceKind.Berry));
            Assert.That(w.Resources[2].Kind, Is.EqualTo(ResourceKind.Water));
        }

        [Test]
        public void Tick_Regenerates_Resources_After_Step()
        {
            var w = new World();
            var berry = Resource.WithAmount(ResourceKind.Berry, 2f, new TilePos(0, 0));
            w.SetResources(new[] { berry });
            w.Tick();
            Assert.That(w.Resources[0].Amount, Is.EqualTo(2f + Resource.BerryRegenPerTick).Within(Eps));
        }

        [Test]
        public void Tick_Counter_Survives_Many_Ticks_Without_Overflow()
        {
            // Stress test: tick decay phase runs for 10k ticks without panic.
            // Citizens starve (no behavior layer yet) but must not throw.
            var w = new World();
            w.SpawnCitizen("A", new TilePos(0, 0));
            w.SpawnCitizen("B", new TilePos(1, 1));
            for (int i = 0; i < 10_000; i++) w.Tick();
            Assert.That(w.TickCount, Is.EqualTo(10_000u));
            Assert.That(w.CitizenVitals[0].Fed, Is.EqualTo(0f).Within(Eps));
            Assert.That(w.CitizenVitals[0].Hydration, Is.EqualTo(0f).Within(Eps));
        }
    }
}

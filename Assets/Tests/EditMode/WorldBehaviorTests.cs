using NUnit.Framework;

namespace DelphAi.Core.Tests
{
    public class WorldBehaviorTests
    {
        const float Eps = 1e-6f;

        [Test]
        public void Spawn_Citizen_Initializes_Behavior_To_Idle()
        {
            var w = new World();
            int idx = w.SpawnCitizen("A", new TilePos(0, 0));
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.Idle));
        }

        [Test]
        public void Tick_Routes_Idle_Citizen_To_Seeking_Food_When_Hungry()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Hungry", new TilePos(0, 0));
            // Force fed below FedLow (decay-only would take 150+ ticks).
            w.CitizenVitals[idx] = new Vitals(0.3f, 0.9f);
            w.SetResources(new[] { Resource.NewBerry(new TilePos(5, 5)) });
            w.Tick();
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.SeekingFood));
            Assert.That(w.CitizenMoves[idx].MoveTarget.HasValue, Is.True);
            Assert.That(w.CitizenMoves[idx].MoveTarget.Value, Is.EqualTo(new TilePos(5, 5)));
        }

        [Test]
        public void Tick_Routes_Idle_Citizen_To_Seeking_Water_When_Thirsty()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Thirsty", new TilePos(0, 0));
            w.CitizenVitals[idx] = new Vitals(0.9f, 0.2f);
            w.SetResources(new[] { Resource.NewWater(new TilePos(3, 3)) });
            w.Tick();
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.SeekingWater));
            Assert.That(w.CitizenMoves[idx].MoveTarget.Value, Is.EqualTo(new TilePos(3, 3)));
        }

        [Test]
        public void Hydration_Priority_Trumps_Food_When_Both_Low_End_To_End()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Both", new TilePos(0, 0));
            w.CitizenVitals[idx] = new Vitals(0.2f, 0.2f);
            w.SetResources(new[]
            {
                Resource.NewBerry(new TilePos(5, 0)),
                Resource.NewWater(new TilePos(0, 5)),
            });
            w.Tick();
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.SeekingWater),
                "hydration<0.3 wins over fed<0.4");
            Assert.That(w.CitizenMoves[idx].MoveTarget.Value, Is.EqualTo(new TilePos(0, 5)));
        }

        [Test]
        public void Adjacent_Berry_Is_Consumed_And_Fed_Rises()
        {
            var w = new World();
            int idx = w.SpawnCitizen("Adjacent", new TilePos(0, 0));
            w.CitizenVitals[idx] = new Vitals(0.3f, 0.9f);
            // Berry at (1, 0): chebyshev=1, adjacent. But Idle→SeekingFood→
            // Gathering takes 2 ticks (decide is Idle-only re-evaluated, and
            // step snaps onto the berry tile during tick 1).
            w.SetResources(new[] { Resource.NewBerry(new TilePos(1, 0)) });

            w.Tick(); // Tick 1: Idle → SeekingFood (set target, then step snaps to (1,0))
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.SeekingFood));

            float fedAfterT1 = w.CitizenVitals[idx].Fed;
            w.Tick(); // Tick 2: SeekingFood + adjacent → Gathering + Consume
            Assert.That(w.CitizenBehaviors[idx], Is.EqualTo(BehaviorState.Gathering));
            // Fed gained: +0.2 per consume * 1.0 taken − 0.004 decay.
            float expected = fedAfterT1 - World.FedDecay + Resource.BerryFedPerUnit * Resource.ConsumePerTick;
            Assert.That(w.CitizenVitals[idx].Fed, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Drinks_Then_Eats_When_Both_Needs_Low()
        {
            // Satiated trap 回避: 両方 low から始まる。Water に隣接して開始 →
            // 1 tick で Drinking → 5 tick で hydration 満タン → Idle 再評価 → fed
            // < FedLow なら Berry へ向かう (Berry は別位置)。
            var w = new World();
            int idx = w.SpawnCitizen("BothLow", new TilePos(0, 0));
            w.CitizenVitals[idx] = new Vitals(0.2f, 0.2f);
            w.SetResources(new[]
            {
                Resource.NewWater(new TilePos(1, 0)),   // adjacent to (0,0)
                Resource.NewBerry(new TilePos(10, 0)),  // far
            });

            // Drink until hydration full or near-full. With HydrationGain 0.2
            // per tick - HydrationDecay 0.007 ≈ +0.193/tick from 0.2 base; we
            // hit ~1.0 in ~5 ticks but include slack.
            for (int t = 0; t < 10; t++) w.Tick();
            Assert.That(w.CitizenVitals[idx].Hydration, Is.GreaterThan(0.9f),
                $"hydration should refill, got {w.CitizenVitals[idx].Hydration}");

            // Now drive forward until decide pivots to food. After hydration
            // saturates the citizen returns to Idle, then re-evaluates and
            // (since fed is still low) heads for the berry at (10,0).
            BehaviorState? sawSeekingFood = null;
            for (int t = 0; t < 200 && sawSeekingFood == null; t++)
            {
                w.Tick();
                if (w.CitizenBehaviors[idx] == BehaviorState.SeekingFood
                    || w.CitizenBehaviors[idx] == BehaviorState.Gathering)
                {
                    sawSeekingFood = w.CitizenBehaviors[idx];
                }
            }
            Assert.That(sawSeekingFood, Is.Not.Null, "citizen never pivoted to food");
        }

        [Test]
        public void Citizen_Survives_10k_Ticks_With_Adjacent_Food_And_Water()
        {
            // 10k tick stress: citizen spawned between abundant resources
            // should not starve (fed > 0) nor dehydrate (hydration > 0) at the
            // end. Without behavior layer they would; this test proves the
            // decide → consume loop closes.
            var w = new World();
            int idx = w.SpawnCitizen("Survivor", new TilePos(0, 0));
            w.SetResources(new[]
            {
                Resource.NewBerry(new TilePos(1, 0)),
                Resource.NewWater(new TilePos(0, 1)),
            });
            for (int t = 0; t < 10_000; t++) w.Tick();
            Assert.That(w.TickCount, Is.EqualTo(10_000u));
            Assert.That(w.CitizenVitals[idx].Fed, Is.GreaterThan(0f),
                $"starved at tick 10k, fed={w.CitizenVitals[idx].Fed}");
            Assert.That(w.CitizenVitals[idx].Hydration, Is.GreaterThan(0f),
                $"dehydrated at tick 10k, hyd={w.CitizenVitals[idx].Hydration}");
        }
    }
}

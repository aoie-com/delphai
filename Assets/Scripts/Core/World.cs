using System.Collections.Generic;

namespace DelphAi.Core
{
    // from delphai-core src/world.rs as of commit 138bfe7. M1.1 added spawn /
    // step / world_pos. M1.2 adds Vitals (decay) + Resources (regenerate).
    // tick decide / Gather / random_walk land in M1.3+.
    public class World
    {
        // FED_DECAY = 0.004/tick ≈ full→empty in 250 ticks (~62s at 4Hz).
        public const float FedDecay = 0.004f;
        // HYDRATION_DECAY = 0.007/tick (forward-port per migration.md).
        public const float HydrationDecay = 0.007f;

        public uint TickCount { get; private set; }
        public List<Citizen> Citizens { get; } = new List<Citizen>();
        public List<MoveState> CitizenMoves { get; } = new List<MoveState>();
        public List<Vitals> CitizenVitals { get; } = new List<Vitals>();
        public List<Resource> Resources { get; private set; } = new List<Resource>();

        public int SpawnCitizen(string name, TilePos tilePos)
        {
            int idx = Citizens.Count;
            Citizens.Add(new Citizen(name));
            CitizenMoves.Add(new MoveState(tilePos));
            CitizenVitals.Add(Vitals.Default);
            return idx;
        }

        // Replace all resources in one shot. Intended for initial seeding.
        // Callers that want to mutate individual entries access Resources directly.
        public void SetResources(IEnumerable<Resource> resources)
        {
            Resources = new List<Resource>(resources);
        }

        // Tick phases (Rust order from world.rs):
        //   1. decay vitals (fed/hydration)         ← M1.2 ✓
        //   2. (M1.3) decide → act
        //   3. step movement                        ← M1.1 ✓
        //   4. (M1.x) update tile history
        //   5. regenerate resources                 ← M1.2 ✓
        //   6. (M1.x) random walk re-target
        public void Tick()
        {
            TickCount++;

            // 1. Decay. Done before decide (M1.3) so a citizen that just
            //    dropped below a threshold this tick immediately re-decides.
            for (int i = 0; i < CitizenVitals.Count; i++)
            {
                CitizenVitals[i] = CitizenVitals[i]
                    .WithFedDecay(FedDecay)
                    .WithHydrationDecay(HydrationDecay);
            }

            // 3. Step movement.
            foreach (var m in CitizenMoves)
            {
                m.Step();
            }

            // 5. Regenerate resources. Last so this tick's gathers (M1.3) don't
            //    partially refill the bush we just pulled from.
            foreach (var r in Resources)
            {
                r.Regenerate();
            }
        }

        public (float X, float Y) GetCitizenWorldPos(int idx, float alpha)
            => CitizenMoves[idx].WorldPos(alpha);
    }
}

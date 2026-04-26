using System.Collections.Generic;

namespace DelphAi.Core
{
    // from delphai-core src/world.rs as of commit 138bfe7. M1.1: spawn / step
    // / world_pos. M1.2: vitals decay + resource regen. M1.3: behavior decide
    // → action exec (Seek / Consume) with INeedEffect routing.
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
        public List<BehaviorState> CitizenBehaviors { get; } = new List<BehaviorState>();
        public List<Resource> Resources { get; private set; } = new List<Resource>();

        public int SpawnCitizen(string name, TilePos tilePos)
        {
            int idx = Citizens.Count;
            Citizens.Add(new Citizen(name));
            CitizenMoves.Add(new MoveState(tilePos));
            CitizenVitals.Add(Vitals.Default);
            CitizenBehaviors.Add(BehaviorState.Idle);
            return idx;
        }

        public void SetResources(IEnumerable<Resource> resources)
        {
            Resources = new List<Resource>(resources);
        }

        // Tick phases (Rust order):
        //   1. decay vitals
        //   2. decide → act (set move_target / Consume + Effect.Apply)
        //   3. step movement
        //   4. (M1.x) update tile history
        //   5. regenerate resources
        //   6. (M1.x) random walk re-target
        public void Tick()
        {
            TickCount++;

            // 1. Decay before decide so threshold crossings react this tick.
            for (int i = 0; i < CitizenVitals.Count; i++)
            {
                CitizenVitals[i] = CitizenVitals[i]
                    .WithFedDecay(FedDecay)
                    .WithHydrationDecay(HydrationDecay);
            }

            // 2. Decide → act.
            for (int i = 0; i < Citizens.Count; i++)
            {
                var citizenTile = CitizenMoves[i].TilePos;
                var nearestFood = NearestForKind(VitalKind.Fed, citizenTile);
                var nearestWater = NearestForKind(VitalKind.Hydration, citizenTile);
                var (newState, action) = Behavior.Decide(
                    CitizenBehaviors[i], CitizenVitals[i], citizenTile,
                    nearestFood, nearestWater);
                CitizenBehaviors[i] = newState;
                ApplyAction(i, action);
            }

            // 3. Step.
            foreach (var m in CitizenMoves) m.Step();

            // 5. Regenerate.
            foreach (var r in Resources) r.Regenerate();
        }

        void ApplyAction(int citizenIdx, BehaviorAction action)
        {
            switch (action)
            {
                case BehaviorAction.Idle:
                    break;
                case BehaviorAction.Seek seek:
                    CitizenMoves[citizenIdx].MoveTarget = seek.Target;
                    break;
                case BehaviorAction.Consume consume:
                    // Stop moving while consuming so the citizen doesn't drift
                    // off the resource tile mid-harvest.
                    CitizenMoves[citizenIdx].MoveTarget = null;
                    if (consume.ResourceIdx >= 0 && consume.ResourceIdx < Resources.Count)
                    {
                        var r = Resources[consume.ResourceIdx];
                        float taken = r.ConsumeOneTick();
                        if (taken > 0f)
                        {
                            CitizenVitals[citizenIdx] = r.Effect.Apply(
                                CitizenVitals[citizenIdx], taken);
                        }
                    }
                    break;
            }
        }

        (int Idx, TilePos Tile)? NearestForKind(VitalKind kind, TilePos from)
        {
            int bestIdx = -1;
            int bestSq = int.MaxValue;
            for (int i = 0; i < Resources.Count; i++)
            {
                var r = Resources[i];
                if (r.IsDepleted) continue;
                if (!r.Effect.Affects(kind)) continue;
                int dx = r.TilePos.X - from.X;
                int dy = r.TilePos.Y - from.Y;
                int sq = dx * dx + dy * dy;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    bestIdx = i;
                }
            }
            return bestIdx < 0 ? null : (bestIdx, Resources[bestIdx].TilePos);
        }

        public (float X, float Y) GetCitizenWorldPos(int idx, float alpha)
            => CitizenMoves[idx].WorldPos(alpha);
    }
}

using System.Collections.Generic;

namespace DelphAi.Core
{
    // from delphai-core src/world.rs as of commit 138bfe7. M1.1 carries only
    // the spawn / step / world_pos slice. tick decay / decide / regenerate /
    // random_walk land in M1.2-M1.4.
    public class World
    {
        public uint TickCount { get; private set; }
        public List<Citizen> Citizens { get; } = new List<Citizen>();
        public List<MoveState> CitizenMoves { get; } = new List<MoveState>();

        // Spawn a new citizen at tilePos. Returns the index in
        // Citizens / CitizenMoves (kept index-parallel — never reorder one
        // without the others).
        public int SpawnCitizen(string name, TilePos tilePos)
        {
            int idx = Citizens.Count;
            Citizens.Add(new Citizen(name));
            CitizenMoves.Add(new MoveState(tilePos));
            return idx;
        }

        // M1.1 minimal tick: just step movement. Phases:
        //   1. (M1.2) decay vitals
        //   2. (M1.3) decide → act
        //   3. step movement   ← this slice
        //   4. (M1.x) update tile history
        //   5. (M1.2) regenerate resources
        //   6. (M1.x) random walk re-target
        public void Tick()
        {
            TickCount++;
            foreach (var m in CitizenMoves)
            {
                m.Step();
            }
        }

        // Linear interpolation between prev_tile_pos and tile_pos at alpha ∈ [0,1].
        // 0 = previous tile, 1 = current tile. Tile-space; caller scales.
        public (float X, float Y) GetCitizenWorldPos(int idx, float alpha)
            => CitizenMoves[idx].WorldPos(alpha);
    }
}

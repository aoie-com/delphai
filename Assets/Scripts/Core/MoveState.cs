using System;

namespace DelphAi.Core
{
    // from delphai-core src/move_state.rs as of commit 138bfe7 (167 tests).
    // Step() is unit-vector / atan2-style. step_with_grid is deferred to M1.x
    // (when WalkGrid lands).
    public class MoveState
    {
        public const float Speed = 1.0f;

        public (float X, float Y) Pos { get; private set; }
        public (float X, float Y) PrevPos { get; private set; }
        public TilePos? MoveTarget { get; set; }

        public MoveState(TilePos tilePos)
        {
            Pos = (tilePos.X, tilePos.Y);
            PrevPos = (tilePos.X, tilePos.Y);
            MoveTarget = null;
        }

        // Raw constructor mirroring Rust's struct-literal usage in tests.
        public MoveState((float X, float Y) pos, (float X, float Y) prevPos, TilePos? moveTarget)
        {
            Pos = pos;
            PrevPos = prevPos;
            MoveTarget = moveTarget;
        }

        public TilePos TilePos => new TilePos(
            (short)MathF.Round(Pos.X, MidpointRounding.AwayFromZero),
            (short)MathF.Round(Pos.Y, MidpointRounding.AwayFromZero));

        public TilePos PrevTilePos => new TilePos(
            (short)MathF.Round(PrevPos.X, MidpointRounding.AwayFromZero),
            (short)MathF.Round(PrevPos.Y, MidpointRounding.AwayFromZero));

        // Advance one tick at SPEED toward MoveTarget using unit-vector
        // movement. PrevPos is always updated first so the renderer can
        // interpolate smoothly. When dist <= SPEED, snap to target and clear it.
        public void Step()
        {
            PrevPos = Pos;
            if (!MoveTarget.HasValue) return;
            var target = MoveTarget.Value;
            float tx = target.X;
            float ty = target.Y;
            float dx = tx - Pos.X;
            float dy = ty - Pos.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist <= Speed)
            {
                Pos = (tx, ty);
                MoveTarget = null;
                return;
            }
            float inv = Speed / dist;
            Pos = (Pos.X + dx * inv, Pos.Y + dy * inv);
        }

        // Linear interpolation between PrevPos and Pos. alpha clamped to [0,1].
        public (float X, float Y) WorldPos(float alpha)
        {
            float a = Math.Clamp(alpha, 0f, 1f);
            return (
                PrevPos.X + (Pos.X - PrevPos.X) * a,
                PrevPos.Y + (Pos.Y - PrevPos.Y) * a);
        }
    }
}

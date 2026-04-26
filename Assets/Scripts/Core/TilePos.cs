using System;

namespace DelphAi.Core
{
    public readonly struct TilePos : IEquatable<TilePos>
    {
        public readonly short X;
        public readonly short Y;

        public TilePos(short x, short y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(TilePos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is TilePos t && Equals(t);
        public override int GetHashCode() => unchecked(((int)X << 16) | (ushort)Y);
        public static bool operator ==(TilePos a, TilePos b) => a.Equals(b);
        public static bool operator !=(TilePos a, TilePos b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }
}

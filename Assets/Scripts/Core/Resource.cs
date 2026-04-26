using System;

namespace DelphAi.Core
{
    public enum ResourceKind
    {
        Berry,
        Water,
    }

    // from delphai-core src/resource.rs as of commit 138bfe7. Berry constants
    // are 1:1; Water is forward-ported per migration.md (Sprint N7
    // unimplemented in Rust). Drink/gather gain constants land in M1.3.
    public class Resource
    {
        public const float BerryAmountMax = 5f;
        public const float BerryRegenPerTick = 0.01f;
        public const float WaterAmountMax = 10f;
        public const float WaterRegenPerTick = 0.05f;
        public const float GatherPerTick = 1f;

        public ResourceKind Kind { get; }
        public float Amount { get; private set; }
        public TilePos TilePos { get; }

        private Resource(ResourceKind kind, float amount, TilePos tilePos)
        {
            Kind = kind;
            Amount = amount;
            TilePos = tilePos;
        }

        public static Resource NewBerry(TilePos tilePos)
            => new Resource(ResourceKind.Berry, BerryAmountMax, tilePos);

        public static Resource NewWater(TilePos tilePos)
            => new Resource(ResourceKind.Water, WaterAmountMax, tilePos);

        // Test seam: matches Rust struct-literal pattern of overriding amount.
        public static Resource WithAmount(ResourceKind kind, float amount, TilePos tilePos)
            => new Resource(kind, amount, tilePos);

        // Returns the amount actually taken (0 when already empty). Caller
        // inspects the return value to decide whether the citizen was fed.
        public float Gather()
        {
            float taken = MathF.Min(Amount, GatherPerTick);
            Amount = MathF.Max(0f, Amount - taken);
            return taken;
        }

        // Apply one tick of regeneration. Clamped at the kind's max.
        public void Regenerate()
        {
            (float rate, float max) = Kind switch
            {
                ResourceKind.Berry => (BerryRegenPerTick, BerryAmountMax),
                ResourceKind.Water => (WaterRegenPerTick, WaterAmountMax),
                _ => (0f, 0f),
            };
            Amount = MathF.Min(max, Amount + rate);
        }

        public bool IsDepleted => Amount <= 0f;
    }
}

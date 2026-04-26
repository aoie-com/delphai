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
    // unimplemented in Rust). M1.3 adds INeedEffect + ConsumeOneTick so
    // future "berry gives fed+hydration" / "hunt drops meat+leather" can
    // swap the effect without touching Resource itself.
    public class Resource
    {
        public const float BerryAmountMax = 5f;
        // Bumped from Rust's 0.01 → 0.04 for long-run sustainability. With
        // FedDecay=0.004 / FedSatiated=0.95, one citizen consumes ~2.75 berry
        // units per ~150-tick cycle; 0.01 regen leaves a -1.25 deficit per
        // cycle, so a single bush starves the citizen by tick ~700. 0.04
        // closes the gap with slack. Rust never long-run tested.
        public const float BerryRegenPerTick = 0.04f;
        public const float WaterAmountMax = 10f;
        public const float WaterRegenPerTick = 0.05f;
        public const float ConsumePerTick = 1f;

        // How much of the relevant vital is restored per unit consumed.
        // Rust delphai-core/src/world.rs::FED_GAIN_PER_GATHER = 0.2 (1:1).
        // Water mirror picks the same value pending M4 visual tuning.
        public const float BerryFedPerUnit = 0.2f;
        public const float WaterHydrationPerUnit = 0.2f;

        public ResourceKind Kind { get; }
        public float Amount { get; private set; }
        public TilePos TilePos { get; }
        public INeedEffect Effect { get; }

        private Resource(ResourceKind kind, float amount, TilePos tilePos, INeedEffect effect)
        {
            Kind = kind;
            Amount = amount;
            TilePos = tilePos;
            Effect = effect;
        }

        public static Resource NewBerry(TilePos tilePos)
            => new Resource(ResourceKind.Berry, BerryAmountMax, tilePos, new FedGain(BerryFedPerUnit));

        public static Resource NewWater(TilePos tilePos)
            => new Resource(ResourceKind.Water, WaterAmountMax, tilePos, new HydrationGain(WaterHydrationPerUnit));

        // Test seam: matches Rust struct-literal pattern of overriding amount.
        public static Resource WithAmount(ResourceKind kind, float amount, TilePos tilePos)
        {
            INeedEffect effect = kind switch
            {
                ResourceKind.Berry => new FedGain(BerryFedPerUnit),
                ResourceKind.Water => new HydrationGain(WaterHydrationPerUnit),
                _ => new FedGain(0f),
            };
            return new Resource(kind, amount, tilePos, effect);
        }

        // Pull one tick's worth from this resource. Returns the amount
        // actually taken (0 when depleted). Caller routes the taken amount
        // into Effect.Apply to update Vitals.
        public float ConsumeOneTick()
        {
            float taken = MathF.Min(Amount, ConsumePerTick);
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

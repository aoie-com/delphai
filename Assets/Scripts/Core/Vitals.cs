using System;

namespace DelphAi.Core
{
    // from delphai-core src/agent/behavior.rs as of commit 138bfe7 (fed only).
    // Hydration is forward-ported per migration.md (HYDRATION_DECAY=0.007/tick),
    // not present in Rust delphai-core (Sprint N7 unimplemented).
    public readonly struct Vitals : IEquatable<Vitals>
    {
        public readonly float Fed;
        public readonly float Hydration;

        public Vitals(float fed, float hydration)
        {
            Fed = fed;
            Hydration = hydration;
        }

        public static Vitals Default => new Vitals(1f, 1f);

        public Vitals WithFedDecay(float decay)
            => new Vitals(MathF.Max(0f, Fed - decay), Hydration);

        public Vitals WithHydrationDecay(float decay)
            => new Vitals(Fed, MathF.Max(0f, Hydration - decay));

        public Vitals WithFedGain(float gain)
            => new Vitals(MathF.Min(1f, Fed + gain), Hydration);

        public Vitals WithHydrationGain(float gain)
            => new Vitals(Fed, MathF.Min(1f, Hydration + gain));

        public bool Equals(Vitals other) => Fed == other.Fed && Hydration == other.Hydration;
        public override bool Equals(object obj) => obj is Vitals v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(Fed, Hydration);
        public override string ToString() => $"Vitals(fed={Fed:F3}, hyd={Hydration:F3})";
    }
}

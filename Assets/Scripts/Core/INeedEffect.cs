namespace DelphAi.Core
{
    public enum VitalKind
    {
        Fed,
        Hydration,
    }

    // Pure-function effect on Vitals. intensity = amount of one consume tick
    // (e.g. taken from Resource.ConsumeOneTick). Composite effects can chain
    // by Apply'ing in sequence.
    public interface INeedEffect
    {
        Vitals Apply(Vitals v, float intensity);
        bool Affects(VitalKind kind);
    }

    public sealed class FedGain : INeedEffect
    {
        public float PerUnit { get; }
        public FedGain(float perUnit) { PerUnit = perUnit; }
        public Vitals Apply(Vitals v, float intensity) => v.WithFedGain(PerUnit * intensity);
        public bool Affects(VitalKind kind) => kind == VitalKind.Fed;
    }

    public sealed class HydrationGain : INeedEffect
    {
        public float PerUnit { get; }
        public HydrationGain(float perUnit) { PerUnit = perUnit; }
        public Vitals Apply(Vitals v, float intensity) => v.WithHydrationGain(PerUnit * intensity);
        public bool Affects(VitalKind kind) => kind == VitalKind.Hydration;
    }
}

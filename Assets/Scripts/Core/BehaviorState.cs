namespace DelphAi.Core
{
    public enum BehaviorState
    {
        Idle,
        SeekingFood,
        Gathering,
        SeekingWater,
        Drinking,
    }

    // Discriminated union for what the World layer should do for a citizen
    // this tick. World maps actions onto move_target / Resource.ConsumeOneTick
    // / Effect.Apply. Decide is pure: it never mutates.
    public abstract record BehaviorAction
    {
        public sealed record Idle() : BehaviorAction;
        public sealed record Seek(TilePos Target) : BehaviorAction;
        public sealed record Consume(int ResourceIdx) : BehaviorAction;

        public static readonly Idle IdleSingleton = new Idle();
    }
}

namespace DelphAi.Core
{
    // from delphai-core src/agent/behavior.rs as of commit 138bfe7. Rust
    // version is fed-only; C# extends with hydration (Sprint N7 unimplemented
    // in Rust). Priority re-evaluation happens only in Idle (hysteresis,
    // matches Rust pattern + delphai-godot-rust/tasks/lessons.md).
    public static class Behavior
    {
        // Hunger threshold below which an Idle citizen seeks food. Above this
        // they idle. Rust 1:1.
        public const float FedLow = 0.4f;

        // Thirst threshold. Tighter than FedLow because HydrationDecay (0.007)
        // is faster than FedDecay (0.004) — proportional urgency.
        public const float HydrationLow = 0.3f;

        // Chebyshev distance at or below which a citizen is "adjacent enough"
        // to a resource to start consuming. 1 = 8-direction neighbor or same
        // tile. Rust 1:1.
        public const int GatherRange = 1;

        // Satiation threshold for exiting Gathering / Drinking. Set < 1.0 so
        // that the per-tick decay (0.004 / 0.007) doesn't immediately re-trip
        // the consume condition next tick — that's the "satiated trap" the
        // citizen would otherwise be stuck in (drinks forever because hyd
        // can't reach exactly 1.0 with decay running).
        public const float FedSatiated = 0.95f;
        public const float HydrationSatiated = 0.95f;

        // Pure decision function. Inputs: current behavior state, vitals,
        // citizen tile, nearest non-depleted food/water (with global resource
        // index). Output: new state to persist + action to take.
        //
        // State machine (priority on Idle re-evaluation):
        //   Idle: hyd<HydrationLow + water → SeekingWater. fed<FedLow + food
        //         → SeekingFood. Hydration wins when both low. else Idle.
        //   SeekingWater: water gone → Idle. cheby≤1 → Drinking + Consume.
        //                 else SeekingWater + Seek.
        //   SeekingFood:  food gone → Idle. cheby≤1 → Gathering + Consume.
        //                 else SeekingFood + Seek.
        //   Drinking:     water exists + cheby≤1 + hyd<1 → Drinking + Consume.
        //                 else Idle. Wandered/depleted/full all collapse.
        //   Gathering:    food exists + cheby≤1 + fed<1 → Gathering + Consume.
        //                 else Idle.
        public static (BehaviorState, BehaviorAction) Decide(
            BehaviorState state,
            Vitals vitals,
            TilePos citizenTile,
            (int Idx, TilePos Tile)? nearestFood,
            (int Idx, TilePos Tile)? nearestWater)
        {
            switch (state)
            {
                case BehaviorState.Idle:
                    if (vitals.Hydration < HydrationLow && nearestWater is { } w)
                    {
                        return (BehaviorState.SeekingWater, new BehaviorAction.Seek(w.Tile));
                    }
                    if (vitals.Fed < FedLow && nearestFood is { } f)
                    {
                        return (BehaviorState.SeekingFood, new BehaviorAction.Seek(f.Tile));
                    }
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);

                case BehaviorState.SeekingFood:
                    if (nearestFood is { } food)
                    {
                        if (Chebyshev(citizenTile, food.Tile) <= GatherRange)
                        {
                            return (BehaviorState.Gathering, new BehaviorAction.Consume(food.Idx));
                        }
                        return (BehaviorState.SeekingFood, new BehaviorAction.Seek(food.Tile));
                    }
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);

                case BehaviorState.SeekingWater:
                    if (nearestWater is { } water)
                    {
                        if (Chebyshev(citizenTile, water.Tile) <= GatherRange)
                        {
                            return (BehaviorState.Drinking, new BehaviorAction.Consume(water.Idx));
                        }
                        return (BehaviorState.SeekingWater, new BehaviorAction.Seek(water.Tile));
                    }
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);

                case BehaviorState.Gathering:
                    if (nearestFood is { } gf
                        && Chebyshev(citizenTile, gf.Tile) <= GatherRange
                        && vitals.Fed < FedSatiated)
                    {
                        return (BehaviorState.Gathering, new BehaviorAction.Consume(gf.Idx));
                    }
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);

                case BehaviorState.Drinking:
                    if (nearestWater is { } dw
                        && Chebyshev(citizenTile, dw.Tile) <= GatherRange
                        && vitals.Hydration < HydrationSatiated)
                    {
                        return (BehaviorState.Drinking, new BehaviorAction.Consume(dw.Idx));
                    }
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);

                default:
                    return (BehaviorState.Idle, BehaviorAction.IdleSingleton);
            }
        }

        public static int Chebyshev(TilePos a, TilePos b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            int adx = dx < 0 ? -dx : dx;
            int ady = dy < 0 ? -dy : dy;
            return adx > ady ? adx : ady;
        }
    }
}

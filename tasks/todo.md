# MVP TODO

**MVP:** 住民5–10人が協力して狩猟・採集・採水し、3段階の技術進化を遂げる俯瞰3Dシミュ。
LLM は Phase 2 で再活性化（Phase 1 中は外してある）。視覚参考: 0 A.D. 風。

**Phase 1 基準**: 起動直後に 5分以上眺めていられる（没入感・視覚的リッチさ）。

**移植元**: 旧 Godot+Rust 実装 (`delphai-godot-rust/`) を C# に移植中（Phase M1）。Rust 側のテスト 167 本は仕様書として参照、コピペは禁止。

---

## 検証順（コミット前）

1. Unity MCP `read_console` error/warning 0 確認
2. Unity Test Framework `run_tests` Edit Mode で対象テスト pass
3. **Don't burn the bridge**: `cd delphai-godot-rust && cargo test --workspace` 154+13=167 維持
4. （Mac 目視は Phase M4 でまとめて）

詳細は `@tasks/lessons.md` の「初日に踏みやすい地雷」を参照。

---

## 現在のスプリント — Phase M1.3 着手前の設計確定

> 移植進捗: M1.0 ✅ / M1.1 ✅ / M1.2 ✅。M1.3 (Behavior + Decide) は **Rust 未実装領域**（hydration / drink / hunt）に入るので、移植ではなく新設計。

### Resource ↔ Need の抽象化方針（移植スコープ外、Unity 期で新設計）

**問題**: 今のままだと `Gather/Drink/Hunt` が hardcoded、`ResourceKind` と需要 (fed/hydration) が 1:1 で結合。将来「ベリーは fed+hydration 両得」「水を汲んで陶器に入れて後で eat」「狩肉から皮も取る」が出ると破綻。

**方針**: `INeedEffect` interface で「効果」を分離、`Resource` が「どの効果を持つか」を保持。

```csharp
// 効果 (vitals をどう変えるか) — 純粋関数、Vitals は immutable struct
public interface INeedEffect
{
    Vitals Apply(Vitals v, float intensity);
}

// 単独効果
public sealed class FedGain : INeedEffect
{
    public float PerUnit { get; }
    public Vitals Apply(Vitals v, float i) => v.WithFedGain(PerUnit * i);
}
public sealed class HydrationGain : INeedEffect { /* 同形 */ }

// 複合（ベリー両得など、M1.4 以降）
public sealed class CompositeEffect : INeedEffect
{
    public IReadOnlyList<INeedEffect> Effects { get; }
    public Vitals Apply(Vitals v, float i)
    {
        foreach (var e in Effects) v = e.Apply(v, i);
        return v;
    }
}
```

`Resource` は所持する効果を持つ:
```csharp
public class Resource
{
    public ResourceKind Kind { get; }
    public INeedEffect Effect { get; }    // 1 個 (M1.3a) → IReadOnlyList<IConsumeAction> (M1.4+)
    public float AmountPerConsumeTick { get; }  // 旧 GatherPerTick
    ...
}
```

`Decide` は需要から「受け入れる effect を持つ最寄り resource」を探す:
```csharp
// hydration<0.3 が fed<0.4 より優先 (migration.md)
if (vitals.Hydration < HydrationLow)
    nearest = FindResource(r => r.Effect.Affects(VitalKind.Hydration));
else if (vitals.Fed < FedLow)
    nearest = FindResource(r => r.Effect.Affects(VitalKind.Fed));
```

### M1.3 段階的着地（スコープ膨張回避）

- **M1.3a (移植 + 抽象化の最小スライス)**:
  - [ ] `INeedEffect` interface + `FedGain` / `HydrationGain` 2 実装
  - [ ] `Resource` に `Effect` 1 個フィールド追加（既存 Gather() を `ConsumeOneTick(Vitals) → (Vitals, taken)` に統一）
  - [ ] `BehaviorState`: `Idle / SeekingFood / Gathering / SeekingWater / Drinking` の 5 値
  - [ ] 純粋関数 `Decide(state, vitals, tile, nearestFood, nearestWater) → (newState, action)` (hydration 優先)
  - [ ] World tick の decide フェーズ配線、Gather/Drink action は Effect.Apply で統一
  - [ ] テスト: `hydration_priority_trumps_food_when_both_low` / `drinks_then_eats_when_both_needs_low` / `runs_many_cycles_without_freezing` (10k tick + 食料水あり版)

- **M1.4 (Hunt + 複合効果の入口)**:
  - [ ] `Animal` (Deer) + `BehaviorState::Hunting` + Hunt action（Effect = FedGain）
  - [ ] hunt fallback テスト: `eventually_hunts_deer_when_berries_run_out`
  - [ ] CompositeEffect は導入だけ（複合 Resource 例は Phase 2 で）

- **M2 以降 (移植スコープ外)**:
  - [ ] `IConsumeAction` 抽象化（Gather / Drink / Hunt を統一インターフェース化、Resource が `IReadOnlyList<IConsumeAction>` を持つ）
  - [ ] `IItemEffect`（陶器・道具を生む消費アクション）
  - [ ] CompositeEffect の本格運用（ベリー fed+hydration 両得、Hunt fed+leather 両得）

### 設計上の注意

- `INeedEffect.Apply` は **純粋関数** (Vitals → Vitals)。tick decay と同じ層で扱える
- `Vitals` は引き続き immutable readonly struct（With\* の戻り値で更新）
- effect は struct ではなく class（複合構造を持つため allocation OK、Phase 1 は数個レベル）
- 当面 `Resource.Effect` は 1 個固定。複数所持に変えるとき `Effect` → `Effects` リストへ migration

### M1 残スコープ判断ライン

- M1.3a / M1.4 で **抽象化は最小限**、Phase 1 視覚 (M2-M4) を優先
- 「陶器・道具・複合効果」のフル抽象は Phase 2 LLM 再統合と同じレーンに送る
- 現時点で commit 増やすなら M1.3a → M1.4 と細かく刻む

---

## やらないこと

マルチプレイ、TTS、青銅器以降、DLC、コンソール、住民 100 人超。

## リファクタ保留（必要になったら着手）

- `MAP_W`/`MAP_H` の Rust/GDScript 共有化 → Unity 期は C# const に統合済（不要）
- `memory_summary` 初期値の 8 をコンフィグ化（Phase 2 LLM 再統合時）
- `Resource.Effect` 単数 → 複数の migration（M2 以降）

---

## Unity 移行 — Phase M0.5 完了 (2026-04-26)

**導入完了**: `CoplayDev/unity-mcp` v9.6.6 を Mac の Unity Editor に UPM 経由で導入、HTTP transport (port 8080) で `claude` から接続成功。`.mcp.json` は `host.docker.internal:8080` を向く。

**起動手順**（次回以降）:
1. Mac で Unity Editor を開く（プロジェクトルート `/workspaces/delphai`）
2. Window > MCP for Unity > Advanced > Allow LAN Bind (HTTP Local) ON、URL を `http://0.0.0.0:8080` にして Save
3. Stop Server → Start Server で再 bind
4. devcontainer で `claude` 起動 → `/mcp` GREEN を確認

**既知の制約**:
- Editor 起動 + Start Server を**毎回手動**でクリック必須
- `make build` / Godot 動作確認は devcontainer 不可、Mac ローカル責務
- M1 の C# 編集は `manage_asset` ではなく `Write/Edit` ツールで直接書いて `refresh_unity` で取り込む方が安定（asmdef の `.meta` 自動生成も同様）
- stdio fallback は Mac Bridge も LAN bind 必須なので機能しない、HTTP のみ採用

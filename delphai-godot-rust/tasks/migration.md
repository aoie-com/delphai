# Godot → Unity 移行計画

**決定日**: 2026-04-23
**ステータス**: 計画中（実装未着手）
**前提**: Phase 1（5 分眺めていられる）までのプロトタイプ。本格リリース前の方向転換。

---

## 移行の鉄則

### 🌉 Don't burn the bridge

**Unity 側が Phase 1 判定（M4）を通すまで、Godot + Rust 実装は一切削除しない。**

- 既存 Godot+Rust 一式は `delphai-godot-rust/` サブディレクトリにそのまま保存（`crates/` `game/` `prebuilt/` `Makefile` `tasks/` `docs/` を含む）
- Unity プロジェクトは **repo ルート**に配置（`Assets/` `Packages/` `ProjectSettings/` 等が `/workspaces/delphai/` 直下）
- `cd delphai-godot-rust && cargo test` / `make build` / Godot 起動は Phase M4 まで全部 green を保つ
- 移植中に Godot 側で bug を踏んでも、hotfix は Godot 側で直して OK（Unity 側に仕様として移す）
- **Phase M4 通過後のみ**、Phase M5 で `archive/godot-prototype` branch に退避 → main から `delphai-godot-rust/` ディレクトリごと削除
- 後戻りできる状態を 2〜3 週間維持する方が、焦って捨てて詰むより安い

---

## 決定事項

### ✅ D1. 中核ロジックの言語は **C# 一本化**（確定 2026-04-23）

**理由**:
- Unity Burst + Jobs + DOTS により Rust FFI に対する性能優位はほぼ消滅
- Rust を残すと FFI 境界設計に脇道に逸れ、Unity 習得の本来目的がブレる
- ロボティクスで Rust が効くのは ROS 2 ノード層・MCU firmware 層であり、**ゲームのビジネスロジックとは直交**。delphai に Rust を残しても直接はロボ力に繋がらない
- 既存 `delphai-core` のテスト 199 本は**仕様書として** C# 再実装時に流用できる（直接実行は失うが、挙動が 1:1 で移るかの確認基準になる）

**代償**:
- 決定論が弱くなる（強化学習環境化を将来やる際に再検討）
- Rust の `delphai-core` で育てた borrow checker ガード付きロジックは捨てる

### Q2. Git LFSなし

- root `.gitignore` でバイナリ拡張子（`*.fbx` `*.glb` `**/*.png` `*.wav` 等）を除外、`!**/icon.png` `!**/inventry.md` 等の必須軽量ファイルのみホワイトリスト
### Q3. MCPForUnity

- **`justinpbarnett/unity-mcp` は不採用** — 同リリースストリーム (v9.6.6) を CoplayDev 側が維持しており、実質的に CoplayDev に統合済
- **CI 統合**: M6 で要否判断、不要なら Phase 2 以降へ持ち越し

---

## 保留中の意思決定

### Q1. Terrain ソリューション

- **候補 A**: Unity Terrain（標準、無料、Terrain Tools パッケージで OK）
- **候補 B**: MicroSplat / MegaSplat（アセットストア、有料）
- **Phase 1 方針**: 候補 A で始める。シェーダ品質に不満が出たら B に移行
- **判断時期**: Phase M2 着手時

### Q2. CI 戦略

- Unity headless build は Unity Pro ライセンスが必要（Personal でも CLI build 可能だが制約あり）
- **Phase M4 まで**: Mac ローカルビルドのみ。Rust 側の `cargo test` は GitHub Actions で継続
- **Phase M5 以降**: Unity ビルドの CI 化を検討、ローカル Mac で十分ならスコープ外

---

## 現資産の棚卸し

### 活かすもの（設計・ロジック・アセット）

| カテゴリ | 資産 | 活用方法 |
|---|---|---|
| **シミュレーションロジックの設計** | `delphai-core`（behavior/world/animal/resource/move_state）の状態機械と tick 順序 | C# に再実装。Rust コードは**仕様書**として参照 |
| **ゲームバランス定数** | `FED_DECAY=0.004` / `HYDRATION_DECAY=0.007` / `BERRY_AMOUNT_MAX=2.0` / `GATHER_PER_TICK=1.0` 等（N7/N8 で tuning 済） | そのまま流用、再チューニング不要 |
| **教訓** | `@tasks/lessons.md` / `@tasks/todo.md` の「N?初日の地雷」セクション | Unity 版でも通用する普遍則を抽出（tick 順序、優先順位再評価の Idle 限定、hunt fallback トリガ条件、など） |
| **アセット** | `game/assets/animal/Animals_FREE.glb` / `nature/various-forest-assets-pack/` / SFX | GLB は Unity でも読める。人型モデル（Unreal 向け FBX）は Unity Humanoid rig で retarget |
| **テスト資産** | `delphai-core` の 199 個のユニットテスト | ロジック再実装時の期待挙動の辞書。C# 側で同名テストを NUnit/Unity Test Framework で書き直す |
| **CODEMAPS** | `docs/CODEMAPS/*.md` | Godot 版アーキ図として保全。Unity 版の CODEMAPS は Phase M2 で新規作成 |

### Phase M4 まで保存、M5 で削除するもの

| 対象 | 削除時期 |
|---|---|
| `delphai-godot-rust/crates/delphai-core/` | Phase M5（C# 移植が Phase 1 判定を通過してから） |
| `delphai-godot-rust/crates/delphai-gdext/` | Phase M5 |
| `delphai-godot-rust/crates/delphai-bench/` | Phase M5（LLM 再統合時に Unity 側で新設） |
| `delphai-godot-rust/game/scenes/` / `delphai-godot-rust/game/scripts/` | Phase M5 |
| `delphai-godot-rust/game/addons/terrain_3d/` | Phase M5 |
| `delphai-godot-rust/game/demo/` / `delphai-godot-rust/game/project.godot` / `delphai-godot-rust/game/delphai.gdextension` | Phase M5 |
| `delphai-godot-rust/prebuilt/` | Phase M5 |
| `delphai-godot-rust/Makefile` の Godot/Rust ターゲット | Phase M5（Unity 用に root の新 Makefile へ書き換え） |
| `delphai-godot-rust/CLAUDE.md` | Phase M5（root の `CLAUDE.md` に統合済みの場合のみ） |

> 実際の M5 作業は「`delphai-godot-rust/` ディレクトリごと `git rm -r delphai-godot-rust/`」で完結する想定。ファイル単位ではなくディレクトリ単位の退避になる。

> **削除前に `archive/godot-prototype` branch を作ってから main で削除する。** M5 は「削除」ではなく「退避 + main から外す」作業。

### Phase M5 以降も保存

> M5 で `delphai-godot-rust/` をディレクトリごと削除する**前**に、以下のファイルだけ root 側へ退避する。

| 対象（旧 path） | 退避先 | 理由 |
|---|---|---|
| `delphai-godot-rust/docs/CODEMAPS/` | `docs/CODEMAPS-godot/` | 「引き継ぐべきだったこと / 引き継がないこと」の根拠資料 |
| `delphai-godot-rust/tasks/lessons.md` | `tasks/lessons.md`（root） | 次の落とし穴を踏まないため |
| `delphai-godot-rust/tasks/todo.md` の N8 までの tuning 履歴 | `tasks/todo.md`（root） | ゲームバランスの根拠資料 |
| `delphai-godot-rust/tasks/migration.md` | `tasks/migration.md`（root） | この計画書自体の保存 |

---

## 移行フェーズ（Don't burn the bridge 準拠）

### Phase M0 / M0.5 ✅ 完了 (2026-04-26)

- Unity プロジェクトは **repo ルート直下**。`unity/` サブへ後移動はメタ再生成リスクで NG、Godot+Rust 側を `delphai-godot-rust/` へ退避するのが正解 (commit `aa0c0e5`)
- Unity プロジェクトを `~/Documents/` 配下に置くと iCloud 同期で `Library/PackageCache/` が `* 2` 重複して破損ループ (`@tasks/lessons.md` 参照)
- Git LFS は採用しない。アセット (fbx/glb/png/wav) は git 追跡せず外部ストレージ。root `.gitignore` でバイナリ除外、必須軽量のみホワイトリスト
- Unity MCP は HTTP transport 一択。stdio fallback は Mac Bridge 側でも LAN bind 設定が要るので機能しない
- Mac の Unity Editor 側で毎セッション: `Window > MCP for Unity > Advanced > Allow LAN Bind ON` + URL を `http://0.0.0.0:8080` にして Save → Stop → Start。`127.0.0.1` のままだと container から届かない
- devcontainer 側の `.mcp.json` は `host.docker.internal:8080/mcp` を向く（`localhost` ではない）

### Phase M1: 中核ロジック C# 移植（2〜3 日）

**Godot 側の動作に一切触らない**（`delphai-godot-rust/` 配下は読み取り専用扱い）

#### 移植元の規模（2026-04-26 棚卸し）

| ファイル | LOC | M1 移植 | 備考 |
|---|---:|---|---|
| `crates/delphai-core/src/world.rs` | 679 | ✅ | tick 順序の本体 |
| `crates/delphai-core/src/move_state.rs` | 381 | ✅ | 補間 + step + step_with_grid |
| `crates/delphai-core/src/agent/behavior.rs` | 270 | ✅ | `decide` 純粋関数 |
| `crates/delphai-core/src/pathfinding.rs` | 158 | 部分 | `WalkGrid` の最低限のみ（`step_with_grid` 経由で必要） |
| `crates/delphai-core/src/resource.rs` | 139 | ✅ | Berry / Water |
| `crates/delphai-core/src/agent/citizen.rs` | 120 | ❌ | LLM 専用、Phase 2 で再構築 |
| `crates/delphai-core/src/llm/*` | — | ❌ | Phase 2 で再構築 |
| **合計** | ~1,750 | ~1,500 | citizen.rs / llm/ を除外 |

#### スライス計画（各スライスで 3 点検証 + commit）

| Slice | 範囲 | 残タスク / RED → GREEN テスト |
|---|---|---|
| **M1.0** ✅ | プロジェクト雛形 + asmdef (commit `ac6920f`) | — |
| **M1.1** ✅ | `TilePos` + `MoveState` + `Citizen` + `World` 補間 (commit `cd31481`) | — |
| **M1.2** ✅ | `Vitals` + `Resource` + decay/regen tick (commit `b9caf8e`) | — |
| **M1.3** ✅ | `INeedEffect` + `BehaviorState` (5 値) + 純粋関数 `Decide` + Gather/Drink、満腹閾値 0.95、Berry regen を Rust 0.01 → Unity 0.04 に逸脱（`@tasks/lessons.md`） | 31 tests (Effect 5 / Behavior 19 / WorldBehavior 7) — hydration 優先・drinks_then_eats・10k 生存全 pass (2026-04-26) |
| **M1.4** | `Animal`（Deer）+ hunt fallback | `eventually_hunts_deer_when_berries_run_out` |

#### M1 完了スライスから引き継ぐ確定事項

- tick 順序は Rust 版と同一: `decay → decide → step → history → regenerate → random_walk`
- ゲームバランス定数は `delphai-godot-rust/crates/delphai-core/` と 1:1 同期（C# 側コメントで `// from delphai-core <file>:<sym> as of commit <sha>` と紐付け）
- 名前空間: `DelphAi.Core` / `DelphAi.Core.Tests`、`DelphAi.Core` asmdef は `noEngineReferences=true` で UnityEngine 依存禁止
- テスト asmdef は `defineConstraints: ["UNITY_INCLUDE_TESTS"]` 必須、`autoReferenced: false`
- 新 asmdef 追加直後は MCP `refresh_unity` を `scope=all` で叩く（`scope=scripts` だと `.meta` 生成が走らずテストランナーに認識されない）
- `MathF.Round` は `MidpointRounding.AwayFromZero` 明示（既定の ToEven は Rust `f32::round` と一致しない）
- `Vitals` は immutable readonly struct。`List<Vitals>` の indexer はコピー返しなので `list[i] = list[i].WithFedDecay(x)` で書き戻す（フィールド直接代入はコンパイルエラー）
- 各スライスの 3 点検証:
  1. Unity MCP `read_console` error 0 + `run_tests` Edit Mode で当該テスト pass
  2. devcontainer `cd delphai-godot-rust && cargo test --workspace` 154+13=167 維持（Don't burn the bridge）
  3. Mac 目視は最終 Slice 後にまとめて

#### 含めない（M1 スコープ外）

- `WalkGrid` 全機能（M2 で地形と一緒に） — M1 では `step_with_grid` の最低限だけ
- LLM 連動 (`citizen.rs` / `llm/`) — Phase 2 で再構築
- gdext FFI 相当 — Unity では C# 直接、FFI 不要
- random_walk は M1.4 まで保留（テストの邪魔になるため）
- `IConsumeAction` 抽象化・複合 effect・陶器/道具系 — M2 以降（`@tasks/todo.md` 参照）

### Phase M2: 視覚・地形（2 日）

- [ ] Unity Terrain で 24×14 + Gaussian 山（`H_MAX=3.0` / `σ=4 tiles`）を再現
- [ ] grass/rock テクスチャ適用（Unity Terrain の Layer System は slot が明示的、Godot Terrain3D の slot 0/1 の罠は無い）
- [ ] `PEAK_BLOCK_HEIGHT_M=2.5` ルールで歩行可能判定（N9.1 hotfix の踏襲）
- [ ] citizen / animal / berry / water の仮ビジュアル（primitive、アセット差し替えは M3）
- [ ] カメラ pan/zoom（Cinemachine 検討）
- [ ] Unity 版 CODEMAPS を root の `docs/CODEMAPS-unity/` に新規作成（Godot 版は `delphai-godot-rust/docs/CODEMAPS/` に保存中）

### Phase M3: アニメーションとアセット（2〜3 日）

- [ ] アセット共有方式を Q3 で決定 → `Assets/Imported/` に GLB/FBX コピー（方式 b なら二重管理）
- [ ] 人型 FBX を Unity Humanoid rig で retarget（`MM_Walk_Fwd.fbx` の出発点を確認、現在 root にあるか `delphai-godot-rust/` 配下か棚卸し）
- [ ] `Animals_FREE.glb::deer` を Unity で読み込み、Animator Controller を組む
- [ ] `various-forest-assets-pack` の tree 3 種（Oak / PineTree / Birch1）を Unity Terrain の Tree system で非歩行タイルに散布
- [ ] アセット LICENSE 再確認（Unity 向け配布で問題ないか）

### Phase M4: Phase 1 判定（Unity 版、1 日）

- [ ] 住民 5 / berry 2 / water 3 / deer 3 / 山 1 の全部入り
- [ ] **5 分眺めていられる判定**（Mac Unity Editor、これは Godot 版と同じ基準）
- [ ] **hunt が 2 分以内に発火**（N8 基準の継続）
- [ ] **ここが鉄則の解除ライン**: M4 が green になるまで Godot+Rust は触らない

### Phase M5: 旧 Godot+Rust プロジェクトの退避（0.5 日）

**M4 通過を確認してから実施**

- [ ] `archive/godot-prototype` branch を切り、main の現状を一旦そこへ push
- [ ] root へ退避: `delphai-godot-rust/tasks/lessons.md` `tasks/todo.md` `tasks/migration.md` `docs/CODEMAPS/` を root の `tasks/` `docs/CODEMAPS-godot/` へ移動
- [ ] `git rm -r delphai-godot-rust/` でディレクトリごと削除
- [ ] root に新 `Makefile` を作成（`make unity-build` `make unity-test` など、必要なら）
- [ ] root の `README.md` / `CLAUDE.md` を Unity 用に更新（検証コマンド項は M6 で正式書き換え）
- [ ] Unity 版 CODEMAPS は引き続き `docs/CODEMAPS-unity/` を維持
- [ ] D2 反転の経緯は `tasks/migration.md` に履歴として残す（このファイル自体）

### Phase M6: Unity MCP 運用整備（0.5 日）

**M5 完了後に実施。Godot+Rust が archive 退避済で、検証順を Unity 系へ正式に置換するタイミング。**

- [ ] `CLAUDE.md` の検証コマンド「1. Godot MCP エラー確認」→「1. Unity MCP エラー確認」に書き換え
  - `delphai-godot-rust/CLAUDE.md`（旧側）と repo ルートの `CLAUDE.md` の両方を確認、最新が指す Unity MCP に統一
- [ ] M0.5 〜 M5 で蓄積した MCP 利用知見を `tasks/lessons.md` に追記
  - どの操作で MCP が効いたか / 効かなかったか / シーン編集の注意点
- [ ] Unity MCP の制約・既知バグを `docs/CODEMAPS-unity/mcp-operations.md` として残す
- [ ] CI への組み込み要否を判断（Q5 / Q6 とリンク）
  - 不要なら「Phase 2 以降で再検討」と明記して保留して構わない
- [ ] 新規メンバー向けセットアップ手順を `README.md` に追記（MCP server 起動 → Claude Code 接続）

**完了基準**: 新規メンバーが README だけで Unity MCP 経由の Claude Code 操作を再現できる。`CLAUDE.md` 検証順 1 が Unity MCP に置換済。

---

## リスク

| リスク | 影響 | 緩和策 |
|---|---|---|
| **Unity 学習コストの過小評価** | 工期 2x | M1 着手時点で 1 日使って見積もり更新、超過したら scope を縮める |
| **C# Burst/Jobs 習熟コスト** | N8 で tuning した性能が 5 人規模でも出ない | まず managed C# で通す、Phase 1 は 5 人前提なので Burst は不要のはず。ボトルネック計測してから Burst 化 |
| **人型 FBX の retarget 失敗** | M3 遅延 | Mixamo など既存リグで retarget、最悪 M4 は primitive のまま進める |
| **アセット使用権の確認漏れ** | ライセンス違反 | M3 前に `game/assets/inventry.md` の全アセット LICENSE を再確認 |
| **Godot 側が移行中に腐る** | M4 で戻れない | M0 の時点で `cargo test` / `make build` / Godot 起動が green を確認、以降各 Phase 完了時に再確認 |
| **M5 での削除ミス** | 退避 branch なしに main から消す | M5 は「archive branch 作成 → push 確認 → main で削除」の手順を必ず守る |
| **アセット二重管理の肥大化** | LFS 容量・git 履歴 | Q3 で方式 (a) を選ぶなら早期に、(b) なら M5 で一括削除 |
| **C# 移植の挙動ドリフト** | N7/N8 の tuning が崩れる | M1 の移植テストで N7/N8 の regression test を必ず通す |
| **Unity MCP の不安定さで M1〜M3 が阻害** | 工期 +0.5 日 | MCP で詰まったら手作業 fallback、MCP が効かない領域は lessons.md に都度記録 |
| **MCP 経由のシーン編集で `.unity` 破損** | M1 以降のロールバック工数 | M0.5 は読み取り中心 + Cube 1 個まで、本格運用前にコミット粒度を細かく |

---

## 検証コマンド

**Phase M0〜M4**（並行期）:

1. Godot 側（`delphai-godot-rust/` 配下で実行）: `cd delphai-godot-rust && cargo test --workspace` / `cargo clippy --workspace --all-targets -- -D warnings` / `make build` / Mac Godot 起動（全部 green 維持）
2. Unity 側（root で実行）: Unity Editor コンソール空 / Unity Test Framework Edit Mode pass / Mac Unity Editor で Play
3. Unity MCP（M0.5 完了以降）: MCP server 起動 / Claude Code から接続成功 / Editor 操作の応答エラーなし

**Phase M5 以降**（M6 完了後の正式形）:

1. Unity MCP エラー確認（`CLAUDE.md` 検証順 1 を Godot MCP から置換、M6 で実施）
2. Unity Editor コンソール空
3. Unity Test Framework Play Mode / Edit Mode pass
4. 人間目視（Mac Unity Editor で Play、5 分眺め基準）

`CLAUDE.md` の「検証順」セクションは M6 で書き換え（M5 は退避作業まで、検証順の正式置換は M6）。

---

## Open Questions（未解決、着手前に人間と合意）

- [ ] Q1〜Q6（上記「保留中の意思決定」）の各判断時期
- [ ] Q6 の最終判断は Phase M0.5 着手時（実装候補の動作確認込み）
- [ ] Unity MCP の CI 統合は Phase M6 で要否判断、不要なら Phase 2 以降に持ち越し
- [ ] Phase 2（LLM 再統合）以降の Unity 内 LLM 推論アーキテクチャ（`delphai-bench` の役割を Unity 側でどう再構築するか、Phase M5 以降に継続議論）
- [ ] ロボティクス学習トラックを別ファイル（`tasks/robotics-learning.md`）で分離するか

---

## 参照

- 旧実装の教訓: `@tasks/lessons.md`
- 現スプリント状況: `@tasks/todo.md`
- アーキテクチャ C4 図（Godot 版）: `@docs/CODEMAPS/`
- アセット在庫: `@game/assets/inventry.md`

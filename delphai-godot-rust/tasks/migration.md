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

## なぜ移行するか

1. **アセット互換性** — Unreal/Unity 向けの 3D アセット（人型モデル含む）を既に所有。Godot への変換コストが高い
2. **エコシステム** — チュートリアル・ドキュメント・アセットストアの厚み、Unity を正しく習得できる
3. **ロボ学習との親和性**（副次的） — Unity Robotics Hub / ROS-TCP-Connector に将来アクセスしやすい

**許容するコスト**:
- ロイヤリティリスク（Unity Personal $200k/年まで無料。現段階では実質ゼロ）
- クローズドソースへの依存
- 現スプリント N9.1 の Terrain3D / 斜面歩行チューニング資産の大半（教訓は残る）

**スコープ外**: ロボティクス学習は別トラック（`tasks/robotics-learning.md` で今後扱う、本移行には含めない）。

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

### ✅ D2. リポジトリ構造: **モノレポ（Unity をルート、Godot+Rust をサブディレクトリ）**

**変更点（2026-04-26）**: 当初は Unity を `unity/` サブディレクトリに置く計画だったが、Unity 6 LTS のプロジェクト作成が **repo ルート直下**で完了したため、構造を反転：旧 Godot+Rust 一式を `delphai-godot-rust/` 配下にまとめ、Unity を root に配置する形で確定した。

```
/workspaces/delphai/
├── Assets/                    ← Unity プロジェクト（root）
├── Packages/                  ← Unity packages
├── ProjectSettings/           ← Unity settings
├── Library/ Logs/ Temp/ UserSettings/  ← Unity 自動生成（gitignore）
├── delphai-godot-rust/        ← 旧 Godot+Rust 一式、Phase M4 まで保存
│   ├── crates/                ← Rust ロジック
│   ├── game/                  ← Godot プロジェクト
│   ├── prebuilt/              ← .dylib 等
│   ├── Makefile               ← `make build` / `make smoke-*`
│   ├── tasks/                 ← lessons.md / todo.md / migration.md
│   ├── docs/                  ← CODEMAPS など
│   └── CLAUDE.md              ← Godot 期の検証順
├── tasks/                     ← Unity 期の TODO（必要なら新設）
├── docs/                      ← Unity 期の CODEMAPS（M2 で新規）
├── .gitignore                 ← Unity 用（root）
├── .gitattributes             ← Git LFS 設定（M0 で導入）
└── CLAUDE.md                  ← Unity 期の検証順（M6 で正式更新）
```

**理由**: 別 repo にすると cross-reference（lessons.md / CODEMAPS / 既存ゲームバランス定数）が断絶する。モノレポならコミット履歴が連続。
**反転の根拠**: Unity プロジェクトを `unity/` サブディレクトリに後から押し込むには `Assets/`〜`ProjectSettings/` を一括移動 + Unity 側のメタ再生成が必要で、リスクが高い。**Unity 側は触らず Godot+Rust を `delphai-godot-rust/` へ退避** したほうが破壊が少ない。

---

## 保留中の意思決定

### Q1. Unity バージョン

- **候補**: Unity 6 LTS（2024.x LTS 後継）。DOTS 1.x / Entities 1.x が安定
- **要検証**: URP vs HDRP（Phase 1 は URP 想定、パフォーマンス優先）
- **判断時期**: Phase M0 着手時（Mac 上で Unity Hub から LTS 選定）

### Q2. Terrain ソリューション

- **候補 A**: Unity Terrain（標準、無料、Terrain Tools パッケージで OK）
- **候補 B**: MicroSplat / MegaSplat（アセットストア、有料）
- **Phase 1 方針**: 候補 A で始める。シェーダ品質に不満が出たら B に移行
- **判断時期**: Phase M2 着手時

### Q3. アセット共有 (`assets-shared/`) の運用

- Godot の `game/assets/` にある GLB/FBX/SFX を Unity からも参照したい
- 選択肢:
  - (a) `assets-shared/` を新設し両方が参照（Godot の `.import` メタを壊さず移せるか要検証）
  - (b) Unity 側に `unity/Assets/Imported/` としてコピーし、移行完了まで二重管理
- **Phase M3 着手時**に決定。(b) が安全、(a) が美しい

### Q4. Git LFS 切替タイミング — ✅ **採用しないことに変更（2026-04-26）**

- **方針転換**: Git で追跡するのはコードベースのみ。アセット（3D モデル / テクスチャ / 音声 / 動画 / Unity package 等）は repo に入れない
- アセット入手は外部ストレージ（Google Drive / Dropbox / S3 / Unity Asset Store の都度 DL）で運用
- root `.gitignore` でバイナリ拡張子（`*.fbx` `*.glb` `**/*.png` `*.wav` 等）を除外、`!**/icon.png` `!**/inventry.md` 等の必須軽量ファイルのみホワイトリスト
- LFS 関連: `git lfs uninstall` 済 / `.gitattributes` 削除済
- 旧 Godot 期の `delphai-godot-rust/game/assets/` は inner `.gitignore` で既に対象外、LFS は出番無し
- **例外**: `delphai-godot-rust/prebuilt/*.dylib` は Godot 動作に必要な build artifact として inner `.gitignore` の `!prebuilt/**/*.dylib` で tracked のまま（Don't burn the bridge 解除の M5 で削除）

### Q5. CI 戦略

- Unity headless build は Unity Pro ライセンスが必要（Personal でも CLI build 可能だが制約あり）
- **Phase M4 まで**: Mac ローカルビルドのみ。Rust 側の `cargo test` は GitHub Actions で継続
- **Phase M5 以降**: Unity ビルドの CI 化を検討、ローカル Mac で十分ならスコープ外

### Q6. Unity MCP 実装の選定

- **候補 A**: `coplaydev/unity-mcp`（star 数最多、UPM 経由 / Python ブリッジ、暫定推奨）
- **候補 B**: `justinpbarnett/unity-mcp`（軽量、個人 fork）
- **判断基準**: メンテ頻度 / Unity 6 LTS 対応 / Claude Code 公式 MCP プロトコル準拠
- **判断時期**: Phase M0.5 着手時（候補 A で動作確認、ダメなら B へ）
- **CI 統合**: M6 で要否判断、不要なら Phase 2 以降へ持ち越し

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

### Phase M0: Unity 環境セットアップ（0.5 日）✅ 大半完了 (2026-04-26)

**並列作業、Godot+Rust は無影響**

- [x] Unity Hub + Unity 6 LTS インストール（Mac 上）
- [x] **repo ルート直下** に新規 Unity プロジェクト作成（当初予定の `unity/` サブディレクトリではなくルート直下、D2 反転参照）
- [x] 旧 Godot+Rust 一式を `delphai-godot-rust/` サブディレクトリへ移動
- [x] Mac で Unity エディタ起動確認、空シーンが Play できる
- [x] root `.gitignore` を Unity テンプレで整備（`Library/` `Temp/` `Obj/` `Build/` `Logs/` `UserSettings/` 等）
- [x] 旧 Godot/Rust 用 `.gitignore` は `delphai-godot-rust/.gitignore` に保存（`/target` `*.import` `.godot/` `**/.DS_Store` 等）
- [x] **Git LFS — 不採用に方針変更（Q4 更新）** — アセット（fbx/glb/png/wav 等）は git で追跡せず外部ストレージ管理
  - `git lfs uninstall` 済、`.gitattributes` 削除済
  - root `.gitignore` でバイナリ拡張子を除外、必須軽量ファイルのみホワイトリスト
  - Mac 側で別途 `git lfs install` を走らせる必要は無い
- [x] **鉄則の踏み絵**: `cd delphai-godot-rust && cargo test --workspace` が引き続き green（2026-04-26: **167 tests passed** / 154 in delphai-core + 13 in delphai-gdext）
  - `make build` / Godot 起動は Mac 環境で別途確認（ローカル責務、devcontainer では godot バイナリ不在）
- [x] root の `.gitignore copy` を削除（古い Godot 用 ignore のバックアップ、不要）
- [x] `.DS_Store` を root `.gitignore` に追加 + `git rm --cached .DS_Store` で untrack

### Phase M0.5: Unity MCP 最小導入（0.5 日）

**目的**: M1〜M3 の作業効率化のため、Claude Code から Unity Editor を直接操作できる状態を作る。M0 完了後・M1 着手前に実施。

**前提**: Phase M0 完了（Unity 6 LTS / repo ルート直下に Unity プロジェクト存在 / Mac で Play 可）

- [ ] Unity MCP 実装の最終選定（Q6、30 分以内）
  - 暫定: `coplaydev/unity-mcp`（Unity 6 LTS 対応・直近メンテを再確認）
- [ ] Unity Package Manager から git URL で MCP package 追加
- [ ] Python ブリッジを `pipx` か venv で隔離インストール
- [ ] Claude Code 側 MCP 設定（プロジェクト `.mcp.json` 推奨）に Unity MCP server を登録
- [ ] 接続スモークテスト
  - [ ] Hierarchy / Scene 一覧が Claude Code から取得できる
  - [ ] 空シーンに Cube を MCP 経由で 1 個追加 → Editor 目視確認
  - [ ] Editor コンソールエラーが出ていない
- [ ] **Don't burn the bridge 踏み絵**: Godot 側 `cargo test --workspace` / `make build` / Mac Godot 起動が引き続き green
- [ ] `tasks/todo.md` に「Unity MCP 導入完了 / 接続手順 / 既知の制約」を 5 行で残す

**完了基準**: Claude Code が Unity Editor の Hierarchy を読み、GameObject を 1 個追加できる。Godot 側 3 種 green 維持。

**注意**: M0.5 では「読み取り中心 + Cube 追加 1 個」までに絞る。シーン破壊リスクのある複雑な編集は M1 以降で慎重に運用。

### Phase M1: 中核ロジック C# 移植（2〜3 日）

**Godot 側の動作に一切触らない**（`delphai-godot-rust/` 配下は読み取り専用扱い）

- [ ] `Assets/Scripts/Core/` に `World` / `Citizen` / `Animal` / `Resource` / `MoveState` / `BehaviorState` を C# で再実装
- [ ] tick 順序は Rust 版と同一: `decay → decide → step → history → regenerate → random_walk`
- [ ] ゲームバランス定数は `delphai-godot-rust/crates/delphai-core/` と 1:1 同期（コメントで `// from delphai-core N? as of commit <sha>` と紐付け）
- [ ] Unity Test Framework (Edit Mode) で最低限の回帰テストを移植:
  - [ ] `hydration_priority_trumps_food_when_both_low`
  - [ ] `citizen_runs_many_cycles_without_freezing`（10k tick 飢え死にしない）
  - [ ] `citizen_eventually_hunts_deer_when_berries_run_out`（hunt fallback 発火）
  - [ ] `citizen_drinks_then_eats_when_both_needs_low`（Satiated trap 回避）
- [ ] **検証**: C# 側テスト pass + `cd delphai-godot-rust && cargo test --workspace` も引き続き pass

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

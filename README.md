# DelphAI

原始時代から現代まで、LLMで駆動する住民たちの文明進化を見守るシミュレーションゲーム。

プレイヤーは声を届けるが、住民が従うかは彼ら次第。
戦争は愚かであるというメッセージを届けたい。

DLC: 住民を細胞、世界を人体にした版。

> Godot+Rust 版から Unity (C# 一本化) へ移行中。Phase M0.5 着手中。旧実装は `delphai-godot-rust/` に Phase M4 通過まで保存。経緯と判断は `delphai-godot-rust/tasks/migration.md`。

---

## 開発環境セットアップ

### Unity MCP（devcontainer の Claude Code から Mac の Unity Editor を操作する）

Mac で動く Unity Editor を、devcontainer 内の Claude Code から MCP 経由で操作する設定。
パッケージ: [`CoplayDev/unity-mcp`](https://github.com/CoplayDev/unity-mcp) v9.6.6 (HTTP transport)

**Mac 側（Unity Editor 起動後、毎セッション）:**

1. `Window > MCP for Unity` を開く
2. **Advanced** セクションで `Allow LAN Bind (HTTP Local)` をチェック
3. **Connection** セクションの `HTTP Local URL` を `http://0.0.0.0:8080` に書き換えて Save
4. Stop Server → Start Server で再 bind
5. Mac terminal で確認:
   ```bash
   lsof -nP -iTCP:8080 -sTCP:LISTEN
   # → *:8080 (LISTEN) が出れば OK。127.0.0.1:8080 のままだと Docker から届かない
   ```

**devcontainer 側（自動）:**

- ルートの `.mcp.json` が `http://host.docker.internal:8080/mcp` を指す
- `.devcontainer/devcontainer.json` の `forwardPorts: [8080]` で VS Code Ports タブから状態が見える

> stdio transport は採用しない。container 内 Python (uvx) から Mac の Unity Bridge に届けるには結局 Bridge 側でも LAN bind 設定が要り、HTTP より手数が増えるだけで fallback として機能しないため。

**接続確認:**

```bash
curl -sS http://host.docker.internal:8080/mcp     # 200 系が返れば OK
claude                                            # /mcp で unityMCP が緑
```

**よくある詰み:**

| 症状 | 原因 | 対処 |
|---|---|---|
| `curl: Failed to connect ... 8080` | Mac で Start Server 押してない / 127.0.0.1 bind のまま | 上記手順 1〜4 を見直す |
| `lsof` が `127.0.0.1:8080` のまま | URL フィールドの Save 忘れ | URL 編集 → Save → Stop → Start |
| `/mcp` で `unityMCP` が出ない | プロジェクト trust 未承認 / `.mcp.json` 不在 | `claude` 起動時の trust prompt に承認 |
| `~/Documents/` 配下で Unity が破損 | iCloud Drive 同期で `Library/PackageCache/` が `* 2.cs` に複製 | iCloud > Drive > "Desktop & Documents" を OFF、`Library/` を `rm -rf` して再オープン |

---

## よく間違えること

### プレイヤーにできることは3つだけ

1. プロンプトを入力する（声を届ける）
2. 住民の声を聴く
3. 世界の様子を確認する

それ以外の操作を足さない。住民を直接動かせない。建物を建てられない。プレイヤーは「声」だけの存在。

### 住民は最初プレイヤーの声が聞こえない

認知度、信頼度0%からスタートする。声を届けてもノイズとして無視される。
これをスキップするチュートリアルを作らない。この「伝わらなさ」がゲーム体験の核。

### Unity プロジェクトを `~/Documents/` 配下に置かない

iCloud Drive 同期で `Library/PackageCache/` が `* 2.cs` として複製され、コンパイルエラーが永遠に直らない。
`~/project/` などホーム直下の iCloud 範囲外に置く。すでにハマったら `Library/` を `rm -rf` で再生成。

### Phase M4 通過まで Godot+Rust を消さない

`delphai-godot-rust/` 配下は Phase M4（Unity 版で「5分眺めていられる」判定）通過まで触らない。
hotfix が要るときは Godot 側で直して Unity 側に仕様として移す。詳細は `delphai-godot-rust/tasks/migration.md` の "Don't burn the bridge" 節。

### LLMは住民全員に毎ターン走らせない

RCTの最大の教訓: **ゲームデザインで計算量を回避する。** RCTはゲストの経路探索を「目的地を選んでから歩く」ではなく「分岐点でランダムに方向を選ぶ」設計に変えて、パスファインディングの爆発を消した。

同じ原則をLLMに適用する:

- 各ターンでLLM推論するのは住民の5-10%だけ。残りはルールベースAI
- 住民は普段は黙々と働いている。会話が発生した時だけLLM
- 「住民100人の大会議」はやらない。「代表者5人の会議 + 群衆はルールベース」

### 1回のLLMコールで複数住民をシミュレートする

住民1人に1回のAPIコール、ではない。入力トークン数の限界まで詰め込んで、1コールで複数住民の会話を同時に生成する。これがこのゲームのスループットを決定的に左右する。

注意点:
- 詰め込みすぎると出力品質が落ちる。ベンチマークで最適なバッチサイズを見つける
- モデルのコンテキストウィンドウ (例: 4096トークン) から出力分を引いた残りが入力の上限
- バッチ内の住民ペアは独立していること。ペアAの結果がペアBに影響する場合はバッチにできない

### LLMスループットをベンチマークで管理する

「なんとなく速くなった気がする」で最適化しない。指標を定義して計測する。

```
LLM効率スコア = (会話品質スコア × 会話数) / 処理時間(秒)
```

| 指標 | 計測方法 | 目標 |
|------|---------|------|
| 会話品質スコア (0-1) | YAML パース成功率 × キャラ一貫性 × 世界観逸脱なし | ≥ 0.7 |
| 会話数 / コール | 1回のLLMコールで生成された会話ペア数 | ≥ 3 |
| 処理時間 | LLMコール発行から全レスポンス受信まで | ≤ 5秒 |
| LLM効率スコア | 上記の複合指標 | ≥ 0.4 |

Phase 2 で LLM を再統合する時点で Unity Test Framework + ベンチハーネスを組む（旧 Godot 期は `cargo bench --bench llm_throughput` で運用）。

このベンチマークで:
- モデル変更時に品質が落ちていないか検知する
- プロンプト変更時にバッチサイズの最適値がズレていないか確認する
- ハードウェアごとの推奨バッチサイズを決定する

### LLMプロバイダーは1つに縛らない

RimWorld LLM mod（RimTalk/EchoColony）の教訓。全て複数プロバイダー対応で成功している。

| 優先 | プロバイダー | 理由 |
|------|------------|------|
| 1st | Player2 | APIキー不要。プレイヤーはアプリを起動するだけ。セットアップの壁がない |
| 2nd | ローカルLLM (llama.cpp) | オフライン動作。買い切りの長期保証。Player2が死んでもゲームは動く |
| 3rd | クラウドAPI (OpenAI等) | ユーザーが自分でAPIキーを取得。上級者向け |

Player2をデフォルトにする。「Ollamaをインストールして、モデルをダウンロードして...」で20人中19人が脱落する（RimTalk開発者の実測値）。

### 技術ツリーはプロトコルで定義する

最初は技術ツリーだけ実装する。ただしハードコードしない。
このプロトコルに従えば社会ツリーも文化ツリーも同じコードで動く。ただしMVPでは技術ツリーだけ。

### 住民の記憶は全会話履歴ではない

コンテキストウィンドウは有限。全部入れると破綻する。

- 住民の個性: タグ5-10個（`勇敢`, `懐疑的`, `農民`）
- 記憶: 圧縮サマリー100-200トークン。古い記憶はLLMで要約して圧縮
- 関係性: `{相手の名前: 関係性}` の短いリスト

### LLMの出力フォーマットはスケールで変える

自由形式のテキストを返させない。パースできなくなる。

**現在: YAML**（JSONより30%速く32%少ないトークン。Gemma4 E2B実測）

```yaml
speech: あの山の向こうに何があるか、見に行こう
inner_thought: 長老は反対するだろうな...
action: propose_exploration
emotion_change: excited
tech_hint: null
```

| フォーマット | avg latency | トークン | 採用条件 |
|---|---|---|---|
| JSON | 1792ms | 103 | ❌ 遅い |
| YAML | 1248ms | 70 | ✅ Phase 2 開始時 |
| HDL（独自形式） | 未計測 | 推定30以下 | 住民20人超でYAMLがボトルネックになった時点で調査 |

**HDL（Human-readable Data Language）について**

住民が20人を超えると、1コールあたりのYAML出力量がスループットのボトルネックになる可能性がある。その時点で独自の超コンパクト形式を検討する。openageの「nyanデータ記述言語」が先例。フィールド名を位置で暗示し、区切り文字だけで表現する案:

```
K|あの山の向こうに何があるか|長老は反対するだろうな...|explore|excited|-
```

**導入条件**: `住民数 ≥ 20 かつ YAML パース成功率 < 90% またはlatency > 3秒` になった時点で調査開始。それまではYAMLのまま。

パース失敗時のフォールバックはどのフォーマットでも必ず書く。LLMは必ず壊れた出力を返す日が来る。

---

## アーキテクチャ

```
Unity 6 LTS (3D俯瞰) ──────▶ C# (シミュレーション) ──[Phase 2]──▶ LLM推論
(表示/UI/入力)                  (needs-driven AI)                    (Player2 / Ollama)
                                (技術ツリー / 人口成長)
```

**Phase 1（移行中）**: LLMなし。住民はneeds-driven AIで自律行動する。
- 食料・水の欲求 → 最寄り資源へ経路探索 → 採取/飲水
- 技術ツリー（石器→農業→青銅器）を研究ポイントで解禁
- 繁栄が続くと新市民が誕生（最大8人）

**Phase 2（LLM再統合）**: 会話イベント発生時のみLLMを呼ぶ。住民の普段の行動はPhase 1のまま。

Unity 6 LTS (C#): アセット互換性（人型 FBX 含む既存資産）と Burst+Jobs+DOTS による性能で選定。Godot/Rust 版で得た決定論的シミュレーションは、ゲームバランス定数（`FED_DECAY=0.004` 等、N7/N8 で tuning 済）と tick 順序（`decay → decide → step → history → regenerate → random_walk`）として C# に 1:1 移植する。Rust の `delphai-core` 199 テストは仕様書として保全。

LLM (Phase 2): Gemma4 E2B（avg 1248ms YAML）を primary。1B-4Bの量子化モデル (Q4_K_M)。7Bは重すぎる。

---

## ビジネス

目標はClaude Code Maxプランの売上 ($200/month)

価格は$18、安すぎると品質が低いと思われて逆に買いたくなくなるため

Unity Personal は $200k/年売上まで無料。本作の価格帯では実質ロイヤリティゼロ。

---

## 参考

| 作品 | 何を学ぶか |
|------|-----------|
| Civilization | 技術ツリーの設計 |
| RimWorld + RimTalk | LLM NPC会話の実装パターン、Player2統合 |
| RollerCoaster Tycoon | デザインレベルでの最適化。「重い機能は設計で回避する」哲学 |
| OpenRCT2 | リバースエンジニアリングによる最適化のオープンリポジトリ |
| 0 A.D. | 古代戦争シミュレーションのAI設計（Petra AI）、市民兵の経済/軍事の二面性 |
| openage | AoE2エンジンのリバースエンジニアリングから得られた最適化知見、nyanデータ記述言語 |

---

## リポジトリ構成

```
/workspaces/delphai/
├── Assets/ Packages/ ProjectSettings/   ← Unity プロジェクト本体
├── .mcp.json                            ← Unity MCP 接続設定
├── .devcontainer/                       ← devcontainer (Claude Code 環境)
├── delphai-godot-rust/                  ← 旧 Godot+Rust 一式（Phase M4 まで保存）
│   ├── crates/                          ← Rust シミュレーション (delphai-core 等)
│   ├── game/                            ← Godot プロジェクト
│   ├── tasks/migration.md               ← 移行計画
│   ├── tasks/lessons.md                 ← 旧期の教訓
│   └── README.md                        ← Godot 期の README（Phase M5 で root へ統合）
└── CLAUDE.md                            ← 判断基準・検証順
```

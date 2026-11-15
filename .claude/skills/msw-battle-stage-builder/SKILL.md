---
name: msw-battle-stage-builder
description: Magic-science-world（Unity製2Dシューティング）のBattle Sceneで、ステージ（道中・会話・ボス戦）や敵・ボスの攻撃/移動を BehaviorTree で実装・修正するためのスキル。BattleStageAsset・BossDefinition・BehaviorTreeAsset・MotionPreset の作成、ステージ固有サービス（風など）の宣言、StageData/Addressables への登録、BGM・会話の接続、再生確認までをガイドする。ユーザーが「新ステージ」「ステージ3を実装」「道中を追加」「ボス戦を作りたい」「弾幕を追加」「敵の攻撃パターンを作りたい」「中ボスを作る」「発狂(Strong)フェーズを追加」「BT/ツリーを組みたい」「ビームを撃たせたい」のように言った場合、たとえ「ステージ」という単語を使っていなくても必ずこのスキルを参照すること。Battle Scene 関連のアセット作業だと判断したら真っ先にこのスキルを読むこと。
---

# Battle Stage Builder（BehaviorTree 版）

Battle Scene のステージ進行・敵/ボスの行動は BehaviorTree（以下 BT）で記述する。
旧形式（`AttackPreset` / `AttackTimeline` / `MovementPreset` と `Stage{N}/Attack|Movement|Ease|Bullet` のデータ）はコード・データともに削除済み。敵の移動と攻撃はすべて BT で書く。

設計の詳細とノード一覧はリポジトリのドキュメントを正とする。

- 仕組み: `Document/Battle/BehaviorTree.md`
- ノード・パラメータの一覧: `Document/Battle/BehaviorTree-Nodes.md`
- データ配置: `Document/Battle/DataStore.md`

## 大原則

1. **Unity 形式のファイル（`.asset` / `.prefab` / `.unity` / `.meta`）をテキストとして直接書き換えない。** 作成・変更は Unity Editor 上で行う（ユーザーへの手順案内、または Unity MCP の `execute_code` で Editor API を使う）。詳細は [authoring.md](references/authoring.md)。
2. **新しい C# ファイルを作る前に必ずユーザーの許可を取る**（`AGENTS.md`）。既存ノードの組み合わせで書けるならコードを足さない。
3. ユーザーが「〜したい」と操作方法を聞いている場合は、実装せずに BT エディタでの手順を案内する。
4. **機械的に展開しない。** 同じ攻撃を Delay で並べる、向きだけ違うアセットを量産する、といった書き方はしない。繰り返しは `Repeat`、本数と角度は `ShotPattern`、共通の攻撃は `SubTree` で表す。[patterns.md](references/patterns.md) の「やってはいけない書き方」を必ず確認する。
5. **ステージ固有の機能は自機や Presenter に直接実装しない。** `StageServiceDefinition` / `IStageService` としてカプセル化し、使うステージの `BattleStageAsset.Services` にだけ宣言する。サービス側から自機・弾へ働きかけ、自機は汎用の受け口（`IMoveModifier` など）だけを持つ。
6. ユーザーが Inspector で調整した値を、想定値で上書きしない。

## 仕組みの要点

```
StageData.asset (stageNumber ごとに stageAddress)
  └─ BattleStageAsset (Stage{N}_BT)          … 1 ステージ = 1 アセット
       ├─ Services: [WindService など]       … このステージでだけ有効な機能
       └─ Tree: Stage{N}_Tree                … ステージツリー。Success で完了するとステージクリア
            Stage
              ├─ Section "Way"  [Way]  : Sequence[ PlayBgm(BattleWay), Wave..., Wait 120 ]
              └─ Section "Boss" [Boss] : Sequence[ Scenario(ボス前の会話, skipOnResume=ON)
                                                     ├─ OnCommand "SpawnBoss"   → SpawnBoss(Stage{N}_Boss)
                                                     └─ OnCommand "PlayBossBgm" → PlayBgm(BattleBoss)
                                                  , BossBattle, Wait 120
                                                  , Scenario(撃破後の会話, skipOnResume=OFF, restorePlayerControl=OFF) ]

BossDefinition (Stage{N}_Boss)  … Prefab / 出現位置 / 登場移動 / ボスのツリー (Stage{N}Boss_Tree)

Commons/BT/Enemy  … 全ステージ共通の雑魚ツリー
  Sequence[ SetDeathBehavior($death), SetMovement($movement), SubTree($attack) ]

BehaviorTreeAsset
  ├─ Root: NodeDefinition (Composite / Decorator / Action)
  └─ Parameters: $名前 で参照できる値 (SubTree・Spawn から上書き可)
```

- **時間**: BT のノードは **フレーム（60fps 固定）**。移動（`MotionStep`）の時間は **秒**。
- **進行**: 道中・会話・ボス戦の順序はすべてステージツリーに書く。Section はリトライ地点。会話中のコマンド（`SpawnBoss` など）は Scenario ノードの子の OnCommand で処理する。
- **敵**: `Wave` の entries（または `Spawn` ノード）で Prefab と Behavior を指定して出す。雑魚の Behavior は共通ツリー `Commons/BT/Enemy` にし、Overrides で `movement`（`Motion/` の MotionPreset）/ `attack`（`Attack/` の攻撃ツリー）/ `death`（撃破時のツリー、任意）を渡す。攻撃中に移動を変えるなど、この形に収まらない特殊な敵だけ `Enemy/` に専用ツリーを作る。雑魚 Prefab は `ActorPool` で使い回される（使い回せない Prefab には `NoActorPool` を付ける）。
- **弾**: BT の外の `BulletSystem` が動かす（自機の弾も同じ）。BT からは `NWayShot` などの弾を撃つノード（撃ち方ごとに別ノード）や `BulletBeam` で撃つだけ。
- **割り込み・フェーズ**: `Until(条件)` で表す。BGM 同期は `BgmSamples`、HP 閾値は `HpBelow`、発狂は `NormalHpDepleted` → `EnterStrongMode`。
- **再利用**: 攻撃は `Attack/` の別ツリーにして参照する（コピーしない）。雑魚は `Enemy` の `attack` パラメータ、ボスは `SubTree` で参照する。

## ワークフロー

1. **要件確認**: ステージ番号、道中 / ボス、敵構成・弾幕、BGM の区切り、会話（ボス前・撃破後）、既存アセットとの関係（`StageData.asset` のエントリ）を確認する。
2. **参照実装を読む**: `Assets/Project/DataStore/Stage{1-4}/BT/` の近いツリーを BT エディタ（`Window/Battle/Behavior Tree Editor`、またはアセットをダブルクリック）で開き、構成と粒度を掴む。YAML を読む場合は読み取りのみ。
3. **下から作る**（[authoring.md](references/authoring.md)）:
   1. `MotionPreset` / `EaseCurve`（必要なものだけ）
   2. 攻撃ツリー（`Attack/`）。特殊な敵がいるときだけ専用ツリー（`Enemy/`）
   3. ボスのツリー（`Stage/Stage{N}Boss_Tree`）と `BossDefinition`（`Stage{N}_Boss`）
   4. ステージツリー（`Stage/Stage{N}_Tree`）。Stage → Section を組み、会話は Scenario ノードで参照して OnCommand を付ける
   5. `BattleStageAsset`（`Stage{N}_BT`）。必要ならサービスを宣言する
4. **登録**: `StageData.stageAddress` と Addressables を設定し、BGM の登録を確認する → [stage-registration.md](references/stage-registration.md)
5. **検証**: コンパイルエラーとコンソールを確認し、`Tools/Battle Debug Launcher` でステージと開始 Section を選んで再生する。検証のコツは [authoring.md](references/authoring.md) の「動作確認」を参照。

## reference 一覧

- [authoring.md](references/authoring.md): アセットの作り方（エディタ操作の案内、MCP `execute_code` での組み立て）、命名、動作確認の方法
- [patterns.md](references/patterns.md): 道中・ボス・敵・攻撃のよく使う組み方と、やってはいけない書き方
- [stage-registration.md](references/stage-registration.md): DataStore の配置、StageData / Addressables、BGM、会話の接続

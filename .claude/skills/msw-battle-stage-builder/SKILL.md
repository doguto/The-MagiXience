---
name: msw-battle-stage-builder
description: Magic-science-world（Unity製2Dシューティング）のBattle Sceneで、ステージ（道中・ボス戦）や敵・ボスの攻撃/移動を BehaviorTree で実装・修正するためのスキル。BattleStageAsset・BehaviorTreeAsset・MotionPreset の作成、ステージ固有サービス（風など）の宣言、StageData/Addressables への登録、BGM・シナリオ接続、再生確認までをガイドする。ユーザーが「新ステージ」「ステージ3を実装」「道中を追加」「ボス戦を作りたい」「弾幕を追加」「敵の攻撃パターンを作りたい」「中ボスを作る」「発狂(Strong)フェーズを追加」「BT/ツリーを組みたい」「ビームを撃たせたい」のように言った場合、たとえ「ステージ」という単語を使っていなくても必ずこのスキルを参照すること。Battle Scene 関連のアセット作業だと判断したら真っ先にこのスキルを読むこと。
---

# Battle Stage Builder（BehaviorTree 版）

Battle Scene のステージ進行・敵/ボスの行動は BehaviorTree（以下 BT）で記述する。
旧形式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset` / `AttackPreset` / `MovementPreset`）は実行時に使われておらず削除予定なので、**新しく作ってはいけない**。

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
StageData.asset (stageNumber ごとに way / boss のアドレス)
  └─ BattleStageAsset (Stage{N}Way_BT / Stage{N}Boss_BT)
       ├─ Services: [WindService など]       … このステージでだけ有効な機能
       ├─ Tree: BehaviorTreeAsset            … 道中: ステージ直下で実行。完了で次へ
       └─ Boss Prefab / 出現位置 / 登場移動 / Boss Tree … ボス戦: ボス自身が Boss Tree を実行。撃破で完了

BehaviorTreeAsset
  ├─ Root: NodeDefinition (Composite / Decorator / Action)
  └─ Parameters: $名前 で参照できる値 (SubTree・Spawn から上書き可)
```

- **時間**: BT のノードは **フレーム（60fps 固定）**。移動（`MotionStep`）の時間は **秒**。
- **敵**: `Spawn` ノードで Prefab と Behavior（敵用ツリー）を指定して出す。敵用ツリーは `SetMovement` と攻撃で構成する。
- **弾**: BT の外の `BulletSystem` が動かす。BT からは `Fire` / `BulletBeam` などで撃つだけ。
- **割り込み・フェーズ**: `Until(条件)` で表す。BGM 同期は `BgmSamples`、HP 閾値は `HpBelow`、発狂は `NormalHpDepleted` → `EnterStrongMode`。
- **再利用**: 攻撃は `Attack/` の別ツリーにして `SubTree` で参照する（コピーしない）。

## ワークフロー

1. **要件確認**: ステージ番号、道中 / ボス、敵構成・弾幕、BGM の区切り、既存アセットとの関係（`StageData.asset` のエントリ）を確認する。
2. **参照実装を読む**: `Assets/Project/DataStore/Stage{1-4}/BT/` の近いツリーを BT エディタ（`Window/Battle/Behavior Tree Editor`、またはアセットをダブルクリック）で開き、構成と粒度を掴む。YAML を読む場合は読み取りのみ。
3. **下から作る**（[authoring.md](references/authoring.md)）:
   1. `MotionPreset` / `EaseCurve`（必要なものだけ）
   2. 攻撃ツリー（`Attack/`）と敵ツリー（`Enemy/`）
   3. ステージツリー（`Stage/`。道中の進行、またはボスのフェーズ）
   4. `BattleStageAsset`（`Stage{N}Way_BT` / `Stage{N}Boss_BT`）。必要ならサービスを宣言する
4. **登録**: `StageData.asset` と Addressables を設定し、BGM とシナリオの命名規則を確認する → [stage-registration.md](references/stage-registration.md)
5. **検証**: コンパイルエラーとコンソールを確認し、`Tools/Battle Debug Launcher` で道中・ボスを再生する。検証のコツは [authoring.md](references/authoring.md) の「動作確認」を参照。

## reference 一覧

- [authoring.md](references/authoring.md): アセットの作り方（エディタ操作の案内、MCP `execute_code` での組み立て）、命名、動作確認の方法
- [patterns.md](references/patterns.md): 道中・ボス・敵・攻撃のよく使う組み方と、やってはいけない書き方
- [stage-registration.md](references/stage-registration.md): DataStore の配置、StageData / Addressables、BGM、シナリオ接続

---
name: msw-battle-stage-builder
description: Magic-science-world（Unity製2Dシューティング）のBattle Sceneに新しいステージ（道中・ボス戦）を実装するためのスキル。DataStore配下へのAttack/Movement/Easeプリセット・TimelineBuilder・BattleSequenceAsset作成、StageDataとAddressableへの登録、BGM・シナリオ接続までを一貫してガイドする。ユーザーが「新ステージ」「ステージ3を実装」「道中を追加」「ボス戦を作りたい」「弾幕を追加」「敵の攻撃パターンを作りたい」「中ボスを作る」「発狂(Strong)フェーズを追加」のように言った場合、たとえ「ステージ」という単語を使っていなくても必ずこのスキルを参照すること。このプロジェクトでは.asset/.prefab/.unity/.metaの直接編集が禁止されているため、少しでもBattle Scene関連のアセット作業だと判断したら真っ先にこのスキルを読むこと。
---

# Battle Stage Builder

Magic-science-worldのBattle Sceneに新規ステージ（道中/ボス戦）を実装するための手順書。

## 大原則（最優先で厳守）

**`.asset` / `.prefab` / `.unity` / `.meta` を直接編集・直接生成してはならない。**

リポジトリの `WARP.md` に明記されている通り、これらの拡張子はレビューが困難なため、Edit/Writeツールでの直接書き込みは一切禁止。ScriptableObjectアセットの新規作成・フィールド値の設定・Prefabの編集・シーンへの配置は、すべて **Unity Editor MCP経由**で行うこと（`unity-mcp-skill` を併読し、Unity Editorを実際に操作してアセットを作る）。

編集してよいのは `.cs` ファイルのみ。新規の移動パターン(`IMovementStep`実装)や新規ExitCondition種別など、既存のプリセット/コンポーネントの組み合わせでは表現できない挙動が必要な場合に限り、C#の追加・修正を行う。

## このスキルの対象

- 新規ステージの道中（Way）シーケンス実装
- 新規ステージのボス（Boss）シーケンス実装
- 既存ステージへのフェーズ追加・弾幕（攻撃パターン）追加
- ボスの発狂（Strong）フェーズ追加

## 仕組みの全体像

1体のステージは **道中シーケンス** と **ボスシーケンス** の2本の `BattleSequenceAsset` で構成される（`Assets/Project/Scenes/Battle/Scripts/Model/BattleSequenceAsset.cs`）。

```
BattleSequenceAsset (ScriptableObject, CreateAssetMenu: "Battle/Phase Sequence")
├─ situation: BattleSituation (Way/Boss)  ※アセット名に"Way"/"Boss"を含めるとOnValidateで自動設定される
├─ sequenceGroups: List<SequenceGroup>
│   └─ SequenceGroup { loop, loopCount(0=無限), phases: List<BattlePhaseDefinition> }
│       └─ BattlePhaseDefinition { phaseId, timelineBuilder, timelineBuilderStrong, exitConditionConfig }
├─ bossPrefab / bossSpawnPosition   ※ボス用シーケンスのみ使用
└─ bossEntranceMovement: List<IMovementStep>  ※ボス入場演出
```

- 各 `BattlePhaseDefinition` は **1フェーズ** を表し、`BattleTimelineBuilderAsset`（敵スポーン・Signal・Audio等のトラック定義）から実行時に `TimelineAsset` を生成する。
- フェーズの終了条件は `IExitConditionConfig`（SerializeReference、`AllEnemiesDefeated` / `TimeLimit` / `BgmPosition` / `BossHpThreshold` / `Composite`）で決まる。
- ボスの弾幕・移動は `AttackPreset` / `MovementPreset` という別アセットに切り出され、`BattleTimelineBuilderAsset` の `bossAttackPreset` / `bossMovementPreset` から参照される。
- 発狂（Strong）フェーズは `timelineBuilderStrong` に別のTimelineBuilderを設定するだけでよく、発動判定自体は `BossEntityPresenter.strongHpRatio`（Boss Prefab側の設定）で行われる。`BattlePhaseDefinition`側に閾値フィールドは**存在しない**（過去のスキーマの名残がアセットYAML上に残っていることがあるが、現行コードでは使われていない）。

各要素の詳細は用途に応じて以下のreferenceを読むこと。

## ワークフロー

1. **要件確認**: ステージ番号、道中/ボス/両方どちらを作るか、敵構成・弾幕パターン、BGM、既存ステージとの関係（`Assets/Project/DataStore/StageData.asset` にエントリ済みか）をユーザーに確認する。
2. **参照実装を読む**: `Assets/Project/DataStore/Stage1/` と `Stage2/` の該当アセット、および対応するC#を実際に読み、命名規則・粒度を掴む。特にStage1は道中(Phase1〜9)とボス(Intro/MelodyA/MelodyB/Chorus+Strong版)の両方が揃っており最も参考になる。
3. **下から上へUnity MCP経由で作成する**:
   1. `AttackPreset` / `MovementPreset` / `EaseCurvePreset`（必要なもののみ） → [presets-and-timelines.md](references/presets-and-timelines.md)
   2. `BattleTimelineBuilderAsset`（フェーズごとに1つ） → [presets-and-timelines.md](references/presets-and-timelines.md)
   3. `BattleSequenceAsset`（道中/ボスそれぞれ1つ、`SequenceGroup`→`BattlePhaseDefinition`で上記TimelineBuilderとExitConditionを組み合わせる）→ 道中なら [way-sequence.md](references/way-sequence.md)、ボスなら [boss-sequence.md](references/boss-sequence.md)
   4. ボスの場合はBoss Prefab（`BossEntityPresenter`必須）も用意 → [boss-sequence.md](references/boss-sequence.md)
4. **ステージ登録**: `StageData.asset` の該当エントリに `waySequenceAddress` / `bossSequenceAddress` を設定し、シナリオ・BGMの命名規則に沿ってシーン遷移が繋がることを確認する → [stage-registration.md](references/stage-registration.md)
5. **検証**: Unity MCPでコンパイルエラー・コンソールエラーがないか確認し、可能であれば実際にBattle Sceneを再生して道中→ボス→シナリオ遷移が正しく動くか確認する。

## reference一覧（必要なものだけ読む）

- [stage-registration.md](references/stage-registration.md): DataStoreレイアウト全体、StageData登録、Addressableアドレスの規則、BGM対応、シナリオ接続の仕組み
- [way-sequence.md](references/way-sequence.md): 道中フェーズの設計・敵スポーントラックの組み方・ExitCondition選択指針
- [boss-sequence.md](references/boss-sequence.md): Boss Prefab構成・入場移動・楽節連動フェーズ・Strong版・HP/BGM系ExitCondition
- [presets-and-timelines.md](references/presets-and-timelines.md): AttackPreset/MovementPreset/EaseCurvePresetとBattleTimelineBuilderAssetのトラック種別詳細

# 道中（Way）シーケンスの作り方

## 全体構造

道中は `BattleSequenceAsset`（`situation: Way`）1つで表現され、通常は `loop: false` の単一 `SequenceGroup` の中に、複数の `BattlePhaseDefinition`（=フェーズ）を時系列で並べる。

Stage1の実例（`Assets/Project/DataStore/Stage1/Way/Stage1Way.asset`）:

```yaml
situation: 0  # Way
sequenceGroups:
- loop: 0
  loopCount: 0
  phases:
  - phaseId: 1
    timelineBuilder: {Phase1.asset への参照}
    timelineBuilderStrong: {fileID: 0}   # 道中フェーズは通常Strong版を持たない
    exitConditionConfig: AllEnemiesDefeatedExitConditionConfig
  - phaseId: 2
    timelineBuilder: {Phase2.asset への参照}
    ...
  # phaseId 3, 4, 5, 6 と続く（Stage1は道中に9フェーズ相当の資産があるが、
  # Stage1Way.asset自体に登録されているのは一部のみ。実際に使われているフェーズ数は
  # Stage1Way.assetを直接読んで確認すること）
```

- `phaseId` は文字列で、フェーズの識別子（表示用途やデバッグ用途に使われる想定。連番の数値文字列でよい）。
- 道中フェーズは基本的に `timelineBuilderStrong` を設定しない（ボスのような発狂概念は道中には無い）。
- `exitConditionConfig` はStage1の道中フェーズでは全て `AllEnemiesDefeatedExitConditionConfig` が使われている（そのフェーズでスポーンした敵を全滅させたら次フェーズへ進む、という設計）。

## フェーズ用TimelineBuilderの組み方（敵スポーン）

各フェーズは1つの `BattleTimelineBuilderAsset`（例: `Phase1.asset`）に対応する。道中フェーズで主に使うのは `enemySpawnTracks` のみで、`bossAttackPreset` / `bossMovementPreset` はボス専用のため空でよい。

Stage1 `Phase1.asset` の実例:

```yaml
enemySpawnTracks:
- trackName: EnemySpawn
  clips:
  - time: 2.5
    spawnPosition: {x: 10, y: -3, z: 0}
    prefab: {敵Prefabへの参照}
  - time: 3.25
    spawnPosition: {x: 10, y: -3, z: 0}
    prefab: {敵Prefabへの参照}
  # ... time順に並べる。1回のクリップ = 1体のスポーン
```

- `time` は秒単位のタイムライン上の位置。フェーズ開始からの経過秒数。
- `spawnPosition` はワールド座標（画面右端付近から出現させる設計が多い、Stage1では `x: 10` が右端出現位置の目安）。
- `prefab` はスポーンする敵Prefab（`Assets/Project/Scenes/Battle/Prefabs/` 配下、`EnemyEntityPresenter` を持つもの）への参照。
- 同時刻に複数体を出したい場合は同じ `time` のクリップを複数並べる。
- 敵側の攻撃パターン・移動パターンは敵Prefab自身（`EnemyEntityPresenter`＋その敵専用のAttack/MovementPreset）が持つ。TimelineBuilder側で個々の敵の弾幕までは制御しない（それは敵Prefab側の責務）。

## ExitConditionの選び方（道中向け）

`Assets/Project/Scenes/Battle/Scripts/Model/ExitCondition/` 配下のいずれかを `[SerializeReference, SubclassSelector]` で選択する。

| 種別 | 用途 | フィールド |
|---|---|---|
| `AllEnemiesDefeatedExitConditionConfig` | そのフェーズでスポーンした敵を全滅させたら終了。**道中の基本形。** | なし |
| `TimeLimitExitConditionConfig` | 一定時間経過で強制的に次フェーズへ。敵を倒しきらなくても進行させたい弾幕耐久フェーズなどに。 | `timeLimitSeconds`（デフォルト10秒） |
| `CompositeExitConditionConfig` | 複数条件のAND/OR合成。 | `mode`(And/Or), `conditions: List<IExitConditionConfig>` |

道中では `BgmPositionExitConditionConfig` / `BossHpThresholdExitConditionConfig` はほぼ使わない（BGM楽節連動・HP閾値はボス戦向けの仕組み。詳細は[boss-sequence.md](boss-sequence.md)参照）。

## 敵の攻撃パターンを新規に作りたい場合

道中の「弾幕を追加」は、実際には以下のいずれかを指す:
1. 既存の敵Prefabの出現数・出現タイミング・出現位置の調整 → 上記の `enemySpawnTracks` の編集だけで完結。
2. 新しい弾幕を持つ敵を追加 → 敵Prefab自体に新規の `AttackPreset` / `MovementPreset` を割り当てる必要があり、[presets-and-timelines.md](presets-and-timelines.md) を参照。

## ループフェーズについて

`SequenceGroup.loop` を `true` にし `loopCount` を設定すると、その中の `phases` を指定回数（`0`なら無限）繰り返す。長時間の耐久区間や、同じ敵編成を繰り返すセクションに使える。Stage1/Stage2では基本的に `loop: false` の単一グループのみが使われているため、ループを使う場合は挙動をUnity MCP上で実際に再生して確認すること。

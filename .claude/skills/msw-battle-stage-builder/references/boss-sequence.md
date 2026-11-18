# ボス（Boss）シーケンスの作り方

## 全体構造

ボスは `BattleSequenceAsset`（`situation: Boss`）1つで表現され、`bossPrefab` / `bossSpawnPosition` / `bossEntranceMovement` を追加で持つ。フェーズはBGMの楽節（イントロ→メロディA→メロディB→サビ、のような構成）に対応させるのが基本パターン。

Stage1の実例フェーズ構成（`Assets/Project/DataStore/Stage1/Boss/`）: `Intro` → `MelodyA` → `MelodyB` → `Chorus`、それぞれに対応する `IntroStrong` `MelodyAStrong` `MelodyBStrong` `ChorusStrong` が `timelineBuilderStrong` として用意されている。

## Boss Prefabの構成

ボスPrefabは `Assets/Project/Scenes/Battle/Prefabs/Stage{N}/` に配置し、必ず `BossEntityPresenter`（`Assets/Project/Scenes/Battle/Scripts/Presenter/Entity/BossEntityPresenter.cs`）をアタッチする。主要フィールド:

| フィールド | 役割 |
|---|---|
| `maxHp` | ボスの最大HP |
| `contactDamage` | 接触時にプレイヤーへ与えるダメージ |
| `strongHpRatio` (0〜1) | **発狂モード切り替わりのHP比率。`BattlePhaseDefinition`側ではなくここで一元管理される。** |
| `overflowDamageMultiplier` | HP超過ダメージの扱い |
| `bulletPools` | 弾種ごとの `BulletPool` 配列。`OnValidate`で子オブジェクトの`BulletPool`を自動収集する仕組みあり |
| `bulletDamage` | 弾のダメージ量 |
| `enemySpawnPrefabs` | ボスが子機を呼び出す場合のPrefab配列 |
| `view` (`BossEntityView`) | 見た目・HPバー表示 |
| `deathDirector` (`BossDeathDirector`) | 死亡演出 |

- `BossEntityView` を要求コンポーネントとするため、Prefab作成時は `RequireComponent` により自動アタッチされる。
- HPバーは `NormalHp`（`strongHpRatio`より上の分）と `StrongHp`（`strongHpRatio`以下の分）の2段階で管理・表示される（`BossEntityModel`側のロジック。詳細実装が必要な場合は `Assets/Project/Scenes/Battle/Scripts/Model/Entity/BossEntityModel.cs` 相当を確認すること）。

## 発狂（Strong）フェーズの発動の流れ

1. `BattleScenePresenter.SpawnBoss()` が `phaseStateMachine.OnPhaseStarted` を購読し、各フェーズ開始時に `ShouldUseStrongAttack(phase)` を判定する。
2. 判定は `bossPresenter.Model.ShouldUseStrongAttack`（= 現在HPが`strongHpRatio`を下回っているか）を見ているだけで、フェーズ定義自体には閾値情報を持たない。
3. Strongと判定されれば `bossPresenter.Model.EnterStrongMode()` を呼び、使用するTimelineBuilderも `phase.BuilderStrong`（= `timelineBuilderStrong`）に切り替える。
4. `BossEntityPresenter.OnPhaseStarted(phase, builder)` が渡された `builder` の `BossAttackPreset` / `BossMovementPreset` を適用する。

つまり **Strong版フェーズを作る際は、`timelineBuilderStrong` に通常版とは別のTimelineBuilder（発狂用の攻撃・移動プリセット参照）を割り当てるだけでよく**、発動条件そのものはBoss Prefab側の`strongHpRatio`一箇所を調整すれば全フェーズに一律適用される。フェーズごとに異なる発狂閾値を持たせる仕組みは現状存在しない。

## 入場移動（bossEntranceMovement）

`BattleSequenceAsset.bossEntranceMovement`（`List<IMovementStep>`, SerializeReference）に、ボス出現時の演出用移動ステップを並べる。`BattleScenePresenter.SpawnBoss()` 内で `bossPresenter.PlayEntranceMovement(bossSequence.BossEntranceMovement)` として一括再生される（DOTween Sequenceで順に繋げて再生）。

利用可能な `IMovementStep` 実装は `Assets/Project/Scenes/Battle/Scripts/Model/Movement/` 配下（`TweenMovementConfig` `ReflectMovementConfig` `LoopMovementConfig` `WaitMovementConfig` `SineMovementConfig` `QuadraticMovementConfig` `PathMovementConfig` `InfiniteMovementConfig` `ForwardMovementConfig` `DriftMovementConfig` `AnimationMovementConfig` `AimStraightMovementConfig` `AimRotateConfig` `AcceleratedMovementConfig` 等）。入場演出は「画面外から所定の停止位置まで動く」程度がStage1の例（`Movement/BossIntro.asset`）。詳細な各Stepの意味はUnity MCPでフィールドをInspector表示するか、該当`.cs`を直接読んで確認すること。

## ExitConditionの選び方（ボス向け）

`Assets/Project/Scenes/Battle/Scripts/Model/ExitCondition/` 配下から選択する。

| 種別 | 用途 | フィールド |
|---|---|---|
| `BgmPositionExitConditionConfig` | BGMの再生位置（サンプル数）が閾値に達したらフェーズ終了。**楽節連動フェーズの基本形。** イントロが終わったらメロディAへ、といった「曲に合わせた」進行に使う。 | `thresholdSamples`（AudioSourceのサンプル位置。曲のBPM・尺から逆算して設定する） |
| `BossHpThresholdExitConditionConfig` | ボスHPが指定%以下になったらフェーズ終了。 | `hpThresholdPercent`（0〜100、デフォルト50） |
| `CompositeExitConditionConfig` | 上記の組み合わせ（AND/OR）。「BGM位置に達した、またはHPが閾値を切った」等の早期遷移に使える。 | `mode`, `conditions` |
| `AllEnemiesDefeatedExitConditionConfig` / `TimeLimitExitConditionConfig` | 中ボスの子機を全滅させる、時間制限で強制進行、等の補助用途。 | — |

`BgmPositionExitConditionConfig.thresholdSamples` は `GetBgmAudioSource`（`Func<AudioSource>`）経由でBGMの `AudioSource` を参照する。これは `BattleScenePresenter.Awake()` で `sequenceModelRepository.SetBgmAudioSourceProvider(() => soundManager.BgmAudioSource)` として実行時に注入される仕組みであり、アセット側は `thresholdSamples` の数値だけを設定すればよい。閾値のサンプル数は `AudioClip.frequency × 秒数` で逆算する（例: 44100Hzの曲でイントロが8秒なら `thresholdSamples ≈ 352800`）。

## 中ボス（サブボス）を作る場合

「中ボス」という専用の仕組みは存在しない。実装上は道中の1フェーズとして、通常の敵Prefabより強力な `EnemyEntityPresenter` 搭載Prefabを `enemySpawnTracks` でスポーンさせるか、あるいはボス相当の演出が必要なら小規模な `BattleSequenceAsset`（situation: Way だが `bossPrefab` を設定し `AllEnemiesDefeated` or `BossHpThreshold` で終了）を道中シーケンスの1フェーズとして組み込む形になる。要件をユーザーに確認し、既存の道中/ボスの仕組みのどちらに近いかを判断すること。

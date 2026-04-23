# バトルステージ実装マニュアル

## バトルシーンのざっくり流れ

### 起動時

```mermaid
flowchart TD

S1["
    BattleScenePresenter.Awake()
    が起動
"]

S2["
    BattleScenePresenter.ResolveStageModel()
    でステージ番号を取得
"]

S3["
    StageModelRepository
    から該当ステージのStageModelを取得
"]

S4["
    PlayerPresenterの取得
"]

S5["
    道中とボスのシーケンス情報を
    StageModelから取得.
    シーケンス情報は
    BattleSequenceModelとして取得
"]

S6["
    RuntimeModelから
    現在が道中かボスかを取得
"]

S7["
    PhaseStateMachineで
    該当のシーケンスを実行開始
"]

BattleScene起動 --> S1 --> S2 --> S3 --> S4 --> S5 --> S6 --> S7
```

### バトルの各シーケンス

#### PlaySequence → MoveNextPhase ループ

```mermaid
flowchart TD
    A([PlaySequence 呼び出し]) --> B{sequence == null?}
    B -- Yes --> C[LogWarning\nreturn]
    B -- No --> D["Stop(sequenceToKeep: sequence)\n現在のシーケンスを安全停止"]
    D --> E["activeSequence = sequence\nsequence.Reset()"]
    E --> F{HasPhases?}
    F -- "No（フェーズなし）" --> G["sequenceCompleted.OnNext\nDisposeSequence\nreturn"]
    F -- Yes --> H[[MoveNextPhase]]

    H --> I["exitSubscription?.Dispose()\nactivePhase?.Exit()"]
    I --> J{activeSequence == null?}
    J -- Yes --> K[return]
    J -- No --> L["nextPhase = activeSequence.MoveNext()"]
    L --> M{nextPhase == null?}
    M -- "Yes（全フェーズ完了）" --> N["playableDirector.Stop()\nsequenceCompleted.OnNext\nDisposeSequence\nactiveSequence / activePhase = null"]
    M -- No --> O["activePhase = nextPhase\nApplyTimeline(activePhase)\nactivePhase.Enter(playableDirector)\nphaseStarted.OnNext(activePhase)"]
    O --> P["OnExitPhase を購読\n→ CompletePhase() で MoveNextPhase へ"]
    P --> Q([フェーズ実行中])
    Q -- "CompletePhase() 呼び出し" --> H
```

#### BattleSequenceModel.MoveNext() — グループ・ループ処理

```mermaid
flowchart TD
    A([MoveNext 開始]) --> B{currentGroup 内に\n次の Phase がある?}
    B -- Yes --> C["phaseFactory でPhase生成\nallCreatedPhases に追加\nreturn phase"]
    B -- No --> D{group.Loop == true?}
    D -- No --> G[次のGroupへ進む]
    D -- Yes --> E{"LoopCount未達?\n(0=無限)"}
    E -- Yes --> F["currentLoopIteration++\ncurrentPhaseInGroup = 0\nPhase生成\nreturn phase"]
    E -- No --> G
    G --> H{groups 終端?}
    H -- Yes --> I["return null\n（シーケンス終了）"]
    H -- No --> B
```

#### ApplyTimeline — Timeline のセットアップ

```mermaid
flowchart TD
    A([ApplyTimeline 呼び出し]) --> B{playableDirector\n割り当て済み?}
    B -- No --> C[LogWarning\nreturn]
    B -- Yes --> D{timelineResolver != null?}
    D -- Yes --> E["timelineResolver.Invoke(phase)"]
    D -- No --> F["phase.ResolveTimeline()"]
    E --> G{TimelineAsset 取得?}
    F --> G
    G -- No --> H[LogWarning\nreturn]
    G -- Yes --> I["playableAsset = timeline\ntime = 0\nEvaluate()\nextrapolationMode = Hold"]
    I --> J{bindingMap\n割り当て済み?}
    J -- Yes --> K["bindingMap.ApplyBindings(director, timeline)"]
    J -- No --> L["playableDirector.Play()"]
    K --> L
```

#### フェーズのライフサイクル

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Entering : Enter(director) 呼び出し
    Entering --> Running : OnEnter() 完了（サブクラス処理）
    Running --> Exiting : CompletePhase() → OnExitPhase.OnNext
    Exiting --> Idle : Exit() → Disposables.Clear()
    Idle --> [*] : Dispose()

    note right of Running
        Timeline が再生中
        サブクラスが条件を満たしたとき
        CompletePhase() を呼ぶ
    end note
```

#### Stop 処理

```mermaid
flowchart TD
    A([Stop 呼び出し]) --> B["exitSubscription?.Dispose()"]
    B --> C["playableDirector?.Stop()"]
    C --> D["activePhase?.Exit()\nactivePhase = null"]
    D --> E{"activeSequence != null\nかつ sequenceToKeep と異なる?"}
    E -- Yes --> F["DisposeSequence(activeSequence)\nactiveSequence = null"]
    E -- No --> G([終了])
    F --> G
```

## 実装手順

### 新規ステージの追加

基盤実装は完成済みのため、以下のデータ作業のみで新しいステージを追加できます。
（ここでは `StageN` を例として記述）

#### ステップ全体の依存関係

```mermaid
flowchart LR
    A["① StageData.asset
にエントリー追加"] --> B
    B["② DataStore/StageN/ フォルダ作成"] --> C
    C["③ BattleTimelineBuilderAsset
各フェーズ分を作成"] --> D
    C --> E
    D["④ StageNWay.asset 作成
（BattleSequenceAsset）"] --> G
    E["⑤ StageNBoss.asset 作成
（BattleSequenceAsset）"] --> G
    F["⑥ シナリオアセット作成
StageNWayScenario.asset
StageNBossScenario.asset"] --> G
    G["⑦ Addressables に全アセットを登録"] --> H
    H["⑧ BgmData.asset に
BGMエントリー追加"]
```

---

#### ① StageData.asset にエントリーを追加

パス: `Assets/Project/DataStore/StageData.asset`
Inspector で `stageData` リストに以下フィールドを持つエントリーを追加する。

| フィールド | 例 | 説明 |
|---|---|---|
| `id` | `"3"` | ステージID（文字列） |
| `stageNumber` | `3` | ステージ番号。`BattleStageType` enum に対応 (1〜6, EX=7) |
| `charaStillAddress` | `"Tatsumi"` | キャラクタースチルのアドレス |
| `title` | `"星雲列車は夜を駆ける"` | ステージタイトル |
| `waySequenceAddress` | `"Assets/Project/DataStore/Stage3/Way/Stage3Way.asset"` | 道中シーケンスアセットのフルパス |
| `bossSequenceAddress` | `"Assets/Project/DataStore/Stage3/Boss/Stage3Boss.asset"` | ボスシーケンスアセットのフルパス |

> `stageNumber + 2` が `SceneType` enum のインデックスに対応するため、enum の順序を変えないこと。

---

#### ② フォルダを作成

```
Assets/Project/DataStore/StageN/
  Way/
  Boss/
  Movement/   ← ボス登場モーション用 Preset を置く場合
```

Stage1 の構成を参考にコピーして中身を差し替えるのが最速。

---

#### ③ BattleTimelineBuilderAsset を作成（フェーズ数分）

メニュー: `Assets > Create > Battle > Timeline Builder`

各フェーズの攻撃パターン・演出を設定するアセット。

| セクション | 内容 |
|---|---|
| Signal Tracks | タイムライン上に発火する汎用シグナル |
| Animation Tracks | キャラや背景のアニメーション |
| Activation Tracks | GameObject の表示/非表示 |
| Audio Tracks | SE・BGM の再生 |
| Control Tracks | 子Timelineの制御 |
| Enemy Spawn Tracks | 敵をスポーンするシグナル（数が `AllEnemiesDefeated` 条件に関係） |
| Boss Attack Preset | ボスの攻撃設定（ボスフェーズのみ） |
| Boss Movement Preset | ボスの移動設定（ボスフェーズのみ） |

---

#### ④ BattleSequenceAsset（道中）を作成

メニュー: `Assets > Create > Battle > Phase Sequence`  
ファイル名に `Way` を含めると `situation` が自動で `Way` にセットされる。

`sequenceGroups` に `SequenceGroup` を追加し、各グループに `BattlePhaseDefinition` を入れる。

**BattlePhaseDefinition の設定項目**

| フィールド | 説明 |
|---|---|
| `phaseId` | フェーズを識別する文字列 |
| `timelineBuilder` | 上記③で作成した `BattleTimelineBuilderAsset` |
| `timelineBuilderStrong` | HP が閾値以下のときに使う強攻撃用ビルダー（任意） |
| `strongAttackHpThresholdPercent` | 強攻撃切り替えHP（%）。デフォルト 50 |
| `exitConditionConfig` | フェーズ終了条件（下表） |

**ExitCondition の選択肢**

| 種別 | 説明 |
|---|---|
| `TimeLimitExitConditionConfig` | 指定秒数経過で次フェーズへ |
| `AllEnemiesDefeatedExitConditionConfig` | スポーンした全敵を倒すと次フェーズへ |
| `BossHpThresholdExitConditionConfig` | ボスHPが閾値を下回ると次フェーズへ |
| `BgmPositionExitConditionConfig` | BGM の再生位置が指定サンプルに達すると次フェーズへ |
| `CompositeExitConditionConfig` | 上記を AND / OR で組み合わせ |

**SequenceGroup のループ設定**

| フィールド | 説明 |
|---|---|
| `loop` | true にするとグループ内フェーズをループ |
| `loopCount` | ループ回数。`0` で無限ループ |

---

#### ⑤ BattleSequenceAsset（ボス）を作成

道中と同じ手順。ファイル名に `Boss` を含めると `situation` が自動で `Boss` になる。

ボスシーケンスには追加設定が必要:

| フィールド | 説明 |
|---|---|
| `bossPrefab` | ボスの GameObject Prefab |
| `bossSpawnPosition` | ボスのスポーン座標 |
| `bossEntranceMovement` | ボス登場時のモーション（`IMovementStep` のリスト） |

---

#### ⑥ シナリオアセットを作成

`ScenarioModelRepository` がファイル名を以下のルールで自動解決するため、**命名規則を厳守**すること。

| ファイル | パス |
|---|---|
| 道中後シナリオ | `Assets/Project/DataStore/StageN/Way/StageNWayScenario.asset` |
| ボス後シナリオ | `Assets/Project/DataStore/StageN/Boss/StageNBossScenario.asset` |

アセットの型は `ScenarioData`。`steps` に台詞・演出コマンドを順番に追加する。

---

#### ⑦ Addressables に全アセットを登録

以下のアセットをすべて Addressables に追加し、**アドレスをアセットのフルパスと一致させる**。
（`BattleSequenceModelRepository` と `ScenarioModelRepository` がフルパスをアドレスとして直接使用するため）

- `StageNWay.asset`
- `StageNBoss.asset`
- `StageNWayScenario.asset`
- `StageNBossScenario.asset`
- 各 `BattleTimelineBuilderAsset`（③で作成したもの）

---

#### ⑧ BgmData.asset に BGM エントリーを追加

パス: `Assets/Project/DataStore/BgmData.asset`

`bgmData` リストに道中・ボスの2エントリーを追加する。

| フィールド | 道中 | ボス |
|---|---|---|
| `sceneType` | `StageN`（※） | `StageN` |
| `bgmType` | `BattleWay` | `BattleBoss` |
| `loopStartSamples` | イントロ終了サンプル | 同左 |
| `loopEndSamples` | 0 でクリップ全体 | 同左 |

> ※ `SceneType.StageN` は `stageNumber + 2` のインデックスに対応。
> Stage1=3, Stage2=4, Stage3=5, Stage4=6, Stage5=7, Stage6=8, StageEx=9

BGM の AudioClip 自体も `SoundAssetRepository` のAddressables経由でロードされるため、対応するクリップアセットもAddressablesへの追加が必要。

---

#### その他確認事項

- `BattleStageType` enum は Stage1〜Stage6 と StageEx (7) まで定義済み。enum 変更不要。
- `SceneType` enum も Stage1〜StageEx まで対応済み。変更不要。
- キャラクタースチル画像（`charaStillAddress`）は `StillAssetRepository` 経由でロードされる。新キャラの場合は Textures フォルダへの追加と Addressables 登録も必要。

---

## Prefabs

すべての Prefab は `Assets/Project/Scenes/Battle/Prefabs/` 以下に格納される。

### フォルダ構成

```
Prefabs/
  Base/           ← 新Prefab作成の元になるテンプレート
    EnemyEntityBase.prefab
    Bullet.prefab
    Boss.prefab
  Player/         ← プレイヤー関連（変更不要）
    Player.prefab
    PlayerNormalBullet.prefab
    PlayerChargeBullet.prefab
  Stage1/         ← Stage1 固有のPrefab
    Loco.prefab           ← Stage1 ボス本体
    MiddleBoss.prefab     ← Stage1 中ボス
    BossIntro.prefab      ← ボスフェーズ演出 (Control Track で制御)
    BossMelodyA.prefab
    BossMelodyB.prefab
    Phase*.prefab         ← 道中各フェーズの敵グループ (EnemyEntityBase をカスタマイズ)
    LinearBullet.prefab   ← ステージ固有の弾
    Wall.prefab           ← 演出オブジェクト (BattleTimelineBindingMap でバインド)
    Spectrum.prefab
    StaffNotation.prefab
  StageN/         ← 新ステージ用にこのフォルダを追加
```

### Base Prefab の役割と構成

| Prefab | 主要コンポーネント | 用途 |
|---|---|---|
| `EnemyEntityBase` | `EnemyEntityPresenter`, `EnemyEntityView`, `SpriteRenderer`, `BulletPool` | 道中・中ボス用敵キャラの基底 |
| `Bullet` | `BulletEntityPresenter` | 敵弾の基底 |
| `Boss` | `BossEntityPresenter`, `EnemyEntityView`, `BulletPool[]` | ボスキャラの基底 |

### Prefab の作成方法

フォルダを選択した状態で右クリックメニューから作成できる：

| メニュー | 作成されるもの |
|---|---|
| `Assets > Create > Battle > Enemy Entity Base` | `EnemyEntityBase.prefab` のコピー |
| `Assets > Create > Battle > Bullet` | `Bullet.prefab` のコピー |
| `Assets > Create > Battle > Boss` | `Boss.prefab` のコピー |

### 各 Prefab の使われ方

#### 敵Prefab (Phase*.prefab など)

```mermaid
flowchart LR
    A["BattleTimelineBuilderAsset\nEnemySpawnTrackDefinition\n└ EnemySpawnDefinition\n  └ prefab 参照"] --"Timeline ビルド時に\nSignalAsset を動的生成"--> B["EnemySpawnSignal\n(SpawnPosition + Prefab)"]
    B --"Timeline 再生中に\nシグナル発火"--> C["EnemySpawnReceiver\n.OnNotify()"]
    C --"Instantiate"--> D["敵 Prefab のインスタンス"]
    D --> E["EnemyTracker に登録\n(AllEnemiesDefeated 条件で使用)"]
```

**設定手順**
1. `Assets > Create > Battle > Enemy Entity Base` でコピーを作成
2. Inspector で HP・当たりダメージ・移動パターン (`MovementPreset` or インライン `IMovementStep`)・攻撃 (`AttackPreset` or `AttackTimeline`) を設定
3. `BattleTimelineBuilderAsset` の `Enemy Spawn Tracks > EnemySpawnDefinition` の `prefab` にアサイン

#### ボスPrefab (Loco.prefab など)

```mermaid
flowchart LR
    A["BattleSequenceAsset\n(Boss)\n└ bossPrefab 参照"] --"BattleScenePresenter\n.SpawnBoss()"--> B["ボス Prefab のインスタンス"]
    B --> C["BossEntityPresenter"]
    C --"OnPhaseStarted() 毎に"--> D["BattleTimelineBuilderAsset\nから AttackPreset / MovementPreset 適用"]
```

**設定手順**
1. `Assets > Create > Battle > Boss` でコピーを作成
2. Inspector で HP・当たりダメージ・`BulletPool[]`（弾の種類数分）を設定
3. `BattleSequenceAsset (Boss)` の `bossPrefab` にアサイン
4. ボスの攻撃・移動は各フェーズの `BattleTimelineBuilderAsset` > `Boss Attack Preset` / `Boss Movement Preset` で制御する

#### 演出Prefab と BattleTimelineBindingMap

`Wall.prefab` や `Spectrum.prefab` などシーン上に常駐する演出オブジェクトは、Timeline の **Control Track** や **Activation Track** で制御される。

Timeline のトラック名と Scene の GameObject の紐付けは `BattleTimelineBindingMap` が担う。

| 設定場所 | 内容 |
|---|---|
| `BattleTimelineBuilderAsset` の `Control Tracks` | トラック名を記載 |
| `BattleTimelineBindingMap` (シーン上の MonoBehaviour) | トラック名 → Scene の GameObject |

> トラック名は完全一致で検索されるため、`BattleTimelineBuilderAsset` の `trackName` と `BattleTimelineBindingMap` の `trackName` を一致させること。

### 新規ステージで必要な Prefab 作業

1. `Prefabs/StageN/` フォルダを作成
2. 道中フェーズ数分の敵 Prefab を `Enemy Entity Base` メニューから作成・カスタマイズ
3. ボス Prefab を `Boss` メニューから作成・カスタマイズ
4. ステージ固有の弾が必要な場合は `Bullet` メニューから作成
5. 演出オブジェクト（Wall 等）が必要な場合はシーンに配置し `BattleTimelineBindingMap` に登録

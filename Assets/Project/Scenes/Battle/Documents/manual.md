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
    A["① StageData.asset\nにエントリー追加"] --> B
    B["② フォルダ作成\nDataStore/StageN/\nPrefabs/StageN/"] --> C
    C["③ Prefab 作成\n・敵Prefab（フェーズ数分）\n・ボスPrefab\n・弾Prefab（必要に応じて）"] --> D
    C --> E
    D["④ BattleTimelineBuilderAsset\n各フェーズ分を作成\n→ EnemySpawn に③をアサイン"] --> F
    D --> E
    F["⑤ StageNWay.asset 作成\n（BattleSequenceAsset）"] --> H
    E["⑥ StageNBoss.asset 作成\n（BattleSequenceAsset）\n→ bossPrefabに③をアサイン"] --> H
    B --> G
    G["⑦ シナリオアセット作成\nStageNWayScenario.asset\nStageNBossScenario.asset"] --> H
    H["⑧ Addressables に全アセットを登録"] --> I
    I["⑨ BgmData.asset に\nBGMエントリー追加"]
```

---

#### ① 基本骨子を作成

```
Assets/Project/DataStore/StageN/
  Attack/     # EnemyのInspectorで設定する攻撃のプリセットが入っている
  Boss/
  Ease/       # DOTweenのカスタムEaseが入っている
  Way/        
  Movement/   # EnemyのInspectorで設定する動きのプリセットが入っている

Assets/Project/Scenes/Battle/Prefabs/StageN/
```

`waySequenceAddress`: `Assets/Project/DataStore/Stage3/Way/Stage3Way.asset`
`bossSequenceAddress`: `Assets/Project/DataStore/Stage3/Boss/Stage3Way.asset`


Stage1 の構成を参考にコピーして中身を差し替えるのが最速。
> Scaffold を作りたい..

---

#### ② StageData.asset にエントリーを追加

パス: `Assets/Project/DataStore/StageData.asset`
Inspector で `stageData` リストに以下フィールドを持つエントリーを追加する。

| フィールド | 例                                                         | 説明 |
|---|-----------------------------------------------------------|---|
| `id` | `"3"`                                                     | ステージID（文字列） |
| `stageNumber` | `3`                                                       | ステージ番号。`BattleStageType` enum に対応 (1〜6, EX=7) |
| `charaStillAddress` | `"Tatsumi"`                                               | キャラクタースチルのアドレス |
| `title` | `"ほげほげほげほげ"`                                              | ステージタイトル |
| `waySequenceAddress` | `"Assets/Project/DataStore/Stage3/Way/Stage3Way.asset"`   | 道中シーケンスアセットのフルパス |
| `bossSequenceAddress` | `"Assets/Project/DataStore/Stage3/Boss/Stage3Boss.asset"` | ボスシーケンスアセットのフルパス |

> `stageNumber + 2` が `SceneType` enum のインデックスに対応するため、enum の順序を変えないこと。

---

#### ③ Prefab を作成

`Assets/Project/Scenes/Battle/Prefabs/StageN/` で右クリックから各エンティティのプレふぁぶを作成する。 

| Base Prefab | 作成メニュー | 主要コンポーネント | 用途 |
|---|---|---|---|
| `EnemyEntityBase` | `Battle > Enemy Entity Base` | `EnemyEntityPresenter`, `EnemyEntityView`, `SpriteRenderer`, `BulletPool` | 道中・中ボス用の敵キャラ |
| `Bullet` | `Battle > Bullet` | `BulletEntityPresenter` | ステージ固有の敵弾 |
| `Boss` | `Battle > Boss` | `BossEntityPresenter`, `EnemyEntityView`, `BulletPool[]` | ボスキャラ |

**敵 Prefab（道中フェーズ数分）**

`Enemy Entity Base` でコピーを作成し、Inspector で以下を設定する。

| フィールド | 説明 |
|---|---|
| `maxHp` | 最大HP |
| `contactDamage` | 体当たりダメージ |
| `movementPreset` / `movementSteps` | 移動パターン（Preset 優先、なければインライン `IMovementStep`） |
| `attackPreset` / `attackTimeline` | 攻撃パターン（Preset 優先） |
| `bulletPool` | 使用する弾 Prefab の Pool |

> 作成した敵 Prefab は ④ の `BattleTimelineBuilderAsset > Enemy Spawn Tracks > EnemySpawnDefinition.prefab` にアサインする。

**ボス Prefab（1体）**

`Boss` でコピーを作成し、Inspector で以下を設定する。

| フィールド | 説明 |
|---|---|
| `maxHp` | 最大HP |
| `contactDamage` | 体当たりダメージ |
| `bulletPools` | 弾の種類数分の BulletPool 配列 |

> 攻撃・移動パターンはボス Prefab に持たせず、各フェーズの `BattleTimelineBuilderAsset` > `Boss Attack Preset` / `Boss Movement Preset` で制御する。

> 作成したボス Prefab は ⑥ の `BattleSequenceAsset (Boss).bossPrefab` にアサインする。

**演出オブジェクト（必要に応じて）**

`Wall` や `Spectrum` などシーン上に常駐させる演出 GameObject は Prefab として `Prefabs/StageN/` に格納しバトルシーンに配置する。
Timeline の Control Track / Activation Track から参照するため、`BattleTimelineBindingMap`（シーン上の MonoBehaviour）に **トラック名 → GameObject** の対応を登録する。

| 設定場所 | 内容 |
|---|---|
| `BattleTimelineBuilderAsset` の `Control Tracks` / `Activation Tracks` の `trackName` | Timeline 側のトラック名 |
| `BattleTimelineBindingMap` の各 `BindingEntry` | トラック名 → シーン上の GameObject |

> トラック名は**完全一致**で検索されるため、両者を必ず一致させること。

------

#### ④ BattleSequenceAsset（道中）を作成

メニュー: `Assets > Create > Battle > Phase Sequence`  
ファイル名に `Way` を含めると `situation` が自動で `Way` にセットされる。

`sequenceGroups` に `SequenceGroup` を追加し、各グループに `BattlePhaseDefinition` を入れる。

**BattlePhaseDefinition の設定項目**

| フィールド | 説明 |
|---|---|
| `phaseId` | フェーズを識別する文字列 |
| `timelineBuilder` | 上記④で作成した `BattleTimelineBuilderAsset` |
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

#### ⑤ BattleTimelineBuilderAsset （道中）を作成（フェーズ数分）

メニュー: `Assets > Create > Battle > Timeline Builder`

![img.png](img.png)

各フェーズの攻撃パターン・演出を設定するアセット。

| セクション | 内容 |
|---|---|
| Signal Tracks | タイムライン上に発火する汎用シグナル |
| Animation Tracks | キャラや背景のアニメーション |
| Activation Tracks | GameObject の表示/非表示 |
| Audio Tracks | SE・BGM の再生 |
| Control Tracks | 子Timelineの制御（演出オブジェクトを `trackName` でバインド） |
| Enemy Spawn Tracks | 敵をスポーンするシグナル。`EnemySpawnDefinition.prefab` に③の敵Prefabをアサイン |
| Boss Attack Preset | ボスの攻撃設定（ボスフェーズのみ） |
| Boss Movement Preset | ボスの移動設定（ボスフェーズのみ） |


#### ⑥ BattleSequenceAsset（ボス）を作成

道中と同じ手順。ファイル名に `Boss` を含めると `situation` が自動で `Boss` になる。

ボスシーケンスには追加設定が必要:

| フィールド | 説明 |
|---|---|
| `bossPrefab` | ボスの GameObject Prefab |
| `bossSpawnPosition` | ボスのスポーン座標 |
| `bossEntranceMovement` | ボス登場時のモーション（`IMovementStep` のリスト） |

---

### ⑦ ⑤のボス版を作成

---

#### ⑧ シナリオアセットを作成

※ 基本的に `Tools/Import Scenario` を使用して `.txt` を取り込めば良い。

`ScenarioModelRepository` がファイル名を以下のルールで自動解決するため、**命名規則を厳守**すること。

| ファイル | パス |
|---|---|
| 道中後シナリオ | `Assets/Project/DataStore/StageN/Way/StageNWayScenario.asset` |
| ボス後シナリオ | `Assets/Project/DataStore/StageN/Boss/StageNBossScenario.asset` |

アセットの型は `ScenarioData`。`steps` に台詞・演出コマンドを順番に追加する。

---

#### ⑨ Addressables に全アセットを登録

以下のアセットをすべて Addressables に追加し、**アドレスをアセットのフルパスと一致させる**。
（`BattleSequenceModelRepository` と `ScenarioModelRepository` がフルパスをアドレスとして直接使用するため）

- `StageNWay.asset`
- `StageNBoss.asset`
- `StageNWayScenario.asset`
- `StageNBossScenario.asset`
- 各 `BattleTimelineBuilderAsset`（④で作成したもの）

---

#### ⑩ BgmData.asset に BGM エントリーを追加

※ これは全てあるはずなので、ゲームを起動して動いていればスキップしてok
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

# Directory Structure

このドキュメントは、Magic-science-world のディレクトリと主要ファイルの配置を説明する。
各項目の末尾に、そのディレクトリまたはファイルの役割を一言で記載する。

## Root

```text
Magic_science_world/
├── .github/                # GitHub Actions など GitHub 関連設定。
├── .idea/                  # Rider/JetBrains IDE のローカル設定。
├── .serena/                # Serena など開発補助ツールの設定。
├── Assets/                 # Unity アセットとゲーム実装本体。
├── Document/               # プロジェクト説明・設計資料。
├── Library/                # Unity が生成するローカルキャッシュ。
├── Logs/                   # Unity 実行・インポートログ。
├── obj/                    # IDE/ビルド補助の中間生成物。
├── Packages/               # Unity Package Manager の依存定義。
├── ProjectSettings/        # Unity プロジェクト設定。
├── Temp/                   # Unity 実行中の一時生成物。
├── UserSettings/           # ローカルユーザー設定。
├── .editorconfig           # エディタ共通の整形ルール。
├── .gitignore              # Git 管理から除外するファイル定義。
├── .vsconfig               # Visual Studio 構成。
├── README.md               # プロジェクト概要。
├── WARP.md                 # 開発補助ツール向けメモ。
└── Magic_science_world.sln  # Rider/IDE 用ソリューション。
```

## Document

```text
Document/
├── Architecture.eddx       # アーキテクチャ図の元データ。
├── Architecture.md         # MVP など基本設計方針の説明。
├── Directory-Structure.md  # ディレクトリ構造の説明。
└── Document.md             # AI/開発者向けの全体構造・責務リファレンス。
```

## Packages

```text
Packages/
├── manifest.json           # 直接依存する Unity パッケージ一覧。
└── packages-lock.json      # 解決済み依存バージョンのロックファイル。
```

主要依存:

- `com.unity.addressables`: Addressables によるアセット管理。
- `com.unity.inputsystem`: キーボード/ゲームパッド入力管理。
- `com.unity.render-pipelines.universal`: URP/2D 描画基盤。
- `com.unity.timeline`: manifest に残っているが、旧バトル方式（Timeline 駆動）の削除後はどのコードからも使われていない。バトル進行は自前の BehaviorTree で行う。
- `com.unity.ugui`: UI 表示基盤。
- `com.mackysoft.serializereference-extensions`: SerializeReference の Inspector 拡張。

## Assets

```text
Assets/
├── AddressableAssetsData/  # Addressables のグループ・ビルド設定。
├── Editor/                 # Project 外のエディタ専用補助データ。
├── Fonts/                  # フォントアセット。
├── Plugins/                # UniRx, UniTask, DOTween など外部ライブラリ。
├── Project/                # ゲーム固有のコード・シーン・データ・画像。
├── Resources/              # DOTweenSettings など Resources ロード対象。
├── Scripts/ZString/        # ZString の同梱ソース。
├── Settings/               # URP/Renderer/Build Profile など Unity 設定。
└── TextMesh Pro/           # TMP 設定・フォント・スプライト。
```

## Assets/Project

```text
Assets/Project/
├── Commons/                # 複数シーンで使う UI/デバッグ/プレイヤー共通部品。
├── DataStore/              # ScriptableObject データベースとデモ用 UserData。
├── DeprecatedResources/    # 旧 Resources 運用時代の退避アセット。
├── Editor/                 # プロジェクト固有のエディタ拡張。
├── Samples/                # MVP サンプルコード。
├── Scenes/                 # Unity シーンとシーン固有スクリプト。
├── Scripts/                # シーン横断の共通コード。
└── Textures/               # Addressables で参照する画像・音声素材。
```

## Commons

```text
Assets/Project/Commons/
├── Debugger/               # 開発用デバッグ補助。
│   ├── Prefabs/            # デバッグ用プレハブ。
│   └── Scripts/            # デバッグ用スクリプト。
│       └── Presenter/
│           └── DebugRuntimeModelInitializer.cs  # 実行時状態を開発用に初期化する。
├── Player/                 # 共通プレイヤー素材の置き場。
│   ├── Prefabs/            # プレイヤー関連プレハブ。
│   └── Scripts/            # プレイヤー共通スクリプト用の予約領域。
└── UI/                     # 共通 UI 部品とモーダル。
    ├── Prefabs/            # 共通 UI プレハブ。
    └── Scripts/
        ├── Presenter/      # 共通モーダルの制御。
        └── View/           # 共通ボタン・リスト・モーダル表示。
```

共通 UI の主要ファイル:

- `ButtonBase.cs`: ボタン UI の基底処理。
- `SimpleButton.cs`: 単体ボタンの入力イベントを扱う。
- `MovableButton.cs`: 選択状態を持つ移動可能なボタン。
- `ButtonListBase.cs`: 複数ボタンの選択・決定処理の基底。
- `ScrollableButtonList.cs`: スクロール可能なボタンリスト。
- `PointerAddon.cs`: ポインタ/選択装飾を補助する。
- `OptionModalPresenter.cs` / `OptionModalView.cs`: 音量などオプション画面を扱う。
- `PauseModalPresenter.cs` / `PauseModalView.cs`: バトル中のポーズ画面を扱う。
- `GameOverModalPresenter.cs` / `GameOverModalView.cs`: ゲームオーバー画面を扱う。
- `TutorialModalPresenter.cs` / `TutorialModalView.cs`: 初回チュートリアル表示を扱う。

## DataStore

```text
Assets/Project/DataStore/
├── BgmData.asset           # BGM 定義データベース。
├── CharacterData.asset     # キャラクター定義データベース。
├── SeData.asset            # SE 定義データベース。
├── StageData.asset         # ステージ定義データベース。
├── UserData.json           # Editor 実行時のユーザーセーブデータ。
├── Commons/BT/Enemy.asset  # 全ステージ共通の雑魚ツリー。
├── Stage1/ 〜 Stage4/      # 各ステージのバトル (BT)・シナリオデータ。
├── MVCapture/              # MV キャプチャ用のシナリオ。
└── Test/                   # バトルデータ検証用アセット。
```

`Stage{N}` の内訳:

```text
Stage{N}/
├── BT/                     # BehaviorTree 形式のバトルデータ。
│   ├── Stage{N}_BT.asset   # BattleStageAsset (StageData.stageAddress から参照)。
│   ├── Stage{N}_Boss.asset # BossDefinition。
│   ├── Stage/              # ステージツリー Stage{N}_Tree とボスのツリー Stage{N}Boss_Tree。
│   ├── Attack/             # 攻撃パターンのツリー (雑魚の attack パラメータ、ボスの SubTree から参照)。
│   ├── Motion/             # 移動ステップ列の MotionPreset。
│   ├── Enemy/              # 共通ツリーに収まらない敵の専用ツリー (Stage2 のみ中身あり)。
│   └── Trail/              # TrailPath (Stage2 のみ)。
├── Scenario/               # ボス前の会話 Stage{N}WayScenario.asset と撃破後の会話 Stage{N}BossScenario.asset。
└── Boss/                   # Stage1 のみ。MusicTerminologySpectrum.asset。
```

データ構造の詳細は `Document/Battle/DataStore.md` を参照。

## Project Editor

```text
Assets/Project/Editor/
├── CommonScaffold.cs                 # MVP 構成の雛形生成を補助する。
├── CreateDB.cs                       # データベース ScriptableObject 作成を補助する。
├── GlobalSceneAutoLoader.cs          # Play 時に Global シーンを自動ロードする。
├── ScenarioImporter.cs               # シナリオ JSON を Unity データへ取り込む。
├── SerializeReferenceAutoExpandEditor.cs # SerializeReference の Inspector 表示を補助する。
└── SerializeReferenceDeduplicator.cs # SerializeReference の重複参照を整理する。
```

## Scenes

```text
Assets/Project/Scenes/
├── Entry.unity             # 起動専用シーン。
├── Global.unity            # 常駐管理シーン。
├── Title.unity             # タイトル画面。
├── StageList.unity         # ステージ選択画面。
├── Battle.unity            # 道中・ボス共通のバトル画面。
├── Scenario.unity          # ノベル/会話シーン。
├── DemoClear.unity         # デモ版クリア画面。
├── Entry/                  # Entry シーン固有スクリプト。
├── Global/                 # Global シーン固有スクリプト。
├── Title/                  # Title シーン固有スクリプト。
├── StageList/              # StageList シーン固有スクリプト。
├── Battle/                 # Battle シーン固有スクリプト・プレハブ・Editor 拡張。
├── Scenario/               # Scenario シーン固有スクリプト。
└── DemoClear/              # DemoClear シーン固有スクリプト。
```

### Entry

```text
Entry/Scripts/Presenter/
└── EntryScenePresenter.cs  # Addressables 初期化後に Global と Title を加算ロードする。
```

### Global

```text
Global/Scripts/Presenter/
├── GlobalScenePresenter.cs   # 常駐 UI、入力、サウンド、画面遷移を束ねる。
├── SceneNavigator.cs         # 加算ロードと旧シーンアンロードによる画面遷移を行う。
└── SoundManagerPresenter.cs  # BGM/SE 再生、音量反映、手動ループ再生を行う。
```

### Title

```text
Title/Scripts/
├── Model/
│   └── TitleModel.cs                  # タイトル画面用背景データを持つ。
├── Presenter/
│   └── TitleScenePresenter.cs         # タイトルメニュー操作と StageList 遷移を制御する。
├── Repository/ModelRepository/
│   └── TitleModelRepository.cs        # TitleModel を生成・保持する。
└── View/
    └── TitleMenuView.cs               # タイトルメニューの表示と入力イベントを提供する。
```

### StageList

```text
StageList/
├── Prefabs/                           # ステージカードなどの UI プレハブ。
└── Scripts/
    ├── Model/
    │   └── StageListModel.cs          # ステージ選択画面用背景データを持つ。
    ├── Presenter/
    │   └── StageListScenePresenter.cs # ステージ一覧表示、選択、Battle 遷移を制御する。
    ├── Repository/ModelRepository/
    │   └── StageListModelRepository.cs # StageListModel を生成・保持する。
    └── View/
        ├── StageCardListView.cs       # ステージカード一覧の選択と決定イベントを扱う。
        ├── StageCardView.cs           # 個別ステージカードの表示を扱う。
        └── StageListSceneView.cs      # ステージ選択画面背景を扱う。
```

### Scenario

```text
Scenario/Scripts/
├── Model/
│   ├── ScenarioData.cs                # シナリオステップのデータ形式を定義する。
│   └── ScenarioModel.cs               # シナリオ進行状態とキャラ画像を保持する。
├── Presenter/
│   └── ScenarioScenePresenter.cs      # シナリオコマンドを解釈し View/Battle/RuntimeModel に反映する。
├── Repository/ModelRepository/
│   └── ScenarioModelRepository.cs     # RuntimeModel から読むべきシナリオを決定して生成する。
└── View/
    └── ScenarioView.cs                # メッセージ、立ち絵、表情、ログ表示を扱う。
```

### DemoClear

```text
DemoClear/Scripts/
├── Presenter/
│   └── DemoClearScenePresenter.cs     # 任意キー入力後に Title へ戻す。
└── View/
    └── DemoClearView.cs               # デモクリア表示と入力イベントを扱う。
```

### Battle

```text
Battle/
├── Documents/                         # Battle 固有の設計メモ置き場 (旧方式時代のもの)。
├── Editor/                            # Battle 用のエディタ拡張 (SpectrumBarHeightRecorder)。
│   └── BehaviorTree/                  # BehaviorTree エディタ (GraphView)。
├── Prefabs/                           # Player/敵/ステージ用バトルプレハブ。
└── Scripts/
    ├── BehaviorTree/                  # BT のコアとノード定義。
    │   ├── Core/                      # Node / NodeDefinition / BehaviorTreeAsset / BattleStageAsset / パラメータ / Runner。
    │   ├── Composite/                 # Sequence / Selector / Parallel / Random。
    │   ├── Decorator/                 # Loop / Repeat / Until / Delay / Inverter / ForceSuccess。
    │   ├── Condition/                 # 条件定義と CheckCondition / WaitUntil。
    │   ├── Action/                    # Wait / Log / SubTree。
    │   └── Battle/                    # 発射・ビーム・移動・出現・ステージ演出のバトル固有ノード。
    ├── Runtime/                       # BT の実行基盤 (BattleFlow / BattleWorld / BattleClock / BattleActor / ActorSpawner / ActorPool / BulletSystem)。
    │   ├── Stage/                     # ステージサービス (StageContext / WindService) と外力の受け口。
    │   └── Motion/                    # 敵・ボス・弾共通の移動定義 MotionStep と実行器。
    ├── Model/                         # Battle の純粋データ・Entity Model・風の設定型。
    ├── Presenter/                     # Battle 全体の進行とエンティティ制御。
    ├── Repository/                    # asmref だけが残る (スクリプトなし)。
    └── View/                          # Battle 表示と当たり判定補助。
```

`Scripts/BehaviorTree/` と `Scripts/Runtime/` は `Project.Scenes.Battle.Presenter.asmref` で Presenter アセンブリに含まれる。仕組みは `Document/Battle/BehaviorTree.md`、ノードの一覧は `Document/Battle/BehaviorTree-Nodes.md` を参照。

Battle BehaviorTree:

- `Core/BattleStageAsset.cs`: 1 ステージ (道中・会話・ボス戦) 全体の定義 (ステージサービス、ステージツリー)。
- `Core/BehaviorTreeAsset.cs`: ツリー 1 本の ScriptableObject (root、未接続ノード、パラメータ)。
- `Core/NodeDefinition.cs` / `Core/Node.cs`: 保存用の不変定義と、Enter/Tick/Abort/Exit を保証する実行時ノードの基底。
- `Core/BuildScope.cs` / `Core/TreeParameter.cs`: パラメータ解決 (`BtInt` などの `$param`) と SubTree の循環検出。
- `Core/BattleContext.cs` / `Core/BehaviorTreeRunner.cs`: ツリー 1 本の実行文脈と駆動。
- `Battle/EmitterSources.cs` / `Battle/FireNodes.cs` / `Battle/ShotNodes.cs` / `Battle/BeamNodes.cs`: 方向・回転・弾設定・撃ち方（ShotPattern）と、撃ち方ごとの弾を撃つノード（SingleShot / NWayShot / SequentialFanShot / SequentialCircleShot / BurstShot）、溜めの演出 ChargeEffect、WarningLine / BulletBeam / SpriteBeam / MoveTrailFan。
- `Battle/ActorNodes.cs`: Move / SetMovement / SetDeathBehavior / SetInvincible / Despawn / PlaySe / Spawn。
- `Battle/TrailNodes.cs` / `Battle/TrailPath.cs`: MovePath / PlaceAlongPath と経路アセット。
- `Battle/FlowNodes.cs` / `Battle/BossDefinition.cs`: Stage / Section / Wave / PlayBgm / SpawnBoss / BossBattle / Scenario / OnCommand と、ボスの定義。
- `Battle/StageNodes.cs`: HP・敵全滅・BGM 位置の条件、EnterStrongMode / ClearBullets / WithWind / BossWindCue / PhaseMarker / WithPrefab。

Battle Runtime:

- `BattleFlow.cs`: バトル 1 回分の BT 実行 (ステージサービス、弾・敵の事前生成、ステージツリー・ボスツリーの開始、停止) を管理する。
- `BattleWorld.cs`: フレーム番号・乱数・予約実行・サービスを持ち、1 フレームを進める。
- `BattleClock.cs`: 固定 60fps で BattleWorld を進める MonoBehaviour。
- `BattleActor.cs`: 敵/ボスに付けてツリーと移動を毎フレーム進める。
- `ActorSpawner.cs`: 敵・ビーム・障害物 Prefab を生成 (プール対象は ActorPool から取得) し、必要ならツリーを付ける。
- `ActorPool.cs` / `PooledActor.cs` / `NoActorPool.cs`: 敵を Prefab ごとに使い回すプール。`IPoolableActor` と、敵を消す共通の入口 `ActorDespawn` も `ActorPool.cs` にある。
- `StagePrefabCollector.cs`: BattleStageAsset から参照をたどり、事前生成する弾・出現物の Prefab を集める。
- `BulletSystem.cs`: 敵弾・自機弾を MotionStep に従って固定フレームで動かす。
- `Stage/StageContext.cs` / `Stage/WindService.cs` / `Stage/ExternalForces.cs`: ステージサービスの仕組み、風、自機・弾への外力の受け口。
- `Motion/MotionCore.cs` / `Motion/MotionSteps.Basic.cs` / `Motion/MotionSteps.Advanced.cs` / `Motion/MotionAssets.cs`: MotionStep の定義・実行、MotionPreset / EaseCurveAsset / BattleTargets (自機の Transform)。

Battle BehaviorTree Editor:

- `BehaviorTreeEditorWindow.cs`: `Window > Battle > Behavior Tree Editor`。BT アセットのダブルクリックでも開く。
- `BehaviorTreeGraphView.cs` / `BehaviorTreeNodeView.cs` / `BehaviorTreePort.cs` / `BehaviorTreeEdge.cs`: グラフ表示と編集。
- `NodePaletteView.cs` / `NodeCatalog.cs` / `NodeSearchWindowProvider.cs`: ノード一覧・検索・ドラッグ作成。
- `ParameterPanelView.cs` / `BtValueDrawer.cs` / `ParameterUtility.cs`: パラメータ一覧と値欄の結びつけ。パラメータ名の変更は値欄・`bindToParent`・`*Param` 欄に追従する。

Battle Model:

- `BattleWind.cs`: 風の設定型 (`WindDirection` / `WindInterval` / `BattleWindSettings`)。
- `BossWindCueSettings.cs`: BossWindCue ノードの設定。
- `BattleBackgroundModel.cs`: 背景の状態。
- `ScreenBoundsCache.cs`: Battle シーンの画面境界をキャッシュする。
- `IEnemyTracker.cs`: 敵数監視の抽象インターフェース。
- `Entity/`: Player/Enemy/Boss/Bullet/SpectrumBar などの純粋 EntityModel。

Battle Presenter:

- `BattleScenePresenter.cs`: バトル全体の初期化、BattleStageAsset のロード、BattleFlow による Way/Boss 進行、Scenario 遷移、Retry/Title 戻りを制御する。
- `WindArrowsPresenter.cs`: 風向き矢印を表示する。WindService が生成する。
- `BackgroundPresenter.cs`: バトル背景の初期化・スクロール・減速を制御する。
- `BattleWallPresenter.cs`: バトル画面の壁/境界関連を制御する。
- `BulletClearReceiver.cs`: 弾・警告線・ビームや敵をまとめて消去する (ClearBullets ノード、Retry)。
- `EnemyTracker.cs`: 生存中の敵数を `EnemyEntityPresenter.OnRemoved` で数え、NoActiveEnemies 条件へ提供する。

Battle Entity Presenter:

- `PlayerEntityPresenter.cs`: プレイヤー移動、攻撃、チャージ、被弾、死亡演出を制御する。
- `EnemyEntityPresenter.cs`: 通常敵の HP・被弾点滅・効かない攻撃の SE・寿命・画面外・接触を扱う (移動と攻撃は BattleActor のツリー)。ActorPool での出し入れに対応する。
- `BossEntityPresenter.cs`: ボスの HP・被弾・撃破演出・登場直後の当たり判定オフを扱う。
- `BulletEntityPresenter.cs`: 弾の見た目・寿命・衝突・画面外・プール返却を扱う (位置は BulletSystem が与える)。
- `BulletPool.cs`: 弾 Prefab ごとの共有 ObjectPool (`GetOrCreateSharedPool` / `Prewarm`) と、撃ち手の弾 Prefab の保持。
- `PhysicsBallPresenter.cs`: 物理挙動を持つ Battle エンティティを制御する。
- `AudioSpectrumPresenter.cs`: 音声スペクトラム表現を制御する。
- `BossPhaseProgressGauge.cs`: ボスフェーズ進行ゲージを表示する。
- `DeathDirectorBase.cs`: 死亡演出の共通基底。
- `PlayerDeathDirector.cs`: プレイヤー死亡演出を実行する。
- `BossDeathDirector.cs`: ボス死亡演出を実行する。
- `SpectrumBarCollisionRelay.cs`: スペクトラムバー衝突をエンティティへ中継する。
- `IEntityPresenter.cs`: MonoBehaviour Presenter から EntityModel を取得する共通契約。
- `IDeathDirector.cs`: 死亡演出の共通契約。

Battle Repository/View:

- `BackgroundView.cs`: 背景表示とスクロール見た目を扱う。
- `FanCollider2D.cs`: 扇形当たり判定を提供する。
- `StaffNotationPresenter.cs`: 五線譜風表示を制御する。
- `EntityViewBase.cs`: Battle Entity View の共通基底。
- `PlayerEntityView.cs`: プレイヤーの見た目・アニメーション・HP 表示を扱う。
- `EnemyEntityView.cs`: 敵の見た目・位置・被弾フラッシュを扱う。
- `BossEntityView.cs`: ボスの見た目・HP ゲージ・被弾表示を扱う。
- `BulletEntityView.cs`: 弾の見た目と表示状態を扱う。
- `SpectrumBarView.cs`: スペクトラムバーの見た目を扱う。

## Scripts

```text
Assets/Project/Scripts/
├── Extensions/             # enum、入力イベント、共通拡張、定数。
├── Infra/                  # ScriptableObject/JSON のデータ定義。
├── Model/                  # シーン横断の純粋 Model。
├── Presenter/              # シーン横断 Presenter 基底と起動補助。
├── Repository/             # シーン横断の Model/Asset Repository。
└── View/                   # シーン横断 View アセンブリ定義。
```

Extensions:

- `SceneType.cs`: Scene enum と実シーン名への変換を定義する。
- `BattleStageType.cs`: ステージ enum と番号変換を定義する。
- `BgmType.cs`: BGM 種別 enum。
- `SeType.cs`: SE 種別 enum。
- `GamePath.cs`: Addressables で使う基準パス定数。
- `InputManager.cs`: Input System の Action を UniRx MessageBroker へ変換する。
- `InputSystemActions.cs`: Input System 生成コード。
- `Message/InputActionMessage.cs`: 入力イベントメッセージ群。
- `Message/SceneNavigationMessage.cs`: 画面遷移状態メッセージ。
- `ObservableExtensions.cs`: UniRx 購読補助。
- `BoolExtensions.cs`, `StringExtensions.cs`: 小さな型拡張。
- `ReadOnlyAttribute.cs`: Inspector 読み取り専用表示用属性。

Infra:

- `StageDataObject.cs`: ステージ番号、タイトル、キャラ、背景、BattleStageAsset のアドレスを定義する。
- `CharacterDataObject.cs`: キャラクター ID/名前/英名を定義する。
- `BgmDataObject.cs`: BGM 種別、対象シーン、ループ位置を定義する。
- `SeDataObject.cs`: SE 種別とループ位置を定義する。
- `KeyConfigData.cs`: キー設定保存用データを定義する。
- `UserData.cs`: セーブデータ、音量、初回チュートリアル状態を定義する。

Model:

- `ModelBase.cs`: UserModel/RuntimeModel 参照を持つ Model 基底。
- `AddressablesModel.cs`: Addressables 初期化処理を表す。
- `RuntimeModel.cs`: 現在ステージと Way/Boss 状態を保持する。
- `SceneRouterModel.cs`: シーン名定数を定義する。
- `StageModel.cs`: ステージ開放/クリア状態と BattleStageAsset のアドレスを持つ。
- `UserModel.cs`: UserData の読み書きと進行状態/音量を管理する。
- `CharacterModel.cs`: キャラクターデータを表す。
- `BgmModel.cs`: BGM データと AudioClip を表す。
- `SeModel.cs`: SE データと AudioClip を表す。
- `KeyConfigModel.cs`: InputActionAsset とキー設定を扱う。

Presenter:

- `MonoPresenter.cs`: GlobalScenePresenter、SoundManager、Repository への共通入口を提供する。
- `GameBootStrapper.cs`: ゲーム起動時の補助処理を置く。

Repository:

```text
Repository/
├── AssetRepository/        # Sprite/Audio/Addressables 初期化などアセットロード。
└── ModelRepository/        # Model の生成・キャッシュ・データロード。
```

AssetRepository:

- `AssetRepositoryBase.cs`: アセット Repository の基底。
- `AddressablesInitializeAssetRepository.cs`: Addressables 初期化を行う。
- `StillAssetRepository.cs`: キャラクタースチル Sprite を読み込む。
- `FaceAssetRepository.cs`: キャラクター表情 Sprite 群を読み込む。
- `AllCharacterStillAssetRepository.cs`: 全キャラクターのスチルを読み込む。
- `BackGroundAssetRepository.cs`: 背景 Sprite を読み込む。
- `SoundAssetRepository.cs`: BGM/SE AudioClip を読み込む。

ModelRepository:

- `ModelRepositoryBase.cs`: DataStore Addressables アドレス生成と共通ロード処理。
- `RuntimeModelRepository.cs`: RuntimeModel のシングルトンを提供する。
- `UserModelRepository.cs`: UserModel のシングルトンを提供する。
- `StageModelRepository.cs`: StageData から StageModel 一覧を生成する。
- `SoundModelRepository.cs`: BgmData/SeData から BgmModel/SeModel を生成する。
- `CharacterModelRepository.cs`: CharacterData から CharacterModel を生成する。
- `KeyConfigModelRepository.cs`: InputActionAsset を受け取り KeyConfigModel を生成する。

## Textures

```text
Assets/Project/Textures/
├── Character/              # キャラクター立ち絵・表情・ドット画像。
├── Enemies/                # 敵キャラクター画像。
├── Images/                 # 背景、ボタン、装飾、ロゴ、テキストボックス。
├── Player/                 # プレイヤー画像。
├── Scenario/               # シナリオ用画像。
└── Sounds/                 # BGM/SE 音声ファイル。
```

キャラクター画像の基本形:

```text
Character/{Name}/
├── Dot/                    # バトルや UI 用ドット絵。
├── Face/                   # シナリオ表示用表情差分。
└── Still/                  # ステージカードやシナリオ用スチル。
```

## Assembly Definitions

```text
Assets/Project/Scripts/
├── Extensions/Extensions.asmdef              # 共通 enum/拡張/メッセージ。
├── Infra/Infra.asmdef                        # データ定義。
├── Model/Model.asmdef                        # 共通 Model。
├── Presenter/Presenter.asmdef                # 共通 Presenter。
├── Repository/Repository.asmdef              # Repository ルート。
├── Repository/AssetRepository/AssetRepository.asmdef # アセットロード。
├── Repository/ModelRepository/ModelRepository.asmdef # Model 生成。
└── View/View.asmdef                          # 共通 View。
```

## Runtime Scene Flow

```text
Entry
  -> Addressables initialize
  -> load Global additively
  -> load Title additively
  -> unload Entry

Title
  -> StageList

StageList
  -> set RuntimeModel.CurrentStageType
  -> set RuntimeModel.CurrentSituation = Way
  -> Battle

Battle
  -> load BattleStageAsset and run the stage tree (BattleFlow)
  -> Section "Way": waves
  -> Section "Boss": load Scenario additively, spawn boss, boss battle, load Scenario additively
  -> stage tree completes and the stage is cleared
  -> DemoClear in current demo flow
```

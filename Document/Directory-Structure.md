# Directory Structure

このドキュメントは、Magic-science-world のディレクトリ構成と、各フォルダに何を置くかを説明する。
個々のファイルは列挙せず、フォルダ単位の役割と置き場所のルールを記載する。

## Root

```text
The-MagiXience/
├── .claude/                # Claude Code のスキル・コマンド・設定。
├── .github/                # GitHub Actions のワークフローと Copilot 向け指示。
├── .serena/                # Serena MCP のプロジェクト設定。
├── Assets/                 # Unity アセットとゲーム実装本体。
├── Document/               # プロジェクト説明・設計資料。
├── Packages/               # Unity Package Manager の依存定義。
├── ProjectSettings/        # Unity プロジェクト設定。
├── .editorconfig           # エディタ共通の整形ルール。
├── .gitignore              # Git 管理から除外するファイル定義。
├── .vsconfig               # Visual Studio 構成。
├── AGENTS.md / CLAUDE.md   # AI エージェント向けのプロジェクト指示。
├── README.md               # プロジェクト概要。
└── WARP.md                 # Warp 向けのプロジェクトメモ。
```

`Library/` `Temp/` `Logs/` `UserSettings/` `obj/` や `*.sln` / `*.csproj` は Unity・IDE がローカルに生成するもので、Git 管理外。

## Document

```text
Document/
├── Battle/                 # Battle シーン (BehaviorTree・ノード・DataStore・Tips) の設計資料。
├── Architecture.md         # MVP など基本設計方針の説明 (図の元データは Architecture.eddx)。
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
- `com.unity.render-pipelines.universal` / `com.unity.feature.2d`: URP/2D 描画基盤。
- `com.unity.ugui`: UI 表示基盤。
- `com.mackysoft.serializereference-extensions`: SerializeReference の Inspector 拡張。
- `com.coplaydev.unity-mcp`: Unity MCP (Editor を MCP から操作する)。
- `com.unity.timeline`: manifest にあるが、コードからは参照されていない。

UniRx / UniTask / DOTween は `Assets/Plugins/`、ZString は `Assets/Scripts/ZString/` に同梱している。

## Assets

```text
Assets/
├── AddressableAssetsData/  # Addressables のグループ・ビルド設定。
├── Editor/                 # Project 外のエディタ専用補助データ (会話 JSON)。
├── Fonts/                  # UI 用ドットフォントと TMP フォントアセット。
├── Plugins/                # UniRx, UniTask, DOTween など外部ライブラリ。
├── Project/                # ゲーム固有のコード・シーン・データ・画像。
├── Resources/              # DOTweenSettings など Resources ロード対象。
├── Screenshots/            # 開発中のスクリーンショット。
├── Scripts/ZString/        # ZString の同梱ソース。
├── Settings/               # URP/Renderer/Build Profile/シーンテンプレートなど Unity 設定。
└── TextMesh Pro/           # TMP 設定・フォント・スプライト・シェーダー。
```

## Assets/Project

```text
Assets/Project/
├── Commons/                # 複数シーンで使う UI/デバッグ/プレイヤー共通部品。
├── DataStore/              # ScriptableObject データ (ステージ・BGM・SE・キャラ・BT)。
├── DeprecatedResources/    # 使われなくなった画像・会話データの退避先。Addressables に登録しない。
├── Editor/                 # プロジェクト固有のエディタ拡張とシナリオ原稿。
├── Samples/                # MVP 構成のサンプル (asmref のみ)。
├── Scenes/                 # Unity シーンとシーン固有スクリプト。
├── Scripts/                # シーン横断の共通コード (グローバル asmdef)。
└── Textures/               # Addressables で参照する画像・フォント・音声素材。
```

## Commons

```text
Assets/Project/Commons/
├── Debugger/               # 開発用デバッグ補助。
│   ├── Prefabs/            # デバッグ用プレハブ (DebuggerObject)。
│   └── Scripts/Presenter/  # 実行時状態のデバッグ初期化、MV 撮影用シーン (MVCapture.unity) の進行役。
├── Player/                 # 共通プレイヤー素材。
│   ├── Prefabs/            # プレイヤー関連プレハブ。
│   └── Scripts/            # プレイヤー共通スクリプト用 (asmref のみ)。
└── UI/                     # 共通 UI 部品とモーダル。
    ├── Prefabs/            # 共通ボタンなどの UI プレハブ。
    └── Scripts/
        ├── Presenter/      # Global シーンに常駐するモーダルの制御 (Option / Pause / GameOver / Tutorial)。
        └── View/           # ボタン・ボタンリストの基底、モーダル表示、音量ゲージ、Global カメラを他シーンの Canvas へ割り当てる補助。
```

- モーダルは `<名前>ModalPresenter.cs` (Presenter/) と `<名前>ModalView.cs` (View/) の組で置く。
- 共通 UI のボタンは `ButtonBase` / `ButtonListBase` を基底にする。

## DataStore

```text
Assets/Project/DataStore/
├── BgmData.asset / SeData.asset / CharacterData.asset / StageData.asset  # 種類ごとの定義データベース。
├── Commons/BT/             # 全ステージ共通の BehaviorTree (共通の雑魚ツリー) と BulletPreset (Bullet/)。
├── Stage1/ 〜 Stage4/      # 各ステージのバトル (BT)・シナリオデータ。
├── MVCapture/              # MV 撮影用シーンで流すシナリオ。
└── Test/                   # BT の動作確認・エディタ確認用アセット。
```

Editor で Play すると、セーブデータ `UserData.json` がこのフォルダに作られる (Git 管理外)。

`Stage{N}` の内訳:

```text
Stage{N}/
├── BT/                     # BehaviorTree 形式のバトルデータ。
│   ├── Stage{N}_BT.asset   # BattleStageAsset (StageData.stageAddress から参照)。
│   ├── Stage{N}_Boss.asset # BossDefinition。
│   ├── Stage/              # ステージツリーとボスのツリー。
│   ├── Attack/             # 攻撃パターンのツリー (雑魚の attack パラメータ、ボスの SubTree から参照)。
│   ├── Motion/             # 移動ステップ列の MotionPreset。
│   ├── Bullet/             # 弾の設定をまとめた BulletPreset。
│   ├── Enemy/              # 共通ツリーに収まらない敵の専用ツリー (必要なステージのみ)。
│   └── Trail/              # TrailPath (必要なステージのみ)。
├── Scenario/               # ボス前の会話 Stage{N}WayScenario.asset と撃破後の会話 Stage{N}BossScenario.asset。
└── Boss/                   # ボス固有の補助データ (必要なステージのみ)。
```

データ構造の詳細は `Document/Battle/DataStore.md` を参照。

## Project Editor

```text
Assets/Project/Editor/
├── *.cs                    # プロジェクト固有のエディタ拡張 (Tools メニュー、AssetPostprocessor、InitializeOnLoad)。
└── Scenario/               # シナリオ原稿の .txt。`Tools > Import Scenario` で DataStore のシナリオアセットに取り込む。
```

主なエディタ拡張: Play 前に Global シーンを開く `GlobalSceneAutoLoader`、ステージ・Section・ボスフェーズを指定して Battle を起動する `BattleDebugLauncher` (`Tools > Battle Debug Launcher`)、Addressables のアドレスを自動で付ける `AddressableAutoAddressAssigner`、SerializeReference の Inspector 補助、MVP 雛形生成。

## Scenes

```text
Assets/Project/Scenes/
├── Entry.unity             # 起動専用シーン。
├── Global.unity            # 常駐管理シーン (カメラ・サウンド・画面遷移・共通モーダル)。
├── Title.unity             # タイトル画面。
├── StageList.unity         # ステージ選択画面。
├── SoundRoom.unity         # サウンドルーム (BGM 鑑賞)。
├── Battle.unity            # 道中・ボス共通のバトル画面。
├── Scenario.unity          # 会話シーン。Battle から加算ロードされる。
├── DemoClear.unity         # ステージクリア後の画面。
├── MVCapture.unity         # MV 撮影用シーン (Commons/Debugger の MVCaptureScenePresenter で進行)。
└── <Scene>/                # 各シーン固有のスクリプト・プレハブ。
```

シーン固有フォルダの共通ルール:

- `<Scene>/Scripts/{Model,View,Presenter,Repository/ModelRepository}/` に置き、各フォルダの `.asmref` でグローバル asmdef に所属させる。
- ファイル名は `<機能名>Model.cs` / `<機能名>View.cs` / `<機能名>Presenter.cs`。シーンの入口は `<Scene>ScenePresenter.cs`。
- シーン固有のプレハブは `<Scene>/Prefabs/` に置く。

各シーンの中身:

- `Entry/`: Addressables 初期化と Global・Title の加算ロード。
- `Global/`: 常駐 UI・入力・サウンド (`SoundManagerPresenter`)・画面遷移 (`SceneNavigator`) を束ねる。
- `Title/`: タイトルメニューと StageList / SoundRoom への遷移。
- `StageList/`: ステージカード一覧と Battle への遷移。カードのプレハブは `Prefabs/`。
- `SoundRoom/`: 解放済み BGM の一覧と再生。
- `Scenario/`: シナリオデータの形式 (Model)、会話の表示とスキップ確認モーダル (View/Presenter)、流すシナリオの受け渡し (ModelRepository)。
- `DemoClear/`: クリア表示と StageList への遷移。
- `Battle/`: 下記。

### Battle

```text
Battle/
├── Documents/                         # Battle 固有の設計メモ。
├── Editor/                            # Battle 用のエディタ拡張。
│   └── BehaviorTree/                  # BehaviorTree エディタ (GraphView、ノードパレット、パラメータ欄)。
├── Prefabs/                           # バトル用プレハブ。
│   ├── Commons/                       # ステージ共通 (ボス HP バー、汎用弾)。
│   ├── Player/                        # 自機と自機弾。
│   └── Stage1/ 〜 Stage4/             # ステージ固有の敵・ボス・弾・演出。
├── Shaders/                           # Battle 固有のシェーダー (風向き矢印)。
└── Scripts/
    ├── BehaviorTree/                  # BT のコアとノード定義。
    │   ├── Core/                      # ノード基底、ツリーアセット (Stage / Enemy / Bullet / Common)、BattleStageAsset、パラメータ (Bt*)、複製、Runner。
    │   ├── Composite/                 # 子を複数持つノード (Sequence / Selector / Parallel、ステージ進行の Stage / PlayScenario など)。
    │   ├── Decorator/                 # 子を 1 つ持つノード (Loop / Phase / Section / WithWind など)。
    │   ├── Action/                    # 葉ノード (Wait / SubTree、弾を撃つ・ビーム・移動・出現・BGM/SE・ボス戦など)。
    │   ├── Condition/                 # 条件の定義 (HpBelow / NoActiveEnemies / Signaled など)。
    │   └── Parts/                     # ノードではない部品 (ShotPattern・DirectionSource・SpawnLayout・BulletConfig・BulletPreset・ビーム) と BossDefinition / TrailPath。
    ├── Runtime/                       # BT の実行基盤 (BattleFlow / BattleWorld / BattleClock、Agent、出現・プール、弾の実行、会話の受け口、合図)。
    │   ├── Stage/                     # ステージサービス (StageContext / WindService) と自機・弾への外力の受け口。
    │   └── Motion/                    # 敵・ボス・弾共通の移動定義 (MotionStep と `*Motion.cs`)、実行器、MotionPreset。
    ├── Model/                         # Battle の純粋データ (風の設定型、背景状態、画面境界)。
    │   └── Entity/                    # Player / Enemy / Boss / Bullet / SpectrumBar の EntityModel。
    ├── Presenter/                     # Battle 全体の進行 (BattleScenePresenter)、背景・壁・弾消去・敵数監視・風向き矢印。
    │   └── Entity/                    # エンティティ制御 (自機・敵・ボス・弾・ビーム)、死亡演出、ステージ固有のボス挙動。
    ├── Repository/ModelRepository/    # asmref のみ。
    └── View/                          # 背景・予告線・ビーム・風向き矢印・曲名表示・当たり判定補助。
        └── Entity/                    # エンティティ View の基底と各 View。
```

- `Scripts/BehaviorTree/` と `Scripts/Runtime/` は `Project.Scenes.Battle.Presenter.asmref` で Presenter アセンブリに含まれる。
- BT は 1 ファイル 1 型。ノードは基底クラスごとのフォルダ (`Composite/` / `Decorator/` / `Action/` / `Condition/`) に置き、ファイル名は型名と同じにする。ノードではない部品は `Parts/` に置く。
- ステージ固有のサービスは `Runtime/Stage/` に `IStageService` と `StageServiceDefinition` の組で置く。
- 移動ステップは `Runtime/Motion/` に `<名前>Motion.cs` で置く。
- BT エディタ (`Window > Battle > Behavior Tree Editor`) のコードは `Editor/BehaviorTree/` に置く。
- 仕組みは `Document/Battle/BehaviorTree.md`、ノードの一覧は `Document/Battle/BehaviorTree-Nodes.md` を参照。

## Scripts

```text
Assets/Project/Scripts/
├── Extensions/             # enum (SceneType / BattleStageType / BgmType / SeType)、入力、共通拡張、定数。
│   └── Message/            # UniRx MessageBroker で流すメッセージ (入力・画面遷移・シナリオコマンド)。
├── Infra/                  # ScriptableObject/JSON のデータ定義 (`*DataObject.cs`、UserData、KeyConfigData)。
├── Model/                  # シーン横断の純粋 Model (UserModel / RuntimeModel / StageModel など)。
├── Presenter/              # シーン横断 Presenter 基底 (MonoPresenter) と起動補助。
├── Repository/             # シーン横断の Repository。
│   ├── AssetRepository/    # Addressables 初期化、Sprite/AudioClip のロード (`*AssetRepository.cs`)。
│   └── ModelRepository/    # Model の生成・キャッシュ・DataStore のロード (`*ModelRepository.cs`)。
└── View/                   # シーン横断 View アセンブリ定義。
```

- `RuntimeModel` は実行中のステージ (`CurrentStageType`) とリトライで戻る Section 番号 (`CheckpointSection`) を持つ。
- `UserModel` は `UserData.json` の読み書きと進行状態・音量を扱う。
- `SceneRouterModel` にシーン名の定数を置く。

## Textures

```text
Assets/Project/Textures/
├── Character/              # キャラクター立ち絵・表情・ドット画像 (キャラ名ごと)。
├── Enemies/                # 敵キャラクター画像。
├── Fonts/                  # 会話・UI 用フォントと TMP フォントアセット。
├── Images/                 # 背景、弾、ボタン、装飾、ロゴ、テキストボックス、サウンドルーム用画像。
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
├── Extensions/Extensions.asmdef                       # 共通 enum/拡張/メッセージ。
├── Infra/Infra.asmdef                                 # データ定義。
├── Model/Model.asmdef                                 # Model。
├── Presenter/Presenter.asmdef                         # Presenter。
├── Repository/Repository.asmdef                       # Repository ルート。
├── Repository/AssetRepository/AssetRepository.asmdef  # アセットロード。
├── Repository/ModelRepository/ModelRepository.asmdef  # Model 生成。
└── View/View.asmdef                                   # View。
```

- `Scenes/` と `Commons/` / `Samples/` のスクリプトは asmdef を持たず、各レイヤフォルダの `.asmref` で上記 Model / View / Presenter / ModelRepository に所属する。
- `Assets/Project/Editor/` と `Scenes/Battle/Editor/` は asmdef を持たない Editor フォルダ。
- ZString は `Assets/Scripts/ZString/ZString.asmdef`。

## Runtime Scene Flow

```text
Entry
  -> Addressables initialize
  -> load Global additively
  -> load Title additively
  -> unload Entry

Title
  -> StageList
  -> SoundRoom (-> Title)

StageList
  -> set RuntimeModel.CurrentStageType
  -> set RuntimeModel.CheckpointSection = 0
  -> Battle

Battle (BattleScenePresenter)
  -> load BattleStageAsset and run the stage tree (BattleFlow)
  -> Section "Way": waves
  -> Section "Boss": load Scenario additively, spawn boss, boss battle, load Scenario additively
  -> stage tree completes: clear the stage and open the next stage
  -> load DemoClear additively

DemoClear
  -> any key: load StageList, unload Battle and DemoClear

Pause / GameOver
  -> Retry: restart from RuntimeModel.CheckpointSection
  -> Title
```

Scenario は `ScenarioScenePresenter` が会話を表示し、演出以外のコマンドは `ScenarioCommandMessage` で Battle のツリーへ渡す。会話が終わると自身をアンロードする。

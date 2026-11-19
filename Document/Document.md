# Magic-science-world Project Document

このドキュメントは、他の AI や新規参加者がこのファイルを読むだけでプロジェクト全体の構造を把握できることを目的にしたリファレンスである。
関数単位の細かい説明ではなく、各ディレクトリ、シーン、中心となるクラス、データアセットが「何を担当できるか」を中心に説明する。
個別のクラスは役割ごとにまとめて記載するので、詳細は記載のパスのソースを参照すること。

## Project Summary

Magic-science-world は、魔法と科学が融合した世界を舞台にした Unity 2D ゲームプロジェクトである。
画面構成はタイトル、ステージ選択、サウンドルーム、バトル、シナリオ、デモクリア、常駐グローバル管理シーンで構成される。

バトルは `Battle` シーンに統合されており、1 ステージ（道中・会話・ボス戦）を 1 つの `BattleStageAsset` とそのステージツリーで表す。道中とボス戦はステージツリーの Section（`BattleSituation` の `Way` / `Boss`）で区切られ、`RuntimeModel` は現在ステージとリトライで戻る Section 番号（`CheckpointSection`）だけを持つ。
バトル進行は ScriptableObject の `BattleStageAsset` と、それが参照する BehaviorTree（`BehaviorTreeAsset`）を中心にデータ駆動で構築される。BehaviorTree は固定 60fps のフレームで実行され、道中の敵出現と敵・ボス・弾の行動を記述する。

## Technology Stack

- Unity: `6000.0.59f2`
- Render Pipeline: Universal Render Pipeline `17.0.4`
- Asset Loading: Addressables `2.7.3`
- Input: Unity Input System `1.14.2`
- Timeline: Unity Timeline `1.8.9`（manifest にあるが、コードからは使われていない）
- Battle Logic: 自前の BehaviorTree 実装（`Scenes/Battle/Scripts/BehaviorTree`）と GraphView ベースのエディタ
- UI: uGUI / TextMesh Pro
- Async: UniTask
- Reactive Event: UniRx
- Tween: DOTween
- String Formatting: ZString
- SerializeReference Inspector: MackySoft SerializeReferenceExtensions

## Architectural Policy

このプロジェクトは MVP を基本方針にしている。

### Model

Model は画面に依存しない状態とロジックを持つ。
原則として `MonoBehaviour` を継承しない。
ScriptableObject データ、セーブデータ、実行時状態、Entity の HP/死亡/衝突判定などを扱う。

代表例:

- `RuntimeModel`: 現在ステージとリトライ地点（`CheckpointSection`）を持つ。
- `StageModel`: ステージの開放/クリア状態とクリアランク、`BattleStageAsset` と背景のアドレスを持つ。
- `UserModel`: セーブデータ（進行、クリアランク、音量、各種オプション）を管理する。
- `BattleStageAsset` / `BehaviorTreeAsset`: バトル進行（道中・ボス戦・敵と弾の行動）のデータ。実行は `Scenes/Battle/Scripts/Runtime` の `BattleFlow` / `BattleWorld` が行う。
- `EntityBase` 派生: プレイヤー、敵、ボス、弾の HP/死亡/衝突を扱う。

### View

View は画面表示、UI、Sprite、Animation、Text、Canvas など Unity コンポーネントの操作を持つ。
原則として `MonoBehaviour` を継承する。
入力イベントの公開や表示更新は行うが、進行ロジックやデータロードは Presenter/Model/Repository に寄せる。

### Presenter

Presenter は Model と View を接続し、シーン進行や入力購読を持つ。
Unity 上で View を `SerializeField` 参照する必要があるため、`MonoBehaviour` を継承する。
共通 Presenter は `MonoPresenter` を継承し、`GlobalScenePresenter`、`SoundManagerPresenter`、共通 Repository にアクセスする。
各シーンには中心となる `<Scene>ScenePresenter` がある。

### Repository

Repository は Model や Asset の生成、ロード、キャッシュを担当する。
Addressables、ScriptableObject、AudioClip、Sprite、InputActionAsset など外部データへのアクセスを集約する。
多くの Repository はシングルトンとして `Instance` を持つ。

- `ModelRepository`: ScriptableObject やセーブデータから Model を生成・保持する。
- `AssetRepository`: Addressables から Sprite / AudioClip などを同期ロードする。

### Infra

Infra は永続データと ScriptableObject データ構造の定義を持つ。
ゲームロジックではなく、データベースとして保存される形を表す。

## Runtime Flow

### Boot

起動時は `Entry.unity` から始まる。

1. `EntryScenePresenter` が Addressables を初期化する。
2. `Global.unity` を Additive でロードする。
3. `Title.unity` を Additive でロードする。
4. Active Scene を Title にする。
5. Entry シーンをアンロードする。

`Global` は常駐シーンとして扱われ、画面遷移後も残る。
Editor で Entry 以外のシーンから再生した場合は、`GameBootStrapper`（Play 時）と `GlobalSceneAutoLoader`（Editor 拡張）が Global を Additive でロードする。

### Global

`GlobalScenePresenter` は常駐管理の中心である。

担当:

- 共通モーダルの保持: Option, Pause, GameOver, Tutorial。
- `SoundManagerPresenter` の保持。
- 全シーン共通のカメラ。各シーンはカメラを持たず、Canvas は `GlobalCameraCanvas` でこのカメラを参照する。
- 画面を 16:9 に固定する。カメラの Viewport Rect で描画領域を作り、余白は黒帯にする。
- `InputActionAsset` を `KeyConfigModelRepository` に渡し、EventSystem の `InputSystemUIInputModule` に設定する。
- `InputManager` を生成し、Input System の入力を UniRx `MessageBroker` へ変換する。
- `SceneNavigator` を公開し、各 Scene Presenter から画面遷移できるようにする。

### Scene Navigation

画面遷移は `SceneNavigator` が担当する。

流れ:

1. `SceneNavigationMessage(InProgress, toSceneName)` を publish する。
2. 移動先シーンを Additive でロードする。
3. 移動先を Active Scene にする。
4. 移動元シーンをアンロードする。
5. `SceneNavigationMessage(Completed, toSceneName)` を publish する。

`BattleScenePresenter` はこの Completed メッセージを受けて初期化を始める。
Battle から Title / DemoClear、DemoClear から StageList への遷移は `SceneNavigator` を使わず、各 Presenter が `SceneManager` で直接ロード/アンロードする。

### Title to Battle

```text
Title
  -> StageList
  -> RuntimeModel.CurrentStageType = 選択したステージ
  -> RuntimeModel.CheckpointSection = 0
  -> Battle
```

Title の Start は StageList へ遷移する。
StageList は開放済みのステージだけ選択でき、選択した `StageModel` の `BattleStageType` を `RuntimeModel` にセットして Battle へ遷移する。

### Battle to Scenario

会話はステージツリーの `PlayScenario` ノードが開始する。

1. `PlayScenario` が `IScenarioService`（`BattleScenePresenter`）に `ScenarioData` を渡す。
2. `BattleScenePresenter` は `ScenarioModelRepository.SetData` で流すシナリオを設定し、`Scenario.unity` を Additive ロードする。会話中は自機の攻撃と当たり判定を止める。
3. `ScenarioScenePresenter` は演出関数以外の行を `ScenarioCommandMessage` として publish する。`BattleScenePresenter` はそれを `ScenarioCommandQueue` に積み、ツリー側（同名の `OnCommand`）が処理する（ボス出現や BGM 切り替えなど）。
4. 会話が終わると `ScenarioScenePresenter.OnScenarioCompleted` を発火し、Scenario シーンをアンロードする。
5. `BattleScenePresenter` が自機の当たり判定（指定があれば攻撃入力も）を戻し、`PlayScenario` が次へ進む。

リトライなどで途中の Section から再開した場合、その Section の会話はノードの設定（`skipOnResume`）に従って表示せずコマンドだけ実行する。

### Stage Clear

ステージツリーが Success で終わると、`BattleScenePresenter.CompleteStage` が次を行う。

1. クリアランクを決めて `StageModel.Clear` で保存する（コンティニューあり: `Normal`、被弾あり: `NoContinue`、被弾なし: `NoDamage`）。
2. 次のステージを開放する（未解放扱いのステージは開放しない）。
3. `BattleFlow` を止め、`DemoClear.unity` を Additive ロードする。
4. `DemoClearScenePresenter` が入力を受けて Title BGM を流し、StageList をロードして Battle と DemoClear をアンロードする。

### Retry

GameOver / Pause のリトライは `BattleScenePresenter.Retry` が処理する。

- 通常: コンティニュー回数を進め、`continueHpTable` に従って自機の最大 HP を上げ、`RuntimeModel.CheckpointSection` の Section から再開する。
- オプション「リトライ時にステージの最初からやり直す」が ON: コンティニュー回数と被弾履歴をリセットし、Section 0 から再開する。クリアランクは通常どおり判定される。

## Core Shared Files

### Assets/Project/Scripts/Presenter

#### `MonoPresenter.cs`

共通 Presenter 基底。
`Start` で `GlobalScenePresenter` を検索（static にキャッシュ）し、`SoundManagerPresenter` を取得する。
`RuntimeModelRepository` と `StageModelRepository` への protected アクセスを提供する。
Global が未ロードの状態で使うと `soundManager` の取得で失敗する。

#### `GameBootStrapper.cs`

Editor 専用。`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` で、Entry を経由しない再生時に Global を Additive ロードする。

### Assets/Project/Scripts/Model

#### `ModelBase.cs`

共通 Model 基底。
`UserModel` と `RuntimeModel` の参照を持ち、各 Model にユーザー進行状態や実行時状態を注入するための共通形として使われる。

#### `RuntimeModel.cs`

実行中のゲーム状態を持つ Model。`BattleSituation`（`Way` / `Boss`）もここで定義する。

保持するもの:

- `CurrentStageType`: 現在のステージ。`IsInGame` は `Null` でないかを返す。
- `CheckpointSection`: リトライで戻る Section の番号（ステージツリーの Stage ノード直下の並び順）。`BattleScenePresenter` が Section 開始ごとに更新する。

できること:

- `ExitStage` でステージ外状態へ戻す。
- Editor/Development build では `SetForDebug` で開始ステージ・Section・ボスの開始フェーズと HP をセットする（`DebugRuntimeModelInitializer` が使う）。

#### `StageModel.cs`

ステージ単位の状態とデータを持つ Model。クリア評価の enum `StageClearRank`（`None` / `Normal` / `NoContinue` / `NoDamage`）もここで定義する。

保持するもの:

- `StageData`、ステージ番号、`BattleStageType`
- `StageAddress`（`BattleStageAsset` のアドレス）、`BackgroundAddress`（Battle 背景のアドレス）
- ステージカード用キャラスチル
- 開放状態、クリア状態、`ClearRank`（`UserModel` から取得）

できること:

- `Start` で `RuntimeModel.CurrentStageType` を更新する。
- `Open` でステージを開放する。未解放扱いのステージ（`UserModel.IsStageConcealed`）は開放しない。
- `Clear(rank)` でステージクリアとランクを `UserModel` へ保存する。
- ステージカード表示用の ID とタイトルを返す。

#### `UserModel.cs`

ユーザーセーブデータを読み書きする Model。

保持/管理するもの:

- `UserData`
- クリア済みステージ番号と、ステージごとの最高クリアランク（`GetStageClearRank`。ランク記録が無いクリア済みステージは `Normal` 扱い）。
- BGM/SE 音量（0-100）。
- シナリオの文字送り速度 `TextSpeed`（1 秒あたりの文字数。`SetTextSpeed` は範囲に丸める）。
- 連打スキップ設定 `IsMashSkipEnabled`、リトライ設定 `IsRetryFromStageStartEnabled`。
- Stage1 初回入場済みフラグ（チュートリアルのスキップ待ち判定）。

できること:

- 開放判定 `IsOpenedStage`（前のステージをクリア済みで、未解放扱いでない）とクリア判定 `IsClearedStage`。
- `IsStageConcealed`: 5 面は前のステージをクリアしても未解放と同じ扱い（選択不可・情報非表示）にする。
- `LatestOpenedStageNumber`: 解放済みで最も先のステージ番号。
- `TitleBgmType`: 4 面クリア後は `Phase2`、それ以外は `Default`。
- 設定値の Set 系はメモリへの反映だけを行い、永続化は `Save()` を別に呼ぶ（Option モーダルを閉じたときなど）。`StageClear` と `MarkEnteredStage1` は即保存する。

保存と読み込みは「Save Data Rules」を参照。

#### その他の Model

- `AddressablesModel.cs`: Addressables の初期化。`EntryScenePresenter` が使う。
- `SceneRouterModel.cs`: シーン名定数（`Global`, `Title`, `StageList`, `Scenario`, `Battle`, `DemoClear`, `SoundRoom`）。
- `BgmModel.cs` / `SeModel.cs`: `BgmData` / `SeData` と AudioClip（必要時にロード）を表す。`BgmModel` は表示名やサウンドルーム用の項目も公開する。
- `CharacterModel.cs`: `CharacterData` をラップする。
- `KeyConfigModel.cs`: InputActionAsset と、アクション（上下左右・攻撃・チャージ・決定・戻る）×スロット（Primary / Secondary）ごとのキー割り当てを扱う。`UserData.keyConfigData` の上書きを起動時に適用し（不正な値は破棄）、割り当て変更・リセットの API を持つ。

### Assets/Project/Scripts/Infra

#### `StageDataObject.cs`

ステージ一覧データベース。`StageDataObject` は `List<StageData>` を持つ ScriptableObject。

`StageData` が定義するもの:

- `id`: 表示/識別用 ID。
- `stageNumber`: int のステージ番号。
- `charaStillAddress`: ステージカードとシナリオに使うキャラクター名（スチル・表情のパスに使う）。
- `backgroundAddress`: Battle 背景 Sprite の Addressables アドレス。
- `title`: ステージタイトル。
- `stageAddress`: `BattleStageAsset`（道中・会話・ボス戦を含む 1 ステージ分）の Addressables アドレス（アセットパス）。
- `scenarioEnemyStillOffset` / `scenarioEnemyStillScale`: シナリオ中の敵立ち絵の位置・拡大率の補正。

#### `BgmDataObject.cs`

BGM データベース。
`BgmData` は名前、表示名、対象 SceneType、BgmType、ループ開始/終了サンプル、サウンドルーム用の項目（解放に必要なクリア数、シークレット、カード画像のステージ番号、説明文）を持つ。

#### `UserData.cs`

セーブデータの JSON 形式。
保持するもの:

- `clearedStageNumber`
- `keyConfigData`（`KeyConfigData.cs`: キー割り当ての上書き一覧）
- `bgmVolume` / `seVolume`（既定 70）
- `hasEnteredStage1`
- `textSpeed`（既定 25、範囲 1-200）
- `stageClearRanks`（index = ステージ番号 - 1。値は `StageClearRank` の int）
- `isMashSkipEnabled`
- `isRetryFromStageStartEnabled`

`Validate` は読み込んだ値を範囲に丸める。`clearedStageNumber` は 0-4 に Clamp され、この上限が公開するステージの範囲を決める。

#### その他

- `CharacterDataObject.cs`: キャラクター ID、名前、英名。
- `SeDataObject.cs`: SE 名、SeType、ループ開始/終了サンプル（ループ SE 用）。

### Assets/Project/Scripts/Repository

ModelRepository（`Repository/ModelRepository/`）:

- `ModelRepositoryBase.cs`: `dataName` から `Assets/Project/DataStore/{dataName}.asset` のアドレスを作り、`LoadDataObject<T>` で同期ロードする。`UserModel` への protected アクセスを持つ。
- `RuntimeModelRepository.cs` / `UserModelRepository.cs`: `RuntimeModel` / `UserModel` のシングルトンを提供する。
- `StageModelRepository.cs`: `StageData.asset` から全 `StageModel` を生成し、`UserModel` の開放/クリア状態を反映する。ステージ番号による取得と全件取得を提供する。
- `SoundModelRepository.cs`: `BgmData.asset` / `SeData.asset` から `BgmModel` / `SeModel` を生成する。BGM は曲が切り替わると前の AudioClip を解放（`RefreshBgmModel`）し、SE は保持し続ける。`CreateAllBgmModels` はサウンドルームの曲一覧用。
- `CharacterModelRepository.cs`: `CharacterData.asset` から `CharacterModel` を生成する。
- `KeyConfigModelRepository.cs`: `GlobalScenePresenter` から InputActionAsset を受け取り、`KeyConfigModel` を生成・保持する。

AssetRepository（`Repository/AssetRepository/`、基底 `AssetRepositoryBase`）は Addressables から同期ロードする。アドレスは「Asset Loading Rules」を参照。

- `StillAssetRepository.cs`: キャラクタースチル（`_Crazy` 差分対応。存在しなければ null）。
- `FaceAssetRepository.cs`: 表情画像群を表情名 key の Dictionary にする。
- `AllCharacterStillAssetRepository.cs` / `BackGroundAssetRepository.cs`: Title / StageList の背景用画像。
- `SoundAssetRepository.cs`: BGM/SE の AudioClip。
- `AddressablesInitializeAssetRepository.cs`: Addressables の初期化。

### Assets/Project/Scripts/Extensions

- `SceneType.cs`: シーン種別 enum。`ToSceneName` で StageN 系は `Battle` に変換される。BGM データ検索にも使う。
- `BattleStageType.cs`: ステージの enum（Stage1〜6、StageEx、Null）と int 変換。
- `BgmType.cs` / `SeType.cs`: BGM / SE 種別 enum。
- `GamePath.cs`: Addressables の基準パス（`Assets/Project/Textures`, `Assets/Project/DataStore`）。
- `InputManager.cs`: InputActionAsset の Player / UI マップの各 Action を `MessageBroker` のメッセージに変換して publish する。
- `Message/InputActionMessage.cs`: `InputManager` が publish するメッセージ型（`PlayerMoveMessage`, `PlayerAttackMessage`, `PlayerChargeMessage`, `PlayerPauseMessage`, `UINavigateMessage`, `UISubmitMessage`, `UICancelMessage`, `UICtrlMessage` など）。
- `Message/SceneNavigationMessage.cs`: 画面遷移の `InProgress` / `Completed` 通知。
- `Message/ScenarioCommandMessage.cs`: 会話中のコマンド行（関数名と引数）の通知。
- `ObservableExtensions.cs`: `SubscribeBlocking`（非同期処理中の再発火を無視して二重実行を防ぐ）。
- `InputSystemActions.cs`: Input System の自動生成コード。
- `BoolExtensions.cs`, `StringExtensions.cs`, `ReadOnlyAttribute.cs`: 小さな補助。

## Scene Details

## Entry Scene

### `EntryScenePresenter.cs`

起動専用 Presenter。
Addressables 初期化、Global ロード、Title ロード、Entry アンロードを順番に行う。

## Global Scene

### `GlobalScenePresenter.cs`

常駐する全体管理 Presenter。担当は「Runtime Flow > Global」を参照。

保持するもの:

- `OptionModalPresenter` / `PauseModalPresenter` / `GameOverModalPresenter` / `TutorialModalPresenter`
- `SoundManagerPresenter`
- `InputActionAsset`、`KeyConfigModel`、`InputManager`
- `SceneNavigator`
- メインカメラ、黒帯用カメラ（`TargetAspect` / `OrthographicSize` 定数を公開する）

`MonoPresenter` は `GlobalScenePresenter` を検索するため、`GlobalScenePresenter` 自身は `MonoPresenter` を継承しない。

### `SceneNavigator.cs`

画面遷移専用クラス。Additive ロードと移動元シーンのアンロードをセットで行い、遷移中/遷移完了メッセージを publish する。

### `SoundManagerPresenter.cs`

BGM/SE 再生を扱う常駐 Presenter。

できること:

- シーン種別と BGM 種別から BGM を再生する（同じ曲の再生中はスキップ可能）。
- BGM を停止する。`FadeOutBGMAsync` でフェードアウトして停止する。
- BGM のループ回数 `BgmLoopCount` を数える（サウンドルームの自動送りで使う）。
- SE を単発再生する。ループ SE を再生/停止する。
- BGM とループ SE のループ位置を `timeSamples` で手動制御する。
- ユーザー音量を dB カーブで AudioSource 音量（最大 0.5）へ変換して反映する。

## Title Scene

`Assets/Project/Scenes/Title/Scripts/`

- `TitleScenePresenter.cs`: タイトルの中心 Presenter。Start で StageList、Sound Room で SoundRoom へ遷移し、Option で共通 OptionModal を開く（開いている間は `UICancelMessage` でも閉じる）。Exit で Editor 再生停止または `Application.Quit`。BGM は `UserModel.TitleBgmType` を再生する。
- `TitleMenuView.cs`: 背景・メンバースチルの表示と、各ボタンの押下イベントを提供する。
- `TitleModel.cs` / `TitleModelRepository.cs`: クリア数に応じた背景 Sprite とメンバースチルを持つ。Title 以外では Refresh で破棄する。

## StageList Scene

`Assets/Project/Scenes/StageList/Scripts/`

### `StageListScenePresenter.cs`

ステージ選択の中心 Presenter。

責務:

- `StageModelRepository.GetAll()` の全ステージをカードに表示する。未開放のステージは選択不可にし、タイトルを伏せる。
- クリアランクに応じたカードの外枠（`ClearFrameType`）を表示する。
- 選択中ステージのキャラスチルを表示する（未クリアならシルエット）。
- 決定時に `RuntimeModel.CurrentStageType` と `CheckpointSection = 0` をセットして Battle へ遷移する。
- `UICancelMessage` で Title に戻る。

View（`StageCardListView` / `StageCardView` / `StageListSceneView`）はカード一覧の選択・決定イベント、カード表示、背景を扱う。
`StageListModel` / `StageListModelRepository` はクリア数に応じた背景 Sprite を持つ。全ステージ開放後のみ、一覧の端で上下を押すと反対側へ回り込む。

## Scenario Scene

`Assets/Project/Scenes/Scenario/Scripts/`

### `ScenarioData.cs`

シナリオデータ（ScriptableObject）。`ScenarioStep`（`function` と `args`）の列を持つ。
会話シーン自身が処理する演出関数（`ShowCastMessage`, `ShowMessage`, `TypeWriter`, `AutoAdvance`, `ShowCast`, `HideCast`, `ChangeCastAnimation`, `ShakeCastV`）を `IsPresentation` で判定する。それ以外の行はバトル側へ通知するコマンドである。

### `ScenarioModel.cs` / `ScenarioModelRepository.cs`

`ScenarioModel` はステップ列と現在位置、プレイヤー（Ten）と敵のスチル・発狂スチル・表情 Sprite、敵立ち絵の位置補正を持つ。
`ScenarioModelRepository` は `SetData` で次に流す `ScenarioData` を受け取り（Battle の `PlayScenario` や MVCapture が設定する）、`RuntimeModel.CurrentStageType` の `StageData` からキャラクター画像と立ち絵の補正を読み込んで `ScenarioModel` を作る。

### `ScenarioView.cs`

メッセージ、立ち絵、表情差分、話者以外の立ち絵のグレー表示、フェードアウトを表示する。
文字送り（TypeWriter）は DOTween で 1 文字ずつ表示し、`CompleteTyping` で全文表示に切り替える。

### `ScenarioScenePresenter.cs`

シナリオ進行の中心 Presenter。`MonoPresenter` は継承しない。

責務:

- `UISubmitMessage` で次ステップへ進める。文字送り中なら全文表示だけ行う。Pause 中とスキップ確認モーダル表示中は進めない。
- Step を順に解釈し、メッセージ表示系（`ShowCastMessage` / `ShowMessage`）で入力待ちにする。それ以外の演出関数は即座に次 Step へ進める。
- `TypeWriter`: 文字送りの有効/無効と速度（1 秒あたりの文字数、`default` ならユーザー設定の `TextSpeed`）を設定する。
- `AutoAdvance`: 全文表示後に指定秒数（`auto` なら TypeWriter の 1 文字分の間隔）で自動的に次へ進める。タイマーは timeScale の影響を受け、ポーズ中は止まる。
- 演出関数以外の行は `ScenarioCommandMessage` として publish する（処理はバトル側）。
- 連打スキップ: `mashWindowSeconds` 秒以内に `mashPressCount` 回決定入力すると連打とみなす。連打スキップ設定が ON なら確認なしでスキップし、OFF なら `SkipConfirmModalPresenter` を開く。「スキップしない」を選ぶと、ゲームを再起動するまで連打検知を止める（保存はしない）。
- スキップはメッセージ表示だけを飛ばし、コマンドと他の演出関数は実行して末尾まで進める。
- 終了時に `OnScenarioCompleted` を 1 回だけ発火し、Scenario シーンをアンロードして `ScenarioModelRepository` を Refresh する。

### `SkipConfirmModalPresenter.cs` / `SkipConfirmModalView.cs`

連打時に出すスキップ確認モーダル。選択結果を `OnSelected`（`SkipConfirmResult`）で通知する。

- `EnableMashSkip`: 連打スキップ設定を ON にして保存し、スキップする。
- `SkipOnce`: 今回だけスキップする。
- `Cancel`: スキップしない（初期フォーカス）。

連打の勢いで即決定されないよう、表示直後 `inputGuardSeconds` 秒は入力を無視する。

## SoundRoom Scene

`Assets/Project/Scenes/SoundRoom/Scripts/`

### `SoundRoomScenePresenter.cs`

サウンドルームの中心 Presenter。

責務:

- `SoundModelRepository.CreateAllBgmModels()` の曲を `BgmData` の登録順にカードへ表示する。未解放のシークレット曲は一覧から除き、シークレット曲には通し番号を振らない。
- 解放条件は「クリア数 ≥ `soundRoomUnlockClearedStage`」かつ「カード画像のステージが未解放扱いでない」こと。未解放の曲はタイトル・説明を伏せ、再生しない。
- 選択した曲を再生し、`autoPlayLoopCount` 回ループしたら `autoPlayFadeDuration` 秒でフェードアウトして次の解放済みの曲へ進む（末尾の次は先頭）。
- 背景はタイトルと同じ画像を使う。
- `UICancelMessage` で Title に戻る。

### `SoundRoomView.cs`

曲カードの一覧（`ScrollableButtonList`）、カード画像（ステージ番号ごと）、再生中ポインタ、説明欄を扱う。全曲解放後のみ端で上下を押すと反対側へ回り込む。

## DemoClear Scene

`Assets/Project/Scenes/DemoClear/Scripts/`

- `DemoClearView.cs`: フェードインし、表示完了後の `UISubmitMessage` / `PlayerAttackMessage` を入力イベントとして公開する。
- `DemoClearScenePresenter.cs`: 入力を 1 回受け取り、Title BGM を流して StageList をロードし、Battle と DemoClear をアンロードする。Battle がロードされていない場合（MVCapture など）は遷移しない。

## Debugger / MVCapture

`Assets/Project/Commons/Debugger/Scripts/Presenter/`。どちらも Editor/Development build でのみコンパイルされる。

- `DebugRuntimeModelInitializer.cs`: Battle シーンを単体で再生するとき、`RuntimeModel.SetForDebug` で開始ステージ・Section・ボスの開始フェーズと HP をセットし、`SceneNavigationMessage(Completed, Battle)` を publish して Battle を開始させる。既にゲーム中なら何もしない。
- `MVCaptureScenePresenter.cs`: 告知 MV などの撮影用シーン `Assets/Project/Scenes/MVCapture.unity` の進行役。指定ステージの Battle 背景の上で `ScenarioData` を流し、終わったら DemoClear を表示する。立ち絵のステージ（`charaStageNumber`）と背景のステージ（`backgroundStageNumber`）を指定できる。破棄時に `RuntimeModel.ExitStage` を呼ぶ。

## Battle Scene Overview

Battle はこのプロジェクトで最も大きい領域である。
バトルの進行（道中の敵出現、ボスの攻撃・移動、フェーズ切り替え）は BehaviorTree（BT）で記述する。

```text
StageData
  -> StageModel
  -> StageAddress (Addressables アドレス = アセットパス)
  -> BattleStageAsset (1 ステージ = 道中・会話・ボス戦)
       services[]  -> StageContext (ステージサービス: 風など)
       tree        -> BehaviorTreeRunner (ステージツリー。Section "Way" / "Boss")
                       Spawn     -> EnemyAgent (雑魚に付与。通常は共通ツリー Commons/BT/Enemy)
                       SpawnBoss -> BossDefinition.tree -> EnemyAgent (ボスに付与し、ボス自身がツリーを実行)
  -> BattleFlow / BattleWorld (固定 60fps のフレームで全ツリーと弾を進める)
```

BT の構造・ノード・データ構成は次のドキュメントを参照すること。

- [Battle/BehaviorTree.md](./Battle/BehaviorTree.md): BT 方式の全体構造、実行順、ステージの流れ、ステージサービス、エディタ、設計判断。
- [Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md): 汎用ノード（制御・条件・基本 Action）のフィールドと戻り値。バトル固有のノードはソースの Tooltip / `Summary` を参照。
- [Battle/DataStore.md](./Battle/DataStore.md): `DataStore/Commons/BT/` と `DataStore/Stage{N}/BT/` のデータ構成。

### Battle Runtime Flow

1. `BattleScenePresenter` が `SceneNavigationMessage.Completed(Battle)` を受けて開始する。
2. Stage1 を Section 0 から始めるときは TutorialModal を表示し、閉じてから開始する。
3. `RuntimeModel.CurrentStageType` から `StageModel` を解決し、背景（`BackgroundAddress`）と `BattleStageAsset`（`StageAddress`）を Addressables で同期ロードする。
4. `BattleFlow.StartStage` がステージサービスを有効にし、弾と敵を事前生成して、`RuntimeModel.CheckpointSection` の Section からステージツリーを開始する。
5. 道中・会話・ボスの出現と戦闘・撃破後の会話は、すべてステージツリーが進める。Presenter は `BattleFlow` のイベントと `IScenarioService` の呼び出しに演出で応える。
6. ツリーが Success で完了したら `CompleteStage`（「Runtime Flow > Stage Clear」）を行う。

### `BattleScenePresenter.cs`

Battle 全体の中心 Presenter。`IScenarioService` を実装する。

保持するもの:

- `BattleFlow`（と、同じ GameObject の `BattleClock`）
- `EnemyTracker` / `BackgroundPresenter` / `BulletClearReceiver` / `BgmNameView`
- `StageModel` / `BattleStageAsset` / `BattleBackgroundModel`
- `continueHpTable`（コンティニュー回数ごとの自機の最大 HP）とコンティニュー回数
- 再生中の会話（`ScenarioCommandQueue`）

できること:

- Pause 入力で PauseModal を開閉する（GameOver 表示中・DemoClear 表示中は無視、Option 表示中は Option だけ閉じる）。
- 自機の死亡演出完了で GameOverModal を開く。
- GameOver / Pause からのリトライ（「Runtime Flow > Retry」）とタイトル戻りを処理する。
- PlayScenario ノードからの依頼で会話シーンを開き、会話中のコマンドをツリーへ渡す。
- PlayBgm ノードからの依頼で BGM を再生し、曲名を表示する。
- `BattleFlow` のイベント（Section 開始・ボス出現・ボス戦開始・ステージ終了）に応じて、リトライ地点の記録、背景、自機アニメ、ボスの当たり判定を切り替える。
- ステージクリア時にクリアランクを保存し、次ステージを開放して DemoClear を表示する。

`StartBattle` は Unity の `Start` ではなく、SceneNavigation 完了後に呼ばれる。

### Runtime（BT 実行の土台）

`Assets/Project/Scenes/Battle/Scripts/Runtime/`。詳細は [Battle/BehaviorTree.md](./Battle/BehaviorTree.md)。

- `BattleFlow.cs`: バトル 1 回分の BT 実行を管理する。`StartStage` / `Stop` と、ボスの生成・戦闘開始（`IBossHost`）を持つ。
- `BattleWorld.cs`: 全ツリーで共有するフレーム番号・乱数・予約実行・合図の配送・`BattleServices`・`StageContext` を持ち、`Step()` で 1 フレーム進める。
- `BattleClock.cs`: 固定 60fps で `BattleWorld.Step()` を呼ぶ（ポーズ中は停止）。
- `EnemyAgent` / `BulletSystem` / `EnemySpawner` / `EnemyPool` など: 敵・弾へのツリーの付与と実行、出現、使い回し、消える判定。
- `Stage/`: ステージ固有機能（ステージサービス）の仕組みと、風の実装。
- `Motion/`: 敵・ボス・弾で共通の移動定義（`MotionStep` / `MotionPreset`）と実行器。

### BehaviorTree

`Assets/Project/Scenes/Battle/Scripts/BehaviorTree/`。ツリーアセット（`BattleStageAsset`、`BehaviorTreeAsset` と Stage / Enemy / Bullet / Common の 4 種類）、ノードの基底と実装、ノードで使う部品を置く。ノードの一覧と使い方は [Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md) を参照。

### Entity / Presenter / View

役割ごとの配置。個々のクラスはソースを参照すること。

- `Scripts/Model/Entity/`: Entity の純粋 Model（`EntityBase` と自機・敵・ボス・弾・スペクトラムバーの派生）。HP、死亡通知、衝突時のダメージを扱う。ボスの HP 区切りと被ダメージ倍率はボスツリーの `HpSections` が設定する。
- `Scripts/Presenter/Entity/`: 自機（入力・移動・射撃・チャージ・被弾）、敵・ボス・弾の Presenter、弾の共有プール `BulletPool`、死亡演出（`DeathDirectorBase` 派生）、ボスのフェーズ進行ゲージなど。敵・弾は自分では消えず、消える判定は `EnemyAgent` / `BulletSystem` が行う。
- `Scripts/Presenter/`: シーン単位の Presenter（`BattleScenePresenter`、背景、壁、弾・敵の一括消去 `BulletClearReceiver`、敵数の監視 `EnemyTracker`、風向き矢印）。
- `Scripts/Model/`: 背景（`BattleBackgroundModel`）、画面境界のキャッシュ（`ScreenBoundsCache`）、風の設定など。
- `Scripts/View/`: Entity と背景、ビーム・警告線、曲名表示（`BgmNameView`）などの表示。

## Common UI

`Assets/Project/Commons/UI/Scripts/`。共通モーダルの Presenter は Global シーンに常駐する。

### Buttons / Gauges

- `ButtonBase.cs`: フォーカスと押下（`OnPressed` / `OnFocusedEvent`）を持つボタン基底（`Selectable`）。`Init(isOpened, isFocused)` で操作可否と初期フォーカスを設定する。
- `SimpleButton.cs`: 通常のボタン。
- `ButtonListBase.cs` / `ScrollableButtonList.cs` / `ButtonListType.cs` / `MovableButton.cs`: フォーカス位置に合わせてスクロールするボタンリスト。操作可能なボタンだけをフォーカス移動先にし、端での回り込みを切り替えられる。
- `PointerAddon.cs`: フォーカスされたボタン（`ButtonBase`）または `Selectable` の横に選択ポインタを移す。
- `VolumeBlockGauge.cs`: 0-100 の値をブロック列で描く Graphic。
- `VolumeGaugeSelectable.cs`: `VolumeBlockGauge` をフォーカス可能にし、左右キーで値を増減する（Ctrl 押下中は大きい刻み）。
- `GlobalCameraCanvas.cs`: Global シーンのカメラを別シーンの Canvas に割り当てる（`ExecuteAlways`）。

### `OptionModalPresenter.cs` / `OptionModalView.cs`

オプション設定のモーダル。Title / Pause / GameOver から開かれる。

- BGM/SE 音量ゲージ（`VolumeGaugeSelectable`）。変更は即座に SoundManager と `UserModel` に反映する。SE ゲージ選択中はチャージ SE をループ再生して音量を確認できる。
- 連打スキップの ON/OFF、リトライ時にステージの最初からやり直すかの ON/OFF（左右キーで切り替え）。
- 閉じるときに `UserModel.Save()` で保存し、`OnClosed` を通知する。

### `PauseModalPresenter.cs` / `PauseModalView.cs`

Battle 中のポーズ。開いている間は `Time.timeScale = 0` と `AudioListener.pause` にする。
再開、リトライ、オプション、タイトルへ戻るを持ち、リトライ・タイトル戻りは `OnRetryRequested` / `OnTitleRequested` で通知する。
フォーカス中のボタンに応じて tips を表示する。リトライの tips は、リトライ設定に応じて「次回の HP 増加率」か「最初からやり直す」を表示する。

### `GameOverModalPresenter.cs` / `GameOverModalView.cs`

自機死亡後のモーダル。リトライ、オプション、タイトルへ戻るを持ち、Pause と同じ形で通知・tips 表示を行う。

### `TutorialModalPresenter.cs` / `TutorialModalView.cs`

Stage1 を最初から始めるときのチュートリアル。
初めて Stage1 に入ったときだけ `skipLockDuration` 秒は閉じられない。その後 `UISubmitMessage` / `PlayerAttackMessage` で閉じて `OnClosed` を通知する。timeScale は操作しない。

## Editor Extensions

### Project Editor

`Assets/Project/Editor/`

- `GlobalSceneAutoLoader.cs`: Edit 中もシーンを開いたときに Global を Additive で開き、Canvas にカメラを割り当てる。Play 前にも Global をロードする（Entry が開いていればしない）。シーン保存中はシーンを跨ぐカメラ参照を外す。
- `BattleDebugLauncher.cs`: `Tools > Battle Debug Launcher`。ステージ・開始 Section・ボスの開始フェーズと HP を選び、Battle シーンに `DebugRuntimeModelInitializer` を置いて再生する（シーンには保存しない）。
- `AddressableAutoAddressAssigner.cs`: `Assets/Project/Textures/` 配下のアセットを、アセットパスをアドレスとして Addressables に自動登録する。`/Deprecated/` 配下は登録しない。手動で付けたアドレスは上書きしない。
- `ScenarioImporter.cs`: `Tools > Import Scenario`。シナリオテキストを `ScenarioData` アセットに変換する。ステージのシナリオはファイル名から保存先を決め、既存アセットを上書きして参照を保つ。
- `CommonScaffold.cs`: `Tools > CommonScaffold`。Commons 配下の MVP フォルダ構成の雛形を作る。
- `CreateDB.cs`: `Tools > Create DB Generator`。DataStore 用 ScriptableObject と Repository のコードを生成する。
- `SerializeReferenceAutoExpandEditor.cs` / `SerializeReferenceDeduplicator.cs`: SerializeReference の Inspector 自動展開と、重複参照の検出・複製。

### Battle Editor

`Assets/Project/Scenes/Battle/Editor/`

- `BehaviorTree/`: BehaviorTree エディタ（`Window > Battle > Behavior Tree Editor`、または `BehaviorTreeAsset` のダブルクリック）。使い方は [Battle/BehaviorTree.md](./Battle/BehaviorTree.md) の「エディタ」を参照。
- `SpectrumBarHeightRecorder.cs`: スペクトラムバーの高さ記録用の Editor 補助。

## Data Assets

`Assets/Project/DataStore/`

- `StageData.asset`: ステージ一覧。各ステージの表示情報、キャラ名、背景と `BattleStageAsset` のアドレス、シナリオの立ち絵補正を持つ。StageList / Battle / Scenario が依存する。
- `BgmData.asset`: BGM 一覧。SceneType と BgmType の組み合わせで検索される。サウンドルームの曲順・解放条件・説明もここで決まる。
- `SeData.asset`: SE 一覧。
- `CharacterData.asset`: キャラクター一覧。
- `Stage{N}/`: ステージごとのバトル進行データ（`BT/`）とシナリオ（`Scenario/`）。全ステージ共通の雑魚ツリーは `Commons/BT/`。構成は [Battle/DataStore.md](./Battle/DataStore.md)。
- `MVCapture/`: MVCapture シーンで流すシナリオ。
- `Test/`: BT エディタの確認用アセット。

`Assets/Project/DeprecatedResources` には新規実装から依存しない。

## Asset Loading Rules

Addressables アドレスはアセットパスをそのまま使う。`Textures/` 配下は `AddressableAutoAddressAssigner` が自動登録する。

代表:

- DataStore: `Assets/Project/DataStore/{DataName}.asset`
- Character Still: `Assets/Project/Textures/Character/{Name}/Still/{Name}_Still.png`（発狂差分は `{Name}_Crazy_Still.png`）
- Character Face: `Assets/Project/Textures/Character/{Name}/Face/{Name}_{Expression}_Face.png`
- Title / StageList 背景: `Assets/Project/Textures/Images/BackGround/BackGround_{クリア数 + 1}.png`、`Assets/Project/Textures/Character/All/All_{クリア数 + 1}_Still.png`
- BGM: `Assets/Project/Textures/Sounds/Bgm/{SceneType}_{BgmType}`（手動で付けたアドレス）
- SE: `Assets/Project/Textures/Sounds/Se/{SeType}`
- Battle 背景と `BattleStageAsset`: `StageData` の `backgroundAddress` / `stageAddress`

`GamePath` が基準パスを提供し、Repository が ZString でアドレスを組み立てる。

## Input and Event Rules

Input System を直接各 Presenter が読むのではなく、`InputManager` が `MessageBroker` にイベントを流す。

代表的な購読:

- `TitleScenePresenter`: `UICancelMessage` で OptionModal を閉じる。
- `StageListScenePresenter` / `SoundRoomScenePresenter`: `UICancelMessage` で Title に戻る。
- `ScenarioScenePresenter`: `UISubmitMessage` で次メッセージへ進む・連打を検知する。
- `BattleScenePresenter`: `PlayerPauseMessage` で Pause を切り替える。
- `PlayerEntityPresenter`: `PlayerMoveMessage`, `PlayerAttackMessage`, `PlayerChargeMessage` で操作する。
- `TutorialModalPresenter` / `DemoClearView`: `UISubmitMessage` または `PlayerAttackMessage` で閉じる・進む。
- `VolumeGaugeSelectable`: `UICtrlMessage` で増減の刻みを切り替える。

UI のフォーカス移動と決定は EventSystem（`InputSystemUIInputModule`）と `ButtonBase` で処理する。
画面遷移の完了は `SceneNavigationMessage`、会話中のコマンドは `ScenarioCommandMessage` で通知する。

## Sound Rules

音声は `SoundManagerPresenter` に集約する。

BGM:

- `SceneType` と `BgmType` で選ぶ。ループ位置は `BgmData` のループサンプルで手動制御する。
- Title: `UserModel.TitleBgmType`（4 面クリア後は `Phase2`）。
- Battle: PlayBgm ノードが `BgmType` を指定し、SceneType はステージ番号から決まる（Stage1 = `SceneType.Stage1`）。切り替わり時に `BgmNameView` が曲名を表示する。
- チュートリアル表示中: `SceneType.Global` の `Tutorial`。
- SoundRoom: `BgmData` の各曲を直接再生する。

SE:

- `SeType` で選ぶ。単発 SE は `PlayOneShot`、チャージなどのループ SE は専用 AudioSource で手動ループする。

音量:

- `UserModel` の `bgmVolume` / `seVolume` を 0-100 で持ち、dB カーブで AudioSource の音量へ変換する。

## Save Data Rules

セーブデータは `UserModel` が `UserData` を JSON で読み書きする。

保存されるもの:

- クリア済みステージ番号とステージごとの最高クリアランク。
- キー設定の上書き。
- BGM / SE 音量、文字送り速度、連打スキップ設定、リトライ設定。
- Stage1 初回入場フラグ。

保存先:

- Editor: `Assets/Project/DataStore/UserData.json`
- ビルド: `Application.persistentDataPath/DataStore/UserData.json`

保存と復旧:

- 保存は一時ファイル `UserData.json.tmp` に書いてから本体と置き換え、直前の本体を `UserData.json.bak` に残す。
- 読み込みは既定値で初期化した `UserData` に JSON を上書きするため、欠損フィールドは既定値になる。その後 `Validate` で範囲に丸める。
- 本体が読めない場合は `UserData.json.corrupt` に退避し、`.bak` から復旧する。どちらも読めなければ初期データを使う。
- ファイルが無ければ初期データを作って保存する。

## Known Design Notes

- `ScenarioScenePresenter` は BattleScenePresenter を直接呼ばない。演出関数以外の行は `ScenarioCommandMessage` として通知し、ステージツリーの PlayScenario ノードが同名の OnCommand で処理する（ボス生成や BGM 切り替え）。
- 公開するステージの範囲は `UserData.Validate` の `clearedStageNumber` の上限（4）と `UserModel.IsStageConcealed`（5 面）で決まる。ステージを公開するときはこの 2 か所を変える。
- Battle からタイトルへ戻っても `RuntimeModel.ExitStage` は呼ばれず、`CurrentStageType` は残る。`ExitStage` を呼ぶのは `MVCaptureScenePresenter` だけである。
- Battle の進行は BehaviorTree で行う。アセット上のノードは雛形で、実行するたびに複製するため、同じ `BehaviorTreeAsset` を複数の敵・弾が同時に使っても状態は共有されない。
- BT の時間はフレーム基準（固定 60fps）、`MotionStep` の時間は秒指定である。単位に注意する。
- Spawn ノードで出す雑魚は `EnemyPool` で使い回す。敵を消すときは `Destroy` ではなく `EnemyDespawn.Despawn` を通す（[Battle/BehaviorTree.md](./Battle/BehaviorTree.md)）。
- `InputManager` は通常の C# クラスで、購読解除処理（`OnDestroy`）はどこからも呼ばれない。
- `MonoPresenter.Start` は Global が存在する前提である。Editor では Global が自動ロードされるが、それ以外の単体シーン起動では `soundManager` を取得できない。
- `UserModel.SetTextSpeed` と `KeyConfigModel` の割り当て変更 API を呼ぶ UI はまだ無い（`OptionModalView` のキー設定ボタンは購読されていない）。

## Where To Modify

目的別の編集場所:

- 新しいステージを追加する: `StageData.asset`（`stageAddress` / `backgroundAddress` / `charaStillAddress`）、`DataStore/StageN/BT`、Addressables 登録、`BattleStageType`、BGM 用の `SceneType` と `BgmData.asset`、公開範囲（`UserData.Validate` と `UserModel.IsStageConcealed`）。
- 道中・ボス・弾幕・敵の行動を作る: [Battle/BehaviorTree.md](./Battle/BehaviorTree.md) と [Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md) を参照し、BT エディタでツリーを編集する。
- ステージ固有の機能を作る: `Runtime/Stage/` にステージサービスを追加し、`BattleStageAsset.services` に宣言する（[Battle/BehaviorTree.md](./Battle/BehaviorTree.md) の「ステージサービス」）。
- 会話を追加・変更する: シナリオテキストを `Tools > Import Scenario` で `ScenarioData` にし、ステージツリーの PlayScenario ノードから参照する。
- 会話シーンの演出関数を追加する: `ScenarioData` の演出関数一覧、`ScenarioScenePresenter.ExecuteSteps`、`ScenarioView`。
- 会話中にバトル側で何かさせる: シナリオにコマンド行を書き、PlayScenario の下に同名の OnCommand を置く。
- 新しい UI モーダルを作る: `Commons/UI/Scripts/View`, `Commons/UI/Scripts/Presenter`, `GlobalScenePresenter` への参照追加。
- 新しいオプション項目を追加する: `UserData`（既定値と `Validate`）、`UserModel`、`OptionModalView` / `OptionModalPresenter`。
- 新しい入力を追加する: InputActionAsset, `InputManager`, `InputActionMessage.cs`, 購読側 Presenter を更新する。キー設定の対象にするなら `KeyConfigModel` も更新する。
- 新しい BGM/SE を追加する: `Textures/Sounds`, Addressables 登録, `BgmData.asset` / `SeData.asset`, `BgmType` / `SeType`。サウンドルームに出す曲は `BgmData` のサウンドルーム項目も設定する。

## Recommended Reading Order

初めて構造を追う場合は以下の順番がよい。

1. `README.md`: プロジェクト概要。
2. `Document/Directory-Structure.md`: 配置の全体像。
3. `Document/Document.md`: このファイル。
4. `EntryScenePresenter.cs`: 起動処理。
5. `GlobalScenePresenter.cs`: 常駐管理。
6. `SceneNavigator.cs`: シーン遷移。
7. `RuntimeModel.cs` と `UserModel.cs`: ゲーム進行状態とセーブデータ。
8. `StageListScenePresenter.cs`: ステージ選択から Battle への入口。
9. `BattleScenePresenter.cs`: バトル全体進行。
10. `Document/Battle/BehaviorTree.md`: バトル進行の仕組み。
11. `BattleFlow.cs` と `BattleWorld.cs`: BT の実行とフレームの進め方。
12. `ScenarioScenePresenter.cs`: 会話の進行とコマンドの通知。

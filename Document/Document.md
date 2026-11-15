# Magic-science-world Project Document

このドキュメントは、他の AI や新規参加者がこのファイルを読むだけでプロジェクト全体の構造を把握できることを目的にしたリファレンスである。
関数単位の細かい説明ではなく、各ディレクトリ、シーン、主要クラス、データアセットが「何を担当できるか」を中心に説明する。

## Project Summary

Magic-science-world は、魔法と科学が融合した世界を舞台にした Unity 2D ゲームプロジェクトである。
画面構成はタイトル、ステージ選択、バトル、シナリオ、デモクリア、常駐グローバル管理シーンで構成される。

バトルは `Battle` シーンに統合されており、道中とボスを別シーンに分けず、`RuntimeModel` の `CurrentSituation` によって `Way` と `Boss` を切り替える。
バトル進行は ScriptableObject の `BattleStageAsset` と、それが参照する BehaviorTree（`BehaviorTreeAsset`）を中心にデータ駆動で構築される。BehaviorTree は固定 60fps のフレームで実行され、道中の敵出現と敵・ボスの行動を記述する。

## Technology Stack

- Unity: `6000.0.59f2`
- Render Pipeline: Universal Render Pipeline `17.0.4`
- Asset Loading: Addressables `2.7.3`
- Input: Unity Input System `1.14.2`
- Timeline: Unity Timeline `1.8.9`（manifest に残っているが、旧バトル方式の削除後はどのコードからも使われていない）
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

- `RuntimeModel`: 現在ステージと `Way/Boss` 状態を持つ。
- `StageModel`: ステージの開放/クリア状態、バトルシーケンス参照を持つ。
- `UserModel`: セーブデータ、音量、チュートリアル初回状態を管理する。
- `BattleStageAsset` / `BehaviorTreeAsset`: バトル進行（道中・ボス戦・敵の行動）のデータ。実行は `Scenes/Battle/Scripts/Runtime` の `BattleFlow` / `BattleWorld` が行う。
- `EntityBase` 派生: プレイヤー、敵、ボス、弾の HP/死亡/衝突を扱う。

### View

View は画面表示、UI、Sprite、Animation、Text、Canvas など Unity コンポーネントの操作を持つ。
原則として `MonoBehaviour` を継承する。
入力イベントの公開や表示更新は行うが、進行ロジックやデータロードは Presenter/Model/Repository に寄せる。

代表例:

- `TitleMenuView`: タイトルメニュー表示とボタンイベントを提供する。
- `ScenarioView`: メッセージ、立ち絵、表情、ログを表示する。
- `PlayerEntityView`: プレイヤーの位置、アニメーション、HP、点滅を表示する。
- `BossEntityView`: ボスの位置、HP ゲージ、被弾表示を扱う。

### Presenter

Presenter は Model と View を接続し、シーン進行や入力購読を持つ。
Unity 上で View を `SerializeField` 参照する必要があるため、`MonoBehaviour` を継承する。
共通 Presenter は `MonoPresenter` を継承し、`GlobalScenePresenter`、`SoundManagerPresenter`、共通 Repository にアクセスする。

代表例:

- `TitleScenePresenter`: タイトル画面の操作と遷移を制御する。
- `StageListScenePresenter`: ステージ選択、RuntimeModel 更新、Battle 遷移を制御する。
- `BattleScenePresenter`: バトル全体の開始、リトライ、シナリオ遷移、ボス生成を制御する。
- `ScenarioScenePresenter`: シナリオコマンドを解釈し、表示・Battle 操作・RuntimeModel 更新を行う。

### Repository

Repository は Model や Asset の生成、ロード、キャッシュを担当する。
Addressables、ScriptableObject、AudioClip、Sprite、InputActionAsset など外部データへのアクセスを集約する。
多くの Repository はシングルトンとして `Instance` を持つ。

代表例:

- `StageModelRepository`: `StageData.asset` から `StageModel` を生成する。
- `SoundModelRepository`: `BgmData.asset` / `SeData.asset` から音声 Model を生成する。
- `StillAssetRepository`: キャラクタースチル画像を Addressables から読む。

### Infra

Infra は永続データと ScriptableObject データ構造の定義を持つ。
ゲームロジックではなく、データベースとして保存される形を表す。

代表例:

- `StageDataObject`: ステージ番号、タイトル、キャラ、道中/ボスの `BattleStageAsset` アドレスを定義する。
- `BgmDataObject`: BGM 種別とループ位置を定義する。
- `UserData`: セーブデータの JSON 形式を定義する。

## Runtime Flow

### Boot

起動時は `Entry.unity` から始まる。

1. `EntryScenePresenter` が Addressables を初期化する。
2. `Global.unity` を Additive でロードする。
3. `Title.unity` を Additive でロードする。
4. Active Scene を Title にする。
5. Entry シーンをアンロードする。

`Global` は常駐シーンとして扱われ、画面遷移後も残る。

### Global

`GlobalScenePresenter` は常駐管理の中心である。

担当:

- 共通モーダルの保持: Option, Pause, GameOver, Tutorial。
- `SoundManagerPresenter` の保持。
- `InputActionAsset` を `KeyConfigModelRepository` に渡す。
- EventSystem の `InputSystemUIInputModule` に InputActionAsset を設定する。
- `InputManager` を生成し、Input System の入力を UniRx `MessageBroker` へ変換する。
- `SceneNavigator` を公開し、各 Scene Presenter から画面遷移できるようにする。

`GlobalScenePresenter` は `MonoPresenter` を継承しない。
理由は、`MonoPresenter` が `GlobalScenePresenter` を検索するため、Global 自身が継承すると依存が循環しやすいからである。

### Scene Navigation

画面遷移は `SceneNavigator` が担当する。

流れ:

1. `SceneNavigationMessage(InProgress, toSceneName)` を publish する。
2. 移動先シーンを Additive でロードする。
3. 移動先を Active Scene にする。
4. 移動元シーンをアンロードする。
5. `SceneNavigationMessage(Completed, toSceneName)` を publish する。

`BattleScenePresenter` や `PlayerEntityPresenter` はこの Completed メッセージを受けて初期化タイミングを合わせる。

### Title to Battle

通常の流れ:

```text
Title
  -> StageList
  -> RuntimeModel.CurrentStageType = selected stage
  -> RuntimeModel.CurrentSituation = Way
  -> Battle
```

Title は StageList へ遷移する。
StageList は選択した `StageModel` からステージ番号を取得し、`RuntimeModel` を更新して Battle へ遷移する。
現在のデモ実装では StageList から選択できるバトルは Stage1 のみに制限されている。

### Battle to Scenario

Battle の道中ツリーが完了する、またはボスの死亡演出が完了すると、`BattleScenePresenter` は `Scenario.unity` を Additive ロードする。
Scenario は `ScenarioModelRepository` を通じて `RuntimeModel` から読むべきシナリオ番号を決定する。

Scenario 完了時:

1. `ScenarioScenePresenter` が `RuntimeModel.AdvanceToNextSequence()` を呼ぶ。
2. `Way` なら `Boss` へ進む。
3. `Boss` なら次ステージの `Way` へ進む。
4. `ScenarioScenePresenter.OnScenarioCompleted` が発火する。
5. Battle 側のコールバックが実行される。
6. Scenario シーンをアンロードする。

デモ版ではボス終了後に `DemoClear.unity` をロードする流れがある。

## Core Shared Files

### Assets/Project/Scripts/Presenter

#### `MonoPresenter.cs`

共通 Presenter 基底。
`Start` で `GlobalScenePresenter` を検索し、`SoundManagerPresenter` を取得する。
`RuntimeModelRepository` と `StageModelRepository` への protected アクセスを提供する。
各 Scene Presenter や Entity Presenter はこれを継承することで、サウンド再生、RuntimeModel、StageModel へアクセスしやすくなる。

注意点:

- `GlobalScenePresenter` が見つからない場合は警告を出す。
- `soundManager = globalScenePresenter!.SoundManagerPresenter` という前提があり、Global が未ロードの状態で使うと壊れる可能性がある。

#### `GameBootStrapper.cs`

ゲーム起動補助用の Presenter。
現状では共通起動処理を置くための拡張ポイントとして扱う。

### Assets/Project/Scripts/Model

#### `ModelBase.cs`

共通 Model 基底。
`UserModel` と `RuntimeModel` の参照を持つ。
各 Model にユーザー進行状態や実行時状態を注入するための共通形として使われる。

#### `AddressablesModel.cs`

Addressables の初期化処理を Model として表す。
`EntryScenePresenter` から起動時に使われる。

#### `RuntimeModel.cs`

実行中のゲーム状態を持つ中心 Model。

保持する状態:

- `CurrentStageType`: 現在のステージ。
- `CurrentSituation`: `Way` または `Boss`。
- `IsInGame`: ステージ中かどうか。

できること:

- 現在のステージと状況からシナリオ番号を計算する。
- `Way -> Boss`、`Boss -> 次ステージ Way` へ進める。
- ステージ外状態へ戻す。
- Editor/Development build でデバッグ用状態をセットする。

#### `SceneRouterModel.cs`

シーン名定数を持つ。
`Global`, `Title`, `StageList`, `Scenario`, `Battle`, `DemoClear` の文字列を集約する。
`SceneNavigator` や手動 SceneManager 操作で使われる。

#### `StageModel.cs`

ステージ単位の状態とデータを持つ Model。

保持するもの:

- `StageData`
- ステージ番号。
- `BattleStageType`
- `BattleStageAsset` のアドレス（`StageAddress`）。
- ステージカード用キャラスチル。
- 開放状態。
- クリア状態。

できること:

- ステージ開始時に `RuntimeModel.CurrentStageType` を更新する。
- ステージを開放状態にする。
- ステージクリアを `UserModel` へ保存する。
- ステージカード表示用の ID とタイトルを返す。

#### `UserModel.cs`

ユーザーセーブデータを読み書きする Model。

保持/管理するもの:

- `UserData`
- クリア済みステージ番号。
- BGM/SE 音量。
- Stage1 初回入場済みフラグ。

保存先:

- Editor: `Assets/Project/DataStore/UserData.json`
- Runtime: `Application.persistentDataPath/DataStore/UserData.json`

できること:

- セーブデータがなければ初期データを作成する。
- JSON を読み込み、欠損フィールドを既定値で補完する。
- ステージクリアを保存する。
- 音量値を 0-100 に丸めて保持する。
- Stage1 初回チュートリアル用フラグを保存する。

#### `CharacterModel.cs`

キャラクターデータを表す Model。
`CharacterData` をラップし、キャラクター ID、名前、英名などを扱う。

#### `BgmModel.cs`

BGM データと AudioClip を表す Model。
`BgmData` のシーン種別、BGM 種別、ループ開始/終了サンプル、ロード済み AudioClip を扱う。

#### `SeModel.cs`

SE データと AudioClip を表す Model。
`SeData` の SE 種別、ループ開始/終了サンプル、ロード済み AudioClip を扱う。

#### `KeyConfigModel.cs`

InputActionAsset とキー設定を扱う Model。
`GlobalScenePresenter` から渡された InputActionAsset を保持し、UI と Player 入力に使う ActionAsset を提供する。

### Assets/Project/Scripts/Infra

#### `StageDataObject.cs`

ステージ一覧データベース。
`StageDataObject` は `List<StageData>` を持つ ScriptableObject。

`StageData` が定義するもの:

- `id`: 表示/識別用 ID。
- `stageNumber`: int のステージ番号。
- `charaStillAddress`: ステージカード用キャラクタースチル参照名。
- `title`: ステージタイトル。
- `stageAddress`: `BattleStageAsset`（道中・会話・ボス戦を含む 1 ステージ分）の Addressables アドレス（アセットパス）。

#### `CharacterDataObject.cs`

キャラクター一覧データベース。
`CharacterData` はキャラクター ID、日本語名、英名を持つ。

#### `BgmDataObject.cs`

BGM データベース。
`BgmData` は名前、対象 SceneType、BgmType、ループ開始/終了サンプルを持つ。
`SoundManagerPresenter` はこのループ位置を使って AudioSource の `timeSamples` を戻し、手動ループする。

#### `SeDataObject.cs`

SE データベース。
`SeData` は名前、SeType、ループ開始/終了サンプルを持つ。
通常 SE とループ SE の両方に使われる。

#### `KeyConfigData.cs`

キー設定保存用データ。
`UserData` に含まれるが、現在の入力は `InputActionAsset` を中心に扱われる。

#### `UserData.cs`

セーブデータの JSON 形式。
保持するもの:

- `clearedStageNumber`
- `keyConfigData`
- `bgmVolume`
- `seVolume`
- `hasEnteredStage1`

`Validate` により範囲外の数値を丸める。
現状は `clearedStageNumber` が 0-1 に制限されており、デモ版の進行範囲に合わせた実装である。

### Assets/Project/Scripts/Repository

#### `ModelRepositoryBase.cs`

ModelRepository の共通基底。
`dataName` から `Assets/Project/DataStore/{dataName}.asset` の Addressables アドレスを生成する。
`LoadDataObject<T>` で ScriptableObject データを同期ロードする。
`UserModel` への protected アクセスも持つ。

#### `RuntimeModelRepository.cs`

`RuntimeModel` のシングルトンを提供する。
ゲーム進行中の現在ステージ/Way/Boss 状態はここから取得される。

#### `UserModelRepository.cs`

`UserModel` のシングルトンを提供する。
生成時に `RuntimeModel` を注入する。

#### `StageModelRepository.cs`

`StageData.asset` から全 `StageModel` を生成し、ユーザーの開放/クリア状態を反映する。
ステージ番号による取得と全ステージ一覧取得を提供する。
各 `StageModel` には `UserModel` と `RuntimeModel` が注入される。

#### `SoundModelRepository.cs`

`BgmData.asset` と `SeData.asset` から `BgmModel` / `SeModel` を生成する。
BGM はシーン種別と BGM 種別で検索し、SE は SE 種別で検索する。
BGM Model は必要に応じて Refresh できるが、SE は頻繁に使う想定で保持される。

#### `CharacterModelRepository.cs`

`CharacterData.asset` から `CharacterModel` を生成する。
キャラクター ID/名前/英名をゲームロジックから扱うための入口になる。

#### `KeyConfigModelRepository.cs`

`GlobalScenePresenter` から InputActionAsset を受け取り、`KeyConfigModel` を生成・保持する。
UI EventSystem の InputModule と `InputManager` はここで扱う ActionAsset を使う。

#### `AssetRepositoryBase.cs`

AssetRepository の共通基底。
具象 Repository の型を揃えるための基底で、Sprite/AudioClip などのロードは派生クラスにある。

#### `AddressablesInitializeAssetRepository.cs`

Addressables 初期化を担当する。
`AddressablesModel` 経由で起動時に使われる。

#### `StillAssetRepository.cs`

キャラクターのスチル画像を Addressables から同期ロードする。
パスは `Assets/Project/Textures/Character/{Name}/Still/{Name}_Still.png` 形式を組み立てる。
Crazy 差分にも対応する。

#### `FaceAssetRepository.cs`

キャラクターの表情画像群を Addressables から読み込み、表情名を key とする Dictionary に変換する。
Scenario で立ち絵の表情差分を切り替えるために使われる。

#### `AllCharacterStillAssetRepository.cs`

複数キャラクターのスチル画像をまとめて読み込む。
タイトルや一覧表示など、複数画像をまとめて扱う用途の Repository。

#### `BackGroundAssetRepository.cs`

背景 Sprite を Addressables から読み込む。
Title や StageList などの背景表示 Model で使われる。

#### `SoundAssetRepository.cs`

BGM/SE の AudioClip を Addressables から読み込む。
`BgmModel` / `SeModel` の AudioClip 解決で使われる。

### Assets/Project/Scripts/Extensions

#### `SceneType.cs`

ゲーム内シーン種別 enum。
`Stage1` 以降の StageN は実シーン名としてはすべて `Battle` に変換される。
BGM データ検索にも使われる。

#### `BattleStageType.cs`

バトル対象ステージの enum。
int のステージ番号との変換、次ステージへの進行、Null 状態を表す。

#### `BgmType.cs`

BGM 種別 enum。
Title、BattleWay、BattleBoss、Tutorial などの用途を区別する。

#### `SeType.cs`

SE 種別 enum。
クリック、キャンセル、攻撃、チャージ、被弾などの効果音を区別する。

#### `GamePath.cs`

Addressables の基準パス定数。
主に `Assets/Project/Textures` と `Assets/Project/DataStore` を表す。

#### `InputManager.cs`

Unity Input System の `InputActionAsset` を読み、各 Action のイベントを UniRx `MessageBroker` に publish する。
プレイヤー入力と UI 入力の両方を扱う。

できること:

- Move/Look/Attack/Jump/Interact/Crouch/Sprint/Charge/Pause を Player 系メッセージへ変換する。
- Navigate/Submit/Cancel/Point/Click/ScrollWheel を UI 系メッセージへ変換する。
- InputActionAsset を Enable する。

注意点:

- `OnDestroy` は通常の C# クラスでは Unity から自動呼び出しされないため、購読解除の設計には注意が必要。

#### `InputActionMessage.cs`

`InputManager` が publish するメッセージ型群。
Presenter は直接 Input System を読む代わりに、これらを `MessageBroker.Default.Receive<T>()` で購読する。

代表:

- `PlayerMoveMessage`
- `PlayerAttackMessage`
- `PlayerChargeMessage`
- `PlayerPauseMessage`
- `UINavigateMessage`
- `UISubmitMessage`
- `UICancelMessage`

#### `SceneNavigationMessage.cs`

画面遷移状態のメッセージ。
`SceneNavigator` が `InProgress` と `Completed` を publish し、Battle などが初期化タイミングに使う。

#### `ObservableExtensions.cs`

UniRx の購読補助。
ブロッキングや連打防止系の購読パターンを共通化する。

#### `BoolExtensions.cs`, `StringExtensions.cs`

小さな型拡張。
可読性や変換処理の補助として使われる。

#### `ReadOnlyAttribute.cs`

Inspector 上で読み取り専用表示を行うための属性。

## Scene Details

## Entry Scene

### `EntryScenePresenter.cs`

起動専用 Presenter。
ゲーム開始時に Addressables 初期化、Global ロード、Title ロード、Entry アンロードを順番に行う。
このシーンは通常プレイヤーに見せる画面ではなく、ブートストラップ用である。

## Global Scene

### `GlobalScenePresenter.cs`

常駐する全体管理 Presenter。

保持するもの:

- `OptionModalPresenter`
- `PauseModalPresenter`
- `GameOverModalPresenter`
- `TutorialModalPresenter`
- `SoundManagerPresenter`
- `InputActionAsset`
- `SceneNavigator`
- `KeyConfigModel`
- `InputManager`

責務:

- InputActionAsset の未設定を検出する。
- `KeyConfigModelRepository` を初期化する。
- EventSystem の `InputSystemUIInputModule` に ActionAsset を設定する。
- `InputManager` を生成し、入力イベントを `MessageBroker` に流す。
- 他 Presenter から共通モーダルや SoundManager にアクセスできるようにする。

### `SceneNavigator.cs`

画面遷移専用クラス。
Additive ロードと旧シーンアンロードをセットで行う。
遷移中/遷移完了メッセージを publish するため、遷移完了を待って初期化する Presenter と相性が良い。

### `SoundManagerPresenter.cs`

BGM/SE 再生を扱う常駐 Presenter。

できること:

- シーン種別と BGM 種別から BGM を再生する。
- 同じ BGM が再生中ならスキップできる。
- BGM を停止する。
- SE を単発再生する。
- ループ SE を手動ループで再生/停止する。
- ユーザー音量を AudioSource 音量へ変換して反映する。
- BGM とループ SE のループ位置を `timeSamples` で制御する。

音量変換は線形ではなく dB カーブを使い、設定値 0-100 を最大 0.5 の AudioSource volume に変換する。

## Title Scene

### `TitleModel.cs`

タイトル画面用 Model。
背景 Sprite などタイトル表示に必要なデータを持つ。

### `TitleModelRepository.cs`

`TitleModel` を生成・保持する Repository。
Title シーン以外で不要になった Model を Refresh できる。

### `TitleMenuView.cs`

タイトルメニュー View。
背景表示、Start/Option/Exit などのボタン表示、選択状態、押下イベントを提供する。

### `TitleScenePresenter.cs`

タイトル画面の中心 Presenter。

責務:

- `TitleModelRepository` から `TitleModel` を取得する。
- `TitleMenuView` に背景 Sprite を渡す。
- Start 押下で StageList へ遷移する。
- Option 押下で共通 OptionModal を開く。
- Exit 押下で Editor 再生停止または Application.Quit を行う。
- Title BGM を再生する。

現在 `StartMain` という Battle 直行用の処理もあるが、通常導線では StageList へ遷移する。

## StageList Scene

### `StageListModel.cs`

ステージ選択画面用 Model。
背景 Sprite など StageList 表示に必要なデータを持つ。

### `StageListModelRepository.cs`

`StageListModel` を生成・保持する Repository。

### `StageCardView.cs`

個別ステージカードの View。
ステージ ID/タイトル、開放状態、選択表示などを扱う。

### `StageCardListView.cs`

ステージカード一覧の View。
カードリストの初期化、選択移動、決定イベント、キャラクター画像表示を扱う。

### `StageListSceneView.cs`

StageList シーン全体 View。
背景 Sprite など、カードリスト以外の画面表示を扱う。

### `StageListScenePresenter.cs`

ステージ選択の中心 Presenter。

責務:

- `StageModelRepository.GetAll()` から全ステージ Model を取得する。
- 開放状態を `StageCardListView` に反映する。
- 各 StageCard に ID/タイトル/開放状態を設定する。
- 選択中ステージのキャラスチルとクリア状態を表示する。
- 決定時に `RuntimeModel.CurrentStageType` と `CurrentSituation` をセットする。
- Battle シーンへ遷移する。
- Cancel 入力で Title に戻る。

現在はデモ都合で `buttonIndex != 0` のステージは Battle 遷移しない。

## Scenario Scene

### `ScenarioData.cs`

シナリオデータ構造。
シナリオは複数の `ScenarioStep` で構成され、各 Step は `function` と `args` を持つ。
`function` は `ShowMessage`, `ShowCast`, `SpawnBoss` などのコマンド名として扱われる。

### `ScenarioModel.cs`

シナリオ進行 Model。

保持するもの:

- シナリオステップ一覧。
- 現在のステップ index。
- プレイヤー/敵のスチル Sprite。
- プレイヤー/敵の表情 Sprite Dictionary。

できること:

- シナリオステップをロードする。
- 次のステップへ進む。
- 現在ステップと終了判定を提供する。
- キャラクター名からスチルと表情差分を読み込む。

### `ScenarioModelRepository.cs`

ScenarioModel を生成する Repository。
`RuntimeModel.GetScenarioNumber()` を使い、現在ステージと `Way/Boss` から読むシナリオを決める。
シナリオデータとキャラ画像を `ScenarioModel` に読み込ませる。

### `ScenarioView.cs`

シナリオ表示 View。

できること:

- コマンドログを表示する。
- キャラ名つきメッセージを表示する。
- 通常メッセージを表示する。
- 立ち絵、スチル、表情差分を表示する。
- キャラをフェードアウトする。
- 表情差分を変更する。

### `ScenarioScenePresenter.cs`

シナリオ進行の中心 Presenter。

責務:

- `ScenarioModelRepository` から ScenarioModel を取得する。
- `UISubmitMessage` で次ステップへ進める。
- Pause 中はシナリオ送りを止める。
- Step の `function` を解釈し、View または Battle に反映する。
- メッセージ表示系コマンドでは入力待ちにする。
- 表示だけで待たないコマンドでは即座に次 Step へ進める。
- `SpawnBoss` で BattleScenePresenter にボス生成を依頼する。
- `PlayBossBgm` で BattleScenePresenter にボス BGM 再生を依頼する。
- シナリオ終了時に `RuntimeModel.AdvanceToNextSequence()` を実行する。
- 完了通知 `OnScenarioCompleted` を発火し、Scenario シーンをアンロードする。

多重完了を防ぐ `isCompleted` ガードがあり、決定入力の連打で RuntimeModel が複数回進む事故を防ぐ。

## DemoClear Scene

### `DemoClearView.cs`

デモクリア画面 View。
表示開始と任意キー入力イベントを提供する。

### `DemoClearScenePresenter.cs`

デモクリア画面 Presenter。
任意キー入力を 1 回だけ受け取り、Title を Additive ロードし、Battle と DemoClear をアンロードする。

## Battle Scene Overview

Battle はこのプロジェクトで最も大きい領域である。
バトルの進行（道中の敵出現、ボスの攻撃・移動、フェーズ切り替え）は BehaviorTree（BT）で記述する。
基本的に次の層で構成される。

```text
StageData
  -> StageModel
  -> StageAddress (Addressables アドレス = アセットパス)
  -> BattleStageAsset (1 ステージ = 道中・会話・ボス戦)
       services[]  -> StageContext (ステージサービス: 風など)
       tree        -> BehaviorTreeRunner (ステージツリー。Section "Way" / "Boss")
                       Spawn     -> BattleActor (雑魚に付与。通常は共通ツリー Commons/BT/Enemy)
                       SpawnBoss -> BossDefinition.tree -> BattleActor (ボスに付与し、ボス自身がツリーを実行)
  -> BattleFlow / BattleWorld (固定 60fps のフレームで全ツリーと弾を進める)
```

詳細は次のドキュメントを参照すること。

- [Battle/BehaviorTree.md](./Battle/BehaviorTree.md): BT 方式の全体構造、実行順、ステージの流れ、ステージサービス、エディタ、設計判断。
- [Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md): 各ノード・条件・MotionStep のフィールドと戻り値、よく使う組み方。
- [Battle/DataStore.md](./Battle/DataStore.md): `DataStore/Commons/BT/` と `DataStore/Stage{N}/BT/` のデータ構成。

旧方式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset` / `AttackPreset` / `MovementPreset` による Unity Timeline 駆動）のコードとデータは削除済みである。

### Battle Runtime Flow

1. `BattleScenePresenter` が `SceneNavigationMessage.Completed(Battle)` を受けて開始する。
2. 必要なら TutorialModal を表示し、閉じてから開始する。
3. `RuntimeModel.CurrentStageType` から `StageModel` を解決し、`StageModel.StageAddress` の `BattleStageAsset` を Addressables で同期ロードする。
4. `BattleFlow.StartStage` がステージサービスを有効にし、弾と敵を事前生成してステージツリーを開始する。
5. 道中・会話・ボスの出現と戦闘・撃破後の会話は、すべてステージツリー（Section "Way" / "Boss"）が進める。Presenter は `BattleFlow` のイベントと `IScenarioService` の呼び出しに演出で応える。
6. ツリーが Success で完了したら `CompleteStage` で DemoClear を表示する。

### `BattleScenePresenter.cs`

Battle 全体の中心 Presenter。

保持するもの:

- `BattleFlow`（と、同じ GameObject の `BattleClock`）
- `EnemyTracker`
- `BackgroundPresenter`
- `BulletClearReceiver`
- `StageModel`
- `BattleStageAsset`
- Player/Boss Presenter 参照
- Scenario 完了コールバック
- Retry/Return 用の購読や CancellationToken

できること:

- Battle 開始タイミングを SceneNavigationMessage で待つ。
- チュートリアル表示後に初期化を続ける。
- Player 死亡時に GameOverModal を開く。
- Pause 入力で PauseModal を開閉する。
- GameOver/Pause から Retry/Title 戻りを処理する。
- StageModel から `BattleStageAsset` をロードし、`BattleFlow.StartStage` でステージツリーを開始する。
- `IScenarioService` として、Scenario ノードからの依頼で会話シーンを開き、会話中のコマンドをツリーへ渡す。
- PlayBgm ノードからの依頼で BGM を再生し、曲名を表示する。
- `BattleFlow` のイベント（Section 開始・ボス出現・ボス戦開始）に応じて背景・自機アニメ・ボスの当たり判定を切り替える。
- ステージツリーの完了で DemoClear シーンを表示する。
- OnDestroy で購読、CTS、Scenario ハンドラを解除し、`BattleFlow` を停止する。

重要な注意:

- `StartBattle` は通常の Unity `Start` ではなく、SceneNavigation 完了後に呼ばれる。
- Retry 時には `BattleFlow.Stop()`（全ツリー・予約・弾・ステージサービスの破棄）、Scenario ロード待ち、購読、Boss、弾、敵を解除してから開始し直す。
- ボスの発狂（Strong）は、ボスツリー内の `NormalHpDepleted` 条件と `EnterStrongMode` ノードで切り替える。

### BattleFlow / BattleWorld / BattleClock

`Assets/Project/Scenes/Battle/Scripts/Runtime/` 配下。BT 実行の土台である。

- `BattleFlow.cs`: バトル 1 回分の BT 実行を管理する。`StartStage`（ステージサービスの開始、弾・敵の事前生成、ステージツリーの開始）と `Stop` を提供し、`IBossHost`（ボスの生成 `Spawn` と戦闘開始 `StartBattle`）を実装する。自機に `BulletSystem` を渡す（`BindBulletSystem`）。
- `BattleWorld.cs`: 全ツリーで共有するフレーム番号・乱数・予約実行 (`Schedule`)・`BattleServices`・`StageContext` を持ち、`Step()` で 1 フレーム進める。
- `BattleClock.cs`: `Update` で `Time.deltaTime` を蓄積し、固定 60fps で `BattleWorld.Step()` を呼ぶ（1 描画フレーム最大 4 ステップ、ポーズ中は停止）。
- `BattleActor.cs`: 敵/ボスの GameObject に付けるコンポーネント。自分のツリーと、ツリーと独立した移動（SetMovement）を毎フレーム進める。
- `ActorSpawner.cs`: Spawn ノードから敵・ビーム・障害物 Prefab を生成し、ツリー指定時は `BattleActor` を付ける。プール対象の敵は `ActorPool` から出す。
- `ActorPool.cs` / `PooledActor.cs` / `NoActorPool.cs`: 敵を Prefab ごとに使い回すプール。敵を消すときの共通の入口 `ActorDespawn.Despawn`（プール品はプールへ戻し、それ以外は Destroy）と、出し入れ時の状態を戻す `IPoolableActor` も `ActorPool.cs` にある。`NoActorPool` を付けた Prefab はプールしない。
- `StagePrefabCollector.cs`: `BattleStageAsset` からシリアライズされた参照をたどり、事前生成する弾 Prefab と出現物 Prefab を集める（Prefab の中身はたどらない）。
- `BulletSystem.cs`: 敵弾・自機弾を固定フレームで動かす。弾の移動は BT の外で、`MotionStep` に従う。外力（風）の受け口 `IBulletForce` を持つ。
- `Stage/StageContext.cs`, `Stage/WindService.cs`, `Stage/ExternalForces.cs`: ステージ固有機能（ステージサービス）の仕組みと、その実装例である風、自機・弾への汎用の受け口。
- `Motion/`: 敵・ボス・弾で共通の移動定義（`MotionStep` / `MotionPreset` / `EaseCurveAsset`）と実行器。自機の Transform は `BattleTargets.Player`（`MotionAssets.cs`）が持つ。

### `BackgroundPresenter.cs` / `BackgroundView.cs`

Battle 背景を管理する。
背景スクロール、初期化、ボス突入時の減速、Retry 時のリセットなどを担う。
WindService が風向き矢印をこの子として生成する。

### `BattleWallPresenter.cs`

Battle 画面内の壁や境界に関する Presenter。
画面端や当たり判定補助など、Battle 空間を構成するオブジェクトを制御する。

### `BulletClearReceiver.cs`

画面上の弾や敵をまとめて消す処理を持つ。敵は `ActorDespawn.Despawn` で消す。
BT の `ClearBullets` ノードと、Retry 時の `BattleScenePresenter` から呼ばれる。

### `EnemyTracker.cs`

生存中の敵数を監視する。
敵登録時に activeCount を増やし、`EnemyEntityPresenter.OnRemoved`（撃破・画面外・寿命・プールへの返却・破棄）で減らす。プールで使い回す敵は破棄されないため、破棄通知では数えない。
Spawn ノードの `tracked` が ON の敵が登録され、`NoActiveEnemies` 条件で使われる。

### `WindArrowsPresenter.cs`

風向きを示す矢印グリッドを流す Presenter。
Stage3 の `WindServiceDefinition` に設定された Prefab から WindService が生成し、`Bind` で WindService を受け取る。風が無効・無風の間は非表示。

### `ScreenBoundsCache.cs`

Battle シーンの画面境界を計算してキャッシュする。
Player、Enemy、Bullet、画面端を使う移動・発射方向が画面内制限や画面外消去に使う。

## Battle BehaviorTree

`Assets/Project/Scenes/Battle/Scripts/BehaviorTree/` 配下。詳細は [Battle/BehaviorTree.md](./Battle/BehaviorTree.md) と [Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md)。

### Core

- `BattleStageAsset.cs`: 1 ステージ（道中・会話・ボス戦）全体の定義。`services`（ステージサービス）と `tree`（ステージツリー）を持つ。
- `BehaviorTreeAsset.cs`: ツリー 1 本の ScriptableObject。`root`、`detachedNodes`、`parameters` を持つ。
- `NodeDefinition.cs`: アセットに保存する不変のノード定義（Composite / Decorator / Action の基底）。実行時は `Create(scope)` で `Node` を毎回生成する。
- `Node.cs`: 実行時ノードの基底。`OnEnter` / `OnTick` / `OnAbort` / `OnExit` の呼び出し順を保証する。
- `BuildScope.cs` / `TreeParameter.cs`: ツリーパラメータの解決（`BtInt` / `BtFloat` などの `$param` 参照、SubTree からの上書き）と SubTree の循環検出。
- `BattleContext.cs`: ツリー 1 本の実行文脈（`World` と、ツリーを持つアクター `Self`）。
- `BehaviorTreeRunner.cs`: ツリー 1 本を毎フレーム Tick し、完了を通知する。

### Nodes

- `Composite/`: Sequence、Selector、Parallel（All / Any / Duration。Duration は指定フレームで打ち切る）、Random（重み・連続回避）。
- `Decorator/`: Loop、Repeat（一定間隔で重ねて開始）、Until（条件成立で子を中断。Finish / Restart / Hold）、Delay、Inverter、ForceSuccess。
- `Condition/Condition.cs`: Constant、FrameElapsed、BgmReached、And / Or / Not と、CheckCondition / WaitUntil ノード。
- `Action/BasicActions.cs`: Wait、Log、SubTree と、`*Param` 欄から Object パラメータを引く `ParamObjects`。
- `Battle/`: 発射（Fire と ShotPattern / DirectionSource / RotationSource / BulletSpec、BulletBeam、SpriteBeam、MoveTrailFan、PlaceAlongPath）、アクター（Move、MovePath、SetMovement、SetDeathBehavior、SetInvincible、Despawn、PlaySe、Spawn）、ステージ（NoActiveEnemies / HpBelow / NormalHpDepleted / BgmSamples 条件、EnterStrongMode、ClearBullets、WithWind、BossWindCue、PhaseMarker、WithPrefab）、進行（Stage、Section、Wave、PlayBgm、SpawnBoss、BossBattle、Scenario、OnCommand）。

### 設計方針

- BT はステージ層（道中・会話・ボス戦の進行）とアクター層（敵・ボスの行動）に適用する。弾は BT の外の `BulletSystem` が動かす。
- 時間はフレーム基準（60fps 固定）。`MotionStep` の時間だけは旧データとの互換で秒指定。
- 割り込みは Until で表す。Blackboard は持たず、値の共通化はツリーパラメータで行う。
- 攻撃パターンや敵の行動は `BehaviorTreeAsset` として切り出し、SubTree / Spawn で参照して再利用する。雑魚は共通ツリー `Commons/BT/Enemy` に移動・攻撃・撃破時の行動をパラメータで渡す。
- ボスと通常敵は同じ `BattleActor` と同じノード群で解釈する。
- ステージ固有機能は `BattleStageAsset.services` に宣言するステージサービスとしてカプセル化し、自機・弾は汎用の受け口だけを持つ。
- 旧実装の「Ease 未指定時は OutQuad」の挙動は `StepClock` で再現している。

## Battle Entity Models

### `EntityBase.cs`

Battle Entity の純粋 Model 基底。

保持するもの:

- Max HP
- Current HP ReactiveProperty
- 死亡 Subject

できること:

- ダメージを受ける。
- HP 0 で死亡通知を発火する。
- 衝突時の抽象処理を派生へ委ねる。
- Player かどうかを派生へ委ねる。

### `PlayerEntityModel.cs`

プレイヤーの Model。

扱うもの:

- HP
- チャージ閾値
- スニーク状態
- スニーク時移動速度倍率
- 無敵時間
- チャージ完了状態
- 被弾後の無敵

できること:

- 移動/攻撃そのものは Presenter が行い、Model はチャージや被弾状態を管理する。
- 衝突相手に応じてダメージを受ける。
- Retry 時に状態を初期化する。

### `EnemyEntityModel.cs`

通常敵の Model。
接触ダメージを持ち、プレイヤーや弾との衝突時に HP や相手へ影響を与える。

### `BossEntityModel.cs`

ボスの Model。

扱うもの:

- 通常 HP と強化 HP。
- 強化攻撃に入る HP 比率。
- オーバーフローダメージ倍率。
- 強化攻撃を使うべきかどうか。

ボスツリーの `NormalHpDepleted` 条件が `ShouldUseStrongAttack`（通常 HP が 0 以下）を参照し、`EnterStrongMode` ノードが `EnterStrongMode()` を呼んで発狂状態に入れる。`HpBelow` 条件は `CurrentHp` と `MaxHp` を参照する。

### `BulletEntityModel.cs`

弾の Model。
ダメージ量、プレイヤー弾か敵弾か、HP/生存状態を持つ。
衝突相手によってダメージを与え、自身の破壊状態を更新する。

### `SpectrumBarEntityModel.cs`

スペクトラムバー用 EntityModel。
EnemyEntityModel を継承し、接触時にプレイヤーを押し出す用途に使われる。

### `SpectrumBarPushMessage.cs`

スペクトラムバーがプレイヤーを押すための MessageBroker メッセージ。
PlayerEntityPresenter が購読し、移動に押し戻し量を加算する。

## Battle Entity Presenters

### `IEntityPresenter.cs`

Entity Presenter 共通契約。
Collider 経由で取得した Presenter から `EntityBase` を取得するために使われる。

### `PlayerEntityPresenter.cs`

プレイヤー操作と表示接続を担当する Presenter。

保持するもの:

- `PlayerEntityModel`
- `PlayerEntityView`
- 通常弾/チャージ弾の BulletPool（弾 Prefab の保持用）と弾の速さ（`normalShotSpeed` / `chargedShotSpeed`）
- `BattleFlow` から受け取る `BulletSystem`
- 移動速度、チャージ閾値、スニーク倍率、無敵時間
- 攻撃クールダウン
- 死亡演出 Director

できること:

- Player 入力メッセージから移動入力を保持する。
- PlayerAttackMessage で通常弾を撃つ。弾は `BulletSystem.Spawn` に `VelocityMotion` で渡す（OutQuad の時間写像が掛かるため、速さ 5 は実際には約 10/秒）。
- PlayerChargeMessage でチャージ開始/解除/チャージ弾発射を行う。
- スニーク中は移動速度を下げる。
- 画面境界内にプレイヤーを制限する。
- HP/無敵/チャージ完了を View に反映する。
- 被弾フラッシュとチャージフラッシュを制御する。
- 死亡時に攻撃入力と Collider を止め、死亡演出完了を通知する。
- Scenario 中や Retry 中に攻撃入力を購読解除/再購読する。
- `AddMoveModifier` / `RemoveMoveModifier` で外部から移動速度の補正（`IMoveModifier`）を受け付ける。WindService などのステージサービスが登録する。
- 自機の Transform を `BattleTargets.Player` に登録する（自機狙いの方向・移動が参照する）。

### `EnemyEntityPresenter.cs`

通常敵の Presenter。

保持するもの:

- `EnemyEntityModel`
- `EnemyEntityView`
- maxHp / contactDamage / onlyChargeDamageable / lifetime
- 被弾点滅の設定

できること:

- 出現位置で Model を初期化する。
- 被弾フラッシュを表示し、効かない攻撃を受けたら SE を鳴らす。
- 撃破・寿命切れ・画面外退出で `ActorDespawn.Despawn` を呼ぶ（プール品はプールへ戻り、それ以外は Destroy）。
- 場から消えたときに `OnRemoved` を 1 回通知する（`EnemyTracker` が使う）。
- `IPoolableActor` として、プールから出たときに再初期化し、戻すときに購読・寿命・点滅・Model を片付ける。
- Collider 衝突時に EntityModel 同士の衝突処理を呼ぶ。

移動と攻撃は持たない。Spawn ノードの `behavior`（雑魚は通常 `Commons/BT/Enemy`）で付いた `BattleActor` のツリーが行う。

### `BossEntityPresenter.cs`

ボスの Presenter。

保持するもの:

- `BossEntityModel`
- `BossEntityView`
- BossDeathDirector
- 死亡完了 Subject

できること:

- ボス HP を通常/強化ゲージへ反映する。
- 被弾フラッシュを表示する。
- 死亡時に Collider を無効にし、死亡演出後に Destroy する。
- 死亡演出完了を BattleScenePresenter へ通知する。
- シナリオ明けに一定時間 Collider を切る（`DisableColliderOnEntry`）。

ボスの登場移動は `BattleFlow.Spawn`（`BossDefinition.entrance`）、攻撃と移動はボスに付与した `BattleActor` のツリーが行う。

### `BulletEntityPresenter.cs`

弾の Presenter。

保持するもの:

- `BulletEntityModel`
- `BulletEntityView`
- ObjectPool 参照
- lifetime / 風の影響の倍率（`WindInfluence`）

できること:

- `BulletSystem` から `InitializeDriven` でダメージ、位置、方向、回転、飛距離を初期化される。
- HP 0、寿命切れ、画面外退出、飛距離到達で Pool に戻る。
- Collider 衝突時に EntityModel 同士の衝突処理を呼ぶ。

自前の移動は持たない。敵弾・自機弾とも、位置は常に `BulletSystem` が毎フレーム与える。

### `BulletPool.cs`

弾 Prefab ごとの共有 ObjectPool（`GetOrCreateSharedPool`）と事前生成（`Prewarm`）を持つ。
コンポーネントとしては撃ち手（自機・敵）の弾 Prefab を保持する（`BulletPrefab`）だけで、発射は `BulletSystem` が共有プールから行う。
共有プールは作ったシーンが閉じたときだけ破棄される（会話シーンの開閉では破棄しない）。

### `PhysicsBallPresenter.cs`

物理挙動を持つ Battle Entity 用 Presenter。
`IEntityPresenter` を実装し、物理ベースの衝突や EntityModel 連携に使う。

### `AudioSpectrumPresenter.cs`

AudioSource のスペクトラムなどを視覚表現へ変換する Presenter。
音楽連動の視覚演出やスペクトラムバー生成/更新に関わる。

### `SpectrumBarCollisionRelay.cs`

スペクトラムバーの Collider 衝突を中継する。
EntityPresenter として EntityModel を提供しつつ、接触時の押し出しなどを Player 側へ伝える。

### `BossPhaseProgressGauge.cs`

ボスフェーズの進行状況を表示する Presenter。
`BattleActor.Attached` で自分を含むボスにツリーが付与されたことを検知し、`BattleServices.PhaseStarted`（BT の `PhaseMarker` ノードが通知）を購読する。フェーズの位置は BGM のサンプル位置と閾値テーブルから求める。

### Death Directors

- `IDeathDirector.cs`: 死亡演出の共通契約。
- `DeathDirectorBase.cs`: 死亡演出の基底 Presenter。
- `PlayerDeathDirector.cs`: プレイヤー死亡演出。
- `BossDeathDirector.cs`: ボス死亡演出。

死亡演出は UniTask/CancellationToken で中断可能に設計され、Retry や Scene 破棄時にキャンセルされる。

## Battle Views

### `EntityViewBase.cs`

Battle Entity View の共通基底。
位置反映、表示状態、Sprite/Animation などの共通表示処理をまとめる。

### `PlayerEntityView.cs`

プレイヤー表示 View。

できること:

- 位置を更新する。
- HP 比率を表示する。
- Run/Charge/Attack/Stay などのアニメーション状態を切り替える。
- 被弾フラッシュを表示/解除する。
- チャージ完了フラッシュを表示/解除する。
- 死亡/Retry 時に表示状態を戻す。

### `EnemyEntityView.cs`

通常敵表示 View。
位置更新、被弾フラッシュ、表示状態の変更を扱う。

### `BossEntityView.cs`

ボス表示 View。
位置更新、通常 HP/強化 HP ゲージ、被弾フラッシュを扱う。

### `BulletEntityView.cs`

弾表示 View。
位置更新、表示/非表示、Pool 再利用時の表示リセットを扱う。

### `SpectrumBarView.cs`

スペクトラムバー表示 View。
音楽連動バーの見た目や位置表示を扱う。

### `BackgroundView.cs`

Battle 背景 View。
背景画像やスクロール表示を扱う。

### `FanCollider2D.cs`

扇形 Collider 補助。
通常の Collider2D では表現しづらい扇形範囲判定を提供する。

### `StaffNotationPresenter.cs`

五線譜風表示の Presenter。
Battle View 配下にあるが、表示制御用 MonoBehaviour として振る舞う。

## Common UI

### Button Views

- `ButtonBase.cs`: ボタン共通基底。
- `SimpleButton.cs`: 単純なクリック/決定入力を持つボタン。
- `MovableButton.cs`: 選択カーソルで移動可能なボタン。
- `ButtonListBase.cs`: ボタンリストの選択/決定/移動を扱う基底。
- `ScrollableButtonList.cs`: スクロールつきボタンリスト。
- `ButtonListType.cs`: ボタンリスト種別。
- `PointerAddon.cs`: 選択ポインタや装飾表示を補助する。

### Option Modal

#### `OptionModalPresenter.cs`

音量などオプション設定を扱う Presenter。
Global 常駐モーダルとして Title/Pause などから開かれる。
閉じる時に UserModel へ反映/保存し、SoundManager の音量を更新する。

#### `OptionModalView.cs`

Option Modal の表示 View。
BGM/SE 音量 UI、閉じるイベント、操作可能状態などを提供する。

### Pause Modal

#### `PauseModalPresenter.cs`

Battle 中の Pause Modal を扱う Presenter。
Retry、Title 戻り、Option 開閉などのイベントを公開する。

#### `PauseModalView.cs`

Pause Modal の表示 View。
再開、リトライ、タイトルへ戻る、オプションなどの UI を扱う。

### GameOver Modal

#### `GameOverModalPresenter.cs`

プレイヤー死亡後の GameOver Modal を扱う Presenter。
Retry と Title 戻りイベントを公開する。

#### `GameOverModalView.cs`

GameOver 表示 View。
ゲームオーバー表示と各ボタン入力を扱う。

### Tutorial Modal

#### `TutorialModalPresenter.cs`

Stage1 Way 初回突入時のチュートリアルモーダルを扱う Presenter。
初回入場かどうかによってスキップ待ちの扱いを切り替える。
閉じたことを BattleScenePresenter に通知する。

#### `TutorialModalView.cs`

Tutorial 表示 View。
チュートリアル画面、ページ/入力、閉じるイベントを扱う。

## Editor Extensions

### Project Editor

- `CommonScaffold.cs`: MVP 構成の雛形生成を補助する。
- `CreateDB.cs`: DataStore 用 ScriptableObject の生成を補助する。
- `GlobalSceneAutoLoader.cs`: Editor Play 時に Global を自動ロードする。
- `ScenarioImporter.cs`: 外部シナリオ JSON を Scenario 用データへ変換する。
- `SerializeReferenceAutoExpandEditor.cs`: SerializeReference の Inspector 表示を自動展開する。
- `SerializeReferenceDeduplicator.cs`: SerializeReference の重複参照を整理する。

### Battle Editor

BehaviorTree エディタ（`Assets/Project/Scenes/Battle/Editor/BehaviorTree/`）:

- `BehaviorTreeEditorWindow.cs`: `Window > Battle > Behavior Tree Editor`、または `BehaviorTreeAsset` のダブルクリックで開くウィンドウ。左にノードパレット、中央にグラフ、右にパラメータ一覧を並べる。
- `BehaviorTreeGraphView.cs` / `BehaviorTreeNodeView.cs` / `BehaviorTreePort.cs` / `BehaviorTreeEdge.cs`: GraphView によるツリー表示と編集（ドラッグ & ドロップ作成、コピー & ペースト、Auto Layout、再生中の実行ノードのハイライト）。入力欄は選択中のノードにだけ作り、大きなツリーでも重くならないようにしている。
- `NodePaletteView.cs` / `NodeCatalog.cs` / `NodeSearchWindowProvider.cs`: ノード一覧と検索。`NodeInfoAttribute` の説明・分類を表示する。BT アセット一覧から SubTree を作成できる。
- `ParameterPanelView.cs` / `BtValueDrawer.cs` / `ParameterUtility.cs`: ツリーパラメータの追加・改名・削除と、`BtInt` などの値欄の `$param` 結びつけ（`▾` メニューまたはドラッグ）。改名は値欄・`bindToParent`・`*Param` 欄に追従する（`ParameterUtility.FindReferencePaths`）。

使い方は [Battle/BehaviorTree.md](./Battle/BehaviorTree.md) の「エディタ」を参照。

その他:

- `SpectrumBarHeightRecorder.cs`: スペクトラムバーの高さ記録用の Editor 補助。

## Data Assets

### `StageData.asset`

ステージ一覧の親データ。
各ステージの表示情報、キャラ画像参照、`BattleStageAsset` のアドレス（`stageAddress`。値はアセットパス）を持つ。
StageList と Battle の両方が間接的に依存する。

### `BgmData.asset`

BGM 一覧の親データ。
`SoundModelRepository` が読み、`SoundManagerPresenter` が再生する。
SceneType と BgmType の組み合わせで検索される。

### `SeData.asset`

SE 一覧の親データ。
`SoundModelRepository` が読み、クリック、キャンセル、攻撃、チャージ、被弾などの再生に使われる。

### `CharacterData.asset`

キャラクター一覧の親データ。
キャラクター名や英名の参照元として使われる。

### Stage DataStore

`Assets/Project/DataStore/Stage1`〜`Stage4` は、バトル進行データの実体を `BT/` 配下に持つ。全ステージ共通の雑魚ツリーは `Commons/BT/Enemy.asset`。

- `BT/Stage{N}_BT.asset`: `BattleStageAsset`。StageData から参照される。
- `BT/Stage{N}_Boss.asset`: `BossDefinition`。
- `BT/Stage/`: ステージツリーとボスのツリー。
- `BT/Attack/`: 攻撃パターンのツリー（雑魚の `attack` パラメータ、ボスの SubTree から参照）。
- `BT/Motion/`: `MotionPreset`。
- `BT/Enemy/`: 共通ツリーに収まらない敵の専用ツリー（Stage2 のみ中身あり）。
- `BT/Trail/`: `TrailPath`（Stage2 のみ）。

- `Scenario/Stage{N}WayScenario.asset` / `Scenario/Stage{N}BossScenario.asset`: ボス前・撃破後の会話。ステージツリーの Scenario ノードが直接参照する。

旧方式のデータ（`Stage{N}` 直下の `Attack` / `Movement` / `Ease` / `Bullet` など）は削除済み。詳細は [Battle/DataStore.md](./Battle/DataStore.md)。

### DeprecatedResources

旧リソース置き場。
現在の主なロードは Addressables と `Assets/Project/Textures` / `DataStore` を使う。
新規実装では基本的にここへ依存しない。

## Asset Loading Rules

このプロジェクトでは Addressables アドレスとして実パスに近い文字列を使う箇所が多い。

代表:

- DataStore: `Assets/Project/DataStore/{DataName}.asset`
- Character Still: `Assets/Project/Textures/Character/{Name}/Still/{Name}_Still.png`
- Character Face: `Assets/Project/Textures/Character/{Name}/Face/{Name}_{Expression}_Face.png`

`GamePath` が基準パスを提供し、Repository が ZString でアドレスを組み立てる。

## Input and Event Rules

Input System を直接各 Presenter が読むのではなく、`InputManager` が `MessageBroker` にイベントを流す。

利点:

- UI と Player の入力購読が統一される。
- Scene Presenter が InputActionAsset への直接依存を避けられる。
- Pause や Scenario など複数の画面状態で購読制御しやすい。

代表的な購読:

- `StageListScenePresenter`: `UICancelMessage` で Title に戻る。
- `ScenarioScenePresenter`: `UISubmitMessage` で次メッセージへ進む。
- `BattleScenePresenter`: `PlayerPauseMessage` で Pause を切り替える。
- `PlayerEntityPresenter`: `PlayerMoveMessage`, `PlayerAttackMessage`, `PlayerChargeMessage` で操作する。

## Sound Rules

音声は `SoundManagerPresenter` に集約する。

BGM:

- `SceneType` と `BgmType` で選ぶ。
- `BgmData` のループサンプルを使い、AudioSource の `timeSamples` を戻す。
- Battle はステージ番号から SceneType を計算し、`Way` なら `BattleWay`、`Boss` なら `BattleBoss` を再生する。

SE:

- `SeType` で選ぶ。
- 単発 SE は `PlayOneShot`。
- チャージなどのループ SE は専用 AudioSource で手動ループする。

音量:

- `UserModel` の `bgmVolume` / `seVolume` を 0-100 で持つ。
- 実 AudioSource volume は dB カーブで変換される。

## Save Data Rules

セーブデータは `UserModel` が直接 JSON を読み書きする。

保存されるもの:

- クリア済みステージ番号。
- キー設定データ。
- BGM 音量。
- SE 音量。
- Stage1 初回入場フラグ。

Editor 実行では `Assets/Project/DataStore/UserData.json` を使う。
ビルドでは `Application.persistentDataPath/DataStore/UserData.json` を使う。

## Known Design Notes

- 既存ドキュメントにある `BattleWay` / `BattleBoss` シーン分割は現状の実装とは異なる。現在は `Battle` シーンに統合されている。
- `RuntimeModel.CurrentSituation` が Way/Boss の進行を表す中心状態である。
- `ScenarioScenePresenter` は BattleScenePresenter を直接呼ばない。演出関数以外の行は `ScenarioCommandMessage` として通知し、ステージツリーの Scenario ノードが同名の OnCommand で処理する（Boss 生成や Boss BGM 再生）。
- Battle の進行は BehaviorTree で行う。ツリーの定義 (`NodeDefinition`) は不変で、実行時は Build のたびに実行時ノードを生成するため、同じ `BehaviorTreeAsset` を複数の敵が同時に使っても状態は共有されない。
- BT の時間はフレーム基準（固定 60fps）、`MotionStep` の時間は秒指定である。混在しているので単位に注意する（[Battle/BehaviorTree-Nodes.md](./Battle/BehaviorTree-Nodes.md)）。
- Spawn ノードで出す雑魚は `ActorPool` で使い回す。敵を消すときは `Destroy` ではなく `ActorDespawn.Despawn` を通し、使い回すと状態が残るコンポーネントを持つ Prefab には `NoActorPool` を付ける（[Battle/BehaviorTree.md](./Battle/BehaviorTree.md) 4.5）。
- `UserData.Validate` は現在 `clearedStageNumber` を 0-1 に丸めるため、Stage2 以降を正式開放する場合は修正が必要になる可能性が高い。
- `InputManager` は通常の C# クラスなので、Unity の `OnDestroy` ライフサイクルに自動的には乗らない。長期的には明示 Dispose の検討余地がある。
- `MonoPresenter.Start` は Global が存在する前提が強い。Global 常駐前提の Scene では問題になりにくいが、単体シーン起動では Null になり得る。

## Where To Modify

目的別の編集場所:

- 新しいステージを追加する: `StageData.asset`（`stageAddress` に `BattleStageAsset` のパス）、`DataStore/StageN/BT`、Addressables 登録（アドレス = アセットパス）、`BattleStageType`, 必要なら `SceneType`, UserData の進行制限。
- 新しい道中/ボスフェーズを作る: ステージツリー・ボスのツリー（`BT/Stage/`）を BT エディタで編集する。フェーズの区切りは Until と条件で表す。
- 新しい弾幕を作る: `Fire` / `BulletBeam` などのノードを組んだ `BehaviorTreeAsset`（`BT/Attack/`）を作り SubTree で参照する。足りない撃ち方は `ShotPattern` / `DirectionSource` / `RotationSource` を追加する。
- 新しい敵移動を作る: `MotionPreset`（`BT/Motion/`）を作るか、`MotionStep` 実装を `Runtime/Motion/` に追加する。
- 新しい敵 Prefab を作る: `EnemyEntityPresenter`, `EnemyEntityView` を設定する。行動は Spawn ノードの `behavior` に共通ツリー `Commons/BT/Enemy` を指定し、`overrides` で `movement`（`BT/Motion/`）/ `attack`（`BT/Attack/`）/ `death` を渡す。この形に収まらない敵だけ `BT/Enemy/` に専用ツリーを作る。
- 新しいボス挙動を作る: Boss Prefab（`BossEntityPresenter`）と、`BossDefinition` の `prefab` / `spawnPosition` / `entrance` / `tree` を設定する。
- 新しいノードを作る: `Scripts/BehaviorTree/` 配下に `NodeDefinition` 派生（定義）と実行時ノードを追加する。`NodeInfoAttribute` を付けるとパレットに説明が出る。
- ステージ固有の機能を作る: `IStageService` と `StageServiceDefinition` を `Runtime/Stage/` に追加し、使うステージの `BattleStageAsset.services` に宣言する（[Battle/BehaviorTree.md](./Battle/BehaviorTree.md) の「ステージサービス」）。
- 新しいシナリオコマンドを作る: `ScenarioScenePresenter.ExecuteSteps` に function 分岐を追加し、必要なら `ScenarioView` / Battle Presenter へ処理を追加する。
- 新しい UI モーダルを作る: `Commons/UI/Scripts/View`, `Commons/UI/Scripts/Presenter`, `GlobalScenePresenter` への参照追加。
- 新しい入力を追加する: InputActionAsset, `InputManager`, `InputActionMessage.cs`, 購読側 Presenter を更新する。
- 新しい BGM/SE を追加する: `Textures/Sounds`, Addressables 登録, `BgmData.asset` / `SeData.asset`, `BgmType` / `SeType` を更新する。

## Recommended Reading Order

初めて構造を追う場合は以下の順番がよい。

1. `README.md`: プロジェクト概要。
2. `Document/Directory-Structure.md`: 配置の全体像。
3. `Document/Document.md`: このファイル。
4. `EntryScenePresenter.cs`: 起動処理。
5. `GlobalScenePresenter.cs`: 常駐管理。
6. `SceneNavigator.cs`: シーン遷移。
7. `RuntimeModel.cs`: ゲーム進行状態。
8. `StageListScenePresenter.cs`: ステージ選択から Battle への入口。
9. `BattleScenePresenter.cs`: バトル全体進行。
10. `Document/Battle/BehaviorTree.md`: バトル進行の仕組み。
11. `BattleFlow.cs` と `BattleWorld.cs`: BT の実行とフレームの進め方。
12. `BattleStageAsset.cs` と `BehaviorTreeAsset.cs`: バトルデータ構造。
13. `ScenarioScenePresenter.cs`: バトル間シナリオと進行更新。

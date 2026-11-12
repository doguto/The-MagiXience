# ステージ登録（DataStore / StageData / Addressable / BGM / シナリオ）

## DataStore のレイアウト

ステージのデータは `Assets/Project/DataStore/Stage{N}/` 配下に置く。BT 形式のデータは `BT/` にまとめる。

```
Assets/Project/DataStore/Stage{N}/
├─ BT/
│   ├─ Stage{N}Way_BT.asset    # 道中の BattleStageAsset (situation = Way)
│   ├─ Stage{N}Boss_BT.asset   # ボス戦の BattleStageAsset (situation = Boss)
│   ├─ Stage/                  # ステージツリー (Stage{N}Way_Tree / Stage{N}Boss_Tree)
│   ├─ Enemy/                  # 敵ツリー (Spawn の Behavior に指定する)
│   ├─ Attack/                 # 複数箇所から SubTree で参照する攻撃ツリー
│   ├─ Motion/                 # MotionPreset (敵・ボス・弾の移動)
│   └─ Ease/                   # EaseCurveAsset
├─ Way/
│   └─ Stage{N}WayScenario.asset   # 道中クリア後のシナリオ (命名規則で自動ロード)
└─ Boss/
    └─ Stage{N}BossScenario.asset  # ボスクリア後のシナリオ (命名規則で自動ロード)
```

- `Way/` `Boss/` にある他のアセット（`Stage{N}Way.asset` などの `BattleSequenceAsset`、`Phase*.asset`）と、`Attack/` `Movement/` `Ease/` `Bullet/` は旧形式のデータで、実行時には使われていない（削除予定）。新しく作らない。
- **シナリオアセットは `Way/` `Boss/` から動かさない。** 読み込み側がパスを決め打ちしている（下の「シナリオ接続」）。

`BattleStageAsset`（`Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BattleStageAsset.cs`）の項目:

| 項目 | 内容 |
|---|---|
| Situation | `Way` / `Boss` |
| Services | このステージでだけ有効な機能（`WindService` など）。宣言しないものは存在しない扱い |
| Tree | 道中: ステージ直下で実行するツリー。完了したら次へ進む |
| Boss Prefab / Boss Spawn Position | ボス戦で出すボス（`BossEntityPresenter` 付き Prefab）と出現位置 |
| Boss Entrance | 登場時の移動（`MotionStep` の列）。Boss Tree の開始時に終点まで進めて打ち切る |
| Boss Tree | ボス自身が実行するツリー。シナリオ明けに開始し、ボス撃破で完了 |

## StageData.asset への登録

`Assets/Project/DataStore/StageData.asset` は `StageDataObject`（`Assets/Project/Scripts/Infra/StageDataObject.cs`）のインスタンスで、`List<StageData>` を保持する。

```csharp
[Serializable]
public class StageData
{
    public string id;
    public int stageNumber;
    public string charaStillAddress;
    public string title;
    public string waySequenceAddress;  // 道中の BattleStageAsset のアドレス (= アセットパス)
    public string bossSequenceAddress; // ボス戦の BattleStageAsset のアドレス (= アセットパス)
}
```

- フィールド名は旧形式の名残で `SequenceAddress` だが、指す先は **`BattleStageAsset`**。例: `Assets/Project/DataStore/Stage1/BT/Stage1Way_BT.asset`。
- `BattleScenePresenter` がこのアドレスで Addressables から `BattleStageAsset` を読み込む。
- Stage5 以降のエントリは用意済みでアドレスが空の場合がある。新規ステージでは既存エントリを埋めれば済むことが多いので、まず `StageData.asset` の現状を確認する。
- 値の設定は Unity Editor 上で行う（Inspector か MCP の `execute_code` で `SerializedObject` を使う）。
- `charaStillAddress` はキャラクター静止画の Addressable キーで、シナリオのキャラ画像ロード（`ScenarioModelRepository.GetEnemyCharaName`）にも使われる。

## BGM

`BattleScenePresenter.PlayBgmForSituation()`（`Assets/Project/Scenes/Battle/Scripts/Presenter/BattleScenePresenter.cs`）が再生ロジックを持つ。

```csharp
var sceneType = (SceneType)(stageNumber + 2);  // SceneType.Stage1=3, Stage2=4, ... の対応
var bgmType = situation == BattleSituation.Boss ? BgmType.BattleBoss : BgmType.BattleWay;
soundManager.PlayBGMAsync(sceneType, bgmType).Forget();
```

`SceneType`（`Assets/Project/Scripts/Extensions/SceneType.cs`）は `Global, Title, StageList, Stage1, Stage2, Stage3, Stage4, Stage5, Stage6, StageEx, Scenario, ...` の順で定義されているため、`stageNumber + 2` で対応するenum値に変換される（Stage1=3なので `1+2=3`）。**新規ステージのBGMアセットは、この対応するSceneType×BgmType.BattleWay/BattleBossの組み合わせでサウンド管理側（SoundManager／BGMテーブル）に登録されている必要がある** — Battle Scene側の実装だけでなく、BGM登録側の設定も忘れずに確認すること。

`BgmType`（`Assets/Project/Scripts/Extensions/BgmType.cs`）: `Default, BattleWay, BattleBoss, Tutorial, Phase1, Phase2, ...`。道中/ボスで共通の2種類のみを使う（フェーズごとに別BGMにするわけではない）。

## シナリオ接続

道中クリア後・ボスクリア後は自動的にシナリオシーンへ遷移する（`BattleScenePresenter.HandleSequenceCompleted` → `TransitionToScenario`）。シナリオデータの読み込みは **明示的な参照ではなく命名規則からの動的解決**で行われる。

`ScenarioModelRepository.LoadData()`（`Assets/Project/Scenes/Scenario/Scripts/Repository/ModelRepository/ScenarioModelRepository.cs`）:

```csharp
string scenarioId = $"Stage{stageNumber}{situationSuffix}Scenario";  // situationSuffix: "Way" or "Boss"
string path = $"{GamePath.DataStorepath}/Stage{stageNumber}/{situationFolder}/{scenarioId}.asset";
// 例: Assets/Project/DataStore/Stage3/Way/Stage3WayScenario.asset
Addressables.LoadAssetAsync<ScenarioData>(path).WaitForCompletion();
```

つまり **`Stage{N}WayScenario.asset` / `Stage{N}BossScenario.asset` を正しいパス・正しい名前で配置しさえすれば、StageData等への追加登録なしに自動的にロードされる**。逆に言えば、命名や配置場所が1文字でもずれるとAddressablesロードが失敗し例外がスローされる（`ScenarioModelRepository.LoadData`は失敗時に例外をthrowする実装）ので、Unity MCPでアセットを作成する際は命名規則を厳密に守ること。

`ScenarioData` の中身自体（会話ステップ等）はこのスキルの対象外（別のシナリオ実装の知識が必要な場合は本文中の `Assets/Project/Scenes/Scenario/` 配下を別途調査すること）。

## Addressables の扱い

このプロジェクトでは、`waySequenceAddress` / `bossSequenceAddress` とシナリオのアドレスに **アセットのプロジェクトパスをそのまま使う**（`Addressables.LoadAssetAsync<T>(path)` にパス文字列を渡している）。

- 既存の `Stage{N}Way_BT` / `Stage{N}Boss_BT` は Default Local Group に、アドレス = アセットパスで登録済み。
- 新しい `BattleStageAsset` を作ったら、Addressables Groups ウィンドウで Default Local Group に追加し、アドレスをアセットパスにする。MCP から行う場合は `AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, settings.DefaultGroup)` で登録し、`entry.address` にパスを入れる。
- `BattleStageAsset` から参照されるツリーや `MotionPreset` は、依存関係として一緒に読み込まれるので個別の登録は要らない。

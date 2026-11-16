# ステージ登録（DataStore / StageData / Addressable / BGM / 会話）

## DataStore のレイアウト

ステージのデータは `Assets/Project/DataStore/Stage{N}/` 配下に置く。BT 形式のデータは `BT/`、会話は `Scenario/` にまとめる。全ステージ共通の雑魚ツリーは `Assets/Project/DataStore/Commons/BT/Enemy.asset`。

```
Assets/Project/DataStore/Stage{N}/
├─ BT/
│   ├─ Stage{N}_BT.asset       # BattleStageAsset (StageData.stageAddress の参照先。1 ステージに 1 つ)
│   ├─ Stage{N}_Boss.asset     # BossDefinition (SpawnBoss / BossBattle ノードから参照)
│   ├─ Stage/                  # ステージツリー Stage{N}_Tree とボスのツリー Stage{N}Boss_Tree
│   ├─ Attack/                 # 攻撃ツリー S{N}_* (雑魚の attack パラメータ、ボスの SubTree から参照)
│   ├─ Motion/                 # MotionPreset (敵・ボス・弾の移動。雑魚の movement パラメータ)
│   ├─ Enemy/                  # 共通ツリーに収まらない特殊な敵の専用ツリー (必要なときだけ)
│   ├─ Trail/                  # TrailPath (必要なときだけ)
│   └─ Ease/                   # EaseCurveAsset (必要なときだけ。現状どのステージにも無い)
└─ Scenario/
    ├─ Stage{N}WayScenario.asset   # ボス前の会話
    └─ Stage{N}BossScenario.asset  # 撃破後の会話
```

- 旧形式のデータ（`Stage{N}/` 直下の `Attack/` `Movement/` `Ease/` `Bullet/`、`Commons/` の `Attack/` `Ease/` `Movement/`）は削除済み。`Stage{N}/` 直下に `BT/` `Scenario/` 以外のフォルダを作らない（`Stage1/Boss/MusicTerminologySpectrum.asset` だけ例外的に残っている）。

`BattleStageAsset`（`Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BattleStageAsset.cs`）の項目:

| 項目 | 内容 |
|---|---|
| Services | このステージでだけ有効な機能（`WindService` など）。ステージ単位で、道中からボス戦まで続く。宣言しないものは存在しない扱い |
| Tree | ステージツリー。ルートは Stage ノード。Success で完了するとステージクリア |

`BossDefinition`（`.../BehaviorTree/Battle/BossDefinition.cs`）の項目:

| 項目 | 内容 |
|---|---|
| Prefab / Spawn Position | ボス（`BossEntityPresenter` 付き Prefab）と出現位置 |
| Entrance | 登場時の移動（`MotionStep` の列）。BossBattle の開始時に終わっていなければ終点まで進める |
| Tree | ボス自身が実行するツリー。BossBattle で開始し、ボス撃破（撃破演出の完了）で BossBattle が完了する |

## StageData.asset への登録

`Assets/Project/DataStore/StageData.asset` は `StageDataObject`（`Assets/Project/Scripts/Infra/StageDataObject.cs`）のインスタンスで、`List<StageData>` を保持する。

```csharp
[Serializable]
public class StageData
{
    public string id;
    public int stageNumber;
    public string charaStillAddress;
    public string backgroundAddress;
    public string title;
    public string stageAddress; // BattleStageAsset のアドレス (= アセットパス)
    public Vector2 scenarioEnemyStillOffset;
    public float scenarioEnemyStillScale = 1f;
}
```

- `stageAddress` の例: `Assets/Project/DataStore/Stage1/BT/Stage1_BT.asset`。`BattleScenePresenter` がこのアドレスで Addressables から `BattleStageAsset` を読み込む。
- Stage5 以降のエントリは用意済みで `stageAddress` が空。新規ステージでは既存エントリを埋めれば済むことが多いので、まず `StageData.asset` の現状を確認する。
- 値の設定は Unity Editor 上で行う（Inspector か MCP の `execute_code` で `SerializedObject` を使う）。
- `charaStillAddress` はキャラクター静止画の Addressable キーで、会話シーンの敵立ち絵のロードにも使われる。

## BGM

BGM はステージツリーの `PlayBgm` ノードで流す（道中の頭で `BattleWay`、ボス前の会話の `PlayBossBgm` コマンドで `BattleBoss`）。曲の選択は `BattleScenePresenter.PlayBgm(bgmType)`（`Assets/Project/Scenes/Battle/Scripts/Presenter/BattleScenePresenter.cs`）が行い、曲名も表示する。

```csharp
var sceneType = (SceneType)(stageModel.StageNumber + 2);  // SceneType.Stage1=3, Stage2=4, ... の対応
soundManager.PlayBGMAsync(sceneType, bgmType).Forget();
```

`SceneType`（`Assets/Project/Scripts/Extensions/SceneType.cs`）は `Global, Title, StageList, Stage1, Stage2, ...` の順なので `stageNumber + 2` で対応する値になる。**新規ステージの BGM は、この SceneType × `BgmType.BattleWay` / `BattleBoss` の組み合わせで `BgmData.asset` に登録されている必要がある。** Battle Scene 側だけでなく BGM 登録側も確認すること。

## 会話の接続

会話（`ScenarioData`）はステージツリーの PlayScenario ノードが直接参照する。命名規則による自動ロードや Addressables への登録は無い。

- ボス前: Section "Boss" の最初の Scenario（`skipOnResume` ON）に `Stage{N}WayScenario` を設定する。会話中の `SpawnBoss` / `PlayBossBgm` は子の OnCommand で処理する。
- 撃破後: Section "Boss" の最後の Scenario（`skipOnResume` OFF、`restorePlayerControl` OFF）に `Stage{N}BossScenario` を設定する。
- 会話シーンが処理する演出関数は `ScenarioData` の一覧（`ShowCastMessage` / `ShowMessage` / `TypeWriter` / `AutoAdvance` / `ShowCast` / `HideCast` / `ChangeCastAnimation` / `ShakeCastV`）だけ。それ以外の行はコマンドとしてツリーへ通知され、同名の OnCommand が無ければ警告が出て何も起きない。PlayScenario ノードの要約欄の ⚠ で過不足を確認する。
- `.txt` からの取り込み（`Assets/Project/Editor/ScenarioImporter.cs`）は、`{N}面ボス前` / `{N}面ボス後` を `Stage{N}/Scenario/Stage{N}WayScenario.asset` / `Stage{N}BossScenario.asset` に上書きする。既存アセットを上書きするので PlayScenario ノードの参照は保たれる。

`ScenarioData` の中身自体（会話ステップ等）はこのスキルの対象外（必要なら `Assets/Project/Scenes/Scenario/` 配下を別途調査すること）。

## Addressables の扱い

`stageAddress` には **アセットのプロジェクトパスをそのまま使う**（`Addressables.LoadAssetAsync<BattleStageAsset>(path)` にパス文字列を渡している）。

- 既存の `Stage{N}_BT` は Default Local Group に、アドレス = アセットパスで登録済み。
- 新しい `BattleStageAsset` を作ったら、Addressables Groups ウィンドウで Default Local Group に追加し、アドレスをアセットパスにする。MCP から行う場合は `AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, settings.DefaultGroup)` で登録し、`entry.address` にパスを入れる。
- `BattleStageAsset` から参照されるツリー・`BossDefinition`・`MotionPreset`・会話は、依存関係として一緒に読み込まれるので個別の登録は要らない。
- ステージ開始時に、`BattleStageAsset` から参照をたどって見つかった弾 Prefab（1 種類 30 発）とプール対象の敵 Prefab（1 種類 4 体）が事前生成される（`StagePrefabCollector`）。Prefab の中身はたどらないので、Prefab の中からだけ参照される弾や敵は事前生成されない（初回の使用時に生成される）。

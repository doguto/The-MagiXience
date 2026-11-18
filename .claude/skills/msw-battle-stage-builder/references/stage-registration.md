# ステージ登録（DataStore / StageData / Addressable / BGM / シナリオ）

## DataStoreのレイアウト

新規ステージ用のデータは `Assets/Project/DataStore/Stage{N}/` 配下に、以下のサブフォルダで構成する（Stage1, Stage2で実例あり）。

```
Assets/Project/DataStore/Stage{N}/
├─ Way/
│   ├─ Stage{N}Way.asset          # 道中のBattleSequenceAsset本体
│   ├─ Phase1.asset ... PhaseK.asset  # 道中の各フェーズのBattleTimelineBuilderAsset
│   └─ Stage{N}WayScenario.asset  # 道中クリア後のシナリオデータ
├─ Boss/
│   ├─ Stage{N}Boss.asset         # ボスのBattleSequenceAsset本体
│   ├─ Intro.asset / MelodyA.asset / MelodyB.asset / Chorus.asset
│   │   （+ IntroStrong.asset等、Strong版があれば）  # 楽節ごとのBattleTimelineBuilderAsset
│   └─ Stage{N}BossScenario.asset # ボスクリア後のシナリオデータ
├─ Attack/
│   └─ {楽節名}Normal.asset / {楽節名}Strong.asset   # AttackPreset（例: IntroNormal, MelodyAStrong）
├─ Movement/
│   └─ 用途ごとの MovementPreset（例: BossIntro, BossMelodyA, QuadInOut, Straight1, Phase1Down）
└─ Ease/
    └─ EaseCurvePreset（例: InLinearOutQuad, InQuadOutLinear, MidStop）
```

命名規則:
- 道中のフェーズ資産は `Phase1`, `Phase2`, ... と連番。
- ボスの資産はBGMの楽節名（`Intro` / `MelodyA` / `MelodyB` / `Chorus`）を使い、発狂版には `Strong` サフィックスを付ける（`ChorusStrong` 等）。
- `BattleSequenceAsset` 自体は `Stage{N}Way` / `Stage{N}Boss` という名前にする。**アセット名に"Way"または"Boss"の文字列が含まれていれば `BattleSequenceAsset.OnValidate()` が `situation` フィールドを自動設定する**ため、命名を外すと道中/ボスの判定がずれる。

## StageData.assetへの登録

`Assets/Project/DataStore/StageData.asset` は `StageDataObject`（`Assets/Project/Scripts/Infra/StageDataObject.cs`）のインスタンスで、`List<StageData>` を保持する。

```csharp
public class StageDataObject : ScriptableObject { public List<StageData> stageData; }

[Serializable]
public class StageData
{
    public string id;
    public int stageNumber;
    public string charaStillAddress;
    public string title;
    public string waySequenceAddress;  // Addressableアドレス = アセットパス文字列
    public string bossSequenceAddress; // Addressableアドレス = アセットパス文字列
}
```

- `waySequenceAddress` / `bossSequenceAddress` には **アセットのパスそのもの**を文字列で入れる（GUIDではない）。例: `Assets/Project/DataStore/Stage1/Way/Stage1Way.asset`。
- 実際、Stage3〜7（`id: 3`〜`6`, `id: EX`(stageNumber: 7)）は既にエントリが存在するが `waySequenceAddress` / `bossSequenceAddress` は空文字のまま。**新規ステージを追加する場合、多くはこの既存エントリの空アドレスを埋めるだけでよい**（エントリ自体を新規追加する必要があるかはStageData.assetの現状を必ず確認すること）。
- この登録・値設定はUnity MCP経由で行う（`.asset`の直接編集禁止）。
- `charaStillAddress` はステージのキャラクター静止画のAddressableキーで、シナリオのキャラ画像ロード（`ScenarioModelRepository.GetEnemyCharaName`）にも使われる。新規ステージでは対応する画像アセットのアドレスを設定すること。

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

## Addressableの扱いについて

このプロジェクトでは `waySequenceAddress` / `bossSequenceAddress` および シナリオのアドレスは、Addressable Groupへの明示登録名ではなく **アセットのプロジェクトパスをそのままAddressableアドレスとして使う運用**になっている（`Addressables.LoadAssetAsync<T>(path)` にパス文字列を直接渡している）。新規アセットを作成した時点でAddressableとして自動的に解決可能になるかは、プロジェクトのAddressable設定（パスベースのアドレス自動割当が有効かどうか）に依存するため、Unity MCPでアセット作成後に対象アセットがAddressableグループに含まれているか確認すること。含まれていなければAddressable Groups WindowでGroup追加が必要になる場合がある。

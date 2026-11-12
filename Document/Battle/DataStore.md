# DataStore データ構造解説

このドキュメントは `Assets/Project/DataStore` 配下に置かれているデータ（主にバトルシーン関連）の構造について解説する。

`DataStore` は ScriptableObject を使ったマスターデータ・ステージ進行データの置き場である。データは大きく分けて以下の3系統から成る。

- **マスターデータ系**（`BgmData` / `SeData` / `CharacterData` / `StageData`）: 単純な `List<T>` を持つだけの ScriptableObject。
- **バトル進行系**（`Stage{N}/BT/` 配下の `BattleStageAsset` / `BehaviorTreeAsset` / `MotionPreset` / `EaseCurveAsset`）: BehaviorTree 形式のステージ・敵・攻撃データ。ノードは `SerializeReference + SubclassSelector` によるポリモーフィックな構成で保存される。
- **シナリオ系**（`ScenarioData`）: 会話・演出イベントを `function` / `args` という汎用コマンド列で表現する簡易スクリプト形式。

これに加えて、開発用の `Test` フォルダが存在する。

> **旧形式について**: `Stage{N}/` 直下の `Attack/` / `Movement/` / `Ease/` / `Bullet/` と、`Way/` / `Boss/` 内の `BattleSequenceAsset`・`BattleTimelineBuilderAsset` は、旧方式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset` / `AttackPreset` / `MovementPreset` / `EaseCurvePreset`）のデータである。実行時には参照されておらず、削除予定。ただし `Way/` / `Boss/` にある `Stage{N}WayScenario.asset` / `Stage{N}BossScenario.asset` は現役のシナリオデータ（後述の Scenario 系）なので残すこと。旧形式の解説は本ドキュメントから削除した。必要なら git 履歴の旧版、および [Timeline.md](./Timeline.md) を参照すること。

## 全体の参照関係

```
StageData.asset
 ├─ waySequenceAddress ──► Stage{N}/BT/Stage{N}Way_BT.asset (BattleStageAsset, situation=Way)
 │                          ├─ services[] (例: WindServiceDefinition。Stage3 のみ)
 │                          └─ tree ──► Stage{N}/BT/Stage/Stage{N}Way_Tree.asset (BehaviorTreeAsset)
 │                                        └─ Spawn ノード
 │                                             ├─ prefab   ──► 敵 Prefab
 │                                             └─ behavior ──► Stage{N}/BT/Enemy/*.asset (BehaviorTreeAsset)
 │
 └─ bossSequenceAddress ──► Stage{N}/BT/Stage{N}Boss_BT.asset (BattleStageAsset, situation=Boss)
                             ├─ services[]
                             ├─ bossPrefab / bossSpawnPosition / bossEntrance (MotionStep[])
                             └─ bossTree ──► Stage{N}/BT/Stage/Stage{N}Boss_Tree.asset (BehaviorTreeAsset)
                                               ├─ SubTree ノード ──► Stage{N}/BT/Attack/*.asset
                                               └─ SetMovement / Move / BulletSpec.motionPreset ──► Stage{N}/BT/Motion/*.asset

Stage{N}/Way/Stage{N}WayScenario.asset / Stage{N}/Boss/Stage{N}BossScenario.asset (ScenarioData)
 └─ 会話・演出コマンド列。ScenarioScenePresenter が解釈し、
    "SpawnBoss" / "PlayBossBgm" コマンドでバトル進行と連動する。

BgmData.asset / SeData.asset
 └─ BgmData のループ位置 (サンプル) が BgmSamples 条件の thresholdSamples と、
    SeType の値が発射系ノードの se フィールドと対応している。
```

以降、各データを個別に解説する。

---

## 1. マスターデータ系

### BgmData（`BgmData.asset`）

- 定義: `Assets/Project/Scripts/Infra/BgmDataObject.cs`
- `BgmDataObject : ScriptableObject` は `List<BgmData> bgmData` を持つだけの単純な構造。

| フィールド | 型 | 説明 |
|---|---|---|
| `name` | `string` | 曲名（表示・ログ用） |
| `sceneType` | `SceneType` | どのシーンで使われる曲かの分類（`Global, Title, StageList, Stage1〜6, StageEx, Scenario, Entry`） |
| `bgmType` | `BgmType` | 同一シーン内での用途分類（`Default, BattleWay, BattleBoss, Tutorial, Phase1, Phase2`） |
| `loopStartSamples` | `int` | ループ開始位置（サンプル数） |
| `loopEndSamples` | `int` | ループ終了位置（`0`ならクリップ全体長） |

`loopStartSamples`/`loopEndSamples` はボスツリーの `BgmSamples` 条件の `thresholdSamples` と同じ数値が使われることがあり、BGMの再生位置をトリガーにボス戦のフェーズを進行させる設計と対応している。

### SeData（`SeData.asset`）

- 定義: `Assets/Project/Scripts/Infra/SeDataObject.cs`

| フィールド | 型 | 説明 |
|---|---|---|
| `name` | `string` | SE名 |
| `seType` | `SeType` | `None, Click, Cancel, EnemyShotShort, EnemyShotLong, Attack, Charge, ChargeRelease, Damage, LocoAttack1, LocoAttack2` |
| `loopStartSamples` / `loopEndSamples` | `int` | ループ再生用（`PlayLoopSE`使用時のみ参照） |

`seType` は BT の発射系ノード（`Fire` / `BulletBeam` / `MoveTrailFan`）や `PlaySe` ノードの `se` フィールドからも参照され、発射時に鳴らすSEの指定に使われる。

### CharacterData（`CharacterData.asset`）

- 定義: `Assets/Project/Scripts/Infra/CharacterDataObject.cs`

| フィールド | 型 | 説明 |
|---|---|---|
| `id` | `int` | キャラID |
| `name` | `string` | 日本語表示名 |
| `englishName` | `string` | アセット参照キー（`StageData.charaStillAddress` やシナリオ側の立ち絵ロード処理と一致する文字列） |

実データには8キャラが登録されている（テン/Ten, ロコ/Loco, アズマ/Azuma, タツミ/Tatsumi, ウシトラ/Ushitora, スイ/Sui, ハナレ/Hanare, コン/Kon）。

### StageData（`StageData.asset`）

- 定義: `Assets/Project/Scripts/Infra/StageDataObject.cs`

| フィールド | 型 | 説明 |
|---|---|---|
| `id` | `string` | ステージID（"1"〜"6", "last"） |
| `stageNumber` | `int` | 数値のステージ番号 |
| `charaStillAddress` | `string` | ステージ画面に表示する立ち絵のアドレス（`CharacterData.englishName`と対応） |
| `backgroundAddress` | `string` | バトル背景画像のアドレス |
| `title` | `string` | ステージタイトル |
| `waySequenceAddress` | `string` | 道中の `BattleStageAsset`（`Stage{N}/BT/Stage{N}Way_BT.asset`）の Addressables アドレス |
| `bossSequenceAddress` | `string` | ボス戦の `BattleStageAsset`（`Stage{N}/BT/Stage{N}Boss_BT.asset`）の Addressables アドレス |
| `scenarioEnemyStillOffset` | `Vector2` | シナリオ中の敵立ち絵の表示位置補正（UI座標） |
| `scenarioEnemyStillScale` | `float` | シナリオ中の敵立ち絵の拡大率（0以下は1倍） |

アドレスはアセットパスそのもの（例: `Assets/Project/DataStore/Stage1/BT/Stage1Way_BT.asset`）。フィールド名に `Sequence` が残っているのは旧形式（`BattleSequenceAsset`）の名残で、現在の参照先は `BattleStageAsset` である。`BattleScenePresenter` が `Addressables.LoadAssetAsync<BattleStageAsset>` で読み込む。

実データは7エントリ（Stage1〜6、last）で、`waySequenceAddress`/`bossSequenceAddress` が設定済みなのは Stage1〜4。Stage5以降は未実装（空文字）。


---

## 2. バトル進行系（`Stage{N}/BT/`）

BehaviorTree 方式のバトルデータ。仕組みは [BehaviorTree.md](./BehaviorTree.md)、ノードとフィールドの意味は [BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) を参照すること。

```text
Assets/Project/DataStore/Stage{N}/BT/
├── Stage{N}Way_BT.asset    # 道中の BattleStageAsset (StageData.waySequenceAddress の参照先)
├── Stage{N}Boss_BT.asset   # ボス戦の BattleStageAsset (StageData.bossSequenceAddress の参照先)
├── Stage/                  # ステージ直下・ボス本体のツリー (Stage{N}Way_Tree / Stage{N}Boss_Tree)
├── Enemy/                  # 敵 1 種ごとの行動ツリー。Spawn ノードの behavior から参照
├── Attack/                 # ボスなどが SubTree で再利用する攻撃パターンのツリー
├── Motion/                 # MotionPreset (移動ステップ列)。敵・ボスの移動、弾の移動に使う
└── Ease/                   # EaseCurveAsset (カスタム Ease カーブ)。現状のデータには存在しない
```

- `Attack/` は Stage2〜4 にのみ存在する。Stage1 の攻撃はツリー内に直接書かれている。
- 現在のデータは旧形式から移行ツールで生成したものを基に、BT エディタで編集している。ファイル名の `S{N}_` 接頭辞や `_2_Azuma` のような接尾辞は移行時に旧アセット名から付けたもの。

### BattleStageAsset（`Stage{N}Way_BT.asset` / `Stage{N}Boss_BT.asset`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BattleStageAsset.cs`

| フィールド | 型 | 説明 |
|---|---|---|
| `situation` | `BattleSituation` | `Way` / `Boss` |
| `services` | `List<StageServiceDefinition>` | このステージでだけ有効な機能。Stage3 の 2 アセットだけが `WindServiceDefinition`（初期の風と矢印 Prefab）を宣言している |
| `tree` | `BehaviorTreeAsset` | ステージ直下のツリー。道中ではこの完了が道中の完了。ボス戦では未設定（ボス戦中のステージ演出が必要なときだけ使う） |
| `bossPrefab` | `GameObject` | ボス Prefab（`BossEntityPresenter` 必須）。ボス戦のみ |
| `bossSpawnPosition` | `Vector3` | ボス生成位置 |
| `bossEntrance` | `List<MotionStep>` | ボス登場の移動（秒指定） |
| `bossTree` | `BehaviorTreeAsset` | ボス自身が実行するツリー。シナリオ明けに開始 |

Addressables には「アドレス = アセットパス」で登録されている（移行ツールが登録）。新しいステージを追加する場合も同じ規則で登録し、`StageData` の `waySequenceAddress` / `bossSequenceAddress` にそのパスを書く。

### BehaviorTreeAsset（`Stage/` `Enemy/` `Attack/`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BehaviorTreeAsset.cs`

| フィールド | 説明 |
|---|---|
| `root` | ルートノード（`[SerializeReference]`） |
| `detachedNodes` | エディタ上で未接続のノード。実行時は使わない |
| `parameters` | ツリーパラメータ（`TreeParameter`）。SubTree / Spawn から上書きできる |

ノードはすべて同じアセットの `references` セクションに `rid` 付きで保存される（`SerializeReference` の YAML 形式）。手で YAML を編集せず、`Window > Battle > Behavior Tree Editor` で編集すること。

役割ごとの典型的な形:

- `Stage/Stage{N}Way_Tree`: `Sequence[ Sequence#Phase1[ Timeline{ Delay(t){Spawn} ... }, WaitUntil(NoActiveEnemies) ], Sequence#Phase2 ... ]`
- `Stage/Stage{N}Boss_Tree`: フェーズごとの `Until(条件){...}` を Sequence / Loop / Random / Selector で組んだもの。BGM 同期（`BgmSamples`）、HP 閾値（`HpBelow`）、発狂（`NormalHpDepleted` + `EnterStrongMode`）など
- `Enemy/*`: `Sequence[ SetDeathBehavior?, SetMovement, Until(FrameElapsed){ Repeat{Fire} } ]`
- `Attack/*`: ボスの 1 フェーズ分の攻撃（`Timeline` / `Repeat` / `Fire` / `BulletBeam` など）

### MotionPreset（`Motion/`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/Runtime/Motion/MotionAssets.cs`（`Create > Battle > Motion Preset`）
- `[SerializeReference] List<MotionStep> steps` を持つだけ。時間は秒指定。
- `Move` / `SetMovement` の `preset`、`BulletSpec.motionPreset`、`LoopMotion` のエントリから参照する。`Bullet_{弾Prefab名}` という名前のものは、旧形式で弾 Prefab 側に設定されていた移動を移行時に切り出したもの。

### EaseCurveAsset（`Ease/`）

- 定義: 同じく `MotionAssets.cs`（`Create > Battle > Ease Curve`）
- `AnimationCurve curve` を 1 つ持つだけ。`MotionEase.easeValue = -1`（カスタムカーブ）のときに `curvePreset` として参照される。

---

## 3. Scenario系（会話イベント・演出シナリオ）

- 定義: `Assets/Project/Scenes/Scenario/Scripts/Model/ScenarioData.cs`

```csharp
public class ScenarioData : ScriptableObject
{
    List<ScenarioStep> steps;
}

[Serializable]
public class ScenarioStep
{
    string function;
    string[] args;
}
```

1行 = 1コマンドという汎用的な形式で会話・演出を表現する。

### インポート元: `ScenarioImporter.cs`（Editor専用）

`Assets/Project/Editor/ScenarioImporter.cs` から `.txt` ファイルを読み込み `ScenarioData` アセットへ変換する。パース規則:

- 空行・`//`コメント・区切り線（`----------------`）は無視。
- `@関数名,引数1,引数2,...` の形式（カンマ区切り）がコマンド行。
- `ShowCastMessage` / `ShowMessage` は特別扱いで、コマンド行の直後にある地の文（複数行可）を改行結合して最後の引数（メッセージ本文）に追加する。これにより会話テキストを複数行で書ける。

ファイル名が `{N}面ボス前` / `{N}面ボス後` の場合、生成された `.asset` は `Assets/Project/DataStore/Stage{N}/Way/Stage{N}WayScenario.asset` / `Stage{N}/Boss/Stage{N}BossScenario.asset` として保存される（既存は上書き）。それ以外のファイル名は保存先をダイアログで選ぶ。実行時は `ScenarioModelRepository` が `Stage{N}/{Way|Boss}/` のこのパスを Addressables で読み込む。

### 実行時の解釈: `ScenarioScenePresenter.cs`

代表的な `function` とその意味:

| function | args | 説明 |
|---|---|---|
| `ShowCastMessage` | キャラ名, メッセージ | キャラ立ち絵付きの会話表示。決定入力待ちが発生する |
| `ShowMessage` | キャラ名, メッセージ | 地の文的メッセージ表示（同様に入力待ちあり） |
| `ShowCast` | キャラ名, ?, 表情差分, 表示時間, 位置(LL/RR), ? | キャラ立ち絵の登場演出。LL＝プレイヤー側、RR＝敵側の表情辞書から取得 |
| `SpawnBoss` | なし | `BattleScenePresenter.SpawnBoss()` を呼びボスを出現させる（Way→Bossの橋渡し） |
| `PlayBossBgm` | なし | ボス戦BGMの再生を開始する |
| `HideCast` | キャラ名, フェード秒数 | 立ち絵をフェードアウト |
| `ChangeCastAnimation` | キャラ名, 表情差分 | 表情差し替え |

一覧に無い関数名（実データに存在する `ShakeCastV` など）は現状**未実装**で、黙って次のステップへ進む。全ステップ終了後は `RuntimeModel.AdvanceToNextSequence()` によりWay→Boss、あるいはBoss→次ステージへと状態が進む。

> **注意**: `function`/`args` 形式は拡張性が高い一方、`ShakeCastV` のように未実装の関数がデータ上に残っていても実行時にエラーにならず黙って無視される。「シナリオにコマンドを書いたのに演出が反映されない」不具合に気づきにくいので、新規演出コマンドを追加する際はPresenter側の分岐漏れに注意する必要がある。


---

## 4. Test配下（テスト用データ）

開発時の動作確認用のデータ。

- `Test/TestSequence.asset` / `Test/TestTimeline.asset`: 旧形式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset`）の最小構成。旧形式とともに削除予定。
- BT エディタの動作確認用の `BehaviorTreeAsset` などが一時的に置かれることがある。ゲーム本編からは参照されない。

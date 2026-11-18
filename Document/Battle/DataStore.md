# DataStore データ構造解説

このドキュメントは `Assets/Project/DataStore` 配下に置かれているデータ（主にバトルシーン関連）の構造について解説する。

`DataStore` は ScriptableObject を使ったマスターデータ・ステージ進行データの置き場である。データは大きく分けて以下の3系統から成る。

- **マスターデータ系**（`BgmData` / `SeData` / `CharacterData` / `StageData`）: 単純な `List<T>` を持つだけの ScriptableObject。
- **バトル進行系**（`Commons/BT/` と `Stage{N}/BT/` 配下の `BattleStageAsset` / `BossDefinition` / `BehaviorTreeAsset` / `MotionPreset` / `EaseCurveAsset`）: BehaviorTree 形式のステージ・ボス・敵・攻撃データ。ノードは `SerializeReference + SubclassSelector` によるポリモーフィックな構成で保存される。
- **シナリオ系**（`Stage{N}/Scenario/` 配下の `ScenarioData`）: 会話・演出イベントを `function` / `args` という汎用コマンド列で表現する簡易スクリプト形式。

これに加えて、開発用の `Test` フォルダが存在する。

> **旧形式について**: 旧形式のデータ（`Stage{N}/` 直下の `Attack/` / `Movement/` / `Ease/` / `Bullet/`、`Commons/` の `Attack/` / `Ease/` / `Movement/`、旧 `Way/` / `Boss/` の `BattleSequenceAsset`・`BattleTimelineBuilderAsset`）はすべて削除済み。`Stage1/Boss/` には `MusicTerminologySpectrum.asset` だけが残る。

## 全体の参照関係

```
StageData.asset
 └─ stageAddress ──► Stage{N}/BT/Stage{N}_BT.asset (BattleStageAsset)
                      ├─ services[] (例: WindServiceDefinition。Stage3 のみ)
                      └─ tree ──► Stage{N}/BT/Stage/Stage{N}_Tree.asset (ステージツリー)
                                   ├─ Wave / Spawn ノード
                                   │    ├─ prefab    ──► 敵 Prefab
                                   │    ├─ behavior  ──► Commons/BT/Enemy.asset (共通の雑魚ツリー。特殊な敵は Stage{N}/BT/Enemy/*.asset)
                                   │    └─ overrides ──► movement: Stage{N}/BT/Motion/*.asset, attack: Stage{N}/BT/Attack/*.asset, death: 撃破時のツリー
                                   ├─ PlayScenario ノード ──► Stage{N}/Scenario/Stage{N}WayScenario.asset / Stage{N}BossScenario.asset
                                   └─ SpawnBoss / BossBattle ノード ──► Stage{N}/BT/Stage{N}_Boss.asset (BossDefinition)
                                        ├─ prefab / spawnPosition / entrance (MotionStep[])
                                        └─ tree ──► Stage{N}/BT/Stage/Stage{N}Boss_Tree.asset
                                                     ├─ SubTree ノード ──► Stage{N}/BT/Attack/*.asset
                                                     └─ Move / BulletConfig.motionPreset ──► Stage{N}/BT/Motion/*.asset

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

`seType` は BT の発射系ノード（`NWayShot` などの弾を撃つノード / `BulletBeam` / `MoveTrailFan`）や `PlaySe` ノードの `se` フィールドからも参照され、発射時に鳴らすSEの指定に使われる。

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
| `stageAddress` | `string` | `BattleStageAsset`（`Stage{N}/BT/Stage{N}_BT.asset`）の Addressables アドレス |
| `scenarioEnemyStillOffset` | `Vector2` | シナリオ中の敵立ち絵の表示位置補正（UI座標） |
| `scenarioEnemyStillScale` | `float` | シナリオ中の敵立ち絵の拡大率（0以下は1倍） |

アドレスはアセットパスそのもの（例: `Assets/Project/DataStore/Stage1/BT/Stage1_BT.asset`）で、Default Local Group に登録する。`BattleScenePresenter` が `Addressables.LoadAssetAsync<BattleStageAsset>` で読み込む。

実データは7エントリ（Stage1〜6、last）で、`stageAddress` が設定済みなのは Stage1〜4。Stage5以降は未実装（空文字）。


---

## 2. バトル進行系（`Commons/BT/` / `Stage{N}/BT/`）

BehaviorTree 方式のバトルデータ。仕組みは [BehaviorTree.md](./BehaviorTree.md)、ノードとフィールドの意味は [BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) を参照すること。

```text
Assets/Project/DataStore/Commons/BT/
└── Enemy.asset             # 全ステージ共通の雑魚ツリー (BehaviorTree.md 5.4)

Assets/Project/DataStore/Stage{N}/BT/
├── Stage{N}_BT.asset       # BattleStageAsset (StageData.stageAddress の参照先)
├── Stage{N}_Boss.asset     # BossDefinition (SpawnBoss / BossBattle ノードの参照先)
├── Stage/                  # ステージツリー Stage{N}_Tree とボスのツリー Stage{N}Boss_Tree
├── Attack/                 # 攻撃パターンのツリー S{N}_*。雑魚の attack パラメータやボスの SubTree から参照
├── Motion/                 # MotionPreset (移動ステップ列)。敵・ボスの移動、弾の移動に使う
├── Enemy/                  # 共通ツリーに収まらない特殊な敵の専用ツリー (現状は Stage2 の 2 つだけ)
└── Trail/                  # TrailPath (MovePath / PlaceAlongPath の経路)。Stage2 のみ
```

- `Attack/` は中身が同じ攻撃を 1 つにまとめてある。雑魚とボスの両方から参照される。
- `Motion/*_Move` は、雑魚のツリーにインラインで書かれていた移動を共通ツリー化のときに切り出したもの。
- `Enemy/` に残っているのは、攻撃中に移動を変える `Stage2/BT/Enemy/S2_2_WayAzuma` と、撃破時の行動ツリー `S2_2_WayEnemy1_death_Death` だけ。Stage1・3・4 の `Enemy/` は空。
- `EaseCurveAsset` 用の `Ease/` は現状どのステージにも無い。必要になったら `Stage{N}/BT/Ease/` に置く。
- ファイル名の `S{N}_` 接頭辞や `_2_Azuma` のような接尾辞は、旧形式から変換したときに旧アセット名から付けたもの。

### BattleStageAsset（`Stage{N}_BT.asset`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BattleStageAsset.cs`
- 1 ステージ = 1 アセット。

| フィールド | 型 | 説明 |
|---|---|---|
| `services` | `List<StageServiceDefinition>` | このステージでだけ有効な機能。Stage3 だけが `WindServiceDefinition`（初期の風と矢印 Prefab）を宣言している |
| `tree` | `BehaviorTreeAsset` | ステージツリー（`Stage/Stage{N}_Tree`）。Success で完了するとステージクリア |

Addressables には「アドレス = アセットパス」で Default Local Group に登録する。`StageData.stageAddress` にそのパスを書く。ツリー・BossDefinition・会話など、ここから参照されるアセットは依存として一緒に読み込まれるので個別の登録は要らない。

### BossDefinition（`Stage{N}_Boss.asset`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Parts/BossDefinition.cs`（`Create > Battle > Boss Definition`）

| フィールド | 型 | 説明 |
|---|---|---|
| `prefab` | `GameObject` | ボス Prefab（`BossEntityPresenter` 必須） |
| `spawnPosition` | `Vector3` | 生成位置 |
| `entrance` | `List<MotionStep>` | 登場の移動（秒指定）。戦闘開始時に終わっていなければ終点まで進める |
| `tree` | `BehaviorTreeAsset` | ボス自身が実行するツリー（`Stage/Stage{N}Boss_Tree`）。BossBattle ノードで別のツリーを指定しなければこれを使う |

### BehaviorTreeAsset（`Commons/BT/Enemy` と `Stage/` `Attack/` `Enemy/`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/BehaviorTreeAsset.cs`

| フィールド | 説明 |
|---|---|
| `root` | ルートノード（`[SerializeReference]`） |
| `detachedNodes` | エディタ上で未接続のノード。実行時は使わない |
| `parameters` | ツリーパラメータ（`TreeParameter`）。SubTree / Spawn から上書きできる |

ノードはすべて同じアセットの `references` セクションに `rid` 付きで保存される（`SerializeReference` の YAML 形式）。手で YAML を編集せず、`Window > Battle > Behavior Tree Editor` で編集すること。

役割ごとの典型的な形:

- `Stage/Stage{N}_Tree`: `Stage[ Section "Way"[ PlayBgm, Wave..., Wait ], Section "Boss"[ PlayScenario{OnCommand SpawnBoss / PlayBossBgm}, BossBattle, Wait, PlayScenario ] ]`（[BehaviorTree.md](./BehaviorTree.md) 6.1）
- `Stage/Stage{N}Boss_Tree`: フェーズごとの `Until(条件){...}` を Sequence / Loop / RandomSelect / Selector で組んだもの。BGM 同期（`BgmSamples`）、HP 閾値（`HpBelow`）、発狂（`NormalHpDepleted` + `EnterStrongMode`）など
- `Commons/BT/Enemy`: `Parallel(All)[ Move($movement), Sequence[ SetDeathBehavior($death), SubTree($attack) ] ]`。パラメータ `movement`（MotionPreset）/ `attack` / `death`（BehaviorTreeAsset）を Spawn の `overrides` で渡す
- `Attack/*`: 雑魚の攻撃（`Until(FrameElapsed){ Repeat{NWayShot} }` など）やボスの 1 フェーズ分の攻撃（`Sequence` + `Wait` / `Repeat` / `NWayShot` などの弾を撃つノード / `BulletBeam` など）
- `Enemy/*`: 共通ツリーで表せない敵の専用ツリー（攻撃の途中で移動を変えるなど）

### MotionPreset（`Motion/`）

- 定義: `Assets/Project/Scenes/Battle/Scripts/Runtime/Motion/MotionPreset.cs`（`Create > Battle > Motion Preset`）
- `[SerializeReference] List<MotionStep> steps` を持つだけ。時間は秒指定。
- `Move` の `preset`、共通ツリー `Enemy` の `movement` パラメータ、`BulletConfig.motionPreset`、`LoopMotion` のエントリから参照する。`Bullet_{弾Prefab名}` という名前のものは、旧形式で弾 Prefab 側に設定されていた移動を移行時に切り出したもの。

### EaseCurveAsset

- 定義: `Runtime/Motion/EaseCurveAsset.cs`（`Create > Battle > Ease Curve`）。現状のデータには無い
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

ファイル名が `{N}面ボス前` / `{N}面ボス後` の場合、生成された `.asset` は `Stage{N}/Scenario/Stage{N}WayScenario.asset` / `Stage{N}/Scenario/Stage{N}BossScenario.asset` に上書き保存される。既存アセットを上書きするので、ステージツリーの PlayScenario ノードからの参照はそのまま保たれる。それ以外のファイル名は保存先をダイアログで選ぶ。

### 実データの配置と参照

```text
Assets/Project/DataStore/Stage{N}/Scenario/
├── Stage{N}WayScenario.asset    # ボス前の会話 (Section "Boss" の最初の PlayScenario ノード)
└── Stage{N}BossScenario.asset   # 撃破後の会話 (Section "Boss" の最後の PlayScenario ノード)
```

会話はステージツリーの PlayScenario ノードが直接参照する。Addressables への登録は不要。流す会話は PlayScenario ノードから `ScenarioModelRepository.SetData(ScenarioData)` で指定される（ステージ番号と Way/Boss からの自動決定は廃止）。

### 実行時の解釈: `ScenarioScenePresenter.cs`

会話シーンが自分で処理する演出関数は `ScenarioData` に列挙されたものだけである。

| function | args | 説明 |
|---|---|---|
| `ShowCastMessage` | キャラ名, メッセージ | キャラ立ち絵付きの会話表示。決定入力待ちが発生する |
| `ShowMessage` | キャラ名, メッセージ | 地の文的メッセージ表示（同様に入力待ちあり） |
| `TypeWriter` | true/false, 1秒あたりの文字数 または `default` | 1文字ずつの表示の切り替え |
| `AutoAdvance` | true/false, 秒数 または `auto` | 自動送りの切り替え |
| `ShowCast` | キャラ名, ?, 表情差分, 表示時間, 位置(LL/RR), ? | キャラ立ち絵の登場演出。LL＝プレイヤー側、RR＝敵側の表情辞書から取得 |
| `HideCast` | キャラ名, フェード秒数 | 立ち絵をフェードアウト |
| `ChangeCastAnimation` | キャラ名, 表情差分 | 表情差し替え |
| `ShakeCastV` | | 演出として扱うが、現状は処理が無く何もしない（コマンドとしても通知しない） |

それ以外の行（実データでは `SpawnBoss` / `PlayBossBgm`）は **コマンド** として `MessageBroker` の `ScenarioCommandMessage` で通知されるだけで、会話シーンは処理しない。処理はステージツリーの PlayScenario ノードが、同名の OnCommand の子で行う（[BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) 7.5）。

> **注意**: 会話に書いたコマンドに対応する OnCommand が無いと、実行時に警告が出るだけで何も起きない。PlayScenario ノードの要約欄に過不足が ⚠ で表示されるので、BT エディタで確認すること。


---

## 4. Test配下（テスト用データ）

開発時の動作確認用のデータ。

- BT エディタの動作確認用の `BehaviorTreeAsset`（`BT_EditorPreview` など）が置かれている。ゲーム本編からは参照されない。

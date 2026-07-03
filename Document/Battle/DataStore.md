# DataStore データ構造解説

このドキュメントは `Assets/Project/DataStore` 配下に置かれているデータ（主にバトルシーン関連）の構造について解説する。

`DataStore` は ScriptableObject を使ったマスターデータ・ステージ演出データの置き場である。データは大きく分けて以下の3系統から成る。

- **マスターデータ系**（`BgmData` / `SeData` / `CharacterData` / `StageData`）: 単純な `List<T>` を持つだけの ScriptableObject。
- **バトル演出系**（`BattleSequenceAsset` を頂点とする Way / Boss / Attack / Movement / Ease 群）: `SerializeReference + SubclassSelector` によるポリモーフィックな構成が多用されており、1つのフェーズに対して「移動」「攻撃」「敵の出現」などの部品を組み合わせて表現する。
- **シナリオ系**（`ScenarioData`）: 会話・演出イベントを `function` / `args` という汎用コマンド列で表現する簡易スクリプト形式。

これに加えて `UserData.json` というセーブデータ（ScriptableObjectではない素のJSON）と、開発用の `Test` フォルダが存在する。

## 全体の参照関係

```
StageData.asset
 ├─ waySequenceAddress ──► Stage1Way.asset (BattleSequenceAsset, situation=Way)
 │                          └─ sequenceGroups[].phases[]
 │                              ├─ timelineBuilder / timelineBuilderStrong ──► Phase1〜9.asset (BattleTimelineBuilderAsset)
 │                              │     └─ enemySpawnTracks[].clips[].prefab ──► 敵Prefab
 │                              └─ exitConditionConfig (SerializeReference)
 │                                    ──► TimeLimit / AllEnemiesDefeated / BgmPosition / BossHpThreshold / Composite
 │
 └─ bossSequenceAddress ──► Stage1Boss.asset (BattleSequenceAsset, situation=Boss)
                             ├─ bossPrefab / bossSpawnPosition / bossEntranceMovement (IMovementStep[])
                             └─ sequenceGroups[].phases[]
                                 └─ timelineBuilder ──► Intro / MelodyA / MelodyB / Chorus.asset
                                       ├─ bossAttackPreset ──► Attack/*.asset
                                       └─ bossMovementPreset ──► Movement/*.asset (Ease/*.assetを参照する場合あり)

Stage1WayScenario.asset / Stage1BossScenario.asset (ScenarioData)
 └─ 会話・演出コマンド列。ScenarioScenePresenter が解釈し、
    "SpawnBoss" / "PlayBossBgm" コマンドで上記バトル演出系と連動する。

BgmData.asset / SeData.asset
 └─ SceneType / BgmType / SeType の値が、Attack系(seType)や
    ExitCondition系(BgmPositionExitConditionConfig.thresholdSamples)と数値的に対応している。
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

`loopStartSamples`/`loopEndSamples` は後述する Boss 系の `BgmPositionExitConditionConfig.thresholdSamples` と同じ数値が使われることがあり、BGMの再生位置をトリガーにボス戦のフェーズを進行させる設計と対応している。

### SeData（`SeData.asset`）

- 定義: `Assets/Project/Scripts/Infra/SeDataObject.cs`

| フィールド | 型 | 説明 |
|---|---|---|
| `name` | `string` | SE名 |
| `seType` | `SeType` | `None, Click, Cancel, EnemyShotShort, EnemyShotLong, Attack, Charge, ChargeRelease, Damage, LocoAttack1, LocoAttack2` |
| `loopStartSamples` / `loopEndSamples` | `int` | ループ再生用（`PlayLoopSE`使用時のみ参照） |

`seType` は後述する Attack 系の `AttackTimelineEntry.seType` からも参照され、攻撃イベント発火時に鳴らすSEの指定に使われる。

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
| `id` | `string` | ステージID（"1"〜"6", "EX"など） |
| `stageNumber` | `int` | 数値のステージ番号 |
| `charaStillAddress` | `string` | ステージ画面に表示する立ち絵のアドレス（`CharacterData.englishName`と対応） |
| `title` | `string` | ステージタイトル |
| `waySequenceAddress` | `string` | 道中の `BattleSequenceAsset` へのパス |
| `bossSequenceAddress` | `string` | ボス戦の `BattleSequenceAsset` へのパス |

実データは7エントリ（Stage1〜6、EX）だが、`waySequenceAddress`/`bossSequenceAddress` が設定済みなのは Stage1・Stage2 のみで、Stage3以降は未実装（空文字）。

---

## 2. Way / Boss 系（`BattleSequenceAsset`）

道中（Way）とボス戦（Boss）は同一のクラス `BattleSequenceAsset` で表現される。

```csharp
// Assets/Project/Scenes/Battle/Scripts/Model/BattleSequenceAsset.cs
public class BattleSequenceAsset : ScriptableObject
{
    BattleSituation situation;                  // Way or Boss
    List<SequenceGroup> sequenceGroups;

    GameObject bossPrefab;                      // Boss用: ボスのPrefab
    Vector3 bossSpawnPosition;                  // Boss用: 出現位置
    List<IMovementStep> bossEntranceMovement;   // Boss用: 入場演出（SerializeReference）
}

public class SequenceGroup
{
    bool loop;
    int loopCount;                // 0 = 無限ループ
    List<BattlePhaseDefinition> phases;
}

public class BattlePhaseDefinition
{
    string phaseId;
    BattleTimelineBuilderAsset timelineBuilder;        // 通常
    BattleTimelineBuilderAsset timelineBuilderStrong;  // 強化(発狂)版。未設定可
    IExitConditionConfig exitConditionConfig;          // フェーズ終了条件（SerializeReference）
}
```

`situation` は `OnValidate()` によってエディタ上でアセット名に "Boss" / "Way" が含まれるかどうかで自動的に上書きされる（手動で矛盾した値を設定してもエディタが訂正してくれる）。

> **補足**: `BattlePhaseDefinition` は実データYAML上に `strongAttackHpThresholdPercent` という値が残存しているが、現行のクラス定義には存在しないフィールドである。Unityは未知のYAMLフィールドを無視するため実害はないが、過去のリファクタで削除された仕様の名残りと考えられる。

### フェーズ終了条件（`IExitConditionConfig`）

`Assets/Project/Scenes/Battle/Scripts/Model/ExitCondition/` 配下。

| クラス | フィールド | 用途 |
|---|---|---|
| `TimeLimitExitConditionConfig` | `float timeLimitSeconds` | 制限時間経過で終了 |
| `AllEnemiesDefeatedExitConditionConfig` | なし | 敵を全滅させたら終了 |
| `BgmPositionExitConditionConfig` | `int thresholdSamples` | BGMが指定サンプル位置に達したら終了 |
| `BossHpThresholdExitConditionConfig` | `float hpThresholdPercent`（0-100） | ボスHPが閾値を下回ったら終了 |
| `CompositeExitConditionConfig` | `CompositeMode mode`(And/Or), `List<IExitConditionConfig> conditions` | 複数条件の合成（入れ子可） |

### 実データ: Way（`Stage1/Way/Stage1Way.asset`）

`situation: Way`、`sequenceGroups` は1グループのみで `loop: false`。中に `phaseId 1〜6` の6フェーズがあり、各フェーズの `exitConditionConfig` はすべて `AllEnemiesDefeatedExitConditionConfig`（=敵を全滅させたら次フェーズへ）。

各フェーズの `timelineBuilder` は `Stage1/Way/Phase1.asset`〜`Phase6.asset` を参照する。中身は `BattleTimelineBuilderAsset` の `enemySpawnTracks` に、時刻と出現位置を指定した敵スポーン定義が並んでいるだけのシンプルな構成（例: `Phase1.asset` は12件のスポーン定義を持つ1トラック）。

`Way`フォルダには `Phase7〜9.asset` も存在するが、`Stage1Way.asset` 側は6フェーズしか参照していない。将来の拡張用に用意された未使用アセットと見られる。

> **補足**: WayとBossが同じ `BattleSequenceAsset` クラスを共有しているのは、「フェーズの列を順に消化し、条件を満たしたら次へ進む」という制御フローが道中もボス戦も本質的に同じであるため。差分は `bossPrefab` 等の追加フィールドと `bossAttackPreset`/`bossMovementPreset` の有無だけで表現されている。

### 実データ: Boss（`Stage1/Boss/Stage1Boss.asset`）

```
situation: Boss
sequenceGroups:
  - loop: false, loopCount: 1          # イントロ専用グループ（1回のみ）
    phases:
      - phaseId: 1, timelineBuilder: Intro.asset
        exitConditionConfig: BgmPositionExitConditionConfig(thresholdSamples: 422391)
  - loop: true, loopCount: 0            # 無限ループグループ
    phases:                             # MelodyA → MelodyB → Chorus
      - timelineBuilder/timelineBuilderStrong 両方設定（発狂版あり）
        exitConditionConfig: BgmPositionExitConditionConfig(thresholdSamples: 2976121 / 3897391 / 1263874)
bossPrefab: <ボスPrefab>
bossSpawnPosition: {x: 10, y: 0, z: 0}
bossEntranceMovement: [TweenMovementConfig(targetOffset:{3.7,0,0}, duration:2, ease:InOutQuad系, isRelative:false)]
```

ボス戦は「BGMの再生サンプル位置」でフェーズを進行させる設計になっている。`thresholdSamples` の値は `BgmData.asset` 内の対応曲（Stage1のBattleBoss曲、`loopStartSamples: 422391` / `loopEndSamples: 3897391`）と一致しており、イントロ→ループ境界→曲終端のタイミングに合わせてフェーズが切り替わる。無限ループグループが `MelodyA → MelodyB → Chorus` の3フェーズを繰り返すのも、BGM自体のループ構造をそのままフェーズ構造に対応させたものである。

`Intro.asset`（`BattleTimelineBuilderAsset`）はトラックがすべて空で、`bossAttackPreset` / `bossMovementPreset` だけが設定されている。つまり Boss 用の Timeline は Way 用と違い、**敵の攻撃パターンと移動パターンをフェーズごとに差し替える**のが主な役目になっている。

> **補足**: BGMのサンプル位置で戦闘フェーズを同期させる設計のため、`BgmData` 側の `loopStartSamples`/`loopEndSamples` と `thresholdSamples` の数値は必ず一致させる必要がある。片方だけ変更すると、BGMのループタイミングとフェーズ切り替えのタイミングがズレてしまう。

---

## 3. Attack系（弾幕・攻撃パターンプリセット）

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/Attack/AttackPreset.cs`
- `AttackPreset : ScriptableObject` は `AttackTimeline attackTimeline` を1つだけ持つ。`CreateTimeline()` で `DeepCopy()` した実行用インスタンスを生成する。

```csharp
[Serializable]
class AttackTimeline : IAttackStrategy
{
    bool loop;
    float loopStart;
    float loopEnd;          // 既定 5
    float cycleDuration;    // 既定 2。ループ時の1サイクルの長さ
    List<AttackTimelineEntry> entries;
}

[Serializable]
class AttackTimelineEntry
{
    float time;                                  // 発火時刻
    IAttackSignal signal;                         // 何を撃つか（SerializeReference）
    IDirectionProvider directionProvider;         // 方向をどう決めるか
    IRotationProvider rotationProvider;           // 回転をどう決めるか
    ISourceIndexProvider sourceIndexProvider;     // 発射口インデックス
    SeType seType;                                // 発射音
}
```

`loop: true` の場合、`loopStart`〜`loopEnd` の範囲を `cycleDuration` 間隔で繰り返し、各 `entry.time` オフセットで攻撃を発火する。

### `IAttackSignal`（何を撃つか）

| クラス | フィールド | 説明 |
|---|---|---|
| `SingleBulletSignal` | なし | 単発弾 |
| `NWaySignal` | `wayCount`(既定3), `spreadAngle`(既定60) | N-way弾 |
| `NWayEnemySpawnAttackSignal` | `wayCount`, `spreadAngle`, `offset` | N方向へ敵をスポーン |
| `EnemySpawnAttackSignal` | `offset` | 単一の敵スポーン |
| `PresetAttackSignal` | `preset`, `loop`, `loopCount`, `cycleDuration` | 他の`AttackPreset`をネストして展開する合成ノード（最大深さ8、循環参照防止あり） |

### 方向・回転・発射口の決定方式

- `IDirectionProvider`: `FixedDirectionConfig`（固定方向）/ `AimDirectionConfig`（自機狙い）/ `ForwardDirectionConfig`（親の正面）
- `IRotationProvider`: `FixedRotationConfig` / `AimRotationConfig` / `OffsetRotationConfig` / `InheritParentRotationConfig`
- `ISourceIndexProvider`: `ConstantSourceIndex` / `RandomSourceIndex`

### 実データ: `Stage1/Attack/IntroNormal.asset`

```
attackTimeline: loop:true, loopStart:1, loopEnd:10, cycleDuration:1
  entries[0]: time:0
    signal: NWaySignal(wayCount:3, spreadAngle:30)
    directionProvider: AimDirectionConfig（自機狙い）
```

1〜10秒の間、1秒おきに自機狙いの3-way弾（拡散30度）を撃ち続ける設定。

> **補足**: `PresetAttackSignal` による「プリセットのネスト」は、弾幕STGでよくある「基本パターンを組み合わせて複雑なパターンを作る」ためのコンポジション設計。最大深さ8・循環参照防止が実装されており、`AttackPreset` 同士が意図せず互いを参照してしまう事故を想定済みである。

---

## 4. Movement系（敵の移動プリセット）

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/Movement/MovementPreset.cs`
- `MovementPreset : ScriptableObject` は `List<IMovementStep> steps` を持ち、各ステップを順番に実行する（Sequence的な構成）。
- `IMovementStep.Play(Transform target, Vector2 direction, Animator animator) : Tween` — DOTweenの`Tween`を返し、呼び出し側でSequenceに繋げる想定。

| クラス | 主なフィールド | 説明 |
|---|---|---|
| `TweenMovementConfig` | `targetOffset`, `duration`, `easeValue`, `customCurve`, `curvePreset`, `isRelative` | DOTweenネイティブ移動。`isRelative`で相対/絶対移動を切替 |
| `PathMovementConfig` | `waypoints[]`, `duration`, `pathType`(CatmullRom/Linear), `easeValue`, `curvePreset` | 複数ウェイポイント経由の移動 |
| `WaitMovementConfig` | `duration` | その場で待機 |
| `ForwardMovementConfig` | `speed`, `duration` | 現在の向き（右方向）へ直進 |
| `AimStraightMovementConfig` | `speed`, `duration` | 自機方向へ直進 |
| `AimRotateConfig` | `duration`, `ease` | 自機方向へ回転のみ（位置は不変） |
| `AcceleratedMovementConfig` | `direction`, `initialSpeed`, `acceleration`, `maxSpeed`, `minSpeed`, `duration` | 加減速移動 |
| `QuadraticMovementConfig` | `initialVelocity`, `acceleration`, `duration` | 放物線移動 |
| `SineMovementConfig` | `baseVelocity`, `amplitude`, `frequency`, `duration` | 基準速度＋直交軸のサイン波揺れ |
| `InfiniteMovementConfig` | `velocity` | 無限に等速移動 |
| `DriftMovementConfig` | `anchor`, `pullStrength`, `moveDistance`, `moveBounds`, `moveDuration`, `pauseDuration`, `moveEase`, `duration` | anchor中心に「移動→停止」をランダム方向に繰り返す居座り移動 |
| `ReflectMovementConfig` | `speed`, `maxReflections`, `reflectTop/Bottom/Left/Right` | 画面端で反射する移動 |
| `AnimationMovementConfig` | `clip`, `layerIndex` | 指定アニメクリップを再生し終了まで待機 |

### `LoopMovementConfig`（複合ループ移動）

```csharp
class LoopMovementConfig : IMovementStep
{
    bool loop;                        // 既定 true
    int loopCount;                    // 既定 0 = 無限
    List<LoopMovementEntry> entries;
}

class LoopMovementEntry
{
    MovementPreset preset;                  // プリセット参照（優先）
    List<IMovementStep> inlineSteps;        // プリセット未指定時のインラインステップ
    int repeatCount;                        // 既定 1。このエントリの繰り返し回数
}
```

「プリセットA×2回 → プリセットB×1回」のような組み合わせを構成でき、ボスの周回パターンなどに使われる。

### 実データ: `Stage1/Movement/Straight1.asset`

```
1. TweenMovementConfig(targetOffset:{-5,0,0}, duration:1, ease:Linear, isRelative:true)
2. WaitMovementConfig(duration:6)
3. TweenMovementConfig(targetOffset:{5,0,0}, duration:1, ease:Linear, isRelative:true)
```

左へ5進む → 6秒待機 → 右へ5戻る、という往復移動。

### 実データ: `Stage1/Movement/DriftSample.asset`

```
1. TweenMovementConfig(targetOffset:{6,0,0}, duration:1.5, ease:InQuad系, isRelative:false)   # (6,0,0)へ入場
2. WaitMovementConfig(duration:1.5)
3. DriftMovementConfig(anchor:{6,0,0}, pullStrength:10, moveDistance:3, moveBounds:{2,5},
                        moveDuration:2, pauseDuration:10, moveEase:OutQuad系, duration:40)   # 40秒間ドリフト
4. TweenMovementConfig(targetOffset:{12,0,0}, duration:3, ease:InOutQuad系, isRelative:false) # (12,0,0)へ退場
```

「入場 → 居座り → 退場」という、ボス敵の典型的な移動パターンの実例。

---

## 5. Ease系（イージングカーブプリセット）

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/Movement/EaseCurvePreset.cs`
- `EaseCurvePreset : ScriptableObject` は `AnimationCurve curve` を1つ持つだけ（既定はLinear）。

`TweenMovementConfig`/`PathMovementConfig` の `easeValue` がカスタムカーブを示す値の場合、`curvePreset`（優先）または `customCurve` がイージングとして適用される。同じカーブを複数の移動アセット間で使い回すための共有プリセットである。

実データ `Stage1/Ease/InQuadOutLinear.asset` は3キーフレームのカスタムカーブで、名前の通り「入りはQuad的、抜けはLinear的」な挙動を表現している。

---

## 6. Scenario系（会話イベント・演出シナリオ）

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

生成された `.asset` は `Assets/Project/DataStore/{ファイル名}.asset` として保存される（`Stage1WayScenario.asset` などはこの仕組みで生成されたもの）。

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

## 7. UserData.json（セーブデータ）

- 定義: `Assets/Project/Scripts/Infra/UserData.cs`（`JsonUtility`でシリアライズする素のC#クラス。ScriptableObjectではない）

| フィールド | 型 | 説明 |
|---|---|---|
| `clearedStageNumber` | `int` | クリア済みステージ番号（`Validate()`で0〜1にClampされる） |
| `keyConfigData` | `KeyConfigData` | キーコンフィグ。`bindingOverrides: List<BindingOverride>`（`actionPath`, `bindingId`, `overridePath`）としてInput Systemのバインディング上書き情報を保持 |
| `bgmVolume` / `seVolume` | `int` | 音量（0〜100、既定70） |
| `hasEnteredStage1` | `bool` | チュートリアルスキップ判定用フラグ |

管理クラスは `Assets/Project/Scripts/Model/UserModel.cs`。

- 保存先: Editorでは `Assets/Project/DataStore/UserData.json`、実機では `Application.persistentDataPath/DataStore/UserData.json`。
- `Load()`: ファイルが無ければ既定値で新規作成・保存。あれば`FromJsonOverwrite`で既定値インスタンスに上書きロードする（欠損フィールドを既定値で補完できる）。
- `Save()`: `JsonUtility.ToJson(userData, true)` で保存。
- `StageClear(stageNumber)` / `MarkEnteredStage1()` / `SetVolume(...)` などのゲームロジック側APIを提供する。

`KeyConfigModel`（`Assets/Project/Scripts/Model/KeyConfigModel.cs`）が `keyConfigData` を使い、Input Systemの `InputActionAsset` とのバインディングオーバーライド同期を行う。

---

## 8. Test配下（テスト用データ）

開発時の動作確認用に用意された最小構成のデータ。

- `Test/TestSequence.asset`: `BattleSequenceAsset`（`situation: Way`）。1フェーズのみで `exitConditionConfig` は `AllEnemiesDefeatedExitConditionConfig`。`timelineBuilder` は `TestTimeline.asset` を参照。
- `Test/TestTimeline.asset`: `BattleTimelineBuilderAsset`。`enemySpawnTracks` に1トラック・1エントリのみを持つ。

---

## 9. BattleTimelineBuilderAsset とトラック定義（Way/Boss共通の中核）

```csharp
// Assets/Project/Scenes/Battle/Scripts/Model/BattleTimelineBuilderAsset.cs
public class BattleTimelineBuilderAsset : ScriptableObject
{
    List<SignalTrackDefinition> signalTracks;
    List<AnimationTrackDefinition> animationTracks;
    List<ActivationTrackDefinition> activationTracks;
    List<AudioTrackDefinition> audioTracks;
    List<ControlTrackDefinition> controlTracks;
    List<EnemySpawnTrackDefinition> enemySpawnTracks;
    List<BulletClearTrackDefinition> bulletClearTracks;

    AttackPreset bossAttackPreset;      // Boss用途のみ意味を持つ
    MovementPreset bossMovementPreset;  // Boss用途のみ意味を持つ

    public TimelineAsset BuildTimeline();  // 各トラック定義から実行時TimelineAssetを動的生成
}
```

各トラック定義は共通基底 `TrackDefinitionBase<TTrack, TClipDefinition>`（`Definitions/TrackDefinitionBase.cs`）を継承し、`trackName` と `clips` を持つ。

| トラック定義 | 生成されるTrack | クリップの主なフィールド | 説明 |
|---|---|---|---|
| `SignalTrackDefinition` | `SignalTrack` | `time`, `signal`(既存の`SignalAsset`) | 汎用シグナル発火 |
| `AnimationTrackDefinition` | `AnimationTrack` | `start`, `duration`, `animationClip` | アニメーション再生 |
| `ActivationTrackDefinition` | `ActivationTrack` | `start`, `duration` | GameObjectのActive切替 |
| `AudioTrackDefinition` | `AudioTrack` | `start`, `duration`, `audioClip` | 音声再生 |
| `ControlTrackDefinition` | `ControlTrack` | `start`, `duration`, `sourceObject` | Timeline Control Track（Prefab制御） |
| `EnemySpawnTrackDefinition` | `SignalTrack` | `time`, `spawnPosition`, `prefab` | 敵の出現。ビルド時に`EnemySpawnSignal`を動的生成 |
| `BulletClearTrackDefinition` | `SignalTrack` | `time` のみ | 画面上の弾を一掃するタイミング。ビルド時に`BulletClearSignal`を動的生成 |

`BuildTimeline()` はこれらの定義から `HideFlags.DontSave` の使い捨て `TimelineAsset` を生成する。ScriptableObjectのアセットデータ自体は「設計図」であり、実際のバトル進行時にはこの設計図から都度Timelineが組み立てられる、という点が重要である。

> **補足**: `EnemySpawnTrackDefinition` や `BulletClearTrackDefinition` が最終的に `SignalTrack` へ変換されているのは、Unity Timelineに「敵をスポーンさせるTrack」や「弾を消すTrack」が標準で存在しないため。汎用の `SignalTrack` + 動的生成した `SignalAsset`（`EnemySpawnSignal`/`BulletClearSignal`）で代用し、Presenter側で `SignalReceiver` として購読する設計になっていると考えられる。標準Trackで表現できない独自のゲームロジックをTimelineに載せる際の定番テクニックである。

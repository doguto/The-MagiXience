# プリセット（Attack/Movement/Ease）とTimelineBuilder

## 全体の依存関係

```
BattleTimelineBuilderAsset (1フェーズ分)
├─ enemySpawnTracks: List<EnemySpawnTrackDefinition>   # 道中で主に使用
├─ signalTracks / animationTracks / activationTracks / audioTracks
│   / controlTracks / bulletClearTracks                # 演出用トラック（必要な分だけ使う）
├─ bossAttackPreset: AttackPreset      # ボス専用。このフェーズ中のボスの弾幕
└─ bossMovementPreset: MovementPreset  # ボス専用。このフェーズ中のボスの移動
```

`BattleTimelineBuilderAsset.BuildTimeline()` は実行時に空の `TimelineAsset` を生成し、各トラック定義から `UnityEngine.Timeline` の `SignalTrack` / `AnimationTrack` / `ActivationTrack` / `AudioTrack` / `ControlTrack` を動的に組み立てる。つまり **エディタ上でTimelineウィンドウを直接編集するのではなく、`BattleTimelineBuilderAsset`のフィールド（トラック定義のリスト）を編集することでTimelineが間接的に構築される**設計。

## トラック定義の仕組み

各トラック種別は `Assets/Project/Scenes/Battle/Scripts/Model/Definitions/` 配下に `TrackDefinitionBase<TTrack, TClipDefinition>` を継承する形で定義されている（`SignalTrackDefinition` `EnemySpawnTrackDefinition` `AudioTrackDefinition` `ActivationTrackDefinition` `ControlTrackDefinition` `BulletClearTrackDefinition`）。共通構造:

```csharp
[Serializable]
public class XxxTrackDefinition : TrackDefinitionBase<TTrack, XxxDefinition>
{
    // trackName: string, clips: List<XxxDefinition> を継承
}

[Serializable]
public class XxxDefinition : IClipDefinition<TTrack>
{
    [SerializeField] double time;  // 各クリップ定義は共通してtime(秒)を持つ
    // ...種別固有のフィールド
    public void Build(TTrack track) { /* SignalEmitter等をtrack上に生成 */ }
}
```

道中の敵スポーンで使う `EnemySpawnDefinition` は `time` / `spawnPosition` / `prefab` の3フィールドのみで、内部的に `EnemySpawnSignal` を生成して `SignalTrack` 上に配置する（実際にゲーム内で敵を生成するのは、このSignalを受信する側のリスナー）。他のトラック種別（Audio, Activation, Control, BulletClear）も同様に「`time` + 種別固有フィールド」の構造なので、新しい演出を組む際は該当する `Definitions/*.cs` を直接読んで必要なフィールドを確認すること。

## AttackPreset（ボスの弾幕）

`Assets/Project/Scenes/Battle/Scripts/Model/Attack/AttackPreset.cs`:

```csharp
[CreateAssetMenu(fileName = "AttackPreset", menuName = "Battle/Attack Preset")]
public class AttackPreset : ScriptableObject
{
    [SerializeField] AttackTimeline attackTimeline;
    public AttackTimeline CreateTimeline() => attackTimeline?.DeepCopy();
}
```

実体は `AttackTimeline`（弾幕のタイミング・弾種・発射方向などを定義する型。`Assets/Project/Scenes/Battle/Scripts/Model/Attack/` 配下で `AttackTimeline` の定義とその構成要素を確認すること）。`BossEntityPresenter.ApplyAttackTimeline()` が `AttackTimeline.InitializeProviders()` でプレイヤー位置・自機位置・向きを注入し、`OnAttackTiming` イベント経由で `AttackEventType.Bullet`（弾発射）/ `AttackEventType.EnemySpawn`（子機スポーン）を発火する。弾幕を新規に組む場合は、Stage1の `Attack/IntroNormal.asset` 等をUnity MCPで開いて既存パターンの構造を確認しながら作るのが最も確実。

命名規則: `{楽節名}Normal.asset`（通常） / `{楽節名}Strong.asset`（発狂）。

## MovementPreset（ボスの移動パターン）

```csharp
[CreateAssetMenu(fileName = "MovementPreset", menuName = "Battle/Movement Preset")]
public class MovementPreset : ScriptableObject
{
    [SerializeReference, SubclassSelector] List<IMovementStep> steps = new();
}
```

`IMovementStep`（`Assets/Project/Scenes/Battle/Scripts/Model/Movement/IMovementStep.cs`）:

```csharp
public interface IMovementStep
{
    Tween Play(Transform target, Vector2 direction, Animator animator);  // DOTweenのTweenを返す
}
```

`steps` に並べた順に `DOTween.Sequence` へ `Append` されて連続再生される（`BossEntityPresenter.RunMovementStepsAsync`）。利用可能な実装は `Assets/Project/Scenes/Battle/Scripts/Model/Movement/` 配下に多数存在する（`TweenMovementConfig` 直線/曲線移動、`ReflectMovementConfig` 反射移動、`LoopMovementConfig` ループ移動、`WaitMovementConfig` 待機、`SineMovementConfig` サイン波移動、`QuadraticMovementConfig` 二次曲線移動、`PathMovementConfig` パス移動、`InfiniteMovementConfig` 無限移動、`ForwardMovementConfig` 前進、`DriftMovementConfig` ドリフト、`AnimationMovementConfig` アニメーション連動、`AimStraightMovementConfig` 自機狙い直線、`AimRotateConfig` 自機狙い回転、`AcceleratedMovementConfig` 加速移動）。新規移動パターンを組む際はまず既存実装の組み合わせで表現できないか検討し、表現できない場合のみ新しい `IMovementStep` 実装（.cs追加）を検討する。

`LoopMovementConfig` は `ForceStop()` を持ち、`BossEntityPresenter.StopMovement()` （フェーズ切り替え時やRetry時）から明示的に停止される特殊なステップなので、ループ移動を多用する場合はこの停止経路も意識すること。

## EaseCurvePreset（イージングカーブ）

```csharp
[CreateAssetMenu(fileName = "NewEaseCurvePreset", menuName = "Battle/Ease Curve Preset")]
public class EaseCurvePreset : ScriptableObject
{
    [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}
```

`AnimationCurve` 1本だけを保持するシンプルなプリセット。`TweenMovementConfig` 等、カーブベースの移動ステップから参照される想定。Stage1の例: `InLinearOutQuad.asset`（入りは線形・抜けは二次減速）、`InQuadOutLinear.asset`（逆）、`MidStop.asset`（中間で一旦停止するようなカーブ）。Unity MCPでAnimationCurveをInspector上で編集する。

## 作成順序の推奨

1. 必要なら `EaseCurvePreset` を先に用意する（Movement/Attackから参照されるため）。
2. `MovementPreset` / `AttackPreset` を用意する（ボスの場合のみ）。
3. `BattleTimelineBuilderAsset` を作り、`enemySpawnTracks` 等のトラックを埋め、ボス用途なら `bossAttackPreset` / `bossMovementPreset` を割り当てる。
4. `BattleSequenceAsset` 側の `BattlePhaseDefinition` から、作成した `BattleTimelineBuilderAsset` と `ExitConditionConfig` を組み合わせる。

いずれもUnity MCP経由でCreateAssetMenuからアセットを新規作成し、Inspector相当の操作でフィールドを設定すること（`.asset`の直接編集は禁止）。

# Battle Timeline 構造解説

このドキュメントは、バトルシーンにおける Unity Timeline (`UnityEngine.Timeline`) の動的生成・再生・イベント連携の仕組みについて解説する。[DataStore.md](./DataStore.md) の「8. BattleTimelineBuilderAsset とトラック定義」で触れた内容を、実行時のフロー（Presenter層でどう再生・Bindingされ、Signalがどう処理されるか）まで含めて掘り下げたもの。

> **注意**: `Assets/Project/Scenes/Battle/Documents/` 配下に開発初期の `フェーズ管理基盤実装.md` / `シーケンス図など？.md` が存在するが、`Stage1SignalReceiver` という単一クラスや `EnemySpawnSignal.MaxHp` フィールド、`extrapolationMode = Loop` など、現在のコードと食い違う記述が残っている（実際は `EnemySpawnReceiver`/`BulletClearReceiver` に分離済み、`EnemySpawnSignal` は `prefab` を持つ、`extrapolationMode` は `Hold`）。本ドキュメントは現在のソースコードを正として書いているので、食い違いがあれば本ドキュメントとソースコードを優先すること。

## 全体像

```
BattleTimelineBuilderAsset（設計図・ScriptableObject）
 └─ BuildTimeline() ──► TimelineAsset（HideFlags.DontSave の使い捨てインスタンス）

BattlePhaseStateMachine（Presenter層）
 ├─ phase.ResolveTimeline() / ResolveTimelineStrong() で TimelineAsset を取得
 ├─ playableDirector.playableAsset = timeline
 ├─ BattleTimelineBindingMap.ApplyBindings() でトラック名 → 実オブジェクトを紐付け
 └─ playableDirector.Play()

Timeline再生中
 └─ SignalEmitter発火 ──► INotificationReceiver実装（EnemySpawnReceiver / BulletClearReceiver 等）
```

「Timelineウィンドウでアセットを直接編集する」のではなく、**`BattleTimelineBuilderAsset` のフィールド（トラック定義のリスト）をインスペクタで編集し、実行時にそこから `TimelineAsset` を都度組み立てる**、という間接的な構成になっているのが最大の特徴。

---

## 1. `BattleTimelineBuilderAsset` — Timeline生成エンジン

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/BattleTimelineBuilderAsset.cs`

```csharp
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

    public TimelineAsset BuildTimeline();
}
```

`BuildTimeline()` は空の `TimelineAsset` を `HideFlags.DontSave` で生成し、7種類のトラック定義それぞれについて総称メソッド `BuildTracks<TTrack, TClipDefinition, TDefinition>()` を呼び出す。

```csharp
void BuildTracks<TTrack, TClipDefinition, TDefinition>(TimelineAsset timeline, List<TDefinition> trackDefinitions)
    where TTrack : TrackAsset, new()
    where TClipDefinition : IClipDefinition<TTrack>
    where TDefinition : TrackDefinitionBase<TTrack, TClipDefinition>
{
    foreach (var trackDef in trackDefinitions)
    {
        if (string.IsNullOrEmpty(trackDef.TrackName)) continue;
        var track = timeline.CreateTrack<TTrack>(null, trackDef.TrackName);
        trackDef.Build(track);
    }
}
```

`trackDef.TrackName` が空文字の場合はスキップされる。**`TrackName` は後述の `BattleTimelineBindingMap` が対象オブジェクトを検索するキーとしても使われるため、Bindingが必要なトラックには必ず名前を付ける必要がある。**

`HideFlags.DontSave` により、生成された `TimelineAsset` はシーン保存対象にならず、実行のたびに新規生成・破棄されるランタイム専用オブジェクトとして扱われる（永続化されるのはあくまで `BattleTimelineBuilderAsset` という「設計図」側だけ）。

`✶ Insight`: `BuildTracks<>` がジェネリックで1メソックに集約されているのは、7種類のトラック種別すべてが「`TrackName` + `List<クリップ定義>` を持ち、各クリップ定義が `Build(track)` を実装する」という共通構造（`TrackDefinitionBase<TTrack, TClipDefinition>` / `IClipDefinition<TTrack>`）に従っているため。新しいトラック種別を追加する際も、この2つの制約さえ満たせば `BuildTimeline()` 自体には手を入れずに済む拡張性の高い設計になっている。

---

## 2. トラック定義とクリップ定義の共通構造

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/Definitions/TrackDefinitionBase.cs`

```csharp
public interface IClipDefinition<in TTrack> where TTrack : TrackAsset
{
    void Build(TTrack track);
}

public abstract class TrackDefinitionBase<TTrack, TClipDefinition>
    where TTrack : TrackAsset
    where TClipDefinition : IClipDefinition<TTrack>
{
    string trackName;
    List<TClipDefinition> clips;

    public virtual void Build(TTrack track)
    {
        foreach (var clipDef in clips) clipDef.Build(track);
    }
}
```

各トラック種別は `Definitions/` 配下でこれを継承した薄いクラス（中身は空）として定義されており、実体は各クリップ定義側の `Build(track)` にある。

| トラック定義 | 生成される`TrackAsset` | クリップ定義の主なフィールド | `Build()`の中身 |
|---|---|---|---|
| `SignalTrackDefinition` | `SignalTrack` | `time`, `signal`(既存の`SignalAsset`参照) | `track.CreateMarker<SignalEmitter>(time)` に既存の`signal`を割り当てるだけ |
| `AnimationTrackDefinition` | `AnimationTrack` | `start`, `duration`, `animationClip` | `AnimationPlayableAsset`のクリップを作成。`duration<=0`なら`animationClip.length`を使う |
| `ActivationTrackDefinition` | `ActivationTrack` | `start`, `duration`(既定1.0) | `CreateDefaultClip()`のみ。GameObjectのActive切替はUnity標準の`ActivationTrack`の挙動に委ねる |
| `AudioTrackDefinition` | `AudioTrack` | `start`, `duration`, `audioClip` | `AudioPlayableAsset`のクリップを作成。`duration<=0`なら`audioClip.length`を使う |
| `ControlTrackDefinition` | `ControlTrack` | `start`, `duration`(既定1.0), `sourceObject` | `ControlPlayableAsset.prefabGameObject`に`sourceObject`を割り当て（再生区間中Prefabをアクティブ化・制御するUnity標準機能） |
| `EnemySpawnTrackDefinition` | `SignalTrack` | `time`, `spawnPosition`, `prefab` | `EnemySpawnSignal`を動的生成して`signal.SetProperties()`で値を注入し、`SignalEmitter`に割り当てる |
| `BulletClearTrackDefinition` | `SignalTrack` | `time` のみ | `BulletClearSignal`を動的生成して`SignalEmitter`に割り当てる |

`EnemySpawnTrackDefinition` と `BulletClearTrackDefinition` は最終的に生成される `TrackAsset` が両方とも `SignalTrack` である点に注目。Unity Timelineには「敵をスポーンさせるTrack」や「弾を一掃するTrack」が標準で存在しないため、汎用の `SignalTrack` + そのつど動的生成する専用の `SignalAsset`（`EnemySpawnSignal` / `BulletClearSignal`）で代用している。標準トラックで表現できない独自ゲームロジックをTimelineに載せる際の定番テクニック。

---

## 3. `BattlePhaseModelBase` — TimelineAssetの解決とキャッシュ

- 定義: `Assets/Project/Scenes/Battle/Scripts/Model/BattlePhaseModelBase.cs`

```csharp
public abstract class BattlePhaseModelBase : IDisposable
{
    TimelineAsset resolvedTimeline;
    bool isTimelineResolved;
    TimelineAsset resolvedTimelineStrong;
    bool isTimelineStrongResolved;

    public TimelineAsset ResolveTimeline()       // 通常版。初回呼び出し時に Definition.CreateTimeline() を実行しキャッシュ
    public TimelineAsset ResolveTimelineStrong() // 発狂版。同様に CreateTimelineStrong() をキャッシュ

    public virtual void Dispose()
    {
        if (resolvedTimeline)       UnityEngine.Object.Destroy(resolvedTimeline);
        if (resolvedTimelineStrong) UnityEngine.Object.Destroy(resolvedTimelineStrong);
    }
}
```

`ResolveTimeline()` / `ResolveTimelineStrong()` はどちらも一度呼ばれるとフラグが立ち、以降は同じインスタンスを返す（`BuildTimeline()` を毎フレーム呼び直すような無駄を避けるための遅延初期化＋キャッシュ）。`Dispose()` で明示的に `Object.Destroy()` しているのは、`HideFlags.DontSave` なオブジェクトはシーン遷移などで自動的に破棄されないため。フェーズ切り替え時（`BattlePhaseStateMachine.DisposeSequence()`）に全フェーズの `Dispose()` が呼ばれ、リークを防いでいる。

`BattlePhaseDefinition`（`BattleSequenceAsset.cs`）側は以下のように、それぞれ対応する `BattleTimelineBuilderAsset` から `BuildTimeline()` を呼ぶだけの薄いファクトリになっている。

```csharp
public TimelineAsset CreateTimeline()       => timelineBuilder       ? timelineBuilder.BuildTimeline()       : null;
public TimelineAsset CreateTimelineStrong() => timelineBuilderStrong ? timelineBuilderStrong.BuildTimeline() : null;
```

`timelineBuilderStrong` が未設定のフェーズ（Way側は基本すべて）では `CreateTimelineStrong()` は常に `null` を返す。

---

## 4. `BattlePhaseStateMachine` — 再生制御

- 定義: `Assets/Project/Scenes/Battle/Scripts/Presenter/BattlePhaseStateMachine.cs`

`PlayableDirector` と `BattleTimelineBindingMap` を保持し、フェーズが切り替わるたびに以下の `ApplyTimeline()` を実行する。

```csharp
void ApplyTimeline(BattlePhaseModelBase phase)
{
    var timeline = timelineResolver?.Invoke(phase) ?? phase.ResolveTimeline();
    if (!timeline) return;

    playableDirector.playableAsset = timeline;
    playableDirector.time = 0;
    playableDirector.Evaluate();
    playableDirector.extrapolationMode = DirectorWrapMode.Hold;

    if (bindingMap) bindingMap.ApplyBindings(playableDirector, timeline);

    playableDirector.Play();
}
```

ポイントは3つ:

1. **`timelineResolver`** は `SetTimelineResolver(Func<BattlePhaseModelBase, TimelineAsset>)` で外部（`BattleScenePresenter`）から差し込める委譲フック。未設定時は素直に `phase.ResolveTimeline()`（通常版）を使う。ボス戦ではこのフックを使って通常/発狂を切り替える（[5章](#5-発狂strongtimelineの切り替え)参照）。
2. **`playableDirector.time = 0; Evaluate();`** で明示的に先頭フレームを評価してから `Play()` している。これは新しい `playableAsset` を設定した直後は前フェーズの状態が1フレーム残ってしまう可能性があるため、切り替わり時の描画が一瞬乱れるのを防ぐための処理と考えられる。
3. **`extrapolationMode = DirectorWrapMode.Hold`** — Timelineの尺を超えた後は最終フレームの状態を保持する設定。フェーズの終了はTimelineの尺そのものではなく `IExitConditionConfig`（[DataStore.md](./DataStore.md#フェーズ終了条件iexitconditionconfig)側）が独立して判定するため、Timeline側が尺を超えても暴走・巻き戻りせず「そのまま止まって次の指示を待つ」動きになる。

---

## 5. 発狂(Strong)Timelineの切り替え

- 定義: `Assets/Project/Scenes/Battle/Scripts/Presenter/BattleScenePresenter.cs`（`SpawnBoss()`内）

ボスがスポーンするタイミングで、`phaseStateMachine.SetTimelineResolver()` に以下のロジックが差し込まれる。

```csharp
phaseStateMachine.SetTimelineResolver(phase =>
{
    if (ShouldUseStrongAttack(phase))
        return phase.ResolveTimelineStrong();
    return phase.ResolveTimeline();
});

bool ShouldUseStrongAttack(BattlePhaseModelBase phase)
{
    if (phase.BuilderStrong == null || bossPresenter?.Model == null) return false;
    return bossPresenter.Model.ShouldUseStrongAttack;
}
```

`BossEntityModel.ShouldUseStrongAttack => normalHp.Value <= 0` （`Assets/Project/Scenes/Battle/Scripts/Model/Entity/BossEntityModel.cs`）。つまり**ボスの通常HPが0以下になった瞬間から、以降に解決されるすべてのフェーズで自動的に `timelineBuilderStrong` 側が使われるようになる**。`OnPhaseStarted` の購読側でも同じ条件で `bossPresenter.Model.EnterStrongMode()` を呼び、`useStrong ? phase.BuilderStrong : phase.Builder` を選んで `bossPresenter.OnPhaseStarted()`（`bossAttackPreset`/`bossMovementPreset`の適用）に渡している。

`✶ Insight`: Way側（道中）にはこの切り替えロジックが一切関与しない。`ShouldUseStrongAttack()` が `bossPresenter?.Model == null` の場合に即座に`false`を返すガードを持っているのは、Way再生中はまだボスが存在しないため。**この判定が「HPが尽きたその場でTimelineを切り替える」のではなく「次にフェーズ遷移した時に切り替わる」設計**であることは、Boss側の`BattleTimelineBuilderAsset`を設計する際に意識しておくとよい（DataStore.mdの`BossHpThresholdExitConditionConfig`と組み合わせ、HP閾値到達＝フェーズ終了条件にもなっている構成が多い）。

---

## 6. `BattleTimelineBindingMap` — トラック名ベースのBinding

- 定義: `Assets/Project/Scenes/Battle/Scripts/Presenter/BattleTimelineBindingMap.cs`

```csharp
public class BattleTimelineBindingMap : MonoBehaviour
{
    List<BindingEntry> bindings;  // { trackName: string, target: UnityEngine.Object }

    public void ApplyBindings(PlayableDirector director, TimelineAsset timeline)
    {
        foreach (var output in timeline.outputs)
        {
            var target = GetBinding(output.streamName);  // streamName = トラック名
            if (!target) continue;
            director.SetGenericBinding(output.sourceObject, target);
        }
    }
}
```

`timeline.outputs` は「動的生成されたトラック（`TrackAsset`）」の列で、`streamName` にはトラック生成時に渡した `trackName` がそのまま入っている。`bindings` リストに `trackName`（完全一致・大小文字区別あり）と `target` の組をあらかじめ登録しておけば、**動的生成された `TimelineAsset` に対しても、コード変更なしにトラック名だけで対象オブジェクトを自動解決できる**。

Bindingが必要な代表的なトラック:

| トラック名（例） | Target | 対応するトラック種別 |
|---|---|---|
| ボスのAnimatorが付くGameObject | `AnimationTrack` |
| `AudioSource` | `AudioTrack` |
| `INotificationReceiver`実装（後述）が付くGameObject | `SignalTrack`（`SignalTrackDefinition`/`EnemySpawnTrackDefinition`/`BulletClearTrackDefinition`いずれも） |

`ActivationTrack` / `ControlTrack` はそれぞれ標準機能（Active切替・Prefab制御）で完結するため、通常は明示的なBindingを必要としない。

---

## 7. Signal受信 — `EnemySpawnReceiver` / `BulletClearReceiver`

Timeline上の `SignalEmitter` が発火すると、その `SignalTrack` にバインドされたGameObject上の `INotificationReceiver` 実装すべてに `OnNotify(origin, notification, context)` が通知される。バトルシーンでは種別ごとに専用のレシーバーが用意されている。

### `EnemySpawnReceiver`（`Assets/Project/Scenes/Battle/Scripts/Presenter/EnemySpawnReceiver.cs`）

```csharp
public class EnemySpawnReceiver : MonoBehaviour, INotificationReceiver
{
    [SerializeField] EnemyTracker enemyTracker;

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is not SignalEmitter emitter) return;
        if (emitter.asset is Model.EnemySpawnSignal signal) SpawnEnemy(signal);
    }

    void SpawnEnemy(Model.EnemySpawnSignal signal)
    {
        var instance = Instantiate(signal.Prefab, signal.SpawnPosition, Quaternion.identity);
        var presenter = instance.GetComponent<IEntityPresenter>();
        if (presenter is EnemyEntityPresenter enemyPresenter && enemyTracker != null)
            enemyTracker.RegisterEnemy(enemyPresenter);
    }
}
```

`notification is not SignalEmitter` / `emitter.asset is EnemySpawnSignal` の二段構えの型チェックにより、同じGameObjectに複数種のSignalTrackがバインドされていても、自分に関係のないSignal（例: `BulletClearSignal`）は無視して黙って抜ける。生成したPrefabが `EnemyEntityPresenter` を持つ場合のみ `EnemyTracker` に登録され、これが `AllEnemiesDefeatedExitConditionConfig`（DataStore.md参照）の生存数判定に使われる。**ボス子機など`EnemyEntityPresenter`以外の`IEntityPresenter`実装をスポーンさせても、敵撃破カウントの対象にはならない**という点は設計上の注意点。

### `BulletClearReceiver`（`Assets/Project/Scenes/Battle/Scripts/Presenter/BulletClearReceiver.cs`）

```csharp
public class BulletClearReceiver : MonoBehaviour, INotificationReceiver
{
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is not SignalEmitter emitter) return;
        if (emitter.asset is not Model.BulletClearSignal) return;

        ClearAllBullets();  // FindObjectsByType<BulletEntityPresenter>() → ReturnToPool()
        ClearAllEnemies();  // FindObjectsByType<EnemyEntityPresenter>() → Destroy()
    }
}
```

`BulletClearSignal` は固有フィールドを持たないマーカー的なSignalで、発火するとシーン上の弾をすべてプールに返却し、敵をすべて破壊する。フェーズの節目（曲の切れ目やボスの入場前など）で画面をリセットする演出に使われる。`FindObjectsByType` によるシーン走査なので、呼び出し頻度が高い演出（例: ループ内で毎サイクル呼ぶ等）には向かない設計であることは留意しておくとよい。

---

## 8. 新しいTimeline演出を組む際の実務フロー

1. `BattleTimelineBuilderAsset` を新規作成し、必要なトラック（`enemySpawnTracks`が最も一般的）にクリップを追加する。
2. Bindingが必要なトラック（`AnimationTrack`/`AudioTrack`/独自Signal用の`SignalTrack`など）には、シーン上の実オブジェクトと一致させる`trackName`を付ける。
3. シーン上の `BattleTimelineBindingMap` に、その`trackName`と対象オブジェクトの組を登録する（一度登録すれば、以降そのシーンで生成される全フェーズのTimelineから同じ名前で解決される）。
4. `BattlePhaseDefinition.timelineBuilder`（発狂させたい場合は`timelineBuilderStrong`も）に割り当てる。

`.asset`/`.prefab`/`.unity`/`.meta` の直接編集は禁止のため、実際の作成作業はUnity MCP経由で行うこと（詳細な手順は `msw-battle-stage-builder` スキルを参照）。

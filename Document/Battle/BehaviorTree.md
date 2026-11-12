# Battle BehaviorTree 構造解説

このドキュメントは、バトルシーンの進行（道中・ボス戦・敵の行動）を駆動する BehaviorTree（以下 BT）方式の構造を解説する。
個々のノードのフィールドや戻り値の詳細は [BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) を参照すること。

旧方式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset` / `AttackPreset` / `MovementPreset` による Unity Timeline 駆動）はコードとデータが残っているが、実行時には使われておらず削除予定である。旧方式の解説は [Timeline.md](./Timeline.md) に残している。

本ドキュメントはソースコードを正として書いている。食い違いがあればソースコードを優先すること。

## 1. 全体像

```text
StageData.asset
 ├─ waySequenceAddress  ──► Stage{N}Way_BT.asset  (BattleStageAsset, situation=Way)
 └─ bossSequenceAddress ──► Stage{N}Boss_BT.asset (BattleStageAsset, situation=Boss)
                             ├─ services[]      (StageServiceDefinition: 例 WindServiceDefinition)
                             ├─ tree            ──► BehaviorTreeAsset (ステージ直下で実行)
                             ├─ bossPrefab / bossSpawnPosition / bossEntrance (MotionStep[])
                             └─ bossTree        ──► BehaviorTreeAsset (ボス自身が実行)

BattleScenePresenter
 └─ BattleFlow
     ├─ BattleClock (MonoBehaviour)  … Update で固定 60fps のステップを刻む
     └─ BattleWorld                  … フレーム番号・乱数・予約実行・サービスを保持
         ├─ tickables
         │   ├─ RunnerTickable(BehaviorTreeRunner)   … ステージ直下のツリー
         │   ├─ BattleActor (敵/ボスの GameObject)   … 自分のツリー + SetMovement の移動
         │   └─ MotionTickable                       … ボス登場移動
         ├─ BattleServices
         │   ├─ BulletSystem   … 敵弾の移動 (BT の外)
         │   ├─ ActorSpawner   … 敵・ビーム・障害物 Prefab の生成
         │   ├─ BulletClearReceiver / Boss / PlaySe / BgmSource / Player / Background
         │   └─ PhaseStarted イベント (PhaseMarker ノードが通知)
         └─ StageContext  … BattleStageAsset.services から生成した IStageService 群
```

BT の適用範囲は次の 2 層である。

- **ステージ層**: `BattleStageAsset.tree`。敵の出現タイミング、フェーズ待ち、ステージ全体の演出を表す。`ctx.Self` は `null`。
- **アクター層**: 敵・ボスの GameObject に付く `BattleActor` が持つツリー。移動・攻撃・HP による分岐を表す。`ctx.Self` はその `BattleActor`。

弾（敵弾）の移動は BT に含めない。BT のノードは「弾を出す」ところまでを担当し、出た弾は `BulletSystem` が `MotionStep` に従って毎フレーム動かす。

## 2. ディレクトリ構成

| パス | 内容 |
|---|---|
| `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/` | `Node` / `NodeDefinition` / `BehaviorTreeAsset` / `BuildScope` / `TreeParameter` / `BattleContext` / `BehaviorTreeRunner` / `BattleStageAsset` |
| `.../BehaviorTree/Composite/` | Sequence / Selector / Parallel / Random |
| `.../BehaviorTree/Decorator/` | Loop / Repeat / Until / Delay / Inverter / ForceSuccess |
| `.../BehaviorTree/Condition/Condition.cs` | 条件定義 (`ConditionDefinition`) と CheckCondition / WaitUntil ノード |
| `.../BehaviorTree/Action/BasicActions.cs` | Wait / Log / SubTree |
| `.../BehaviorTree/Battle/` | バトル固有ノード (発射・ビーム・移動・出現・ステージ演出) |
| `Assets/Project/Scenes/Battle/Scripts/Runtime/` | `BattleWorld` / `BattleClock` / `BattleFlow` / `BattleActor` / `ActorSpawner` / `BulletSystem` |
| `.../Runtime/Stage/` | `StageContext` / `IStageService` / `WindService` / 外力の受け口 (`ExternalForces.cs`) |
| `.../Runtime/Motion/` | `MotionStep` 定義と実行器 (`MotionCore.cs` / `MotionSteps.Basic.cs` / `MotionSteps.Advanced.cs`)、`MotionPreset` / `EaseCurveAsset` |
| `Assets/Project/Scenes/Battle/Editor/BehaviorTree/` | GraphView ベースの BT エディタ |
| `.../Editor/BehaviorTree/Migration/` | 旧データからの一回限りの変換ツール（削除予定） |
| `Assets/Project/DataStore/Stage{N}/BT/` | 各ステージの BT データ（[DataStore.md](./DataStore.md) 参照） |

`BehaviorTree/` と `Runtime/` は `Project.Scenes.Battle.Presenter.asmref` により Presenter アセンブリに含まれる。

## 3. データモデル

### 3.1 定義と実行時ノードの分離

```text
NodeDefinition (アセットに保存・不変)   ──Create(scope)──►   INode / Node (実行時状態を持つ)
```

- `NodeDefinition`（`Core/NodeDefinition.cs`）は `[SerializeReference]` でアセットに保存される不変の定義。`guid`（エディタ・デバッグ用）、`graphPosition`、`comment` を持つ。
- 実行時は `BehaviorTreeAsset.Build()` が定義から `INode` を毎回新しく生成する。状態（経過フレーム、現在の子 index など）は実行時ノード側だけが持つ。
- そのため、同じ `BehaviorTreeAsset` を複数の敵が同時に使っても状態は共有されない。ScriptableObject の DeepCopy は不要。

定義の基底は 3 種類ある。

| 基底 | 子 | 例 |
|---|---|---|
| `CompositeNodeDefinition` | 複数 (`children`、左から右の順) | Sequence, Selector, Parallel, Random |
| `DecoratorNodeDefinition` | 1 つ (`child`、未接続なら即 Success のノード) | Loop, Repeat, Until, Delay, WithWind |
| `ActionNodeDefinition` | なし | Wait, Fire, Spawn, SubTree |

条件は別系統の `ConditionDefinition`（`Condition/Condition.cs`）で、`Build()` で `ICondition`（`Reset` / `Evaluate`）を生成する。Until / CheckCondition / WaitUntil の `condition` フィールドに `[SerializeReference, SubclassSelector]` で入れる。

### 3.2 BehaviorTreeAsset

`Core/BehaviorTreeAsset.cs`（`Create > Battle > Behavior Tree`）。

| フィールド | 説明 |
|---|---|
| `root` | ルートノード定義 (`[SerializeReference]`) |
| `detachedNodes` | エディタ上で親に未接続のノード。実行時は使わない |
| `parameters` | `List<TreeParameter>`。ツリーが公開するパラメータ（後述） |

`root` が `null` の場合、`Build()` は即 Success するノードを返す。

### 3.3 BattleStageAsset

`Core/BattleStageAsset.cs`（`Create > Battle > Battle Stage`）。道中 1 本、またはボス戦 1 本を表す。

| フィールド | 説明 |
|---|---|
| `situation` | `Way` / `Boss` |
| `services` | `List<StageServiceDefinition>` (`[SerializeReference]`)。このステージでだけ有効な機能（風など） |
| `tree` | ステージ直下で実行するツリー。道中ではこれの完了がステージ完了 |
| `bossPrefab` / `bossSpawnPosition` | ボス Prefab と生成位置 |
| `bossEntrance` | ボス登場の移動 (`List<MotionStep>`) |
| `bossTree` | ボス自身に持たせるツリー。シナリオ明けに開始 |

### 3.4 BattleContext

`Core/BattleContext.cs`。ツリー 1 本分の実行文脈で、ノードの `Tick(ctx)` に渡される。

- `World`: 全ツリーで共有する `BattleWorld`
- `Self`: このツリーを持つ `BattleActor`（ステージ直下なら `null`）
- `Frame` / `Random` / `BgmTime` / `Services` / `Stage` は `World` の値の委譲
- `SecondsToFrames(seconds)`: 秒をフレームへ四捨五入で変換

BT には Blackboard が無い。ノード間で値を受け渡す仕組みは持たず、値の共通化はツリーパラメータで行う。

## 4. 実行時の流れ

### 4.1 フレームの刻み方

`Runtime/BattleClock.cs` は `Update` で `Time.deltaTime` を蓄積し、`BattleWorld.DeltaTime`（1/60 秒）ごとに `BattleWorld.Step()` を呼ぶ。

- 1 描画フレームあたり最大 4 ステップまで追いつく。超えた分は切り捨てる（処理落ち時はスロー扱い）。
- `Time.timeScale = 0`（ポーズ）の間は `deltaTime` が 0 なので止まる。

### 4.2 BattleWorld.Step の順序

`Runtime/BattleWorld.cs`:

```text
Step()
 1. RunScheduled()      … Schedule() で予約された処理のうち、このフレームが期限のものを (期限, 登録順) で実行
 2. FlushPending()      … 前フレーム中に Register() されたもの (新しく出現した敵など) を tickables へ追加
 3. tickables を順に BattleTick()
      - RunnerTickable  → BehaviorTreeRunner.Tick() (ルートを 1 回 Tick)
      - BattleActor     → SetMovement の移動を 1 フレーム進めた後、自分のツリーを Tick
      - MotionTickable  → ボス登場移動
    完了したもの (IsFinished) を取り除く
 4. Services.Bullets.Tick(dt) … 全敵弾の移動・外力 (風) の適用
 5. Stage.Tick(dt)            … ステージサービス (WindService など) の時間を進める
 6. AdvanceFrame()            … Frame++
```

注意点:

- `Register()` で追加したものは次のフレームの `Step` から動く。ツリーの途中で出現させた敵のツリーは、出現の翌フレームから実行される。
- `Schedule(delayFrames, action, owner)` は `delayFrames <= 0` なら即実行する。`owner`（`BattleActor`）を渡すと、その敵が倒れた/消えた時点で予約が取り消される（期限到来時にも生存を確認する）。
- 弾は出現したフレームのうちに手順 4 で 1 フレーム分動く（`StartDelayFrames` が 0 の場合）。

### 4.3 ノードのライフサイクル

`Core/Node.cs` の `Node` 基底は `Tick` / `Abort` だけを公開し、`OnEnter` / `OnTick` / `OnAbort` / `OnExit` の呼び出し順を保証する。

```text
Tick(ctx):
  非アクティブなら IsActive = true; OnEnter(ctx)
  status = OnTick(ctx)
  status != Running なら IsActive = false; OnExit(ctx)
  return status

Abort(ctx):
  アクティブでなければ何もしない
  IsActive = false; OnAbort(ctx); OnExit(ctx)
```

- `Tick` は 1 フレームに 1 回が原則。ただし Sequence / Loop など一部の複合ノードは、子が完了したフレームのうちに次の子を開始する（詳細はノードリファレンス）。
- 完了したノードを再び `Tick` すると `OnEnter` からやり直しになる。Loop や Until(Restart) の繰り返しはこの性質を使っている。
- 親が子を中断するときは `Abort` を呼ぶ。`OnExit` は完了・中断のどちらでも必ず呼ばれるため、「子の実行中だけ有効」な処理（WithWind、WithPrefab、Move の停止）は `OnExit` で後始末する。
- `NodeStatus` は `Running` / `Success` / `Failure` の 3 値。

`BehaviorTreeRunner`（`Core/BehaviorTreeRunner.cs`）はルートを毎フレーム `Tick` し、ルートが `Running` 以外を返したら `Result` を確定させて `Finished` を通知し、以後は何もしない。ノード内で例外が出た場合は `Debug.LogException` の上で `Failure` 扱いにする。`Stop()` はルートを `Abort` する。

### 4.4 アクター (BattleActor)

`Runtime/BattleActor.cs` は敵/ボスの GameObject に付けるコンポーネント。

- 付与: `BattleActor.Attach(gameObject, world, tree, overrides)`。`ActorSpawner`（Spawn ノード経由）と `BattleFlow.StartBossTree` が呼ぶ。付与時に static イベント `BattleActor.Attached` が発火する（`BossPhaseProgressGauge` が購読に使う）。
- `Model`: 同じ GameObject の `BossEntityPresenter` または `EnemyEntityPresenter` の Model。HP 系条件はこれを見る。
- `AttackTime`: 付与されてからの経過秒数（`(Frame - StartFrame) / 60`）。発射時刻で変わる移動（頂点の揺らぎなど）に渡す。
- `SetMovement(steps, continueIfSame)`: ツリーと独立して流し続ける移動を差し替える。毎フレーム、ツリーより先に進む。
- `SetDeathBehavior(tree)`: 倒されたときに 1 回だけ実行するツリーを登録する。`Model.OnDeath` で新しい Runner を作り、**1 回だけ Tick して即 Stop** する。そのため死亡時ツリーで意味を持つのは、その 1 フレームで完了する処理（Fire、Spawn など）に限られる。
- 死亡・Destroy 後は `IsAlive` が false になり、ツリーを停止し tickables から外れる。

`ActorSpawner.Spawn` で `Behavior`（ツリー）を指定した場合、`EnemyEntityPresenter.DetachLegacyBehaviour()` を呼んで Prefab 側に設定された旧来の移動・攻撃・死亡時攻撃を止めてから `BattleActor` を付ける。ツリー未指定なら Prefab の旧来の動きがそのまま動く。

## 5. パラメータと SubTree

### 5.1 値フィールド (BtInt など)

`Core/TreeParameter.cs` の `BtInt` / `BtFloat` / `BtBool` / `BtVector2` / `BtObject<T>` は「固定値 `value`」と「パラメータ名 `param`」の組である。

- `param` が空なら `value` を使う。
- `param` があれば Build 時に `BuildScope` から同名パラメータを探し、その値を使う。見つからなければ `value` に戻る。
- `BtFloat` は Int パラメータも受け付け、`BtInt` は Float パラメータを四捨五入して受け付ける。
- エディタ上では `$名前` と表示される。

現状 `Bt*` 型になっているのは、Parallel.duration / Loop.count / Repeat.count, interval / Delay.frames / Wait.frames / FrameElapsed.frames / BgmReached.seconds のみである。Fire や Move などバトルノードのフィールドは通常の値で、パラメータ化できない。

### 5.2 TreeParameter と BuildScope

`TreeParameter` は `name` / `kind`（Float, Int, Bool, Vector2, Object）/ 各型の値 / `bindToParent` を持つ。

`BuildScope`（`Core/BuildScope.cs`）は Build 時のスコープで、次を行う。

1. 親スコープを辿り、同じアセットが既に含まれていれば `InvalidOperationException`（SubTree の循環検出）。
2. このアセットの `parameters` を既定値として登録する。
3. 呼び出し側の `overrides` で同名のものを上書きする。宣言されていない名前の上書きは警告して無視する。
4. 上書き側の `bindToParent` が空でなければ、親スコープの同名パラメータをそのまま引き継ぐ。

パラメータは **Build 時に 1 回だけ解決される**。実行中に値が変わることはない。

### 5.3 SubTree

`SubTreeNodeDefinition`（`Action/BasicActions.cs`）は別の `BehaviorTreeAsset` を自分の位置に展開する。

- `tree`: 実行するツリー
- `overrides`: 子ツリーのパラメータの上書き（`bindToParent` で親のパラメータを引き継げる）

SubTree は Build 時にその場で子ツリーを `Build(scope, overrides)` するだけで、実行時には子ツリーのルートノードそのものになる。攻撃パターンを `BT/Attack/` に切り出し、ボスの複数フェーズから再利用する用途で使う。

`Spawn` ノードの `behavior` / `overrides` でも敵に別ツリーとパラメータ上書きを渡せる。ただしこちらは敵側で親スコープ無しに Build されるため、`bindToParent` は効かない（固定値の上書きのみ有効）。

## 6. ステージの流れ (道中 → ボス → シナリオ)

`Scripts/Presenter/BattleScenePresenter.cs` と `Runtime/BattleFlow.cs` が担当する。

```text
InitializeAndStart()
 ├─ StageModel.WaySequenceAddress / BossSequenceAddress を Addressables で BattleStageAsset としてロード
 │    (アドレス = アセットパス。WaitForCompletion で同期ロード)
 └─ CurrentSituation に応じて開始
     Way:
       flow.BeginStage(wayStage)                … StageContext を作り直し、宣言サービスを Begin
       flow.StartStageTree(wayStage.Tree, cb)   … ステージ直下のツリーを Runner として登録
       ツリー完了 → 2 秒待ち → Scenario を Additive ロード
         シナリオ中の "SpawnBoss" コマンド → BattleScenePresenter.SpawnBoss()
             ├─ bossPrefab を生成
             ├─ flow.BeginStage(bossStage)        … ここで道中のサービスを End、ボスのサービスを Begin
             └─ flow.PlayEntrance(boss, bossEntrance)
         シナリオ完了 → StartBossSequence()
     Boss:
       StartBossSequence()
         ├─ flow.StartBossTree(boss, bossStage.BossTree)
         │    … 登場移動を終点まで一気に進めて止め、BattleActor を付与。Services.Boss に登録
         └─ bossStage.Tree があれば flow.StartStageTree(bossStage.Tree, null) (ボス戦中のステージ演出)
       ボス死亡 (OnDeath) → Services.Boss.Stop()
       死亡演出完了 → 2 秒待ち → Scenario → CompleteStage()
```

- ボス本ステージ（`bossSequenceAddress`）が未設定の場合、道中完了後はボス前シナリオへ進まずに止まる（制作中ステージ用）。
- Retry / タイトル戻り / ステージ完了では `flow.Stop()` が呼ばれ、弾の消去、`BattleClock.End()` → `BattleWorld.Clear()`（tickables・予約の破棄、`SetStage(null)` によるサービスの End）を行う。次の開始時に新しい `BattleWorld` が作られる。
- ボス戦の勝敗はツリーの結果ではなくボスの死亡で決まる。ボスツリーが先に完了してもステージは終わらない。

## 7. ステージサービス

ステージ固有の機能（現状は風のみ）は、`BattleStageAsset.services` に宣言したステージでだけ存在する「ステージサービス」としてカプセル化している。

### 7.1 仕組み

`Runtime/Stage/StageContext.cs`:

```csharp
public interface IStageService
{
    void Begin(StageContext stage);
    void Tick(float dt);
    void End();
}

[Serializable]
public abstract class StageServiceDefinition
{
    public abstract IStageService Create();
}
```

- `StageServiceDefinition` はアセットに保存する設定。実行時の状態は `Create()` が返す別インスタンスに持たせる。
- `BattleFlow.BeginStage(stage)` が `new StageContext(world, stage.Services)` を作り `BattleWorld.SetStage()` に渡す。前のステージの `StageContext` は `End()` される。
- `StageContext.Tick(dt)` は `BattleWorld.Step` の手順 5 で毎フレーム呼ばれる。
- ノードからは `ctx.Stage?.Get<T>()` / `TryGet<T>()` で取得する。宣言されていないサービスを使おうとしたノードは `StageContext.WarnMissing<T>()` で 1 回だけ警告を出し、何もしない（Success は返す）。

設計方針として、サービスは自分から自機・敵弾・表示へ働きかける。自機や弾の側はサービスを知らず、汎用の受け口だけを公開する（`Runtime/Stage/ExternalForces.cs`）。

| 受け口 | 実装側 | 内容 |
|---|---|---|
| `IMoveModifier.Modify(velocity, input, speed)` | `PlayerEntityPresenter.AddMoveModifier / RemoveMoveModifier` | 自機の移動速度に補正を加える |
| `IBulletForce` (`Active` / `LaunchAxis` / `Integrate`) | `BulletSystem.AddForce / RemoveForce` | 敵弾の本来の軌道に外力によるずれを足す |

### 7.2 例: WindService

`Runtime/Stage/WindService.cs`。Stage3 の `Stage3Way_BT.asset` / `Stage3Boss_BT.asset` だけが `WindServiceDefinition` を宣言している。

`WindServiceDefinition` のフィールド:

| フィールド | 説明 |
|---|---|
| `initial` (`BattleWindSettings`) | ステージ開始時の風。`intervals` が空なら無風で始まる |
| `arrowsPrefab` (`WindArrowsPresenter`) | 風向きを示す矢印。Begin 時に背景 (`Services.Background`) の子として生成する |

`BattleWindSettings`（`Scripts/Model/BattleWind.cs`）: `enabled`、`startSe`、`playerSpeedRatio`、`intervals`（`WindDirection` × `seconds` のリスト。順に繰り返す。秒単位、最小 0.1）、`bulletAcceleration`（ワールド単位/秒²）、`bulletDrag`（1/秒）。

`WindService` の動作:

- `Begin`: `Services.Player.AddMoveModifier(this)`、`Services.Bullets.AddForce(this)`、矢印を生成して `Bind`、`Apply(initial)`。
- `Tick(dt)`: 予定表を秒で進め、区間が終われば次の向きへ。無風から風ありになった瞬間に `startSe` を鳴らす。
- `End`: 受け口から自分を外し、矢印を破棄する。
- `Apply(settings)`: 予定表を差し替えて先頭から始める。`WithWind` ノード（子の実行中だけ別の風にし、終了時に直前の風へ戻す）と `BossWindCue` ノードが使う。
- `enabled` が OFF でも予定表は進む。このとき自機・弾への影響と矢印表示は無いが、`ProjectileDirection` は予定表の向きを返し、自機狙い放物線 (`AcceleratedMotion` の `useBattleWind`) の曲げる向きに使われる。
- 弾への影響は発射時点で決まる。`BulletSystem.Spawn` 時に風が有効で、かつ `BulletSpec.affectedByWind` が ON の弾だけが外力を受ける（`BulletEntityPresenter.WindInfluence` を倍率として掛ける）。

### 7.3 新しいサービスを追加する手順

1. `Runtime/Stage/` に `IStageService` を実装したクラスと、それを生成する `[Serializable] StageServiceDefinition` 派生クラスを作る。設定値は定義側、状態はサービス側に持つ。
2. 自機や弾に作用する場合は、既存の受け口（`IMoveModifier` / `IBulletForce`）を使う。足りなければ自機/弾側には汎用の受け口だけを追加し、ステージ固有の知識は持たせない。
3. 必要な参照（`Player`、`Background`、`PlaySe` など）は `StageContext.World.Services` から取る。
4. サービスを操作するノードが必要なら `BehaviorTree/Battle/` に追加し、`ctx.Stage?.Get<T>()` が `null` のときは `WarnMissing<T>()` を出して何もしないようにする。
5. 使うステージの `BattleStageAsset.services` に定義を追加する。宣言しないステージでは存在しない扱いになる。

## 8. 弾と移動

### 8.1 弾の発射

発射系ノード（Fire、BulletBeam、MoveTrailFan）はすべて `Battle/EmitterSources.cs` の `Emitter.Spawn` を通して `BulletSystem.Spawn` を呼ぶ。

- 方向は `DirectionSource`、弾の回転は `RotationSource`、弾の設定は `BulletSpec`、撃ち方は `ShotPattern` で指定する（詳細はノードリファレンス）。
- 発射位置は「撃つ敵の位置 + muzzleOffset + パターンごとのずれ」。ステージ直下（`Self == null`）なら原点基準。
- パターンの `SpawnDelayFrames` が付いた弾は `BattleWorld.Schedule` で予約され、予約時点ではなく出現時点の撃ち手の位置から出る。撃ち手が倒れると予約は取り消される（死亡時ツリーから撃った弾は取り消し対象にしない）。
- 方向は発射ノードを実行したフレームに 1 回だけ決める。

`BulletSystem`（`Runtime/BulletSystem.cs`）は弾 Presenter を `BulletPool` から取り出し、`BulletEntityPresenter.InitializeDriven` で「位置を外から与えられる弾」として初期化する。見た目・当たり判定・寿命・画面外判定は Presenter 側、位置は `BulletSystem` 側が担う。

- `StartDelayFrames` の間はその場で止まる。
- 移動は `MotionSequence` で進める。`Motion` が空なら `VelocityMotion`（発射方向へ等速）を使う。
- 移動ステップが `GrantsInvincibility` を返す間は弾を無敵にする。
- 移動が開始できない設定（`Rejected`）なら即プールへ戻す。
- 外力 (`IBulletForce`) は、作者が設定した移動の速度を元に補正量を積分し、`WindInfluence` を掛けて位置に足す。

### 8.2 MotionStep

`Runtime/Motion/` は敵・ボス・弾で共通の移動定義である。

- `MotionStep`（定義、`[SerializeReference]`）→ `CreateRunner()` → `MotionRunner`（実行状態）という BT と同じ定義/実行の分離をとる。
- `MotionSequence` はステップの列を順に実行し、完了したフレームのうちに次のステップを開始する。
- 時間は **秒** で指定する（BT ノードのフレーム指定と異なる点に注意）。実行時は 1/60 秒ずつ進む。
- `MotionPreset`（`Create > Battle > Motion Preset`）はステップ列の共有用アセット。`EaseCurveAsset`（`Create > Battle > Ease Curve`）はカスタム Ease カーブの共有用アセット。
- 対象は `IMotionBody`。敵/ボスは Transform そのもの (`TransformBody`)、弾は外力補正前の「本来の軌道」。

旧実装との互換のため、`StepClock`（`MotionCore.cs`）は旧 `PullMovementHelper` の挙動を再現する。旧実装では Ease 未指定の移動に DOTween 既定の OutQuad が掛かっていたため、`legacyOutQuad` が ON のステップ（Velocity、AimStraight、Forward、Homing、StraightWithHoming、Sine、Reflect、FadeMove）では実時間 t に対し内部時間 τ = D(2u − u²)（u = t/D）を使う。duration 0（無限、内部的には D = 999999）では序盤がほぼ 2 倍速になる。この癖は意図的に残している。同様に `MotionEase` / `MotionEasing` でも `Ease.Unset` は OutQuad として扱う。

## 9. エディタ

`Window > Battle > Behavior Tree Editor`、または `BehaviorTreeAsset` をダブルクリックで開く（`Editor/BehaviorTree/BehaviorTreeEditorWindow.cs`）。

画面構成:

| 領域 | 内容 |
|---|---|
| ツールバー | 対象アセット、`Auto Layout`（ルートから下向きに整列）、`Frame All`、`Blackboard`（右ペインの表示切替）、`MiniMap` |
| 左ペイン (`NodePaletteView`) | ノード一覧（Composite / Decorator / Condition / Action とサブカテゴリ）と SubTree 用の BT アセット一覧。検索欄あり。項目をキャンバスへドラッグして作成 |
| 中央 (`BehaviorTreeGraphView`) | ノードグラフ。上が親、子は左から右の順に実行される。ヘッダーの番号は親の中での実行順。Wait を含む Sequence の子には、Sequence 開始から何フレーム目に動くか（`@150f`）も出す。時間の読めない子（Move / SubTree など）より後ろには出さない |
| 右ペイン (`ParameterPanelView`) | ツリーパラメータの一覧（名前 / 型 / 既定値）。`+` で追加、右クリックで `Select Usages` / `Delete` |

操作:

- ノード作成: パレットからドラッグ、キャンバスの右クリック（Create Node）で検索ウィンドウ、または出力ポートから線を引いて何もない所で離すと、その先に接続した状態で作成。ドロップ位置に子を受け付けるノードがあれば、その最後の子として接続される。
- Project ウィンドウの `BehaviorTreeAsset` をキャンバスへドロップすると SubTree ノードになる。SubTree ノードの右クリック `Open SubTree` で子ツリーを開く。
- コピー & ペースト、Undo / Redo に対応。ノード右クリックの `Edit Comment` でコメントを付けられる。
- 値欄 (`BtInt` など) の `▾` から、パラメータへの結びつけ・`固定値を使う`・`今の値で新しいパラメータを作成` を選べる。右ペインのパラメータ行を値欄へドラッグしても結びつけられる。型の合わないパラメータは警告表示になる。
- パラメータを削除すると、参照していた値欄は固定値に戻る。
- 再生中は、最近 Tick されたノードがハイライトされる（`BehaviorTreeDebug`）。同じ定義を複数の敵が使っている場合はまとめて光る。

性能上の工夫として、ノードの入力欄（PropertyField）は選択中のノードにだけ作り、それ以外はヘッダーと一行要約（`NodeDefinition.Summary`）だけを表示する。構造変更時もグラフ全体を作り直さず、増減した要素だけを反映する。

### 移行ツール (削除予定)

`Editor/BehaviorTree/Migration/` の `Tools > Battle > Migrate Legacy Stage > Stage N` は旧 `Stage{N}/Way|Boss/*.asset` (`BattleSequenceAsset`) を BT 形式へ一括変換し、`DataStore/Stage{N}/BT/` を **削除して作り直した上で** StageData と Addressables を新アセットへ向け替える。移行は完了済みで、以後のデータは BT エディタで直接編集している。手作業の修正を消してしまうため、再実行しないこと。

## 10. 設計判断

| 判断 | 内容 |
|---|---|
| BT の適用範囲 | ステージ層とアクター（敵/ボス）層に適用する。敵弾は数が多く個々に判断を持たないため、BT の外の `BulletSystem` が `MotionStep` で動かす |
| 時間の基準 | BT はフレーム基準（60fps 固定）。描画フレームとは独立した `BattleClock` で進めるため、処理落ちしても弾幕の相対タイミングが崩れない。移動 (`MotionStep`) の長さは旧データとの互換で秒指定 |
| 割り込み | 「条件成立で中断」は Until デコレータで表す。毎フレーム子より先に条件を評価し、成立したら子を Abort する。フェーズの終了条件・HP による打ち切りはすべてこれ |
| Blackboard を持たない | ノード間の動的な値の受け渡しはしない。共通化したい値はツリーパラメータで Build 時に解決する |
| 再利用 | 攻撃パターンや敵の行動は `BehaviorTreeAsset` として切り出し、SubTree / Spawn.behavior で参照する。コピーせず参照するので、元アセットの修正がすべての使用箇所に効く |
| 保存形式 | ScriptableObject + `[SerializeReference]`。定義は不変で、実行時ノードを毎回生成する |
| 旧挙動の再現 | 旧実装の「Ease 未指定時は OutQuad」の癖は `StepClock` / `MotionEasing` で再現し、移行前後で弾道を変えない |
| ボスと雑魚の統一 | ボスと通常敵は同じ `BattleActor` + 同じノード群で攻撃・移動を解釈する。ボス固有なのは HP 系条件の対象 (`HpTarget.Boss`) と EnterStrongMode 程度 |
| ステージ固有機能 | `BattleStageAsset.services` に宣言するステージサービスとしてカプセル化する。サービスが自機・弾へ働きかけ、自機・弾は汎用の受け口だけを持つ |

## 11. 既知の制約・注意点

- **乱数は再現性がない**: `BattleWorld` の乱数シードは `Environment.TickCount`。さらに `DriftMotion` は `UnityEngine.Random`、自機狙い放物線は static な発射カウンタ (`AimedParabola.NextShotIndex`) を使うため、同じ入力でも同じ展開にはならない。
- **パラメータ化できるフィールドが限られる**: `Bt*` 型のフィールドは時間・回数系の一部だけ（5.1 参照）。
- **死亡時ツリーは 1 フレームだけ**: `SetDeathBehavior` のツリーは 1 回 Tick して停止する。Wait や Delay を含めても後半は実行されない。
- **ツリー開始は翌フレーム**: 出現させた敵のツリー、`StartStageTree` で登録したツリーは次の `Step` から動く。
- **秒とフレームの混在**: BT ノードの時間はフレーム、`MotionStep` と一部のバトルノードのフィールド（Spawn.beamDuration、SpriteBeam.bodyDuration、MoveTrailFan.moveDuration、ChargeShot.gatherDuration、ScreenEdgeDirection など）は秒。各フィールドの単位はノードリファレンスを参照。
- **PhaseMarker の phaseId は現状使われていない**: `BossPhaseProgressGauge` は通知の回数だけを見て、BGM のサンプル位置から現在フェーズを推定する。
- **旧コードへの依存が残っている**: 新方式が使う型の一部が旧方式のファイルに定義されている（`BossWindCueSettings` は `BattleTimelineBuilderAsset.cs`、`BattleWindSettings` / `WindDirection` は static `BattleWind` と同じ `BattleWind.cs`、`PlayerPositionReference` は `Model/Movement/`）。旧方式を削除するときはこれらを移設する必要がある。
- **旧方式のコードとデータ**: `BattleSequenceAsset`、`BattleTimelineBuilderAsset`、`AttackPreset` / `AttackTimeline` / 各種 Signal、`MovementPreset` / `IMovementStep`、`BattlePhaseStateMachine`、`BattleTimelineBindingMap`、static `BattleWind`、`DataStore/Stage{N}/Attack|Movement|Ease|Bullet` と `Way|Boss` 内の旧アセットは実行時には使われていない。削除予定。ただし `DataStore/Stage{N}/Way|Boss/Stage{N}{Way|Boss}Scenario.asset` は `ScenarioModelRepository` がこのパスから読み込む現役のシナリオデータなので、フォルダごと消さないこと。

# Battle BehaviorTree 構造解説

このドキュメントは、バトルシーンの進行（道中・ボス戦・敵の行動）を駆動する BehaviorTree（以下 BT）方式の構造を解説する。
個々のノードのフィールドや戻り値の詳細は [BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) を参照すること。

旧方式（`BattleSequenceAsset` / `BattleTimelineBuilderAsset` による Unity Timeline 駆動）と旧攻撃タイムライン（`AttackPreset` / `AttackTimeline` / `MovementPreset`）は削除済みである。

本ドキュメントはソースコードを正として書いている。食い違いがあればソースコードを優先すること。

## 1. 全体像

```text
StageData.asset
 └─ stageAddress ──► Stage{N}_BT.asset (BattleStageAsset。1 ステージ = 1 アセット)
                      ├─ services[]  (StageServiceDefinition: 例 WindServiceDefinition)
                      └─ tree        ──► Stage{N}_Tree.asset (ステージツリー。ルートは Stage ノード)
                                          ├─ Section "Way"  … 道中
                                          └─ Section "Boss" … 会話 → ボス戦 → 撃破後の会話
                                               ├─ PlayScenario ──► ScenarioData (会話)
                                               └─ SpawnBoss / BossBattle ──► Stage{N}_Boss.asset (BossDefinition)
                                                                              └─ tree ──► Stage{N}Boss_Tree.asset (ボス自身が実行)

BattleScenePresenter (IScenarioService を実装)
 └─ BattleFlow (IBossHost を実装)
     ├─ BattleClock (MonoBehaviour)  … Update で固定 60fps のステップを刻む
     └─ BattleWorld                  … フレーム番号・乱数・予約実行・サービスを保持
         ├─ tickables
         │   ├─ RunnerTickable(BehaviorTreeRunner)   … ステージツリー
         │   ├─ BattleActor (敵/ボスの GameObject)   … 自分のツリー (移動も Move ノードで行う)
         │   └─ MotionTickable                       … ボス登場移動
         ├─ BattleServices
         │   ├─ BulletSystem   … 敵弾・自機弾の移動 (BT の外)
         │   ├─ ActorSpawner   … 敵・ビーム・障害物 Prefab の生成 (プール対象の敵は ActorPool から出す)
         │   ├─ BossHost (= BattleFlow) / Scenario (= BattleScenePresenter) / Progress (StageProgress)
         │   ├─ BulletClearReceiver / Boss / PlaySe / PlayBgm / BgmSource / Player / Background
         │   └─ OnPhaseStarted (PhaseMarker ノードが通知)
         └─ StageContext  … BattleStageAsset.services から生成した IStageService 群
```

BT の適用範囲は次の 2 層である。

- **ステージ層**: `BattleStageAsset.tree`。Section の並び、敵の出現、会話、ボスの出現と戦闘、BGM を表す。`ctx.Self` は `null`。
- **アクター層**: 敵・ボスの GameObject に付く `BattleActor` が持つツリー。移動・攻撃・HP による分岐を表す。`ctx.Self` はその `BattleActor`。

弾（敵弾）の移動は BT に含めない。BT のノードは「弾を出す」ところまでを担当し、出た弾は `BulletSystem` が `MotionStep` に従って毎フレーム動かす。

## 2. ディレクトリ構成

| パス | 内容 |
|---|---|
| `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/Core/` | `Node`（基底と `CompositeNode` / `DecoratorNode` / `ActionNode`）/ `BehaviorTreeAsset` / `TreeScope`（`NodeCloner` を含む）/ `TreeParameter` / `BattleContext` / `BehaviorTreeRunner` / `BattleStageAsset` |
| `.../BehaviorTree/Composite/` | Sequence / Selector / Parallel / RandomSelect と、ステージ進行の Stage / PlayScenario |
| `.../BehaviorTree/Decorator/` | Loop / Repeat / Until / Delay / Inverter / ForceSuccess / WarningLine / Section / OnCommand / WithWind / WithPrefab |
| `.../BehaviorTree/Condition/` | 条件の基底 `Condition` と Constant / FrameElapsed / BgmReached / And / Or / Not / HpBelow / NoActiveEnemies など |
| `.../BehaviorTree/Action/` | Wait / Log / SubTree / CheckCondition / WaitUntil、発射・ビーム・移動・出現・ステージ演出・ステージ進行の Action ノード |
| `.../BehaviorTree/Parts/` | ノードではない部品 (ShotPattern / DirectionSource / RotationSource / SpawnLayout / BulletSpec / Emitter / BeamAim など) と `BossDefinition` / `TrailPath` |
| `Assets/Project/Scenes/Battle/Scripts/Runtime/` | `BattleWorld` / `BattleServices` / `BattleClock` / `BattleFlow` / `BattleActor` / `ActorSpawner` / `BulletSystem`、`IScenarioService` / `IBossHost` / `StageProgress`、敵のプール（`ActorPool` / `IPoolableActor` / `ActorDespawn` / `PooledActor` / `NoActorPool`）、事前生成用の `StagePrefabCollector` |
| `.../Runtime/Stage/` | `StageContext` / `IStageService` / `WindService` / 外力の受け口 (`IMoveModifier` / `IBulletForce`) |
| `.../Runtime/Motion/` | `MotionStep` と各ステップ (`*Motion.cs`)、実行器 (`MotionRunner` / `PullRunner` / `MotionSequence`)、`MotionPreset` / `EaseCurveAsset` / `BattleTargets`（自機の Transform） |
| `Assets/Project/Scenes/Battle/Editor/BehaviorTree/` | GraphView ベースの BT エディタ |
| `Assets/Project/DataStore/Stage{N}/BT/` | 各ステージの BT データ（[DataStore.md](./DataStore.md) 参照） |

`BehaviorTree/` と `Runtime/` は `Project.Scenes.Battle.Presenter.asmref` により Presenter アセンブリに含まれる。

## 3. データモデル

### 3.1 ノード 1 種類 = 1 クラス

```text
アセット上の Node (雛形)  ──NodeCloner.Clone──►  実行用の Node (複製)  ──Setup(scope)──►  Tick
```

- ノードは種類ごとに 1 クラス（`Wait`、`Sequence`、`NWayShot` など）で、設定・実行中の状態・処理を 1 つに持つ。基底は `Core/Node.cs` の `Node`。
  - 設定は `[SerializeField]` / `[SerializeReference]` でアセットに保存する。`guid`（エディタ・デバッグ用）、`graphPosition`、`comment` は基底が持つ。
  - 実行中の状態（経過フレーム、現在の子の番号など）は `[NonSerialized]` のフィールドに持つ。アセットには保存されない。
- アセット上のノードは雛形で、直接は実行しない。`BehaviorTreeAsset.Instantiate()` が `NodeCloner.Clone` で複製し、`Setup(scope)` でパラメータを解決してから実行する。そのため、同じ `BehaviorTreeAsset` を複数の敵が同時に使っても状態は共有されない。
- `NodeCloner`（`Core/TreeScope.cs`）が複製するのはノード・条件と、それを中に持つ入れ物（`WaveEntry` など）だけ。方向・回転・弾の設定・移動・撃ち方（`ShotPattern`）などの読むだけのデータは雛形と共有する。こうしたデータのクラスに、実行中に書き換わる状態を持たせてはいけない。

ノードの基底は 3 種類ある。

| 基底 | 子 | 例 |
|---|---|---|
| `CompositeNode` | 複数 (`children`、左から右の順。`Setup` で空の要素を除く) | Sequence, Selector, Parallel, RandomSelect |
| `DecoratorNode` | 1 つ (`child`、未接続なら即 Success する `EmptyNode` を入れる) | Loop, Repeat, Until, Delay, WithWind |
| `ActionNode` | なし | Wait, NWayShot などの発射, Spawn, SubTree |

条件は別系統の `Condition`（`Condition/Condition.cs`）で、`Reset` / `Evaluate` を持つ。ノードと同じく実行するツリーごとに複製され、状態（経過フレームの起点など）を `[NonSerialized]` に持つ。Until / CheckCondition / WaitUntil の `condition`、Wave の `until` に `[SerializeReference, SubclassSelector]` で入れ、持ち主のノードが `OnSetup` で条件の `Setup` を呼ぶ。

新しいノードの書き方:
- 基底のどれかを継承し、`OnTick` を実装する。必要なら `OnEnter`（開始時の初期化）/ `OnExit`（完了・中断どちらでも呼ばれる後始末）/ `OnAbort`（中断時だけ）を上書きする。
- パラメータの解決など、実行のたびに変わらない準備は `OnSetup(scope)` で行い、結果を `[NonSerialized]` のフィールドに入れる。
- `[Serializable, NodeInfo("説明", "分類")]` を付けると BT エディタのパレットに出る。

### 3.2 BehaviorTreeAsset

`Core/BehaviorTreeAsset.cs`（`Create > Battle > Behavior Tree`）。

| フィールド | 説明 |
|---|---|
| `root` | ルートノード定義 (`[SerializeReference]`) |
| `detachedNodes` | エディタ上で親に未接続のノード。実行時は使わない |
| `parameters` | `List<TreeParameter>`。ツリーが公開するパラメータ（後述） |

`Instantiate(parentScope, overrides)` は実行用の複製を作り、パラメータを解決して返す。アセット上のノードは書き換えない。`root` が `null` の場合は即 Success する `EmptyNode` を返す。

### 3.3 BattleStageAsset

`Core/BattleStageAsset.cs`（`Create > Battle > Battle Stage`）。1 ステージ（道中・会話・ボス戦）全体を 1 つで表す。

| フィールド | 説明 |
|---|---|
| `services` | `List<StageServiceDefinition>` (`[SerializeReference]`)。このステージでだけ有効な機能（風など） |
| `tree` | ステージツリー。ルートは Stage ノードにする。Success で完了するとステージクリア |

### 3.4 BossDefinition

`Battle/BossDefinition.cs`（`Create > Battle > Boss Definition`）。ボス 1 体を出すのに必要なものをまとめ、SpawnBoss / BossBattle ノードから参照する。

| フィールド | 説明 |
|---|---|
| `prefab` | `BossEntityPresenter` を持つ Prefab |
| `spawnPosition` | 生成位置 |
| `entrance` | 登場の移動 (`List<MotionStep>`)。戦闘開始時に終わっていなければ終点まで進める |
| `tree` | ボス自身が実行するツリー。BossBattle で別のツリーを指定しなければこれを使う |

### 3.5 BattleContext

`Core/BattleContext.cs`。ツリー 1 本分の実行文脈で、ノードの `Tick(ctx)` に渡される。

- `World`: 全ツリーで共有する `BattleWorld`
- `Self`: このツリーを持つ `BattleActor`（ステージ直下なら `null`）
- `Frame` / `Random` / `BgmTime` / `Services` / `Stage` は `World` の値の委譲
- `SecondsToFrames(seconds)`: 秒をフレームへ四捨五入で変換

BT には Blackboard が無い。値の共通化はツリーパラメータで行う。実行中に値を受け渡すのは、Repeat / Cycle / Loop の `indexParam` と Calc の `resultParam` がツリーパラメータへ書き込む場合だけ（5.2）。

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
      - BattleActor     → 自分のツリーを Tick
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

- 付与: `BattleActor.Attach(gameObject, world, tree, overrides)`。`ActorSpawner`（Spawn ノード経由）と `BattleFlow.StartBattle`（BossBattle ノード経由）が呼ぶ。付与時に static な `BattleActor.OnAttached` (IObservable) に流れる（`BossPhaseProgressGauge` が購読に使う）。
- `Model`: 同じ GameObject の `BossEntityPresenter` または `EnemyEntityPresenter` の Model。HP 系条件はこれを見る。
- `AttackTime`: 付与されてからの経過秒数（`(Frame - StartFrame) / 60`）。発射時刻で変わる移動（頂点の揺らぎなど）に渡す。
- `SetDeathBehavior(tree)`: 倒されたときに 1 回だけ実行するツリーを登録する。`Model.OnDeath` で新しい Runner を作り、**1 回だけ Tick して即 Stop** する。そのため死亡時ツリーで意味を持つのは、その 1 フレームで完了する処理（Fire、Spawn など）に限られる。
- 死亡・Destroy・プールへの返却後は `IsAlive` が false になり、ツリーを停止し tickables から外れる。

敵の移動と攻撃はすべて `BattleActor` のツリーが行う。`EnemyEntityPresenter` が持つのは HP・被弾点滅・効かない攻撃の SE・寿命・画面外判定・接触だけで、Spawn で `behavior` を指定しない敵はその場から動かない。

### 4.5 敵のプールと事前生成

Spawn ノードで出す敵は `ActorPool`（`Runtime/ActorPool.cs`）で Prefab ごとに使い回す。

- `ActorPool` は `BattleFlow` が初回に Battle シーンへ `"ActorPool"` オブジェクトとして作り、リトライをまたいで使い回す（シーンと一緒に破棄される）。
- プール対象は、`EnemyEntityPresenter` を持ち、`BossEntityPresenter` / `TwinParabolaMidBossController` / `DamageRelayToBoss` / `StaffNotationPresenter` を持たず、`NoActorPool` が付いていない Prefab（`ActorPool.IsPoolable`）。任意の敵をプールから外したいときは Prefab に `NoActorPool` を付ける。
- `ActorSpawner` はプール対象なら `ActorPool.Get`、それ以外は `Instantiate` で出す。
- 敵を消すときは必ず `ActorDespawn.Despawn` を通す。プール品はフレームの最後（`LateUpdate`）にプールへ戻し、それ以外は `Destroy` する。撃破・画面外・寿命・Despawn ノード・`BulletClearReceiver.ClearAllEnemies` がすべてこれを通る。
- 出し入れ時の状態の戻しは `IPoolableActor`（`OnTakenFromPool` / `OnReturnedToPool`）で行う。`EnemyEntityPresenter` は再初期化と後片付け（返却時に `OnRemoved` を通知）、`BattleActor` はツリー・移動・予約・撃破時の行動を片付け、次に `Attach` されるまで生存扱いを外す。`OnTakenFromPool` は 2 回目以降に出したときだけ呼ばれる（初回は `Awake` で初期化される）。
- 同じ敵が付け直されることがあるため、`BattleWorld.Register` は登録済みのものを重複登録しない。`EnemyTracker` は破棄通知ではなく `EnemyEntityPresenter.OnRemoved` で生存数を減らす。

`BattleFlow.StartStage` は、ツリー開始前に `StagePrefabCollector.Collect(stage)` でステージが参照する弾 Prefab と出現物 Prefab を集め、事前生成する。

- 弾: 1 種類あたり 30 発（`BulletPool.Prewarm`）
- プール対象の敵: 1 種類あたり 4 体（`ActorPool.Prewarm`）。非アクティブの親の下に作るので、出すまで `Awake` は走らない

`StagePrefabCollector` は `BattleStageAsset` からシリアライズされるフィールドをリフレクションで総当たりにたどる（ScriptableObject はたどるが、Prefab の中身はたどらない）。足りない分は実行中にプールが増やす。

## 5. パラメータと SubTree

### 5.1 値フィールド (BtInt など)

`Core/TreeParameter.cs` の `BtInt` / `BtFloat` / `BtBool` / `BtVector2` / `BtObject<T>` は「固定値 `value`」と「パラメータ名 `param`」の組である。

- `param` が空なら `value` を使う。
- `param` があれば `Setup` 時に `TreeScope` から同名パラメータを探し、その値を使う。見つからなければ `value` に戻る。
- `BtFloat` は Int パラメータも受け付け、`BtInt` は Float パラメータを四捨五入して受け付ける。
- エディタ上では `$名前` と表示される。

現状 `Bt*` 型になっているのは、Parallel.duration / Loop.count / Repeat.count, interval / Delay.frames / Wait.frames / FrameElapsed.frames / BgmReached.seconds、`ChargeEffect` の各フィールド、MovePath.path / PlaceAlongPath.path, spacing, fireDelayFrames のみである。弾を撃つノードや Move などバトルノードのその他のフィールドは通常の値で、パラメータ化できない。

例外として、次のアセット参照は `*Param` 欄（Object パラメータ名）で差し替えられる。値の解決は `ParamObjects.Resolve`（`Parts/ParamObjects.cs`）で、欄が空なら固定値を使う。

| 欄 | 差し替える対象 | パラメータの値が空のとき |
|---|---|---|
| `Move.presetParam` | `MotionPreset` | 動かずに Success |
| `SubTree.treeParam` | 実行するツリー | 何もしない（Success） |
| `SetDeathBehavior.treeParam` | 撃破時のツリー | 撃破時の行動なし |
| `BulletSpec.prefabParam` | 弾 Prefab | `prefab` を使う |

### 5.2 TreeParameter と TreeScope

`TreeParameter` は `name` / `kind`（Float, Int, Bool, Vector2, Object）/ 各型の値 / `bindToParent` を持つ。

`TreeScope`（`Core/TreeScope.cs`）は実行中のツリー 1 本分（SubTree ごとに 1 つ）のパラメータの値で、作るときに次を行う。

1. 親スコープを辿り、同じアセットが既に含まれていれば `InvalidOperationException`（SubTree の循環検出）。
2. このアセットの `parameters` を既定値として登録する。
3. 呼び出し側の `overrides` で同名のものを上書きする。宣言されていない名前の上書きは警告して無視する。
4. 上書き側の `bindToParent` が空でなければ、親スコープの同名パラメータをそのまま引き継ぐ。

パラメータは基本的に **`Setup` 時に 1 回だけ解決される**（各ノードの `OnSetup`）。

例外として、`TreeScope.Set(name, value)` で実行中に値を書き込める。書き込むのは Repeat / Cycle / Loop の `indexParam`（今が何回目か、0 始まり）と Calc の `resultParam` だけ。

- 書き込んだ値は宣言値より優先される。`bindToParent` で引き継いだパラメータへの書き込みは親スコープへ届く。
- 書き込んだ値が効くのは、撃つたびに読み直す欄だけ: Calc の `a` / `b`、`FixedDirection` の `angle`。他の欄は `Setup` 時の値のまま。
- 書き込み先の名前もツリーパラメータとして宣言しておく（エディタで `$` 欄から選べるようにするため）。

### 5.3 SubTree

`SubTree`（`Action/SubTree.cs`）は別の `BehaviorTreeAsset` を自分の位置で実行する。

- `tree`: 実行するツリー
- `treeParam`: 指定すると、その Object パラメータの値のツリーを実行する（5.1）
- `overrides`: 子ツリーのパラメータの上書き（`bindToParent` で親のパラメータを引き継げる）

SubTree は `Setup` 時に子ツリーを `Instantiate(scope, overrides)` で複製して持ち、実行中はそのルートを Tick する。攻撃パターンを `BT/Attack/` に切り出し、ボスの複数フェーズや雑魚から再利用する用途で使う。

`Spawn` ノードの `behavior` / `overrides` でも敵に別ツリーとパラメータ上書きを渡せる。ただしこちらは敵側で親スコープ無しに複製されるため、`bindToParent` は効かない（固定値の上書きのみ有効）。

### 5.4 雑魚の共通ツリー

雑魚は原則として共通ツリー `DataStore/Commons/BT/Enemy.asset` で動かす。

```text
Enemy (パラメータ: movement = MotionPreset, attack = BehaviorTreeAsset, death = BehaviorTreeAsset)
 └─ Parallel(All)[ Move($movement), Sequence[ SetDeathBehavior($death), SubTree($attack) ] ]
```

Spawn（Wave の entries を含む）で `behavior = Enemy` とし、`overrides` で `movement`（`Stage{N}/BT/Motion/`）、`attack`（`Stage{N}/BT/Attack/`）、`death` を指定する。指定しないものは空なので、移動しない / 攻撃しない / 撃破時の行動なしになる。攻撃中に移動を変えるなど、この形に収まらない敵だけ専用ツリーを作る（例: `Stage2/BT/Enemy/S2_2_WayAzuma`）。

## 6. ステージの流れ

道中・会話・ボス戦の進行はすべてステージツリーに書く。`Scripts/Presenter/BattleScenePresenter.cs` はツリーを起動し、`Runtime/BattleFlow.cs` が出すイベントにシーン側の演出で応えるだけである。

### 6.1 ステージツリーの形

Stage1〜4 の実データは次の形をとる（各ノードの詳細は [BehaviorTree-Nodes.md](./BehaviorTree-Nodes.md) 7.5）。

```text
Stage
 ├─ Section "Way"  [Way]  : Sequence[ PlayBgm(BattleWay), 道中 (Wave の並び), Wait 120 ]
 └─ Section "Boss" [Boss] : Sequence[ PlayScenario(ボス前の会話, skipOnResume=ON)
                                        ├─ OnCommand "SpawnBoss"   → SpawnBoss(Stage{N}_Boss)
                                        └─ OnCommand "PlayBossBgm" → PlayBgm(BattleBoss)
                                    , BossBattle
                                    , Wait 120
                                    , PlayScenario(撃破後の会話, skipOnResume=OFF, restorePlayerControl=OFF) ]
```

### 6.2 起動とイベント

```text
InitializeAndStart()
 ├─ StageModel.StageAddress を Addressables で BattleStageAsset としてロード
 │    (アドレス = アセットパス。WaitForCompletion で同期ロード)
 └─ flow.StartStage(stage, startSection = RuntimeModel.CheckpointSection, resume = startSection > 0, OnStageFinished)
      ├─ StageContext を作り、宣言サービスを Begin
      ├─ Services.Progress.StartSection / ResumeStart を設定
      ├─ 弾と敵を事前生成 (4.5)
      └─ ステージツリーを Runner として登録
ツリーが Success で完了 → CompleteStage() (ステージクリア)
```

`BattleFlow` の通知 (IObservable) と Presenter の反応:

| 出来事 | 発生元 | Presenter の反応 |
|---|---|---|
| `OnSectionStarted` | Stage ノードが Section に入った | `RuntimeModel.CheckpointSection` に記録。`StartStage` 後の最初の Section なら背景スクロールを初期化（Way なら流す） |
| `OnBossSpawned` | SpawnBoss ノード（`IBossHost.Spawn`） | 自機の走りアニメを止め、背景を減速 |
| `OnBossBattleStarted` | BossBattle ノード（`IBossHost.StartBattle`） | ボスの当たり判定を一時的に切り、自機のアニメを再開 |
| `OnStageFinished` | ステージツリーの完了 | Success ならステージクリア |

BGM と会話はサービス経由で Presenter が直接受ける。

- `Services.PlayBgm`（PlayBgm ノード）→ `PlayBgm(bgmType)`: `SceneType = stageNumber + 2` と `BgmType` で再生し、曲名を表示する。
- `Services.Scenario`（PlayScenario ノード、`IScenarioService.Play`）→ 会話シーンを Additive ロード。会話中は自機の攻撃入力と当たり判定を無効にし、終了時に当たり判定を戻す。攻撃入力は `restorePlayerControl` が ON のときだけ戻す。

### 6.3 ボスの生成と戦闘

`BattleFlow` が `IBossHost` を実装する。

- `Spawn(BossDefinition)`: Prefab を `spawnPosition` に生成し、`entrance` を `MotionTickable` として流す。ボスは同時に 1 体まで。
- `StartBattle(tree)`: 登場移動を終点まで一気に進めて止め、`BattleActor` を付けて `Services.Boss` に登録する。
- ボス死亡 (`OnDeath`) で `Services.Boss.Stop()`、撃破演出完了 (`OnDeathSequenceCompleted`) で `IsDefeated` が true になり、BossBattle ノードが Success を返す。
- ボス戦の勝敗はボスツリーの結果ではなくボスの撃破で決まる。ボスツリーが先に完了しても BossBattle は終わらない。

### 6.4 会話とコマンド

- PlayScenario ノードは `IScenarioService.Play(data, skip, restorePlayerControl)` を呼び、Presenter は `ScenarioModelRepository.SetData(data)` で流す会話を指定してから会話シーンを開く。
- 会話シーン（`ScenarioScenePresenter`）が自分で処理するのは `ScenarioData` に列挙された演出関数（`ShowCastMessage` / `ShowMessage` / `TypeWriter` / `AutoAdvance` / `ShowCast` / `HideCast` / `ChangeCastAnimation` / `ShakeCastV`）だけで、それ以外の行は `MessageBroker` の `ScenarioCommandMessage` として通知する。会話シーンは BattleScenePresenter を直接呼ばない。
- Presenter は届いたコマンドを `ScenarioCommandQueue` に積み、PlayScenario ノードが毎フレーム取り出して同名の OnCommand の子を実行する。

### 6.5 リトライとデバッグ起動

- Retry / タイトル戻り / ステージ完了では `flow.Stop()` が呼ばれ、ステージツリーの Abort（実行中の会話も閉じる）、ボスの破棄、弾の消去、`BattleClock.End()` → `BattleWorld.Clear()`（tickables・予約の破棄、`SetStage(null)` によるサービスの End）を行う。次の開始時に新しい `BattleWorld` が作られる。`ActorPool` と弾の共有プールは作り直さず使い回す。
- リトライは `CheckpointSection` の Section から `resume = true` で再開する。その Section の `skipOnResume` が ON の Scenario は会話を出さず、コマンドだけを順に即実行する（ボス前の会話を飛ばしてもボスは出る）。
- `Tools/Battle Debug Launcher` と `DebugRuntimeModelInitializer` は `stageNumber` と `startSection`（Stage 直下の並び順）を指定する。途中の Section から始めた場合も resume 扱いで会話を飛ばす。

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
- `BattleFlow.StartStage` が `new StageContext(world, stage.Services)` を作り `BattleWorld.SetStage()` に渡す。サービスはステージ単位で、道中からボス戦まで同じものが続く。
- `StageContext.Tick(dt)` は `BattleWorld.Step` の手順 5 で毎フレーム呼ばれる。
- ノードからは `ctx.Stage?.Get<T>()` / `TryGet<T>()` で取得する。宣言されていないサービスを使おうとしたノードは `StageContext.WarnMissing<T>()` で 1 回だけ警告を出し、何もしない（Success は返す）。

設計方針として、サービスは自分から自機・敵弾・表示へ働きかける。自機や弾の側はサービスを知らず、汎用の受け口だけを公開する（`Runtime/Stage/IMoveModifier.cs` / `IBulletForce.cs`）。

| 受け口 | 実装側 | 内容 |
|---|---|---|
| `IMoveModifier.Modify(velocity, input, speed)` | `PlayerEntityPresenter.AddMoveModifier / RemoveMoveModifier` | 自機の移動速度に補正を加える |
| `IBulletForce` (`Active` / `LaunchAxis` / `Integrate`) | `BulletSystem.AddForce / RemoveForce` | 敵弾の本来の軌道に外力によるずれを足す |

### 7.2 例: WindService

`Runtime/Stage/WindService.cs`。Stage3 の `Stage3_BT.asset` だけが `WindServiceDefinition` を宣言している。

`WindServiceDefinition` のフィールド:

| フィールド | 説明 |
|---|---|
| `initial` (`BattleWindSettings`) | ステージ開始時の風。`intervals` が空なら無風で始まる |
| `arrowsPrefab` (`WindArrowsPresenter`) | 風向きを示す矢印。Begin 時に背景 (`Services.Background`) の子として生成する |

`BattleWindSettings`（`Scripts/Model/BattleWindSettings.cs`）: `enabled`、`startSe`、`playerSpeedRatio`、`intervals`（`WindDirection` × `seconds` のリスト。順に繰り返す。秒単位、最小 0.1）、`bulletAcceleration`（ワールド単位/秒²）、`bulletDrag`（1/秒）。

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
4. サービスを操作するノードが必要なら `BehaviorTree/Action/` などノードの種類のフォルダに追加し、`ctx.Stage?.Get<T>()` が `null` のときは `WarnMissing<T>()` を出して何もしないようにする。
5. 使うステージの `BattleStageAsset.services` に定義を追加する。宣言しないステージでは存在しない扱いになる。

## 8. 弾と移動

### 8.1 弾の発射

発射系ノード（SingleShot / NWayShot などの弾を撃つノード、BulletBeam、MoveTrailFan、PlaceAlongPath）はすべて `Parts/Emitter.cs` の `Emitter.Spawn` を通して `BulletSystem.Spawn` を呼ぶ。

- 方向は `DirectionSource`、弾の回転は `RotationSource`、弾の設定は `BulletSpec`、撃ち方は `ShotPattern` で指定する（詳細はノードリファレンス）。
- 発射位置は「撃つ敵の位置 + muzzleOffset + パターンごとのずれ」。ステージ直下（`Self == null`）なら原点基準。
- パターンの `SpawnDelayFrames` が付いた弾は `BattleWorld.Schedule` で予約され、予約時点ではなく出現時点の撃ち手の位置から出る。撃ち手が倒れると予約は取り消される（死亡時ツリーから撃った弾は取り消し対象にしない）。
- 方向は発射ノードを実行したフレームに 1 回だけ決める。

`BulletSystem`（`Runtime/BulletSystem.cs`）は弾 Presenter を弾 Prefab ごとの共有プール（`BulletPool.GetOrCreateSharedPool`）から取り出し、`BulletEntityPresenter.InitializeDriven` で初期化する。`BulletEntityPresenter` は自前の移動を持たず、見た目・当たり判定・寿命・画面外判定だけを担う。位置は常に `BulletSystem` が与える。

- 撃ち手の Prefab に付いた `BulletPool` コンポーネントは、弾 Prefab の保持（`BulletPrefab`）と共有プールの作成だけを行う。共有プールは作ったシーンが閉じたときだけ破棄される（会話シーンの開閉では破棄しない）。
- 自機の弾も `BulletSystem` で撃つ。`PlayerEntityPresenter` は `BattleFlow` から `BindBulletSystem` で受け取った `BulletSystem` に、`VelocityMotion`（速さは Inspector の `normalShotSpeed` = 5 / `chargedShotSpeed` = 0）の弾を出す。旧来と同じく OutQuad の時間写像（8.2）が掛かるので、速さ 5 は実際には約 10/秒で飛ぶ。チャージ弾は `BulletSpawn.IsPlayerChargeBullet` を ON にして出す（`BulletEntityModel.IsPlayerChargeBullet` になり、「チャージ攻撃のみ有効」の敵に効く）。
- Stage3 の双子中ボス（`TwinParabolaMidBossController`）は行動を自前で進めるが、放物線弾は `BulletSystem` に `VertexParabolaMotion` で渡す。

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

旧実装との互換のため、`StepClock`（`PullRunner.cs`）は旧 `PullMovementHelper` の挙動を再現する。旧実装では Ease 未指定の移動に DOTween 既定の OutQuad が掛かっていたため、`legacyOutQuad` が ON のステップ（Velocity、AimStraight、Forward、Homing、StraightWithHoming、Sine、Reflect、FadeMove）では実時間 t に対し内部時間 τ = D(2u − u²)（u = t/D）を使う。duration 0（無限、内部的には D = 999999）では序盤がほぼ 2 倍速になる。この癖は意図的に残している。同様に `MotionEase` / `MotionEasing` でも `Ease.Unset` は OutQuad として扱う。

## 9. エディタ

`Window > Battle > Behavior Tree Editor`、または `BehaviorTreeAsset` をダブルクリックで開く（`Editor/BehaviorTree/BehaviorTreeEditorWindow.cs`）。

画面構成:

| 領域 | 内容 |
|---|---|
| ツールバー | 対象アセット、`Auto Layout`（ルートから下向きに整列）、`Frame All`、`Side Panel`（右ペインの表示切替）、`MiniMap` |
| 左ペイン (`NodePaletteView`) | ノード一覧（Composite / Decorator / Condition / Action とサブカテゴリ）と SubTree 用の BT アセット一覧。検索欄あり。項目をキャンバスへドラッグして作成 |
| 中央 (`BehaviorTreeGraphView`) | ノードグラフ。上が親、子は左から右の順に実行される。ヘッダーの番号は親の中での実行順。Wait を含む Sequence の子には、Sequence 開始から何フレーム目に動くか（`@150f`）も出す。時間の読めない子（Move / SubTree など）より後ろには出さない |
| 右ペイン `Node` タブ (`NodeInspectorView`) | 選択中のノードの種類・分類・説明・要約・コメント・設定欄。再生中は実行中かどうかも出す。設定欄はノード内の欄と同じ値に結びついており、どちらで編集してももう片方に反映される。複数選択時は個数だけを出す |
| 右ペイン `Parameters` タブ (`ParameterPanelView`) | ツリーパラメータの一覧（名前 / 型 / 既定値）。`+` で追加、右クリックで `Select Usages` / `Delete` |

操作:

- ノード作成: パレットからドラッグ、キャンバスの右クリック（Create Node）で検索ウィンドウ、または出力ポートから線を引いて何もない所で離すと、その先に接続した状態で作成。ドロップ位置に子を受け付けるノードがあれば、その最後の子として接続される。
- Project ウィンドウの `BehaviorTreeAsset` をキャンバスへドロップすると SubTree ノードになる。SubTree ノードの右クリック `Open SubTree` で子ツリーを開く。
- コピー & ペースト、Undo / Redo に対応。ノード右クリックの `Edit Comment` でコメントを付けられる。
- 値欄 (`BtInt` など) の `▾` から、パラメータへの結びつけ・`固定値を使う`・`今の値で新しいパラメータを作成` を選べる。右ペインのパラメータ行を値欄へドラッグしても結びつけられる。型の合わないパラメータは警告表示になる。
- パラメータ名を変えると、参照している値欄・`bindToParent`・`*Param` 欄も追従する（`ParameterUtility.FindReferencePaths`）。
- パラメータを削除すると、参照していた値欄は固定値に戻る。
- 再生中は、最近 Tick されたノードがハイライトされる（`BehaviorTreeDebug`）。同じ定義を複数の敵が使っている場合はまとめて光る。

- PlayScenario ノードの右クリック `会話のコマンドから OnCommand を作る` で、会話に出てくるのに OnCommand が無いコマンドの子をまとめて作れる。

性能上の工夫として、ノードの入力欄（PropertyField）は選択中のノードにだけ作り、それ以外はヘッダーと一行要約（`Node.Summary`）だけを表示する。構造変更時もグラフ全体を作り直さず、増減した要素だけを反映する。

## 10. 設計判断

| 判断 | 内容 |
|---|---|
| BT の適用範囲 | ステージ層とアクター（敵/ボス）層に適用する。敵弾は数が多く個々に判断を持たないため、BT の外の `BulletSystem` が `MotionStep` で動かす |
| 時間の基準 | BT はフレーム基準（60fps 固定）。描画フレームとは独立した `BattleClock` で進めるため、処理落ちしても弾幕の相対タイミングが崩れない。移動 (`MotionStep`) の長さは旧データとの互換で秒指定 |
| 割り込み | 「条件成立で中断」は Until デコレータで表す。毎フレーム子より先に条件を評価し、成立したら子を Abort する。フェーズの終了条件・HP による打ち切りはすべてこれ |
| Blackboard を持たない | ノード間の動的な値の受け渡しはしない。共通化したい値はツリーパラメータで `Setup` 時に解決する |
| 再利用 | 攻撃パターンや敵の行動は `BehaviorTreeAsset` として切り出し、SubTree / Spawn.behavior で参照する。コピーせず参照するので、元アセットの修正がすべての使用箇所に効く。雑魚は共通ツリー `Enemy` に移動・攻撃・撃破時の行動をパラメータで渡す |
| 敵のプール | 雑魚は `ActorPool` で使い回し、ステージ開始時に事前生成する。敵を消す処理は `ActorDespawn.Despawn` に一本化する |
| ノードの形 | ノード 1 種類 = 1 クラス（設定・状態・処理を 1 つに持つ）。アセット上のノードは雛形で、実行するツリーごとに複製する。OSS の BT と同じ形で、「設定のクラス」と「実行時のクラス」を 2 つ組で書かない |
| 保存形式 | ScriptableObject + `[SerializeReference]`。実行中の状態は `[NonSerialized]` にしてアセットに残さない |
| 旧挙動の再現 | 旧実装の「Ease 未指定時は OutQuad」の癖は `StepClock` / `MotionEasing` で再現し、移行前後で弾道を変えない |
| ボスと雑魚の統一 | ボスと通常敵は同じ `BattleActor` + 同じノード群で攻撃・移動を解釈する。ボス固有なのは HP 系条件の対象 (`HpTarget.Boss`) と EnterStrongMode 程度 |
| ステージ固有機能 | `BattleStageAsset.services` に宣言するステージサービスとしてカプセル化する。サービスが自機・弾へ働きかけ、自機・弾は汎用の受け口だけを持つ |
| 進行もツリーに書く | 道中・会話・ボス戦の順序と、会話中のコマンド（ボスの出現・BGM）はステージツリーに書く。Presenter はツリーからの通知に演出で応えるだけで、進行を判断しない |
| 1 ステージ = 1 アセット | 道中とボス戦を別アセットに分けず、Section で区切る。リトライ地点は Section 単位 |

## 11. 既知の制約・注意点

- **乱数は再現性がない**: `BattleWorld` の乱数シードは `Environment.TickCount`。さらに `DriftMotion` は `UnityEngine.Random`、自機狙い放物線は static な発射カウンタ (`AimedParabola.NextShotIndex`) を使うため、同じ入力でも同じ展開にはならない。
- **パラメータ化できるフィールドが限られる**: `Bt*` 型のフィールドは時間・回数系などの一部だけ、アセット参照は `*Param` 欄のある 4 つだけ（5.1 参照）。
- **プール品の状態**: 使い回す敵の Prefab に、`Start` などで一度だけ状態を作るコンポーネントがあると 2 体目以降で動きが変わる。`IPoolableActor` で戻せないものは `NoActorPool` を付ける。
- **死亡時ツリーは 1 フレームだけ**: `SetDeathBehavior` のツリーは 1 回 Tick して停止する。Wait や Delay を含めても後半は実行されない。
- **ツリー開始は翌フレーム**: 出現させた敵のツリー、`StartStage` で登録したステージツリーは次の `Step` から動く。
- **秒とフレームの混在**: BT ノードの時間はフレーム、`MotionStep` と一部のバトルノードのフィールド（Spawn.beamDuration、SpriteBeam.bodyDuration、MoveTrailFan.moveDuration、ScreenEdgeDirection など）は秒。各フィールドの単位はノードリファレンスを参照。
- **PhaseMarker の phaseId は現状使われていない**: `BossPhaseProgressGauge` は通知の回数だけを見て、BGM のサンプル位置から現在フェーズを推定する。
- **型の置き場所**: `WindDirection` / `WindInterval` / `BattleWindSettings` は `Model/` に 1 型 1 ファイルで、`BossWindCueSettings` は `Model/BossWindCueSettings.cs`、自機の Transform を持つ `BattleTargets` は `Runtime/Motion/BattleTargets.cs` にある。
- `BossEntityPresenter` は HP・被弾・撃破演出・登場直後の当たり判定オフだけを持つ。`Stage3WindBossController` は BossWindCue ノードの腕上げ演出の受け手（`IWindCueReceiver`）として残っている。

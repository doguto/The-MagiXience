# Battle BehaviorTree ノードリファレンス

BT で使えるノード・条件・部品の一覧と、各フィールドの意味・単位・戻り値をまとめる。
仕組み全体（ライフサイクル、パラメータ、ステージの流れ）は [BehaviorTree.md](./BehaviorTree.md) を参照すること。

## 0. 読み方

- **単位**: 「f」はフレーム（60f = 1 秒）、「s」は秒。BT ノードの時間は原則フレーム、`MotionStep` と一部のフィールドは秒である。
- **`$` 可**: `BtInt` / `BtFloat` など、ツリーパラメータに結びつけられるフィールド。それ以外は固定値のみ。
- **フレームの数え方**: ノードを開始したフレームを f とする。`Wait(N)` は f+N のフレームで Success を返す。`Delay(N)` の子は f+N のフレームで初めて Tick される。
- **即時ノード**: Action の多くは開始したフレームのうちに Success を返す（以下「即 Success」）。時間を空けるには Wait / Delay / Repeat などと組み合わせる。
- エディタのパレット上の分類は `NodeInfoAttribute` の `category`（未指定なら基底クラスで決まる）。

ソース:

| 種類 | ファイル |
|---|---|
| Composite | `Scripts/BehaviorTree/Composite/*.cs` |
| Decorator | `Scripts/BehaviorTree/Decorator/*.cs`、`Battle/StageNodes.cs`（WithWind, WithPrefab） |
| 条件 | `Scripts/BehaviorTree/Condition/Condition.cs`、`Battle/StageNodes.cs` |
| 基本 Action | `Scripts/BehaviorTree/Action/BasicActions.cs` |
| バトル Action | `Scripts/BehaviorTree/Battle/FireNodes.cs` / `BeamNodes.cs` / `ActorNodes.cs` / `StageNodes.cs` |
| 発射の部品 | `Scripts/BehaviorTree/Battle/EmitterSources.cs`、`FireNodes.cs` |
| MotionStep | `Scripts/Runtime/Motion/MotionSteps.Basic.cs` / `MotionSteps.Advanced.cs` |

## 1. Composite

### Sequence

子を左から順に実行する。

- 子が Success したフレームのうちに次の子を開始する（同じフレームで複数の即時ノードを連続実行できる）。
- 子が Running ならそこで止まり Running。子が Failure なら Failure（以降の子は実行しない）。
- 全ての子が Success なら Success。子が 0 個なら即 Success。

### Selector

子を左から順に試す。

- 子が Failure したフレームのうちに次の子を試す。
- 子が Running ならそこで止まり Running。子が Success なら Success。
- 全て Failure なら Failure。一度 Running になった子が後から Failure しても、次の子へ進む。

### Parallel

全ての子を毎フレーム、左から順に Tick する。

| フィールド | 説明 |
|---|---|
| `policy` | `RequireAll`: 全ての子が Success したら Success / `RequireOne`: どれか 1 つが Success したら、残りを Abort して Success |

- どちらのポリシーでも、子が 1 つでも Failure したら残りを Abort して Failure。
- 完了した子は以後 Tick しない。

### Random

開始時（Enter）に子を 1 つ抽選し、その子の結果をそのまま返す。

| フィールド | 説明 |
|---|---|
| `weights` | 子の並び順に対応する重み。足りない分は 1、負の値は 0 として扱う |
| `avoidRepeat` | ON（既定）なら直前に選んだ子を連続で選ばない（子が 2 つ以上のとき） |

- 「直前」は同じ実行時ノードで前回選んだ子。Loop の中に置くと、周回をまたいで連続を避ける。
- 子が 0 個なら即 Success。

### Timeline

子を同時に実行し、`duration` で打ち切る。各子の開始時刻は子側に Delay を挟んで指定する。

| フィールド | 単位 | 説明 |
|---|---|---|
| `duration` ($ 可) | f | 0 より大きければ、このフレーム数経過した時点で実行中の子を Abort して Success。0 なら全ての子の完了を待つ |

- `duration > 0` の場合、子は f 〜 f+duration−1 の duration フレームだけ Tick され、f+duration のフレームで Success を返す。子が全て早く終わっても duration までは Running のまま。
- Loop の子に置くと、Loop の同フレーム再開規則（後述）により **ちょうど duration フレーム周期** で繰り返す。攻撃パターンの 1 周期と、`SetMovement` の移動 1 周の長さを揃える用途で使う。
- `duration = 0` の場合は全ての子が Success したフレームで Success。
- どちらでも子が Failure したら残りを Abort して Failure。

## 2. Decorator

### Loop

子を指定回数繰り返す。

| フィールド | 単位 | 説明 |
|---|---|---|
| `count` ($ 可) | 回 | 0 = 無限 |

- 子が Failure したら Failure。`count` 回 Success したら Success。
- **同フレーム再開規則**: 前フレーム以前に始まった周が完了した場合は、同じフレームのうちに次の周を開始する（周期がずれないように）。同じフレーム内で始まって完了した周（中身が全て即時ノードの場合など）の次は、翌フレームに回す（無限ループ防止）。

### Repeat

子を `interval` フレームおきに開始する。前の回が終わっていなくても次を開始し、複数の回が並行して進む。

| フィールド | 単位 | 説明 |
|---|---|---|
| `count` ($ 可) | 回 | 開始する回数。0 = 無限 |
| `interval` ($ 可) | f | 開始の間隔。既定 60、最小 1 |

- 1 回目は開始フレーム f、k 回目は f + (k−1)×interval に開始する。開始したフレームのうちにその回を Tick する。
- 実行中の回のどれかが Failure したら、全ての回を Abort して Failure。
- `count > 0` なら、指定回数を開始し全ての回が終わったフレームで Success。`count = 0` なら終わらない（親の Until などで打ち切る）。
- 回ごとの実行時ノードは使い回す（同時に走る数だけ生成される）。

### Until

条件が成立した瞬間に子を中断して完了する。フェーズの終了条件や割り込みはこれで表す。

| フィールド | 説明 |
|---|---|
| `condition` | 条件（`ConditionDefinition`）。未設定なら成立しない |
| `onChildCompleted` | 子が条件成立より先に完了したときの扱い（下表） |

| `onChildCompleted` | 動作 |
|---|---|
| `Finish`（既定） | 子の結果（Success / Failure）をそのまま返して終了 |
| `Restart` | Running を返し、翌フレームから子をやり直す。条件成立まで続ける |
| `Hold` | 子の完了後は何もせず、条件成立まで Running で待つ |

- 開始時に条件の `Reset` を呼ぶ（FrameElapsed はここから数える）。
- 毎フレーム、子より先に条件を評価する。成立したらそのフレームでは子を Tick せず Abort して Success。
- 開始したフレームで既に成立していれば、子を一度も実行せずに Success。

### Delay

`frames` フレーム待ってから子を実行する。内部的には `Sequence[Wait(frames), child]`。

| フィールド | 単位 | 説明 |
|---|---|---|
| `frames` ($ 可) | f | 子を開始するまでの待機フレーム数 |

子の結果をそのまま返す。

### Inverter

子の Success / Failure を反転する。Running はそのまま。

### ForceSuccess

子の結果に関わらず Success を返す。Running はそのまま。

### WithWind

子の実行中だけ風を吹かせる。カテゴリは Decorator。

| フィールド | 説明 |
|---|---|
| `wind` (`BattleWindSettings`) | `enabled` / `startSe` / `playerSpeedRatio`（0〜1）/ `intervals`（`WindDirection` × `seconds`(s) を順に繰り返す）/ `bulletAcceleration`（ワールド単位/s²）/ `bulletDrag`（1/s） |

- 開始時に `WindService.Current` を退避して `Apply(wind)`、終了・中断時に退避した風へ戻す。
- ステージに `WindService` が宣言されていなければ警告して子だけを実行する。
- 子の結果をそのまま返す。

### WithPrefab

子の実行中だけ Prefab を出しておく（障害物・演出など。旧 Control トラック相当）。

| フィールド | 説明 |
|---|---|
| `prefab` | 出す Prefab |
| `position` | ワールド座標。回転は Prefab 本来の回転 |

開始時に Instantiate、終了・中断時に Destroy する。子の結果をそのまま返す。

## 3. 条件 (ConditionDefinition)

Until / CheckCondition / WaitUntil の `condition` に設定する。

| 条件 | フィールド | 成立する条件 |
|---|---|---|
| `Constant` | `value` | 常に `value` |
| `FrameElapsed` | `frames` ($ 可, f) | Reset（Until などの開始）からの経過フレーム ≧ frames |
| `BgmReached` | `seconds` ($ 可, s) | `AudioSource.time` ≧ seconds |
| `BgmSamples` | `thresholdSamples`（サンプル） | BGM の `timeSamples` が閾値に達した、またはループで先頭へ戻った。開始時点で既に閾値を過ぎていれば、ループで戻るのを待つ。BGM が無ければ常に成立 |
| `NoActiveEnemies` | なし | `EnemyTracker` の生存敵数が 0（Spawn の `tracked` が ON の敵だけが数えられる）。Spawner が無ければ成立 |
| `HpBelow` | `target`（Self / Boss）、`percent`（0〜100） | 現在 HP ≦ ceil(最大 HP × percent / 100)。対象の Model が無ければ成立 |
| `NormalHpDepleted` | `target`（Self / Boss） | 対象がボスで、通常 HP を削り切った（`BossEntityModel.ShouldUseStrongAttack`）。ボス以外は不成立 |
| `And` | `conditions` | 全て成立（空なら成立） |
| `Or` | `conditions` | どれか成立（空なら不成立） |
| `Not` | `condition` | 中の条件が不成立（未設定なら成立） |

- `HpTarget.Self` はツリーを持つアクター自身、`Boss` は `Services.Boss`（ボス戦で `StartBossTree` された BattleActor）。ステージ直下のツリーからボスの HP を見るときは `Boss` を使う。
- 移行データの FrameElapsed は 601 や 1801 のように +1 された値が多い。旧データのループ終端時刻ちょうどの発射を含めるために移行ツールが付けたもの。

## 4. 基本 Action

### Wait

| フィールド | 単位 | 説明 |
|---|---|---|
| `frames` ($ 可) | f | 待機フレーム数。0 なら即 Success |

開始フレーム f から数えて f+frames のフレームで Success。

### CheckCondition

条件を 1 回評価し、成立なら Success、不成立なら Failure。条件未設定なら Success。カテゴリは Condition。

### WaitUntil

条件が成立するまで Running、成立したフレームで Success。条件未設定なら即 Success。カテゴリは Condition。

### Log

`message` を `[BehaviorTree] frame=... message` 形式でコンソールに出し、即 Success。デバッグ用。

### SubTree

別の `BehaviorTreeAsset` をこの位置に展開する。パレットには個別ノードとしてではなく、BT アセットの一覧として表示される。

| フィールド | 説明 |
|---|---|
| `tree` | 実行するツリー。未設定なら警告して即 Success |
| `overrides` | 子ツリーのパラメータの上書き（`TreeParameter`）。`bindToParent` に親ツリーのパラメータ名を入れると、その値を引き継ぐ |

Build 時に展開されるため、実行時の戻り値は子ツリーのルートと同じ。循環参照は Build 時に例外になる。

## 5. バトル Action: 発射

### Fire（Action/Attack）

弾を撃つ。即 Success。

| フィールド | 説明 |
|---|---|
| `bullet` (`BulletSpec`) | 弾の設定（後述） |
| `pattern` (`ShotPattern`) | 撃ち方。既定は Single |
| `direction` (`DirectionSource`) | 基準方向。既定は Fixed(左) |
| `rotation` (`RotationSource`) | 弾の回転（Prefab 本来の回転に対するオフセット）。既定は Inherit |
| `muzzleOffset` | 撃つ敵の位置からのずれ（ワールド単位）。ステージ直下では原点からのずれ |
| `se` | 発射時に 1 回鳴らす SE。`None` なら鳴らさない |

- 方向は実行したフレームで決める。パターンの `SpawnDelayFrames` が付いた弾は予約され、出現時点の撃ち手の位置から出る。
- `DirectionSource` が発射位置を返す場合（ScreenEdge）、その位置を使う。

### BulletBeam（Action/Attack）

警告線を出したあと、同じ位置・向きに弾を一定間隔で連射してビームにする。即 Success（連射は予約で行う）。

| フィールド | 単位 | 説明 |
|---|---|---|
| `origin` | | `Self`: 撃つ敵の位置 + `position`（連射ごとにその時点の位置）/ `World`: `position` をワールド座標として固定 |
| `position` | ワールド単位 | 上記の基準位置またはずれ |
| `direction` | | 基準方向 (`DirectionSource`) |
| `pattern` | | ビームの本数と角度 (`ShotPattern`)。パターンの各方向に 1 本ずつ出す |
| `rotation` | | 弾の回転。既定は Fixed(0°) |
| `range` | ワールド単位 | 弾の飛距離と警告線の長さ。0 なら弾/警告線 Prefab の値 |
| `bullets` | | 1 回の連射で撃つ弾 (`List<BulletSpec>`)。複数なら同じ位置・向きに重ねて撃つ |
| `shotCount` | 回 | 連射回数（最小 1、既定 36） |
| `shotIntervalFrames` | f | 連射の間隔（最小 1、既定 5） |
| `warningPrefab` | | 警告線の Prefab。未指定なら警告線を出さない |
| `warningFrames` | f | 連射を始めるまでの待ち。警告線を出さない場合も待つ（既定 60） |
| `warningDisplayFrames` | f | 警告線の表示時間。0 なら `warningFrames` と同じ |
| `warningWidth` | ワールド単位 | 警告線の幅。0 以下は Prefab の値 |
| `se` | | 連射 1 回ごとに鳴らす SE |

- 向きと本数は実行時に 1 回だけ決める。
- i 回目の連射は実行から `warningFrames + i × shotIntervalFrames` フレーム後。撃ち手が倒れると以降の連射は取り消される。

### SpriteBeam（Action/Attack）

警告線のあと、1 枚絵のビーム本体を出す（旧 SpriteBeamAttackSignal）。即 Success。

| フィールド | 単位 | 説明 |
|---|---|---|
| `line` (`start` / `end`) | ワールド単位 | ビームの線分。向き・長さはこの線分から決まる |
| `isRelative` | | ON なら線分を撃つ敵の位置からの相対にする（実行時点の位置で固定） |
| `bodyPrefab` | | ビーム本体の Prefab (`IBeamVisualReceiver`) |
| `bodyDuration` | s | 本体の表示時間 |
| `showWarning` / `warningPrefab` | | 警告線の有無と Prefab。どちらかが欠けると警告なしで本体を即出す |
| `warningFrames` | f | 警告線の表示時間。本体はこの後に出る |

### MoveTrailFan（Action/Attack）

この敵が移動しながら軌跡上に弾を置き、移動後に全弾を扇状に同時に放つ（旧 MoveTrailFanBulletSignal）。移動と配置が終わるまで Running。`Self` が無ければ Failure。

| フィールド | 単位 | 説明 |
|---|---|---|
| `targetOffset` / `isRelative` | ワールド単位 | 移動先。ON なら現在位置からの相対 |
| `moveDuration` | s | 移動時間 |
| `moveEase` | | 移動の Ease (`MotionEase`) |
| `emitBullets` | | OFF なら移動だけ |
| `bullet` / `direction` / `rotation` / `se` | | Fire と同じ。方向は開始時に決める |
| `wayCount` | 発 | 弾数。移動の進捗が i/(n−1) に達した位置に i 発目を置く |
| `spreadAngle` | 度 | 扇の隣り合う弾の角度 |
| `releaseDelayFrames` | f | 移動終了から全弾が動き出すまで |

### 発射の部品: BulletSpec

| フィールド | 説明 |
|---|---|
| `prefab` | `BulletEntityPresenter` を持つ弾 Prefab。未設定なら何も出さない |
| `damage` | ダメージ（既定 10） |
| `motionPreset` | 指定するとインラインの `motion` より優先 |
| `motion` | 弾の移動 (`List<MotionStep>`)。空なら発射方向へ等速 (`VelocityMotion`) |
| `range` | 飛距離の上限（発射方向への進捗、ワールド単位）。0 で無制限 |
| `affectedByWind` | 発射時に風 (`IBulletForce`) が有効なら外力を受ける（既定 ON） |

### 発射の部品: ShotPattern

基準方向から弾ごとの方向・ずれ・遅延を作る。

| パターン | フィールド | 動作 |
|---|---|---|
| `SingleShot` | なし | 基準方向に 1 発 |
| `NWayShot` | `wayCount`、`spreadAngle`(度、隣り合う弾の角度)、`radius`(発射位置を各方向へずらす距離)、`startDelayFrames`(f、動き出すまで)、`rotationOffset`(度、反時計回り) | 同時に扇状に撃つ。奇数は中央に 1 発、偶数は中央を空ける |
| `SequentialFanShot` | `wayCount`、`spreadAngle`(度)、`spawnIntervalFrames`(f)、`releaseDelayFrames`(f) | 扇状に 1 発ずつ `spawnIntervalFrames` おきに出し、最後の 1 発から `releaseDelayFrames` 後に全弾同時に動き出す |
| `SequentialCircleShot` | `wayCount`、`radius`、`spawnIntervalFrames`(f)、`releaseDelayFrames`(f) | 円周上（360°/wayCount 間隔、半径 radius）に 1 発ずつ出し、全弾同時に動き出す |
| `ChargeShot` | `wayCount`、`radius`、`targetRadius`、`gatherDuration`(s)、`rotationSpeed`(度/s、負で時計回り)、`startDelayFrames`(f) | 周囲に弾を並べ、渦を巻きながら `targetRadius` まで集める。弾の移動は `SpiralMotion` に置き換わり、`BulletSpec.motion` は使われない |

### 発射の部品: DirectionSource

| 種類 | フィールド | 方向 |
|---|---|---|
| `FixedDirection` | `direction` | 固定方向（正規化。0 なら左） |
| `AimDirection` | `maxUpAngle` / `maxDownAngle`（度、0〜180） | 自機狙い。左向きを 0° として上下の振れ幅を制限する。両方 180 なら制限なし。自機がいなければ左 |
| `ForwardDirection` | なし | 撃つ敵の向き（回転 × 左）。ステージ直下では左 |
| `ScreenEdgeDirection` | `fromLeft`、`followWind`、`edgeInset`、`verticalMargin`、`lateralOffset`、`angularSpeed`、`attackTimeOffset` | 画面端から撃つ（下記） |

`ScreenEdgeDirection` の詳細:

- 方向: `followWind` が ON で `WindService` の風が吹いていれば風下向き、それ以外は `fromLeft` なら右向き、OFF なら左向き。
- 発射位置: 方向の反対側（風上）の画面端。`edgeInset`（ワールド単位、端からの内側距離。負で画面外）だけ内側。
- 端に沿った位置は `AttackTime`（撃ち手が動き出してからの秒数、ステージ直下ならバトル経過秒）で往復する: `sin(max(0, AttackTime − attackTimeOffset[s]) × angularSpeed[rad/s])` を `verticalMargin`（ワールド単位、端の両側の余白）を除いた範囲に写し、`lateralOffset` を足す。

### 発射の部品: RotationSource

弾（または Spawn で出す Prefab）の回転を決める。

| 種類 | フィールド | 回転 |
|---|---|---|
| `InheritRotation` | なし | 撃つ敵の回転 |
| `FixedRotation` | `degrees` | Z 軸回りの固定角度 |
| `OffsetRotation` | `offsetDegrees` | 撃つ敵の回転 + オフセット |
| `AimRotation` | なし | 左向きを基準に自機の方向へ向ける |

## 6. バトル Action: アクター

`Self` を対象にするノードは、ステージ直下（`Self == null`）では何もしない（Move のみ Failure）。

### Move（Action/Move）

この敵を `MotionStep` の列で動かす。移動が終わると Success（無限の移動は終わらない）。`Self` が無ければ Failure。

| フィールド | 説明 |
|---|---|
| `preset` | `MotionPreset`。指定するとインラインより優先 |
| `steps` | インラインの `List<MotionStep>` |

中断・完了時に移動を止める。ツリーと移動を独立させたい場合は SetMovement を使う。

### SetMovement（Action/Move）

この敵の移動を差し替える。移動はツリーと独立して流れ続け、ノードは即 Success。

| フィールド | 説明 |
|---|---|
| `preset` / `steps` | Move と同じ。空なら移動を止める |
| `continueIfSame` | ON なら、同じ移動（同じリスト参照）が再生中のときはやり直さない。フェーズをまたいで続く移動に使う |

### SetDeathBehavior（Action/Actor）

この敵が倒されたときに 1 回だけ実行するツリー (`tree`) を設定する。即 Success。
死亡時ツリーは 1 フレームだけ Tick して停止するため、即時ノード（Fire など）だけが意味を持つ。

### SetInvincible（Action/Actor）

`invincible`（既定 ON）でこの敵の無敵を切り替える。即 Success。

### Despawn（Action/Actor）

この敵を消す（撃破扱いにしない）。この敵が予約していた発射も取り消す。即 Success。

### PlaySe（Action）

`se` を鳴らす。即 Success。

### Spawn（Action/Spawn）

敵・障害物・ビームなどの Prefab を出現させる。即 Success。

| フィールド | 単位 | 説明 |
|---|---|---|
| `prefab` | | 出す Prefab |
| `behavior` | | この Prefab を動かすツリー。指定すると Prefab 側の旧来の移動・攻撃を止めて `BattleActor` を付ける。未指定なら Prefab の旧来の動き |
| `overrides` | | `behavior` のパラメータ上書き（固定値のみ。`bindToParent` は効かない） |
| `position` | ワールド単位 | 撃つ敵がいなければワールド座標、いればその位置からの相対 |
| `worldSpace` | | ON なら撃つ敵がいてもワールド座標で出す |
| `layout` | | 並べ方 (`SpawnLayout`、下表)。既定は Single |
| `rotation` | | 出す Prefab の基準回転 (`RotationSource`)。未設定なら回転なし |
| `direction` | | 方向 (`DirectionSource`)。現状のレイアウトはいずれもこの値を使わない |
| `tracked` | | ON（既定）なら `EnemyTracker` に登録し、NoActiveEnemies の判定対象にする |
| `beamRange` | ワールド単位 | ビーム/警告線の長さ。0 以下は Prefab の値 |
| `beamDuration` | s | ビーム/警告線の表示時間。0 以下は Prefab の値 |
| `beamWidth` | ワールド単位 | ビーム/警告線の幅。0 以下は Prefab の値 |

`beam*` は Prefab が `IBeamVisualReceiver` を持つ場合だけ使われる。出現した敵のツリーは翌フレームから動く。

### SpawnLayout

| 種類 | フィールド | 並べ方 |
|---|---|---|
| `SingleLayout` | `offset` | 1 体。基準位置 + offset |
| `LineLayout` | `count`、`spacing`、`lineAxis`(既定 上)、`centerOffset` | 中心から `lineAxis` 方向に等間隔で一列 |
| `FanLayout` | `wayCount`、`spreadAngle`(度)、`offset` | `offset` を扇の角度だけ回した位置に並べ、各体もその角度だけ回す。角度の決め方は NWay と同じ |

各体の回転は「レイアウトの回転 × `rotation` の回転」。

## 7. バトル Action: ステージ・ボス

### EnterStrongMode（Action/Boss）

`target`（Self / Boss）のボスを発狂 (Strong) 状態にする（`BossEntityModel.EnterStrongMode`）。ボス以外には何もしない。即 Success。

### ClearBullets（Action/Stage）

画面の敵弾（`BulletSystem`）と、`BulletClearReceiver` 経由で警告線・ビームなどを消す。`clearEnemies` が ON なら敵も消す。即 Success。

### BossWindCue（Action/Boss）

合図の演出のあと風向きを切り替える。即 Success（切り替え自体は風の予定表で遅らせる）。

| フィールド (`cue`: `BossWindCueSettings`) | 単位 | 説明 |
|---|---|---|
| `enabled` | | 未使用（ノードでは常に実行される） |
| `direction` | | 切り替え後の風向き |
| `cueSeconds` | s | この秒数の間は直前の風を保ち、その後 `direction` に切り替える |
| `playerSpeedRatio` / `bulletAcceleration` / `bulletDrag` | | 切り替え後の風の強さ |
| `cueScale` | | 合図中のスケール変化率（`IWindCueReceiver` 側が使う） |

実行時に `WindService.Apply` で「直前の向き × cueSeconds → direction × 3600 秒」の予定表を入れ、`Self` が `IWindCueReceiver` を持っていれば `PlayCue(cue)` を呼ぶ。`WindService` が無ければ警告して何もしない。

### PhaseMarker（Action/Stage）

`phaseId` を `BattleServices.PhaseStarted` で通知する。即 Success。`BossPhaseProgressGauge` がボスのフェーズ区切りの検出に使う（現状 `phaseId` の値は見ていない）。

## 8. MotionStep

敵・ボス・弾の移動の 1 ステップ。`Move` / `SetMovement` / `BulletSpec.motion` / `BattleStageAsset.bossEntrance` / `MotionPreset` に並べる。
**時間はすべて秒**。`duration` が「0 で無限」のステップは終わらないため、後ろのステップに進まない。

「発射方向」は弾なら発射時の方向、敵/ボスでは無し（ゼロ）。「OutQuad 再現」が付いたステップは、時間の進みに旧実装の OutQuad 癖が掛かる（[BehaviorTree.md](./BehaviorTree.md) 8.2 参照）。

### 基本 (`MotionSteps.Basic.cs`)

| ステップ | フィールド | 動作 |
|---|---|---|
| `WaitMotion` | `duration`(s)、`invincible` | その場で待つ。`invincible` は弾のみ有効（待機中無敵） |
| `VelocityMotion` | `velocity`(単位/s)、`duration`(s, 0=無限) | 等速移動。発射方向があればその向きに \|velocity\| の速さ。OutQuad 再現 |
| `MoveToMotion` | `targetOffset`、`isRelative`、`duration`(s)、`ease`(`MotionEase`) | 指定位置へ補間移動。`isRelative` が ON なら開始位置からの相対 |
| `MoveToPlayerMotion` | `duration`(s)、`x`、`y`、`ease` | 開始時の自機位置へ補間移動。`x` / `y` で追う軸を選ぶ（両方 OFF なら待つだけ） |
| `AimStraightMotion` | `speed`(単位/s)、`duration`(s, 0=無限) | 開始時の自機方向へ直進。OutQuad 再現 |
| `ForwardMotion` | `speed`、`duration`(s, 0=無限) | 開始時の向き（回転 × 右）へ直進。OutQuad 再現 |
| `HomingMotion` | `speed`、`turnSpeed`(度/s)、`duration`(s, 0=無限) | 自機へ旋回しながら進む。初期方向は回転 × 右。OutQuad 再現 |
| `StraightWithHomingMotion` | `speed`、`verticalTrackingSpeed`(単位/s)、`duration`(s, 0=無限) | 常に左へ進みつつ Y を自機に寄せる。OutQuad 再現 |
| `SineMotion` | `baseVelocity`、`amplitude`、`frequency`(Hz)、`duration`(s, 0=無限) | `baseVelocity` で進みつつ、その垂直方向にサイン波で揺れる。差分加算なので他の移動と合成できる。OutQuad 再現 |
| `AnchoredSineMotion` | `anchor`、`amplitude`(Vector3)、`angularSpeed`(rad/s)、`cueSeconds`(s)、`cueOffset`、`duration`(s, 0=無限) | `anchor` の周りで絶対位置として揺れる（開始時に anchor へ吸着）。最初の `cueSeconds` は `cueOffset` 方向へ半周期の合図動作 |
| `QuadraticMotion` | `initialVelocity`、`acceleration`、`duration`(s, 0=無限) | 等加速度運動 |
| `AimRotateMotion` | `duration`(s)、`ease`(DOTween の `Ease`) | 位置は動かさず、自機の方向へ回転する |
| `AnimationMotion` | `clip`、`layerIndex` | Animator の先頭のクリップを `clip` に差し替えて `Base` ステートを再生し、クリップの長さだけ待つ |

### 応用 (`MotionSteps.Advanced.cs`)

| ステップ | フィールド | 動作 |
|---|---|---|
| `AcceleratedMotion` | 通常: `direction`、`useConfiguredDirection`、`initialSpeed`、`acceleration`(単位/s²、負で減速)、`maxSpeed`、`minSpeed`、`duration`(s, 0=無限) | 発射方向（`useConfiguredDirection` が ON なら `direction`）へ加速。速さを min〜max に制限 |
| | 放物運動: `useWorldAcceleration`、`worldAcceleration` | 初速 + 固定方向の加速度で放物運動 |
| | 自機狙い放物線: `aimAtPlayer`、`useBattleWind`、`targetFlightTime`(s)、`vertexDepth`、`vertexScreenMargin`、`rightShotInterval`(発)、`alternateVertical`、`flightTimeVariation`(0〜0.4)、`vertexDepthVariation`(0〜0.8) | 画面内に頂点を作り、`targetFlightTime` 秒で発射時の自機位置へ届く放物線。曲げる軸は `useBattleWind` なら発射時の風の予定上の向き、それ以外は `worldAcceleration` の向き。`alternateVertical` で上下交互、`rightShotInterval` 発ごとに右向きの放物線を試す。揺らぎは全弾共有の発射カウンタで決まる。自機がいなければ動かない |
| `VertexParabolaMotion` | `orientation`(Vertical/Horizontal)、`vertex`、`initialSpeed`、`isRelative`、`oscillateVertex`、`vertexAmplitude`、`vertexAngularSpeed`(rad/s)、`attackTimeOffset`(s) | 発射位置から頂点へ向かい折り返す軸平行の放物線。`oscillateVertex` なら発射時刻 (`AttackTime − attackTimeOffset`) で頂点を揺らす。解けない設定なら弾を即消す。終わらない |
| `ReflectMotion` | `speed`、`maxReflections`、`reflectTop` / `reflectBottom` / `reflectLeft` / `reflectRight` | 発射方向へ進み、一度画面内に入ってから指定した画面端で反射する。終わらない。OutQuad 再現 |
| `ZigzagMotion` | `speed`、`angleDegrees`(度)、`segmentLength`、`initialSign`、`maxReflections`(0=無制限)、`firstSegmentLength`(0=segmentLength)、`duration`(s, 0=無限)、`fallbackAxis`、`rotateToVelocity`、`rotationOffsetDegrees` | 発射方向（無ければ `fallbackAxis`）を軸に ±angle で折り返しながら進む |
| `SpiralMotion` | `startRadius`、`targetRadius`、`duration`(s)、`rotationSpeed`(度/s、負で時計回り) | 開始位置から発射方向の逆へ `startRadius` の点を中心に回りながら半径を変える。ChargeShot が使う |
| `DriftMotion` | `anchor`、`pullStrength`、`moveDistance`、`moveBounds`(幅, 高さ)、`moveDuration`(s)、`pauseDuration`(s)、`moveEase`(DOTween の `Ease`)、`duration`(s, 0=無限) | `anchor` の周りを「移動 → 停止」で漂う。行き先は `UnityEngine.Random` で決める |
| `FadeMoveMotion` | `duration`(s)、`velocity`、`startAlpha`、`endAlpha`、`invincible` | 等速で動きながら SpriteRenderer のアルファを変える。`invincible` は弾のみ有効。OutQuad 再現 |
| `PathMotion` | `waypoints`、`duration`(s)、`shape`(CatmullRom/Linear)、`ease`、`isRelative` | ウェイポイントを通る経路を、経路長に対して `ease` で進む |
| `LoopMotion` | `loop`、`loopCount`(0=無限)、`entries[]`（`preset` / `inlineSteps` / `repeatCount`） | エントリを順に（各 `repeatCount` 回）再生し、`loop` なら繰り返す。`loop` OFF なら 1 周で終わる |

### MotionEase

`MoveToMotion` / `PathMotion` / `MoveTrailFan` の Ease 指定。

| フィールド | 説明 |
|---|---|
| `easeValue` | DOTween の `Ease` の値。`-1` でカスタムカーブ。`Unset` は OutQuad として扱う |
| `customCurve` | カスタムカーブ（インライン） |
| `curvePreset` | `EaseCurveAsset`。指定すると `customCurve` より優先 |

## 9. よく使う組み方

実データ（`DataStore/Stage*/BT/`）で使われている定型。

### フェーズ = Until(条件){本体}

```text
Until [onChildCompleted=Hold]  cond: BgmSamples(422391)
 └─ Sequence
     ├─ SetMovement (continueIfSame)
     └─ Until [Finish]  cond: FrameElapsed(601)
         └─ Sequence
             ├─ Wait 60
             └─ Repeat ∞ / 60f
                 └─ Fire
```

外側の Until がフェーズの終了条件、内側の Until が攻撃の打ち切り。攻撃が先に終わってもフェーズを終えたくない場合は外側を `Hold` にする（Stage1 ボスのイントロ）。

### k フレームおきの発射 = Repeat

```text
Until cond: FrameElapsed(481)
 └─ Repeat ∞ / 150f
     └─ Fire
```

`Repeat(interval=k)` は前の回の終了を待たずに k フレームおきに子を始める。子が SequentialFan のように時間のかかる Sequence でも周期がずれない。1 周期の中で複数の発射タイミングがある場合は `Loop ∞ { Timeline(duration=k) { Fire, Delay(a){Fire}, Delay(b){Fire} } }` を使う。

### BGM 同期のボスフェーズ = BgmSamples

```text
Loop ∞
 └─ Sequence
     ├─ Sequence #MelodyA
     │   ├─ PhaseMarker
     │   └─ (MelodyA 本体)  … Until cond: BgmSamples(1263874)
     ├─ Sequence #MelodyB  … Until cond: BgmSamples(2976121)
     └─ Sequence #Chorus   … ...
```

各フェーズを BGM のサンプル位置で区切る。BgmSamples は「閾値到達」または「ループで先頭へ戻った」で成立し、開始時点で既に閾値を過ぎていればループを待つので、最後のフェーズもループと同時に終わる。閾値は `BgmData` のループ位置と同じサンプル単位。

### 発狂 (Strong) = Selector[Sequence[Check NormalHpDepleted, EnterStrongMode, 発狂本体], 通常本体]

```text
Selector
 ├─ Sequence
 │   ├─ CheckCondition  cond: NormalHpDepleted
 │   ├─ EnterStrongMode
 │   └─ Until [Hold]  cond: BgmSamples(...)
 │       └─ (発狂本体)
 └─ Until [Hold]  cond: BgmSamples(...)
     └─ (通常本体)
```

フェーズ開始時に通常 HP が削り切られていれば発狂側、そうでなければ通常側を実行する。Check が Failure を返すと Selector が次の子（通常本体）へ進む（Stage1 / Stage3 ボス）。

### HP 閾値までランダムにフェーズを選ぶ

```text
Sequence
 ├─ Until #Group0  cond: HpBelow(Self, 40%)
 │   └─ Loop ∞
 │       └─ Random (avoidRepeat)
 │           ├─ Sequence [Until #PhaseA (FrameElapsed), Until #Interlude]
 │           ├─ Sequence [Until #PhaseB (FrameElapsed), Until #Interlude]
 │           └─ ...
 ├─ Until #Interlude (FrameElapsed)  … 定位置へ戻る
 └─ 次のグループ
```

HP が閾値を下回った瞬間に Until が中のフェーズを中断して Success を返し、次へ進む（Stage2 ボス）。HP の判定はこの 1 か所だけに置き、各フェーズの Until 条件に同じ `HpBelow` を重ねない。`Inverter` / `ForceSuccess` で Loop を失敗させて抜ける書き方はしない。

### 敵のツリー = Sequence[SetDeathBehavior?, SetMovement, 攻撃]

```text
Sequence
 ├─ SetDeathBehavior (撃破時: 死亡時攻撃ツリー)   … 必要な敵のみ
 ├─ SetMovement (移動プリセット)
 └─ Until cond: FrameElapsed(N)
     └─ Repeat ∞ / k f
         └─ Fire
```

移動は SetMovement でツリーと独立させ、ツリー本体は攻撃だけを書く。HP で行動を変える敵は `Sequence[Until(HpBelow){通常}, 後半]` のように並べる。

### 道中 = Sequence[Timeline{Delay(t){Spawn}...}, WaitUntil(NoActiveEnemies)]

```text
Sequence
 ├─ Sequence #Phase1
 │   ├─ Timeline (duration=0)
 │   │   ├─ Delay 150 → Spawn
 │   │   ├─ Delay 195 → Spawn
 │   │   └─ ...
 │   └─ WaitUntil  cond: NoActiveEnemies
 └─ Sequence #Phase2 ...
```

出現時刻を Delay で並べ、全員出し終えたら全滅を待って次のフェーズへ進む。Stage3 道中では一部のフェーズを `WithWind` で包み、その間だけ風を吹かせる。

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
| Composite | `Scripts/BehaviorTree/Composite/<型名>.cs` |
| Decorator | `Scripts/BehaviorTree/Decorator/<型名>.cs` |
| 条件 | `Scripts/BehaviorTree/Condition/<型名>.cs` |
| Action | `Scripts/BehaviorTree/Action/<型名>.cs` |
| 部品 (ShotPattern / DirectionSource / BulletConfig / BossDefinition など) | `Scripts/BehaviorTree/Parts/<型名>.cs` |
| MotionStep | `Scripts/Runtime/Motion/<型名>.cs` |

1 ファイルに 1 型。1 つの型からしか使われない enum などの付属型だけ、その型のファイルに同居させる。

## 1. Composite

### Sequence

子を左から順に実行する。

- 子が Success したフレームのうちに次の子を開始する（同じフレームで複数の即時ノードを連続実行できる）。
- 子が Running ならそこで止まり Running。子が Failure なら Failure（以降の子は実行しない）。
- 全ての子が Success なら Success。子が 0 個なら即 Success。
- `Wait N` の次の子は、前の子の N フレーム後に動く。即時に終わる子（NWayShot などの発射 / Spawn など）だけなら `Sequence[Wait a, X, Wait b, Y]` で時刻つきの出来事を表せる。`Delay` は `Sequence[Wait, 子]` と同じ動きをする。

### Selector

子を左から順に試す。

- 子が Failure したフレームのうちに次の子を試す。
- 子が Running ならそこで止まり Running。子が Success なら Success。
- 全て Failure なら Failure。一度 Running になった子が後から Failure しても、次の子へ進む。

### Cycle

子を `interval` フレームの周期で 1 つずつ順に実行し、最後の子の次は 1 番目に戻る（Repeat の子を複数にしたもの）。`count` 回実行したら完了（0 = 無限）。開始のたびに 1 番目の子から始める。`interval` と `indexParam` の扱いは Repeat と同じ（小数可）。

```text
Cycle(72f, ∞)                 … 72f ごとに 2 つのパターンを交互に
├ Cycle(18f, ×3)[A, B]        → A B A
└ Cycle(18f, ×3)[B, A]        → B A B
```

向きを交互に変える連射など、同じ周期で中身だけが入れ替わるものを、1 発ずつ並べずに書ける（Stage1 ボスの Chorus）。

### If

開始時に `condition` を 1 回だけ判定し、成立なら 1 番目の子、不成立なら 2 番目の子を実行して、その結果を返す。2 番目の子が無ければ不成立のとき何もせず Success。実行中に条件が変わっても選び直さない（毎フレーム判定して中断したいなら Until を使う）。

### HpSections

HP の区切りで段階を進める。区切りは HP ゲージの色の区切りにもなる（`BossEntityModel.HpBreaks` 経由。ゲージ側に閾値は書かない）。区切りの値はツリーの上の方でこのノードにだけ書き、下のノードは段階の番号だけを見る。

| フィールド | 説明 |
|---|---|
| `target` | Self / Boss |
| `breaks` | 区切りの HP (%)。大きい順 |
| `switchAt` | `Immediate`: 区切りを下回ったらすぐ次の段階へ / `NextPhase`: 下回った後、次の Phase の開始で次の段階へ |
| `pendingDamageScale` | `NextPhase` のとき、区切りを下回ってから次の段階に入るまでの被ダメージ倍率 |
| `indexParam` | 空でなければ、今の段階（0 始まり）をこのパラメータに書き込む |

- 開始時は今の HP から段階を決める（区切りを既に下回っていればその段階から）。
- 子が複数なら段階ごとに子を切り替える（区切りを越えたら今の子を中断して次の子へ）。途中の子が先に終わったら区切りまで待ち、最後の子が終われば Success。
- 子が 1 つなら段階が進んでも同じ子を続ける。段階は `indexParam` と Compare 条件で子に伝える。

```text
HpSections  70% / 30%                 … Stage4 ボス
├ 第1形態
├ 第2形態
└ 第3形態

HpSections  25%  次の Phase から (待機中の被ダメ ×0.25) → $stage   … Stage1 ボス
└ Sequence[ Phase «Intro», Loop∞{ Phase «MelodyA», … } ]
     Phase の中: If($stage ≧ 1)[ 発狂攻撃, 通常攻撃 ]
```

### Parallel

全ての子を毎フレーム、左から順に Tick する。各子の開始時刻は子側に Delay を挟んで指定する。時間のかかる子（Repeat / Move / SubTree など）を重ねて動かすときに使い、即時に終わる子だけなら Sequence + Wait で書く。

| フィールド | 単位 | 説明 |
|---|---|---|
| `policy` | | `All`: 全ての子が Success したら Success / `Any`: どれか 1 つが Success したら、残りを Abort して Success / `Duration`: `duration` フレーム経過で実行中の子を Abort して Success |
| `duration` ($ 可) | f | `policy = Duration` のときだけ表示・使用。0 以下なら `All` と同じ |

- どのポリシーでも、子が 1 つでも Failure したら残りを Abort して Failure。
- 完了した子は以後 Tick しない。
- `Duration` の場合、子は f 〜 f+duration−1 の duration フレームだけ Tick され、f+duration のフレームで Success を返す。子が全て早く終わっても duration までは Running のまま。
- `Duration` を Loop の子に置くと、Loop の同フレーム再開規則（後述）により **ちょうど duration フレーム周期** で繰り返す。`Loop{ Parallel(Duration 1 周){ Move 1 周分, 攻撃 } }` と書くと、移動と攻撃が毎周同じフレームから始まり、ずれない。

### RandomSelect

開始時（Enter）に子を 1 つ抽選し、その子の結果をそのまま返す。

| フィールド | 説明 |
|---|---|
| `weights` | 子の並び順に対応する重み。足りない分は 1、負の値は 0 として扱う |
| `avoidRepeat` | ON（既定）なら直前に選んだ子を連続で選ばない（子が 2 つ以上のとき） |

- 「直前」は同じ実行時ノードで前回選んだ子。Loop の中に置くと、周回をまたいで連続を避ける。
- 子が 0 個なら即 Success。

## 2. Decorator

### Loop

子を指定回数繰り返す。

| フィールド | 単位 | 説明 |
|---|---|---|
| `count` ($ 可) | 回 | 0 = 無限 |
| `indexParam` | | 空でなければ、今が何回目か（0 始まり）をこのパラメータに書き込む |

- 子が Failure したら Failure。`count` 回 Success したら Success。
- **同フレーム再開規則**: 前フレーム以前に始まった周が完了した場合は、同じフレームのうちに次の周を開始する（周期がずれないように）。同じフレーム内で始まって完了した周（中身が全て即時ノードの場合など）の次は、翌フレームに回す（無限ループ防止）。

### Repeat

子を `interval` フレームの周期で直列に繰り返す。回が重なることはない。

| フィールド | 単位 | 説明 |
|---|---|---|
| `count` ($ 可) | 回 | 回数。0 = 無限 |
| `interval` ($ 可) | f | 周期（ある回の開始から次の回の開始まで）。既定 60、最小 1。小数も可で、k 回目から k+1 回目までを `round((k+1)×interval) − round(k×interval)` フレームにする（22.5 なら 22, 23, 23, 22, …） |
| `indexParam` | | 空でなければ、今が何回目か（0 始まり）をこのパラメータに書き込む。計算は Calc で行う |

- 1 回目は開始フレーム f、子が周期内に終われば k 回目は f + (k−1)×interval に開始する。
- 子が周期より長くかかったときは、その回が終わったフレームに次の回を始める（以降の開始はそこから数え直す）。
- 最後の回が終わったフレームで Success。最後の回の後は待たない。`count = 0` なら終わらない（親の Until などで打ち切る）。
- 子が Failure したら Failure。
- `Loop{ Sequence[子, Wait] }` との違いは 2 点: 周期が子の長さに左右されないことと、最後の回の後に待ちが入らないこと。

回数から角度を作ってなぎ払う例（Stage4 ボスの BossBeamSub）:

```text
Repeat(×15, 12f, → $i)
└ Sequence
   ├ Calc  $angle = $i × 0.8
   ├ Calc  $angle = $angle + 180
   └ NWayShot(向き: 固定 $angle)      … 180°, 180.8°, …, 191.2°
```

### Until

条件が成立した瞬間に子を中断して完了する。フェーズの終了条件や割り込みはこれで表す。

| フィールド | 説明 |
|---|---|
| `condition` | 条件（`Condition`）。未設定なら成立しない |
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

### Phase

フェーズ 1 つ分。Until と同じ動き（`condition` / `onChildCompleted`）に加えて次を行う。

| フィールド | 説明 |
|---|---|
| `phaseId` | フェーズ名。開始時に `BattleServices.OnPhaseStarted` へ流す（`HpSections` の `NextPhase` が使う） |

- 実行中は終了条件の進み具合（`Condition.Progress`）を `BattleServices.PhaseProgress` へ流す。`BossPhaseProgressGauge` はこれを表示するだけで、閾値を持たない。
- 進み具合が決まるのは `BgmSamples`（開始時の BGM 位置 → 閾値）と `FrameElapsed`。それ以外の条件ではゲージを隠す。

### WithPrefab

子の実行中だけ Prefab を出しておく（障害物・演出など）。

| フィールド | 説明 |
|---|---|
| `prefab` | 出す Prefab |
| `position` | ワールド座標。回転は Prefab 本来の回転 |

開始時に Instantiate、終了・中断時に Destroy する。子の結果をそのまま返す。

## 3. 条件 (Condition)

Until / CheckCondition / WaitUntil の `condition` に設定する。

| 条件 | フィールド | 成立する条件 |
|---|---|---|
| `Constant` | `value` | 常に `value` |
| `FrameElapsed` | `frames` ($ 可, f) | Reset（Until などの開始）からの経過フレーム ≧ frames |
| `BgmReached` | `seconds` ($ 可, s) | `AudioSource.time` ≧ seconds |
| `BgmSamples` | `thresholdSamples`（サンプル、BtInt。パラメータで差し替え可） | BGM の `timeSamples` が閾値に達した、またはループで先頭へ戻った。開始時点で既に閾値を過ぎていれば、ループで戻るのを待つ。BGM が無ければ常に成立 |
| `NoActiveEnemies` | なし | `EnemyTracker` の生存敵数が 0（Spawn の `tracked` が ON の敵だけが数えられる）。Spawner が無ければ成立 |
| `HpBelow` | `target`（Self / Boss）、`percent`（0〜100） | 現在 HP ≦ ceil(最大 HP × percent / 100)。対象の Model が無ければ成立 |
| `Compare` | `a` ($ 可)、`op`（≧ > ≦ < = ≠）、`b` ($ 可) | `a op b`。パラメータは判定のたびに読み直す（HpSections の段階などを見る） |
| `Signaled` | `key` | このフレームに自分（`Self`）へ合図 `key` が届いた（SendSignal、6 章）。届いたフレームに判定されなかった合図は受け取れない。`Self` が無ければ不成立 |
| `And` | `conditions` | 全て成立（空なら成立） |
| `Or` | `conditions` | どれか成立（空なら不成立） |
| `Not` | `condition` | 中の条件が不成立（未設定なら成立） |

- `HpTarget.Self` はツリーを持つ敵自身、`Boss` は `Services.Boss`（BossBattle ノードでツリーを付けたボスの `EnemyAgent`）。ステージツリーからボスの HP を見るときは `Boss` を使う。
- 実データの FrameElapsed は 601 や 1801 のように +1 された値が多い。ループ終端ちょうどの発射を含めるため。

## 4. 基本 Action

### Wait

| フィールド | 単位 | 説明 |
|---|---|---|
| `frames` ($ 可) | f | 待機フレーム数。0 なら即 Success |

開始フレーム f から数えて f+frames のフレームで Success。

### Calc

`resultParam = a 演算 b` を 1 回計算してパラメータに書き込み、即 Success。演算は 1 ノードに 1 つで、式が長いときは Calc を並べる。

| フィールド | 説明 |
|---|---|
| `resultParam` | 書き込み先のパラメータ名 |
| `a` ($ 可) / `b` ($ 可) | 値。実行のたびにパラメータを読み直す |
| `op` | Add (+) / Subtract (−) / Multiply (×) / Divide (÷、b が 0 なら 0) |

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
| `tree` | 実行するツリー。未設定なら警告して即 Success。`CommonTreeAsset` か、開いているツリーと同じ種類のツリーだけを指定できる（Common のツリーからは Common だけ） |
| `treeParam` | ツリーの Object パラメータ名。指定するとその値のツリーを `tree` の代わりに実行する。値が空なら何もせず即 Success（共通の雑魚ツリー `Enemy` の `attack` で使う） |
| `overrides` | 子ツリーのパラメータの上書き（`TreeParameter`）。`bindToParent` に親ツリーのパラメータ名を入れると、その値を引き継ぐ |

Build 時に展開されるため、実行時の戻り値は子ツリーのルートと同じ。循環参照は Build 時に例外になる。

## 5. バトル Action: 発射

### 弾を撃つノード: SingleShot / NWayShot / SequentialFanShot / SequentialCircleShot（Action/Attack）

撃ち方ごとに別のノードになっている（グラフ上でノードの種類を見れば何を撃つか分かる）。共通部分は `ShotNode`（`Action/ShotNode.cs`）。即 Success。

| ノード | 撃ち方 (`pattern`) |
|---|---|
| `SingleShot` | 基準方向に 1 発 |
| `NWayShot` | 扇状に同時に撃つ |
| `SequentialFanShot` | 扇状に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す |
| `SequentialCircleShot` | 円周上に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す |

同じ扇を間隔をあけて何回か撃つ（連射）は、専用ノードを作らず `Repeat(回数, 間隔){ NWayShot }` で組む。Stage2 では `Stage2/BT/Attack/S2_NWayRepeat`（パラメータ `way` / `spread` / `count` / `interval`）を SubTree で呼ぶ。

`pattern` の各フィールドは後述の「発射の部品: ShotPattern」を参照。どのノードも次の共通フィールドを持つ。

| フィールド | 説明 |
|---|---|
| `bullet` (`BulletConfig`) | 弾の設定（後述） |
| `direction` (`DirectionSource`) | 基準方向。既定は Fixed(180°) |
| `rotation` (`RotationSource`) | 弾の回転（Prefab 本来の回転に対するオフセット）。既定は Inherit |
| `muzzleOffset` | 撃つ敵の位置からのずれ（ワールド単位）。ステージ直下では原点からのずれ |
| `se` | 発射時に 1 回鳴らす SE。`None` なら鳴らさない |

- 方向は実行したフレームで決める。パターンの `SpawnDelayFrames` が付いた弾は予約され、出現時点の撃ち手の位置から出る。
- `DirectionSource` が発射位置を返す場合（ScreenEdge）、その位置を使う。

### WarningLine（Decorator）

予測線を出して `frames` 待ってから、子のビームを実行する。子は `IBeam` を実装するビーム（`BulletBeam` / `SpriteBeam`）。予測線とビームの位置・向きは、WarningLine の開始時に子のビームから 1 回だけ決めて（`CreateAim`）、そのまま子に渡す（`IBeam.UseAim`）。自機狙いのビームでも予測線と撃つ向きがずれない。

| フィールド | 単位 | 説明 |
|---|---|---|
| `prefab` | | 予測線の Prefab。未指定なら予測線を出さずに待つだけ |
| `frames` | f | 子のビームを撃つまでの待ち（既定 60）。Wait と同じく開始から `frames` フレーム目に子を実行する |
| `displayFrames` | f | 予測線の表示時間。0 なら `frames` と同じ |
| `width` | ワールド単位 | 予測線の太さ。0 以下は Prefab の値 |
| `waitForBeam` | | ON（既定）: ビームを撃つまで Running（後ろのノードを待たせる。途中で打ち切られたらビームも撃たない）/ OFF: 予測線を出したらすぐ Success。ビームは `frames` 後に予約で撃つ（撃ち手が倒れたら取り消し、ツリーが打ち切られても撃つ） |

溜めと並べる例（Stage2 ボスの Beam フェーズ）:

```text
Parallel(All)
 ├─ SubTree S2_Charge            … 溜め
 └─ Delay +6f
     └─ WarningLine 78f          … 予測線
         └─ BulletBeam 9way 40°  … ビーム
```

### BulletBeam（Action/Attack）

同じ位置・向きに弾を一定間隔で連射してビームにする。即 Success（連射は予約で行う）。予測線は `WarningLine` で包んで出す。

| フィールド | 単位 | 説明 |
|---|---|---|
| `origin` | | `Self`: 撃つ敵の位置 + `position`（連射ごとにその時点の位置）/ `World`: `position` をワールド座標として固定 |
| `position` | ワールド単位 | 上記の基準位置またはずれ |
| `direction` | | 基準方向 (`DirectionSource`) |
| `pattern` | | ビームの本数と角度 (`ShotPattern`)。パターンの各方向に 1 本ずつ出す |
| `rotation` | | 弾の回転。既定は Fixed(0°) |
| `range` | ワールド単位 | 弾の飛距離と予測線の長さ。0 なら弾/予測線 Prefab の値 |
| `bullets` | | 1 回の連射で撃つ弾 (`List<BulletConfig>`)。複数なら同じ位置・向きに重ねて撃つ |
| `shotCount` | 回 | 連射回数（最小 1、既定 36） |
| `shotIntervalFrames` | f | 連射の間隔（最小 1、既定 5） |
| `se` | | 連射 1 回ごとに鳴らす SE |

- 向きと本数は 1 回だけ決める（WarningLine の子なら WarningLine の開始時、そうでなければ実行時）。
- i 回目の連射は実行から `i × shotIntervalFrames` フレーム後。撃ち手が倒れると以降の連射は取り消される。

### SpriteBeam（Action/Attack）

1 枚絵のビーム本体を出す。即 Success。予測線は `WarningLine` で包んで出す。

| フィールド | 単位 | 説明 |
|---|---|---|
| `line` (`start` / `end`) | ワールド単位 | ビームの線分。向き・長さはこの線分から決まる |
| `isRelative` | | ON なら線分を撃つ敵の位置からの相対にする（狙いを決めた時点の位置で固定） |
| `bodyPrefab` | | ビーム本体の Prefab (`IBeamVisualReceiver`) |
| `bodyDuration` | s | 本体の表示時間 |

### MoveTrailFan（Action/Attack）

この敵が移動しながら軌跡上に弾を置き、移動後に全弾を扇状に同時に放つ。移動と配置が終わるまで Running。`Self` が無ければ Failure。

| フィールド | 単位 | 説明 |
|---|---|---|
| `targetOffset` / `isRelative` | ワールド単位 | 移動先。ON なら現在位置からの相対 |
| `moveDuration` | s | 移動時間 |
| `moveEase` | | 移動の Ease (`MotionEase`) |
| `emitBullets` | | OFF なら移動だけ |
| `bullet` / `direction` / `rotation` / `se` | | 弾を撃つノードと同じ。方向は開始時に決める |
| `wayCount` | 発 | 弾数。移動の進捗が i/(n−1) に達した位置に i 発目を置く |
| `spreadAngle` | 度 | 扇の隣り合う弾の角度 |
| `releaseDelayFrames` | f | 移動終了から全弾が動き出すまで |

### PlaceAlongPath（Action/Attack）

経路アセット（`TrailPath`）に沿って弾を等間隔に置き、経路の移動時間 + `fireDelayFrames` 後に一斉に放つ。敵の実際の位置ではなく経路から計算した位置に置くので、同じ経路の `MovePath` と `Parallel` で並べると移動と弾がずれない。経路の移動時間が経つまで Running。

| フィールド | 単位 | 説明 |
|---|---|---|
| `path` | | `TrailPath`（パラメータ可） |
| `bullet` / `rotation` / `se` | | 弾を撃つノードと同じ |
| `spacing` | ワールド単位 | 弾を置く間隔（パラメータ可） |
| `fireDelayFrames` | f | 経路の移動を終えてから放つまで（パラメータ可） |

弾の向きと扇は区間ごとに `TrailPath` 側で指定する。扇の角度は区間内の弾の並び順で決まる。

### TrailPath（ScriptableObject、Create → Battle/Trail Path）

| フィールド | 説明 |
|---|---|
| `relative` | ON なら各点を直前の点からの移動量として扱う。OFF ならワールド座標 |
| `frames` | 経路全体を等速で移動するフレーム数 |
| `segments[].position` | 区間の終点 |
| `segments[].direction` / `spread` | この区間に置いた弾を撃つ向きと、隣り合う弾の角度（度） |
| `segments[].extraFireDelayFrames` | 全体の発射時刻からさらに遅らせるフレーム数（区間ごとに時間差で撃つとき） |

### 発射の部品: BulletConfig

| フィールド | 説明 |
|---|---|
| `prefab` | `BulletEntityPresenter` を持つ弾 Prefab。未設定なら何も出さない |
| `prefabParam` | ツリーの Object パラメータ名。指定するとその値（弾 Prefab）を `prefab` の代わりに使う。SubTree で弾だけ差し替えるときに使う（弾を撃つノード / `PlaceAlongPath` が対応） |
| `damage` | ダメージ（既定 10） |
| `motionPreset` | 指定するとインラインの `motion` より優先 |
| `motion` | 弾の移動 (`List<MotionStep>`)。空なら発射方向へ等速 (`VelocityMotion`) |
| `range` | 飛距離の上限（発射方向への進捗、ワールド単位）。0 で無制限 |
| `lifetime` | 消えるまでのフレーム数（`StartDelayFrames` の間も数える）。0 で無制限。弾 Prefab は寿命を持たない |
| `affectedByForce` | 発射時にステージの外力 (`IBulletForce`。Stage3 の風など) が有効なら外力を受ける（既定 ON） |
| `behavior` | 弾を動かすツリー（`BulletTreeAsset`）。未指定なら MoveSelf だけの組み込みツリーで、`motionPreset` / `motion` の移動を実行する。指定したツリーで移動させるには MoveSelf を置く |
| `overrides` | `behavior` のパラメータ上書き（固定値のみ） |
| `group` | 合図の宛先 `Group` で使う名前。空ならグループに入らない |

撃った弾の `AttackTime` は撃った時点の撃ち手の `AttackTime`（弾のツリーから撃った子弾は親弾の値を引き継ぐ）、`Owner` は撃ち手になる。

### ChargeEffect（Action/Effect）

周囲に弾を並べ、渦を巻きながら中心へ集める溜めの演出。撃つ攻撃ではないので弾を撃つノードとは別の型で、向き・回転・弾の動き・ダメージの欄は持たない（ダメージは常に 0。弾は当たり判定のない Prefab を使う）。即 Success（弾を出すだけで、溜めの長さは呼ぶ側の `Wait` で待つ）。寄せ終わった弾はその位置に止まる。

| フィールド | 説明 |
|---|---|
| `prefab` / `prefabParam` | 弾の Prefab。`prefabParam` にツリーの Object パラメータ名を書くとその値を使う（`BulletConfig` と同じ） |
| `count` | 並べる弾の数 |
| `radius` | 出し始めの半径 |
| `targetRadius` | 寄せた先の半径（0 で中心） |
| `gatherFrames` (f) | 寄せるのにかけるフレーム数 |
| `rotationSpeed` (度/s) | 回る速さ。負で時計回り |
| `lifetime` (f) | 溜めの弾が消えるまでのフレーム数。0 で無制限（パラメータ不可） |
| `se` | 出したときの SE |

`count` / `radius` / `targetRadius` / `gatherFrames` / `rotationSpeed` はツリーのパラメータにできる。例: `Stage2/BT/Attack/S2_Charge`（パラメータ prefab / count / frames / radius）。

### 発射の部品: ShotPattern

基準方向から弾ごとの方向・ずれ・遅延を作る。弾を撃つノードがそれぞれ 1 つずつ持つ（`NWayShot` ノードなら `NWayPattern`）。`BulletBeam` はビームの本数と角度に使う。

| パターン | フィールド | 動作 |
|---|---|---|
| `SinglePattern` | なし | 基準方向に 1 発 |
| `NWayPattern` | `wayCount`(BtInt)、`spreadAngle`(BtFloat、度、隣り合う弾の角度。どちらもパラメータで差し替えられる)、`radius`(発射位置を各方向へずらす距離)、`startDelayFrames`(f、動き出すまで)、`rotationOffset`(度、反時計回り) | 同時に扇状に撃つ。奇数は中央に 1 発、偶数は中央を空ける |
| `SequentialFanPattern` | `wayCount`、`spreadAngle`(度)、`spawnIntervalFrames`(f)、`releaseDelayFrames`(f) | 扇状に 1 発ずつ `spawnIntervalFrames` おきに出し、最後の 1 発から `releaseDelayFrames` 後に全弾同時に動き出す |
| `SequentialCirclePattern` | `wayCount`、`radius`、`spawnIntervalFrames`(f)、`releaseDelayFrames`(f) | 円周上（360°/wayCount 間隔、半径 radius）に 1 発ずつ出し、全弾同時に動き出す |

### 発射の部品: DirectionSource

| 種類 | フィールド | 方向 |
|---|---|---|
| `FixedDirection` | `angle` ($ 可、度) | 固定方向。0 = 右、90 = 上、180 = 左（既定）。撃つたびにパラメータを読み直すので、Calc の結果でなぎ払える |
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

## 6. バトル Action: 敵・弾 (Self)

`Self` を対象にするノードは、ステージ直下（`Self == null`）では何もしない（Move のみ Failure）。`Self` は敵（`EnemyAgent`）か弾（`BulletAgent`）で、`EnemyActionNode` を継承するノード（SetInvincible / BossWindCue）は敵のツリー（`EnemyTreeAsset`）にだけ置ける。

### MoveSelf（Action/Move）

出現させた側から渡された移動（`Self.Motion`）を、`Self.Direction` / `Self.AttackTime` で実行する。フィールドは無い。移動が終わると Success（無限の移動は終わらない）。移動が空なら即 Success。

- 敵の移動は Spawn の `motionPreset` / `motion`、弾の移動は `BulletConfig` の `motionPreset` / `motion` から渡る。共通の雑魚ツリー `Enemy` と、ツリー未指定の弾の組み込みツリーはこのノードで動く。
- 移動が開始できない設定（`Rejected`。頂点が発射位置と同じ放物線など）なら `Self.Despawn()` で消して Failure。
- 中断・完了時に移動を止める。

### Move（Action/Move）

この敵（または弾）を `MotionStep` の列で動かす。移動が終わると Success（無限の移動は終わらない）。`Self` が無ければ Failure。

| フィールド | 説明 |
|---|---|
| `preset` | `MotionPreset`。指定するとインラインより優先 |
| `presetParam` | ツリーの Object パラメータ名。指定するとその値の `MotionPreset` を使う。値が空なら動かずに Success |
| `steps` | インラインの `List<MotionStep>` |

敵の移動は必ずこのノードか MoveSelf で行う（ツリーの外で流れる移動はない）。中断・完了時に移動を止める。
- 攻撃と同時に動かすときは `Parallel{ Move, 攻撃 }` と並べる（Move を先頭に置くと、そのフレームの発射は移動後の位置になる）。
- 移動の後に一定時間待つときは `Parallel{ Move, Wait N }` にする。`Sequence[Move, Wait]` だと移動の長さぶん後ろがずれる。
- 複数のフェーズにまたがって続ける移動は、そのフェーズをまとめた外側に `Parallel{ Move, Sequence[フェーズ…] }` と置く（例: Stage3 ボスの横揺れ）。

### MovePath（Action/Move）

経路アセット（`TrailPath`）に沿ってこの敵を等速（Linear）で動かす。`TrailPath.frames` 経つと Success。`Self` が無ければ Failure。`PlaceAlongPath` と同じ経路を渡して `Parallel` で並べると「走りながら弾を置く」攻撃になる（例: `Stage2/BT/Attack/S2_TrailFire`）。

| フィールド | 説明 |
|---|---|
| `path` | `TrailPath`（パラメータ可） |

### SetDespawnBehavior（Action）

`Self`（敵・弾）が消えるときに 1 回だけ実行するツリーを設定する。どの種類のツリーにも置ける。即 Success。

| フィールド | 説明 |
|---|---|
| `tree` | 消えるときに実行するツリー |
| `treeParam` | ツリーの Object パラメータ名。指定するとその値のツリーを使い、値が空なら消えるときの行動なし |
| `reasons` | このどれかの理由（`DespawnReason`、複数選択）で消えたときだけ実行する。既定は `Defeated` |

| 理由 | 敵 | 弾 |
|---|---|---|
| `Defeated` | HP が 0 になった | 何かに当たって消えた |
| `Expired` | 寿命が尽きた | 寿命・飛距離が尽きた |
| `OutOfScreen` | 一度画面に入ってから外へ出た | 画面外へ出た |
| `Despawned` | Despawn ノード | Despawn ノード |
| `Cleared` | — | 一括消去・リトライ・バトル終了 |

- `Cleared` は普通は含めない（一括消去で子弾が出ないように）。
- 消えるときのツリーは 1 フレームだけ Tick して返すため、即時ノード（NWayShot などの発射、Spawn）だけが意味を持つ。`Self` は消えていく敵・弾で、そこから撃つ。
- 敵の撃破時の行動は、撃破の次の Step で実行される（[BehaviorTree.md](./BehaviorTree.md) 4.6）。

### SetInvincible（Action/Enemy）

`invincible`（既定 ON）でこの敵の無敵を切り替える。即 Success。`EnemyActionNode` なので敵のツリーにだけ置ける。

### Despawn（Action）

`Self`（敵・弾）を消す（撃破扱いにしない。理由は `Despawned`）。`Self.Despawn()` を呼んで印を付けるだけで、実際に消えるのは敵ならそのフレームの `CompleteEnemyDespawns`、弾なら `BulletSystem` の処理の中。消えた敵・弾が予約していた発射も取り消す。敵はプールから出したものならプールへ戻す。即 Success。

### SendSignal（Action）

合図を送る。次のフレームの頭に宛先の敵・弾へ届き、`Signaled` 条件で受け取れる。会話のコマンド（OnCommand）とは別の仕組み。即 Success。

| フィールド | 説明 |
|---|---|
| `key` | 合図の名前 |
| `target` | `Spawned`（既定。自分が出した敵・弾と、それらがさらに出したもの）/ `Group`（同じグループ名の敵・弾）/ `All`（自分以外のすべての敵・弾） |
| `group` | `target = Group` のときの宛先グループ名（`Spawn.group` / `BulletConfig.group` で付けた名前） |

- ステージツリー（`Self == null`）から `Spawned` で送ると、すべての敵・弾に届く。
- 受け取る側は `Until(Signaled)` / `If(Signaled)` などで判定する。届いたフレームに判定しないと取りこぼす。

### PlaySe（Action）

`se` を鳴らす。即 Success。

### Spawn（Action/Spawn）

敵・障害物・ビームなどの Prefab を出現させる。即 Success。

| フィールド | 単位 | 説明 |
|---|---|---|
| `prefab` | | 出す Prefab |
| `behavior` | | この Prefab を動かすツリー（`EnemyTreeAsset`。`EnemyAgent` を付ける）。雑魚は通常 `Commons/BT/Enemy`。未指定ならツリーを付けず、その場から動かない（`EnemyEntityPresenter` を持つ Prefab には、消えるかの判定のため `EnemyAgent` だけ付ける） |
| `motionPreset` | | 出した敵に渡す移動（`EnemyAgent.Motion`）。ツリーの MoveSelf が実行する。指定するとインラインの `motion` より優先 |
| `motion` | | インラインの移動 (`List<MotionStep>`) |
| `overrides` | | `behavior` のパラメータ上書き（固定値のみ。`bindToParent` は効かない） |
| `group` | | 合図の宛先 `Group` で使う名前。空ならグループに入らない |
| `position` | ワールド単位 | 撃つ敵がいなければワールド座標、いればその位置からの相対 |
| `worldSpace` | | ON なら撃つ敵がいてもワールド座標で出す |
| `layout` | | 並べ方 (`SpawnLayout`、下表)。既定は Single |
| `rotation` | | 出す Prefab の基準回転 (`RotationSource`)。未設定なら回転なし |
| `direction` | | 方向 (`DirectionSource`)。現状のレイアウトはいずれもこの値を使わない |
| `tracked` | | ON（既定）なら `EnemyTracker` に登録し、NoActiveEnemies の判定対象にする |
| `beamRange` | ワールド単位 | ビーム/警告線の長さ。0 以下は Prefab の値 |
| `beamDuration` | s | ビーム/警告線の表示時間。0 以下は Prefab の値 |
| `beamWidth` | ワールド単位 | ビーム/警告線の幅。0 以下は Prefab の値 |

`beam*` は Prefab が `IBeamVisualReceiver` を持つ場合だけ使われる。出現した敵のツリーは翌フレームから動く。プール対象の敵 Prefab は `EnemyPool` から出す（[BehaviorTree.md](./BehaviorTree.md) 4.5）。

### SpawnLayout

| 種類 | フィールド | 並べ方 |
|---|---|---|
| `SingleLayout` | `offset` | 1 体。基準位置 + offset |
| `LineLayout` | `count`、`spacing`、`lineAxis`(既定 上)、`centerOffset` | 中心から `lineAxis` 方向に等間隔で一列 |
| `FanLayout` | `wayCount`、`spreadAngle`(度)、`offset` | `offset` を扇の角度だけ回した位置に並べ、各体もその角度だけ回す。角度の決め方は NWay と同じ |

各体の回転は「レイアウトの回転 × `rotation` の回転」。

## 7. バトル Action: ステージ・ボス

### ClearBullets（Action/Stage）

画面の弾を `BulletSystem.Clear()`（理由 `Cleared`）で消し、`BulletClearReceiver.ClearAllBeams` で警告線・ビームを消す。`clearEnemies` が ON なら敵も消す（`ClearAllEnemies`。消えるときのツリーは実行しない）。即 Success。

### BossWindCue（Action/Boss）

合図の演出のあと風向きを切り替える。即 Success（切り替え自体は風の予定表で遅らせる）。`EnemyActionNode` なので敵のツリーにだけ置ける。

| フィールド (`cue`: `BossWindCueSettings`) | 単位 | 説明 |
|---|---|---|
| `enabled` | | 未使用（ノードでは常に実行される） |
| `direction` | | 切り替え後の風向き |
| `cueSeconds` | s | この秒数の間は直前の風を保ち、その後 `direction` に切り替える |
| `playerSpeedRatio` / `bulletAcceleration` / `bulletDrag` | | 切り替え後の風の強さ |
| `cueScale` | | 合図中のスケール変化率（`IWindCueReceiver` 側が使う） |

実行時に `WindService.Apply` で「直前の向き × cueSeconds → direction × 3600 秒」の予定表を入れ、`Self` が `IWindCueReceiver` を持っていれば `PlayCue(cue)` を呼ぶ。`WindService` が無ければ警告して何もしない。

## 7.5 ステージ進行

ステージツリー（`BattleStageAsset.tree`）で道中・会話・ボス戦を並べるノード。ソースは `Composite/` (Stage / PlayScenario)、`Decorator/` (Section / OnCommand)、`Action/` (Wave / PlayBgm / SpawnBoss / BossBattle)。ツリー全体の形は [BehaviorTree.md](./BehaviorTree.md) 6.1 を参照。

### Stage（Flow）

ステージツリーのルートに置く Composite。子の Section を順に実行し、全て Success なら Success。

- 開始時は `Services.Progress.StartSection`（リトライ・デバッグ起動の開始位置）番目の Section から始める。
- Section に入るたびに `Services.Progress.Enter` で通知し、Presenter が `RuntimeModel.CheckpointSection` に記録する。
- 子が Section でないと Build 時に警告する（Way 扱いで実行はする）。

### Section（Flow）

ステージの区切り（Decorator）。リトライするとこの Section の頭からやり直す。子の結果をそのまま返す。

| フィールド | 説明 |
|---|---|
| `label` | 表示名（BT エディタ・Battle Debug Launcher の一覧に出る） |
| `situation` | `Way` / `Boss`。背景スクロールなどシーン側の扱いに使う |

### Wave（Flow）

出現一覧を時刻どおりに出し、全部出したあと終了条件が成立したら Success。

| フィールド | 単位 | 説明 |
|---|---|---|
| `entries[].frame` | f | Wave の開始から数えたフレーム。`Sequence[Wait(N), Spawn]` の Spawn と同じ数え方（0 なら開始フレーム） |
| `entries[].spawn` | | 出す内容（Spawn ノードと同じフィールド） |
| `until` | | 全部出したあとに待つ条件（例: `NoActiveEnemies`）。未設定なら最後の出現のフレームで Success |

entries は `frame` 順に並べ替えて実行する。

### PlayBgm（Action/Stage）

`bgm`（`BgmType`、既定 BattleWay）を `Services.PlayBgm` で流す。即 Success。曲の選択（`SceneType = stageNumber + 2`）と曲名の表示は Presenter が行う。

### SpawnBoss（Action/Boss）

`boss`（`BossDefinition`）のボスを生成し、登場の移動を流す。即 Success。ツリーはまだ付けない（BossBattle で付ける）。BossHost や BossDefinition が無い、Prefab に `BossEntityPresenter` が無い、既にボスが出ている場合は Failure。

### BossBattle（Action/Boss）

出ているボスにツリーを付けて戦闘を始め、撃破演出が終わるまで Running、終わったら Success。

| フィールド | 説明 |
|---|---|
| `tree` | ボスに付けるツリー。未指定なら `BossDefinition.tree` |

- 開始時に登場の移動が残っていれば終点まで一気に進める。
- ボスが出ていない、またはツリーが無ければ警告して Failure。
- ボスツリーの結果は見ない。ボスツリーが先に完了しても撃破まで待つ。

### PlayScenario（Flow）

会話を流し、終わるまで待つ Composite。子には OnCommand だけを置く。

| フィールド | 説明 |
|---|---|
| `scenario` | 流す会話 (`ScenarioData`) |
| `skipOnResume` | ON（既定）なら、リトライやデバッグ起動でこの Section を途中から始めたときは会話を見せず、コマンドだけを順に即実行する |
| `restorePlayerControl` | ON（既定）なら会話の後に自機の攻撃入力を戻す。ステージ最後の会話では OFF |

- 会話中に届いたコマンド（演出関数以外の行）は、同名の `command` を持つ OnCommand の子を実行する。子は会話と並行して動き、同じ子がまだ実行中なら後のコマンドは警告して無視する。
- 会話が終わり、かつ実行中の子が全て終わったら Success。
- 対応する OnCommand が無いコマンドは警告して無視する。
- 要約欄に、OnCommand が無いコマンド（`⚠ OnCommand が無い`）と会話に出てこない OnCommand（`⚠ 会話に無いコマンド`）を表示する。
- BT エディタで PlayScenario ノードを右クリック →「会話のコマンドから OnCommand を作る」で、足りない OnCommand をまとめて作れる。

### OnCommand（Flow）

PlayScenario の子に置く Decorator。会話が `command`（シナリオの `function` 名。例: `SpawnBoss`）の行に来たら子を実行する。子の結果をそのまま返す。

### BossDefinition（ScriptableObject、Create → Battle/Boss Definition）

| フィールド | 説明 |
|---|---|
| `prefab` | `BossEntityPresenter` を持つ Prefab |
| `spawnPosition` | 生成位置 |
| `entrance` | 登場の移動 (`List<MotionStep>`、秒) |
| `tree` | ボス自身が実行するツリー（`EnemyTreeAsset`） |

## 8. MotionStep

敵・ボス・弾の移動の 1 ステップ。`Move` / `Spawn.motion` / `BulletConfig.motion` / `BossDefinition.entrance` / `MotionPreset` に並べる。
**時間はすべて秒**。`duration` が「0 で無限」のステップは終わらないため、後ろのステップに進まない。

「発射方向」は弾なら発射時の方向、敵/ボスでは無し（ゼロ）。「OutQuad 再現」が付いたステップは、時間の進みに OutQuad 癖が掛かる（[BehaviorTree.md](./BehaviorTree.md) 8.3 参照）。

### 基本

| ステップ | フィールド | 動作 |
|---|---|---|
| `WaitMotion` | `duration`(s)、`invincible` | その場で待つ。`invincible` は弾のみ有効（待機中無敵） |
| `VelocityMotion` | `velocity`(単位/s)、`duration`(s, 0=無限) | 等速移動。発射方向があればその向きに \|velocity\| の速さ。OutQuad 再現 |
| `MoveToMotion` | `targetOffset`、`isRelative`、`duration`(s)、`ease`(`MotionEase`) | 指定位置へ補間移動。`isRelative` が ON なら開始位置からの相対 |
| `MoveToPlayerMotion` | `duration`(s)、`x`、`y`、`ease` | 開始時の自機位置へ補間移動。`x` / `y` で追う軸を選ぶ（両方 OFF なら待つだけ） |
| `StraightWithHomingMotion` | `speed`、`verticalTrackingSpeed`(単位/s)、`duration`(s, 0=無限) | 常に左へ進みつつ Y を自機に寄せる。OutQuad 再現 |
| `SineMotion` | `baseVelocity`、`amplitude`、`frequency`(Hz)、`duration`(s, 0=無限) | `baseVelocity` で進みつつ、その垂直方向にサイン波で揺れる。差分加算なので他の移動と合成できる。OutQuad 再現 |
| `AnchoredSineMotion` | `anchor`、`amplitude`(Vector3)、`angularSpeed`(rad/s)、`cueSeconds`(s)、`cueOffset`、`duration`(s, 0=無限) | `anchor` の周りで絶対位置として揺れる（開始時に anchor へ吸着）。最初の `cueSeconds` は `cueOffset` 方向へ半周期の合図動作 |
| `QuadraticMotion` | `initialVelocity`、`acceleration`、`duration`(s, 0=無限) | 等加速度運動 |

### 応用

| ステップ | フィールド | 動作 |
|---|---|---|
| `AcceleratedMotion` | 通常: `direction`、`useConfiguredDirection`、`initialSpeed`、`acceleration`(単位/s²、負で減速)、`maxSpeed`、`minSpeed`、`duration`(s, 0=無限) | 発射方向（`useConfiguredDirection` が ON なら `direction`）へ加速。速さを min〜max に制限 |
| | 放物運動: `useWorldAcceleration`、`worldAcceleration` | 初速 + 固定方向の加速度で放物運動 |
| `VertexParabolaMotion` | `orientation`(Vertical/Horizontal)、`vertex`、`initialSpeed`、`isRelative`、`oscillateVertex`、`vertexAmplitude`、`vertexAngularSpeed`(rad/s)、`attackTimeOffset`(s) | 発射位置から頂点へ向かい折り返す軸平行の放物線。`oscillateVertex` なら発射時刻 (`AttackTime − attackTimeOffset`) で頂点を揺らす。解けない設定なら弾を即消す。終わらない |
| `ReflectMotion` | `speed`、`maxReflections`、`reflectTop` / `reflectBottom` / `reflectLeft` / `reflectRight` | 発射方向へ進み、一度画面内に入ってから指定した画面端で反射する。終わらない。OutQuad 再現 |
| `ZigzagMotion` | `speed`、`angleDegrees`(度)、`segmentLength`、`initialSign`、`maxReflections`(0=無制限)、`firstSegmentLength`(0=segmentLength)、`duration`(s, 0=無限)、`fallbackAxis`、`rotateToVelocity`、`rotationOffsetDegrees` | 発射方向（無ければ `fallbackAxis`）を軸に ±angle で折り返しながら進む |
| `SpiralMotion` | `startRadius`、`targetRadius`、`duration`(s)、`rotationSpeed`(度/s、負で時計回り) | 開始位置から発射方向の逆へ `startRadius` の点を中心に回りながら半径を変える。ChargeEffect が使う |
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
 └─ Parallel [All]
     ├─ Move BossIntro
     └─ Until [Finish]  cond: FrameElapsed(601)
         └─ Sequence
             ├─ Wait 60
             └─ Repeat ∞ / 60f
                 └─ NWayShot
```

外側の Until がフェーズの終了条件、内側の Until が攻撃の打ち切り。攻撃が先に終わってもフェーズを終えたくない場合は外側を `Hold` にする（Stage1 ボスのイントロ）。

### k フレームおきの発射 = Repeat

```text
Until cond: FrameElapsed(481)
 └─ Repeat ∞ / 150f
     └─ NWayShot
```

`Repeat(interval=k)` は k フレームの周期で子を繰り返す。子が時間のかかる Sequence でも、周期より短ければ開始間隔はずれない。1 周期の中で複数の発射タイミングがある場合は `Repeat ∞ / k { Sequence[NWayShot, Wait a, NWayShot, Wait (b−a), NWayShot] }` のように子の中に並べる（残りは Repeat が周期まで待つ）。

### BGM 同期のボスフェーズ = Phase(BgmSamples)

```text
Loop ∞
 └─ Sequence
     ├─ Phase «MelodyA»  〜 BgmSamples(1263874)
     ├─ Phase «MelodyB»  〜 BgmSamples(2976121)
     └─ Phase «Chorus»   〜 BgmSamples(3897391)
```

各フェーズを BGM のサンプル位置で区切る。BgmSamples は「閾値到達」または「ループで先頭へ戻った」で成立し、開始時点で既に閾値を過ぎていればループを待つので、最後のフェーズもループと同時に終わる。閾値は `BgmData` のループ位置と同じサンプル単位。

### 発狂 (Strong) = HpSections(NextPhase) + If($stage ≧ 1)

```text
HpSections  25%  次の Phase から → $stage     … 区切りはここだけ (HP ゲージにも出る)
└─ …
    └─ Phase [Hold]  〜 BgmSamples(...)
        └─ Parallel
            ├─ Move (フェーズの移動)
            └─ If  cond: $stage ≧ 1
                ├─ 1: 発狂本体
                └─ 2: 通常本体
```

区切りを越えた後の次の Phase の開始で `$stage` が 1 になり、そのフェーズから 1 番目（発狂）を実行する。越えてから次の Phase までの被ダメージは `pendingDamageScale` 倍（Stage1 は ×0.25、Stage3 は ×1）。BGM の区切りと移動は発狂・通常で共通なので If の外に置く（Stage1 / Stage3 ボス）。

SubTree は複数箇所から使い回す攻撃・部品（プリセット）にだけ使い、1 箇所でしか使わない処理はツリーに直接書く。入れ子が深くなるとグラフで追いにくくなるため。

### HP 閾値までランダムにフェーズを選ぶ

```text
HpSections  40%
 ├─ Loop ∞
 │   └─ RandomSelect (avoidRepeat)
 │       ├─ Sequence [Until #PhaseA (FrameElapsed), Until #Interlude]
 │       └─ ...
 └─ Sequence
     ├─ Until #Interlude (FrameElapsed)  … 定位置へ戻る
     └─ 次のグループ
```

HP が区切りを下回った瞬間に HpSections が 1 番目の子を中断して 2 番目へ進む（Stage2 ボス）。HP の区切りはこの 1 か所だけに置き、各フェーズの条件に同じ `HpBelow` を重ねない。`Inverter` / `ForceSuccess` で Loop を失敗させて抜ける書き方はしない。

### 雑魚 = 共通ツリー Enemy + overrides

```text
Commons/BT/Enemy (EnemyTreeAsset)
 └─ Parallel [All]
     ├─ MoveSelf                        … Spawn の motionPreset / motion (Stage{N}/BT/Motion/*)
     └─ Sequence
         ├─ SetDespawnBehavior  $death  … 撃破時のツリー (空なら無し。reasons = Defeated)
         └─ SubTree             $attack … Stage{N}/BT/Attack/S{N}_* (CommonTreeAsset)

Stage{N}/BT/Attack/S{N}_*
 └─ Until cond: FrameElapsed(N)
     └─ Repeat ∞ / k f
         └─ NWayShot
```

Spawn（Wave の entries を含む）で `behavior = Enemy` とし、移動を `motionPreset`（または `motion`）に、`overrides` で `attack` / `death` を渡す。移動と攻撃は Parallel で同時に進む。ツリーが終わっても敵は消えず、撃破・寿命・画面外で消えるまで残る。攻撃ツリーは攻撃だけを書く。攻撃の途中で移動を変える、HP で行動を変える（`Sequence[Until(HpBelow){通常}, 後半]`）など、この形に収まらない敵だけ `Stage{N}/BT/Enemy/` に専用ツリーを作る（例: `Stage2/BT/Enemy/S2_2_WayAzuma`）。

### 弾のツリーと合図

```text
撃つ側 (敵の攻撃ツリー)
 └─ Sequence
     ├─ NWayShot  bullet.behavior = 弾のツリー
     ├─ Wait 60
     └─ SendSignal "split" → Spawned

弾のツリー (BulletTreeAsset)
 └─ Sequence
     ├─ Until [Finish]  cond: Signaled("split")
     │   └─ MoveSelf
     └─ (合図の後の処理。分裂して撃つ、Despawn など)
```

（組み方の例。実データにある定型ではない）弾の動きを変えたいときは `BulletConfig.behavior` に `BulletTreeAsset` を指定し、移動させる部分に MoveSelf を置く。合図は送った次のフレームに届き、そのフレームに評価された `Signaled` だけが成立する。弾が消えるときに子弾を撃つなら、弾のツリーで `SetDespawnBehavior`（`reasons` に `Expired` など）を使う。

### 道中 = Section "Way" [ PlayBgm, Wave..., Wait ]

```text
Section "Way" [Way]
 └─ Sequence
     ├─ PlayBgm  BattleWay
     ├─ Wave  (150f: Spawn, 195f: Spawn, ...)  until: NoActiveEnemies
     ├─ Wave  ...
     └─ Wait 120
```

出現の時刻を Wave の `entries` に並べ、全員出し終えたら `until`（全滅など）を待って次の Wave へ進む。Stage3 道中では一部の Wave を `WithWind` で包み、その間だけ風を吹かせる。

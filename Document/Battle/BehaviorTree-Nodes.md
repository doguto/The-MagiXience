# Battle BehaviorTree 汎用ノードリファレンス

ステージ・敵・弾のどのツリーでも使う汎用ノード（制御・条件・基本 Action）をまとめる。
仕組み全体は [BehaviorTree.md](./BehaviorTree.md) を参照すること。
発射・移動・出現・ボス・ステージ進行などバトル固有のノードと部品、`MotionStep` はここに載せない。ソースの Tooltip / `Summary` と、BT エディタのノード要約を正とする。

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

## 3. 条件 (Condition)

Until / CheckCondition / WaitUntil の `condition` に設定する。

| 条件 | フィールド | 成立する条件 |
|---|---|---|
| `Constant` | `value` | 常に `value` |
| `FrameElapsed` | `frames` ($ 可, f) | Reset（Until などの開始）からの経過フレーム ≧ frames |
| `Compare` | `a` ($ 可)、`op`（≧ > ≦ < = ≠）、`b` ($ 可) | `a op b`。パラメータは判定のたびに読み直す |
| `Signaled` | `key` | このフレームに自分（`Self`）へ合図 `key` が届いた（SendSignal）。届いたフレームに判定されなかった合図は受け取れない。`Self` が無ければ不成立 |
| `And` | `conditions` | 全て成立（空なら成立） |
| `Or` | `conditions` | どれか成立（空なら不成立） |
| `Not` | `condition` | 中の条件が不成立（未設定なら成立） |


## 4. 基本 Action

### Wait

| フィールド | 単位 | 説明 |
|---|---|---|

開始フレーム f から数えて f+frames のフレームで Success。

### Calc

`resultParam = a 演算 b` を 1 回計算してパラメータに書き込み、即 Success。演算は 1 ノードに 1 つで、式が長いときは Calc を並べる。

| フィールド | 説明 |
|---|---|

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

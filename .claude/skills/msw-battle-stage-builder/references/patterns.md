# よく使う組み方

ノードの細かい設定項目は `Document/Battle/BehaviorTree-Nodes.md` を参照する。ここでは実データ（`DataStore/Stage{1-4}/BT/`）で使っている形と、避けるべき書き方をまとめる。

## 基本の考え方

- **フェーズ = `Until`**: 「条件を満たすまで中身を実行する」。中身が先に終わったときの扱いは `Until` のモードで決める（Finish: 終了 / Restart: やり直す / Hold: 条件まで待つ）。
- **時刻つきの出来事 = `Sequence` + `Wait`**: 撃つ・出すなど即時に終わる出来事は `Sequence[Wait a, X, Wait b, Y]` と間隔で並べる。同じ時刻のものは Wait を挟まずに並べる。BT エディタでは各子に開始からのフレーム数（`@150f`）が出る。
- **時間のかかるものを重ねる = `Parallel` + `Delay`**: `Repeat` / `Move` / `SubTree` など終わるまで時間がかかる子を同時に走らせるときだけ使う。Policy `Duration` でその長さで打ち切る（子が先に終わっても長さまで待つ）。
- **一定周期の繰り返し = `Repeat`**: 子を N 回（0 で無限）、k フレームの周期で直列に繰り返す。子が早く終われば周期まで待ち、最後の回の後は待たない。回が重なることはない。
- **周回 = `Loop`**: 子が終わるたびにやり直す。「1 周 = 最後に余りの `Wait` を置いた `Sequence`（重ねるなら `Parallel(Duration)`）」と組み合わせて、周期の決まった攻撃に使う。

## 道中（ステージツリー）

```
Sequence
  Until #Phase1 (NoActiveEnemies / FrameElapsed / BgmSamples …)
    Sequence
      Wait 60
      Spawn 敵A (Behavior = Enemy, Overrides: movement / attack)   … @60f
      Wait 60
      Spawn 敵B                           … @120f
  Until #Phase2 …
```
- 「全滅したら次へ」は `Sequence[Wait, Spawn …, WaitUntil(NoActiveEnemies)]` でも書ける。
- 弾消しは `ClearBullets`、背景などの一時オブジェクトは `WithPrefab`。
- 道中で風を使う区間は `WithWind` で囲む（ステージに `WindService` の宣言が必要）。

## 雑魚（共通ツリー `Commons/BT/Enemy` + Overrides）

```
Commons/BT/Enemy (パラメータ: movement / attack / death)
  Sequence
    SetDeathBehavior ($death)     … 空なら撃破時の行動なし
    SetMovement ($movement)       … 空なら動かない
    SubTree ($attack)             … 空なら攻撃しない

Stage{N}/BT/Attack/S{N}_xxx (攻撃ツリー)
  Sequence
    Wait 75
    Repeat ∞ / 15f おき
      NWayShot など (弾 / 撃ち方の設定 / 向き)
```
- 雑魚は敵ごとのツリーを作らない。Wave の entries / Spawn で Behavior = `Enemy` とし、Overrides で `movement`（`Motion/` の MotionPreset）、`attack`（`Attack/` の攻撃ツリー）、`death`（撃破時のツリー）を渡す。
- 攻撃ツリーは中身が同じなら 1 つを使い回す。移動だけが違う敵は `movement` だけ変える。
- 移動は `SetMovement` で「背景で流し続ける」。移動の完了を待ちたいときだけ `Move` を使う。
- 攻撃の途中で移動を変える、HP で行動を変えるなど、共通ツリーの形に収まらない敵だけ `Enemy/` に専用ツリーを作る（例: `Stage2/BT/Enemy/S2_2_WayAzuma`）。専用ツリーでも攻撃部分は `Attack/` を `SubTree` で参照し、中身を複製しない。
- ツリーのパラメータ（`$名前`）を使えるのは、数値では `Parallel`（Duration）/ `Loop` / `Repeat` / `Delay` / `Wait` / `FrameElapsed` / `BgmReached` と `ChargeEffect` / `MovePath` / `PlaceAlongPath` の一部、アセット参照では `*Param` 欄（`ChargeEffect.prefabParam` / `SetMovement.presetParam` / `SubTree.treeParam` / `SetDeathBehavior.treeParam` / `BulletSpec.prefabParam`）だけ。弾を撃つノードの向きや本数などはパラメータにできない。
- `Spawn` の overrides には固定値を書く。親ツリーのパラメータを渡す（bind to parent）は敵ツリーでは効かない。
- 撃破時のツリーは 1 フレームだけ実行される。`NWayShot` などの発射や `Spawn` など、即時に終わるものだけを書く。

## ボスツリー（`BossDefinition.Tree`、ボス自身が実行）

BGM の区切りでフェーズを進める例:
```
Sequence
  Until #Intro (BgmSamples)
    Sequence
      SetMovement BossIntro (Continue If Same)
      Repeat ∞ / 60f おき → NWayShot …
  Until #MelodyA (BgmSamples) …
```

発狂（Strong）版を持つフェーズ:
```
Selector
  Sequence
    CheckCondition (NormalHpDepleted)
    EnterStrongMode
    Until #Strong … (発狂版の中身)
  Until … (通常版の中身)
```

HP が閾値を切るまでフェーズをランダムに回す例:
```
Sequence
  Until #Group0 (HpBelow Self 40%)
    Loop ∞
      Random
        Sequence [Until #PhaseA (FrameElapsed), Until #Interlude …]
        Sequence [Until #PhaseB (FrameElapsed), Until #Interlude …]
  Until #Interlude (FrameElapsed) → SetMovement 定位置へ戻る
  …次のグループ
```
- HP の判定は外側の `Until` 1 か所に置く。各フェーズの条件に同じ `HpBelow` を `Or` で重ねない。
- ボスの攻撃周期と移動の周期は自動では揃わない。周期攻撃は 1 周の長さ（Sequence の Wait の合計や `Parallel(Duration)`）か `Repeat` の間隔を、移動プリセットの 1 周の長さに合わせる。
- 進行ゲージ（`BossPhaseProgressGauge`）を持つボスは、各フェーズの先頭に `PhaseMarker` を置く。

## 攻撃ツリー（`Attack/`）

- 雑魚の攻撃は必ず `Attack/` に置き、`Enemy` の `attack` パラメータで参照する。
- ボスで複数のフェーズから使う攻撃も `Attack/` に切り出し、`SubTree` で参照する。既存の攻撃をコピーして別アセットにしない（元を直せば全体に効くようにする）。
- 逆に、ボスのツリーで 1 か所でしか使わない攻撃や、数ノードで書ける小さな攻撃は SubTree にしない。ツリーを開いたときに中身が見えている方が読みやすい。SubTree にするのは「複数のツリーから使う」か「名前の付いたひとまとまりの攻撃として調整する」ものだけ。
- 値だけ変えたいときは、攻撃ツリーにパラメータを作り、`SubTree` の overrides で上書きする。弾だけ変えたいときは `BulletSpec.prefabParam` を使う。

## ビーム

弾を同じ向きに連射してビームにする場合は `BulletBeam` 1 つで書く。予測線は `WarningLine` で包んで出す（予測線とビームの位置・向きは WarningLine が 1 回だけ決めて揃えるので、ビーム側にだけ設定すればよい）。
```
WarningLine  Prefab: 2_WarningLine / Frames 78 (撃ち始めまで) / Display Frames 60 (表示の長さ)
  └─ BulletBeam
       Origin: Self (撃つ敵の位置) / World (Position をワールド座標として使う)
       Direction: Fixed 180°        Pattern: NWay 9本 40°おき   … 本数と角度
       Bullets: [TriUp, TriDown]    … 同じ位置・向きに重ねる弾
       Shot Count 36 / Interval 5f
```
画像 1 枚のビームは `SpriteBeam` を使う。予測線の付け方は同じ（`WarningLine{ SpriteBeam }`）。
溜めと同時に予測線を出すときは `Parallel{ 溜め, WarningLine{ ビーム } }` と並べる。

## ステージ固有の機能（サービス）

- 風のように一部のステージだけが使う機能は、`BattleStageAsset` の `Services` に宣言する。宣言がないステージでは存在しない扱いになり、ノードから使うと警告が 1 回出る。
- 既存のサービス: `WindService`。開始時に自機の移動補正（`IMoveModifier`）、弾の外力（`IBulletForce`）、風向きの矢印表示を自分で登録し、終了時に外す。
- 新しいサービスを足すときは `StageServiceDefinition`（アセットに置く設定）と `IStageService`（実行時の状態）を作る。自機や Presenter にサービス専用の `SetXxx` を足さない。汎用の受け口で足りない場合は、受け口の設計をユーザーと相談する。新しい C# ファイルになるので、作る前に許可を取る。

## やってはいけない書き方

| 書き方 | 代わりに |
|---|---|
| 同じ攻撃を `Delay 0, 5, 10, …` と並べる | `Repeat`（回数と間隔） |
| `Loop` → `Parallel(Duration k)` → 攻撃 1 つ | `Repeat ∞ / k フレームおき` |
| 向きだけ違う攻撃アセットを方向の数だけ作る | `ShotPattern`（NWay など）か、向きをパラメータにする |
| 弾の種類違いでノードを 2 つ並べる（ビーム） | `BulletBeam` の Bullets に並べる |
| 子のない `Parallel` / `Sequence`、子 1 つだけの `Sequence` | 取り除く |
| 即時に終わる子だけの `Parallel` に `Delay` を並べる | `Sequence` + `Wait` |
| `Sequence` の中にコメントなしの `Sequence` を入れる | 外側に展開する |
| `Inverter` + `ForceSuccess` で Loop をわざと失敗させて抜ける | `Until(条件)` で Loop を囲む |
| 同じ終了条件を外側と各フェーズの両方に書く | 外側の `Until` 1 か所にまとめる |
| 成立しない条件の `Until`（`HpBelow 0%`、極端に長い `FrameElapsed` など） | 取り除く。撃破でツリーごと止まる |
| 移動 × 攻撃の組み合わせごとに敵ツリーを作る | 共通ツリー `Enemy` に Overrides で `movement` / `attack` を渡す |
| 敵を消すために Prefab 側のスクリプトで `Destroy` する | `Despawn` ノード（内部は `ActorDespawn.Despawn`。プール品はプールへ戻る） |
| ステージ固有の処理を全ステージのツリーに入れる（例: 風を使わないステージで風を設定） | サービスとして、使うステージにだけ宣言する |
| 自機や Presenter にステージ固有の処理を書く | サービス側から汎用の受け口へ働きかける |

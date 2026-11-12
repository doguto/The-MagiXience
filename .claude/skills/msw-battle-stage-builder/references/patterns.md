# よく使う組み方

ノードの細かい設定項目は `Document/Battle/BehaviorTree-Nodes.md` を参照する。ここでは実データ（`DataStore/Stage{1-4}/BT/`）で使っている形と、避けるべき書き方をまとめる。

## 基本の考え方

- **フェーズ = `Until`**: 「条件を満たすまで中身を実行する」。中身が先に終わったときの扱いは `Until` のモードで決める（Finish: 終了 / Restart: やり直す / Hold: 条件まで待つ）。
- **時刻つきの出来事 = `Timeline` + `Delay`**: `Timeline` の子は同時に始まり、各子の開始時刻は `Delay` で指定する。`Duration` を入れるとその長さで打ち切る。
- **一定間隔の繰り返し = `Repeat`**: 子を N 回（0 で無限）、k フレームおきに開始する。前の回が終わっていなくても次を始める。
- **周回 = `Loop`**: 子が終わるたびにやり直す。「1 周 = `Timeline(Duration)`」と組み合わせて、周期の決まった攻撃に使う。

## 道中（ステージツリー）

```
Sequence
  Until #Phase1 (NoActiveEnemies / FrameElapsed / BgmSamples …)
    Timeline
      Delay 60  → Spawn 敵A (Behavior = S{N}_EnemyA)
      Delay 120 → Spawn 敵B
  Until #Phase2 …
```
- 「全滅したら次へ」は `Sequence[出現の Timeline, WaitUntil(NoActiveEnemies)]` でも書ける。
- 弾消しは `ClearBullets`、背景などの一時オブジェクトは `WithPrefab`。
- 道中で風を使う区間は `WithWind` で囲む（ステージに `WindService` の宣言が必要）。

## 敵ツリー（`Enemy/`）

```
Sequence
  SetDeathBehavior (倒されたときに実行するツリー)   … 必要なときだけ
  SetMovement (MotionPreset)
  Sequence
    Wait 75
    Repeat ∞ / 15f おき
      Fire (弾 / ShotPattern / 向き)
```
- 移動は `SetMovement` で「背景で流し続ける」。移動の完了を待ちたいときだけ `Move` を使う。
- ツリーのパラメータ（`$名前`）を使えるのは、今のところ `Timeline` / `Loop` / `Repeat` / `Delay` / `Wait` / `FrameElapsed` / `BgmReached` の数値だけ。`SetMovement` のプリセットや `Fire` の設定はパラメータにできない。
- そのため、移動だけが違う敵は敵ツリーを分けてよいが、攻撃部分は `Attack/` のツリーを `SubTree` で共有し、中身を複製しない。移動・攻撃のパラメータ化は未対応で、必要になったらユーザーと相談する。
- `Spawn` の overrides には固定値を書く。親ツリーのパラメータを渡す（bind to parent）は敵ツリーでは効かない。

## ボスツリー（`BattleStageAsset.BossTree`、ボス自身が実行）

BGM の区切りでフェーズを進める例:
```
Sequence
  Until #Intro (BgmSamples)
    Sequence
      SetMovement BossIntro (Continue If Same)
      Repeat ∞ / 60f おき → Fire …
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

HP が閾値を切るまでフェーズをランダムに回す例（周の区切りでだけ判定する）:
```
ForceSuccess
  Loop ∞
    Sequence
      Inverter → CheckCondition (HpBelow Self 40%)
      Random
        Sequence [Until #PhaseA …, Until #Interlude …]
        Sequence [Until #PhaseB …, Until #Interlude …]
```
- ボスの攻撃周期と移動の周期は自動では揃わない。周期攻撃は `Timeline(Duration)` か `Repeat` の間隔を、移動プリセットの 1 周の長さに合わせる。
- 進行ゲージ（`BossPhaseProgressGauge`）を持つボスは、各フェーズの先頭に `PhaseMarker` を置く。

## 攻撃ツリー（`Attack/`）

- 複数の敵・フェーズで使う攻撃は `Attack/` に切り出し、`SubTree` で参照する。既存の攻撃をコピーして別アセットにしない（元を直せば全体に効くようにする）。
- 値だけ変えたいときは、攻撃ツリーにパラメータを作り、`SubTree` の overrides で上書きする。

## ビーム

弾を同じ向きに連射してビームにする場合は `BulletBeam` 1 つで書く。
```
BulletBeam
  Origin: Self (撃つ敵の位置) / World (Position をワールド座標として使う)
  Direction: Fixed 180°        Pattern: NWay 9本 40°おき   … 本数と角度
  Bullets: [TriUp, TriDown]    … 同じ位置・向きに重ねる弾
  Shot Count 36 / Interval 5f
  Warning Prefab / Warning Frames 78 (撃ち始めまで) / Warning Display Frames 60 (表示の長さ)
```
画像 1 枚のビームは `SpriteBeam` を使う。

## ステージ固有の機能（サービス）

- 風のように一部のステージだけが使う機能は、`BattleStageAsset` の `Services` に宣言する。宣言がないステージでは存在しない扱いになり、ノードから使うと警告が 1 回出る。
- 既存のサービス: `WindService`。開始時に自機の移動補正（`IMoveModifier`）、弾の外力（`IBulletForce`）、風向きの矢印表示を自分で登録し、終了時に外す。
- 新しいサービスを足すときは `StageServiceDefinition`（アセットに置く設定）と `IStageService`（実行時の状態）を作る。自機や Presenter にサービス専用の `SetXxx` を足さない。汎用の受け口で足りない場合は、受け口の設計をユーザーと相談する。新しい C# ファイルになるので、作る前に許可を取る。

## やってはいけない書き方

| 書き方 | 代わりに |
|---|---|
| 同じ攻撃を `Delay 0, 5, 10, …` と並べる | `Repeat`（回数と間隔） |
| `Loop` → `Timeline(k)` → 攻撃 1 つ | `Repeat ∞ / k フレームおき` |
| 向きだけ違う攻撃アセットを方向の数だけ作る | `ShotPattern`（NWay など）か、向きをパラメータにする |
| 弾の種類違いでノードを 2 つ並べる（ビーム） | `BulletBeam` の Bullets に並べる |
| 子のない `Timeline` / `Sequence`、子 1 つだけの `Sequence` | 取り除く |
| 移動 × 攻撃の組み合わせごとに、攻撃の中身まで複製した敵ツリーを作る | 攻撃は `SubTree` で共有する（移動・攻撃のパラメータ化は未対応） |
| ステージ固有の処理を全ステージのツリーに入れる（例: 風を使わないステージで風を設定） | サービスとして、使うステージにだけ宣言する |
| 自機や Presenter にステージ固有の処理を書く | サービス側から汎用の受け口へ働きかける |

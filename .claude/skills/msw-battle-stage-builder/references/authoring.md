# アセットの作り方と動作確認

## 作るアセットの種類

| 種類 | 作成メニュー（Project ウィンドウ右クリック → Create） | 置き場所 |
|---|---|---|
| `BattleStageAsset` | `Battle/Battle Stage` | `DataStore/Stage{N}/BT/Stage{N}_BT.asset`（1 ステージに 1 つ） |
| `BossDefinition` | `Battle/Boss Definition` | `DataStore/Stage{N}/BT/Stage{N}_Boss.asset` |
| `StageTreeAsset`（ステージツリー） | `Battle/Behavior Tree/Stage` | `DataStore/Stage{N}/BT/Stage/Stage{N}_Tree.asset` |
| `EnemyTreeAsset`（敵・ボスが実行するツリー） | `Battle/Behavior Tree/Enemy` | `DataStore/Stage{N}/BT/Stage/Stage{N}Boss_Tree.asset`（ボス）、`DataStore/Stage{N}/BT/Enemy`（共通ツリーに収まらない敵の専用ツリー） |
| `CommonTreeAsset`（SubTree で使い回すツリー） | `Battle/Behavior Tree/Common` | `DataStore/Stage{N}/BT/Attack`（攻撃。**攻撃ツリーは必ず Common**）、撃破時のツリー（`Enemy/` の `*_Death` など） |
| `BulletTreeAsset`（弾が実行するツリー） | `Battle/Behavior Tree/Bullet` | 弾に途中で動きを変えさせるときだけ。`BulletConfig.behavior` に指定する（置き場所の慣例はまだ無い。`DataStore/Stage{N}/BT/` 配下にフォルダを作る前にユーザーと相談する） |
| `MotionPreset` | `Battle/Motion Preset` | `DataStore/Stage{N}/BT/Motion` |
| `EaseCurveAsset` | `Battle/Ease Curve` | `DataStore/Stage{N}/BT/Ease` |
| `ScenarioData` | （`ScenarioImporter` で `.txt` から生成。[stage-registration.md](stage-registration.md) の「会話の接続」） | `DataStore/Stage{N}/Scenario/Stage{N}WayScenario.asset`（ボス前）、`Stage{N}BossScenario.asset`（撃破後） |

ツリーの種類で、置けるノードと SubTree で参照できるツリーが決まる（`Document/Battle/BehaviorTree.md` 3.2）。

- SubTree で参照できるのは Common か、自分と同じ種類のツリーだけ。Common のツリーからは Common だけ。そのため攻撃ツリーを Enemy で作ると、ステージツリーや弾のツリーから使えない。
- `SetInvincible` / `BossWindCue`（`EnemyActionNode`）は Enemy のツリーにだけ置ける。
- `Spawn.behavior` / `BossDefinition.tree` / `BossBattle.tree` は Enemy、`BattleStageAsset.tree` は Stage、`BulletConfig.behavior` は Bullet のツリーしか受け付けない。
- BT エディタのパレット・検索・ドロップ・貼り付けは種類で絞り込まれ、タイトルに `[Stage]` などの種類が出る。置きたいノードがパレットに無いときは、ツリーの種類を確認する。

命名の目安:
- ステージツリー: `Stage{N}_Tree`。ボスのツリー: `Stage{N}Boss_Tree`
- 攻撃ツリー: `S{N}_{攻撃の名前}`。中身が同じ攻撃は 1 つにまとめる。向きや本数だけが違うものは、別アセットにせず `ShotPattern`（NWay など）で表す
- 雑魚の移動: `Motion/` の MotionPreset（例: `S{N}_{敵の名前}_Move`）
- 雑魚は敵ごとのツリーを作らない。Spawn の Behavior に `Commons/BT/Enemy` を指定し、移動は Spawn の `Motion Preset`（または `Motion`）に、Overrides で `attack` / `death` を渡す
- 特殊な敵の専用ツリー（`Enemy/`）: `S{N}_{敵の名前}`

## ユーザーに操作を案内する場合（BT エディタ）

1. `Window/Battle/Behavior Tree Editor` を開く。ツリーのアセットをダブルクリックしても開く。
2. 左のノード一覧からグラフへドラッグ＆ドロップしてノードを置く。
3. 親ノードの下側のポートから子ノードの上側のポートへドラッグして繋ぐ。Composite の子は左から順に実行される。
4. ノードを選択すると、ノード上に設定欄が出る。
5. 右ペインでツリーのパラメータを追加する。値欄の `$` から選ぶか、パラメータを値欄へドラッグすると、その値がパラメータ参照になる。
6. 配置は常に自動整列される。ノードのドラッグは兄弟の並べ替えにだけ使う。

Inspector で `BehaviorTreeAsset` を直接触るより、エディタの方が子の順番と構造を確認しやすい。

ステージツリーの組み方（形は SKILL.md の「仕組みの要点」）:

1. ルートに `Stage`、その子に `Section` を道中・ボスの順に置く。Section の `label` と `situation`（Way / Boss）を設定する。
2. 道中の Section の下に `Sequence[ PlayBgm(BattleWay), Wave..., Wait 120 ]` を置く。Wave の `entries` に出現フレームと Spawn の内容を並べ、`until` に `NoActiveEnemies` などを入れる。雑魚の Spawn は Behavior に `Commons/BT/Enemy` を指定し、`Motion Preset` に移動（`Motion/` の MotionPreset）を入れ、Overrides の `+` で `attack`（攻撃ツリー）/ `death`（撃破時のツリー、任意）を追加して値を入れる。
3. ボスの Section の下に `Sequence[ PlayScenario, BossBattle, Wait 120, PlayScenario ]` を置く。
4. 1 つ目の PlayScenario にボス前の会話を設定し、ノードを右クリック →「会話のコマンドから OnCommand を作る」で OnCommand を作る。`SpawnBoss` の下に `SpawnBoss`（`Stage{N}_Boss` を指定）、`PlayBossBgm` の下に `PlayBgm(BattleBoss)` を繋ぐ。
5. 2 つ目の PlayScenario に撃破後の会話を設定し、`skipOnResume` と `restorePlayerControl` を OFF にする。
6. PlayScenario ノードの要約欄に ⚠ が出ていないことを確認する（コマンドと OnCommand の過不足）。

## エージェントが Unity MCP で作る場合

`[SerializeReference]` のノードは MCP の patch 系ツールでは作れない。`execute_code` で C# を実行して組み立てる。

注意点:
- `execute_code` はメソッド本体に埋め込まれるため、`using` 文が書けない。型名は完全修飾名で書く。
- コンパイラは C# 6 相当になることがある。ローカル関数・タプル・`is not` は使わず、ラムダで代用する。
- ノード定義の多くは private な `[SerializeField]` を持つ。リフレクションのヘルパーで設定する。
- アセットの削除は `execute_code` では弾かれる（`AssetDatabase.DeleteAsset` がブロックされる）。`manage_asset` の `delete` を使う。
- 書き換えたら `EditorUtility.SetDirty` と `AssetDatabase.SaveAssets` を呼ぶ。

組み立ての例（攻撃ツリー: 60 フレームおきに自機狙い 3way を撃つ）:

```csharp
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
System.Func<System.Type, string, System.Reflection.FieldInfo> field = (t, n) =>
{
    for (var x = t; x != null; x = x.BaseType)
    {
        var f = x.GetField(n, F | System.Reflection.BindingFlags.DeclaredOnly);
        if (f != null) return f;
    }
    return null;
};
System.Action<object, string, object> set = (o, n, v) => field(o.GetType(), n).SetValue(o, v);

// 攻撃ツリーは CommonTreeAsset で作る (BehaviorTreeAsset は抽象クラスなので CreateInstance できない)
var tree = UnityEngine.ScriptableObject.CreateInstance<Project.Scenes.Battle.Scripts.BehaviorTree.CommonTreeAsset>();

var fire = new Project.Scenes.Battle.Scripts.BehaviorTree.NWayShot();
var nway = new Project.Scenes.Battle.Scripts.BehaviorTree.NWayPattern();
set(nway, "wayCount", new Project.Scenes.Battle.Scripts.BehaviorTree.BtInt(3));
set(nway, "spreadAngle", new Project.Scenes.Battle.Scripts.BehaviorTree.BtFloat(20f));
set(fire, "pattern", nway);   // 撃ち方はノードの種類で決まる。pattern はその種類のパターン
set(fire, "direction", new Project.Scenes.Battle.Scripts.BehaviorTree.AimDirection());
// set(fire, "bullet", new Project.Scenes.Battle.Scripts.BehaviorTree.BulletConfig { prefab = ..., damage = 10, motionPreset = ..., lifetime = 0 /* フレーム。0 で無制限 */ });

var repeat = new Project.Scenes.Battle.Scripts.BehaviorTree.Repeat();
set(repeat, "count", new Project.Scenes.Battle.Scripts.BehaviorTree.BtInt(0));
set(repeat, "interval", new Project.Scenes.Battle.Scripts.BehaviorTree.BtFloat(60f));
repeat.SetChildren(new Project.Scenes.Battle.Scripts.BehaviorTree.Node[] { fire });
tree.Root = repeat;

UnityEditor.AssetDatabase.CreateAsset(tree, "Assets/Project/DataStore/Stage5/BT/Attack/S5_Sample.asset");
UnityEditor.AssetDatabase.SaveAssets();
```

雑魚に使うときは、Spawn の `behavior` に `Commons/BT/Enemy`（`EnemyTreeAsset`）を、`motionPreset` に移動を、`overrides` に Object パラメータを入れる（`TreeParameter` の `objectValue` は private なので同じヘルパーで設定する）:

```csharp
var spawn = new Project.Scenes.Battle.Scripts.BehaviorTree.Spawn();
set(spawn, "behavior", UnityEditor.AssetDatabase.LoadAssetAtPath<Project.Scenes.Battle.Scripts.BehaviorTree.EnemyTreeAsset>("Assets/Project/DataStore/Commons/BT/Enemy.asset"));
// 移動はツリーのパラメータではなく Spawn に持たせる (共通ツリーの MoveSelf が実行する)
set(spawn, "motionPreset", UnityEditor.AssetDatabase.LoadAssetAtPath<Project.Scenes.Battle.Scripts.Runtime.Motion.MotionPreset>("Assets/Project/DataStore/Stage5/BT/Motion/S5_Sample_Move.asset"));
var attack = new Project.Scenes.Battle.Scripts.BehaviorTree.TreeParameter("attack", Project.Scenes.Battle.Scripts.BehaviorTree.TreeParameterKind.Object);
set(attack, "objectValue", tree);
var overrides = (System.Collections.Generic.List<Project.Scenes.Battle.Scripts.BehaviorTree.TreeParameter>)field(spawn.GetType(), "overrides").GetValue(spawn);
overrides.Add(attack);
// death (撃破時のツリー。CommonTreeAsset) も同様。共通ツリー Enemy のパラメータは attack と death だけ
```

- フィールド名は `Assets/Project/Scenes/Battle/Scripts/BehaviorTree/` のソースで確認する。推測で書かない。
- 作ったツリーは BT エディタで開いて構造を確認してからユーザーに見せる（配置は自動整列される）。
- ツリーアセットは種類ごとの型（`StageTreeAsset` / `EnemyTreeAsset` / `BulletTreeAsset` / `CommonTreeAsset`）で `CreateInstance` する。`BehaviorTreeAsset` は抽象クラス。
- フォルダを作る `AssetDatabase.CreateFolder` は再インポートを起こすことがある。フォルダを先に作り、アセットは `AssetDatabase.StartAssetEditing` / `StopAssetEditing` の間でまとめて作る。
- 既存のツリーを直すときは、全体を作り直さずに対象ノードだけを差し替える。ユーザーが調整した値を残すため。

## 動作確認

1. `read_console` でコンパイルエラーがないことを確認する。`refresh_unity` のあと、型がまだ読み込まれていない場合はもう一度確認する。
2. `Tools/Battle Debug Launcher` でステージ番号と開始 Section を選んで再生する。MCP から行う場合は、Battle シーンの `DebugRuntimeModelInitializer` の `stageNumber` / `startSection`（Stage 直下の並び順）を設定して Play にする。途中の Section から始めると、その Section の `skipOnResume` の会話は飛ばしてコマンドだけ実行される。**シーンは保存しない。** 誤って保存した場合は `startSection` などの値を元に戻す。
3. フレームを確実に進めたい場合は、`EditorApplication.isPaused = true` にしてから `execute_code` 内で `BattleWorld.Step()` を必要な回数呼ぶ。`BattleWorld` は `BattleScenePresenter` の private フィールド `flow`（`BattleFlow.World`）から取れる。
   - エディタがバックグラウンドだと Unity のフレームが進まないことがある。そのときは手動で `Step()` を呼ぶ方法が確実。
   - BGM の再生位置で進むフェーズ（`BgmSamples`）は、手動で進めても BGM が進まない。Pause を解除して実時間で流して確認する。
   - ボスの HP 条件を試すときは、ボスの `BossEntityModel.TakeDamage` を呼んで HP を下げる。
4. 弾の発射を確かめるときは、`BulletSystem.ActiveCount` や弾の一覧と Prefab 名で数える。弾は `BulletSystem` が消えるかを判定してプールへ戻すので、Presenter 側で消えるのを待たない。警告線などの Spawn 物は GameObject 名で探す。
5. 終わったら Play を止める。セーブデータが変わる操作（クリアまで進めるなど）は、ユーザーの了承なしに行わない。

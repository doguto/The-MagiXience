# アセットの作り方と動作確認

## 作るアセットの種類

| 種類 | 作成メニュー（Project ウィンドウ右クリック → Create） | 置き場所 |
|---|---|---|
| `BattleStageAsset` | `Battle/Battle Stage` | `DataStore/Stage{N}/BT/Stage{N}Way_BT.asset`、`Stage{N}Boss_BT.asset` |
| `BehaviorTreeAsset` | `Battle/Behavior Tree` | `DataStore/Stage{N}/BT/Stage`（ステージ・ボス）、`Enemy`（敵）、`Attack`（攻撃） |
| `MotionPreset` | `Battle/Motion Preset` | `DataStore/Stage{N}/BT/Motion` |
| `EaseCurveAsset` | `Battle/Ease Curve` | `DataStore/Stage{N}/BT/Ease` |

命名の目安:
- ステージツリー: `Stage{N}Way_Tree` / `Stage{N}Boss_Tree`
- 敵ツリー: `S{N}_{敵の名前}`。攻撃部分は `Attack/` のツリーを `SubTree` で共有する
- 攻撃ツリー: `S{N}_{攻撃の名前}`。向きや本数だけが違うものは、別アセットにせず `ShotPattern`（NWay など）で表す

## ユーザーに操作を案内する場合（BT エディタ）

1. `Window/Battle/Behavior Tree Editor` を開く。`BehaviorTreeAsset` をダブルクリックしても開く。
2. 左のノード一覧からグラフへドラッグ＆ドロップしてノードを置く。
3. 親ノードの下側のポートから子ノードの上側のポートへドラッグして繋ぐ。Composite の子は左から順に実行される。
4. ノードを選択すると、ノード上に設定欄が出る。
5. 右ペインでツリーのパラメータを追加する。値欄の `$` から選ぶか、パラメータを値欄へドラッグすると、その値がパラメータ参照になる。
6. 配置が崩れたら Auto Layout で整える。

Inspector で `BehaviorTreeAsset` を直接触るより、エディタの方が子の順番と構造を確認しやすい。

## エージェントが Unity MCP で作る場合

`[SerializeReference]` のノードは MCP の patch 系ツールでは作れない。`execute_code` で C# を実行して組み立てる。

注意点:
- `execute_code` はメソッド本体に埋め込まれるため、`using` 文が書けない。型名は完全修飾名で書く。
- コンパイラは C# 6 相当になることがある。ローカル関数・タプル・`is not` は使わず、ラムダで代用する。
- ノード定義の多くは private な `[SerializeField]` を持つ。リフレクションのヘルパーで設定する。
- アセットの削除は `execute_code` では弾かれる（`AssetDatabase.DeleteAsset` がブロックされる）。`manage_asset` の `delete` を使う。
- 書き換えたら `EditorUtility.SetDirty` と `AssetDatabase.SaveAssets` を呼ぶ。

組み立ての例（敵ツリー: 移動を設定して、60 フレームおきに自機狙い 3way を撃つ）:

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

var tree = UnityEngine.ScriptableObject.CreateInstance<Project.Scenes.Battle.Scripts.BehaviorTree.BehaviorTreeAsset>();

var move = new Project.Scenes.Battle.Scripts.BehaviorTree.SetMovementNodeDefinition();
set(move, "preset", UnityEditor.AssetDatabase.LoadAssetAtPath<Project.Scenes.Battle.Scripts.Runtime.Motion.MotionPreset>("Assets/Project/DataStore/Stage5/BT/Motion/Straight.asset"));

var fire = new Project.Scenes.Battle.Scripts.BehaviorTree.FireNodeDefinition();
var nway = new Project.Scenes.Battle.Scripts.BehaviorTree.NWayShot();
set(nway, "wayCount", 3);
set(nway, "spreadAngle", 20f);
set(fire, "pattern", nway);
set(fire, "direction", new Project.Scenes.Battle.Scripts.BehaviorTree.AimDirection());
// set(fire, "bullet", new Project.Scenes.Battle.Scripts.BehaviorTree.BulletSpec { prefab = ..., damage = 10, motionPreset = ... });

var repeat = new Project.Scenes.Battle.Scripts.BehaviorTree.RepeatNodeDefinition
{
    Count = new Project.Scenes.Battle.Scripts.BehaviorTree.BtInt(0),
    Interval = new Project.Scenes.Battle.Scripts.BehaviorTree.BtInt(60),
};
repeat.SetChildren(new Project.Scenes.Battle.Scripts.BehaviorTree.NodeDefinition[] { fire });

var root = new Project.Scenes.Battle.Scripts.BehaviorTree.SequenceNodeDefinition();
root.SetChildren(new Project.Scenes.Battle.Scripts.BehaviorTree.NodeDefinition[] { move, repeat });
tree.Root = root;

UnityEditor.AssetDatabase.CreateAsset(tree, "Assets/Project/DataStore/Stage5/BT/Enemy/S5_Sample.asset");
UnityEditor.AssetDatabase.SaveAssets();
```

- フィールド名は `Document/Battle/BehaviorTree-Nodes.md` か、`Assets/Project/Scenes/Battle/Scripts/BehaviorTree/` のソースで確認する。推測で書かない。
- 作ったツリーは BT エディタで開き、Auto Layout で配置を整えてからユーザーに見せる。
- フォルダを作る `AssetDatabase.CreateFolder` は再インポートを起こすことがある。フォルダを先に作り、アセットは `AssetDatabase.StartAssetEditing` / `StopAssetEditing` の間でまとめて作る。
- 既存のツリーを直すときは、全体を作り直さずに対象ノードだけを差し替える。ユーザーが調整した値を残すため。

## 動作確認

1. `read_console` でコンパイルエラーがないことを確認する。`refresh_unity` のあと、型がまだ読み込まれていない場合はもう一度確認する。
2. `Tools/Battle Debug Launcher` でステージ番号と Way / Boss を選んで再生する。MCP から行う場合は、Battle シーンの `DebugRuntimeModelInitializer` の `stageNumber` / `situation` を設定して Play にする。**シーンは保存しない。** 誤って保存した場合は `situation` などの値を元に戻す。
3. フレームを確実に進めたい場合は、`EditorApplication.isPaused = true` にしてから `execute_code` 内で `BattleWorld.Step()` を必要な回数呼ぶ。`BattleWorld` は `BattleScenePresenter` の private フィールド `flow`（`BattleFlow.World`）から取れる。
   - エディタがバックグラウンドだと Unity のフレームが進まないことがある。そのときは手動で `Step()` を呼ぶ方法が確実。
   - BGM の再生位置で進むフェーズ（`BgmSamples`）は、手動で進めても BGM が進まない。Pause を解除して実時間で流して確認する。
   - ボスの HP 条件を試すときは、ボスの `BossEntityModel.TakeDamage` を呼んで HP を下げる。
4. 弾の発射を確かめるときは、`BulletSystem` の弾の一覧と Prefab 名で数える。警告線などの Spawn 物は GameObject 名で探す。
5. 終わったら Play を止める。セーブデータが変わる操作（クリアまで進めるなど）は、ユーザーの了承なしに行わない。

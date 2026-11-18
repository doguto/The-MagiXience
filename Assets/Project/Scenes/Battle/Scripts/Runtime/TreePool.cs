using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 複製したツリーを、アセットと上書きパラメータの組ごとに使い回す。
    // 複製はリフレクションを使って重く、弾のように頻繁に出るものでは GC の負担になるため。
    // 使い回せるのは、ノードが実行中の状態を OnEnter で必ず初期化し、実行をまたぐ状態は OnRecycle で戻すから。
    public sealed class TreePool
    {
        readonly Dictionary<(BehaviorTreeAsset, IReadOnlyList<TreeParameter>), Stack<Node>> idle = new();

        public Node Get(BehaviorTreeAsset asset, IReadOnlyList<TreeParameter> overrides)
        {
            if (idle.TryGetValue((asset, overrides), out var stack) && stack.Count > 0) return stack.Pop();
            return asset.Instantiate(null, overrides);
        }

        public void Return(BehaviorTreeAsset asset, IReadOnlyList<TreeParameter> overrides, Node root, BattleContext ctx)
        {
            if (root == null) return;
            root.Abort(ctx);
            root.Recycle();
            var key = (asset, overrides);
            if (!idle.TryGetValue(key, out var stack)) idle[key] = stack = new Stack<Node>();
            stack.Push(root);
        }

        public void Clear() => idle.Clear();
    }
}

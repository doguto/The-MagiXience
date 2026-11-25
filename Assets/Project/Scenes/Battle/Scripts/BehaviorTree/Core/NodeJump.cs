using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // デバッグ起動で、ルートから指定したノードまでの経路 (Node.Guid の並び) をたどって途中から始める。
    // 経路上の親は経路が続く子から始め、目的のノードに着いたら以降は普通に実行する。
    // 同じ SubTree が複数箇所で使われると Guid が重なるため、直前に着いたノードの子であることも確かめる
    public sealed class NodeJump
    {
        readonly string[] path;
        int cursor;
        Node current;

        NodeJump(string[] path) => this.path = path;

        public static NodeJump Parse(string path) => string.IsNullOrEmpty(path) ? null : new NodeJump(path.Split(','));

        internal void Arrive(Node node, BattleContext ctx)
        {
            if (current != null && !CrossesTree(current) && !Contains(current.Children, node)) return;

            if (node.Guid != path[cursor])
            {
                // Decorator の子は 1 つしかないので、違えば経路がもう合っていない
                if (current is DecoratorNode) Abandon(ctx, $"{current.GetType().Name} の子が経路と一致しません");
                return;
            }

            current = node;
            cursor++;
            if (cursor < path.Length) return;

            Debug.Log($"[NodeJump] 開始位置に着きました: {node.GetType().Name} {node.Comment}");
            ctx.Services.Jump = null;
        }

        internal int NextChild(Node parent, BattleContext ctx)
        {
            if (!ReferenceEquals(parent, current)) return -1;

            var children = parent.Children;
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i]?.Guid == path[cursor]) return i;
            }
            Abandon(ctx, $"{parent.GetType().Name} の子に経路のノードがありません");
            return -1;
        }

        // 子ではなく別のツリーのルートへ続くノード
        static bool CrossesTree(Node node) => node is SubTree or BossBattle;

        static bool Contains(IReadOnlyList<Node> children, Node node)
        {
            foreach (var child in children)
            {
                if (ReferenceEquals(child, node)) return true;
            }
            return false;
        }

        void Abandon(BattleContext ctx, string reason)
        {
            Debug.LogWarning($"[NodeJump] {reason}。ツリーが編集された可能性があるため、ここから普通に再生します");
            ctx.Services.Jump = null;
        }
    }
}

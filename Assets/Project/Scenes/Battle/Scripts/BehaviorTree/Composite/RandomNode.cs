using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("開始時に子を1つ抽選して実行する")]
    public sealed class RandomNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField, Tooltip("子の並び順に対応する重み。不足分は1として扱う")]
        List<float> weights = new();
        [SerializeField, Tooltip("直前に選んだ子を連続で選ばない")]
        bool avoidRepeat = true;

        public override string Summary => avoidRepeat ? "1つ抽選 (連続回避)" : "1つ抽選";

        public override INode Build(BuildScope scope)
        {
            var children = BuildChildren(scope);
            var w = new float[children.Length];
            for (var i = 0; i < w.Length; i++) w[i] = i < weights.Count ? Mathf.Max(0f, weights[i]) : 1f;
            return new RandomNode(children, w, avoidRepeat);
        }
    }

    // Enter 時に子を 1 つ抽選し、その子の結果を返す。
    public sealed class RandomNode : Node
    {
        readonly INode[] children;
        readonly float[] weights;
        readonly bool avoidRepeat;
        int current = -1;
        int previous = -1;

        public RandomNode(INode[] children, float[] weights, bool avoidRepeat)
        {
            this.children = children;
            this.weights = weights;
            this.avoidRepeat = avoidRepeat;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            current = Pick(ctx.Random);
            previous = current;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (current < 0) return NodeStatus.Success;
            return children[current].Tick(ctx);
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (current >= 0) children[current].Abort(ctx);
        }

        int Pick(System.Random random)
        {
            if (children.Length == 0) return -1;

            var excluded = avoidRepeat && children.Length > 1 ? previous : -1;
            var total = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                if (i != excluded) total += weights[i];
            }
            if (total <= 0f) return excluded == 0 && children.Length > 1 ? 1 : 0;

            var r = (float)random.NextDouble() * total;
            for (var i = 0; i < weights.Length; i++)
            {
                if (i == excluded) continue;
                r -= weights[i];
                if (r < 0f) return i;
            }
            return excluded == weights.Length - 1 ? weights.Length - 2 : weights.Length - 1;
        }
    }
}

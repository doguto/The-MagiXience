using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // System.Random / UnityEngine.Random とぶつからないよう RandomSelect とする
    [Serializable, NodeInfo("開始時に子を1つ抽選して実行する")]
    public sealed class RandomSelect : CompositeNode
    {
        [SerializeField, Tooltip("子の並び順に対応する重み。不足分は1として扱う")]
        List<float> weights = new();
        [SerializeField, Tooltip("直前に選んだ子を連続で選ばない")]
        bool avoidRepeat = true;

        [NonSerialized] float[] resolvedWeights;
        [NonSerialized] int current = -1;
        [NonSerialized] int previous = -1;

        public override string Summary => avoidRepeat ? "1つ抽選 (連続回避)" : "1つ抽選";

        protected override void OnSetup(TreeScope scope)
        {
            resolvedWeights = new float[Children.Count];
            for (var i = 0; i < resolvedWeights.Length; i++) resolvedWeights[i] = i < weights.Count ? Mathf.Max(0f, weights[i]) : 1f;
            current = -1;
            previous = -1;
        }

        protected override void OnRecycle()
        {
            current = -1;
            previous = -1;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            current = Pick(ctx.Random);
            previous = current;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (current < 0) return NodeStatus.Success;
            return Children[current].Tick(ctx);
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (current >= 0) Children[current].Abort(ctx);
        }

        int Pick(System.Random random)
        {
            var count = Children.Count;
            if (count == 0) return -1;

            var excluded = avoidRepeat && count > 1 ? previous : -1;
            var total = 0f;
            for (var i = 0; i < count; i++)
            {
                if (i != excluded) total += resolvedWeights[i];
            }
            if (total <= 0f) return excluded == 0 && count > 1 ? 1 : 0;

            var r = (float)random.NextDouble() * total;
            for (var i = 0; i < count; i++)
            {
                if (i == excluded) continue;
                r -= resolvedWeights[i];
                if (r < 0f) return i;
            }
            return excluded == count - 1 ? count - 2 : count - 1;
        }
    }
}

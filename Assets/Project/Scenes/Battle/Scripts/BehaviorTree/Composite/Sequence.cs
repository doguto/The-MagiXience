using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 子が完了したフレームのうちに次の子へ進む
    [Serializable, NodeInfo("子を左から順に実行する。子が Failure なら中断して Failure")]
    public sealed class Sequence : CompositeNode
    {
        [NonSerialized] int index;

        protected override void OnEnter(BattleContext ctx)
        {
            index = Math.Max(0, JumpTarget(ctx));
            for (var i = 0; i < index; i++) Children[i].FastForward(ctx);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (index < Children.Count)
            {
                var status = Children[index].Tick(ctx);
                if (status != NodeStatus.Success) return status;
                index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (index < Children.Count) Children[index].Abort(ctx);
        }
    }
}

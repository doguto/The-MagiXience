using System;
using System.Linq;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- ステージの進行 ----------

    [Serializable, NodeInfo("ステージツリーの一番上に置く。子の Section を順に実行し、リトライ時は記録した Section から始める", "Flow")]
    public sealed class Stage : CompositeNode
    {
        [NonSerialized] int index;

        public override string Summary => $"{Children.Count} Section";

        protected override void OnSetup(TreeScope scope)
        {
            foreach (var child in Children)
            {
                if (child is not Section) Debug.LogWarning($"[BehaviorTree] Stage の子は Section にしてください: {child.GetType().Name}");
            }
        }

        static BattleSituation SituationOf(Node child) => child is Section section ? section.Situation : BattleSituation.Way;

        protected override void OnEnter(BattleContext ctx)
        {
            var progress = ctx.Services.Progress;
            var jump = JumpTarget(ctx);
            index = jump >= 0 ? jump : Mathf.Clamp(progress.StartSection, 0, Mathf.Max(0, Children.Count - 1));
            if (index < Children.Count) progress.Enter(index, SituationOf(Children[index]), progress.ResumeStart || jump > 0);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (index < Children.Count)
            {
                var status = Children[index].Tick(ctx);
                if (status != NodeStatus.Success) return status;
                index++;
                if (index < Children.Count) ctx.Services.Progress.Enter(index, SituationOf(Children[index]), false);
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (index < Children.Count) Children[index].Abort(ctx);
        }
    }
}

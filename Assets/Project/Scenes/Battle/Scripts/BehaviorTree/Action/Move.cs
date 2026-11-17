using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵を動かす。移動が終わると完了 (無限の移動は完了しない)", "Action/Move")]
    public sealed class Move : ActionNode
    {
        [SerializeField, Tooltip("指定するとインラインより優先")] MotionPreset preset;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値の MotionPreset を使う (空なら移動なしで完了)")] string presetParam;
        [SerializeReference, SubclassSelector] List<MotionStep> steps = new();

        [NonSerialized] IReadOnlyList<MotionStep> resolvedSteps;
        [NonSerialized] MotionSequence sequence;

        public override string Summary
        {
            get
            {
                if (!string.IsNullOrEmpty(presetParam)) return "$" + presetParam;
                if (preset != null) return preset.name;
                if (steps.Count == 1) return steps[0]?.Summary;
                return steps.Count == 0 ? "(移動なし)" : $"{steps.Count} ステップ";
            }
        }

        protected override void OnSetup(TreeScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, presetParam, preset);
            resolvedSteps = resolved != null ? resolved.Steps : string.IsNullOrEmpty(presetParam) ? steps : null;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            if (ctx.Self == null) return;
            sequence = new MotionSequence(resolvedSteps, ctx.Self.Body, new MotionLaunch { AttackTime = ctx.Self.AttackTime });
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (sequence == null) return NodeStatus.Failure;
            sequence.Tick(BattleWorld.DeltaTime);
            return sequence.IsFinished ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnExit(BattleContext ctx)
        {
            sequence?.Stop();
            sequence = null;
        }
    }
}

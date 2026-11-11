using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public interface ICondition
    {
        // 評価の起点となるタイミング(Until の開始時など)で呼ばれる
        void Reset(BattleContext ctx);
        bool Evaluate(BattleContext ctx);
    }

    [Serializable]
    public abstract class ConditionDefinition
    {
        public abstract ICondition Build(BuildScope scope);
    }

    public sealed class ConstantCondition : ICondition
    {
        readonly bool value;
        public ConstantCondition(bool value) => this.value = value;
        public void Reset(BattleContext ctx) { }
        public bool Evaluate(BattleContext ctx) => value;
    }

    public sealed class DelegateCondition : ICondition
    {
        readonly Func<BattleContext, bool> evaluate;
        readonly Action<BattleContext> reset;

        public DelegateCondition(Func<BattleContext, bool> evaluate, Action<BattleContext> reset = null)
        {
            this.evaluate = evaluate;
            this.reset = reset;
        }

        public void Reset(BattleContext ctx) => reset?.Invoke(ctx);
        public bool Evaluate(BattleContext ctx) => evaluate(ctx);
    }

    [Serializable]
    public sealed class FrameElapsedConditionDefinition : ConditionDefinition
    {
        [SerializeField, Tooltip("Reset からの経過フレーム数")] BtInt frames;

        public override ICondition Build(BuildScope scope)
        {
            var target = frames.Resolve(scope);
            var startFrame = 0;
            return new DelegateCondition(ctx => ctx.Frame - startFrame >= target, ctx => startFrame = ctx.Frame);
        }
    }

    [Serializable]
    public sealed class BgmReachedConditionDefinition : ConditionDefinition
    {
        [SerializeField, Tooltip("BGM の再生位置(秒)。ループを含む累計時刻で判定する")] BtFloat seconds;

        public override ICondition Build(BuildScope scope)
        {
            var target = seconds.Resolve(scope);
            return new DelegateCondition(ctx => ctx.BgmTime >= target);
        }
    }

    [Serializable]
    public sealed class AndConditionDefinition : ConditionDefinition
    {
        [SerializeReference, SubclassSelector] List<ConditionDefinition> conditions = new();

        public override ICondition Build(BuildScope scope) => new CompositeCondition(BuildAll(conditions, scope), true);

        internal static ICondition[] BuildAll(List<ConditionDefinition> defs, BuildScope scope)
        {
            var list = new List<ICondition>();
            foreach (var d in defs)
            {
                if (d != null) list.Add(d.Build(scope));
            }
            return list.ToArray();
        }
    }

    [Serializable]
    public sealed class OrConditionDefinition : ConditionDefinition
    {
        [SerializeReference, SubclassSelector] List<ConditionDefinition> conditions = new();

        public override ICondition Build(BuildScope scope) => new CompositeCondition(AndConditionDefinition.BuildAll(conditions, scope), false);
    }

    [Serializable]
    public sealed class NotConditionDefinition : ConditionDefinition
    {
        [SerializeReference, SubclassSelector] ConditionDefinition condition;

        public override ICondition Build(BuildScope scope)
        {
            var inner = condition?.Build(scope) ?? new ConstantCondition(false);
            return new DelegateCondition(ctx => !inner.Evaluate(ctx), inner.Reset);
        }
    }

    public sealed class CompositeCondition : ICondition
    {
        readonly ICondition[] conditions;
        readonly bool requireAll;

        public CompositeCondition(ICondition[] conditions, bool requireAll)
        {
            this.conditions = conditions;
            this.requireAll = requireAll;
        }

        public void Reset(BattleContext ctx)
        {
            foreach (var c in conditions) c.Reset(ctx);
        }

        public bool Evaluate(BattleContext ctx)
        {
            if (conditions.Length == 0) return requireAll;
            foreach (var c in conditions)
            {
                if (c.Evaluate(ctx) != requireAll) return !requireAll;
            }
            return requireAll;
        }
    }

    [Serializable]
    public sealed class CheckConditionNodeDefinition : ActionNodeDefinition
    {
        [SerializeReference, SubclassSelector] ConditionDefinition condition;

        public override INode Build(BuildScope scope)
        {
            var cond = condition?.Build(scope) ?? new ConstantCondition(true);
            return new ActionNode(ctx => cond.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Failure, cond.Reset);
        }
    }

    [Serializable]
    public sealed class WaitUntilNodeDefinition : ActionNodeDefinition
    {
        [SerializeReference, SubclassSelector] ConditionDefinition condition;

        public override INode Build(BuildScope scope)
        {
            var cond = condition?.Build(scope) ?? new ConstantCondition(true);
            return new ActionNode(ctx => cond.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running, cond.Reset);
        }
    }
}

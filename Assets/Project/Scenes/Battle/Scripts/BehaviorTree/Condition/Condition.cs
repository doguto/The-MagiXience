using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // Until などが毎フレーム判定する条件。ノードと同じく実行するツリーごとに複製される
    [Serializable]
    public abstract class Condition
    {
        public virtual string Summary => GetType().Name;

        internal void Setup(TreeScope scope) => OnSetup(scope);
        protected virtual void OnSetup(TreeScope scope) { }

        // 評価の起点となるタイミング (Until の開始時など) で呼ばれる
        public virtual void Reset(BattleContext ctx) { }
        public abstract bool Evaluate(BattleContext ctx);
    }

    [Serializable]
    public sealed class Constant : Condition
    {
        [SerializeField] bool value;

        public override string Summary => value ? "常に成立" : "成立しない";
        public override bool Evaluate(BattleContext ctx) => value;
    }

    [Serializable]
    public sealed class FrameElapsed : Condition
    {
        [SerializeField, Tooltip("Reset からの経過フレーム数")] BtInt frames;

        [NonSerialized] int target;
        [NonSerialized] int startFrame;

        public override string Summary => "経過 " + frames.ToFrameString();

        protected override void OnSetup(TreeScope scope) => target = frames.Resolve(scope);
        public override void Reset(BattleContext ctx) => startFrame = ctx.Frame;
        public override bool Evaluate(BattleContext ctx) => ctx.Frame - startFrame >= target;
    }

    [Serializable]
    public sealed class BgmReached : Condition
    {
        [SerializeField, Tooltip("BGM の再生位置(秒)。ループを含む累計時刻で判定する")] BtFloat seconds;

        [NonSerialized] float target;

        public override string Summary => $"BGM {seconds}s 到達";

        protected override void OnSetup(TreeScope scope) => target = seconds.Resolve(scope);
        public override bool Evaluate(BattleContext ctx) => ctx.BgmTime >= target;
    }

    [Serializable]
    public sealed class And : Condition
    {
        [SerializeReference, SubclassSelector] List<Condition> conditions = new();

        public override string Summary => Conditions.Join(conditions, " かつ ");

        protected override void OnSetup(TreeScope scope) => Conditions.Setup(conditions, scope);
        public override void Reset(BattleContext ctx) => Conditions.Reset(conditions, ctx);

        public override bool Evaluate(BattleContext ctx)
        {
            foreach (var c in conditions)
            {
                if (c != null && !c.Evaluate(ctx)) return false;
            }
            return true;
        }
    }

    [Serializable]
    public sealed class Or : Condition
    {
        [SerializeReference, SubclassSelector] List<Condition> conditions = new();

        public override string Summary => Conditions.Join(conditions, " または ");

        protected override void OnSetup(TreeScope scope) => Conditions.Setup(conditions, scope);
        public override void Reset(BattleContext ctx) => Conditions.Reset(conditions, ctx);

        public override bool Evaluate(BattleContext ctx)
        {
            foreach (var c in conditions)
            {
                if (c != null && c.Evaluate(ctx)) return true;
            }
            return false;
        }
    }

    [Serializable]
    public sealed class Not : Condition
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public override string Summary => "NOT " + (condition?.Summary ?? "?");

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);
        public override void Reset(BattleContext ctx) => condition?.Reset(ctx);
        public override bool Evaluate(BattleContext ctx) => condition == null || !condition.Evaluate(ctx);
    }

    static class Conditions
    {
        public static string Join(List<Condition> conditions, string separator)
        {
            var parts = new List<string>();
            foreach (var c in conditions) parts.Add(c?.Summary ?? "?");
            return parts.Count == 0 ? "(空)" : "(" + string.Join(separator, parts) + ")";
        }

        public static void Setup(List<Condition> conditions, TreeScope scope)
        {
            foreach (var c in conditions) c?.Setup(scope);
        }

        public static void Reset(List<Condition> conditions, BattleContext ctx)
        {
            foreach (var c in conditions) c?.Reset(ctx);
        }
    }

    [Serializable, NodeInfo("条件を判定し、成立なら Success・不成立なら Failure")]
    public sealed class CheckCondition : ActionNode
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public override string Summary => "判定: " + (condition?.Summary ?? "(未設定)");

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);
        protected override void OnEnter(BattleContext ctx) => condition?.Reset(ctx);
        protected override NodeStatus OnTick(BattleContext ctx) => condition == null || condition.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Failure;
    }

    [Serializable, NodeInfo("条件が成立するまで待機する")]
    public sealed class WaitUntil : ActionNode
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public override string Summary => "待機: " + (condition?.Summary ?? "(未設定)");

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);
        protected override void OnEnter(BattleContext ctx) => condition?.Reset(ctx);
        protected override NodeStatus OnTick(BattleContext ctx) => condition == null || condition.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running;
    }
}

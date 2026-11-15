using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public sealed class ActionNode : Node
    {
        readonly Func<BattleContext, NodeStatus> tick;
        readonly Action<BattleContext> enter;
        readonly Action<BattleContext> exit;

        public ActionNode(Func<BattleContext, NodeStatus> tick, Action<BattleContext> enter = null, Action<BattleContext> exit = null)
        {
            this.tick = tick;
            this.enter = enter;
            this.exit = exit;
        }

        protected override void OnEnter(BattleContext ctx) => enter?.Invoke(ctx);
        protected override NodeStatus OnTick(BattleContext ctx) => tick(ctx);
        protected override void OnExit(BattleContext ctx) => exit?.Invoke(ctx);
    }

    [Serializable, NodeInfo("指定フレーム待機する (60f = 1秒)")]
    public sealed class WaitNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("待機フレーム数(60 = 1秒)")] BtInt frames;

        public BtInt Frames
        {
            get => frames;
            set => frames = value;
        }

        public override INode Build(BuildScope scope) => new WaitNode(Mathf.Max(0, frames.Resolve(scope)));

        public override string Summary => frames.ToFrameString();
    }

    // 開始フレーム f から数えて f + frames のフレームで Success。0 なら即 Success。
    public sealed class WaitNode : Node
    {
        readonly int frames;
        int elapsed;

        public WaitNode(int frames) => this.frames = frames;

        protected override void OnEnter(BattleContext ctx) => elapsed = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (elapsed >= frames) return NodeStatus.Success;
            elapsed++;
            return NodeStatus.Running;
        }
    }

    [Serializable, NodeInfo("コンソールにログを出力する (デバッグ用)")]
    public sealed class LogNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] string message;

        public override string Summary => message;

        public override INode Build(BuildScope scope)
        {
            var text = message;
            return new ActionNode(ctx =>
            {
                Debug.Log($"[BehaviorTree] frame={ctx.Frame} {text}");
                return NodeStatus.Success;
            });
        }
    }

    [Serializable, NodeInfo("別の BehaviorTree を実行する。パラメータを上書きできる")]
    public sealed class SubTreeNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを実行する (値が空なら何もしない)")] string treeParam;
        [SerializeField] List<TreeParameter> overrides = new();

        public BehaviorTreeAsset Tree
        {
            get => tree;
            set => tree = value;
        }

        public string TreeParam
        {
            get => treeParam;
            set => treeParam = value;
        }

        public List<TreeParameter> Overrides => overrides;

        public override string Summary =>
            (!string.IsNullOrEmpty(treeParam) ? "$" + treeParam : tree ? tree.name : "(未設定)") + (overrides.Count > 0 ? $"  ({overrides.Count} override)" : "");

        public override INode Build(BuildScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, treeParam, tree);
            if (resolved == null)
            {
                if (string.IsNullOrEmpty(treeParam)) Debug.LogWarning("[BehaviorTree] SubTree asset is not assigned.");
                return new SucceedNode();
            }
            return resolved.Build(scope, overrides);
        }
    }

    // ノードの「〜Param」欄 (パラメータ名) から Object パラメータの値を引く。名前が空なら固定値を使う
    public static class ParamObjects
    {
        public static T Resolve<T>(BuildScope scope, string param, T fallback) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(param)) return fallback;
            if (scope != null && scope.TryGet(param, out var p)) return p.ObjectValue as T;
            Debug.LogWarning($"[BehaviorTree] パラメータ '{param}' が見つかりません");
            return fallback;
        }
    }
}

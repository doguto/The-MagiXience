using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 開始フレーム f から数えて f + frames のフレームで Success。0 なら即 Success
    [Serializable, NodeInfo("指定フレーム待機する (60f = 1秒)")]
    public sealed class Wait : ActionNode
    {
        [SerializeField, Tooltip("待機フレーム数(60 = 1秒)")] BtInt frames;

        [NonSerialized] int target;
        [NonSerialized] int elapsed;

        public BtInt Frames
        {
            get => frames;
            set => frames = value;
        }

        public override string Summary => frames.ToFrameString();

        protected override void OnSetup(TreeScope scope) => target = Mathf.Max(0, frames.Resolve(scope));
        protected override void OnEnter(BattleContext ctx) => elapsed = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (elapsed >= target) return NodeStatus.Success;
            elapsed++;
            return NodeStatus.Running;
        }
    }

    [Serializable, NodeInfo("コンソールにログを出力する (デバッグ用)")]
    public sealed class Log : ActionNode
    {
        [SerializeField] string message;

        public override string Summary => message;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            Debug.Log($"[BehaviorTree] frame={ctx.Frame} {message}");
            return NodeStatus.Success;
        }
    }

    [Serializable, NodeInfo("別の BehaviorTree を実行する。パラメータを上書きできる")]
    public sealed class SubTree : ActionNode
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを実行する (値が空なら何もしない)")] string treeParam;
        [SerializeField] List<TreeParameter> overrides = new();

        [NonSerialized] Node root;

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

        protected override void OnSetup(TreeScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, treeParam, tree);
            if (resolved == null && string.IsNullOrEmpty(treeParam)) Debug.LogWarning("[BehaviorTree] SubTree asset is not assigned.");
            root = resolved != null ? resolved.Instantiate(scope, overrides) : null;
        }

        protected override NodeStatus OnTick(BattleContext ctx) => root?.Tick(ctx) ?? NodeStatus.Success;
        protected override void OnAbort(BattleContext ctx) => root?.Abort(ctx);
    }

    // ノードの「〜Param」欄 (パラメータ名) から Object パラメータの値を引く。名前が空なら固定値を使う
    public static class ParamObjects
    {
        public static T Resolve<T>(TreeScope scope, string param, T fallback) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(param)) return fallback;
            if (scope != null && scope.TryGet(param, out var p)) return p.ObjectValue as T;
            Debug.LogWarning($"[BehaviorTree] パラメータ '{param}' が見つかりません");
            return fallback;
        }
    }
}

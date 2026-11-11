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

    [Serializable]
    public sealed class WaitNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("待機フレーム数(60 = 1秒)")] BtInt frames;

        public BtInt Frames
        {
            get => frames;
            set => frames = value;
        }

        public override INode Build(BuildScope scope) => new WaitNode(Mathf.Max(0, frames.Resolve(scope)));
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

    [Serializable]
    public sealed class LogNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] string message;

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

    [Serializable]
    public sealed class SubTreeNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField] List<TreeParameter> overrides = new();

        public BehaviorTreeAsset Tree
        {
            get => tree;
            set => tree = value;
        }

        public List<TreeParameter> Overrides => overrides;

        public override INode Build(BuildScope scope)
        {
            if (tree == null)
            {
                Debug.LogWarning("[BehaviorTree] SubTree asset is not assigned.");
                return new SucceedNode();
            }
            return tree.Build(scope, overrides);
        }
    }
}

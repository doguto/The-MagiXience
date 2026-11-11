using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 1 本のツリーを固定フレームで駆動する。ルートが完了したら停止する。
    public sealed class BehaviorTreeRunner
    {
        readonly INode root;
        readonly BattleContext context;

        public BehaviorTreeRunner(BehaviorTreeAsset asset, BattleContext context)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            this.context = context;
            root = asset.Build();
            Name = asset.name;
        }

        public string Name { get; }
        public NodeStatus? Result { get; private set; }
        public bool IsFinished => Result.HasValue;
        public event Action<NodeStatus> Finished;

        public void Tick()
        {
            if (IsFinished) return;

            NodeStatus status;
            try
            {
                status = root.Tick(context);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                status = NodeStatus.Failure;
            }

            if (status == NodeStatus.Running) return;
            Result = status;
            Finished?.Invoke(status);
        }

        public void Stop()
        {
            if (IsFinished) return;
            root.Abort(context);
            Result = NodeStatus.Failure;
        }
    }
}

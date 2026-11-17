using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 1 本のツリーを固定フレームで駆動する。ルートが完了したら停止する。
    public sealed class BehaviorTreeRunner
    {
        readonly Node root;
        readonly BattleContext context;
        readonly AsyncSubject<NodeStatus> finished = new();

        public BehaviorTreeRunner(BehaviorTreeAsset asset, BattleContext context, IReadOnlyList<TreeParameter> overrides = null)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            this.context = context;
            root = asset.Instantiate(null, overrides);
            Name = asset.name;
        }

        public string Name { get; }
        public BattleContext Context => context;
        public NodeStatus? Result { get; private set; }
        public bool IsFinished => Result.HasValue;
        // 完了したときだけ値を流す。Stop で打ち切った場合は値なしで完了する
        public IObservable<NodeStatus> OnFinished => finished;

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
            finished.OnNext(status);
            finished.OnCompleted();
        }

        public void Stop()
        {
            if (IsFinished) return;
            root.Abort(context);
            Result = NodeStatus.Failure;
            finished.OnCompleted();
        }
    }
}

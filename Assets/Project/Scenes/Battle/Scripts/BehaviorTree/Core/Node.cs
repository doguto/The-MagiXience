namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum NodeStatus
    {
        Running,
        Success,
        Failure,
    }

    public interface INode
    {
        bool IsActive { get; }
        NodeStatus Tick(BattleContext ctx);
        void Abort(BattleContext ctx);
    }

    // Enter/Exit の呼び出しを保証する実行時ノードの基底。Tick は 1 フレーム 1 回。
    public abstract class Node : INode
    {
        public bool IsActive { get; private set; }

        public NodeStatus Tick(BattleContext ctx)
        {
            if (!IsActive)
            {
                IsActive = true;
                OnEnter(ctx);
            }

            var status = OnTick(ctx);
            if (status != NodeStatus.Running)
            {
                IsActive = false;
                OnExit(ctx);
            }

            return status;
        }

        public void Abort(BattleContext ctx)
        {
            if (!IsActive) return;
            IsActive = false;
            OnAbort(ctx);
            OnExit(ctx);
        }

        protected virtual void OnEnter(BattleContext ctx) { }
        protected abstract NodeStatus OnTick(BattleContext ctx);
        protected virtual void OnAbort(BattleContext ctx) { }
        protected virtual void OnExit(BattleContext ctx) { }
    }
}

using System;

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
}

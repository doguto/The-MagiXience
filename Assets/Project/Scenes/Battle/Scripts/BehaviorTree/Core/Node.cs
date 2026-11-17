using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ノード 1 種類 = 1 クラス。設定はアセットに保存し、実行中の状態は [NonSerialized] に持つ。
    // アセット上のノードは雛形で、実行するツリーごとに NodeCloner で複製し、Setup でパラメータを解決してから Tick する。
    [Serializable]
    public abstract class Node
    {
        [SerializeField, HideInInspector] string guid = System.Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] Vector2 graphPosition;
        [SerializeField] string comment;

        [NonSerialized] bool active;
        [NonSerialized] TreeScope scope;

        public string Guid => guid;
        public string Comment => comment;

        public Vector2 GraphPosition
        {
            get => graphPosition;
            set => graphPosition = value;
        }

        public bool IsActive => active;
        protected TreeScope Scope => scope;

        // エディタのノード上に表示する一行要約
        public virtual string Summary => null;

        public virtual NodeChildCapacity ChildCapacity => NodeChildCapacity.None;
        public virtual IReadOnlyList<Node> Children => Array.Empty<Node>();
        public virtual void SetChildren(IList<Node> children) { }

        public void RegenerateGuid() => guid = System.Guid.NewGuid().ToString("N");

        internal void Setup(TreeScope treeScope)
        {
            scope = treeScope;
            PrepareChildren();
            OnSetup(treeScope);
            foreach (var child in Children) child?.Setup(treeScope);
        }

        // 複製後に 1 回だけ呼ばれる。パラメータの解決など、実行のたびに変わらない準備をする
        protected virtual void OnSetup(TreeScope treeScope) { }

        private protected virtual void PrepareChildren() { }

        public NodeStatus Tick(BattleContext ctx)
        {
#if UNITY_EDITOR
            BehaviorTreeDebug.MarkTicked(guid);
#endif
            if (!active)
            {
                active = true;
                OnEnter(ctx);
            }

            var status = OnTick(ctx);
            if (status != NodeStatus.Running)
            {
                active = false;
                OnExit(ctx);
            }

            return status;
        }

        public void Abort(BattleContext ctx)
        {
            if (!active) return;
            active = false;
            OnAbort(ctx);
            OnExit(ctx);
        }

        protected virtual void OnEnter(BattleContext ctx) { }
        protected abstract NodeStatus OnTick(BattleContext ctx);
        protected virtual void OnAbort(BattleContext ctx) { }
        protected virtual void OnExit(BattleContext ctx) { }
    }
}

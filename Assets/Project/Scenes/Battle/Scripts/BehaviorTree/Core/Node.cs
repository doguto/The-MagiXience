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
                ctx.Services.Jump?.Arrive(this, ctx);
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

        // TreePool に戻すとき (別の敵・弾で使い回す前) に呼ぶ
        internal void Recycle()
        {
            OnRecycle();
            foreach (var child in Children) child?.Recycle();
        }

        // 実行をまたいで持ち続ける状態 (前回の抽選結果など) だけを戻す。実行中の状態は OnEnter で初期化する
        protected virtual void OnRecycle() { }

        // デバッグ起動で途中から始めるとき、前にあって飛ばすノードに呼ばれる。実行はせず、後のノードが前提にする外部の状態 (BGM の位置・ボスなど) だけを進める
        public virtual void FastForward(BattleContext ctx)
        {
            foreach (var child in Children) child?.FastForward(ctx);
        }

        // デバッグ起動の経路上にいれば、経路が続く子の番号。いなければ -1
        protected int JumpTarget(BattleContext ctx) => ctx.Services.Jump?.NextChild(this, ctx) ?? -1;

        protected virtual void OnEnter(BattleContext ctx) { }
        protected abstract NodeStatus OnTick(BattleContext ctx);
        protected virtual void OnAbort(BattleContext ctx) { }
        protected virtual void OnExit(BattleContext ctx) { }
    }
}

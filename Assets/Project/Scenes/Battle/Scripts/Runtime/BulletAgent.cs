using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 弾 1 発。BulletSystem が出し入れし、弾のツリーの Self になる。
    // Body (Position) は外力を足す前の本来の軌道で、表示位置は BulletSystem が外力を足して決める
    public sealed class BulletAgent : IAgent, IMotionBody, IInvincibleBody
    {
        internal BulletSystem System;
        internal BulletEntityPresenter Presenter;
        internal BehaviorTreeAsset TreeAsset;
        internal IReadOnlyList<TreeParameter> Overrides;
        internal Node Tree;
        internal BattleContext Context;
        internal bool TreeFinished;
        internal int LastTickFrame;
        internal int DelayFrames;
        internal int LifetimeFrames;
        internal int ElapsedFrames;
        internal Vector3 TravelOrigin;
        internal Vector2 TravelAxis;
        internal float TravelRange;
        internal bool Force;
        internal Vector2 ForceVelocity;
        internal Vector2 ForceOffset;
        internal Vector3 PreviousPosition;
        internal DespawnReason PendingReason;
        internal DespawnBehavior DespawnBehavior;
        bool invincible;

        public BattleWorld World => System.World;
        public IMotionBody Body => this;
        public IReadOnlyList<MotionStep> Motion { get; internal set; }
        public Vector2 Direction { get; internal set; }
        // 撃った時点の撃ち手の時刻。発射時刻で変わる移動 (頂点の揺らぎなど) と、この弾から撃つ子弾に引き継ぐ
        public float AttackTime { get; internal set; }
        public bool IsAlive => Presenter != null && PendingReason == DespawnReason.None;
        public int Serial { get; internal set; }
        public IAgent Owner { get; internal set; }
        public int OwnerSerial { get; internal set; }
        public string Group { get; internal set; }
        public SignalInbox Signals { get; } = new();

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Transform Transform => Presenter.transform;

        public bool Invincible
        {
            set
            {
                if (invincible == value || Presenter == null) return;
                invincible = value;
                Presenter.Model.SetInvincible(value);
            }
        }

        public void Despawn() => System.MarkDespawn(this, DespawnReason.Despawned);

        public void SetDespawnBehavior(BehaviorTreeAsset tree, DespawnReason reasons) =>
            DespawnBehavior = new DespawnBehavior { Tree = tree, Reasons = reasons };

        internal void ResetInvincible() => invincible = false;
    }
}

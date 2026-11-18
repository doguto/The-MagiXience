using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 弾を固定フレームで動かす。弾ごとにツリーを Tick し、外力を足して位置を反映し、消えるかを判定する。
    // 弾の見た目と当たり判定は BulletEntityPresenter が担う。
    public sealed class BulletSystem
    {
        // 出た瞬間に弾を出す弾の連鎖が止まらないときの上限
        const int MaxSpawnRounds = 8;

        static readonly MotionStep[] DefaultMotion = { new VelocityMotion() };

        readonly List<BulletAgent> bullets = new();
        readonly Stack<BulletAgent> free = new();
        readonly Stack<MoveSelf> defaultTrees = new();
        readonly List<IBulletForce> forces = new();

        public BulletSystem(BattleWorld world) => World = world;

        public BattleWorld World { get; }
        public int ActiveCount => bullets.Count;

        public void AddForce(IBulletForce force)
        {
            if (force != null && !forces.Contains(force)) forces.Add(force);
        }

        public void RemoveForce(IBulletForce force) => forces.Remove(force);

        public BulletEntityPresenter Spawn(in BulletSpawn spawn)
        {
            if (spawn.Prefab == null)
            {
                Debug.LogWarning("[BulletSystem] Bullet prefab is not assigned.");
                return null;
            }

            var pool = BulletPool.GetOrCreateSharedPool(spawn.Prefab);
            var presenter = pool.Get();
            var rotation = presenter.InitializeDriven(spawn.Damage, spawn.Position, pool, spawn.Rotation, spawn.IsPlayerChargeBullet);

            var b = free.Count > 0 ? free.Pop() : new BulletAgent();
            b.System = this;
            b.Presenter = presenter;
            b.Position = spawn.Position;
            b.PreviousPosition = spawn.Position;
            b.Rotation = rotation;
            b.Motion = spawn.Motion != null && spawn.Motion.Count > 0 ? spawn.Motion : DefaultMotion;
            b.Direction = spawn.Direction;
            b.AttackTime = spawn.AttackTime;
            b.DelayFrames = Mathf.Max(0, spawn.StartDelayFrames);
            b.LifetimeFrames = Mathf.Max(0, spawn.LifetimeFrames);
            b.ElapsedFrames = 0;
            b.TravelOrigin = spawn.Position;
            b.TravelAxis = spawn.Direction.normalized;
            b.TravelRange = b.TravelAxis == Vector2.zero ? 0f : spawn.Range;
            b.Force = spawn.AffectedByForce && AnyActiveForce();
            b.ForceVelocity = b.ForceOffset = Vector2.zero;
            b.PendingReason = DespawnReason.None;
            b.DespawnBehavior = default;
            b.LastTickFrame = -1;
            b.ResetInvincible();
            b.Context ??= new BattleContext(World, b);
            b.TreeAsset = spawn.Tree;
            b.Overrides = spawn.Overrides;
            b.Tree = spawn.Tree != null ? World.Trees.Get(spawn.Tree, spawn.Overrides) : TakeDefaultTree();
            b.TreeFinished = false;
            b.Serial = World.NextSerial();
            b.Owner = spawn.Owner;
            b.OwnerSerial = spawn.Owner?.Serial ?? 0;
            b.Group = spawn.Group;
            b.Signals.Clear();
            bullets.Add(b);
            return presenter;
        }

        // ツリー未指定の弾は「渡された移動を実行するだけ」のツリーで動かす
        MoveSelf TakeDefaultTree()
        {
            if (defaultTrees.Count > 0) return defaultTrees.Pop();
            var tree = new MoveSelf();
            tree.Setup(null);
            return tree;
        }

        bool AnyActiveForce()
        {
            foreach (var force in forces)
            {
                if (force.Active) return true;
            }
            return false;
        }

        // 1 フレーム分進める。途中で出た弾も、出たフレームのうちに 1 回動かす
        public void Tick(float dt) => Run(dt, DespawnReason.None);

        // Tick の後に出た弾 (敵が消えるときのツリーが撃った弾など) を、出たフレームのうちに動かす
        public void Flush(float dt) => Run(dt, DespawnReason.None);

        public void Clear() => Run(0f, DespawnReason.Cleared);

        internal void Deliver(in Signal signal, int frame)
        {
            foreach (var b in bullets)
            {
                if (b.IsAlive && signal.Reaches(b)) b.Signals.Add(signal.Key, frame);
            }
        }

        internal void MarkDespawn(BulletAgent b, DespawnReason reason)
        {
            if (b.IsAlive) b.PendingReason = reason;
        }

        void Run(float dt, DespawnReason clearReason)
        {
            var frame = World.Frame;
            var start = 0;
            for (var round = 0; ; round++)
            {
                for (var i = start; i < bullets.Count; i++)
                {
                    var b = bullets[i];
                    if (clearReason != DespawnReason.None) MarkDespawn(b, clearReason);
                    else Step(b, frame, dt);
                }
                start = bullets.Count;
                CompleteDespawns();
                if (bullets.Count == start) break;
                if (round + 1 >= MaxSpawnRounds)
                {
                    Debug.LogWarning("[BulletSystem] 消えるときに弾を出す連鎖が止まらないため、残りは次のフレームに回します。");
                    break;
                }
            }
            Compact();
        }

        void Step(BulletAgent b, int frame, float dt)
        {
            if (!b.IsAlive || b.LastTickFrame == frame) return;
            b.LastTickFrame = frame;

            if (b.Presenter.IsConsumed)
            {
                MarkDespawn(b, DespawnReason.Defeated);
                return;
            }

            b.ElapsedFrames++;
            if (b.DelayFrames > 0)
            {
                b.DelayFrames--;
            }
            else
            {
                if (!b.TreeFinished) TickTree(b);
                if (!b.IsAlive) return;
                ApplyPosition(b, dt);
            }

            if (b.LifetimeFrames > 0 && b.ElapsedFrames >= b.LifetimeFrames) MarkDespawn(b, DespawnReason.Expired);
            else if (HasReachedRange(b)) MarkDespawn(b, DespawnReason.Expired);
            else if (IsOutOfScreen(b)) MarkDespawn(b, DespawnReason.OutOfScreen);
        }

        static void TickTree(BulletAgent b)
        {
            NodeStatus status;
            try
            {
                status = b.Tree.Tick(b.Context);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                status = NodeStatus.Failure;
            }
            if (status != NodeStatus.Running) b.TreeFinished = true;
        }

        void ApplyPosition(BulletAgent b, float dt)
        {
            var position = b.Position;
            if (b.Force && forces.Count > 0 && dt > 0f)
            {
                var authoredVelocity = (Vector2)(position - b.PreviousPosition) / dt;
                foreach (var force in forces) force.Integrate(authoredVelocity, dt, ref b.ForceVelocity, ref b.ForceOffset);
                position += (Vector3)(b.ForceOffset * b.Presenter.ForceInfluence);
            }
            b.PreviousPosition = b.Position;
            b.Presenter.transform.SetPositionAndRotation(position, b.Rotation);
        }

        // 蛇行する移動でも終点が揃うよう、起点からの直線距離ではなく発射方向への進み具合で判定する
        static bool HasReachedRange(BulletAgent b)
        {
            if (b.TravelRange <= 0f) return false;
            return Vector2.Dot((Vector2)(b.Presenter.transform.position - b.TravelOrigin), b.TravelAxis) >= b.TravelRange;
        }

        static bool IsOutOfScreen(BulletAgent b)
        {
            var position = b.Presenter.transform.position;
            var margin = b.Presenter.ScreenMargin;
            return position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                   position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;
        }

        // 消える弾の、消えるときのツリーを実行してから片付ける。ツリーが撃った弾はリストの後ろに足される
        void CompleteDespawns()
        {
            for (var i = 0; i < bullets.Count; i++)
            {
                var b = bullets[i];
                if (b.Presenter == null || b.PendingReason == DespawnReason.None) continue;
                b.DespawnBehavior.Run(b, b.PendingReason);
                Release(b);
            }
        }

        void Release(BulletAgent b)
        {
            World.CancelScheduled(b);
            if (b.TreeAsset != null) World.Trees.Return(b.TreeAsset, b.Overrides, b.Tree, b.Context);
            else if (b.Tree is MoveSelf defaultTree)
            {
                defaultTree.Abort(b.Context);
                defaultTrees.Push(defaultTree);
            }
            b.Tree = null;
            b.TreeAsset = null;
            b.Overrides = null;
            b.Owner = null;
            b.DespawnBehavior = default;
            b.Presenter.ReturnToPool();
            b.Presenter = null;
        }

        void Compact()
        {
            var write = 0;
            for (var read = 0; read < bullets.Count; read++)
            {
                var b = bullets[read];
                if (b.Presenter == null)
                {
                    free.Push(b);
                    continue;
                }
                bullets[write++] = b;
            }
            bullets.RemoveRange(write, bullets.Count - write);
        }
    }
}

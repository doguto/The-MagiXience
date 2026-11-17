using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵弾を固定フレームで動かす。弾の見た目・当たり判定・寿命・画面外判定は BulletEntityPresenter が担う。
    public sealed class BulletSystem
    {
        sealed class Bullet : IMotionBody
        {
            public BulletEntityPresenter Presenter;
            public MotionSequence Motion;
            public int DelayFrames;
            public bool Force;
            public Vector2 ForceVelocity;
            public Vector2 ForceOffset;
            public Vector3 PreviousPosition;
            bool invincible;

            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }
            public Transform Transform => Presenter.transform;

            public void SetInvincible(bool value)
            {
                if (invincible == value) return;
                invincible = value;
                Presenter.Model.SetInvincible(value);
            }
        }

        static readonly MotionStep[] DefaultMotion = { new VelocityMotion() };

        readonly List<Bullet> bullets = new();
        readonly Stack<Bullet> free = new();
        readonly List<IBulletForce> forces = new();

        public void AddForce(IBulletForce force)
        {
            if (force != null && !forces.Contains(force)) forces.Add(force);
        }

        public void RemoveForce(IBulletForce force) => forces.Remove(force);

        public int ActiveCount => bullets.Count;

        public BulletEntityPresenter Spawn(in BulletSpawn spawn)
        {
            if (spawn.Prefab == null)
            {
                Debug.LogWarning("[BulletSystem] Bullet prefab is not assigned.");
                return null;
            }

            var pool = BulletPool.GetOrCreateSharedPool(spawn.Prefab);
            var presenter = pool.Get();
            var rotation = presenter.InitializeDriven(spawn.Damage, spawn.Position, spawn.Direction, pool, spawn.Rotation, spawn.Range, spawn.IsPlayerChargeBullet);

            var bullet = free.Count > 0 ? free.Pop() : new Bullet();
            bullet.Presenter = presenter;
            bullet.Position = spawn.Position;
            bullet.PreviousPosition = spawn.Position;
            bullet.Rotation = rotation;
            bullet.DelayFrames = Mathf.Max(0, spawn.StartDelayFrames);
            bullet.Force = spawn.AffectedByForce && AnyActiveForce();
            bullet.ForceVelocity = bullet.ForceOffset = Vector2.zero;
            var steps = spawn.Motion != null && spawn.Motion.Count > 0 ? spawn.Motion : DefaultMotion;
            bullet.Motion = new MotionSequence(steps, bullet, new MotionLaunch
            {
                Direction = spawn.Direction,
                AttackTime = spawn.AttackTime,
                WindAxis = LaunchAxis(),
            });
            bullets.Add(bullet);
            return presenter;
        }

        bool AnyActiveForce()
        {
            foreach (var force in forces)
            {
                if (force.Active) return true;
            }
            return false;
        }

        Vector2 LaunchAxis()
        {
            foreach (var force in forces)
            {
                if (force.LaunchAxis != Vector2.zero) return force.LaunchAxis;
            }
            return Vector2.zero;
        }

        public void Tick(float dt)
        {
            for (var i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                if (!b.Presenter || !b.Presenter.IsActive)
                {
                    Release(i);
                    continue;
                }

                if (b.DelayFrames > 0)
                {
                    b.DelayFrames--;
                    continue;
                }

                b.Motion.Tick(dt);
                b.SetInvincible(b.Motion.GrantsInvincibility);
                if (b.Motion.Rejected)
                {
                    b.Presenter.ReturnToPool();
                    Release(i);
                    continue;
                }

                var position = b.Position;
                if (b.Force && forces.Count > 0)
                {
                    var authoredVelocity = (Vector2)(position - b.PreviousPosition) / dt;
                    foreach (var force in forces) force.Integrate(authoredVelocity, dt, ref b.ForceVelocity, ref b.ForceOffset);
                    position += (Vector3)(b.ForceOffset * b.Presenter.WindInfluence);
                }
                b.PreviousPosition = b.Position;
                b.Presenter.transform.SetPositionAndRotation(position, b.Rotation);
            }
        }

        public void Clear()
        {
            for (var i = bullets.Count - 1; i >= 0; i--)
            {
                if (bullets[i].Presenter && bullets[i].Presenter.IsActive) bullets[i].Presenter.ReturnToPool();
                Release(i);
            }
        }

        void Release(int index)
        {
            var b = bullets[index];
            b.Motion?.Stop();
            b.Motion = null;
            b.Presenter = null;
            bullets.RemoveAt(index);
            free.Push(b);
        }
    }
}

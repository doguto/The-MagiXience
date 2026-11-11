using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public struct BulletSpawn
    {
        public BulletEntityPresenter Prefab;
        public int Damage;
        public Vector3 Position;
        public Vector2 Direction;
        // Prefab 本来の回転に対するオフセット
        public Quaternion Rotation;
        // 飛距離の上限 (発射方向への進捗)。0 以下で無制限
        public float Range;
        // 出現してから動き出すまでのフレーム数
        public int StartDelayFrames;
        public IReadOnlyList<MotionStep> Motion;
        public float AttackTime;
        public bool AffectedByWind;
    }

    // 敵弾を固定フレームで動かす。弾の見た目・当たり判定・寿命・画面外判定は BulletEntityPresenter が担う。
    public sealed class BulletSystem
    {
        sealed class Bullet : IMotionBody
        {
            public BulletEntityPresenter Presenter;
            public MotionSequence Motion;
            public int DelayFrames;
            public bool Wind;
            public Vector2 WindVelocity;
            public Vector2 WindOffset;
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
            var rotation = presenter.InitializeDriven(spawn.Damage, spawn.Position, spawn.Direction, pool, spawn.Rotation, spawn.Range);

            var bullet = free.Count > 0 ? free.Pop() : new Bullet();
            bullet.Presenter = presenter;
            bullet.Position = spawn.Position;
            bullet.PreviousPosition = spawn.Position;
            bullet.Rotation = rotation;
            bullet.DelayFrames = Mathf.Max(0, spawn.StartDelayFrames);
            bullet.Wind = spawn.AffectedByWind && BattleWind.Enabled;
            bullet.WindVelocity = bullet.WindOffset = Vector2.zero;
            var steps = spawn.Motion != null && spawn.Motion.Count > 0 ? spawn.Motion : DefaultMotion;
            bullet.Motion = new MotionSequence(steps, bullet, new MotionLaunch { Direction = spawn.Direction, AttackTime = spawn.AttackTime });
            bullets.Add(bullet);
            return presenter;
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
                if (b.Wind)
                {
                    var authoredVelocity = (Vector2)(position - b.PreviousPosition) / dt;
                    BattleWind.IntegrateBullet(authoredVelocity, dt, ref b.WindVelocity, ref b.WindOffset);
                    position += (Vector3)(b.WindOffset * b.Presenter.WindInfluence);
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

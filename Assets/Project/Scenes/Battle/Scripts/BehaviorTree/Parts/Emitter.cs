using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public static class Emitter
    {
        // 1 発を出す。delayFrames > 0 なら予約し、その時点の撃ち手の位置から出す
        public static void Spawn(BattleContext ctx, BulletSpec bullet, Vector2 muzzleOffset, Func<EmitContext, Vector2> position,
            Vector2 direction, Quaternion rotation, int spawnDelayFrames, int startDelayFrames,
            IReadOnlyList<MotionStep> motionOverride = null, float rangeOverride = -1f)
        {
            if (bullet?.prefab == null) return;
            var world = ctx.World;
            var self = ctx.Self;
            // 倒された瞬間に撃つ弾 (死亡時攻撃) は撃ち手が既にいないため、予約の取り消し対象にしない
            var owner = self != null && self.IsAlive ? self : null;
            world.Schedule(spawnDelayFrames, () =>
            {
                var e = new EmitContext(ctx, muzzleOffset);
                var pos = position(e);
                var z = self != null ? self.Body.Position.z : 0f;
                world.Services.Bullets.Spawn(new BulletSpawn
                {
                    Prefab = bullet.prefab,
                    Damage = bullet.damage,
                    Position = new Vector3(pos.x, pos.y, z),
                    Direction = direction,
                    Rotation = rotation,
                    Range = rangeOverride >= 0f ? rangeOverride : bullet.range,
                    StartDelayFrames = startDelayFrames,
                    Motion = motionOverride ?? bullet.Motion,
                    AttackTime = e.AttackTime,
                    AffectedByForce = bullet.affectedByWind,
                });
            }, owner);
        }

        public static void PlaySe(BattleContext ctx, SeType se)
        {
            if (se != SeType.None) ctx.Services.PlaySe?.Invoke(se);
        }
    }
}

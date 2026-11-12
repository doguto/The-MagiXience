using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 発射時点の情報。撃つ敵がいなければ (ステージ直下の発射) Self は null
    public readonly struct EmitContext
    {
        public EmitContext(BattleContext ctx, Vector2 muzzleOffset)
        {
            Context = ctx;
            Self = ctx.Self;
            Origin = (Self != null ? (Vector2)Self.Body.Position : Vector2.zero) + muzzleOffset;
            SelfRotation = Self != null ? Self.Body.Rotation : Quaternion.identity;
            AttackTime = Self != null ? Self.AttackTime : ctx.Frame * BattleWorld.DeltaTime;
        }

        public BattleContext Context { get; }
        public BattleActor Self { get; }
        public Vector2 Origin { get; }
        public Quaternion SelfRotation { get; }
        public float AttackTime { get; }

        public Vector2 PlayerOrDefault(Vector2 fallback) =>
            BattleTargets.TryGetPlayerPosition(out var p) ? (Vector2)p : fallback;
    }

    // ---------- 方向 ----------

    [Serializable]
    public abstract class DirectionSource
    {
        public abstract Vector2 GetDirection(in EmitContext e);
        // 発射位置を差し替える場合のみ値を返す (画面端からの発射など)
        public virtual Vector2? GetOrigin(in EmitContext e) => null;
        public virtual string Summary => GetType().Name.Replace("Direction", "");
    }

    [Serializable]
    public sealed class FixedDirection : DirectionSource
    {
        [SerializeField] Vector2 direction = Vector2.left;

        public FixedDirection() { }
        public FixedDirection(Vector2 direction) => this.direction = direction;

        public override Vector2 GetDirection(in EmitContext e) => direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        public override string Summary => $"固定 {direction}";
    }

    // 旧 AimDirectionConfig。左向きを 0° として上下の振れ幅を制限できる
    [Serializable]
    public sealed class AimDirection : DirectionSource
    {
        [SerializeField, Range(0f, 180f)] float maxUpAngle = 180f;
        [SerializeField, Range(0f, 180f)] float maxDownAngle = 180f;

        public override Vector2 GetDirection(in EmitContext e)
        {
            var to = e.PlayerOrDefault(e.Origin + Vector2.left) - e.Origin;
            if (to.sqrMagnitude < 1e-8f) return Vector2.left;
            to.Normalize();
            if (maxUpAngle >= 180f && maxDownAngle >= 180f) return to;

            var angle = Mathf.Atan2(to.y, -to.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, -maxDownAngle, maxUpAngle);
            var rad = angle * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public override string Summary => "自機狙い";
    }

    // 旧 ForwardDirectionConfig。撃つ敵の向き (左が正面)
    [Serializable]
    public sealed class ForwardDirection : DirectionSource
    {
        public override Vector2 GetDirection(in EmitContext e) => ((Vector2)(e.SelfRotation * Vector3.left)).normalized;
        public override string Summary => "敵の正面";
    }

    // 旧 ScreenEdgeDirectionConfig。風上の画面端から撃つ。発射位置は発射時刻で端に沿って往復する
    [Serializable]
    public sealed class ScreenEdgeDirection : DirectionSource
    {
        [SerializeField] bool fromLeft;
        [SerializeField, Tooltip("風向きに合わせる")] bool followWind;
        [SerializeField, Tooltip("画面端からの内側距離。負で画面外")] float edgeInset = 0.02f;
        [SerializeField, Min(0f)] float verticalMargin = 0.6f;
        [SerializeField] float lateralOffset;
        [SerializeField] float angularSpeed = 1.7f;
        [SerializeField] float attackTimeOffset = 0.9f;

        public override Vector2 GetDirection(in EmitContext e)
        {
            if (followWind && BattleWind.Direction != WindDirection.None) return BattleWind.Vector;
            return fromLeft ? Vector2.right : Vector2.left;
        }

        public override Vector2? GetOrigin(in EmitContext e)
        {
            var dir = GetDirection(e);
            var t = Mathf.Max(0f, e.AttackTime - attackTimeOffset);
            var wave = (Mathf.Sin(t * angularSpeed) + 1f) * 0.5f;

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                var x = dir.x > 0f ? ScreenBoundsCache.MinX + edgeInset : ScreenBoundsCache.MaxX - edgeInset;
                var y = Mathf.Lerp(ScreenBoundsCache.MinY + verticalMargin, ScreenBoundsCache.MaxY - verticalMargin, wave) + lateralOffset;
                return new Vector2(x, y);
            }
            else
            {
                var y = dir.y > 0f ? ScreenBoundsCache.MinY + edgeInset : ScreenBoundsCache.MaxY - edgeInset;
                var x = Mathf.Lerp(ScreenBoundsCache.MinX + verticalMargin, ScreenBoundsCache.MaxX - verticalMargin, wave) + lateralOffset;
                return new Vector2(x, y);
            }
        }

        public override string Summary => followWind ? "画面端 (風上)" : fromLeft ? "画面左端" : "画面右端";
    }

    // ---------- 弾の回転 ----------

    [Serializable]
    public abstract class RotationSource
    {
        public abstract Quaternion GetRotation(in EmitContext e);
    }

    [Serializable]
    public sealed class InheritRotation : RotationSource
    {
        public override Quaternion GetRotation(in EmitContext e) => e.SelfRotation;
    }

    [Serializable]
    public sealed class FixedRotation : RotationSource
    {
        [SerializeField] float degrees;
        public override Quaternion GetRotation(in EmitContext e) => Quaternion.Euler(0f, 0f, degrees);
    }

    [Serializable]
    public sealed class OffsetRotation : RotationSource
    {
        [SerializeField] float offsetDegrees;
        public override Quaternion GetRotation(in EmitContext e) => e.SelfRotation * Quaternion.Euler(0f, 0f, offsetDegrees);
    }

    [Serializable]
    public sealed class AimRotation : RotationSource
    {
        public override Quaternion GetRotation(in EmitContext e)
        {
            var to = e.PlayerOrDefault(e.Origin + Vector2.left) - e.Origin;
            return to.sqrMagnitude < 1e-8f ? Quaternion.identity : Quaternion.FromToRotation(Vector2.left, to);
        }
    }

    // ---------- 弾の設定 ----------

    [Serializable]
    public sealed class BulletSpec
    {
        public BulletEntityPresenter prefab;
        [Min(0)] public int damage = 10;
        [Tooltip("指定するとインラインの移動より優先")] public MotionPreset motionPreset;
        [SerializeReference, SubclassSelector] public List<MotionStep> motion = new();
        [Tooltip("飛距離の上限。0 で無制限")] public float range;
        public bool affectedByWind = true;

        public IReadOnlyList<MotionStep> Motion => motionPreset != null ? motionPreset.Steps : motion;
        public override string ToString() => prefab ? prefab.name : "(弾未設定)";
    }

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
                    AffectedByWind = bullet.affectedByWind,
                });
            }, owner);
        }

        public static void PlaySe(BattleContext ctx, SeType se)
        {
            if (se != SeType.None) ctx.Services.PlaySe?.Invoke(se);
        }
    }
}

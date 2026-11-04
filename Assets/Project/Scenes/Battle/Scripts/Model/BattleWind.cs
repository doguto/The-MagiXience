using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public enum WindDirection { None, Up, Down, Left, Right }

    [Serializable]
    public class WindInterval
    {
        public WindDirection direction;
        [Min(0.1f)] public float seconds = 6f;
    }

    [Serializable]
    public class BattleWindSettings
    {
        [Tooltip("自機への風の影響と風向き表示を有効にする。OFFでも弾用の方向スケジュールは継続する。")]
        public bool enabled;
        [Range(0f, 1f)] public float playerSpeedRatio = 0.2f;
        public List<WindInterval> intervals = new();
        [Min(0f), Tooltip("敵弾に与える風の加速度（ワールド単位/秒²）")]
        public float bulletAcceleration = 3f;
        [Min(0f), Tooltip("敵弾の速度に比例する抵抗係数（1/秒）。直進弾の終端速度は加速度÷この値。0なら抵抗なし")]
        public float bulletDrag = 0.6f;
    }

    // One active battle sequence owns this state. Bullets take a snapshot at launch.
    public static class BattleWind
    {
        static BattleWindSettings settings;
        static int index;
        static float remaining;
        static bool HasSchedule => settings != null && settings.intervals != null && settings.intervals.Count > 0;
        public static bool Enabled => HasSchedule && settings.enabled;
        // Projectile paths retain the authored schedule even when player wind and its HUD are disabled.
        public static WindDirection ProjectileDirection => HasSchedule ? settings.intervals[index].direction : WindDirection.None;
        public static Vector2 ProjectileVector => ToVector(ProjectileDirection);
        public static WindDirection Direction => Enabled ? ProjectileDirection : WindDirection.None;
        public static Vector2 Vector => ToVector(Direction);
        public static float PlayerSpeedRatio => Enabled ? Mathf.Clamp01(settings.playerSpeedRatio) : 0f;
        public static float BulletAcceleration => Enabled ? Mathf.Max(0f, settings.bulletAcceleration) : 0f;
        public static float BulletDrag => Enabled ? Mathf.Max(0f, settings.bulletDrag) : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { settings = null; index = 0; remaining = 0f; }

        public static void Begin(BattleWindSettings value)
        {
            Reset();
            settings = value;
            if (HasSchedule) remaining = Mathf.Max(0.1f, settings.intervals[0].seconds);
        }

        public static void Tick(float deltaTime)
        {
            if (!HasSchedule || deltaTime <= 0f) return;
            remaining -= deltaTime;
            while (remaining <= 0f)
            {
                index = (index + 1) % settings.intervals.Count;
                remaining += Mathf.Max(0.1f, settings.intervals[index].seconds);
            }
        }

        public static Vector2 ToVector(WindDirection direction) => direction switch
        {
            WindDirection.Up => Vector2.up,
            WindDirection.Down => Vector2.down,
            WindDirection.Left => Vector2.left,
            WindDirection.Right => Vector2.right,
            _ => Vector2.zero
        };

        public static Vector2 PlayerVelocity(Vector2 input, float currentSpeed)
            => (input + Vector * PlayerSpeedRatio) * currentSpeed;

        // Integrate the correction to an existing trajectory, without changing its movement preset.
        // The analytic solution is stable even when drag * deltaTime is large.
        public static void IntegrateBullet(Vector2 authoredVelocity, float deltaTime,
            ref Vector2 velocityCorrection, ref Vector2 positionCorrection)
        {
            if (deltaTime <= 0f) return;
            if (Direction == WindDirection.None)
            {
                velocityCorrection = Vector2.zero;
                return;
            }
            var acceleration = Vector * BulletAcceleration;
            float drag = BulletDrag;
            if (drag < 0.00001f)
            {
                positionCorrection += velocityCorrection * deltaTime + acceleration * (0.5f * deltaTime * deltaTime);
                velocityCorrection += acceleration * deltaTime;
                return;
            }
            var terminalCorrection = acceleration / drag - authoredVelocity;
            float decay = Mathf.Exp(-drag * deltaTime);
            positionCorrection += terminalCorrection * deltaTime +
                (velocityCorrection - terminalCorrection) * ((1f - decay) / drag);
            velocityCorrection = terminalCorrection + (velocityCorrection - terminalCorrection) * decay;
        }
    }
}

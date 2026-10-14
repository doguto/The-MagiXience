using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public enum WindDirection { None, Up, Down, Left }

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
            _ => Vector2.zero
        };

        public static Vector2 PlayerVelocity(Vector2 input, float currentSpeed)
            => (input + Vector * PlayerSpeedRatio) * currentSpeed;
    }
}

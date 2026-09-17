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
        public static bool Enabled => settings != null && settings.enabled && settings.intervals.Count > 0;
        public static WindDirection Direction => Enabled ? settings.intervals[index].direction : WindDirection.None;
        public static Vector2 Vector => ToVector(Direction);
        public static float PlayerSpeedRatio => Enabled ? Mathf.Clamp01(settings.playerSpeedRatio) : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { settings = null; index = 0; remaining = 0f; }

        public static void Begin(BattleWindSettings value)
        {
            Reset();
            settings = value;
            if (Enabled) remaining = Mathf.Max(0.1f, settings.intervals[0].seconds);
        }

        public static void Tick(float deltaTime)
        {
            if (!Enabled || deltaTime <= 0f) return;
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

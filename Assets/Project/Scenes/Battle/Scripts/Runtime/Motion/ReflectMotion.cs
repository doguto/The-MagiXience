using System;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 ReflectMovementConfig。一度画面内に入ってから画面端で反射する
    [Serializable]
    public sealed class ReflectMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField] int maxReflections = 1;
        [SerializeField] bool reflectTop = true;
        [SerializeField] bool reflectBottom = true;
        [SerializeField] bool reflectLeft;
        [SerializeField] bool reflectRight;

        public override string Summary => $"反射 {speed:0.##} / 最大 {maxReflections} 回";

        public override MotionRunner CreateRunner()
        {
            var vel = Vector2.zero;
            var reflections = 0;
            var entered = false;
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            var runner = new DelegatePullRunner(0f, true, (r, dt) =>
            {
                var pos = (Vector2)r.Target.Position + vel * dt;
                var inScreen = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
                if (!entered)
                {
                    entered = inScreen;
                }
                else if (reflections < maxReflections)
                {
                    if (reflectLeft && pos.x <= minX) { vel.x = -vel.x; pos.x = minX; reflections++; }
                    else if (reflectRight && pos.x >= maxX) { vel.x = -vel.x; pos.x = maxX; reflections++; }
                    if (reflectBottom && pos.y <= minY) { vel.y = -vel.y; pos.y = minY; reflections++; }
                    else if (reflectTop && pos.y >= maxY) { vel.y = -vel.y; pos.y = maxY; reflections++; }
                }
                r.Target.Position = new Vector3(pos.x, pos.y, r.Target.Position.z);
            });
            return new BeginHook(runner, r =>
            {
                minX = ScreenBoundsCache.MinX; maxX = ScreenBoundsCache.MaxX;
                minY = ScreenBoundsCache.MinY; maxY = ScreenBoundsCache.MaxY;
                vel = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized * speed : Vector2.left * speed;
            });
        }
    }
}

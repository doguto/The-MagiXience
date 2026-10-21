using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// direction(配置時点で中心から見た方向)を元に中心座標を逆算し、
    /// 半径をstartRadiusからtargetRadiusへ縮めながら角速度rotationSpeedで回転し続ける螺旋移動。
    /// ChargeSignal が AttackEvent.BulletMovement 経由で弾ごとに動的に渡す想定(Bullet Prefab側には持たせない)。
    /// </summary>
    [Serializable]
    public class SpiralMovementConfig : IMovementStep
    {
        readonly float startRadius;
        readonly float targetRadius;
        readonly float duration;
        readonly float rotationSpeed;

        public SpiralMovementConfig(float startRadius, float targetRadius, float duration, float rotationSpeed)
        {
            this.startRadius = startRadius;
            this.targetRadius = targetRadius;
            this.duration = Mathf.Max(duration, 0.01f);
            this.rotationSpeed = rotationSpeed;
        }

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3 center = Vector3.zero;
            float startAngle = 0f;
            float elapsed = 0f;

            return PullMovementHelper.Create(target, duration, t =>
            {
                var dir = direction == Vector2.zero ? (Vector2)t.right : direction.normalized;
                center = t.position - (Vector3)(dir * startRadius);
                startAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }, (t, dt) =>
            {
                elapsed += dt;
                float ratio = Mathf.Clamp01(elapsed / duration);
                float radius = Mathf.Lerp(startRadius, targetRadius, ratio);
                float angle = startAngle + rotationSpeed * elapsed;
                float rad = angle * Mathf.Deg2Rad;
                t.position = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            }, Ease.Linear);
        }
    }
}

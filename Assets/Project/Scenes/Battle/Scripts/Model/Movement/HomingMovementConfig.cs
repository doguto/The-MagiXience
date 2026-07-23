using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 自機の方向へ turnSpeed の角速度上限で進行方向を補間し続けながら前進する、
    /// いわゆるホーミング移動ステップ。
    /// 初期進行方向は transform.right（AimRotateConfig 等で向きを決めた後の想定）。
    /// </summary>
    [Serializable]
    public class HomingMovementConfig : IMovementStep
    {
        [SerializeField, Min(0f)] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("1秒あたりに旋回できる角度（度）")] float turnSpeed = 180f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 direction = Vector3.zero;

            return PullMovementHelper.Create(target, duration,
                onStart: t =>
                {
                    direction = t.right;
                },
                onUpdate: (t, dt) =>
                {
                    var playerTransform = PlayerPositionReference.Transform;
                    if (playerTransform != null)
                    {
                        Vector3 toPlayer = (playerTransform.position - t.position).normalized;
                        float maxRadiansDelta = turnSpeed * Mathf.Deg2Rad * dt;
                        direction = Vector3.RotateTowards(direction, toPlayer, maxRadiansDelta, 0f).normalized;
                    }

                    t.position += direction * speed * dt;
                });
        }
    }
}

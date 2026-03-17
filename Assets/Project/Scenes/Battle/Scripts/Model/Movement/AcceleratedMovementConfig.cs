using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class AcceleratedMovementConfig : IMovementStep
    {
        [SerializeField] Vector2 direction = Vector2.left;
        [SerializeField, Min(0f)] float initialSpeed = 2f;
        [SerializeField, Tooltip("負の値で減速")] float acceleration = 1f;
        [SerializeField, Min(0f)] float maxSpeed = 10f;
        [SerializeField, Min(0f)] float minSpeed = 0f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 dir = ((Vector3)(overrideDirection != Vector2.zero ? overrideDirection : direction)).normalized;
            Vector3 velocity = dir * initialSpeed;
            float accel = acceleration;
            float max = maxSpeed;

            float min = minSpeed;

            return PullMovementHelper.Create(target, duration, (t, dt) =>
            {
                velocity += dir * accel * dt;
                float speed = velocity.magnitude;
                if (max > 0f && speed > max) velocity = velocity.normalized * max;
                if (speed < min) velocity = speed > 0f ? velocity.normalized * min : dir * min;
                t.position += velocity * dt;
            });
        }
    }
}

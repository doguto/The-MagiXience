using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class AimStraightMovementConfig : IMovementStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 velocity = Vector3.zero;

            return PullMovementHelper.Create(target, duration,
                onStart: t =>
                {
                    var playerTransform = PlayerPositionReference.Transform;
                    Vector3 dir = playerTransform != null
                        ? (playerTransform.position - t.position).normalized
                        : Vector3.left;
                    velocity = dir * speed;
                },
                onUpdate: (t, dt) =>
                {
                    t.position += velocity * dt;
                });
        }
    }
}

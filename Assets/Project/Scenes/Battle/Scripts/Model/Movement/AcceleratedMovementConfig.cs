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
        [SerializeField] float acceleration = 1f;
        [SerializeField, Min(0f)] float maxSpeed = 10f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 dir = ((Vector3)(overrideDirection != Vector2.zero ? overrideDirection : direction)).normalized;
            var strategy = new AcceleratedMovement(dir * initialSpeed, dir * acceleration, maxSpeed);
            return PullMovementHelper.Wrap(target, strategy, duration);
        }
    }
}

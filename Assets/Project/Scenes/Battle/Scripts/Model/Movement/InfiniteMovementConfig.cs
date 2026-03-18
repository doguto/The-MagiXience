using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 指定した速度ベクトルで無限に移動し続ける。
    /// velocity = Vector3.zero でその場に止まり続ける。
    /// </summary>
    [Serializable]
    public class InfiniteMovementConfig : IMovementStep
    {
        [SerializeField] Vector3 velocity = Vector3.left;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 vel = overrideDirection != Vector2.zero
                ? (Vector3)(overrideDirection.normalized) * velocity.magnitude
                : velocity;

            if (vel == Vector3.zero)
                return PullMovementHelper.Create(target, 0f, (_, __) => { });

            return PullMovementHelper.Create(target, 0f, (t, dt) =>
                t.position += vel * dt);
        }
    }
}

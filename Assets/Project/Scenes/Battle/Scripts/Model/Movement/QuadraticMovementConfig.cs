using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class QuadraticMovementConfig : IMovementStep
    {
        [SerializeField] Vector3 initialVelocity = Vector3.left;
        [SerializeField] Vector3 acceleration = Vector3.down;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            return PullMovementHelper.Wrap(target, new QuadraticMovement(initialVelocity, acceleration), duration);
        }
    }
}

using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class SineMovementConfig : IMovementStep
    {
        [SerializeField] Vector3 baseVelocity = Vector3.left;
        [SerializeField, Min(0f)] float amplitude = 1f;
        [SerializeField, Min(0.01f)] float frequency = 1f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            return PullMovementHelper.Wrap(target, new SineMovement(baseVelocity, amplitude, frequency), duration);
        }
    }
}

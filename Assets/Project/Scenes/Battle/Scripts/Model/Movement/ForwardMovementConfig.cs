using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// transform の現在の回転方向に向かって直進する移動ステップ。
    /// AimRotateConfig 等で回転させた後に使うことを想定。
    /// スプライトの初期回転を考慮するため、開始時の rotation をオフセットとして記録し、
    /// そこからの差分を進行方向として使う。
    /// </summary>
    [Serializable]
    public class ForwardMovementConfig : IMovementStep
    {
        [SerializeField, Min(0f)] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector3 velocity = Vector3.zero;

            return PullMovementHelper.Create(target, duration,
                onStart: t =>
                {
                    // transform.right はスプライト初期回転込みの方向なので
                    // そのまま進行方向として使える
                    // （AimRotateConfig が初期回転を考慮して回転角を設定する前提）
                    velocity = t.right * speed;
                },
                onUpdate: (t, dt) =>
                {
                    t.position += velocity * dt;
                });
        }
    }
}

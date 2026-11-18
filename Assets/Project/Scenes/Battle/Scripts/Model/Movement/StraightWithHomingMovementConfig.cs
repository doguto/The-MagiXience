using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 水平方向（左向き）には一定速度で流れ続け、垂直方向のみ自機のY座標へ
    /// verticalTrackingSpeed の速度上限で追尾する移動ステップ。
    /// HomingMovementConfig と違い方向ベクトルごと回転させないため、
    /// 水平方向の速度は常に一定に保たれる。
    /// </summary>
    [Serializable]
    public class StraightWithHomingMovementConfig : IMovementStep
    {
        [SerializeField, Min(0f), Tooltip("水平方向（左向き）の移動速度")] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("垂直方向の自機追尾速度上限")] float verticalTrackingSpeed = 3f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            return PullMovementHelper.Create(target, duration,
                onUpdate: (t, dt) =>
                {
                    float x = t.position.x - speed * dt;

                    float y = t.position.y;
                    var playerTransform = PlayerPositionReference.Transform;
                    if (playerTransform != null)
                    {
                        y = Mathf.MoveTowards(y, playerTransform.position.y, verticalTrackingSpeed * dt);
                    }

                    t.position = new Vector3(x, y, t.position.z);
                });
        }
    }
}

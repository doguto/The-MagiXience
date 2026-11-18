using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 基準方向(axis)を中心に ±angleDegrees の折れ線を描いて進む移動ステップ。
    /// segmentLength だけ進むごとに折り返すため、axis が同じで initialSign だけ逆の弾を
    /// 同時に撃つと、互いに交差し続ける「編み込み」状のビームになる。
    ///
    /// axis は Play() に渡される direction（IDirectionProvider の出力）。
    /// direction が zero の場合のみ fallbackAxis を使う。
    /// </summary>
    [Serializable]
    public class ZigzagMovementConfig : IMovementStep
    {
        [SerializeField, Min(0f)] float speed = 6f;

        [SerializeField, Tooltip("基準方向からの振れ角(度)。45で直交するクロスになる")]
        float angleDegrees = 45f;

        [SerializeField, Min(0.01f), Tooltip("折り返すまでの移動距離(斜辺方向の実距離)")]
        float segmentLength = 1f;

        [SerializeField, Tooltip("最初に振れる向き。+1で反時計回り側、-1で時計回り側。ペアで逆符号にする")]
        int initialSign = 1;

        [SerializeField, Min(0), Tooltip("折り返し回数の上限。0で無制限")]
        int maxReflections = 0;

        [SerializeField, Min(0f), Tooltip("最初の折り返しまでの距離。0でsegmentLengthと同じ。半分にすると振幅の位相をずらせる")]
        float firstSegmentLength = 0f;

        [SerializeField, Min(0f), Tooltip("継続時間(秒)。0で無限")]
        float duration = 0f;

        [SerializeField, Tooltip("direction が渡されなかった場合に使う基準方向")]
        Vector2 fallbackAxis = Vector2.left;

        [SerializeField, Tooltip("進行方向にスプライトを向ける")]
        bool rotateToVelocity;

        [SerializeField, Tooltip("rotateToVelocity 時の補正角。スプライトの正面が+X以外を向いている場合に使う")]
        float rotationOffsetDegrees;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            var axis = direction != Vector2.zero ? direction.normalized : fallbackAxis.normalized;
            if (axis == Vector2.zero) axis = Vector2.left;

            var axisAngle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            var sign = initialSign >= 0 ? 1 : -1;
            var reflections = 0;
            var traveled = 0f;
            var currentSegment = firstSegmentLength > 0f ? firstSegmentLength : segmentLength;
            var velocity = AngleToDirection(axisAngle + angleDegrees * sign);

            // duration=0 のとき Ease.Unset だと DOTween 既定のイージングで dt が歪むため、
            // 等速を保証するために Linear を明示する。
            return PullMovementHelper.Create(target, duration,
                onStart: t => ApplyRotation(t, velocity),
                onUpdate: (t, dt) =>
                {
                    var remaining = speed * dt;

                    // 1フレームで複数回折り返す可能性があるため、残り距離を消化しきるまで回す
                    while (remaining > 0f)
                    {
                        var canTurn = maxReflections <= 0 || reflections < maxReflections;
                        var toTurn = canTurn ? currentSegment - traveled : remaining;
                        var move = Mathf.Min(remaining, toTurn);

                        t.position += (Vector3)(velocity * move);
                        remaining -= move;

                        if (!canTurn) break;

                        traveled += move;
                        if (traveled < currentSegment - Mathf.Epsilon) break;

                        traveled = 0f;
                        currentSegment = segmentLength;
                        sign = -sign;
                        reflections++;
                        velocity = AngleToDirection(axisAngle + angleDegrees * sign);
                        ApplyRotation(t, velocity);
                    }
                },
                ease: Ease.Linear);
        }

        void ApplyRotation(Transform target, Vector2 velocity)
        {
            if (!rotateToVelocity) return;
            var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            target.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        static Vector2 AngleToDirection(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}

using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AimDirectionConfig : IDirectionProvider
    {
        // 水平左向きを0度として、上下それぞれ何度まで自機を狙えるか。180なら制限なし。
        [SerializeField, Range(0f, 180f)] float maxUpAngle = 180f;
        [SerializeField, Range(0f, 180f)] float maxDownAngle = 180f;

        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
        }

        public Vector2 GetDirection()
        {
            var direction = ((Vector2)(getPlayerPosition() - getEnemyPosition())).normalized;
            return direction == Vector2.zero ? Vector2.left : ClampAngle(direction);
        }

        Vector2 ClampAngle(Vector2 direction)
        {
            // 左向きを0度、上を正とする符号付き角度(-180〜180)
            var angle = Mathf.Atan2(direction.y, -direction.x) * Mathf.Rad2Deg;
            var radian = Mathf.Clamp(angle, -maxDownAngle, maxUpAngle) * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Cos(radian), Mathf.Sin(radian));
        }

        public IDirectionProvider Clone() => new AimDirectionConfig
        {
            maxUpAngle = maxUpAngle,
            maxDownAngle = maxDownAngle
        };
    }
}

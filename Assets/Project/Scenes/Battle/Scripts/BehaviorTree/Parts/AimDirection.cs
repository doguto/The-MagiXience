using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 AimDirectionConfig。左向きを 0° として上下の振れ幅を制限できる
    [Serializable]
    public sealed class AimDirection : DirectionSource
    {
        [SerializeField, Range(0f, 180f)] float maxUpAngle = 180f;
        [SerializeField, Range(0f, 180f)] float maxDownAngle = 180f;

        public override Vector2 GetDirection(in EmitContext e)
        {
            var to = e.PlayerOrDefault(e.Origin + Vector2.left) - e.Origin;
            if (to.sqrMagnitude < 1e-8f) return Vector2.left;
            to.Normalize();
            if (maxUpAngle >= 180f && maxDownAngle >= 180f) return to;

            var angle = Mathf.Atan2(to.y, -to.x) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, -maxDownAngle, maxUpAngle);
            var rad = angle * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public override string Summary => "自機狙い";
    }
}

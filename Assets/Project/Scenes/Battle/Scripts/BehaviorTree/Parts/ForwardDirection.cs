using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 ForwardDirectionConfig。撃つ敵の向き (左が正面)
    [Serializable]
    public sealed class ForwardDirection : DirectionSource
    {
        public override Vector2 GetDirection(in EmitContext e) => ((Vector2)(e.SelfRotation * Vector3.left)).normalized;
        public override string Summary => "敵の正面";
    }
}

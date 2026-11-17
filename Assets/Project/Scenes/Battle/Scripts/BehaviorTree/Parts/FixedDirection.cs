using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class FixedDirection : DirectionSource
    {
        [SerializeField] Vector2 direction = Vector2.left;

        public FixedDirection() { }
        public FixedDirection(Vector2 direction) => this.direction = direction;

        public override Vector2 GetDirection(in EmitContext e) => direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        public override string Summary => $"固定 {direction}";
    }
}

using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class FixedDirection : DirectionSource
    {
        [SerializeField, Tooltip("向き (度)。0 = 右、90 = 上、180 = 左")] BtFloat angle = new(180);

        public override Vector2 GetDirection(in EmitContext e)
        {
            var rad = angle.Resolve(e.Scope) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public override string Summary => string.IsNullOrEmpty(angle.param) ? $"固定 {angle}°" : $"固定 {angle}";
    }
}

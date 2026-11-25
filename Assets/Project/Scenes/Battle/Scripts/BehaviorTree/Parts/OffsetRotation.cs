using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class OffsetRotation : RotationSource
    {
        [SerializeField] float offsetDegrees;
        public override Quaternion GetRotation(in EmitContext e) => e.SelfRotation * Quaternion.Euler(0f, 0f, offsetDegrees);
    }
}

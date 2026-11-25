using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class InheritRotation : RotationSource
    {
        public override Quaternion GetRotation(in EmitContext e) => e.SelfRotation;
    }
}

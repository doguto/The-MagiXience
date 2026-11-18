using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class FixedRotation : RotationSource
    {
        [SerializeField] float degrees;
        public override Quaternion GetRotation(in EmitContext e) => Quaternion.Euler(0f, 0f, degrees);
    }
}

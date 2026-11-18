using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class AimRotation : RotationSource
    {
        public override Quaternion GetRotation(in EmitContext e)
        {
            var to = e.PlayerOrDefault(e.Origin + Vector2.left) - e.Origin;
            return to.sqrMagnitude < 1e-8f ? Quaternion.identity : Quaternion.FromToRotation(Vector2.left, to);
        }
    }
}

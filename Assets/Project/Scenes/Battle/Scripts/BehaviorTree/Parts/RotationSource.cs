using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 弾の回転 ----------

    [Serializable]
    public abstract class RotationSource
    {
        public abstract Quaternion GetRotation(in EmitContext e);
    }
}

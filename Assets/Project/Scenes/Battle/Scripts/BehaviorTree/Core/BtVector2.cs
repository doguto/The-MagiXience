using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BtVector2
    {
        public Vector2 value;
        public string param;

        public BtVector2(Vector2 value)
        {
            this.value = value;
            param = null;
        }

        public Vector2 Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(param) || scope == null || !scope.TryGet(param, out var p)) return value;
            return p.Vector2Value;
        }

        public static implicit operator BtVector2(Vector2 v) => new(v);
    }
}

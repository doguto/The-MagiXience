using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BtObject<T> where T : UnityEngine.Object
    {
        public T value;
        public string param;

        public T Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(param) || scope == null || !scope.TryGet(param, out var p)) return value;
            return p.ObjectValue as T ?? value;
        }
    }
}

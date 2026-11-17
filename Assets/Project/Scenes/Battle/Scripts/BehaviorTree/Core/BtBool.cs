using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BtBool
    {
        public bool value;
        public string param;

        public BtBool(bool value)
        {
            this.value = value;
            param = null;
        }

        public bool Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(param) || scope == null || !scope.TryGet(param, out var p)) return value;
            return p.BoolValue;
        }

        public static implicit operator BtBool(bool v) => new(v);
    }
}

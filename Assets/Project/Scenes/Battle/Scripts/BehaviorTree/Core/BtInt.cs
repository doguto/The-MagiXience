using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BtInt
    {
        public int value;
        public string param;

        public BtInt(int value)
        {
            this.value = value;
            param = null;
        }

        public int Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(param) || scope == null) return value;
            if (scope.TryGetVariable(param, out var v)) return Mathf.RoundToInt(v);
            if (!scope.TryGet(param, out var p)) return value;
            return p.Kind == TreeParameterKind.Float ? Mathf.RoundToInt(p.FloatValue) : p.IntValue;
        }

        public static implicit operator BtInt(int v) => new(v);

        public override string ToString() => string.IsNullOrEmpty(param) ? value.ToString() : "$" + param;

        public string ToFrameString() => string.IsNullOrEmpty(param) ? $"{value}f ({value / (float)BattleContext.FramesPerSecond:0.##}s)" : "$" + param;
    }
}

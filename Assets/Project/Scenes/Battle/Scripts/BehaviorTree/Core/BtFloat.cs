using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BtFloat
    {
        public float value;
        public string param;

        public BtFloat(float value)
        {
            this.value = value;
            param = null;
        }

        public float Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(param) || scope == null || !scope.TryGet(param, out var p)) return value;
            return p.Kind == TreeParameterKind.Int ? p.IntValue : p.FloatValue;
        }

        public static implicit operator BtFloat(float v) => new(v);

        public override string ToString() => string.IsNullOrEmpty(param) ? value.ToString("0.##") : "$" + param;

        public string ToFrameString() => string.IsNullOrEmpty(param) ? $"{value:0.##}f ({value / BattleContext.FramesPerSecond:0.##}s)" : "$" + param;
    }
}

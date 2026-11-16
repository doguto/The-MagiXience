using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum TreeParameterKind
    {
        Float,
        Int,
        Bool,
        Vector2,
        Object,
    }

    // ツリーが公開するパラメータ。SubTree 側では同じ型で上書き値を持つ。
    [Serializable]
    public class TreeParameter
    {
        [SerializeField] string name;
        [SerializeField] TreeParameterKind kind;
        [SerializeField] float floatValue;
        [SerializeField] int intValue;
        [SerializeField] bool boolValue;
        [SerializeField] Vector2 vector2Value;
        [SerializeField] UnityEngine.Object objectValue;
        [SerializeField, Tooltip("空でなければ親ツリーの同名パラメータを引き継ぐ")]
        string bindToParent;

        public string Name => name;
        public TreeParameterKind Kind => kind;
        public float FloatValue => floatValue;
        public int IntValue => intValue;
        public bool BoolValue => boolValue;
        public Vector2 Vector2Value => vector2Value;
        public UnityEngine.Object ObjectValue => objectValue;

        public TreeParameter() { }

        public TreeParameter(string name, TreeParameterKind kind)
        {
            this.name = name;
            this.kind = kind;
        }

        internal TreeParameter ResolveAgainst(TreeScope parentScope)
        {
            if (string.IsNullOrEmpty(bindToParent) || parentScope == null) return this;
            return parentScope.TryGet(bindToParent, out var p) ? p : this;
        }
    }

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
    }

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
            if (string.IsNullOrEmpty(param) || scope == null || !scope.TryGet(param, out var p)) return value;
            return p.Kind == TreeParameterKind.Float ? Mathf.RoundToInt(p.FloatValue) : p.IntValue;
        }

        public static implicit operator BtInt(int v) => new(v);

        public override string ToString() => string.IsNullOrEmpty(param) ? value.ToString() : "$" + param;

        public string ToFrameString() => string.IsNullOrEmpty(param) ? $"{value}f ({value / (float)BattleContext.FramesPerSecond:0.##}s)" : "$" + param;
    }

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

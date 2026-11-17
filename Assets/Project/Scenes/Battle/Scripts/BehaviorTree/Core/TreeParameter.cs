using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
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
}

using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // エディタのノードパレットに表示する説明。category を指定するとパレット上の分類を上書きする。
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NodeInfoAttribute : Attribute
    {
        public NodeInfoAttribute(string description, string category = null)
        {
            Description = description;
            Category = category;
        }

        public string Description { get; }
        public string Category { get; }
    }

    // エディタのノード上で、同じノードの別フィールドが指定値のときだけこのフィールドを表示する。
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowWhenAttribute : Attribute
    {
        public ShowWhenAttribute(string field, object value)
        {
            Field = field;
            Value = value;
        }

        public string Field { get; }
        public object Value { get; }
    }
}

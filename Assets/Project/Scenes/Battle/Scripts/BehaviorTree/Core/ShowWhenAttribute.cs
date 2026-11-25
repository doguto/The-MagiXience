using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
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

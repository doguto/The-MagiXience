using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 実行中のツリー 1 本ぶんのパラメータの値。SubTree ごとに子のスコープを作り、循環も検出する
    public sealed class TreeScope
    {
        readonly TreeScope parent;
        readonly BehaviorTreeAsset asset;
        readonly Dictionary<string, TreeParameter> values = new();

        TreeScope(TreeScope parent, BehaviorTreeAsset asset)
        {
            this.parent = parent;
            this.asset = asset;
        }

        public BehaviorTreeAsset Asset => asset;

        public static TreeScope Enter(TreeScope parent, BehaviorTreeAsset asset, IReadOnlyList<TreeParameter> overrides)
        {
            for (var s = parent; s != null; s = s.parent)
            {
                if (s.asset == asset)
                {
                    throw new InvalidOperationException($"[BehaviorTree] SubTree cycle detected: {DescribePath(parent)} -> {asset.name}");
                }
            }

            var scope = new TreeScope(parent, asset);
            foreach (var p in asset.Parameters)
            {
                if (!string.IsNullOrEmpty(p.Name)) scope.values[p.Name] = p;
            }

            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (string.IsNullOrEmpty(o.Name)) continue;
                    if (!scope.values.ContainsKey(o.Name))
                    {
                        Debug.LogWarning($"[BehaviorTree] Unknown parameter override: {asset.name}.{o.Name}");
                        continue;
                    }
                    scope.values[o.Name] = o.ResolveAgainst(parent);
                }
            }

            return scope;
        }

        public bool TryGet(string name, out TreeParameter parameter) => values.TryGetValue(name, out parameter);

        static string DescribePath(TreeScope scope)
        {
            var names = new List<string>();
            for (var s = scope; s != null; s = s.parent) names.Add(s.asset ? s.asset.name : "null");
            names.Reverse();
            return string.Join(" -> ", names.Where(n => n != null));
        }
    }

    // 雛形のノードを実行用に複製する。ノードと条件 (状態を持つもの) と、それを中に持つ入れ物だけを複製し、
    // 方向・弾の設定・移動などの読むだけのデータは雛形と共有する
    public static class NodeCloner
    {
        const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static readonly Func<object, object> ShallowCopy =
            (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>),
                typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic));

        static readonly Dictionary<Type, FieldInfo[]> cloneFields = new();
        static readonly Dictionary<Type, bool> holdsState = new();

        public static T Clone<T>(T source) where T : class => (T)Clone(source, new Dictionary<object, object>(ReferenceComparer.Instance));

        static object Clone(object source, Dictionary<object, object> map)
        {
            if (source == null) return null;
            if (map.TryGetValue(source, out var existing)) return existing;

            if (source is IList list && source.GetType().IsGenericType)
            {
                var copyList = (IList)Activator.CreateInstance(source.GetType(), list.Count);
                map[source] = copyList;
                foreach (var item in list) copyList.Add(Clone(item, map));
                return copyList;
            }

            if (source is Array array)
            {
                var copyArray = (Array)array.Clone();
                map[source] = copyArray;
                for (var i = 0; i < copyArray.Length; i++) copyArray.SetValue(Clone(array.GetValue(i), map), i);
                return copyArray;
            }

            if (!HoldsState(source.GetType())) return source;

            var copy = ShallowCopy(source);
            map[source] = copy;
            foreach (var field in FieldsToClone(source.GetType())) field.SetValue(copy, Clone(field.GetValue(source), map));
            return copy;
        }

        // 複製が要る型: ノード・条件と、それらを (入れ物越しにでも) 持つクラス
        static bool HoldsState(Type type)
        {
            if (typeof(Node).IsAssignableFrom(type) || typeof(Condition).IsAssignableFrom(type)) return true;
            if (type.IsValueType || type == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
            if (holdsState.TryGetValue(type, out var cached)) return cached;
            holdsState[type] = false;
            var result = FieldsToClone(type).Length > 0;
            holdsState[type] = result;
            return result;
        }

        static FieldInfo[] FieldsToClone(Type type)
        {
            if (cloneFields.TryGetValue(type, out var cached)) return cached;
            var fields = new List<FieldInfo>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(InstanceFields))
                {
                    if (field.IsNotSerialized) continue;
                    if (MayHoldState(field.FieldType)) fields.Add(field);
                }
            }
            return cloneFields[type] = fields.ToArray();
        }

        // 宣言型から判断する。基底型 (DirectionSource など) の派生がノードを持つことはない前提
        static bool MayHoldState(Type type)
        {
            if (type.IsArray) return MayHoldState(type.GetElementType());
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return MayHoldState(type.GetGenericArguments()[0]);
            if (type.IsAbstract && !typeof(Node).IsAssignableFrom(type) && !typeof(Condition).IsAssignableFrom(type)) return false;
            return HoldsState(type);
        }

        sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}

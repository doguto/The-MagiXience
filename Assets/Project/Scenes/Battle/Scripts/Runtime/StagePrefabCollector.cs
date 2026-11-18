using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ステージで使う Prefab を、アセットの参照をたどって集める (事前生成用)。
    // ノードの型ごとに分岐すると新しいノードで漏れるため、シリアライズされるフィールドを総当たりでたどる。
    // Prefab の中身 (子の BulletPool など) まではたどらない。
    public static class StagePrefabCollector
    {
        public sealed class Result
        {
            public readonly HashSet<BulletEntityPresenter> Bullets = new();
            public readonly HashSet<GameObject> Spawnables = new();
        }

        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        const int MaxDepth = 64;

        public static Result Collect(BattleStageAsset stage)
        {
            var result = new Result();
            if (stage != null) Visit(stage, result, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
            return result;
        }

        static void Visit(object value, Result result, HashSet<object> visited, int depth)
        {
            if (value == null || depth > MaxDepth) return;

            if (value is UnityEngine.Object unityObject)
            {
                if (unityObject == null || !visited.Add(unityObject)) return;
                switch (unityObject)
                {
                    case BulletEntityPresenter bullet:
                        result.Bullets.Add(bullet);
                        return;
                    case GameObject go:
                        if (go.TryGetComponent<BulletEntityPresenter>(out var asBullet)) result.Bullets.Add(asBullet);
                        else result.Spawnables.Add(go);
                        return;
                    case Component component:
                        result.Spawnables.Add(component.gameObject);
                        return;
                    case ScriptableObject:
                        VisitFields(unityObject, result, visited, depth);
                        return;
                    default:
                        return;
                }
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || type.IsPointer) return;

            if (value is IList list)
            {
                foreach (var item in list) Visit(item, result, visited, depth + 1);
                return;
            }

            if (!type.IsValueType && !visited.Add(value)) return;
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine")) return;
            VisitFields(value, result, visited, depth);
        }

        static void VisitFields(object owner, Result result, HashSet<object> visited, int depth)
        {
            for (var type = owner.GetType(); type != null && type != typeof(object) && type != typeof(ScriptableObject); type = type.BaseType)
            {
                foreach (var field in type.GetFields(Fields))
                {
                    if (!IsSerialized(field)) continue;
                    var fieldType = field.FieldType;
                    if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType == typeof(string)) continue;
                    Visit(field.GetValue(owner), result, visited, depth + 1);
                }
            }
        }

        static bool IsSerialized(FieldInfo field)
        {
            if (field.IsNotSerialized || field.IsStatic) return false;
            return field.IsPublic
                   || field.IsDefined(typeof(SerializeField), false)
                   || field.IsDefined(typeof(SerializeReference), false);
        }

        sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}

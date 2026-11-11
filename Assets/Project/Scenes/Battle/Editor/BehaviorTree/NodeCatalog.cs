using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public static class NodeCatalog
    {
        public static readonly string[] CategoryOrder = { "Composite", "Decorator", "Condition", "Action" };

        public sealed class Entry
        {
            public Type Type;
            public string Name;
            public string Category;
            public string Description;
        }

        static List<Entry> cache;

        public static IReadOnlyList<Entry> All => cache ??= TypeCache.GetTypesDerivedFrom<NodeDefinition>()
            .Where(t => !t.IsAbstract && !t.IsGenericType && t != typeof(SubTreeNodeDefinition))
            .Select(t =>
            {
                var info = t.GetCustomAttribute<NodeInfoAttribute>();
                return new Entry
                {
                    Type = t,
                    Name = BehaviorTreeNodeView.DisplayName(t),
                    Category = info?.Category ?? DefaultCategory(t),
                    Description = info?.Description ?? "",
                };
            })
            .OrderBy(e => Array.IndexOf(CategoryOrder, e.Category) is var i && i < 0 ? int.MaxValue : i)
            .ThenBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToList();

        public static string DefaultCategory(Type type)
        {
            if (typeof(CompositeNodeDefinition).IsAssignableFrom(type)) return "Composite";
            if (typeof(DecoratorNodeDefinition).IsAssignableFrom(type)) return "Decorator";
            if (type == typeof(CheckConditionNodeDefinition) || type == typeof(WaitUntilNodeDefinition)) return "Condition";
            return "Action";
        }

        public static string CategoryKey(NodeDefinition definition)
        {
            if (definition is SubTreeNodeDefinition) return "subtree";
            var info = definition.GetType().GetCustomAttribute<NodeInfoAttribute>();
            var category = info?.Category ?? DefaultCategory(definition.GetType());
            return category.Split('/')[0].ToLowerInvariant();
        }

        public static string Description(Type type) => type.GetCustomAttribute<NodeInfoAttribute>()?.Description ?? "";

        public static IEnumerable<BehaviorTreeAsset> SubTreeAssets() =>
            AssetDatabase.FindAssets("t:" + nameof(BehaviorTreeAsset))
                         .Select(g => AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(AssetDatabase.GUIDToAssetPath(g)))
                         .Where(a => a != null)
                         .OrderBy(a => a.name);
    }
}

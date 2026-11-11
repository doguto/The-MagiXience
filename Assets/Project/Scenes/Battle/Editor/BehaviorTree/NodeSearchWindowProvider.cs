using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public class NodeSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        Action<Type, Vector2> onSelected;

        public void Initialize(Action<Type, Vector2> callback) => onSelected = callback;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Create Node")) };

            var types = TypeCache.GetTypesDerivedFrom<NodeDefinition>()
                                 .Where(t => !t.IsAbstract && !t.IsGenericType)
                                 .ToList();

            foreach (var group in types.GroupBy(Category).OrderBy(g => CategoryOrder(g.Key)))
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent(group.Key), 1));
                foreach (var type in group.OrderBy(t => t.Name))
                {
                    tree.Add(new SearchTreeEntry(new GUIContent(BehaviorTreeNodeView.DisplayName(type))) { level = 2, userData = type });
                }
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is not Type type) return false;
            onSelected?.Invoke(type, context.screenMousePosition);
            return true;
        }

        static string Category(Type type)
        {
            var path = type.Namespace ?? "";
            var group = typeof(CompositeNodeDefinition).IsAssignableFrom(type) ? "Composite"
                : typeof(DecoratorNodeDefinition).IsAssignableFrom(type) ? "Decorator"
                : "Action";
            var lastSegment = path.Split('.').LastOrDefault();
            return lastSegment is "BehaviorTree" or null or "" ? group : $"{group}/{lastSegment}";
        }

        static int CategoryOrder(string category) =>
            category.StartsWith("Composite") ? 0 : category.StartsWith("Decorator") ? 1 : 2;
    }
}

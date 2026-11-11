using System;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 左ペインのノード一覧。項目をキャンバスへドラッグするとノードを作成する。
    public class NodePaletteView : VisualElement
    {
        public const string NodeTypeKey = "BehaviorTree.NodeType";
        public const string SubTreeKey = "BehaviorTree.SubTree";

        readonly ToolbarSearchField searchField;
        readonly ScrollView scroll;
        readonly Label descriptionLabel;
        Func<BehaviorTreeAsset> currentAsset;

        public NodePaletteView()
        {
            AddToClassList("bt-palette");

            searchField = new ToolbarSearchField();
            searchField.AddToClassList("bt-palette__search");
            searchField.RegisterValueChangedCallback(_ => Rebuild());
            Add(searchField);

            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("bt-palette__scroll");
            Add(scroll);

            descriptionLabel = new Label("");
            descriptionLabel.AddToClassList("bt-palette__description");
            Add(descriptionLabel);

            Rebuild();
        }

        public void SetAssetProvider(Func<BehaviorTreeAsset> provider)
        {
            currentAsset = provider;
            Rebuild();
        }

        public void Rebuild()
        {
            scroll.Clear();
            var filter = searchField.value?.Trim() ?? "";

            foreach (var group in NodeCatalog.All.GroupBy(e => e.Category))
            {
                var items = group.Where(e => Matches(filter, e.Name, e.Description)).ToList();
                if (items.Count == 0) continue;

                var foldout = CreateFoldout(group.Key);
                foreach (var entry in items)
                {
                    foldout.Add(CreateItem(entry.Name, entry.Description, NodeCatalog.CategoryKey((NodeDefinition)Activator.CreateInstance(entry.Type)),
                        () => StartDrag(NodeTypeKey, entry.Type, entry.Name)));
                }
                scroll.Add(foldout);
            }

            var self = currentAsset?.Invoke();
            var trees = NodeCatalog.SubTreeAssets().Where(t => t != self && Matches(filter, t.name, "")).ToList();
            if (trees.Count > 0)
            {
                var foldout = CreateFoldout("SubTrees");
                foreach (var tree in trees)
                {
                    var path = AssetDatabase.GetAssetPath(tree);
                    foldout.Add(CreateItem(tree.name, path, "subtree", () => StartDrag(SubTreeKey, tree, tree.name)));
                }
                scroll.Add(foldout);
            }
        }

        static bool Matches(string filter, string name, string description) =>
            string.IsNullOrEmpty(filter)
            || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
            || description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        static Foldout CreateFoldout(string title)
        {
            var foldout = new Foldout { text = title, value = true, viewDataKey = "bt-palette-" + title };
            foldout.AddToClassList("bt-palette__group");
            return foldout;
        }

        VisualElement CreateItem(string name, string description, string categoryKey, Action beginDrag)
        {
            var item = new VisualElement { tooltip = description };
            item.AddToClassList("bt-palette__item");
            item.AddToClassList("bt-node--" + categoryKey);

            var accent = new VisualElement();
            accent.AddToClassList("bt-marker");
            item.Add(accent);

            var label = new Label(name);
            label.AddToClassList("bt-palette__item-label");
            item.Add(label);

            item.RegisterCallback<MouseEnterEvent>(_ => descriptionLabel.text = $"{name}\n{description}");

            var pressed = false;
            var pressPosition = Vector2.zero;
            item.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                pressed = true;
                pressPosition = e.position;
            });
            item.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!pressed || ((Vector2)e.position - pressPosition).sqrMagnitude < 16f) return;
                pressed = false;
                beginDrag();
            });
            item.RegisterCallback<PointerUpEvent>(_ => pressed = false);
            item.RegisterCallback<PointerLeaveEvent>(_ => pressed = false);
            return item;
        }

        static void StartDrag(string key, object data, string title)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(key, data);
            if (data is UnityEngine.Object obj) DragAndDrop.objectReferences = new[] { obj };
            else DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
            DragAndDrop.StartDrag(title);
        }
    }
}

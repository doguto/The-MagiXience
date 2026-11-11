using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // アセット上の定義を正とし、構造を変更したら定義を書き換えてから全体を描き直す。
    public class BehaviorTreeGraphView : GraphView
    {
        readonly EditorWindow window;
        readonly NodeSearchWindowProvider searchProvider;
        readonly Dictionary<NodeDefinition, BehaviorTreeNodeView> views = new();
        BehaviorTreeAsset asset;
        SerializedObject serializedObject;
        BehaviorTreeNodeView rootView;
        bool isReloading;

        public BehaviorTreeGraphView(EditorWindow window)
        {
            this.window = window;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            searchProvider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
            searchProvider.hideFlags = HideFlags.HideAndDontSave;
            searchProvider.Initialize(CreateNodeAtScreenPosition);
            nodeCreationRequest = ctx => SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), searchProvider);

            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = SerializeForCopy;
            canPasteSerializedData = data => asset != null && !string.IsNullOrEmpty(data);
            unserializeAndPaste = Paste;
        }

        public void Load(BehaviorTreeAsset target)
        {
            asset = target;
            Reload();
            schedule.Execute(() => FrameAll());
        }

        public void Reload()
        {
            isReloading = true;
            DeleteElements(graphElements.ToList());
            views.Clear();
            rootView = null;
            isReloading = false;

            if (asset == null) return;

            serializedObject = new SerializedObject(asset);
            var paths = CollectPropertyPaths(serializedObject);

            rootView = new BehaviorTreeNodeView();
            rootView.SetPosition(new Rect(0, -150, 120, 60));
            AddElement(rootView);

            foreach (var def in AllDefinitions())
            {
                var prop = paths.TryGetValue(def, out var path) ? serializedObject.FindProperty(path) : null;
                var view = new BehaviorTreeNodeView(def, prop);
                view.SetPosition(new Rect(def.GraphPosition, Vector2.zero));
                views[def] = view;
                AddElement(view);
            }

            isReloading = true;
            if (asset.Root != null && views.TryGetValue(asset.Root, out var rootChild))
            {
                AddElement(rootView.Output.ConnectTo(rootChild.Input));
            }

            foreach (var (def, view) in views)
            {
                foreach (var child in def.Children)
                {
                    if (child != null && views.TryGetValue(child, out var childView))
                    {
                        AddElement(view.Output.ConnectTo(childView.Input));
                    }
                }
            }
            isReloading = false;
        }

        public void RefreshRuntimeState()
        {
            foreach (var (def, view) in views)
            {
                view.SetRuntimeActive(BehaviorTreeDebug.WasTickedRecently(def.Guid));
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) =>
            ports.ToList().Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is BehaviorTreeNodeView { Definition: SubTreeNodeDefinition { Tree: { } tree } })
            {
                evt.menu.AppendAction("Open SubTree", _ => BehaviorTreeEditorWindow.Open(tree));
            }
        }

        IEnumerable<NodeDefinition> AllDefinitions()
        {
            var visited = new HashSet<NodeDefinition>();
            var stack = new Stack<NodeDefinition>();
            if (asset.Root != null) stack.Push(asset.Root);
            foreach (var d in asset.DetachedNodes) if (d != null) stack.Push(d);

            while (stack.Count > 0)
            {
                var def = stack.Pop();
                if (!visited.Add(def)) continue;
                yield return def;
                foreach (var child in def.Children) if (child != null) stack.Push(child);
            }
        }

        static Dictionary<NodeDefinition, string> CollectPropertyPaths(SerializedObject so)
        {
            var result = new Dictionary<NodeDefinition, string>();
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ManagedReference) continue;
                if (it.managedReferenceValue is NodeDefinition def && !result.ContainsKey(def))
                {
                    result[def] = it.propertyPath;
                }
            }
            return result;
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isReloading || asset == null) return change;

            var structural = false;
            Undo.RecordObject(asset, "Edit Behavior Tree");

            if (change.elementsToRemove != null)
            {
                foreach (var edge in change.elementsToRemove.OfType<Edge>())
                {
                    Disconnect(edge);
                    structural = true;
                }

                foreach (var view in change.elementsToRemove.OfType<BehaviorTreeNodeView>())
                {
                    if (view.IsRoot) continue;
                    RemoveDefinition(view.Definition);
                    structural = true;
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    Connect(edge);
                    structural = true;
                }
            }

            if (change.movedElements != null)
            {
                foreach (var view in change.movedElements.OfType<BehaviorTreeNodeView>())
                {
                    if (view.IsRoot) continue;
                    view.Definition.GraphPosition = view.GetPosition().position;
                }
                SortAllChildrenByPosition();
            }

            EditorUtility.SetDirty(asset);
            if (structural)
            {
                change.edgesToCreate?.Clear();
                schedule.Execute(Reload);
            }

            return change;
        }

        void Connect(Edge edge)
        {
            if (edge.output.node is not BehaviorTreeNodeView parent || edge.input.node is not BehaviorTreeNodeView child) return;

            DetachFromCurrentParent(child.Definition);
            asset.DetachedNodes.Remove(child.Definition);

            if (parent.IsRoot)
            {
                if (asset.Root != null) asset.DetachedNodes.Add(asset.Root);
                asset.Root = child.Definition;
                return;
            }

            var children = parent.Definition.Children.Where(c => c != null).ToList();
            if (parent.Definition.ChildCapacity == NodeChildCapacity.Single)
            {
                asset.DetachedNodes.AddRange(children);
                children.Clear();
            }
            children.Add(child.Definition);
            parent.Definition.SetChildren(SortByPosition(children));
        }

        void Disconnect(Edge edge)
        {
            if (edge.input.node is not BehaviorTreeNodeView child) return;
            if (DetachFromCurrentParent(child.Definition)) asset.DetachedNodes.Add(child.Definition);
        }

        bool DetachFromCurrentParent(NodeDefinition target)
        {
            if (asset.Root == target)
            {
                asset.Root = null;
                return true;
            }

            foreach (var def in AllDefinitions().ToList())
            {
                var children = def.Children.ToList();
                if (!children.Remove(target)) continue;
                def.SetChildren(children);
                return true;
            }
            return false;
        }

        void RemoveDefinition(NodeDefinition target)
        {
            DetachFromCurrentParent(target);
            asset.DetachedNodes.Remove(target);
            foreach (var child in target.Children)
            {
                if (child != null) asset.DetachedNodes.Add(child);
            }
            target.SetChildren(Array.Empty<NodeDefinition>());
        }

        void SortAllChildrenByPosition()
        {
            foreach (var def in AllDefinitions().ToList())
            {
                if (def.ChildCapacity != NodeChildCapacity.Multiple) continue;
                var sorted = SortByPosition(def.Children.ToList());
                if (!sorted.SequenceEqual(def.Children)) def.SetChildren(sorted);
            }
        }

        // 子の実行順は左から右。Timeline 等の子ごとの設定値もこの順に対応する。
        static List<NodeDefinition> SortByPosition(List<NodeDefinition> children) =>
            children.OrderBy(c => c.GraphPosition.x).ToList();

        void CreateNodeAtScreenPosition(Type type, Vector2 screenPosition)
        {
            if (asset == null) return;

            var windowPos = screenPosition - window.position.position;
            var localPos = contentViewContainer.WorldToLocal(windowPos);
            Undo.RecordObject(asset, "Create Behavior Tree Node");
            var def = (NodeDefinition)Activator.CreateInstance(type);
            def.GraphPosition = localPos;
            if (asset.Root == null) asset.Root = def;
            else asset.DetachedNodes.Add(def);
            EditorUtility.SetDirty(asset);
            Reload();
        }

        string SerializeForCopy(IEnumerable<GraphElement> elements)
        {
            var defs = elements.OfType<BehaviorTreeNodeView>().Where(v => !v.IsRoot).Select(v => v.Definition).ToList();
            if (defs.Count == 0) return null;
            var holder = new CopyHolder { nodes = defs.Select(d => new CopyEntry { node = d }).ToList() };
            return JsonUtility.ToJson(holder);
        }

        void Paste(string operationName, string data)
        {
            var holder = JsonUtility.FromJson<CopyHolder>(data);
            if (holder?.nodes == null) return;

            Undo.RecordObject(asset, "Paste Behavior Tree Nodes");
            foreach (var entry in holder.nodes)
            {
                if (entry.node == null) continue;
                // 子は別エントリとして貼り付けるため、ここでは単体ノードとして扱う
                entry.node.SetChildren(Array.Empty<NodeDefinition>());
                entry.node.RegenerateGuid();
                entry.node.GraphPosition += new Vector2(40, 40);
                asset.DetachedNodes.Add(entry.node);
            }
            EditorUtility.SetDirty(asset);
            Reload();
        }

        [Serializable]
        class CopyHolder
        {
            public List<CopyEntry> nodes;
        }

        [Serializable]
        class CopyEntry
        {
            [SerializeReference] public NodeDefinition node;
        }
    }
}

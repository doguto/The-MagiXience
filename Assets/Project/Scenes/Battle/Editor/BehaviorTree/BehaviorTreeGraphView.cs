using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UniRx;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using BtNode = Project.Scenes.Battle.Scripts.BehaviorTree.Node;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // アセット上の定義を正とし、構造を変更したら定義を書き換えてから全体を描き直す。
    public class BehaviorTreeGraphView : GraphView
    {
        readonly EditorWindow window;
        readonly NodeSearchWindowProvider searchProvider;
        readonly Dictionary<BtNode, BehaviorTreeNodeView> views = new();
        BehaviorTreeAsset asset;
        SerializedObject serializedObject;
        BehaviorTreeNodeView rootView;
        bool isReloading;
        readonly MiniMap miniMap;

        // 選択が変わったとき、または選択中のノードの入力欄を作り直すべきとき (構造変更・パラメータ改名) に 1 フレームにまとめて通知する
        readonly Subject<Unit> selectionChanged = new();
        public IObservable<Unit> OnSelectionChanged => selectionChanged;
        bool selectionChangeQueued;

        public IReadOnlyList<BehaviorTreeNodeView> SelectedNodes =>
            selection.OfType<BehaviorTreeNodeView>().Where(v => !v.IsRoot).ToList();

        void QueueSelectionChanged()
        {
            if (selectionChangeQueued) return;
            selectionChangeQueued = true;
            schedule.Execute(() =>
            {
                selectionChangeQueued = false;
                selectionChanged.OnNext(Unit.Default);
            });
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            QueueSelectionChanged();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            QueueSelectionChanged();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            QueueSelectionChanged();
        }

        public void ToggleMiniMap() =>
            miniMap.style.display = miniMap.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;

        public void FrameWholeGraph()
        {
            ClearSelection();
            FrameAll();
        }

        public BehaviorTreeGraphView(EditorWindow window)
        {
            this.window = window;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            AddToClassList("bt-graph");
            var styleSheet = LoadStyleSheet();
            if (styleSheet != null) styleSheets.Add(styleSheet);

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 10, 200, 140));
            miniMap.style.display = DisplayStyle.None;
            Add(miniMap);

            searchProvider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
            searchProvider.hideFlags = HideFlags.HideAndDontSave;
            searchProvider.Initialize(CreateNodeAtScreenPosition);
            nodeCreationRequest = ctx =>
            {
                pendingPort = null;
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), searchProvider);
            };

            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = SerializeForCopy;
            canPasteSerializedData = data => asset != null && !string.IsNullOrEmpty(data);
            unserializeAndPaste = Paste;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(_ => SetDropTarget(null));
            RegisterCallback<DragExitedEvent>(_ => SetDropTarget(null));
        }

        BehaviorTreeNodeView dropTarget;

        static bool TryGetDraggedItem(out Type nodeType, out BehaviorTreeAsset subTree)
        {
            nodeType = DragAndDrop.GetGenericData(NodePaletteView.NodeTypeKey) as Type;
            subTree = DragAndDrop.GetGenericData(NodePaletteView.SubTreeKey) as BehaviorTreeAsset;
            if (nodeType == null && subTree == null)
            {
                subTree = DragAndDrop.objectReferences.OfType<BehaviorTreeAsset>().FirstOrDefault();
            }
            return nodeType != null || subTree != null;
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (asset == null || !TryGetDraggedItem(out _, out var subTree) || subTree == asset)
            {
                SetDropTarget(null);
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            SetDropTarget(FindParentCandidate(evt.mousePosition));
            evt.StopPropagation();
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            var parent = dropTarget;
            SetDropTarget(null);
            if (asset == null || !TryGetDraggedItem(out var nodeType, out var subTree) || subTree == asset) return;

            DragAndDrop.AcceptDrag();
            var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            BtNode def;
            if (nodeType != null)
            {
                def = (BtNode)Activator.CreateInstance(nodeType);
            }
            else
            {
                def = new SubTree { Tree = subTree };
            }

            CreateNode(def, localPos, parent);
            evt.StopPropagation();
        }

        // ドロップ位置にある、子を受け付けられるノード(またはルート)
        BehaviorTreeNodeView FindParentCandidate(Vector2 worldMouse)
        {
            foreach (var view in views.Values.Append(rootView))
            {
                if (view == null || view.Output == null) continue;
                if (!view.worldBound.Contains(worldMouse)) continue;
                return view;
            }
            return null;
        }

        void SetDropTarget(BehaviorTreeNodeView target)
        {
            if (dropTarget == target) return;
            dropTarget?.RemoveFromClassList("bt-drop-target");
            dropTarget = target;
            dropTarget?.AddToClassList("bt-drop-target");
        }

        public void Load(BehaviorTreeAsset target)
        {
            asset = target;
            frameAfterLayout = true;
            Reload();
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
            pathCache = null;

            rootView = new BehaviorTreeNodeView(OnDropOutsidePort);
            rootView.SetPosition(new Rect(0, -150, 120, 60));
            rootView.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
            AddElement(rootView);

            Sync();
        }

        // パラメータの改名などで要約だけが変わった場合に使う
        public void RefreshTexts()
        {
            serializedObject?.Update();
            foreach (var view in views.Values) view.RefreshTexts();
            QueueSelectionChanged();
        }

        Dictionary<BtNode, string> pathCache;

        // 入力欄の対象プロパティ。構造が変わるとパスがずれるため、必要になった時点で引き直す
        SerializedProperty FindPropertyFor(BtNode def)
        {
            if (serializedObject == null) return null;
            if (pathCache == null)
            {
                serializedObject.Update();
                pathCache = CollectPropertyPaths(serializedObject);
            }
            return pathCache.TryGetValue(def, out var path) ? serializedObject.FindProperty(path) : null;
        }

        // 定義を正として、ノードの増減と線の過不足だけを画面に反映する (全体の作り直しは重いため)
        void Sync()
        {
            if (asset == null || rootView == null) return;
            // 定義を直接書き換えた後なので、古い SerializedObject の値で上書きしないよう読み直す
            serializedObject?.Update();
            pathCache = null;
            isReloading = true;

            var alive = new HashSet<BtNode>(AllNodes());
            var removed = views.Keys.Where(d => !alive.Contains(d)).ToList();
            foreach (var def in removed)
            {
                var view = views[def];
                DeleteElements(view.Input.connections.Concat(view.Output?.connections ?? Enumerable.Empty<Edge>()).ToList());
                RemoveElement(view);
                views.Remove(def);
            }

            foreach (var def in alive)
            {
                if (views.ContainsKey(def)) continue;
                var target = def;
                var view = new BehaviorTreeNodeView(def, () => FindPropertyFor(target), OnDropOutsidePort);
                view.OnValuesChanged.Subscribe(_ => RefreshOrderBadges());
                view.SetPosition(new Rect(def.GraphPosition, Vector2.zero));
                view.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
                views[def] = view;
                AddElement(view);
            }

            var wanted = new HashSet<(Port, Port)>();
            if (asset.Root != null && views.TryGetValue(asset.Root, out var rootChild)) wanted.Add((rootView.Output, rootChild.Input));
            foreach (var (def, view) in views)
            {
                foreach (var child in def.Children)
                {
                    if (child != null && views.TryGetValue(child, out var childView)) wanted.Add((view.Output, childView.Input));
                }
            }

            var stale = new List<GraphElement>();
            foreach (var edge in edges.ToList())
            {
                if (!wanted.Remove((edge.output, edge.input))) stale.Add(edge);
            }
            if (stale.Count > 0) DeleteElements(stale);

            foreach (var (output, input) in wanted)
            {
                var edge = output.ConnectTo<BehaviorTreeEdge>(input);
                AddElement(edge);
                // 線がポートより手前にあるとポートを掴めないため、線を背面に回す
                edge.SendToBack();
            }

            isReloading = false;
            RefreshOrderBadges();
            RequestLayout();

            QueueSelectionChanged();
        }

        void RefreshOrderBadges()
        {
            foreach (var view in views.Values) view.SetOrder(0);
            foreach (var def in views.Keys)
            {
                if (def.ChildCapacity != NodeChildCapacity.Multiple) continue;
                var order = 1;
                var showTime = def is Sequence && def.Children.Any(c => c is Wait);
                int? time = 0;
                foreach (var child in def.Children)
                {
                    if (child != null && views.TryGetValue(child, out var childView))
                        childView.SetOrder(order++, showTime ? time : null);
                    time = NextTime(time, child);
                }
            }
        }

        // Sequence の子が開始から何フレーム目に動くか。時間の読めない子より後ろは表示しない
        static int? NextTime(int? time, BtNode child)
        {
            if (time == null || child == null) return time;
            if (child is Wait wait)
                return string.IsNullOrEmpty(wait.Frames.param) ? time + Mathf.Max(0, wait.Frames.value) : null;
            return IsInstant(child) ? time : null;
        }

        // 実行したフレームのうちに Success を返すノード
        public static bool IsInstant(BtNode node) =>
            node is ActionNode
            && node is not (SubTree or Wait or Scripts.BehaviorTree.WaitUntil or Move or MoveTrailFan or MovePath or PlaceAlongPath or Wave or BossBattle);

        const float HorizontalGap = 40f;
        const float VerticalGap = 70f;

        bool layoutPending;
        bool frameAfterLayout;

        // 位置は常に自動で決める。ノードの大きさは描画後にしか分からないため、大きさが変わるたびに並べ直す
        void OnNodeGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size != evt.newRect.size) RequestLayout();
        }

        void RequestLayout()
        {
            if (layoutPending) return;
            layoutPending = true;
            schedule.Execute(() =>
            {
                layoutPending = false;
                Layout();
                if (!frameAfterLayout) return;
                frameAfterLayout = false;
                FrameWholeGraph();
            });
        }

        // 部分木の幅を丸ごと確保すると、深い段だけが広い部分木の隣が無駄に空くため、
        // 高さが重なるノード同士だけを比べて左右に詰める (Reingold–Tilford と同じ考え方)
        void Layout()
        {
            if (asset == null || rootView == null) return;
            var rootSize = SizeOf(rootView);
            rootView.SetPosition(new Rect(new Vector2(-rootSize.x / 2f, 0f), rootSize));

            var tops = new List<BtNode>();
            if (asset.Root != null && views.ContainsKey(asset.Root)) tops.Add(asset.Root);
            tops.AddRange(asset.DetachedNodes.Where(d => d != null && views.ContainsKey(d)));
            if (tops.Count == 0) return;
            var plan = new Dictionary<BtNode, (float offset, float y)>();
            var y0 = rootSize.y + VerticalGap;
            var shapes = tops.Select(t => Measure(t, y0, plan)).ToList();
            var offsets = Pack(shapes, HorizontalGap * 3f);
            var changed = false;
            for (var i = 0; i < tops.Count; i++) changed |= Apply(tops[i], offsets[i] - offsets[0], plan);
            if (changed) EditorUtility.SetDirty(asset);
        }

        // 部分木が占める矩形の一覧を、自分の中心 x を 0 として返す。子の相対位置は plan に記録する
        List<Rect> Measure(BtNode def, float y, Dictionary<BtNode, (float offset, float y)> plan)
        {
            var size = SizeOf(views[def]);
            var children = def.Children.Where(c => c != null && views.ContainsKey(c)).ToList();
            var childY = y + size.y + VerticalGap;
            var shapes = children.Select(c => Measure(c, childY, plan)).ToList();
            var offsets = Pack(shapes, HorizontalGap);
            if (children.Count > 0)
            {
                var mid = (offsets[0] + offsets[^1]) / 2f;
                for (var i = 0; i < offsets.Count; i++) offsets[i] -= mid;
            }

            var result = new List<Rect> { new(-size.x / 2f, y, size.x, size.y) };
            for (var i = 0; i < children.Count; i++)
            {
                plan[children[i]] = (offsets[i], childY);
                result.AddRange(shapes[i].Select(r => new Rect(r.x + offsets[i], r.y, r.width, r.height)));
            }
            plan[def] = (plan.TryGetValue(def, out var p) ? p.offset : 0f, y);
            return result;
        }

        // 左から順に、高さが重なる矩形とだけ gap を空けて詰めた各中心 x を返す
        static List<float> Pack(List<List<Rect>> shapes, float gap)
        {
            var offsets = new List<float>();
            var placed = new List<Rect>();
            foreach (var shape in shapes)
            {
                var x = offsets.Count == 0 ? 0f : float.MinValue;
                foreach (var b in shape)
                foreach (var a in placed)
                {
                    if (a.yMin < b.yMax && b.yMin < a.yMax) x = Mathf.Max(x, a.xMax + gap - b.xMin);
                }
                if (x == float.MinValue) x = offsets[^1];
                offsets.Add(x);
                placed.AddRange(shape.Select(r => new Rect(r.x + x, r.y, r.width, r.height)));
            }
            return offsets;
        }

        // 子の順番は GraphPosition.x で決まるため、表示だけでなく定義側の位置も揃える
        bool Apply(BtNode def, float centerX, Dictionary<BtNode, (float offset, float y)> plan)
        {
            var view = views[def];
            var size = SizeOf(view);
            var pos = new Vector2(Mathf.Round(centerX - size.x / 2f), Mathf.Round(plan[def].y));
            var changed = def.GraphPosition != pos;
            def.GraphPosition = pos;
            view.SetPosition(new Rect(pos, size));
            foreach (var c in def.Children)
            {
                if (c != null && views.ContainsKey(c)) changed |= Apply(c, centerX + plan[c].offset, plan);
            }
            return changed;
        }

        static Vector2 SizeOf(GraphElement element)
        {
            var r = element.layout;
            var w = float.IsNaN(r.width) || r.width < 1f ? 220f : r.width;
            var h = float.IsNaN(r.height) || r.height < 1f ? 80f : r.height;
            return new Vector2(w, h);
        }

        public static StyleSheet LoadStyleSheet()
        {
            foreach (var guid in AssetDatabase.FindAssets("BehaviorTreeEditor t:StyleSheet"))
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guid));
                if (sheet != null) return sheet;
            }
            Debug.LogWarning("[BehaviorTreeEditor] BehaviorTreeEditor.uss not found.");
            return null;
        }

        // パラメータを参照している値欄を含むノードを選択して表示する
        public void SelectNodesUsingParameter(string parameterName)
        {
            if (asset == null || serializedObject == null) return;
            serializedObject.Update();
            var refs = ParameterUtility.FindReferencePaths(serializedObject, parameterName);
            var paths = CollectPropertyPaths(serializedObject);

            ClearSelection();
            foreach (var (def, view) in views)
            {
                if (!paths.TryGetValue(def, out var nodePath)) continue;
                var prefix = nodePath + ".";
                // 子ノード配下の参照は子自身に属するので除外する
                var own = refs.Any(r => r.StartsWith(prefix) && !paths.Any(kv => kv.Key != def && kv.Value.StartsWith(prefix) && r.StartsWith(kv.Value + ".")));
                if (own) AddToSelection(view);
            }

            if (selection.Count > 0) FrameSelection();
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
            if (evt.target is BehaviorTreeNodeView { TreeNode: SubTree { Tree: { } tree } })
            {
                evt.menu.AppendAction("Open SubTree", _ => BehaviorTreeEditorWindow.Open(tree));
            }

            if (evt.target is BehaviorTreeNodeView { TreeNode: PlayScenario scenario })
            {
                var missing = scenario.MissingCommands().ToList();
                evt.menu.AppendAction("会話のコマンドから OnCommand を作る", _ => AddOnCommands(scenario, missing),
                    missing.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        // 子の並びは x 座標で決まるので、既存の子の右側へ順に置く
        void AddOnCommands(PlayScenario scenario, List<string> commands)
        {
            if (commands.Count == 0) return;
            Undo.RecordObject(asset, "Add OnCommand Nodes");

            var children = scenario.Children.ToList();
            var x = children.Count > 0 ? children.Max(c => c.GraphPosition.x) : scenario.GraphPosition.x - HorizontalGap;
            var y = scenario.GraphPosition.y + VerticalGap + 80f;
            foreach (var command in commands)
            {
                x += 220f;
                children.Add(new OnCommand { Command = command, GraphPosition = new Vector2(x, y) });
            }
            scenario.SetChildren(children);
            EditorUtility.SetDirty(asset);
            Reload();
        }

        IEnumerable<BtNode> AllNodes()
        {
            var visited = new HashSet<BtNode>();
            var stack = new Stack<BtNode>();
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

        static Dictionary<BtNode, string> CollectPropertyPaths(SerializedObject so)
        {
            var result = new Dictionary<BtNode, string>();
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ManagedReference) continue;
                if (it.managedReferenceValue is BtNode def && !result.ContainsKey(def))
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
                    RemoveNode(view.TreeNode);
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
                    view.TreeNode.GraphPosition = view.GetPosition().position;
                }
                // ドラッグは兄弟の並べ替えにだけ使い、離したら整列し直して元の段に戻す
                SortAllChildrenByPosition();
                RefreshOrderBadges();
                RequestLayout();
            }

            EditorUtility.SetDirty(asset);
            if (structural)
            {
                change.edgesToCreate?.Clear();
                schedule.Execute(Sync);
            }

            return change;
        }

        void Connect(Edge edge)
        {
            if (edge.output.node is not BehaviorTreeNodeView parent || edge.input.node is not BehaviorTreeNodeView child) return;

            DetachFromCurrentParent(child.TreeNode);
            asset.DetachedNodes.Remove(child.TreeNode);

            if (parent.IsRoot)
            {
                if (asset.Root != null) asset.DetachedNodes.Add(asset.Root);
                asset.Root = child.TreeNode;
                return;
            }

            var children = parent.TreeNode.Children.Where(c => c != null).ToList();
            if (parent.TreeNode.ChildCapacity == NodeChildCapacity.Single)
            {
                asset.DetachedNodes.AddRange(children);
                children.Clear();
            }
            children.Add(child.TreeNode);
            parent.TreeNode.SetChildren(SortByPosition(children));
        }

        void Disconnect(Edge edge)
        {
            if (edge.input.node is not BehaviorTreeNodeView child) return;
            if (DetachFromCurrentParent(child.TreeNode)) asset.DetachedNodes.Add(child.TreeNode);
        }

        bool DetachFromCurrentParent(BtNode target)
        {
            if (asset.Root == target)
            {
                asset.Root = null;
                return true;
            }

            foreach (var def in AllNodes().ToList())
            {
                var children = def.Children.ToList();
                if (!children.Remove(target)) continue;
                def.SetChildren(children);
                return true;
            }
            return false;
        }

        void RemoveNode(BtNode target)
        {
            DetachFromCurrentParent(target);
            asset.DetachedNodes.Remove(target);
            foreach (var child in target.Children)
            {
                if (child != null) asset.DetachedNodes.Add(child);
            }
            target.SetChildren(Array.Empty<BtNode>());
        }

        void SortAllChildrenByPosition()
        {
            foreach (var def in AllNodes().ToList())
            {
                if (def.ChildCapacity != NodeChildCapacity.Multiple) continue;
                var sorted = SortByPosition(def.Children.ToList());
                if (!sorted.SequenceEqual(def.Children)) def.SetChildren(sorted);
            }
        }

        // 子の実行順は左から右。Timeline 等の子ごとの設定値もこの順に対応する。
        static List<BtNode> SortByPosition(List<BtNode> children) =>
            children.OrderBy(c => c.GraphPosition.x).ToList();

        Port pendingPort;

        // 線を何もない所で離したら検索ウィンドウを開き、選んだノードをその線の先に接続する
        void OnDropOutsidePort(Port origin, Vector2 worldPosition)
        {
            if (asset == null || origin == null) return;
            pendingPort = origin;
            var screen = worldPosition + window.position.position;
            SearchWindow.Open(new SearchWindowContext(screen), searchProvider);
        }

        void CreateNodeAtScreenPosition(Type type, Vector2 screenPosition)
        {
            if (asset == null) return;
            var windowPos = screenPosition - window.position.position;
            var localPos = contentViewContainer.WorldToLocal(windowPos);
            var def = (BtNode)Activator.CreateInstance(type);

            var port = pendingPort;
            pendingPort = null;
            if (port?.node is BehaviorTreeNodeView owner && port.direction == Direction.Output)
            {
                CreateNode(def, localPos, owner);
                return;
            }

            CreateNode(def, localPos, null);
            if (port?.node is BehaviorTreeNodeView child && port.direction == Direction.Input
                && def.ChildCapacity != NodeChildCapacity.None && !child.IsRoot)
            {
                Undo.RecordObject(asset, "Connect Behavior Tree Node");
                DetachFromCurrentParent(child.TreeNode);
                asset.DetachedNodes.Remove(child.TreeNode);
                if (asset.Root == null) asset.Root = def;
                else if (asset.Root != def && !asset.DetachedNodes.Contains(def)) asset.DetachedNodes.Add(def);
                def.SetChildren(new[] { child.TreeNode });
                def.GraphPosition = child.TreeNode.GraphPosition + new Vector2(0, -(SizeOf(child).y + VerticalGap + 60f));
                EditorUtility.SetDirty(asset);
                Sync();
            }
        }

        // parent を指定した場合はその最後の子として接続し、親の下に配置する
        void CreateNode(BtNode def, Vector2 localPos, BehaviorTreeNodeView parent)
        {
            Undo.RecordObject(asset, "Create Behavior Tree Node");
            def.GraphPosition = localPos;

            if (parent != null && parent.IsRoot)
            {
                if (asset.Root != null) asset.DetachedNodes.Add(asset.Root);
                asset.Root = def;
                def.GraphPosition = BelowOf(parent, null);
            }
            else if (parent != null)
            {
                var children = parent.TreeNode.Children.Where(c => c != null).ToList();
                if (parent.TreeNode.ChildCapacity == NodeChildCapacity.Single)
                {
                    asset.DetachedNodes.AddRange(children);
                    children.Clear();
                }
                def.GraphPosition = BelowOf(parent, children.LastOrDefault());
                children.Add(def);
                parent.TreeNode.SetChildren(children);
            }
            else if (asset.Root == null)
            {
                asset.Root = def;
            }
            else
            {
                asset.DetachedNodes.Add(def);
            }

            EditorUtility.SetDirty(asset);
            Sync();
            if (views.TryGetValue(def, out var created))
            {
                ClearSelection();
                AddToSelection(created);
            }
        }

        // 親の真下、既存の子がいればその右隣
        Vector2 BelowOf(BehaviorTreeNodeView parent, BtNode lastChild)
        {
            var p = parent.GetPosition();
            var y = p.y + SizeOf(parent).y + VerticalGap;
            if (lastChild != null && views.TryGetValue(lastChild, out var lastView))
            {
                return new Vector2(lastChild.GraphPosition.x + SizeOf(lastView).x + HorizontalGap, lastChild.GraphPosition.y);
            }
            return new Vector2(p.x, y);
        }

        string SerializeForCopy(IEnumerable<GraphElement> elements)
        {
            var defs = elements.OfType<BehaviorTreeNodeView>().Where(v => !v.IsRoot).Select(v => v.TreeNode).ToList();
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
                entry.node.SetChildren(Array.Empty<BtNode>());
                entry.node.RegenerateGuid();
                entry.node.GraphPosition += new Vector2(40, 40);
                asset.DetachedNodes.Add(entry.node);
            }
            EditorUtility.SetDirty(asset);
            Sync();
        }

        [Serializable]
        class CopyHolder
        {
            public List<CopyEntry> nodes;
        }

        [Serializable]
        class CopyEntry
        {
            [SerializeReference] public BtNode node;
        }
    }
}

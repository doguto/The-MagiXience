using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UniRx;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using BtNode = Project.Scenes.Battle.Scripts.BehaviorTree.Node;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public class BehaviorTreeNodeView : UnityEditor.Experimental.GraphView.Node
    {
        static readonly HashSet<string> HiddenFields = new() { "guid", "graphPosition", "children", "child", "comment" };

        readonly Label orderLabel;
        readonly Label summaryLabel;
        readonly System.Action<Port, UnityEngine.Vector2> onDropOutsidePort;

        public BtNode TreeNode { get; }
        public bool IsRoot { get; }
        readonly Subject<Unit> valuesChanged = new();
        public IObservable<Unit> OnValuesChanged => valuesChanged;
        public Port Input { get; }
        public Port Output { get; }

        // 入力欄は右ペインの Node タブに出す。ノード自体はヘッダーと要約だけにして、選択しても大きさを変えない
        readonly System.Func<SerializedProperty> resolveProperty;

        public BehaviorTreeNodeView(BtNode definition, System.Func<SerializedProperty> resolveProperty, System.Action<Port, UnityEngine.Vector2> onDropOutsidePort)
        {
            this.onDropOutsidePort = onDropOutsidePort;
            this.resolveProperty = resolveProperty;
            TreeNode = definition;
            viewDataKey = definition.Guid;
            AddToClassList("bt-node");
            AddToClassList("bt-node--" + NodeCatalog.CategoryKey(definition));
            tooltip = NodeCatalog.Description(definition.GetType());

            Input = CreatePort(Direction.Input, Port.Capacity.Single);
            mainContainer.Add(PortRow(Input, "bt-port-row--in"));

            var header = new VisualElement();
            header.AddToClassList("bt-header");
            var marker = new VisualElement();
            marker.AddToClassList("bt-marker");
            header.Add(marker);
            header.Add(StyledLabel(DisplayName(definition.GetType()), "bt-title"));
            orderLabel = StyledLabel("", "bt-order");
            header.Add(orderLabel);
            mainContainer.Add(header);

            var body = new VisualElement();
            body.AddToClassList("bt-body");
            summaryLabel = StyledLabel("", "bt-summary");
            body.Add(summaryLabel);
            mainContainer.Add(body);

            if (definition.ChildCapacity != NodeChildCapacity.None)
            {
                var capacity = definition.ChildCapacity == NodeChildCapacity.Multiple ? Port.Capacity.Multi : Port.Capacity.Single;
                Output = CreatePort(Direction.Output, capacity);
                mainContainer.Add(PortRow(Output, "bt-port-row--out"));
            }

            RefreshTexts();
            RefreshExpandedState();
            RefreshPorts();
        }

        // ツリーのルート接続口を表す擬似ノード
        public BehaviorTreeNodeView(System.Action<Port, UnityEngine.Vector2> onDropOutsidePort)
        {
            this.onDropOutsidePort = onDropOutsidePort;
            IsRoot = true;
            viewDataKey = "__root__";
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);
            AddToClassList("bt-node");
            AddToClassList("bt-node--root");

            var header = new VisualElement();
            header.AddToClassList("bt-header");
            header.Add(StyledLabel("Start", "bt-title"));
            mainContainer.Add(header);

            Output = CreatePort(Direction.Output, Port.Capacity.Single);
            mainContainer.Add(PortRow(Output, "bt-port-row--out"));
            RefreshExpandedState();
            RefreshPorts();
        }

        public SerializedProperty ResolveProperty() => resolveProperty?.Invoke();
        public bool IsRuntimeActive => ClassListContains("bt-running");

        public void NotifyValuesChanged()
        {
            RefreshTexts();
            valuesChanged.OnNext(Unit.Default);
        }

        public void SetOrder(int order, int? startFrame = null)
        {
            if (orderLabel == null) return;
            var text = order > 0 ? order.ToString() : "";
            if (startFrame.HasValue) text += $"  @{startFrame.Value}f";
            orderLabel.text = text;
        }

        public void SetRuntimeActive(bool active) => EnableInClassList("bt-running", active);

        public void RefreshTexts()
        {
            if (TreeNode == null) return;
            var summary = TreeNode.Summary;
            summaryLabel.text = summary ?? "";
            summaryLabel.style.display = string.IsNullOrEmpty(summary) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        Port CreatePort(Direction direction, Port.Capacity capacity) =>
            BehaviorTreePort.Create(direction, capacity, onDropOutsidePort);

        static VisualElement PortRow(Port port, string modifier)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("bt-port-row");
            row.AddToClassList(modifier);
            row.Add(port);
            return row;
        }

        static Label StyledLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        // 子ノード配列が HideInInspector のため NextVisible では中に入れない。Next で直下のフィールドを列挙する。
        // コメント以外の設定欄を body に並べ、コメントのプロパティを返す (置き方は呼び出し側で決める)
        public static SerializedProperty BuildFields(SerializedProperty property, VisualElement body, System.Action onChanged)
        {
            if (property == null) return null;

            var fields = new List<SerializedProperty>();
            var it = property.Copy();
            var end = it.GetEndProperty();
            var depth = property.depth + 1;
            if (it.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(it, end)) break;
                    if (it.depth != depth) continue;
                    fields.Add(it.Copy());
                } while (it.Next(false));
            }

            var nodeType = property.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue?.GetType()
                : null;

            SerializedProperty commentProp = null;
            foreach (var field in fields)
            {
                if (field.name == "comment")
                {
                    commentProp = field;
                    continue;
                }
                if (HiddenFields.Contains(field.name)) continue;
                if (nodeType == typeof(SubTree) && field.name == "overrides")
                {
                    var tree = fields.Find(f => f.name == "tree");
                    if (tree != null)
                    {
                        body.Add(SubTreeOverridesField.Create(field, tree, onChanged));
                        continue;
                    }
                }
                var element = BindField(field, onChanged);
                body.Add(element);
                ApplyShowWhen(nodeType, field, fields, element);
            }
            return commentProp;
        }

        static void ApplyShowWhen(System.Type nodeType, SerializedProperty field, List<SerializedProperty> fields, VisualElement element)
        {
            System.Reflection.FieldInfo info = null;
            for (var t = nodeType; t != null && info == null; t = t.BaseType)
            {
                info = t.GetField(field.name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
            }
            var showWhen = info?.GetCustomAttributes(typeof(ShowWhenAttribute), false);
            if (showWhen == null || showWhen.Length == 0) return;

            var attr = (ShowWhenAttribute)showWhen[0];
            var controller = fields.Find(f => f.name == attr.Field);
            if (controller == null) return;

            void Refresh(SerializedProperty p)
            {
                var visible = p.propertyType switch
                {
                    SerializedPropertyType.Enum => p.intValue == System.Convert.ToInt32(attr.Value),
                    SerializedPropertyType.Boolean => p.boolValue.Equals(attr.Value),
                    _ => true,
                };
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Refresh(controller);
            element.TrackPropertyValue(controller, Refresh);
        }

        public static PropertyField BindField(SerializedProperty prop, System.Action onChanged)
        {
            var field = new PropertyField(prop.Copy());
            field.Bind(prop.serializedObject);
            field.RegisterValueChangeCallback(_ => onChanged?.Invoke());
            return field;
        }

        public static string DisplayName(System.Type type) => ObjectNames.NicifyVariableName(type.Name);
    }
}

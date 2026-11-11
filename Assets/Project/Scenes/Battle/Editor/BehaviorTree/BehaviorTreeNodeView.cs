using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public class BehaviorTreeNodeView : UnityEditor.Experimental.GraphView.Node
    {
        static readonly HashSet<string> HiddenFields = new() { "guid", "graphPosition", "children", "child", "comment" };

        readonly Label orderBadge;
        readonly Label summaryLabel;
        readonly Label commentLabel;

        public NodeDefinition Definition { get; }
        public bool IsRoot { get; }
        public Port Input { get; }
        public Port Output { get; }

        public BehaviorTreeNodeView(NodeDefinition definition, SerializedProperty property)
        {
            Definition = definition;
            viewDataKey = definition.Guid;
            AddToClassList("bt-node");
            AddToClassList("bt-node--" + CategoryKey(definition));
            tooltip = NodeCatalog.Description(definition.GetType());

            Input = CreatePort(Direction.Input, Port.Capacity.Single);
            mainContainer.Insert(0, PortRow(Input, "bt-port-row--in"));

            var header = new VisualElement();
            header.AddToClassList("bt-header");
            header.Add(Accent());
            var texts = new VisualElement();
            texts.AddToClassList("bt-header-texts");
            texts.Add(StyledLabel(CategoryLabel(definition), "bt-category"));
            texts.Add(StyledLabel(DisplayName(definition.GetType()), "bt-title"));
            header.Add(texts);
            orderBadge = StyledLabel("", "bt-order");
            orderBadge.style.display = DisplayStyle.None;
            header.Add(orderBadge);
            mainContainer.Insert(1, header);

            summaryLabel = StyledLabel("", "bt-summary");
            mainContainer.Insert(2, summaryLabel);
            commentLabel = StyledLabel("", "bt-comment");
            mainContainer.Insert(3, commentLabel);

            BuildFields(property);

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
        public BehaviorTreeNodeView()
        {
            IsRoot = true;
            viewDataKey = "__root__";
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);
            AddToClassList("bt-node");
            AddToClassList("bt-node--root");

            var header = new VisualElement();
            header.AddToClassList("bt-header");
            header.Add(StyledLabel("START", "bt-title"));
            mainContainer.Insert(0, header);

            Output = CreatePort(Direction.Output, Port.Capacity.Single);
            mainContainer.Add(PortRow(Output, "bt-port-row--out"));
            RefreshExpandedState();
            RefreshPorts();
        }

        public void SetOrder(int order)
        {
            if (orderBadge == null) return;
            orderBadge.text = order.ToString();
            orderBadge.style.display = order > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetRuntimeActive(bool active) => EnableInClassList("bt-running", active);

        public void RefreshTexts()
        {
            if (Definition == null) return;

            var summary = Definition.Summary;
            summaryLabel.text = summary ?? "";
            summaryLabel.style.display = string.IsNullOrEmpty(summary) ? DisplayStyle.None : DisplayStyle.Flex;

            var comment = Definition.Comment;
            commentLabel.text = comment ?? "";
            commentLabel.style.display = string.IsNullOrEmpty(comment) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        Port CreatePort(Direction direction, Port.Capacity capacity)
        {
            var port = InstantiatePort(Orientation.Vertical, direction, capacity, typeof(bool));
            port.portName = "";
            return port;
        }

        static VisualElement PortRow(Port port, string modifier)
        {
            var row = new VisualElement();
            row.AddToClassList("bt-port-row");
            row.AddToClassList(modifier);
            row.Add(port);
            return row;
        }

        static VisualElement Accent()
        {
            var accent = new VisualElement();
            accent.AddToClassList("bt-accent");
            return accent;
        }

        static Label StyledLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        // 子ノード配列が HideInInspector のため NextVisible では中に入れない。Next で直下のフィールドを列挙する。
        void BuildFields(SerializedProperty property)
        {
            if (property == null) return;

            var body = new VisualElement();
            body.AddToClassList("bt-body");

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

            SerializedProperty commentProp = null;
            foreach (var field in fields)
            {
                if (field.name == "comment")
                {
                    commentProp = field;
                    continue;
                }
                if (HiddenFields.Contains(field.name)) continue;
                body.Add(BindField(field));
            }

            if (commentProp != null)
            {
                var foldout = new Foldout { text = "Comment", value = !string.IsNullOrEmpty(commentProp.stringValue) };
                foldout.Add(BindField(commentProp));
                body.Add(foldout);
            }

            if (body.childCount == 0) return;
            mainContainer.Add(body);
        }

        PropertyField BindField(SerializedProperty prop)
        {
            var field = new PropertyField(prop.Copy());
            field.Bind(prop.serializedObject);
            field.RegisterValueChangeCallback(_ => RefreshTexts());
            return field;
        }

        static string CategoryKey(NodeDefinition definition) => NodeCatalog.CategoryKey(definition);

        static string CategoryLabel(NodeDefinition definition) => CategoryKey(definition).ToUpperInvariant();

        public static string DisplayName(System.Type type)
        {
            var name = type.Name;
            if (name.EndsWith("NodeDefinition")) name = name[..^"NodeDefinition".Length];
            else if (name.EndsWith("Definition")) name = name[..^"Definition".Length];
            return ObjectNames.NicifyVariableName(name);
        }
    }
}

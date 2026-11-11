using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public class BehaviorTreeNodeView : UnityEditor.Experimental.GraphView.Node
    {
        static readonly Color CompositeColor = new(0.25f, 0.42f, 0.65f);
        static readonly Color DecoratorColor = new(0.55f, 0.38f, 0.18f);
        static readonly Color ActionColor = new(0.22f, 0.5f, 0.3f);
        static readonly Color SubTreeColor = new(0.5f, 0.28f, 0.55f);
        static readonly Color RootColor = new(0.45f, 0.15f, 0.15f);
        static readonly Color ActiveBorder = new(1f, 0.85f, 0.2f);

        public NodeDefinition Definition { get; }
        public bool IsRoot { get; }
        public Port Input { get; }
        public Port Output { get; }

        public BehaviorTreeNodeView(NodeDefinition definition, SerializedProperty property)
        {
            Definition = definition;
            title = DisplayName(definition.GetType());
            viewDataKey = definition.Guid;
            style.minWidth = 180;

            Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = "";
            inputContainer.Add(Input);

            if (definition.ChildCapacity != NodeChildCapacity.None)
            {
                var capacity = definition.ChildCapacity == NodeChildCapacity.Multiple ? Port.Capacity.Multi : Port.Capacity.Single;
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, capacity, typeof(bool));
                Output.portName = "";
                outputContainer.Add(Output);
            }

            titleContainer.style.backgroundColor = CategoryColor(definition);
            BuildFields(property);
            RefreshExpandedState();
            RefreshPorts();
        }

        // ツリーのルート接続口を表す擬似ノード
        public BehaviorTreeNodeView()
        {
            IsRoot = true;
            title = "Root";
            viewDataKey = "__root__";
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);
            Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
            Output.portName = "";
            outputContainer.Add(Output);
            titleContainer.style.backgroundColor = RootColor;
            RefreshExpandedState();
            RefreshPorts();
        }

        public void SetRuntimeActive(bool active)
        {
            var color = active ? ActiveBorder : Color.clear;
            var width = active ? 2f : 0f;
            mainContainer.style.borderTopColor = color;
            mainContainer.style.borderBottomColor = color;
            mainContainer.style.borderLeftColor = color;
            mainContainer.style.borderRightColor = color;
            mainContainer.style.borderTopWidth = width;
            mainContainer.style.borderBottomWidth = width;
            mainContainer.style.borderLeftWidth = width;
            mainContainer.style.borderRightWidth = width;
        }

        void BuildFields(SerializedProperty property)
        {
            if (property == null) return;

            var container = new VisualElement { style = { paddingLeft = 4, paddingRight = 4, paddingBottom = 4, backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.95f) } };
            var it = property.Copy();
            var end = it.GetEndProperty();
            var enterChildren = true;
            while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
            {
                enterChildren = false;
                var field = new PropertyField(it.Copy());
                field.Bind(property.serializedObject);
                container.Add(field);
            }

            if (container.childCount > 0) extensionContainer.Add(container);
            expanded = true;
        }

        static Color CategoryColor(NodeDefinition definition) => definition switch
        {
            SubTreeNodeDefinition => SubTreeColor,
            CompositeNodeDefinition => CompositeColor,
            DecoratorNodeDefinition => DecoratorColor,
            _ => ActionColor,
        };

        public static string DisplayName(System.Type type)
        {
            var name = type.Name;
            if (name.EndsWith("NodeDefinition")) name = name[..^"NodeDefinition".Length];
            else if (name.EndsWith("Definition")) name = name[..^"Definition".Length];
            return ObjectNames.NicifyVariableName(name);
        }
    }
}

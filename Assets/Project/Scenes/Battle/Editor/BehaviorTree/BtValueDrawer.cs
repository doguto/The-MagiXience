using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // BtInt / BtFloat などを「ラベル + 値 + [$]」の1行で表示する。[$] でパラメータ参照に切り替える。
    [CustomPropertyDrawer(typeof(BtInt))]
    [CustomPropertyDrawer(typeof(BtFloat))]
    [CustomPropertyDrawer(typeof(BtBool))]
    [CustomPropertyDrawer(typeof(BtVector2))]
    [CustomPropertyDrawer(typeof(BtObject<>))]
    public class BtValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProp = property.FindPropertyRelative("value");
            var paramProp = property.FindPropertyRelative("param");

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var valueField = new PropertyField(valueProp, property.displayName) { style = { flexGrow = 1 } };
            valueField.BindProperty(valueProp);

            var paramField = new TextField(property.displayName) { style = { flexGrow = 1 } };
            paramField.BindProperty(paramProp);
            paramField.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            var toggle = new Button { text = "$", tooltip = "ツリーのパラメータを参照する" };
            toggle.style.width = 20;
            toggle.style.height = 18;
            toggle.style.marginLeft = 2;
            toggle.style.paddingLeft = 0;
            toggle.style.paddingRight = 0;

            void Refresh()
            {
                var bound = !string.IsNullOrEmpty(paramProp.stringValue) || paramField.userData is true;
                valueField.style.display = bound ? DisplayStyle.None : DisplayStyle.Flex;
                paramField.style.display = bound ? DisplayStyle.Flex : DisplayStyle.None;
                toggle.style.backgroundColor = bound ? new Color(0.88f, 0.42f, 0.62f, 0.8f) : StyleKeyword.Null;
            }

            toggle.clicked += () =>
            {
                var bound = !string.IsNullOrEmpty(paramProp.stringValue) || paramField.userData is true;
                if (bound)
                {
                    paramProp.stringValue = "";
                    paramProp.serializedObject.ApplyModifiedProperties();
                    paramField.userData = false;
                }
                else
                {
                    paramField.userData = true;
                }
                Refresh();
            };

            paramField.RegisterValueChangedCallback(_ => Refresh());
            row.Add(valueField);
            row.Add(paramField);
            row.Add(toggle);
            Refresh();
            return row;
        }
    }
}

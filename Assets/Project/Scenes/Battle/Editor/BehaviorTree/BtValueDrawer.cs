using System;
using System.Collections;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // BtInt / BtFloat などの値欄。固定値のときは通常の入力欄、パラメータ参照のときはチップで表示する。
    // ▾ のメニュー、またはパラメータ一覧からのドラッグで結びつける。
    [CustomPropertyDrawer(typeof(BtInt))]
    [CustomPropertyDrawer(typeof(BtFloat))]
    [CustomPropertyDrawer(typeof(BtBool))]
    [CustomPropertyDrawer(typeof(BtVector2))]
    [CustomPropertyDrawer(typeof(BtObject<>))]
    public class BtValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var so = property.serializedObject;
            var path = property.propertyPath;
            var accepted = ParameterUtility.AcceptedKinds(ValueType());

            var row = new VisualElement();
            row.AddToClassList("bt-value");
            row.AddToClassList(BaseField<int>.alignedFieldUssClassName);

            var label = new Label(property.displayName);
            label.AddToClassList("bt-value__label");
            row.Add(label);

            var valueProp = property.FindPropertyRelative("value");
            var valueField = new PropertyField(valueProp, "");
            valueField.AddToClassList("bt-value__field");
            valueField.BindProperty(valueProp);
            row.Add(valueField);

            var chip = new VisualElement();
            chip.AddToClassList("bt-value__chip");
            var chipLabel = new Label();
            chipLabel.AddToClassList("bt-value__chip-label");
            chip.Add(chipLabel);
            var unbind = new Button { text = "×", tooltip = "固定値に戻す" };
            unbind.AddToClassList("bt-value__chip-remove");
            chip.Add(unbind);
            row.Add(chip);

            var menuButton = new Button { text = "▾", tooltip = "パラメータに結びつける" };
            menuButton.AddToClassList("bt-value__menu");
            row.Add(menuButton);

            SerializedProperty Prop() => so.FindProperty(path);
            string CurrentParam() => Prop()?.FindPropertyRelative("param").stringValue ?? "";

            void Refresh()
            {
                if (Prop() == null) return;
                var param = CurrentParam();
                var bound = !string.IsNullOrEmpty(param);
                valueField.style.display = bound ? DisplayStyle.None : DisplayStyle.Flex;
                chip.style.display = bound ? DisplayStyle.Flex : DisplayStyle.None;
                if (!bound) return;

                var declared = ParameterUtility.Find(so, param);
                var valid = declared != null && accepted.Contains((TreeParameterKind)declared.FindPropertyRelative("kind").enumValueIndex);
                chipLabel.text = (valid ? "⛓ " : "⚠ ") + param;
                chip.EnableInClassList("bt-value__chip--missing", !valid);
                chip.tooltip = valid ? $"パラメータ '{param}' を参照" : $"パラメータ '{param}' が未宣言、または型が合いません";
            }

            void Bind(string name)
            {
                so.Update();
                Prop().FindPropertyRelative("param").stringValue = name ?? "";
                so.ApplyModifiedProperties();
                Refresh();
            }

            unbind.clicked += () => Bind("");

            menuButton.clicked += () =>
            {
                so.Update();
                var menu = new GenericMenu();
                var current = CurrentParam();
                menu.AddItem(new GUIContent("固定値"), string.IsNullOrEmpty(current), () => Bind(""));
                menu.AddSeparator("");

                var any = false;
                foreach (var p in ParameterUtility.Parameters(so))
                {
                    var name = p.FindPropertyRelative("name").stringValue;
                    var kind = (TreeParameterKind)p.FindPropertyRelative("kind").enumValueIndex;
                    if (!accepted.Contains(kind) || string.IsNullOrEmpty(name)) continue;
                    any = true;
                    menu.AddItem(new GUIContent($"{name}    ({ParameterUtility.KindLabel(kind)})"), name == current, () => Bind(name));
                }
                if (!any) menu.AddDisabledItem(new GUIContent("(型が合うパラメータがありません)"));

                if (accepted.Length > 0 && so.FindProperty("parameters") != null)
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("今の値で新しいパラメータを作成"), false, () =>
                    {
                        so.Update();
                        var created = ParameterUtility.Add(so, property.name, accepted[0]);
                        ParameterUtility.CopyValue(Prop().FindPropertyRelative("value"), created, accepted[0]);
                        var createdName = created.FindPropertyRelative("name").stringValue;
                        Prop().FindPropertyRelative("param").stringValue = createdName;
                        so.ApplyModifiedProperties();
                        Refresh();
                    });
                }
                menu.DropDown(menuButton.worldBound);
            };

            row.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (DragAndDrop.GetGenericData(ParameterUtility.DragKey) is not ParameterUtility.DragPayload payload) return;
                var ok = accepted.Contains(payload.Kind);
                DragAndDrop.visualMode = ok ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                row.EnableInClassList("bt-value--drop", ok);
                e.StopPropagation();
            });
            row.RegisterCallback<DragLeaveEvent>(_ => row.RemoveFromClassList("bt-value--drop"));
            row.RegisterCallback<DragPerformEvent>(e =>
            {
                row.RemoveFromClassList("bt-value--drop");
                if (DragAndDrop.GetGenericData(ParameterUtility.DragKey) is not ParameterUtility.DragPayload payload) return;
                if (!accepted.Contains(payload.Kind)) return;
                DragAndDrop.AcceptDrag();
                Bind(payload.Name);
                e.StopPropagation();
            });

            row.TrackSerializedObjectValue(so, _ => Refresh());
            Refresh();
            return row;
        }

        Type ValueType()
        {
            var t = fieldInfo.FieldType;
            if (t.IsArray) return t.GetElementType();
            if (typeof(IList).IsAssignableFrom(t) && t.IsGenericType) return t.GetGenericArguments()[0];
            return t;
        }
    }
}

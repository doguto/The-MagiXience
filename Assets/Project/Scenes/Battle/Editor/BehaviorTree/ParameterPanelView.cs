using System;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 右ペインのパラメータ一覧。追加・改名・既定値の編集、使用箇所の表示、値欄へのドラッグ元を兼ねる。
    public class ParameterPanelView : VisualElement
    {
        readonly ScrollView list;
        readonly Label emptyLabel;
        BehaviorTreeAsset asset;
        SerializedObject serializedObject;
        int lastArraySize = -1;

        public event Action<string> UsageClicked;
        public event Action Changed;

        public ParameterPanelView()
        {
            AddToClassList("bt-params");

            var header = new VisualElement();
            header.AddToClassList("bt-params__header");
            header.Add(new Label("PARAMETERS") { name = "title" });
            var addButton = new Button(ShowAddMenu) { text = "+", tooltip = "パラメータを追加" };
            addButton.AddToClassList("bt-params__add");
            header.Add(addButton);
            Add(header);

            list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("bt-params__list");
            Add(list);

            emptyLabel = new Label("パラメータはまだありません。\n+ から追加すると、ノードの値をこのツリーの引数にできます。\nSubTree で呼ぶ側から値を上書きできます。");
            emptyLabel.AddToClassList("bt-params__empty");
            Add(emptyLabel);

            var hint = new Label("行をノードの値欄へドラッグすると結びつけます。");
            hint.AddToClassList("bt-params__hint");
            Add(hint);
        }

        public void SetAsset(BehaviorTreeAsset target)
        {
            asset = target;
            serializedObject = asset ? new SerializedObject(asset) : null;
            this.Unbind();
            if (serializedObject != null)
            {
                this.TrackSerializedObjectValue(serializedObject, _ => OnSerializedChanged());
            }
            Rebuild();
        }

        void OnSerializedChanged()
        {
            serializedObject.Update();
            var size = serializedObject.FindProperty("parameters").arraySize;
            if (size != lastArraySize) Rebuild();
            else RefreshUsages();
        }

        public void Rebuild()
        {
            list.Clear();
            if (serializedObject == null)
            {
                emptyLabel.style.display = DisplayStyle.None;
                lastArraySize = -1;
                return;
            }

            serializedObject.Update();
            var array = serializedObject.FindProperty("parameters");
            lastArraySize = array.arraySize;
            for (var i = 0; i < array.arraySize; i++)
            {
                list.Add(CreateRow(array.GetArrayElementAtIndex(i), i));
            }
            emptyLabel.style.display = array.arraySize == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshUsages()
        {
            foreach (var row in list.Children())
            {
                if (row.userData is not string name) continue;
                var usage = row.Q<Button>(className: "bt-params__usage");
                if (usage != null) SetUsageText(usage, ParameterUtility.CountUsages(serializedObject, name));
            }
        }

        VisualElement CreateRow(SerializedProperty parameter, int index)
        {
            var nameProp = parameter.FindPropertyRelative("name");
            var kindProp = parameter.FindPropertyRelative("kind");
            var kind = (TreeParameterKind)kindProp.enumValueIndex;

            var row = new VisualElement { userData = nameProp.stringValue };
            row.AddToClassList("bt-params__row");
            row.AddToClassList("bt-params__row--" + kind.ToString().ToLowerInvariant());

            var top = new VisualElement();
            top.AddToClassList("bt-params__row-top");

            var handle = new Label("⋮⋮") { tooltip = "ドラッグしてノードの値欄に結びつける" };
            handle.AddToClassList("bt-params__handle");
            top.Add(handle);

            var nameField = new TextField { value = nameProp.stringValue, isDelayed = true };
            nameField.AddToClassList("bt-params__name");
            nameField.RegisterValueChangedCallback(e => RenameParameter(index, e.previousValue, e.newValue));
            top.Add(nameField);

            var kindLabel = new Label(ParameterUtility.KindLabel(kind));
            kindLabel.AddToClassList("bt-params__kind");
            top.Add(kindLabel);

            var remove = new Button(() => RemoveParameter(index)) { text = "×", tooltip = "削除" };
            remove.AddToClassList("bt-params__remove");
            top.Add(remove);
            row.Add(top);

            var bottom = new VisualElement();
            bottom.AddToClassList("bt-params__row-bottom");

            var valueProp = parameter.FindPropertyRelative(ParameterUtility.ValuePropertyName(kind));
            var valueField = new PropertyField(valueProp, "") { tooltip = "既定値 (SubTree で上書きされない場合の値)" };
            valueField.AddToClassList("bt-params__value");
            valueField.BindProperty(valueProp);
            bottom.Add(valueField);

            var usage = new Button(() => UsageClicked?.Invoke(nameProp.stringValue)) { tooltip = "使用しているノードを選択" };
            usage.AddToClassList("bt-params__usage");
            SetUsageText(usage, ParameterUtility.CountUsages(serializedObject, nameProp.stringValue));
            bottom.Add(usage);
            row.Add(bottom);

            RegisterDragSource(handle, nameProp.stringValue, kind);
            RegisterDragSource(kindLabel, nameProp.stringValue, kind);
            return row;
        }

        static void SetUsageText(Button usage, int count)
        {
            usage.text = count + " use";
            usage.EnableInClassList("bt-params__usage--unused", count == 0);
        }

        static void RegisterDragSource(VisualElement element, string name, TreeParameterKind kind)
        {
            var pressed = false;
            var start = Vector2.zero;
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                pressed = true;
                start = e.position;
            });
            element.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!pressed || ((Vector2)e.position - start).sqrMagnitude < 16f) return;
                pressed = false;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(ParameterUtility.DragKey, new ParameterUtility.DragPayload { Name = name, Kind = kind });
                DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
                DragAndDrop.StartDrag("$" + name);
            });
            element.RegisterCallback<PointerUpEvent>(_ => pressed = false);
        }

        void ShowAddMenu()
        {
            if (serializedObject == null) return;
            var menu = new GenericMenu();
            foreach (TreeParameterKind kind in Enum.GetValues(typeof(TreeParameterKind)))
            {
                var k = kind;
                menu.AddItem(new GUIContent(kind.ToString()), false, () =>
                {
                    serializedObject.Update();
                    ParameterUtility.Add(serializedObject, k.ToString().ToLowerInvariant(), k);
                    serializedObject.ApplyModifiedProperties();
                    Rebuild();
                    Changed?.Invoke();
                    schedule.Execute(() => list.Query<TextField>(className: "bt-params__name").Last()?.Q("unity-text-input")?.Focus());
                });
            }
            menu.ShowAsContext();
        }

        void RenameParameter(int index, string oldName, string newName)
        {
            serializedObject.Update();
            newName = newName?.Trim();
            if (string.IsNullOrEmpty(newName) || newName == oldName)
            {
                Rebuild();
                return;
            }

            if (ParameterUtility.Find(serializedObject, newName) != null)
            {
                Debug.LogWarning($"[BehaviorTreeEditor] Parameter name already exists: {newName}");
                Rebuild();
                return;
            }

            var p = serializedObject.FindProperty("parameters").GetArrayElementAtIndex(index);
            p.FindPropertyRelative("name").stringValue = newName;
            ParameterUtility.Rename(serializedObject, oldName, newName);
            serializedObject.ApplyModifiedProperties();
            Rebuild();
            Changed?.Invoke();
        }

        void RemoveParameter(int index)
        {
            serializedObject.Update();
            var array = serializedObject.FindProperty("parameters");
            var name = array.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue;
            var usages = ParameterUtility.CountUsages(serializedObject, name);
            if (usages > 0 && !EditorUtility.DisplayDialog("パラメータの削除",
                    $"'{name}' は {usages} 箇所で使われています。削除すると、それらの値欄は固定値に戻ります。", "削除", "キャンセル"))
            {
                return;
            }

            foreach (var path in ParameterUtility.FindReferencePaths(serializedObject, name))
            {
                serializedObject.FindProperty(path).stringValue = "";
            }
            array.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            Rebuild();
            Changed?.Invoke();
        }
    }
}

using System;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 右ペインのパラメータ一覧 (Shader Graph の Blackboard 相当)。
    // 行: 名前 / 型 / 既定値。行のドラッグで値欄に結びつけ、右クリックで削除。
    public class ParameterPanelView : VisualElement
    {
        readonly Label titleLabel;
        readonly VisualElement list;
        readonly Label footer;
        readonly Button selectUsagesButton;
        BehaviorTreeAsset asset;
        SerializedObject serializedObject;
        int lastArraySize = -1;
        string selectedName;

        public event Action<string> UsageClicked;
        public event Action Changed;

        public ParameterPanelView()
        {
            AddToClassList("bt-params");

            var header = new VisualElement();
            header.AddToClassList("bt-params__header");
            var titles = new VisualElement { style = { flexGrow = 1 } };
            titleLabel = new Label();
            titleLabel.AddToClassList("bt-params__title");
            titles.Add(titleLabel);
            var subtitle = new Label("Parameters");
            subtitle.AddToClassList("bt-params__subtitle");
            titles.Add(subtitle);
            header.Add(titles);
            var addButton = new Button(ShowAddMenu) { text = "+", tooltip = "パラメータを追加" };
            addButton.AddToClassList("bt-params__add");
            header.Add(addButton);
            Add(header);

            var columns = new VisualElement();
            columns.AddToClassList("bt-params__columns");
            columns.Add(new Label("Name") { name = "name" });
            columns.Add(new Label("Type") { name = "type" });
            columns.Add(new Label("Default") { name = "value" });
            Add(columns);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("bt-params__scroll");
            list = scroll.contentContainer;
            Add(scroll);

            var footerRow = new VisualElement();
            footerRow.AddToClassList("bt-params__footer");
            footer = new Label();
            footer.AddToClassList("bt-params__footer-text");
            footerRow.Add(footer);
            selectUsagesButton = new Button(() => { if (selectedName != null) UsageClicked?.Invoke(selectedName); }) { text = "Select" };
            selectUsagesButton.AddToClassList("bt-params__select");
            footerRow.Add(selectUsagesButton);
            Add(footerRow);
        }

        public void SetAsset(BehaviorTreeAsset target)
        {
            asset = target;
            serializedObject = asset ? new SerializedObject(asset) : null;
            selectedName = null;
            this.Unbind();
            if (serializedObject != null)
            {
                // ツリー全体を監視すると大きなツリーで毎フレーム重くなるため、パラメータ一覧だけを監視する
                this.TrackPropertyValue(serializedObject.FindProperty("parameters"), _ => OnSerializedChanged());
            }
            Rebuild();
        }

        void OnSerializedChanged()
        {
            serializedObject.Update();
            var size = serializedObject.FindProperty("parameters").arraySize;
            if (size != lastArraySize) Rebuild();
            else RefreshFooter();
        }

        public void Rebuild()
        {
            list.Clear();
            titleLabel.text = asset ? asset.name : "-";
            if (serializedObject == null)
            {
                lastArraySize = -1;
                RefreshFooter();
                return;
            }

            serializedObject.Update();
            var array = serializedObject.FindProperty("parameters");
            lastArraySize = array.arraySize;
            for (var i = 0; i < array.arraySize; i++)
            {
                list.Add(CreateRow(array.GetArrayElementAtIndex(i), i));
            }

            if (array.arraySize == 0)
            {
                var empty = new Label("No parameters");
                empty.AddToClassList("bt-params__empty");
                list.Add(empty);
            }
            RefreshFooter();
        }

        void RefreshFooter()
        {
            if (serializedObject == null || string.IsNullOrEmpty(selectedName) || ParameterUtility.Find(serializedObject, selectedName) == null)
            {
                footer.text = "";
                selectUsagesButton.style.display = DisplayStyle.None;
                return;
            }

            var count = ParameterUtility.CountUsages(serializedObject, selectedName);
            footer.text = count == 0 ? $"{selectedName} — 未使用" : $"{selectedName} — {count} 箇所で使用";
            selectUsagesButton.style.display = count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        VisualElement CreateRow(SerializedProperty parameter, int index)
        {
            var nameProp = parameter.FindPropertyRelative("name");
            var kind = (TreeParameterKind)parameter.FindPropertyRelative("kind").enumValueIndex;
            var name = nameProp.stringValue;

            var row = new VisualElement { userData = name };
            row.AddToClassList("bt-params__row");
            row.EnableInClassList("bt-params__row--selected", name == selectedName);

            var nameField = new TextField { value = name, isDelayed = true };
            nameField.AddToClassList("bt-params__name");
            nameField.RegisterValueChangedCallback(e => RenameParameter(index, e.previousValue, e.newValue));
            row.Add(nameField);

            var kindLabel = new Label(ParameterUtility.KindLabel(kind));
            kindLabel.AddToClassList("bt-params__kind");
            row.Add(kindLabel);

            var valueProp = parameter.FindPropertyRelative(ParameterUtility.ValuePropertyName(kind));
            var valueField = new PropertyField(valueProp, "");
            valueField.AddToClassList("bt-params__value");
            valueField.BindProperty(valueProp);
            row.Add(valueField);

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                selectedName = name;
                foreach (var r in list.Children()) r.EnableInClassList("bt-params__row--selected", Equals(r.userData, name));
                RefreshFooter();
            }, TrickleDown.TrickleDown);

            row.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Select Usages", _ => UsageClicked?.Invoke(name));
                evt.menu.AppendAction("Delete", _ => RemoveParameter(index));
            }));

            RegisterDragSource(kindLabel, name, kind);
            return row;
        }

        static void RegisterDragSource(VisualElement element, string name, TreeParameterKind kind)
        {
            var pressed = false;
            var start = Vector2.zero;
            element.tooltip = "ドラッグしてノードの値欄に結びつける";
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
                DragAndDrop.StartDrag(name);
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
                    var created = ParameterUtility.Add(serializedObject, k.ToString().ToLowerInvariant(), k);
                    selectedName = created.FindPropertyRelative("name").stringValue;
                    serializedObject.ApplyModifiedProperties();
                    Rebuild();
                    Changed?.Invoke();
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
            if (selectedName == oldName) selectedName = newName;
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
            if (selectedName == name) selectedName = null;
            Rebuild();
            Changed?.Invoke();
        }
    }
}

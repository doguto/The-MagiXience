using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // SubTree の overrides 欄。呼び出すツリーのパラメータを 1 行ずつ並べ、値を入れたものだけ上書きとして保存する
    public static class SubTreeOverridesField
    {
        public static VisualElement Create(SerializedProperty overrides, SerializedProperty tree, Action onChanged)
        {
            var so = overrides.serializedObject;
            var overridesPath = overrides.propertyPath;
            var treePath = tree.propertyPath;

            var root = new VisualElement();
            var header = new Label("パラメータ");
            header.AddToClassList("bt-overrides__header");

            void Rebuild()
            {
                root.Clear();
                root.Add(header);
                so.Update();

                var asset = so.FindProperty(treePath)?.objectReferenceValue as BehaviorTreeAsset;
                var declared = asset != null ? new SerializedObject(asset) : null;
                var names = new HashSet<string>();
                if (declared != null)
                {
                    foreach (var p in ParameterUtility.Parameters(declared))
                    {
                        var name = p.FindPropertyRelative("name").stringValue;
                        if (string.IsNullOrEmpty(name) || !names.Add(name)) continue;
                        root.Add(Row(so, overridesPath, name, (TreeParameterKind)p.FindPropertyRelative("kind").enumValueIndex, p, onChanged));
                    }
                }
                if (declared != null && names.Count == 0) root.Add(Note("(このツリーにパラメータはありません)"));

                var array = so.FindProperty(overridesPath);
                for (var i = 0; i < array.arraySize; i++)
                {
                    var name = array.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                    if (names.Contains(name)) continue;
                    root.Add(StaleRow(so, overridesPath, name, () => { onChanged?.Invoke(); Rebuild(); }));
                }
                header.style.display = root.childCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            root.TrackPropertyValue(tree, _ => Rebuild());
            Rebuild();
            return root;
        }

        static VisualElement Row(SerializedObject so, string overridesPath, string name, TreeParameterKind kind, SerializedProperty defaultParam, Action onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("bt-value");
            row.AddToClassList(BaseField<int>.alignedFieldUssClassName);

            var label = new Label(name) { tooltip = ParameterUtility.KindLabel(kind) };
            label.AddToClassList("bt-value__label");
            row.Add(label);

            var valueName = ParameterUtility.ValuePropertyName(kind);
            Action<SerializedProperty> show = null;

            void Write(Action<SerializedProperty> set)
            {
                so.Update();
                var entry = FindOrAdd(so, overridesPath, name, kind);
                entry.FindPropertyRelative("bindToParent").stringValue = "";
                set(entry.FindPropertyRelative(valueName));
                so.ApplyModifiedProperties();
                onChanged?.Invoke();
            }

            var field = CreateValueField(kind, Write, out show);
            field.AddToClassList("bt-value__field");
            row.Add(field);

            var boundLabel = new Label();
            boundLabel.AddToClassList("bt-value__bound");
            row.Add(boundLabel);

            var resetButton = new Button { text = "↺", tooltip = "ツリーの既定値に戻す" };
            resetButton.AddToClassList("bt-value__menu");
            row.Add(resetButton);

            var menuButton = new Button { text = "▾", tooltip = "呼び出し側のパラメータを引き継ぐ" };
            menuButton.AddToClassList("bt-value__menu");
            row.Add(menuButton);

            void Refresh()
            {
                so.Update();
                var entry = Find(so, overridesPath, name);
                var bound = entry?.FindPropertyRelative("bindToParent").stringValue ?? "";
                var isBound = !string.IsNullOrEmpty(bound);
                show((entry ?? defaultParam).FindPropertyRelative(valueName));
                field.style.display = isBound ? DisplayStyle.None : DisplayStyle.Flex;
                boundLabel.style.display = isBound ? DisplayStyle.Flex : DisplayStyle.None;
                boundLabel.text = bound;
                boundLabel.tooltip = $"呼び出し側のパラメータ '{bound}' を引き継ぐ";
                resetButton.style.visibility = entry != null ? Visibility.Visible : Visibility.Hidden;
                row.EnableInClassList("bt-override--default", entry == null);
            }

            void Bind(string parent)
            {
                so.Update();
                FindOrAdd(so, overridesPath, name, kind).FindPropertyRelative("bindToParent").stringValue = parent;
                so.ApplyModifiedProperties();
                onChanged?.Invoke();
                Refresh();
            }

            resetButton.clicked += () =>
            {
                so.Update();
                Remove(so, overridesPath, name);
                so.ApplyModifiedProperties();
                onChanged?.Invoke();
                Refresh();
            };

            menuButton.clicked += () =>
            {
                so.Update();
                var menu = new GenericMenu();
                var current = Find(so, overridesPath, name)?.FindPropertyRelative("bindToParent").stringValue ?? "";
                var any = false;
                foreach (var p in ParameterUtility.Parameters(so))
                {
                    var parent = p.FindPropertyRelative("name").stringValue;
                    var parentKind = (TreeParameterKind)p.FindPropertyRelative("kind").enumValueIndex;
                    if (string.IsNullOrEmpty(parent) || !Compatible(kind, parentKind)) continue;
                    any = true;
                    menu.AddItem(new GUIContent($"{parent}    ({ParameterUtility.KindLabel(parentKind)})"), parent == current, () => Bind(parent));
                }
                if (!any) menu.AddDisabledItem(new GUIContent("(型が合うパラメータがありません)"));
                menu.DropDown(menuButton.worldBound);
            };

            row.TrackPropertyValue(so.FindProperty(overridesPath), _ => Refresh());
            Refresh();
            return row;
        }

        static VisualElement CreateValueField(TreeParameterKind kind, Action<Action<SerializedProperty>> write, out Action<SerializedProperty> show)
        {
            switch (kind)
            {
                case TreeParameterKind.Int:
                {
                    var f = new IntegerField { isDelayed = true };
                    f.RegisterValueChangedCallback(e => write(p => p.intValue = e.newValue));
                    show = p => f.SetValueWithoutNotify(p.intValue);
                    return f;
                }
                case TreeParameterKind.Float:
                {
                    var f = new FloatField { isDelayed = true };
                    f.RegisterValueChangedCallback(e => write(p => p.floatValue = e.newValue));
                    show = p => f.SetValueWithoutNotify(p.floatValue);
                    return f;
                }
                case TreeParameterKind.Bool:
                {
                    var f = new Toggle();
                    f.RegisterValueChangedCallback(e => write(p => p.boolValue = e.newValue));
                    show = p => f.SetValueWithoutNotify(p.boolValue);
                    return f;
                }
                case TreeParameterKind.Vector2:
                {
                    var f = new Vector2Field();
                    f.RegisterValueChangedCallback(e => write(p => p.vector2Value = e.newValue));
                    show = p => f.SetValueWithoutNotify(p.vector2Value);
                    return f;
                }
                default:
                {
                    var f = new ObjectField { objectType = typeof(Object), allowSceneObjects = false };
                    f.RegisterValueChangedCallback(e => write(p => p.objectReferenceValue = e.newValue));
                    show = p => f.SetValueWithoutNotify(p.objectReferenceValue);
                    return f;
                }
            }
        }

        static VisualElement StaleRow(SerializedObject so, string overridesPath, string name, Action onRemoved)
        {
            var row = new VisualElement();
            row.AddToClassList("bt-value");

            var label = new Label(name);
            label.AddToClassList("bt-value__label");
            row.Add(label);

            var note = new Label("ツリーにないパラメータ") { tooltip = "呼び出すツリーにこの名前のパラメータがないため使われません" };
            note.AddToClassList("bt-value__bound");
            note.AddToClassList("bt-value__bound--missing");
            row.Add(note);

            var remove = new Button(() =>
            {
                so.Update();
                Remove(so, overridesPath, name);
                so.ApplyModifiedProperties();
                onRemoved();
            }) { text = "×", tooltip = "削除" };
            remove.AddToClassList("bt-value__menu");
            row.Add(remove);
            return row;
        }

        static Label Note(string text)
        {
            var label = new Label(text);
            label.AddToClassList("bt-overrides__note");
            return label;
        }

        static bool Compatible(TreeParameterKind a, TreeParameterKind b) =>
            a == b || (a is TreeParameterKind.Int or TreeParameterKind.Float && b is TreeParameterKind.Int or TreeParameterKind.Float);

        static SerializedProperty Find(SerializedObject so, string overridesPath, string name)
        {
            var array = so.FindProperty(overridesPath);
            for (var i = 0; i < array.arraySize; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("name").stringValue == name) return e;
            }
            return null;
        }

        static SerializedProperty FindOrAdd(SerializedObject so, string overridesPath, string name, TreeParameterKind kind)
        {
            var entry = Find(so, overridesPath, name);
            if (entry == null)
            {
                var array = so.FindProperty(overridesPath);
                array.arraySize++;
                entry = array.GetArrayElementAtIndex(array.arraySize - 1);
                entry.FindPropertyRelative("name").stringValue = name;
                entry.FindPropertyRelative("floatValue").floatValue = 0f;
                entry.FindPropertyRelative("intValue").intValue = 0;
                entry.FindPropertyRelative("boolValue").boolValue = false;
                entry.FindPropertyRelative("vector2Value").vector2Value = default;
                entry.FindPropertyRelative("objectValue").objectReferenceValue = null;
                entry.FindPropertyRelative("bindToParent").stringValue = "";
            }
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            return entry;
        }

        static void Remove(SerializedObject so, string overridesPath, string name)
        {
            var array = so.FindProperty(overridesPath);
            for (var i = array.arraySize - 1; i >= 0; i--)
            {
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) array.DeleteArrayElementAtIndex(i);
            }
        }
    }
}

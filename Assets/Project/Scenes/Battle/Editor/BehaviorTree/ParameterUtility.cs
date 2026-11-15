using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEditor;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public static class ParameterUtility
    {
        public const string DragKey = "BehaviorTree.Parameter";

        public sealed class DragPayload
        {
            public string Name;
            public TreeParameterKind Kind;
        }

        // BtXxx 型が受け付けるパラメータの型。先頭が新規作成時の型。
        public static TreeParameterKind[] AcceptedKinds(Type btType)
        {
            if (btType == typeof(BtInt)) return new[] { TreeParameterKind.Int, TreeParameterKind.Float };
            if (btType == typeof(BtFloat)) return new[] { TreeParameterKind.Float, TreeParameterKind.Int };
            if (btType == typeof(BtBool)) return new[] { TreeParameterKind.Bool };
            if (btType == typeof(BtVector2)) return new[] { TreeParameterKind.Vector2 };
            if (btType.IsGenericType && btType.GetGenericTypeDefinition() == typeof(BtObject<>)) return new[] { TreeParameterKind.Object };
            return Array.Empty<TreeParameterKind>();
        }

        public static string ValuePropertyName(TreeParameterKind kind) => kind switch
        {
            TreeParameterKind.Float => "floatValue",
            TreeParameterKind.Int => "intValue",
            TreeParameterKind.Bool => "boolValue",
            TreeParameterKind.Vector2 => "vector2Value",
            _ => "objectValue",
        };

        public static IEnumerable<SerializedProperty> Parameters(SerializedObject so)
        {
            var array = so.FindProperty("parameters");
            if (array == null) yield break;
            for (var i = 0; i < array.arraySize; i++) yield return array.GetArrayElementAtIndex(i);
        }

        public static SerializedProperty Find(SerializedObject so, string name) =>
            Parameters(so).FirstOrDefault(p => p.FindPropertyRelative("name").stringValue == name);

        // ノード側の参照(BtXxx.param)と SubTree の親引き継ぎ(bindToParent)のプロパティパスを列挙する
        public static List<string> FindReferencePaths(SerializedObject so, string name)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(name)) return result;

            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.String) continue;
                if (it.name != "param" && it.name != "bindToParent" && !it.name.EndsWith("Param")) continue;
                if (it.propertyPath.StartsWith("parameters.")) continue;
                if (it.stringValue == name) result.Add(it.propertyPath);
            }
            return result;
        }

        public static int CountUsages(SerializedObject so, string name) => FindReferencePaths(so, name).Count;

        public static void Rename(SerializedObject so, string oldName, string newName)
        {
            if (oldName == newName || string.IsNullOrEmpty(oldName)) return;
            // SubTree の overrides の bindToParent はこのツリーのパラメータを指すので一緒に書き換える
            foreach (var path in FindReferencePaths(so, oldName))
            {
                so.FindProperty(path).stringValue = newName;
            }
        }

        public static string UniqueName(SerializedObject so, string baseName)
        {
            var names = new HashSet<string>(Parameters(so).Select(p => p.FindPropertyRelative("name").stringValue));
            if (!names.Contains(baseName)) return baseName;
            for (var i = 2; ; i++)
            {
                var candidate = baseName + i;
                if (!names.Contains(candidate)) return candidate;
            }
        }

        public static SerializedProperty Add(SerializedObject so, string name, TreeParameterKind kind)
        {
            var array = so.FindProperty("parameters");
            array.arraySize++;
            var p = array.GetArrayElementAtIndex(array.arraySize - 1);
            p.FindPropertyRelative("name").stringValue = UniqueName(so, string.IsNullOrEmpty(name) ? "param" : name);
            p.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            p.FindPropertyRelative("floatValue").floatValue = 0f;
            p.FindPropertyRelative("intValue").intValue = 0;
            p.FindPropertyRelative("boolValue").boolValue = false;
            p.FindPropertyRelative("vector2Value").vector2Value = default;
            p.FindPropertyRelative("objectValue").objectReferenceValue = null;
            p.FindPropertyRelative("bindToParent").stringValue = "";
            return p;
        }

        // 現在の値をそのままパラメータの既定値にコピーする
        public static void CopyValue(SerializedProperty from, SerializedProperty toParameter, TreeParameterKind kind)
        {
            var target = toParameter.FindPropertyRelative(ValuePropertyName(kind));
            switch (from.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (kind == TreeParameterKind.Float) target.floatValue = from.intValue; else target.intValue = from.intValue;
                    break;
                case SerializedPropertyType.Float:
                    if (kind == TreeParameterKind.Int) target.intValue = (int)Math.Round(from.floatValue); else target.floatValue = from.floatValue;
                    break;
                case SerializedPropertyType.Boolean:
                    target.boolValue = from.boolValue;
                    break;
                case SerializedPropertyType.Vector2:
                    target.vector2Value = from.vector2Value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    target.objectReferenceValue = from.objectReferenceValue;
                    break;
            }
        }

        public static string KindLabel(TreeParameterKind kind) => kind switch
        {
            TreeParameterKind.Vector2 => "Vec2",
            TreeParameterKind.Object => "Obj",
            _ => kind.ToString(),
        };
    }
}

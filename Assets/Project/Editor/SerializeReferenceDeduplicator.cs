using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// [SerializeReference] の重複参照（shallow copy）を検出し、
    /// JSON 経由のディープコピーで独立させるユーティリティ。
    /// </summary>
    public static class SerializeReferenceDeduplicator
    {
        /// <summary>
        /// 配列要素の指定フィールド同士で重複参照を検出・修正する。
        /// 例: entries[i].signal と entries[j].signal が同一参照の場合にコピーする。
        /// </summary>
        public static void DeduplicateField(SerializedProperty arrayProp, params string[] fieldNames)
        {
            if (arrayProp == null || !arrayProp.isArray) return;

            bool changed = false;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                for (int j = i + 1; j < arrayProp.arraySize; j++)
                {
                    foreach (var fieldName in fieldNames)
                    {
                        changed |= DeduplicateFieldPair(
                            arrayProp.GetArrayElementAtIndex(i).FindPropertyRelative(fieldName),
                            arrayProp.GetArrayElementAtIndex(j).FindPropertyRelative(fieldName));
                    }
                }
            }

            if (changed) arrayProp.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 配列要素自体が [SerializeReference] の場合に重複参照を検出・修正する。
        /// 例: conditions[i] と conditions[j] が同一参照の場合にコピーする。
        /// </summary>
        public static void DeduplicateElements(SerializedProperty arrayProp)
        {
            if (arrayProp == null || !arrayProp.isArray) return;

            bool changed = false;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                for (int j = i + 1; j < arrayProp.arraySize; j++)
                {
                    changed |= DeduplicateFieldPair(
                        arrayProp.GetArrayElementAtIndex(i),
                        arrayProp.GetArrayElementAtIndex(j));
                }
            }

            if (changed) arrayProp.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 対象プロパティが属する親配列を自動検出し、
        /// 兄弟要素の同名フィールドとの重複参照を検出・修正する。
        /// 例: phases[0].exitConditionConfig を渡すと、phases 配列内の全兄弟と比較する。
        /// </summary>
        public static void DeduplicateAmongSiblings(SerializedProperty property, string fieldName)
        {
            var path = property.propertyPath;
            var bracketIndex = path.LastIndexOf('[');
            if (bracketIndex < 0) return;

            var dotBeforeBracket = path.LastIndexOf('.', bracketIndex);
            if (dotBeforeBracket < 0) return;

            var arrayPath = path.Substring(0, dotBeforeBracket);
            var arrayProp = property.serializedObject.FindProperty(arrayPath);
            if (arrayProp == null || !arrayProp.isArray) return;

            DeduplicateField(arrayProp, fieldName);
        }

        /// <summary>
        /// SerializedObject 内の複数配列にまたがって、指定フィールドの重複参照を検出・修正する。
        /// 例: sequenceGroups[0].phases と sequenceGroups[1].phases の間で
        /// exitConditionConfig が共有されている場合にコピーする。
        /// </summary>
        /// <param name="property">任意の配列要素プロパティ（例: sequenceGroups.Array.data[0].phases.Array.data[1]）</param>
        /// <param name="outerArrayFieldName">外側配列のフィールド名（例: "sequenceGroups"）</param>
        /// <param name="innerArrayFieldName">内側配列のフィールド名（例: "phases"）</param>
        /// <param name="fieldName">重複チェック対象のフィールド名（例: "exitConditionConfig"）</param>
        public static void DeduplicateAcrossNestedArrays(
            SerializedProperty property,
            string outerArrayFieldName,
            string innerArrayFieldName,
            string fieldName)
        {
            var outerArrayProp = property.serializedObject.FindProperty(outerArrayFieldName);
            if (outerArrayProp == null || !outerArrayProp.isArray) return;

            // 全内側配列の全要素から対象フィールドを収集
            var allProps = new System.Collections.Generic.List<SerializedProperty>();

            for (int i = 0; i < outerArrayProp.arraySize; i++)
            {
                var innerArrayProp = outerArrayProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(innerArrayFieldName);
                if (innerArrayProp == null || !innerArrayProp.isArray) continue;

                for (int j = 0; j < innerArrayProp.arraySize; j++)
                {
                    var prop = innerArrayProp.GetArrayElementAtIndex(j).FindPropertyRelative(fieldName);
                    if (prop != null) allProps.Add(prop);
                }
            }

            bool changed = false;

            for (int i = 0; i < allProps.Count; i++)
            {
                for (int j = i + 1; j < allProps.Count; j++)
                {
                    changed |= DeduplicateFieldPair(allProps[i], allProps[j]);
                }
            }

            if (changed) property.serializedObject.ApplyModifiedProperties();
        }

        static bool DeduplicateFieldPair(SerializedProperty propA, SerializedProperty propB)
        {
            if (propA == null || propB == null) return false;
            if (propA.propertyType != SerializedPropertyType.ManagedReference) return false;
            if (propB.propertyType != SerializedPropertyType.ManagedReference) return false;

            var refA = propA.managedReferenceValue;
            var refB = propB.managedReferenceValue;

            if (refA == null || refB == null) return false;
            if (!ReferenceEquals(refA, refB)) return false;

            var json = JsonUtility.ToJson(refB);
            var copy = JsonUtility.FromJson(json, refB.GetType());
            propB.managedReferenceValue = copy;
            return true;
        }
    }
}

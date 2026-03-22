using System;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack.Editor
{
    [CustomPropertyDrawer(typeof(AttackTimeline))]
    public class AttackTimelineDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var entriesProp = property.FindPropertyRelative("entries");

            // 描画前に重複参照を検出・修正
            DeduplicateManagedReferences(entriesProp);

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// entries 内の signal / directionProvider が同一の managed reference を
        /// 共有している場合、JSON シリアライズ経由でディープコピーして独立させる。
        /// </summary>
        static void DeduplicateManagedReferences(SerializedProperty entriesProp)
        {
            if (entriesProp == null || !entriesProp.isArray) return;

            bool changed = false;

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                for (int j = i + 1; j < entriesProp.arraySize; j++)
                {
                    changed |= DeduplicateField(entriesProp, i, j, "signal");
                    changed |= DeduplicateField(entriesProp, i, j, "directionProvider");
                }
            }

            if (changed)
            {
                entriesProp.serializedObject.ApplyModifiedProperties();
            }
        }

        static bool DeduplicateField(SerializedProperty entriesProp, int i, int j, string fieldName)
        {
            var propA = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative(fieldName);
            var propB = entriesProp.GetArrayElementAtIndex(j).FindPropertyRelative(fieldName);

            if (propA == null || propB == null) return false;
            if (propA.propertyType != SerializedPropertyType.ManagedReference) return false;
            if (propB.propertyType != SerializedPropertyType.ManagedReference) return false;

            var refA = propA.managedReferenceValue;
            var refB = propB.managedReferenceValue;

            if (refA == null || refB == null) return false;
            if (!ReferenceEquals(refA, refB)) return false;

            // JSON 経由でディープコピー
            var json = JsonUtility.ToJson(refB);
            var copy = JsonUtility.FromJson(json, refB.GetType());
            propB.managedReferenceValue = copy;
            return true;
        }
    }
}

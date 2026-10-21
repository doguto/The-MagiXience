using Project.Editor;
using Project.Scenes.Battle.Scripts.Model.Attack;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor
{
    [CustomPropertyDrawer(typeof(AttackTimeline))]
    public class AttackTimelineDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // AttackTimeline は phases[].entries[] のネスト構造なので、
            // 全 phase の全 entry を横断して重複参照を解消する。
            SerializeReferenceDeduplicator.DeduplicateAcrossNestedArraysRelative(
                property, "phases", "entries", "signal", "directionProvider", "rotationProvider", "sourceIndexProvider");

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

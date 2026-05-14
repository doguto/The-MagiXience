using Project.Editor;
using UnityEditor;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;

namespace Project.Scenes.Battle.Editor
{
    [CustomPropertyDrawer(typeof(CompositeExitConditionConfig))]
    public class CompositeExitConditionConfigDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializeReferenceDeduplicator.DeduplicateElements(
                property.FindPropertyRelative("conditions"));

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

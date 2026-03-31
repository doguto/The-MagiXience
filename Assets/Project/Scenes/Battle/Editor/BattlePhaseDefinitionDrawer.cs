using Project.Editor;
using Project.Scenes.Battle.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor
{
    [CustomPropertyDrawer(typeof(BattlePhaseDefinition))]
    public class BattlePhaseDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializeReferenceDeduplicator.DeduplicateAcrossNestedArrays(
                property, "sequenceGroups", "phases", "exitConditionConfig");
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

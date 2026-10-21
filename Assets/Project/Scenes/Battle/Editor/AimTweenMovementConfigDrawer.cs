using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [CustomPropertyDrawer(typeof(AimTweenMovementConfig))]
    public class AimTweenMovementConfigDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // foldout
            if (!property.isExpanded) return height;

            // duration, x, y, ease dropdown
            int lines = 4 + EaseDrawerHelper.GetCustomCurveLineCount(property);
            height += lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("duration"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("x"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("y"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EaseDrawerHelper.DrawEaseFields(lineRect, property);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}

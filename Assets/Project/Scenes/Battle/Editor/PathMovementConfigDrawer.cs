using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [CustomPropertyDrawer(typeof(PathMovementConfig))]
    public class PathMovementConfigDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // foldout
            if (!property.isExpanded) return height;

            // waypoints, duration, pathType, ease dropdown, isRelative
            int lines = 5 + EaseDrawerHelper.GetCustomCurveLineCount(property);

            // waypoints は配列なので展開時の高さを取得
            var waypoints = property.FindPropertyRelative("waypoints");
            float waypointsHeight = EditorGUI.GetPropertyHeight(waypoints, true);

            height += waypointsHeight
                + (lines - 1) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
                + EditorGUIUtility.standardVerticalSpacing;
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

                // waypoints (配列なので高さが可変)
                var waypoints = property.FindPropertyRelative("waypoints");
                float wpHeight = EditorGUI.GetPropertyHeight(waypoints, true);
                var wpRect = new Rect(lineRect.x, lineRect.y, lineRect.width, wpHeight);
                EditorGUI.PropertyField(wpRect, waypoints, true);
                lineRect = new Rect(lineRect.x, wpRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    lineRect.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("duration"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("pathType"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                lineRect = EaseDrawerHelper.DrawEaseFields(lineRect, property);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("isRelative"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}

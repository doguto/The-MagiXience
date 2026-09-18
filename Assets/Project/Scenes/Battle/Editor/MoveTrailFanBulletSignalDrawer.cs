using Project.Scenes.Battle.Scripts.Model.Attack;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [CustomPropertyDrawer(typeof(MoveTrailFanBulletSignal))]
    public class MoveTrailFanBulletSignalDrawer : PropertyDrawer
    {
        const int FieldLineCount = 7; // targetOffset, isRelative, moveDuration, moveEaseValue, wayCount, spreadAngle, releaseDelay

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // foldout
            if (!property.isExpanded) return height;

            height += FieldLineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
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

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("targetOffset"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("isRelative"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("moveDuration"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EaseDrawerHelper.DrawEaseOnlyPopup(lineRect, property.FindPropertyRelative("moveEaseValue"), "Move Ease");
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("wayCount"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("spreadAngle"));
                lineRect = EaseDrawerHelper.NextLine(lineRect);

                EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("releaseDelay"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}

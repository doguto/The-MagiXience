using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// Ease ドロップダウン + CustomCurve の描画を共有するヘルパー。
    /// </summary>
    internal static class EaseDrawerHelper
    {
        static string[] _displayNames;
        static int[] _easeValues;

        internal static void BuildPopupEntries()
        {
            if (_displayNames != null) return;

            var entries = new List<(string name, int value)>();
            entries.Add(("CustomCurve", TweenMovementConfig.CustomCurveValue));

            foreach (Ease e in Enum.GetValues(typeof(Ease)))
            {
                if (e == Ease.Unset || e == Ease.INTERNAL_Zero || e == Ease.INTERNAL_Custom)
                    continue;
                entries.Add((e.ToString(), (int)e));
            }

            _displayNames = entries.Select(x => x.name).ToArray();
            _easeValues = entries.Select(x => x.value).ToArray();
        }

        /// <summary>
        /// CustomCurve 選択時に追加されるライン数を返す。
        /// </summary>
        internal static int GetCustomCurveLineCount(SerializedProperty property)
        {
            var easeValue = property.FindPropertyRelative("easeValue");
            if (easeValue == null || easeValue.intValue != TweenMovementConfig.CustomCurveValue)
                return 0;

            int lines = 1; // curvePreset
            var preset = property.FindPropertyRelative("curvePreset");
            if (preset != null && preset.objectReferenceValue == null)
                lines += 1; // customCurve
            lines += 1; // save button
            return lines;
        }

        /// <summary>
        /// Ease ドロップダウンを描画し、CustomCurve なら追加フィールドも描画する。
        /// lineRect を進めて返す。
        /// </summary>
        internal static Rect DrawEaseFields(Rect lineRect, SerializedProperty property)
        {
            BuildPopupEntries();

            var easeValue = property.FindPropertyRelative("easeValue");
            int currentIndex = Array.IndexOf(_easeValues, easeValue.intValue);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUI.Popup(lineRect, "Ease", currentIndex, _displayNames);
            easeValue.intValue = _easeValues[newIndex];
            lineRect = NextLine(lineRect);

            if (easeValue.intValue == TweenMovementConfig.CustomCurveValue)
            {
                var curvePreset = property.FindPropertyRelative("curvePreset");
                EditorGUI.PropertyField(lineRect, curvePreset, new GUIContent("Preset"));
                lineRect = NextLine(lineRect);

                if (curvePreset.objectReferenceValue == null)
                {
                    var customCurve = property.FindPropertyRelative("customCurve");
                    EditorGUI.PropertyField(lineRect, customCurve, new GUIContent("Curve"));
                    lineRect = NextLine(lineRect);
                }

                var buttonRect = EditorGUI.IndentedRect(lineRect);
                if (GUI.Button(buttonRect, "Save as Preset..."))
                {
                    SavePreset(property);
                }
                lineRect = NextLine(lineRect);
            }

            return lineRect;
        }

        internal static Rect NextLine(Rect rect)
        {
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return rect;
        }

        static readonly string DefaultSaveDir = Application.dataPath + "/Project/DataStore";

        static void SavePreset(SerializedProperty property)
        {
            var path = ToAssetsPath(EditorUtility.SaveFilePanel(
                "Save Ease Curve Preset",
                DefaultSaveDir,
                "NewEaseCurvePreset",
                "asset"));

            if (string.IsNullOrEmpty(path)) return;

            var preset = ScriptableObject.CreateInstance<EaseCurvePreset>();

            var sourceCurve = property.FindPropertyRelative("customCurve");
            var so = new SerializedObject(preset);
            so.FindProperty("curve").animationCurveValue = sourceCurve.animationCurveValue;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            property.FindPropertyRelative("curvePreset").objectReferenceValue = preset;
            property.serializedObject.ApplyModifiedProperties();
        }

        static string ToAssetsPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            var dataPath = Application.dataPath;
            if (!absolutePath.StartsWith(dataPath))
            {
                Debug.LogError("Assets フォルダ外には保存できません。");
                return null;
            }
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
    }
}

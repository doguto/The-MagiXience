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
    /// Ease ドロップダウン、CustomCurve、読み取り専用グラフの描画を共有するヘルパー。
    /// </summary>
    internal static class EaseDrawerHelper
    {
        internal const float PreviewHeight = 130f;
        internal static float PreviewLayoutHeight => PreviewHeight + EditorGUIUtility.standardVerticalSpacing;

        const int PreviewSampleCount = 128;
        static readonly Vector3[] PreviewPoints = new Vector3[PreviewSampleCount + 1];

        static string[] _displayNames;
        static int[] _easeValues;

        static string[] _easeOnlyDisplayNames;
        static int[] _easeOnlyValues;

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

        internal static void BuildEaseOnlyPopupEntries()
        {
            if (_easeOnlyDisplayNames != null) return;

            var entries = new List<(string name, int value)>();
            foreach (Ease e in Enum.GetValues(typeof(Ease)))
            {
                if (e == Ease.Unset || e == Ease.INTERNAL_Zero || e == Ease.INTERNAL_Custom)
                    continue;
                entries.Add((e.ToString(), (int)e));
            }

            _easeOnlyDisplayNames = entries.Select(x => x.name).ToArray();
            _easeOnlyValues = entries.Select(x => x.value).ToArray();
        }

        /// <summary>
        /// CustomCurve を持たないフィールド向けに、Ease ドロップダウンだけを描画する。
        /// </summary>
        internal static void DrawEaseOnlyPopup(Rect rect, SerializedProperty easeValueProperty, string label = "Ease")
        {
            BuildEaseOnlyPopupEntries();

            int currentIndex = Array.IndexOf(_easeOnlyValues, easeValueProperty.intValue);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUI.Popup(rect, label, currentIndex, _easeOnlyDisplayNames);
            easeValueProperty.intValue = _easeOnlyValues[newIndex];
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
            bool wasMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = easeValue.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(lineRect, "Ease", currentIndex, _displayNames);
            if (EditorGUI.EndChangeCheck())
                easeValue.intValue = _easeValues[newIndex];
            EditorGUI.showMixedValue = wasMixed;
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

            DrawEasePreview(new Rect(lineRect.x, lineRect.y, lineRect.width, PreviewHeight), property);
            lineRect.y += PreviewLayoutHeight;
            return lineRect;
        }

        static void DrawEasePreview(Rect rect, SerializedProperty property)
        {
            if (Event.current.type != EventType.Repaint) return;

            rect = EditorGUI.IndentedRect(rect);
            var easeValue = property.FindPropertyRelative("easeValue");
            bool mixed = easeValue.hasMultipleDifferentValues;
            AnimationCurve curve = null;
            if (easeValue.intValue == TweenMovementConfig.CustomCurveValue)
            {
                var presetProperty = property.FindPropertyRelative("curvePreset");
                mixed |= presetProperty.hasMultipleDifferentValues;
                if (presetProperty.objectReferenceValue is EaseCurvePreset preset)
                {
                    curve = preset.Curve;
                }
                else
                {
                    var curveProperty = property.FindPropertyRelative("customCurve");
                    mixed |= curveProperty.hasMultipleDifferentValues;
                    curve = curveProperty.animationCurveValue;
                }
            }

            if (mixed)
            {
                EditorGUI.HelpBox(rect, "異なるEaseが選択されています。1つ選択するとグラフを表示します。", MessageType.Info);
                return;
            }

            float min = 0f;
            float max = 1f;
            for (int i = 0; i <= PreviewSampleCount; i++)
            {
                float t = i / (float)PreviewSampleCount;
                // 実際のTweenと同じDOTweenの評価を使い、0～1の外側も保持する。
                float value = curve != null
                    ? DOVirtual.EasedValue(0f, 1f, t, curve)
                    : DOVirtual.EasedValue(0f, 1f, t, (Ease)easeValue.intValue,
                        DOTween.defaultEaseOvershootOrAmplitude, DOTween.defaultEasePeriod);
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    EditorGUI.HelpBox(rect, "カーブに表示できない値が含まれています。", MessageType.Warning);
                    return;
                }
                PreviewPoints[i] = new Vector3(t, value, 0f);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            float padding = (max - min) * 0.08f;
            min -= padding;
            max += padding;

            float labelHeight = EditorGUIUtility.singleLineHeight;
            var plot = new Rect(rect.x + 36f, rect.y + labelHeight,
                Mathf.Max(1f, rect.width - 42f), rect.height - labelHeight * 2f);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, labelHeight), "進行度（%）", EditorStyles.miniLabel);
            EditorGUI.DrawRect(plot, EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.9f, 0.9f, 0.9f));

            var gridColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.18f) : new Color(0f, 0f, 0f, 0.18f);
            for (int i = 0; i <= 2; i++)
            {
                float fraction = i * 0.5f;
                float y = plot.yMax - Mathf.InverseLerp(min, max, fraction) * plot.height;
                EditorGUI.DrawRect(new Rect(plot.x, y, plot.width, 1f), gridColor);
                EditorGUI.DrawRect(new Rect(plot.x + fraction * (plot.width - 1f), plot.y, 1f, plot.height), gridColor);
                GUI.Label(new Rect(rect.x, y - labelHeight * 0.5f, 34f, labelHeight),
                    (fraction * 100f).ToString("0"), EditorStyles.miniLabel);
            }

            for (int i = 0; i <= PreviewSampleCount; i++)
            {
                var sample = PreviewPoints[i];
                PreviewPoints[i] = new Vector3(plot.x + sample.x * plot.width,
                    plot.yMax - Mathf.InverseLerp(min, max, sample.y) * plot.height, 0f);
            }

            var previousColor = Handles.color;
            try
            {
                Handles.color = EditorGUIUtility.isProSkin
                    ? new Color(0.3f, 0.85f, 1f) : new Color(0f, 0.4f, 0.7f);
                Handles.DrawAAPolyLine(2f, PreviewPoints);
            }
            finally
            {
                Handles.color = previousColor;
            }

            GUI.Label(new Rect(plot.x, plot.yMax, 30f, labelHeight), "0", EditorStyles.miniLabel);
            GUI.Label(new Rect(plot.xMax - 24f, plot.yMax, 24f, labelHeight), "100", EditorStyles.miniLabel);
            GUI.Label(new Rect(plot.center.x - 28f, plot.yMax, 64f, labelHeight), "時間（%）", EditorStyles.miniLabel);
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

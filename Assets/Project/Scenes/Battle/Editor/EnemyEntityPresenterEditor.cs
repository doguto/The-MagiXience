using UnityEditor;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity.Editor
{
    [CustomEditor(typeof(EnemyEntityPresenter))]
    public class EnemyEntityPresenterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var movementPresetProp = serializedObject.FindProperty("movementPreset");
            var attackPresetProp = serializedObject.FindProperty("attackPreset");

            // Entity Settings
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("contactDamage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lifetime"));

            // Movement
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(movementPresetProp);
            if (movementPresetProp.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("movementSteps"));

                if (GUILayout.Button("Save as Movement Preset"))
                {
                    SaveMovementPreset();
                }
            }

            // Attack
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attack", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletPool"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletDamage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enemySpawnPrefabs"));
            EditorGUILayout.PropertyField(attackPresetProp);
            if (attackPresetProp.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attackTimeline"));

                if (GUILayout.Button("Save as Attack Preset"))
                {
                    SaveAttackPreset();
                }
            }

            // Component References
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Component References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("view"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spriteRenderer"));

            serializedObject.ApplyModifiedProperties();
        }

        static readonly string DefaultSaveDir = Application.dataPath + "/Project/DataStore";

        void SaveMovementPreset()
        {
            var path = ToAssetsPath(EditorUtility.SaveFilePanel(
                "Save Movement Preset", DefaultSaveDir, "MovementPreset", "asset"));
            if (string.IsNullOrEmpty(path)) return;

            var preset = ScriptableObject.CreateInstance<MovementPreset>();
            var presetSO = new SerializedObject(preset);
            var srcProp = serializedObject.FindProperty("movementSteps");
            var dstProp = presetSO.FindProperty("steps");

            CopyArrayProperty(srcProp, dstProp);
            presetSO.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            serializedObject.FindProperty("movementPreset").objectReferenceValue = preset;
            serializedObject.ApplyModifiedProperties();
        }

        void SaveAttackPreset()
        {
            var path = ToAssetsPath(EditorUtility.SaveFilePanel(
                "Save Attack Preset", DefaultSaveDir, "AttackPreset", "asset"));
            if (string.IsNullOrEmpty(path)) return;

            var preset = ScriptableObject.CreateInstance<AttackPreset>();
            var presetSO = new SerializedObject(preset);
            var srcProp = serializedObject.FindProperty("attackTimeline");
            var dstProp = presetSO.FindProperty("attackTimeline");

            CopyPropertyValue(srcProp, dstProp);
            presetSO.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            serializedObject.FindProperty("attackPreset").objectReferenceValue = preset;
            serializedObject.ApplyModifiedProperties();
        }

        static void CopyArrayProperty(SerializedProperty src, SerializedProperty dst)
        {
            dst.arraySize = src.arraySize;
            for (int i = 0; i < src.arraySize; i++)
            {
                dst.GetArrayElementAtIndex(i).managedReferenceValue =
                    src.GetArrayElementAtIndex(i).managedReferenceValue;
            }
        }

        static void CopyPropertyValue(SerializedProperty src, SerializedProperty dst)
        {
            var srcJson = new SerializedObject(src.serializedObject.targetObject);
            var dstObj = dst.serializedObject;

            // プロパティパスを使って直接値をコピー
            var srcIter = src.Copy();
            var dstIter = dst.Copy();
            var endProp = src.Copy();
            endProp.Next(false);

            if (srcIter.Next(true))
            {
                dstIter.Next(true);
                do
                {
                    if (SerializedProperty.EqualContents(srcIter, endProp)) break;

                    switch (srcIter.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            dstIter.intValue = srcIter.intValue;
                            break;
                        case SerializedPropertyType.Boolean:
                            dstIter.boolValue = srcIter.boolValue;
                            break;
                        case SerializedPropertyType.Float:
                            dstIter.floatValue = srcIter.floatValue;
                            break;
                        case SerializedPropertyType.String:
                            dstIter.stringValue = srcIter.stringValue;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            dstIter.objectReferenceValue = srcIter.objectReferenceValue;
                            break;
                        case SerializedPropertyType.ManagedReference:
                            dstIter.managedReferenceValue = srcIter.managedReferenceValue;
                            break;
                        case SerializedPropertyType.Enum:
                            dstIter.enumValueIndex = srcIter.enumValueIndex;
                            break;
                        case SerializedPropertyType.Vector2:
                            dstIter.vector2Value = srcIter.vector2Value;
                            break;
                        case SerializedPropertyType.Vector3:
                            dstIter.vector3Value = srcIter.vector3Value;
                            break;
                        case SerializedPropertyType.AnimationCurve:
                            dstIter.animationCurveValue = srcIter.animationCurveValue;
                            break;
                    }
                } while (srcIter.Next(false) && dstIter.Next(false));
            }
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

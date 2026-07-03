using System.Linq;
using Project.Commons.Debugger.Scripts.Presenter;
using Project.Scripts.Infra;
using Project.Scripts.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.Editor
{
    public class BattleDebugLauncher : EditorWindow
    {
        const string BattleScenePath = "Assets/Project/Scenes/Battle.unity";
        const string StageDataAssetPath = "Assets/Project/DataStore/StageData.asset";
        const string PrefKeyStageNumber = "BattleDebugLauncher.StageNumber";
        const string PrefKeySituation = "BattleDebugLauncher.Situation";

        int stageNumber = 1;
        BattleSituation situation = BattleSituation.Way;
        StageData[] stages = System.Array.Empty<StageData>();

        [MenuItem("Tools/Battle Debug Launcher")]
        public static void ShowWindow()
        {
            GetWindow<BattleDebugLauncher>("Battle Debug Launcher");
        }

        void OnEnable()
        {
            stageNumber = EditorPrefs.GetInt(PrefKeyStageNumber, 1);
            situation = (BattleSituation)EditorPrefs.GetInt(PrefKeySituation, (int)BattleSituation.Way);

            var dataObject = AssetDatabase.LoadAssetAtPath<StageDataObject>(StageDataAssetPath);
            stages = dataObject != null ? dataObject.stageData.ToArray() : System.Array.Empty<StageData>();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Battle Debug Launcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("選択したステージ・シーケンスからBattleシーンを単独で起動します。", MessageType.Info);
            EditorGUILayout.Space();

            if (stages.Length > 0)
            {
                var labels = stages.Select(s => $"{s.stageNumber}: {s.title}").ToArray();
                var index = System.Array.FindIndex(stages, s => s.stageNumber == stageNumber);
                if (index < 0) index = 0;
                index = EditorGUILayout.Popup("Stage", index, labels);
                stageNumber = stages[index].stageNumber;
            }
            else
            {
                stageNumber = EditorGUILayout.IntField("Stage Number", stageNumber);
            }

            situation = (BattleSituation)EditorGUILayout.EnumPopup("Situation", situation);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Battleシーンを起動"))
                {
                    Launch();
                }
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("再生中は起動できません。再生を停止してから実行してください。", MessageType.Warning);
            }
        }

        void Launch()
        {
            EditorPrefs.SetInt(PrefKeyStageNumber, stageNumber);
            EditorPrefs.SetInt(PrefKeySituation, (int)situation);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);

            var initializer = FindFirstObjectByType<DebugRuntimeModelInitializer>();
            if (initializer == null)
            {
                var go = new GameObject("DebugRuntimeModelInitializer");
                initializer = go.AddComponent<DebugRuntimeModelInitializer>();
            }

            var serialized = new SerializedObject(initializer);
            serialized.FindProperty("stageNumber").intValue = stageNumber;
            serialized.FindProperty("situation").enumValueIndex = (int)situation;
            serialized.ApplyModifiedProperties();

            // シーンファイルへは保存しない（デバッグ用の一時的な配置のため）
            EditorApplication.isPlaying = true;
        }
    }
}

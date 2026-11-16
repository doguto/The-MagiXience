using System.Linq;
using Project.Commons.Debugger.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scripts.Infra;
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
        const string PrefKeyStartSection = "BattleDebugLauncher.StartSection";

        static readonly Vector2 WindowMinSize = new(360f, 200f);
        static readonly Vector2 WindowInitialSize = new(420f, 240f);

        int stageNumber = 1;
        int startSection;
        StageData[] stages = System.Array.Empty<StageData>();

        // 選択中のステージのツリーから作った Section 一覧のキャッシュ
        string[] sectionLabels = System.Array.Empty<string>();
        int sectionCacheStage = -1;
        string sectionUnavailableReason;

        [MenuItem("Tools/Battle Debug Launcher")]
        public static void ShowWindow()
        {
            var window = GetWindow<BattleDebugLauncher>("Battle Debug Launcher");
            window.minSize = WindowMinSize;
            window.position = new Rect(window.position.position, WindowInitialSize);
        }

        void OnEnable()
        {
            stageNumber = EditorPrefs.GetInt(PrefKeyStageNumber, 1);
            startSection = EditorPrefs.GetInt(PrefKeyStartSection, 0);

            var dataObject = AssetDatabase.LoadAssetAtPath<StageDataObject>(StageDataAssetPath);
            stages = dataObject != null ? dataObject.stageData.ToArray() : System.Array.Empty<StageData>();
            sectionCacheStage = -1;
        }

        // アセットを編集して戻ってきたときに一覧を取り直す
        void OnFocus()
        {
            sectionCacheStage = -1;
            Repaint();
        }

        void OnGUI()
        {
            // 描画中にコントロール数が変わらないよう、一覧の再構築はLayoutパスに限定する
            if (Event.current.type == EventType.Layout) RefreshSectionLabelsIfNeeded();

            EditorGUILayout.LabelField("Battle Debug Launcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("選択したステージ・開始 Section からBattleシーンを単独で起動します。途中の Section から始めると、その Section の会話は飛ばします（リトライと同じ）。", MessageType.Info);
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

            DrawStartSectionField();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Battleシーンを起動")) Launch();
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("再生中は起動できません。再生を停止してから実行してください。", MessageType.Warning);
            }

            // ステージが変わった直後は次のLayoutパスで一覧を作り直す必要があるため再描画を促す
            if (sectionCacheStage != stageNumber) Repaint();
        }

        void DrawStartSectionField()
        {
            if (sectionLabels.Length == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup("Start Section", 0, new[] { "(なし)" });
                }
                if (!string.IsNullOrEmpty(sectionUnavailableReason)) EditorGUILayout.HelpBox(sectionUnavailableReason, MessageType.Warning);
                startSection = 0;
                return;
            }

            startSection = Mathf.Clamp(startSection, 0, sectionLabels.Length - 1);
            startSection = EditorGUILayout.Popup("Start Section", startSection, sectionLabels);
        }

        void RefreshSectionLabelsIfNeeded()
        {
            if (sectionCacheStage == stageNumber) return;

            sectionCacheStage = stageNumber;
            sectionLabels = System.Array.Empty<string>();
            sectionUnavailableReason = null;

            var stage = stages.FirstOrDefault(s => s.stageNumber == stageNumber);
            if (stage == null)
            {
                sectionUnavailableReason = $"StageData に Stage {stageNumber} の定義がありません。";
                return;
            }
            if (string.IsNullOrEmpty(stage.stageAddress))
            {
                sectionUnavailableReason = $"Stage {stageNumber} の stageAddress が StageData に設定されていません。";
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<BattleStageAsset>(stage.stageAddress);
            if (asset == null || asset.Tree == null)
            {
                sectionUnavailableReason = $"BattleStageAsset またはそのツリーを読み込めませんでした: {stage.stageAddress}";
                return;
            }
            if (asset.Tree.Root is not Scenes.Battle.Scripts.BehaviorTree.Stage root)
            {
                sectionUnavailableReason = $"{asset.Tree.name} のルートが Stage ノードではありません。";
                return;
            }

            sectionLabels = root.Children
                                .Select((c, i) => c is Scenes.Battle.Scripts.BehaviorTree.Section s ? $"{i}: {s.Label} [{s.Situation}]" : $"{i}: (Section ではない)")
                                .ToArray();
        }

        void Launch()
        {
            EditorPrefs.SetInt(PrefKeyStageNumber, stageNumber);
            EditorPrefs.SetInt(PrefKeyStartSection, startSection);

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
            serialized.FindProperty("startSection").intValue = startSection;
            serialized.ApplyModifiedProperties();

            // シーンファイルへは保存しない（デバッグ用の一時的な配置のため）
            EditorApplication.isPlaying = true;
        }
    }
}

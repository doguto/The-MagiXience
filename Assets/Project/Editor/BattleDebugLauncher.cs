using System.Collections.Generic;
using System.Linq;
using Project.Commons.Debugger.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Model;
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
        const string PrefKeyStartPhaseIndex = "BattleDebugLauncher.StartPhaseIndex";

        static readonly Vector2 WindowMinSize = new(360f, 220f);
        static readonly Vector2 WindowInitialSize = new(420f, 260f);

        int stageNumber = 1;
        BattleSituation situation = BattleSituation.Way;
        int startPhaseIndex;
        StageData[] stages = System.Array.Empty<StageData>();

        // 選択中のStage/Situationに対応するシーケンスから作ったフェーズ一覧のキャッシュ
        string[] phaseLabels = System.Array.Empty<string>();
        string phaseCacheKey;
        string phaseUnavailableReason;

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
            situation = (BattleSituation)EditorPrefs.GetInt(PrefKeySituation, (int)BattleSituation.Way);
            startPhaseIndex = EditorPrefs.GetInt(PrefKeyStartPhaseIndex, 0);

            var dataObject = AssetDatabase.LoadAssetAtPath<StageDataObject>(StageDataAssetPath);
            stages = dataObject != null ? dataObject.stageData.ToArray() : System.Array.Empty<StageData>();

            phaseCacheKey = null;
        }

        // アセットを編集して戻ってきたときにフェーズ一覧を取り直す
        void OnFocus()
        {
            phaseCacheKey = null;
            Repaint();
        }

        void OnGUI()
        {
            // 描画中にコントロール数が変わらないよう、一覧の再構築はLayoutパスに限定する
            if (Event.current.type == EventType.Layout)
            {
                RefreshPhaseLabelsIfNeeded();
            }

            EditorGUILayout.LabelField("Battle Debug Launcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("選択したステージ・シーケンス・開始フェーズからBattleシーンを単独で起動します。", MessageType.Info);
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

            DrawStartPhaseField();

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

            // Stage/Situationが変わった直後は次のLayoutパスで一覧を作り直す必要があるため再描画を促す
            if (phaseCacheKey != BuildPhaseCacheKey())
            {
                Repaint();
            }
        }

        void DrawStartPhaseField()
        {
            if (phaseLabels.Length == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup("Start Phase", 0, new[] { "(なし)" });
                }

                if (!string.IsNullOrEmpty(phaseUnavailableReason))
                {
                    EditorGUILayout.HelpBox(phaseUnavailableReason, MessageType.Warning);
                }

                startPhaseIndex = 0;
                return;
            }

            startPhaseIndex = Mathf.Clamp(startPhaseIndex, 0, phaseLabels.Length - 1);
            startPhaseIndex = EditorGUILayout.Popup("Start Phase", startPhaseIndex, phaseLabels);
        }

        string BuildPhaseCacheKey()
        {
            return $"{stageNumber}/{situation}";
        }

        void RefreshPhaseLabelsIfNeeded()
        {
            var cacheKey = BuildPhaseCacheKey();
            if (phaseCacheKey == cacheKey) return;

            phaseCacheKey = cacheKey;
            phaseLabels = System.Array.Empty<string>();
            phaseUnavailableReason = null;

            var stage = stages.FirstOrDefault(s => s.stageNumber == stageNumber);
            if (stage == null)
            {
                phaseUnavailableReason = $"StageData に Stage {stageNumber} の定義がありません。";
                return;
            }

            var address = situation == BattleSituation.Boss ? stage.bossSequenceAddress : stage.waySequenceAddress;
            if (string.IsNullOrEmpty(address))
            {
                phaseUnavailableReason = $"Stage {stageNumber} の {situation} シーケンスが StageData に設定されていません。";
                return;
            }

            var sequenceAsset = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(address);
            if (sequenceAsset == null)
            {
                phaseUnavailableReason = $"シーケンスアセットを読み込めませんでした: {address}";
                return;
            }

            phaseLabels = BuildPhaseLabels(sequenceAsset);
            if (phaseLabels.Length == 0)
            {
                phaseUnavailableReason = $"{sequenceAsset.name} にフェーズが登録されていません。";
            }
        }

        // シーケンス内の全グループを平坦化し、TimelineBuilderアセット名をそのままラベルにする。
        // Popupの選択値は「実行順の通し番号」なので、ラベルが重複していても選択自体は破綻しない。
        static string[] BuildPhaseLabels(BattleSequenceAsset sequenceAsset)
        {
            var labels = new List<string>();

            foreach (var group in sequenceAsset.SequenceGroups)
            foreach (var phase in group.Phases)
                labels.Add(phase.TimelineBuilder != null ? phase.TimelineBuilder.name : "(no timeline)");

            return labels.ToArray();
        }

        void Launch()
        {
            EditorPrefs.SetInt(PrefKeyStageNumber, stageNumber);
            EditorPrefs.SetInt(PrefKeySituation, (int)situation);
            EditorPrefs.SetInt(PrefKeyStartPhaseIndex, startPhaseIndex);

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
            serialized.FindProperty("startPhaseIndex").intValue = startPhaseIndex;
            serialized.ApplyModifiedProperties();

            // シーンファイルへは保存しない（デバッグ用の一時的な配置のため）
            EditorApplication.isPlaying = true;
        }
    }
}

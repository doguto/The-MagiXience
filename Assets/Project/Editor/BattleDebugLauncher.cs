using System.Collections.Generic;
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
        const string PrefKeyBossPhase = "BattleDebugLauncher.BossStartPhase";
        const string PrefKeyBossHp = "BattleDebugLauncher.BossStartHpPercent";

        static readonly Vector2 WindowMinSize = new(360f, 200f);
        static readonly Vector2 WindowInitialSize = new(420f, 300f);

        int stageNumber = 1;
        int startSection;
        StageData[] stages = System.Array.Empty<StageData>();

        // 選択中のステージのツリーから作った Section 一覧のキャッシュ
        string[] sectionLabels = System.Array.Empty<string>();
        int sectionCacheStage = -1;
        string sectionUnavailableReason;

        // 選択中の Section で戦うボスのツリーにある Phase 名。ボス戦の Section でなければ null
        string[] bossPhases;
        int bossPhaseCacheStage = -1;
        int bossPhaseCacheSection = -1;
        string bossStartPhase = "";
        float bossStartHpPercent = 100f;

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
            bossStartPhase = EditorPrefs.GetString(PrefKeyBossPhase, "");
            bossStartHpPercent = EditorPrefs.GetFloat(PrefKeyBossHp, 100f);

            var dataObject = AssetDatabase.LoadAssetAtPath<StageDataObject>(StageDataAssetPath);
            stages = dataObject != null ? dataObject.stageData.ToArray() : System.Array.Empty<StageData>();
            sectionCacheStage = -1;
        }

        // アセットを編集して戻ってきたときに一覧を取り直す
        void OnFocus()
        {
            sectionCacheStage = -1;
            bossPhaseCacheStage = -1;
            Repaint();
        }

        void OnGUI()
        {
            // 描画中にコントロール数が変わらないよう、一覧の再構築はLayoutパスに限定する
            if (Event.current.type == EventType.Layout)
            {
                RefreshSectionLabelsIfNeeded();
                RefreshBossPhasesIfNeeded();
            }

            EditorGUILayout.LabelField("Battle Debug Launcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("選択したステージ・開始 Section からBattleシーンを単独で起動します。途中の Section から始めると、その Section の会話は飛ばします（リトライと同じ）。ボス戦の Section では開始 Phase と HP も選べます。", MessageType.Info);
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
            DrawBossFields();
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
            if (sectionCacheStage != stageNumber || bossPhaseCacheStage != stageNumber || bossPhaseCacheSection != startSection) Repaint();
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

        void DrawBossFields()
        {
            if (bossPhases == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ボス戦", EditorStyles.boldLabel);
            var labels = new[] { "(最初から)" }.Concat(bossPhases).ToArray();
            var index = Mathf.Max(0, System.Array.IndexOf(bossPhases, bossStartPhase) + 1);
            index = EditorGUILayout.Popup(new GUIContent("Start Phase", "この Phase に着くまでの Phase を飛ばす。BGM もその位置へ進める"), index, labels);
            bossStartPhase = index == 0 ? "" : bossPhases[index - 1];
            bossStartHpPercent = EditorGUILayout.Slider(new GUIContent("Start HP (%)", "ボス戦開始時のボスの HP"), bossStartHpPercent, 1f, 100f);
        }

        void RefreshBossPhasesIfNeeded()
        {
            if (bossPhaseCacheStage == stageNumber && bossPhaseCacheSection == startSection) return;
            bossPhaseCacheStage = stageNumber;
            bossPhaseCacheSection = startSection;
            bossPhases = null;

            var stage = stages.FirstOrDefault(s => s.stageNumber == stageNumber);
            if (stage == null || string.IsNullOrEmpty(stage.stageAddress)) return;
            var asset = AssetDatabase.LoadAssetAtPath<BattleStageAsset>(stage.stageAddress);
            if (asset?.Tree?.Root is not Scenes.Battle.Scripts.BehaviorTree.Stage root) return;
            if (startSection < 0 || startSection >= root.Children.Count) return;

            var section = root.Children[startSection];
            if (section is not Scenes.Battle.Scripts.BehaviorTree.Section { Situation: Scripts.Model.BattleSituation.Boss }) return;

            var nodes = Descendants(section).ToList();
            var tree = nodes.OfType<BossBattle>().Select(b => b.Tree).FirstOrDefault(t => t != null)
                       ?? nodes.OfType<SpawnBoss>().Select(s => s.Boss != null ? s.Boss.Tree : null).FirstOrDefault(t => t != null);
            bossPhases = tree != null
                ? Descendants(tree.Root).OfType<Phase>().Select(p => p.PhaseId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToArray()
                : System.Array.Empty<string>();
        }

        static IEnumerable<Scenes.Battle.Scripts.BehaviorTree.Node> Descendants(Scenes.Battle.Scripts.BehaviorTree.Node node)
        {
            if (node == null) yield break;
            yield return node;
            foreach (var child in node.Children)
            {
                foreach (var d in Descendants(child)) yield return d;
            }
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
            EditorPrefs.SetString(PrefKeyBossPhase, bossStartPhase);
            EditorPrefs.SetFloat(PrefKeyBossHp, bossStartHpPercent);

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
            var isBoss = bossPhases != null;
            serialized.FindProperty("bossStartPhase").stringValue = isBoss ? bossStartPhase : "";
            serialized.FindProperty("bossStartHpPercent").floatValue = isBoss ? bossStartHpPercent : 100f;
            serialized.ApplyModifiedProperties();

            // シーンファイルへは保存しない（デバッグ用の一時的な配置のため）
            EditorApplication.isPlaying = true;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Project.Commons.Debugger.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scripts.Infra;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using BtNode = Project.Scenes.Battle.Scripts.BehaviorTree.Node;
using BtStage = Project.Scenes.Battle.Scripts.BehaviorTree.Stage;

namespace Project.Editor
{
    public class BattleDebugLauncher : EditorWindow
    {
        const string BattleScenePath = "Assets/Project/Scenes/Battle.unity";
        const string StageDataAssetPath = "Assets/Project/DataStore/StageData.asset";
        const string PrefKeyStageNumber = "BattleDebugLauncher.StageNumber";
        const string PrefKeyStartPath = "BattleDebugLauncher.StartNodePath.";
        const string PrefKeyBossHp = "BattleDebugLauncher.BossStartHpPercent";

        static readonly Vector2 WindowMinSize = new(380f, 360f);
        static readonly Vector2 WindowInitialSize = new(480f, 640f);

        int stageNumber = 1;
        StageData[] stages = System.Array.Empty<StageData>();
        float bossStartHpPercent = 100f;

        [SerializeField] TreeViewState treeState;
        StartNodeTreeView treeView;
        SearchField searchField;
        int treeCacheStage = -1;
        string treeUnavailableReason;

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
            bossStartHpPercent = EditorPrefs.GetFloat(PrefKeyBossHp, 100f);

            var dataObject = AssetDatabase.LoadAssetAtPath<StageDataObject>(StageDataAssetPath);
            stages = dataObject != null ? dataObject.stageData.ToArray() : System.Array.Empty<StageData>();

            treeState ??= new TreeViewState();
            treeView = new StartNodeTreeView(treeState);
            treeView.OnSelected += entry => EditorPrefs.SetString(PrefKeyStartPath + stageNumber, entry.Path);
            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
            treeCacheStage = -1;
        }

        // アセットを編集して戻ってきたときに一覧を取り直す
        void OnFocus()
        {
            treeCacheStage = -1;
            Repaint();
        }

        void OnGUI()
        {
            // 描画中にコントロール数が変わらないよう、一覧の再構築はLayoutパスに限定する
            if (Event.current.type == EventType.Layout) RebuildTreeIfNeeded();

            EditorGUILayout.HelpBox("ステージのツリーから開始ノードを選んで Battle シーンを単独で起動します。選んだノードより前は飛ばし、BGM の位置・ボスの登場など後に必要な状態だけ進めます。Section の会話は飛ばします（リトライと同じ）。", MessageType.Info);

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

            DrawStartNodeTree();

            var selected = treeView.Selected;
            if (selected is { CrossesBossBattle: true })
            {
                bossStartHpPercent = EditorGUILayout.Slider(new GUIContent("Start HP (%)", "ボス戦開始時のボスの HP"), bossStartHpPercent, 1f, 100f);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || selected == null))
            {
                if (GUILayout.Button("Battleシーンを起動", GUILayout.Height(28f))) Launch(selected);
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("再生中は起動できません。再生を停止してから実行してください。", MessageType.Warning);
            }

            // ステージが変わった直後は次のLayoutパスで一覧を作り直す必要があるため再描画を促す
            if (treeCacheStage != stageNumber) Repaint();
        }

        void DrawStartNodeTree()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start Node", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(treeUnavailableReason))
            {
                EditorGUILayout.HelpBox(treeUnavailableReason, MessageType.Warning);
                return;
            }

            treeView.searchString = searchField.OnGUI(treeView.searchString);
            var rect = GUILayoutUtility.GetRect(0f, 10000f, 160f, 10000f);
            treeView.OnGUI(rect);

            var selected = treeView.Selected;
            EditorGUILayout.LabelField(selected != null ? selected.Breadcrumb : "(未選択)", EditorStyles.wordWrappedMiniLabel);
        }

        void RebuildTreeIfNeeded()
        {
            if (treeCacheStage == stageNumber) return;
            treeCacheStage = stageNumber;
            treeUnavailableReason = null;

            var stage = stages.FirstOrDefault(s => s.stageNumber == stageNumber);
            if (stage == null)
            {
                treeUnavailableReason = $"StageData に Stage {stageNumber} の定義がありません。";
                treeView.SetRoot(null, null);
                return;
            }
            if (string.IsNullOrEmpty(stage.stageAddress))
            {
                treeUnavailableReason = $"Stage {stageNumber} の stageAddress が StageData に設定されていません。";
                treeView.SetRoot(null, null);
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<BattleStageAsset>(stage.stageAddress);
            if (asset == null || asset.Tree == null)
            {
                treeUnavailableReason = $"BattleStageAsset またはそのツリーを読み込めませんでした: {stage.stageAddress}";
                treeView.SetRoot(null, null);
                return;
            }
            if (asset.Tree.Root is not BtStage)
            {
                treeUnavailableReason = $"{asset.Tree.name} のルートが Stage ノードではありません。";
                treeView.SetRoot(null, null);
                return;
            }

            treeView.SetRoot(StartNodeTreeBuilder.Build(asset.Tree), EditorPrefs.GetString(PrefKeyStartPath + stageNumber, ""));
        }

        void Launch(StartNodeEntry entry)
        {
            EditorPrefs.SetInt(PrefKeyStageNumber, stageNumber);
            EditorPrefs.SetString(PrefKeyStartPath + stageNumber, entry.Path);
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
            serialized.FindProperty("startSection").intValue = Mathf.Max(0, entry.Section);
            // ルートや Section そのものを選んだときは経路を渡さず、今までどおり Section の頭から始める
            serialized.FindProperty("startNodePath").stringValue = entry.Depth >= 2 ? entry.Path : "";
            serialized.FindProperty("bossStartHpPercent").floatValue = entry.CrossesBossBattle ? bossStartHpPercent : 100f;
            serialized.ApplyModifiedProperties();

            // シーンファイルへは保存しない（デバッグ用の一時的な配置のため）
            EditorApplication.isPlaying = true;
        }
    }

    sealed class StartNodeEntry
    {
        public string Path;
        public string Label;
        public string Breadcrumb;
        // ルートからの深さ。0 = Stage, 1 = Section
        public int Depth;
        public int Section = -1;
        public bool CrossesBossBattle;
        public List<StartNodeEntry> Children = new();
    }

    // ステージツリーを、SubTree とボスのツリーの中まで展開して一覧にする
    static class StartNodeTreeBuilder
    {
        public static StartNodeEntry Build(StageTreeAsset tree)
        {
            BossDefinition lastBoss = null;
            return Visit(tree.Root, null, null, new List<string>(), new HashSet<BehaviorTreeAsset> { tree }, ref lastBoss);
        }

        // Section の番号は、親が Stage (深さ 0) のとき子として何番目か。実行時と同じく null の子は数えない
        static StartNodeEntry Visit(BtNode node, string treeName, StartNodeEntry parent, List<string> path, HashSet<BehaviorTreeAsset> visiting, ref BossDefinition lastBoss)
        {
            path.Add(node.Guid);
            var label = LabelOf(node);
            var entry = new StartNodeEntry
            {
                Path = string.Join(",", path),
                Label = treeName != null ? $"[{treeName}] {label}" : label,
                Depth = path.Count - 1,
                Section = parent == null ? 0 : parent.Depth == 0 ? parent.Children.Count : parent.Section,
                CrossesBossBattle = parent is { CrossesBossBattle: true } || node is BossBattle,
            };
            entry.Breadcrumb = parent == null ? label : parent.Breadcrumb + "  >  " + label;

            if (node is SpawnBoss spawn && spawn.Boss != null) lastBoss = spawn.Boss;

            foreach (var child in node.Children)
            {
                if (child == null) continue;
                entry.Children.Add(Visit(child, null, entry, path, visiting, ref lastBoss));
            }

            var inner = InnerTree(node, lastBoss);
            if (inner != null && visiting.Add(inner))
            {
                entry.Children.Add(Visit(inner.Root, inner.name, entry, path, visiting, ref lastBoss));
                visiting.Remove(inner);
            }

            path.RemoveAt(path.Count - 1);
            return entry;
        }

        // 実行時に子ではなく別のツリーへ続くノード。treeParam で実行時に決まる SubTree はたどれない
        static BehaviorTreeAsset InnerTree(BtNode node, BossDefinition lastBoss)
        {
            BehaviorTreeAsset tree = node switch
            {
                SubTree sub when string.IsNullOrEmpty(sub.TreeParam) => sub.Tree,
                BossBattle battle => battle.Tree != null ? battle.Tree : lastBoss != null ? lastBoss.Tree : null,
                _ => null,
            };
            return tree != null && tree.Root != null ? tree : null;
        }

        static string LabelOf(BtNode node)
        {
            var type = ObjectNames.NicifyVariableName(node.GetType().Name);
            if (!string.IsNullOrEmpty(node.Comment)) return $"{type} «{node.Comment}»";
            var summary = node.Summary;
            if (string.IsNullOrEmpty(summary)) return type;
            var firstLine = summary.Split('\n')[0];
            return $"{type}  {firstLine}";
        }
    }

    sealed class StartNodeTreeView : TreeView
    {
        readonly Dictionary<int, StartNodeEntry> entries = new();
        StartNodeEntry rootEntry;

        public event System.Action<StartNodeEntry> OnSelected;

        public StartNodeTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        public StartNodeEntry Selected
        {
            get
            {
                var ids = GetSelection();
                return ids.Count > 0 && entries.TryGetValue(ids[0], out var entry) ? entry : null;
            }
        }

        public void SetRoot(StartNodeEntry root, string selectedPath)
        {
            rootEntry = root;
            Reload();
            if (root == null) return;

            var target = entries.Values.FirstOrDefault(e => e.Path == selectedPath) ?? root;
            var id = IdOf(target);
            SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
            if (state.expandedIDs.Count == 0) SetExpanded(IdOf(root), true);
        }

        protected override TreeViewItem BuildRoot()
        {
            entries.Clear();
            var hidden = new TreeViewItem { id = 0, depth = -1, displayName = "" };
            if (rootEntry != null) hidden.AddChild(ToItem(rootEntry));
            else hidden.children = new List<TreeViewItem>();
            SetupDepthsFromParentsAndChildren(hidden);
            return hidden;
        }

        TreeViewItem ToItem(StartNodeEntry entry)
        {
            var item = new TreeViewItem { id = IdOf(entry), displayName = entry.Label };
            entries[item.id] = entry;
            foreach (var child in entry.Children) item.AddChild(ToItem(child));
            return item;
        }

        // 再構築しても開閉状態が残るよう、経路から ID を決める
        int IdOf(StartNodeEntry entry)
        {
            var id = entry.Path.GetHashCode();
            if (id == 0) id = 1;
            while (entries.TryGetValue(id, out var other) && !ReferenceEquals(other, entry)) id++;
            return id;
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0 && entries.TryGetValue(selectedIds[0], out var entry)) OnSelected?.Invoke(entry);
        }
    }
}

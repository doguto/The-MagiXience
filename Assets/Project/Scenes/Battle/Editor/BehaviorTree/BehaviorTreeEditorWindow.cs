using Project.Scenes.Battle.Scripts.BehaviorTree;
using UniRx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        [SerializeField] BehaviorTreeAsset asset;
        BehaviorTreeGraphView graphView;
        Label titleLabel;
        ObjectField assetField;
        NodePaletteView palette;
        ParameterPanelView parameterPanel;
        NodeInspectorView nodeInspector;

        [MenuItem("Window/Battle/Behavior Tree Editor")]
        public static void OpenEmpty() => Open(null);

        public static void Open(BehaviorTreeAsset target)
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.titleContent = new GUIContent("Behavior Tree");
            window.SetAsset(target);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not BehaviorTreeAsset tree) return false;
            Open(tree);
            return true;
        }

        void CreateGUI()
        {
            var toolbar = new Toolbar();
            assetField = new ObjectField { objectType = typeof(BehaviorTreeAsset), allowSceneObjects = false, value = asset };
            assetField.style.width = 260;
            assetField.RegisterValueChangedCallback(e => SetAsset(e.newValue as BehaviorTreeAsset));
            toolbar.Add(assetField);
            toolbar.Add(new ToolbarButton(() => graphView?.FrameWholeGraph()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => graphView?.AutoLayout()) { text = "Auto Layout" });
            var blackboardToggle = new ToolbarToggle { text = "Side Panel", value = true };
            toolbar.Add(blackboardToggle);
            toolbar.Add(new ToolbarButton(() => graphView?.ToggleMiniMap()) { text = "MiniMap" });
            toolbar.Add(new ToolbarSpacer { flex = true });
            titleLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleRight, marginRight = 8, color = new Color(0.55f, 0.55f, 0.55f) } };
            toolbar.Add(titleLabel);
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, 230, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "bt-editor-split" };
            split.style.flexGrow = 1;

            palette = new NodePaletteView();
            palette.SetAssetProvider(() => asset);
            split.Add(palette);

            var rightSplit = new TwoPaneSplitView(1, 320, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "bt-editor-split-right" };
            graphView = new BehaviorTreeGraphView(this);
            graphView.style.flexGrow = 1;
            rightSplit.Add(graphView);

            parameterPanel = new ParameterPanelView();
            parameterPanel.OnUsageClicked.Subscribe(graphView.SelectNodesUsingParameter);
            parameterPanel.OnChanged.Subscribe(_ => graphView.RefreshTexts());

            nodeInspector = new NodeInspectorView();
            graphView.OnSelectionChanged.Subscribe(_ => nodeInspector.Show(graphView.SelectedNodes));

            var tabs = new TabView { viewDataKey = "bt-editor-side-tabs" };
            tabs.AddToClassList("bt-side-tabs");
            var nodeTab = new Tab("Node");
            nodeTab.Add(nodeInspector);
            tabs.Add(nodeTab);
            var parameterTab = new Tab("Parameters");
            parameterTab.Add(parameterPanel);
            tabs.Add(parameterTab);
            rightSplit.Add(tabs);

            split.Add(rightSplit);
            blackboardToggle.RegisterValueChangedCallback(e =>
            {
                if (e.newValue) rightSplit.UnCollapse();
                else rightSplit.CollapseChild(1);
            });
            rootVisualElement.Add(split);

            var styleSheet = BehaviorTreeGraphView.LoadStyleSheet();
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

            Undo.undoRedoPerformed += OnUndoRedo;
            SetAsset(asset);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            graphView?.Reload();
            parameterPanel?.Rebuild();
        }

        void SetAsset(BehaviorTreeAsset target)
        {
            asset = target;
            assetField?.SetValueWithoutNotify(asset);
            if (titleLabel != null) titleLabel.text = asset ? $"{AssetDatabase.GetAssetPath(asset)}  [{asset.GetType().Name.Replace("TreeAsset", "")}]" : "(no asset)";
            graphView?.Load(asset);
            parameterPanel?.SetAsset(asset);
            palette?.Rebuild();
        }

        void OnProjectChange() => palette?.Rebuild();

        void OnInspectorUpdate()
        {
            if (!EditorApplication.isPlaying) return;
            graphView?.RefreshRuntimeState();
            nodeInspector?.RefreshRuntime();
        }
    }
}

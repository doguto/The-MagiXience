using Project.Scenes.Battle.Scripts.BehaviorTree;
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
            assetField.style.minWidth = 240;
            assetField.RegisterValueChangedCallback(e => SetAsset(e.newValue as BehaviorTreeAsset));
            toolbar.Add(assetField);
            toolbar.Add(new ToolbarButton(() => graphView?.AutoLayout()) { text = "Auto Layout" });
            toolbar.Add(new ToolbarButton(() => graphView?.FrameWholeGraph()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => graphView?.ToggleMiniMap()) { text = "MiniMap" });
            toolbar.Add(new ToolbarButton(() => graphView?.Reload()) { text = "Reload" });
            titleLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };
            toolbar.Add(titleLabel);
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, 230, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "bt-editor-split" };
            split.style.flexGrow = 1;

            palette = new NodePaletteView();
            palette.SetAssetProvider(() => asset);
            split.Add(palette);

            var rightSplit = new TwoPaneSplitView(1, 250, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "bt-editor-split-right" };
            graphView = new BehaviorTreeGraphView(this);
            graphView.style.flexGrow = 1;
            rightSplit.Add(graphView);

            parameterPanel = new ParameterPanelView();
            parameterPanel.UsageClicked += name => graphView.SelectNodesUsingParameter(name);
            parameterPanel.Changed += () => graphView.Reload();
            rightSplit.Add(parameterPanel);

            split.Add(rightSplit);
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
            if (titleLabel != null) titleLabel.text = asset ? AssetDatabase.GetAssetPath(asset) : "(no asset)";
            graphView?.Load(asset);
            parameterPanel?.SetAsset(asset);
            palette?.Rebuild();
        }

        void OnProjectChange() => palette?.Rebuild();

        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) graphView?.RefreshRuntimeState();
        }
    }
}

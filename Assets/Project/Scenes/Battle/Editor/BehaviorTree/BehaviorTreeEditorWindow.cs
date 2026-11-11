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
            var assetField = new ObjectField { objectType = typeof(BehaviorTreeAsset), allowSceneObjects = false, value = asset };
            assetField.style.minWidth = 240;
            assetField.RegisterValueChangedCallback(e => SetAsset(e.newValue as BehaviorTreeAsset));
            toolbar.Add(assetField);
            toolbar.Add(new ToolbarButton(() => graphView?.FrameAll()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => graphView?.Reload()) { text = "Reload" });
            titleLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };
            toolbar.Add(titleLabel);
            rootVisualElement.Add(toolbar);

            graphView = new BehaviorTreeGraphView(this);
            graphView.style.flexGrow = 1;
            rootVisualElement.Add(graphView);

            Undo.undoRedoPerformed += OnUndoRedo;
            SetAsset(asset);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo() => graphView?.Reload();

        void SetAsset(BehaviorTreeAsset target)
        {
            asset = target;
            if (titleLabel != null) titleLabel.text = asset ? AssetDatabase.GetAssetPath(asset) : "(no asset)";
            graphView?.Load(asset);
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) graphView?.RefreshRuntimeState();
        }
    }
}

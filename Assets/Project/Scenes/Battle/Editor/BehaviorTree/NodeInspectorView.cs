using System.Collections.Generic;
using System.Reflection;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UniRx;
using UnityEditor;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 右ペインの Node タブ。選択中のノードの種類・説明・コメント・設定欄を出す
    public class NodeInspectorView : VisualElement
    {
        readonly ScrollView scroll;
        readonly SerialDisposable valuesSubscription = new();
        BehaviorTreeNodeView target;
        Label summaryLabel;
        Label stateLabel;

        public NodeInspectorView()
        {
            AddToClassList("bt-inspector");
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("bt-inspector__scroll");
            Add(scroll);
            ShowMessage("ノードを選択すると、ここに説明と設定が出ます。");
        }

        public void Show(IReadOnlyList<BehaviorTreeNodeView> selected)
        {
            Detach();
            scroll.Clear();
            if (selected.Count != 1)
            {
                ShowMessage(selected.Count == 0 ? "ノードを選択すると、ここに説明と設定が出ます。" : $"{selected.Count} 個のノードを選択中");
                return;
            }

            target = selected[0];
            var type = target.TreeNode.GetType();
            var header = new VisualElement();
            header.AddToClassList("bt-inspector__header");
            header.AddToClassList("bt-node--" + NodeCatalog.CategoryKey(target.TreeNode));
            var marker = new VisualElement();
            marker.AddToClassList("bt-marker");
            header.Add(marker);
            var titles = new VisualElement { style = { flexGrow = 1 } };
            titles.Add(Styled(BehaviorTreeNodeView.DisplayName(type), "bt-inspector__title"));
            titles.Add(Styled(type.GetCustomAttribute<NodeInfoAttribute>()?.Category ?? NodeCatalog.DefaultCategory(type), "bt-inspector__category"));
            header.Add(titles);
            scroll.Add(header);

            var description = NodeCatalog.Description(type);
            if (!string.IsNullOrEmpty(description)) scroll.Add(Styled(description, "bt-inspector__description"));

            stateLabel = Styled("", "bt-inspector__state");
            scroll.Add(stateLabel);
            RefreshRuntime();

            summaryLabel = Styled("", "bt-inspector__summary");
            scroll.Add(summaryLabel);
            RefreshSummary();

            var body = new VisualElement();
            body.AddToClassList("bt-inspector__fields");
            var commentProp = BehaviorTreeNodeView.BuildFields(target.ResolveProperty(), body, OnEdited);
            if (commentProp != null) body.Insert(0, BehaviorTreeNodeView.BindField(commentProp, OnEdited));
            scroll.Add(body);

            valuesSubscription.Disposable = target.OnValuesChanged.Subscribe(_ => RefreshSummary());
        }

        public void RefreshRuntime()
        {
            if (stateLabel == null || target == null) return;
            var playing = EditorApplication.isPlaying;
            stateLabel.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
            if (!playing) return;
            var active = target.IsRuntimeActive;
            stateLabel.text = active ? "● 実行中" : "○ 停止中";
            stateLabel.EnableInClassList("bt-inspector__state--active", active);
        }

        void OnEdited() => target?.NotifyValuesChanged();

        void RefreshSummary()
        {
            if (summaryLabel == null || target == null) return;
            var summary = target.TreeNode.Summary;
            summaryLabel.text = summary ?? "";
            summaryLabel.style.display = string.IsNullOrEmpty(summary) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Detach()
        {
            valuesSubscription.Disposable = null;
            target = null;
            summaryLabel = null;
            stateLabel = null;
        }

        void ShowMessage(string text) => scroll.Add(Styled(text, "bt-inspector__empty"));

        static Label Styled(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}

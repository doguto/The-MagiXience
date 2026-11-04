using System.Collections.Generic;
using DG.Tweening;
using Project.Scripts.Extensions.Message;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // TODO: UnityEventを使う形に修正
    // 管理下のボタン(buttons)を buttonInterval 間隔で並べ、フォーカス位置に合わせてスクロールする。
    // フォーカスの移動先は管理下のボタンのうち、操作可能(interactable)なものに限る
    public class ScrollableButtonList : ButtonListBase
    {
        [SerializeField] float buttonInterval;
        bool isLoop;

        // 並べ直しの基準。Inspectorで設定した先頭ボタンの初期位置
        Vector2 layoutOrigin;
        // 各ボタンの初期位置。MovableButtonは初期位置からの相対量で移動するため、並べ直し時の補正に使う
        readonly Dictionary<MovableButton, Vector2> initialPositions = new();
        // 管理下の各ボタンを、並び順どおりの位置へ置くための初期位置からの補正量
        readonly List<Vector2> layoutOffsets = new();

        void Awake()
        {
            foreach (var button in buttons) RegisterInitialPosition(button);
            if (buttons.Count > 0) layoutOrigin = initialPositions[buttons[0]];
        }

        public override void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            base.Init(buttonListType, index, isActive);
            RebuildLayout();
            UpdateNavigation();
        }

        // 管理下のボタンを差し替え、渡された順に並べ直す。管理外になったボタンの表示切り替えは呼び出し側で行う
        public void SetButtons(IReadOnlyList<MovableButton> newButtons)
        {
            if (newButtons.Count == 0)
            {
                Debug.LogWarning("[ScrollableButtonList] SetButtons: empty list is not allowed", this);
                return;
            }

            foreach (var button in newButtons) RegisterInitialPosition(button);
            buttons = new List<MovableButton>(newButtons);
            SetButtonIndex(0);
            RebuildLayout();
            ScrollTo(ButtonIndex, immediate: true);
            UpdateNavigation();
        }

        public void SetLoop(bool loop)
        {
            isLoop = loop;
            UpdateNavigation();
        }

        void RegisterInitialPosition(MovableButton button)
        {
            if (initialPositions.ContainsKey(button)) return;
            initialPositions[button] = button.transform.localPosition;
        }

        void RebuildLayout()
        {
            // スクロール量は index * buttonInterval だけ正方向に加算されるため、i番目は基準から負方向に並べる
            var axis = buttonListType == ButtonListType.Vertical ? Vector2.up : Vector2.right;
            layoutOffsets.Clear();
            for (var i = 0; i < buttons.Count; i++)
            {
                var layoutPosition = layoutOrigin - axis * (buttonInterval * i);
                layoutOffsets.Add(layoutPosition - initialPositions[buttons[i]]);
            }
        }

        void ScrollTo(int index, bool immediate = false)
        {
            var scroll = index * buttonInterval;
            var scrollVector = buttonListType == ButtonListType.Vertical ? new Vector2(0, scroll) : new Vector2(scroll, 0);
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].Move(scrollVector + layoutOffsets[i]);
                if (immediate) buttons[i].transform.DOComplete();
            }
        }

        bool IsFocusable(int index)
        {
            var button = buttons[index];
            if (!button.gameObject.activeInHierarchy) return false;

            var selectable = button.GetComponent<Selectable>();
            return selectable == null || selectable.interactable;
        }

        // fromから step(+1/-1) 方向にある次のフォーカス可能なボタンを探す。無ければ-1
        int FindNextFocusableIndex(int from, int step)
        {
            var count = buttons.Count;
            for (var n = 1; n < count; n++)
            {
                var index = from + step * n;
                if (index < 0 || index >= count)
                {
                    if (!isLoop) return -1;
                    // 端から反対側の端へ回り込む
                    index = (index % count + count) % count;
                }

                if (IsFocusable(index)) return index;
            }
            return -1;
        }

        // スクロールとEventSystemの選択が一致するよう、フォーカス可能なボタン同士の上下(横なら左右)の遷移先を明示する
        void UpdateNavigation()
        {
            var isVertical = buttonListType == ButtonListType.Vertical;
            for (var i = 0; i < buttons.Count; i++)
            {
                var selectable = buttons[i].GetComponent<Selectable>();
                if (selectable == null) continue;

                var prevIndex = FindNextFocusableIndex(i, -1);
                var nextIndex = FindNextFocusableIndex(i, 1);
                var prev = prevIndex < 0 ? null : buttons[prevIndex].GetComponent<Selectable>();
                var next = nextIndex < 0 ? null : buttons[nextIndex].GetComponent<Selectable>();
                selectable.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = isVertical ? prev : null,
                    selectOnDown = isVertical ? next : null,
                    // Move()では右入力でインデックスが戻るため、それに合わせる
                    selectOnLeft = isVertical ? null : next,
                    selectOnRight = isVertical ? null : prev,
                };
            }
        }

        public override void Move(UINavigateMessage message)
        {
            if (!IsActive) return;

            // 非Openなカードのクリック等でEventSystemの選択が外れていると、
            // 標準ナビが起点を失いフォーカス(拡大演出)が更新されなくなる。
            // 選択が現在のButtonIndexのカードとズレていたら選び直してから移動を続行する。
            var currentButton = buttons[ButtonIndex].gameObject;
            if (EventSystem.current.currentSelectedGameObject != currentButton)
            {
                EventSystem.current.SetSelectedGameObject(currentButton);
            }

            var isVertical = buttonListType == ButtonListType.Vertical;
            bool isUp;
            if (isVertical)
            {
                if (message.value.y == 0) return;
                isUp = message.value.y > 0;
            }
            else
            {
                if (message.value.x == 0) return;
                isUp = message.value.x > 0;
            }

            var nextIndex = FindNextFocusableIndex(ButtonIndex, isUp ? -1 : 1);
            if (nextIndex < 0) return;

            ScrollTo(nextIndex);
            SetButtonIndex(nextIndex);
        }
    }
}

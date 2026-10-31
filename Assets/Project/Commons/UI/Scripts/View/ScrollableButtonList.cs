using Project.Scripts.Extensions.Message;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // TODO: UnityEventを使う形に修正
    public class ScrollableButtonList : ButtonListBase
    {
        [SerializeField] float buttonInterval;
        float targetPosition;
        int maxScrollIndex;
        bool isLoop;

        public override void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            base.Init(buttonListType, index, isActive);
            maxScrollIndex = buttons.Count - 1;
        }

        public void SetMaxScrollIndex(int index)
        {
            maxScrollIndex = index;
        }

        public void SetLoop(bool loop)
        {
            isLoop = loop;
            if (isLoop) SetLoopNavigation();
        }

        // スクロールだけでなくEventSystemの選択も端から反対側の端へ回り込むよう、
        // buttonsの並び順で上下(横なら左右)の遷移先を明示する
        void SetLoopNavigation()
        {
            var isVertical = buttonListType == ButtonListType.Vertical;
            var count = buttons.Count;
            for (var i = 0; i < count; i++)
            {
                var selectable = buttons[i].GetComponent<Selectable>();
                if (selectable == null) continue;

                var prev = buttons[(i - 1 + count) % count].GetComponent<Selectable>();
                var next = buttons[(i + 1) % count].GetComponent<Selectable>();
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

            var nextIndex = isUp ? ButtonIndex - 1 : ButtonIndex + 1;
            if (nextIndex < 0 || nextIndex > maxScrollIndex)
            {
                if (!isLoop) return;
                // 端から反対側の端へ回り込む
                nextIndex = nextIndex < 0 ? maxScrollIndex : 0;
            }

            targetPosition = nextIndex * buttonInterval;
            var moveVector = isVertical ? new Vector2(0, targetPosition) : new Vector2(targetPosition, 0);
            foreach (var button in buttons) button.Move(moveVector);

            SetButtonIndex(nextIndex);
        }
    }
}

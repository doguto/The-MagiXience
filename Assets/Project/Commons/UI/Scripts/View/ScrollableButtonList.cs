using Project.Scripts.Extensions.Message;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    // TODO: UnityEventを使う形に修正
    public class ScrollableButtonList : ButtonListBase
    {
        [SerializeField] float buttonInterval;
        float targetPosition;
        int maxScrollIndex;

        public override void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            base.Init(buttonListType, index, isActive);
            maxScrollIndex = buttons.Count - 1;
        }

        public void SetMaxScrollIndex(int index)
        {
            maxScrollIndex = index;
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

            if (ButtonIndex == maxScrollIndex && !isUp) return;
            if (ButtonIndex == 0 && isUp) return;
            
            targetPosition = isUp ? targetPosition - buttonInterval : targetPosition + buttonInterval;
            var moveVector = isVertical ? new Vector2(0, targetPosition) : new Vector2(targetPosition, 0);
            foreach (var button in buttons) button.Move(moveVector);

            SetButtonIndex(isUp ? ButtonIndex - 1 : ButtonIndex + 1);
        }
    }
}

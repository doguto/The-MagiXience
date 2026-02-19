using Project.Scripts.Extensions.Message;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    // TODO: UnityEventを使う形に修正
    public class ScrollableButtonList : ButtonListBase
    {
        [SerializeField] float buttonInterval;
        float targetPosition;

        public override void Move(UINavigateMessage message)
        {
            if (!IsActive) return;

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

            if (ButtonIndex == buttons.Count - 1 && !isUp) return;
            targetPosition = isUp ? targetPosition - buttonInterval : targetPosition + buttonInterval;
            var moveVector = isVertical ? new Vector2(0, targetPosition) : new Vector2(targetPosition, 0);
            foreach (var button in buttons) button.Move(moveVector);

            SetButtonIndex(isUp ? ButtonIndex - 1 : ButtonIndex + 1);
        }
    }
}

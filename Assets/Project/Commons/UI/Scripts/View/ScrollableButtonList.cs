using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    // TODO: UnityEventを使う形に修正
    public class ScrollableButtonList : ButtonListBase
    {
        [SerializeField] float buttonInterval;
        float targetPosition;

        protected override bool MoveNextFlag => buttonListType switch
        {
            ButtonListType.Vertical => Input.GetKeyDown(KeyCode.UpArrow),
            ButtonListType.Horizontal => Input.GetKeyDown(KeyCode.RightArrow),
            _ => false
        };

        protected override bool MoveBackFlag => buttonListType switch
        {
            ButtonListType.Vertical => Input.GetKeyDown(KeyCode.DownArrow),
            ButtonListType.Horizontal => Input.GetKeyDown(KeyCode.LeftArrow),
            _ => false
        };

        public override void MoveNext(bool isUp = true)
        {
            // 両端の場合はもう一端に行かずに移動不可にする
            if (ButtonIndex == buttons.Count - 1 && !isUp) return;
            if (ButtonIndex == 0 && isUp) return;
            
            targetPosition = isUp? targetPosition - buttonInterval : targetPosition + buttonInterval;
            foreach (var button in buttons)
            {
                button.Move(new(0, targetPosition));
            }

            SetButtonIndex( isUp ? ButtonIndex - 1 : ButtonIndex + 1 );
        }
    }
}

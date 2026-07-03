using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    /// <summary>
    /// 1面道中開始時に操作方法を提示するチュートリアルモーダルのView。
    /// ボタンは持たず、表示/非表示の切り替えのみを担う（閉じる入力はPresenterが購読する）。
    /// </summary>
    public class TutorialModalView : MonoBehaviour
    {
        [SerializeField] GameObject pressZToStartText;

        public void Show()
        {
            gameObject.SetActive(true);
            SetPushZToStartActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetPushZToStartActive(bool isActive)
        {
            if (pressZToStartText != null)
            {
                pressZToStartText.SetActive(isActive);
            }
        }
    }
}

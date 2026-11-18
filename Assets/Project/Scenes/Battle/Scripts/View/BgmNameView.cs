using System.Collections;
using TMPro;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View
{
    // BGMが切り替わった際に、画面右下へ曲名を一定時間表示する
    public class BgmNameView : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] RectTransform content;
        [SerializeField] TMP_Text nameText;

        [SerializeField] string prefix = "♪ ";
        [SerializeField] float slideDistance = 40f;
        [SerializeField] float fadeInDuration = 0.5f;
        [SerializeField] float displayDuration = 4f;
        [SerializeField] float fadeOutDuration = 1f;

        Vector2 originalPosition;
        Coroutine showCoroutine;

        void Awake()
        {
            originalPosition = content.anchoredPosition;
            canvasGroup.alpha = 0f;
        }

        public void Show(string bgmName)
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
            }

            nameText.text = prefix + bgmName;
            showCoroutine = StartCoroutine(ShowRoutine());
        }

        IEnumerator ShowRoutine()
        {
            // 右からスライドしながらフェードイン
            var startPosition = originalPosition + Vector2.right * slideDistance;
            var elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fadeInDuration);
                var eased = 1f - (1f - t) * (1f - t);
                canvasGroup.alpha = t;
                content.anchoredPosition = Vector2.Lerp(startPosition, originalPosition, eased);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            content.anchoredPosition = originalPosition;

            yield return new WaitForSeconds(displayDuration);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            showCoroutine = null;
        }
    }
}

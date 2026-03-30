using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 五線譜演出。手動配置されたト音記号SpriteRenderer + LineRenderer[]を参照し、
    /// 左から右にマスクを外すようなフェードイン/フェードアウトを行う。
    /// ActivationTrackでGameObjectを有効化/無効化して使う。
    /// </summary>
    public class StaffNotationPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] SpriteRenderer gClefRenderer;
        [SerializeField] LineRenderer[] lineRenderers;

        [Header("Reveal Animation")]
        [SerializeField] float revealDuration = 1.0f;
        [SerializeField] Ease revealEase = Ease.OutCubic;

        [Header("Fade Out")]
        [SerializeField] float fadeOutDuration = 0.5f;

        // 各LineRendererの元の右端X座標
        float[] originalEndX;
        float[] lineY;
        float lineLeftX;

        // ト音記号の初期状態
        float trebleClefOriginalAlpha;

        Tween currentTween;

        void Awake()
        {
            CacheOriginalPositions();
        }

        void OnEnable()
        {
            PlayReveal();
        }

        void OnDisable()
        {
            PlayFadeOut();
        }

        void CacheOriginalPositions()
        {
            if (lineRenderers == null || lineRenderers.Length == 0) return;

            originalEndX = new float[lineRenderers.Length];
            lineY = new float[lineRenderers.Length];

            // 左端Xは全LineRenderer共通と仮定（最初の線の始点から取得）
            lineLeftX = lineRenderers[0].GetPosition(0).x;

            for (var i = 0; i < lineRenderers.Length; i++)
            {
                var endPos = lineRenderers[i].GetPosition(1);
                originalEndX[i] = endPos.x;
                lineY[i] = lineRenderers[i].GetPosition(0).y;
            }

            if (gClefRenderer != null)
            {
                trebleClefOriginalAlpha = gClefRenderer.color.a;
            }
        }

        void SetRevealProgress(float t)
        {
            // 各線の右端Xを lineLeftX → originalEndX[i] で補間
            for (var i = 0; i < lineRenderers.Length; i++)
            {
                var currentEndX = Mathf.Lerp(lineLeftX, originalEndX[i], t);
                lineRenderers[i].SetPosition(1, new Vector3(currentEndX, lineY[i], 0f));
            }
        }

        void SetFadeAlpha(float alpha)
        {
            if (gClefRenderer != null)
            {
                var c = gClefRenderer.color;
                gClefRenderer.color = new Color(c.r, c.g, c.b, trebleClefOriginalAlpha * alpha);
            }

            for (var i = 0; i < lineRenderers.Length; i++)
            {
                SetLineAlpha(lineRenderers[i], alpha);
            }
        }

        void PlayReveal()
        {
            currentTween?.Kill();

            // 初期状態: 全て非表示
            SetRevealProgress(0f);

            currentTween = DOVirtual.Float(0f, 1f, revealDuration, SetRevealProgress)
                .SetEase(revealEase);
        }

        void PlayFadeOut()
        {
            currentTween?.Kill();

            currentTween = DOVirtual.Float(1f, 0f, fadeOutDuration, SetFadeAlpha);
        }

        static void SetLineAlpha(LineRenderer lr, float alpha)
        {
            var startColor = lr.startColor;
            var endColor = lr.endColor;
            startColor.a = alpha;
            endColor.a = alpha;
            lr.startColor = startColor;
            lr.endColor = endColor;
        }

        void OnDestroy()
        {
            currentTween?.Kill();
        }
    }
}

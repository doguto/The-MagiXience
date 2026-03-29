using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>
    /// BGMのオーディオスペクトラムを32本のバーとして画面下部に表示する。
    /// ActivationTrackで最終フェーズ中だけGameObjectを有効化して使う。
    /// </summary>
    public class AudioSpectrumPresenter : MonoBehaviour
    {
        [Header("Spectrum Settings")] [SerializeField]
        int barCount = 32;

        [SerializeField] float maxBarHeight = 3f;
        [SerializeField] float lerpSpeed = 15f;
        [SerializeField] float sensitivity = 50f;
        [SerializeField] int spectrumRangeStart = 8;
        [SerializeField] int spectrumRangeEnd = 21;

        [Header("Visual")] [SerializeField] Color barColor = new(0.2f, 0.8f, 1f, 0.8f);

        [Header("Collision")] [SerializeField] int contactDamage = 10;
        [SerializeField] float pushForce = 8f;
        [SerializeField] int spectrumBarLayer = 11;

        const int FftSize = 1024;

        AudioSource audioSource;
        SpectrumBarEntityModel sharedModel;
        SpectrumBarView[] barViews;
        float[] spectrumData;
        float[] smoothedHeights;
        Camera mainCamera;

        void Awake()
        {
            mainCamera = Camera.main;
            sharedModel = new SpectrumBarEntityModel(contactDamage);
            spectrumData = new float[FftSize];
            smoothedHeights = new float[barCount];

            CreateBars();
        }

        void Start()
        {
            audioSource = GameObject.Find("BgmAudioSource")?.GetComponent<AudioSource>();
        }

    void CreateBars()
        {
            barViews = new SpectrumBarView[barCount];

            var screenLeft = mainCamera.ViewportToWorldPoint(Vector3.zero).x;
            var screenRight = mainCamera.ViewportToWorldPoint(Vector3.one).x;
            var totalWidth = screenRight - screenLeft;
            var barWidth = totalWidth / barCount;

            for (var i = 0; i < barCount; i++)
            {
                var barObj = new GameObject($"SpectrumBar_{i}");
                barObj.transform.SetParent(transform);
                barObj.layer = spectrumBarLayer;

                // X位置を均等配置
                var xPos = screenLeft + barWidth * (i + 0.5f);
                barObj.transform.position = new Vector3(xPos, 0f, 0f);

                // View
                var view = barObj.AddComponent<SpectrumBarView>();
                view.Initialize(barWidth, barColor);
                barViews[i] = view;

                // CollisionRelay
                var relay = barObj.AddComponent<SpectrumBarCollisionRelay>();
                relay.Initialize(sharedModel, pushForce);
            }
        }

        void Update()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            
            var screenBottomY = mainCamera.ViewportToWorldPoint(Vector3.zero).y;

            // 元の32分割対数マッピングからspectrumRangeStart~spectrumRangeEndの帯域を切り出し
            var rangeBinStart = LogarithmicBinIndex(spectrumRangeStart, barCount, FftSize);
            var rangeBinEnd = LogarithmicBinIndex(spectrumRangeEnd + 1, barCount, FftSize);
            var rangeBinCount = rangeBinEnd - rangeBinStart;

            for (var i = 0; i < barCount; i++)
            {
                // 切り出した帯域を32本に均等割り振り（反転: 左が高周波、右が低周波）
                var reversedIndex = barCount - 1 - i;
                var binStart = rangeBinStart + rangeBinCount * reversedIndex / barCount;
                var binEnd = rangeBinStart + rangeBinCount * (reversedIndex + 1) / barCount;
                if (binEnd <= binStart) binEnd = binStart + 1;

                // bin範囲の平均値を取得
                var sum = 0f;
                for (var b = binStart; b < binEnd && b < FftSize; b++)
                {
                    sum += spectrumData[b];
                }
                var avg = sum / (binEnd - binStart);

                // 音量で正規化してから感度を適用してターゲット高さを算出
                var normalizedAvg = avg / Mathf.Max(audioSource.volume, 0.01f);
                var targetHeight = Mathf.Clamp(normalizedAvg * sensitivity, 0f, maxBarHeight);

                // Lerpで滑らかに
                smoothedHeights[i] = Mathf.Lerp(smoothedHeights[i], targetHeight, Time.deltaTime * lerpSpeed);

                barViews[i].UpdateBar(smoothedHeights[i], screenBottomY);
            }
        }

        static int LogarithmicBinIndex(int barIndex, int totalBars, int fftSize)
        {
            // 対数スケールで0〜fftSize/2をtotalBars分割にマッピング
            var halfFft = fftSize / 2;
            var logMin = Mathf.Log10(1f);
            var logMax = Mathf.Log10(halfFft);
            var logVal = logMin + (logMax - logMin) * barIndex / totalBars;
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(10f, logVal)), 0, halfFft - 1);
        }

        void OnDisable()
        {
            // フェーズ切替でバーを非表示にリセット
            if (smoothedHeights != null)
            {
                for (var i = 0; i < barCount; i++)
                {
                    smoothedHeights[i] = 0f;
                }
            }
        }

        void OnDestroy()
        {
            sharedModel?.Dispose();
        }
    }
}

using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public class AudioSpectrumPresenter : MonoBehaviour
    {
        [Header("Spectrum Settings")] [SerializeField]
        SpectrumBarHeightData heightData;

        [SerializeField] float maxBarHeight = 3f;
        [SerializeField] float lerpSpeed = 15f;

        [Header("Visual")] [SerializeField] Color barColor = new(0.2f, 0.8f, 1f, 0.8f);

        [Header("Collision")] [SerializeField] int contactDamage = 10;
        [SerializeField] float pushForce = 8f;
        [SerializeField] int spectrumBarLayer = 11;

        AudioSource audioSource;
        SpectrumBarEntityModel sharedModel;
        SpectrumBarView[] barViews;
        float[] smoothedHeights;
        int barCount;

        void Awake()
        {
            sharedModel = new SpectrumBarEntityModel(contactDamage);

            if (heightData == null)
            {
                Debug.LogError("[AudioSpectrumPresenter] heightData is not assigned.");
                return;
            }

            barCount = heightData.BarCount;
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

            var screenLeft = ScreenBoundsCache.MinX;
            var screenRight = ScreenBoundsCache.MaxX;
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
            if (heightData == null || audioSource == null || !audioSource.isPlaying) return;

            var hasFrame = heightData.TryGetFrameOffset(audioSource.timeSamples, out var frameOffset);
            var screenBottomY = ScreenBoundsCache.MinY;

            for (var i = 0; i < barCount; i++)
            {
                var targetHeight = hasFrame ? heightData.GetHeightRate(frameOffset, i) * maxBarHeight : 0f;

                // Lerpで滑らかに
                smoothedHeights[i] = Mathf.Lerp(smoothedHeights[i], targetHeight, Time.deltaTime * lerpSpeed);

                barViews[i].UpdateBar(smoothedHeights[i], screenBottomY);
            }
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

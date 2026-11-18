using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class SoundManagerPresenter : MonoPresenter
    {
        [SerializeField] AudioSource bgmAudioSource;
        [SerializeField] AudioSource seAudioSource;
        [SerializeField] AudioSource loopSeAudioSource;

        public AudioSource BgmAudioSource => bgmAudioSource;

        SoundModelRepository soundModelRepository;
        BgmModel currentBgmModel;

        // 再生開始からループ地点に戻った回数。サウンドルームの自動送りで使う
        public int BgmLoopCount { get; private set; }

        float bgmBaseVolume;
        CancellationTokenSource bgmFadeCts;

        int bgmLoopStartSamples;
        int bgmLoopEndSamples;
        int loopSeLoopStartSamples;
        int loopSeLoopEndSamples;

        void Awake()
        {
            soundModelRepository = SoundModelRepository.Instance;

            var userModel = UserModelRepository.Instance.Get();
            SetBGMVolume(userModel.BgmVolume);
            SetSEVolume(userModel.SeVolume);
        }

        void Update()
        {
            if (bgmAudioSource.isPlaying && bgmLoopEndSamples > 0
                && bgmAudioSource.timeSamples >= bgmLoopEndSamples)
            {
                bgmAudioSource.timeSamples = bgmLoopStartSamples;
                BgmLoopCount++;
            }

            if (loopSeAudioSource.isPlaying && loopSeLoopEndSamples > 0
                && loopSeAudioSource.timeSamples >= loopSeLoopEndSamples)
            {
                loopSeAudioSource.timeSamples = loopSeLoopStartSamples;
            }
        }

        public async UniTask PlayBGMAsync(SceneType sceneType, BgmType bgmType = BgmType.Default, bool skipIfSamePlaying = false)
        {
            var bgmModel = soundModelRepository.GetBgmModel(sceneType, bgmType);
            var bgmClip = bgmModel.AudioClip;

            bgmLoopStartSamples = bgmModel.LoopStartSamples;
            bgmLoopEndSamples = bgmModel.LoopEndSamples > 0
                ? bgmModel.LoopEndSamples
                : bgmClip.samples;

            var shouldSkip = skipIfSamePlaying && bgmAudioSource.isPlaying && bgmAudioSource.clip == bgmClip;
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = false;
            if (!shouldSkip)
            {
                CancelBGMFade();
                BgmLoopCount = 0;
                bgmAudioSource.Play();
            }

            // 曲が切り替わったら前の曲を解放する
            if (currentBgmModel != null && currentBgmModel != bgmModel)
            {
                soundModelRepository.RefreshBgmModel(currentBgmModel);
            }
            currentBgmModel = bgmModel;

            await UniTask.CompletedTask;
        }

        public void StopBGM()
        {
            CancelBGMFade();
            bgmAudioSource.Stop();
            bgmLoopEndSamples = 0;
        }

        // BGMを徐々に小さくして停止する。途中で別のBGMが再生された場合はキャンセルされる
        public async UniTask FadeOutBGMAsync(float duration, CancellationToken cancellationToken = default)
        {
            CancelBGMFade();
            var fadeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            bgmFadeCts = fadeCts;

            try
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    bgmAudioSource.volume = bgmBaseVolume * (1f - elapsed / duration);
                    await UniTask.Yield(PlayerLoopTiming.Update, fadeCts.Token);
                    elapsed += Time.unscaledDeltaTime;
                }

                bgmAudioSource.Stop();
                bgmLoopEndSamples = 0;
            }
            finally
            {
                // 自分が最新のフェードのときだけ音量を戻す（新しいフェードの音量を上書きしないため）
                if (bgmFadeCts == fadeCts)
                {
                    bgmFadeCts = null;
                    bgmAudioSource.volume = bgmBaseVolume;
                }
                fadeCts.Dispose();
            }
        }

        void CancelBGMFade()
        {
            if (bgmFadeCts == null) return;

            bgmFadeCts.Cancel();
            bgmFadeCts = null;
            bgmAudioSource.volume = bgmBaseVolume;
        }

        public void PlaySE(SeType seType)
        {
            var seModel = soundModelRepository.GetSeModel(seType);
            var seClip = seModel.AudioClip;

            seAudioSource.PlayOneShot(seClip);
        }
        
        public void PlayLoopSE(SeType seType)
        {
            if (loopSeAudioSource == null)
            {
                Debug.LogWarning("[SoundManagerPresenter] loopSeAudioSource is not assigned.");
                return;
            }

            var seModel = soundModelRepository.GetSeModel(seType);
            var seClip = seModel.AudioClip;

            loopSeLoopStartSamples = seModel.LoopStartSamples;
            loopSeLoopEndSamples = seModel.LoopEndSamples > 0
                ? seModel.LoopEndSamples
                : seClip.samples;

            loopSeAudioSource.clip = seClip;
            loopSeAudioSource.loop = false; // Update内で手動ループ
            loopSeAudioSource.timeSamples = 0;
            loopSeAudioSource.Play();
        }

        public void StopLoopSE()
        {
            if (loopSeAudioSource == null) return;
            loopSeAudioSource.Stop();
            loopSeLoopEndSamples = 0;
        }

        public async UniTask PlaySEAsync(SeType seType)
        {
            var seModel = soundModelRepository.GetSeModel(seType);
            var seClip = seModel.AudioClip;
            seAudioSource.PlayOneShot(seClip);
            await UniTask.CompletedTask;
        }

        const float MaxAudioVolume = 0.5f;
        const float MinVolumeDb = -40f;

        public void SetBGMVolume(int volume)
        {
            bgmBaseVolume = ConvertToAudioVolume(volume);
            // フェード中はフェード側が音量を制御するので、基準値だけ更新する
            if (bgmFadeCts == null) bgmAudioSource.volume = bgmBaseVolume;
        }

        public void SetSEVolume(int volume)
        {
            float v = ConvertToAudioVolume(volume);
            seAudioSource.volume = v;
            if (loopSeAudioSource != null) loopSeAudioSource.volume = v;
        }

        // 設定値(0-100)をAudioSourceの音量(0-MaxAudioVolume)へ変換する。
        // 人間の聴覚に合わせて、線形ではなく対数(dB)カーブで補間する。
        static float ConvertToAudioVolume(int volume)
        {
            float t = Mathf.Clamp01(volume / 100.0f);
            if (t <= 0f) return 0f; // 無音

            // tを MinVolumeDb 〜 0dB の範囲へ線形にマッピングし、dB→振幅へ変換する。
            float db = Mathf.Lerp(MinVolumeDb, 0f, t);
            float amplitude = Mathf.Pow(10f, db / 20f);
            return amplitude * MaxAudioVolume;
        }
    }
}

using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions;
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
                bgmAudioSource.Play();
            }

            await UniTask.CompletedTask;
        }

        public void StopBGM()
        {
            bgmAudioSource.Stop();
            bgmLoopEndSamples = 0;
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
            bgmAudioSource.volume = ConvertToAudioVolume(volume);
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

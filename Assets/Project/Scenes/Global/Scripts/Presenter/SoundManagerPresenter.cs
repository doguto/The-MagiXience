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

            // TODO: UserDataから音量設定を取得して設定する
            SetBGMVolume(15);
            SetSEVolume(15);
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

        public void SetBGMVolume(int volume)
        {
            bgmAudioSource.volume = Mathf.Clamp01(volume / 100.0f);
        }

        public void SetSEVolume(int volume)
        {
            float v = Mathf.Clamp01(volume / 100.0f);
            seAudioSource.volume = v;
            if (loopSeAudioSource != null) loopSeAudioSource.volume = v;
        }
    }
}

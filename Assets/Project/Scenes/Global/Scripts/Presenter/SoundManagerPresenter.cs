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

        SoundModelRepository soundModelRepository;

        void Awake()
        {
            soundModelRepository = SoundModelRepository.Instance;

            // TODO: UserDataから音量設定を取得して設定する
            SetBGMVolume(15);
            SetSEVolume(15);
        }

        public async UniTask PlayBGMAsync(SceneType sceneType, BgmType bgmType = BgmType.Default)
        {
            var bgmModel = soundModelRepository.GetBgmModel(sceneType, bgmType);
            var bgmClip = bgmModel.AudioClip;

            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();

            await UniTask.CompletedTask;
        }

        public void StopBGM()
        {
            bgmAudioSource.Stop();
        }

        public void PlaySE(SeType seType)
        {
            var seModel = soundModelRepository.GetSeModel(seType);
            var seClip = seModel.AudioClip;

            seAudioSource.PlayOneShot(seClip);
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
            seAudioSource.volume = Mathf.Clamp01(volume / 100.0f);
        }
    }
}

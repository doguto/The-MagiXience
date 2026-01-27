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
            soundModelRepository = new SoundModelRepository();
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

        public void SetBGMVolume(float volume)
        {
            bgmAudioSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSEVolume(float volume)
        {
            seAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
}

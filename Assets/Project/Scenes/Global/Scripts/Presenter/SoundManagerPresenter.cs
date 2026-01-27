using Cysharp.Threading.Tasks;
using Project.Scenes.Global.Scripts.Model;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class SoundManagerPresenter : MonoPresenter
    {
        [SerializeField] AudioSource bgmAudioSource;
        [SerializeField] AudioSource seAudioSource;

        SoundModel soundModel;

        void Awake()
        {
            soundModel = new SoundModel();
        }

        public async UniTask PlayBGM(SceneType sceneType, BgmType bgmType = BgmType.Default)
        {
            var bgmClip = soundModel.GetBGM(sceneType, bgmType);

            if (bgmClip == null)
            {
                Debug.LogWarning($"BGM not found: {sceneType}_{bgmType}");
                return;
            }

            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();

            await UniTask.CompletedTask;
        }

        public void StopBGM()
        {
            bgmAudioSource.Stop();
        }

        public void PlaySE(string seName)
        {
            var seClip = soundModel.GetSE(seName);

            if (seClip == null)
            {
                Debug.LogWarning($"SE not found: {seName}");
                return;
            }

            seAudioSource.PlayOneShot(seClip);
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

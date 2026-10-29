using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scenes.SoundRoom.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
using UniRx;
using UnityEngine;

namespace Project.Scenes.SoundRoom.Scripts.Presenter
{
    public class SoundRoomScenePresenter : MonoPresenter
    {
        const string UndiscoveredTitle = "？？？";
        const string UndiscoveredDescription = "まだ発見していない曲です。";

        [SerializeField] SoundRoomView soundRoomView;

        IReadOnlyList<BgmModel> bgmModels;
        UserModel userModel;

        void Awake()
        {
            bgmModels = SoundModelRepository.Instance.CreateAllBgmModels();
            userModel = UserModelRepository.Instance.Get();
        }

        protected override void Start()
        {
            base.Start();

            if (bgmModels.Count > soundRoomView.TrackCount)
            {
                Debug.LogWarning($"[SoundRoom] 曲数({bgmModels.Count})に対してカードが足りません({soundRoomView.TrackCount})", this);
            }

            for (var i = 0; i < bgmModels.Count; i++)
            {
                soundRoomView.SetTrack(i, $"No.{i + 1:00}", GetTitle(i));
            }
            soundRoomView.Init(bgmModels.Count);
            ShowDescription(0);

            soundRoomView.OnTrackFocused.Subscribe(ShowDescription).AddTo(this);
            soundRoomView.OnTrackPressed.Subscribe(PlayTrack).AddTo(this);
            MessageBroker.Default.Receive<UICancelMessage>()
                .SubscribeBlocking(_ => BackToTitle())
                .AddTo(this);
        }

        bool IsUnlocked(int index)
        {
            return userModel.ClearedStageNumber >= bgmModels[index].SoundRoomUnlockClearedStage;
        }

        string GetTitle(int index)
        {
            if (!IsUnlocked(index)) return UndiscoveredTitle;

            return bgmModels[index].DisplayName;
        }

        void ShowDescription(int index)
        {
            if (index < 0 || index >= bgmModels.Count) return;

            var description = IsUnlocked(index) ? bgmModels[index].Description : UndiscoveredDescription;
            soundRoomView.SetDescription(GetTitle(index), description);
        }

        // 選択した曲をループ再生する。未発見の曲は再生しない
        void PlayTrack(int index)
        {
            if (index < 0 || index >= bgmModels.Count) return;
            if (!IsUnlocked(index))
            {
                soundManager.PlaySE(SeType.Cancel);
                return;
            }

            var model = bgmModels[index];
            soundManager.PlayBGMAsync(model.SceneType, model.BgmType).Forget();
        }

        async UniTask BackToTitle()
        {
            soundManager.PlaySEAsync(SeType.Cancel).Forget();
            await globalScenePresenter.SceneNavigator.NavigateTo(SceneRouterModel.Title, SceneRouterModel.SoundRoom);
        }
    }
}

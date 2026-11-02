using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        [Header("Auto Play")]
        [Tooltip("1曲をループ何回分流してからフェードアウトするか")]
        [SerializeField] int autoPlayLoopCount = 1;
        [Tooltip("次の曲へ移る前のフェードアウト時間(秒)")]
        [SerializeField] float autoPlayFadeDuration = 5f;

        // サウンドルームに表示する曲（未解放のシークレット曲は除外済み）
        IReadOnlyList<BgmModel> bgmModels;
        UserModel userModel;

        CancellationTokenSource playbackCts;

        void Awake()
        {
            userModel = UserModelRepository.Instance.Get();
            bgmModels = SoundModelRepository.Instance.CreateAllBgmModels()
                .Where(x => !x.IsSoundRoomSecret || IsUnlocked(x))
                .ToList();
        }

        protected override void Start()
        {
            base.Start();

            if (bgmModels.Count > soundRoomView.TrackCount)
            {
                Debug.LogWarning($"[SoundRoom] 曲数({bgmModels.Count})に対してカードが足りません({soundRoomView.TrackCount})", this);
            }

            // シークレット曲は通し番号を振らず、後続の番号も詰める
            var trackNumber = 0;
            for (var i = 0; i < bgmModels.Count; i++)
            {
                var indexText = bgmModels[i].IsSoundRoomSecret ? string.Empty : $"No.{++trackNumber:00}";
                soundRoomView.SetTrack(i, indexText, GetTitle(i), bgmModels[i].SoundRoomBackgroundStageNumber);
            }
            soundRoomView.Init(bgmModels.Count);
            ShowTrackInfo(0);

            soundRoomView.OnTrackFocused.Subscribe(ShowTrackInfo).AddTo(this);
            soundRoomView.OnTrackPressed.Subscribe(PlayTrack).AddTo(this);
            MessageBroker.Default.Receive<UICancelMessage>()
                .SubscribeBlocking(_ => BackToTitle())
                .AddTo(this);
        }

        void OnDestroy()
        {
            CancelPlayback();
        }

        bool IsUnlocked(BgmModel model)
        {
            return userModel.ClearedStageNumber >= model.SoundRoomUnlockClearedStage;
        }

        bool IsUnlocked(int index) => IsUnlocked(bgmModels[index]);

        string GetTitle(int index)
        {
            if (!IsUnlocked(index)) return UndiscoveredTitle;

            return bgmModels[index].DisplayName;
        }

        void ShowTrackInfo(int index)
        {
            if (index < 0 || index >= bgmModels.Count) return;

            var description = IsUnlocked(index) ? bgmModels[index].Description : UndiscoveredDescription;
            soundRoomView.SetDescription(GetTitle(index), description);
        }

        // 選択した曲を再生し、指定ループ数流したらフェードアウトして次の曲へ進む。未発見の曲は再生しない
        void PlayTrack(int index)
        {
            if (index < 0 || index >= bgmModels.Count) return;
            if (!IsUnlocked(index))
            {
                soundManager.PlaySE(SeType.Cancel);
                return;
            }

            CancelPlayback();
            playbackCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var model = bgmModels[index];
            soundManager.PlayBGMAsync(model.SceneType, model.BgmType).Forget();
            AutoAdvanceAsync(index, playbackCts.Token).Forget();
        }

        async UniTaskVoid AutoAdvanceAsync(int index, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.WaitUntil(() => soundManager.BgmLoopCount >= autoPlayLoopCount, cancellationToken: cancellationToken);
                await soundManager.FadeOutBGMAsync(autoPlayFadeDuration, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var nextIndex = FindNextPlayableIndex(index);
            if (nextIndex < 0) return;

            PlayTrack(nextIndex);
        }

        // indexの次にある解放済みの曲を探す。末尾まで行ったら先頭に戻る
        int FindNextPlayableIndex(int index)
        {
            for (var offset = 1; offset <= bgmModels.Count; offset++)
            {
                var candidate = (index + offset) % bgmModels.Count;
                if (IsUnlocked(candidate)) return candidate;
            }
            return -1;
        }

        void CancelPlayback()
        {
            if (playbackCts == null) return;

            playbackCts.Cancel();
            playbackCts.Dispose();
            playbackCts = null;
        }

        async UniTask BackToTitle()
        {
            CancelPlayback();
            soundManager.PlaySEAsync(SeType.Cancel).Forget();
            await globalScenePresenter.SceneNavigator.NavigateTo(SceneRouterModel.Title, SceneRouterModel.SoundRoom);
        }
    }
}

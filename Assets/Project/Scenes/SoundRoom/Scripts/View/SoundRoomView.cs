using System;
using System.Collections.Generic;
using Project.Commons.UI.Scripts.View;
using TMPro;
using UniRx;
using UnityEngine;

namespace Project.Scenes.SoundRoom.Scripts.View
{
    public class SoundRoomView : MonoBehaviour, IDisposable
    {
        // 曲カードはステージ選択と同じボタンを仮で流用する
        [SerializeField] ScrollableButtonList scrollableButtonList;
        [SerializeField] List<SimpleButton> trackButtons;
        [SerializeField] List<TMP_Text> trackIndexTexts;
        [SerializeField] List<TMP_Text> trackTitleTexts;

        [Header("Card Image")]
        [Tooltip("ステージ番号ごとのカード画像。Element0=黒カード, 1〜6=各ステージ, 7=EX")]
        [SerializeField] List<Sprite> cardSpritesByStage;

        [Header("Playing Pointer")]
        [Tooltip("再生中の曲のカードに表示するポインタ。カードの子に付け替えてスクロールに追従させる")]
        [SerializeField] GameObject playingPointer;
        [Tooltip("カード中心からのポインタの位置(カードのローカル座標)")]
        [SerializeField] Vector2 playingPointerOffset;

        [Header("Description")]
        [SerializeField] TMP_Text descriptionTitleText;
        [SerializeField] TMP_Text descriptionText;

        readonly Subject<int> onTrackFocused = new();
        readonly Subject<int> onTrackPressed = new();
        public IObservable<int> OnTrackFocused => onTrackFocused;
        public IObservable<int> OnTrackPressed => onTrackPressed;

        public int TrackCount => trackButtons.Count;

        public void Init(int trackCount)
        {
            scrollableButtonList.Init(ButtonListType.Vertical, isActive: true);
            // 何も再生していない間はポインタを出さない
            if (playingPointer != null) playingPointer.SetActive(false);

            // データより多いカードは非表示にし、表示するカードだけをスクロールの管理下に置く
            var visibleCount = Mathf.Min(trackCount, trackButtons.Count);
            var visibleButtons = new List<MovableButton>(visibleCount);
            for (var i = 0; i < trackButtons.Count; i++)
            {
                var index = i;
                trackButtons[i].gameObject.SetActive(i < visibleCount);
                if (i >= visibleCount) continue;

                // 未発見の曲もカーソルを合わせられるよう、ボタン自体は常に開放状態にする
                trackButtons[i].Init(isOpened: true, isFocused: i == 0);
                trackButtons[i].OnFocusedEvent.Subscribe(_ => onTrackFocused.OnNext(index)).AddTo(this);
                trackButtons[i].OnPressed.Subscribe(_ => onTrackPressed.OnNext(index)).AddTo(this);
                visibleButtons.Add(trackButtons[i].GetComponent<MovableButton>());
            }
            scrollableButtonList.SetButtons(visibleButtons);
            // ステージ選択と同様、端で上下を押したら反対側の端へ回り込む
            scrollableButtonList.SetLoop(true);
        }

        public void SetTrack(int index, string indexText, string title, int cardStageNumber)
        {
            if (index >= trackButtons.Count) return;

            trackIndexTexts[index].text = indexText;
            trackTitleTexts[index].text = title;
            SetCardSprite(index, cardStageNumber);
        }

        // カード本体のSpriteRendererの画像を、ステージ番号に対応する画像に差し替える
        void SetCardSprite(int index, int cardStageNumber)
        {
            if (cardStageNumber < 0 || cardStageNumber >= cardSpritesByStage.Count || cardSpritesByStage[cardStageNumber] == null)
            {
                Debug.LogWarning($"[SoundRoom] カード画像が未設定: stage={cardStageNumber}", this);
                return;
            }

            var cardRenderer = trackButtons[index].GetComponent<SpriteRenderer>();
            if (cardRenderer == null) return;

            cardRenderer.sprite = cardSpritesByStage[cardStageNumber];
        }

        // 再生中の曲のカードへポインタを移す
        public void SetPlayingTrack(int index)
        {
            if (playingPointer == null || index < 0 || index >= trackButtons.Count) return;

            var pointerTransform = playingPointer.transform;
            pointerTransform.SetParent(trackButtons[index].transform, false);
            pointerTransform.localPosition = new Vector3(playingPointerOffset.x, playingPointerOffset.y, pointerTransform.localPosition.z);
            playingPointer.SetActive(true);
        }

        public void SetDescription(string title, string description)
        {
            descriptionTitleText.text = title;
            descriptionText.text = description;
        }

        public void Dispose()
        {
            onTrackFocused.Dispose();
            onTrackPressed.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using DG.Tweening;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;


namespace Project.Scenes.StageList.Scripts.View
{
    public class StageCardListView : MonoBehaviour, IDisposable
    {
        [SerializeField] ScrollableButtonList scrollableButtonList;
        [SerializeField] List<SimpleButton> simpleButtons;
        [SerializeField] SpriteRenderer charaImage;

        public List<StageCardView> stageCardViews;

        readonly Subject<int> onButtonChanged = new();
        readonly Subject<int> onButtonPressed = new();
        public IObservable<int> OnButtonChanged => onButtonChanged;
        public IObservable<int> OnButtonPressed => onButtonPressed;

        public void Init(IReadOnlyList<bool> isOpenedList)
        {
            scrollableButtonList.Init(ButtonListType.Vertical, isActive: true);

            // 未解放のカードにはフォーカスを移せないよう、解放済みの最後のカードまでに制限する
            var maxScrollIndex = 0;
            for (var i = 0; i < simpleButtons.Count; i++)
            {
                var isOpened = i < isOpenedList.Count && isOpenedList[i];
                if (isOpened) maxScrollIndex = i;
                simpleButtons[i].Init(isOpened, i == 0 && isOpened);
                simpleButtons[i].OnFocusedEvent.Subscribe(PublishOnButtonChanged).AddTo(this);
                simpleButtons[i].OnPressed.Subscribe(PublishOnButtonPressed).AddTo(this);
            }
            scrollableButtonList.SetMaxScrollIndex(maxScrollIndex);
            // 最後のカード(EX)に挑戦可能になったら、端で上下を押したとき反対側の端へ回り込む
            scrollableButtonList.SetLoop(maxScrollIndex == simpleButtons.Count - 1);
        }

        public void SetCharaImage(Sprite charaSprite, bool isCleared)
        {
            // TODO: マジックナンバー修正
            charaImage.DOFade(0f, 0f);
            charaImage.sprite = charaSprite;
            charaImage.color = isCleared ? Color.white : Color.black;
            charaImage.DOFade(1f, 0.25f).SetDelay(0.1f);
        }

        void PublishOnButtonChanged(Unit _)
        {
            var index = simpleButtons.FindIndex(b => b.IsFocused);
            onButtonChanged.OnNext(index);
        }

        void PublishOnButtonPressed(Unit _)
        {
            var index = simpleButtons.FindIndex(b => b.IsFocused);
            onButtonPressed.OnNext(index);
        }

        public void Dispose()
        {
            onButtonChanged?.Dispose();
            onButtonPressed?.Dispose();
        }
    }
}

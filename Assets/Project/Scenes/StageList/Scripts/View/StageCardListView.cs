using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;


namespace Project.Scenes.StageList.Scripts.View
{
    public class StageCardListView : MonoBehaviour
    {
        [SerializeField] ScrollableButtonList scrollableButtonList;
        [SerializeField] List<SimpleButton> simpleButtons;
        [SerializeField] SpriteRenderer charaImage;
        
        public List<StageCardView> stageCardViews;
        
        readonly Subject<int> onButtonChanged = new();
        public IObservable<int> OnButtonChanged => onButtonChanged;

        public void Init()
        {
            scrollableButtonList.Init(ButtonListType.Vertical, isActive: true);

            for (int i = 0; i < simpleButtons.Count; i++)
            {
                simpleButtons[i].OnFocusedEvent.Subscribe(PublishOnButtonChanged).AddTo(this);
            }
        }

        public void SetCharaImage(Sprite charaSprite)
        {
            // TODO: マジックナンバー修正
            charaImage.DOFade(0f, 0f);
            charaImage.sprite = charaSprite;
            charaImage.DOFade(1f, 0.25f).SetDelay(0.1f);
        }

        void PublishOnButtonChanged(Unit _)
        {
            var index = simpleButtons.FindIndex(b => b.IsFocused);
            onButtonChanged.OnNext(index);
        }
    }
}

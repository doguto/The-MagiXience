using System;
using System.Collections.Generic;
using DG.Tweening;
using Project.Commons.UI.Scripts.View;
using UnityEngine;


namespace Project.Scenes.StageList.Scripts.View
{
    public class StageCardListView : MonoBehaviour
    {
        [SerializeField] ScrollableButtonList scrollableButtonList;
        [SerializeField] SpriteRenderer charaImage;
        
        public List<StageCardView> stageCardViews;
        public IObservable<int> OnButtonChanged => scrollableButtonList.OnButtonChanged;

        public void Init()
        {
            scrollableButtonList.Init(ButtonListType.Vertical, isActive: true);
            
        }

        public void SetCharaImage(Sprite charaSprite)
        {
            // TODO: マジックナンバー修正
            charaImage.DOFade(0f, 0f);
            charaImage.sprite = charaSprite;
            charaImage.DOFade(1f, 0.25f).SetDelay(0.1f);
        }
    }
}

using System;
using System.Collections.Generic;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Title.Scripts.View
{
    public class TitleMenuView : MonoBehaviour
    {
        [SerializeField] SimpleButton startButton;
        [SerializeField] SimpleButton exitButton;
        [SerializeField] List<Sprite> backgroundSprites;
        [SerializeField] SpriteRenderer memberStillRenderer;

        public IObservable<Unit> OnPressedStart => startButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        public void Init(Sprite memberStillSprite)
        {
            memberStillRenderer.sprite = memberStillSprite;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void SetBackGround(int clearedStageAmount)
        {
            memberStillRenderer.sprite = backgroundSprites[clearedStageAmount];
        }
    }
}
using System;
using System.Collections.Generic;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Scenes.Title.Scripts.View
{
    public class TitleMenuView : MonoBehaviour
    {
        [SerializeField] CanvasGroup buttonList;
        [SerializeField] SimpleButton startMainButton;
        [SerializeField] SimpleButton stageSelectButton;
        [SerializeField] SimpleButton configButton;
        [SerializeField] SimpleButton exitButton;
        [SerializeField] List<Sprite> backgroundSprites;
        [SerializeField] SpriteRenderer memberStillRenderer;
        [SerializeField] SpriteRenderer backGroundRenderer;

        // public IObservable<Unit> OnPressedStartMain => startMainButton.OnPressed;
        public IObservable<Unit> OnPressedStart => stageSelectButton.OnPressed;
        public IObservable<Unit> OnPressedOption => configButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        public void Init((Sprite memberStill, Sprite backGround) sprites)
        {
            memberStillRenderer.sprite = sprites.memberStill;
            backGroundRenderer.sprite = sprites.backGround;
        }

        public void InitStart()
        {
            // startMainButton.Init(isFocused: true);
            stageSelectButton.Init(isFocused: true);
            configButton.Init();
            exitButton.Init();
        }

        public void SetInteractable(bool interactable)
        {
            buttonList.interactable = interactable;
            buttonList.blocksRaycasts = interactable;
        }
    }
}

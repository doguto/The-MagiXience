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
        [SerializeField] SimpleButton startButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;
        [SerializeField] List<Sprite> backgroundSprites;
        [SerializeField] SpriteRenderer memberStillRenderer;
        [SerializeField] SpriteRenderer backGroundRenderer;

        public IObservable<Unit> OnPressedStart => startButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        public void Init((Sprite memberStill, Sprite backGround)sprites)
        {
            memberStillRenderer.sprite = sprites.memberStill;
            backGroundRenderer.sprite = sprites.backGround;
        }

        public void InitStart()
        {
            startButton.Init(isFocused: true);
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void SetInteractable(bool interactable)
        {
            buttonList.interactable = interactable;
            buttonList.blocksRaycasts = interactable;
        }
    }
}
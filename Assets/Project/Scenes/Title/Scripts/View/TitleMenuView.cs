using System;
using System.Collections.Generic;
using Project.Commons.Button.Scripts.View;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Title.Scripts.View
{
    public class TitleMenuView : MonoBehaviour
    {
        [SerializeField] ButtonList buttonList;  // 0: Start, 1: Exit
        [SerializeField] SpriteRenderer memberStillRenderer;
        [SerializeField] SpriteRenderer backGroundRenderer;

        public IObservable<Unit> OnPressedStart => buttonList.GetButtonEvent(0);
        public IObservable<Unit> OnPressedExit => buttonList.GetButtonEvent(1);

        public void Init((Sprite memberStill, Sprite backGround)sprites)
        {
            memberStillRenderer.sprite = sprites.memberStill;
            backGroundRenderer.sprite = sprites.backGround;
            
            buttonList.Init(ButtonListType.Vertical, 0, true);
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

    }
}
using System;
using UnityEngine;
using Project.Scenes.Battle.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// 背景のUVスクロールを駆動する。BattleScenePresenterから減速開始やリセットを指示される。
    /// </summary>
    [RequireComponent(typeof(BackgroundView))]
    public class BackgroundPresenter : MonoPresenter
    {
        const float LoopLength = 1f;

        [SerializeField] BackgroundView view;
        [SerializeField] Vector2 scrollSpeed = new(0.1f, 0f);
        [Tooltip("減速開始時に1秒あたり減らす速度量。値が大きいほど早く停止する。")]
        [SerializeField] float decelerationPerSecond = 0.1f;

        Vector2 currentSpeed;
        Vector2 offset;
        bool isDecelerating;

        void Reset()
        {
            view = GetComponent<BackgroundView>();
        }

        public void Initialize()
        {
            if (RuntimeModelRepository.Get().CurrentSituation == BattleSituation.Way) currentSpeed = scrollSpeed;
        }

        public void StartDeceleration()
        {
            isDecelerating = true;
        }

        public void ResetScroll(bool isMoving = true)
        {
            isDecelerating = false;
            currentSpeed = isMoving ? scrollSpeed : Vector2.zero;
            offset = Vector2.zero;
            view?.ApplyOffset(offset);
        }

        void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;

            if (isDecelerating)
            {
                Decelerate(deltaTime);
            }

            AdvanceOffset(deltaTime);
            view?.ApplyOffset(offset);
        }

        void Decelerate(float deltaTime)
        {
            var step = decelerationPerSecond * deltaTime;
            currentSpeed.x = Mathf.Max(0f, currentSpeed.x - step);
            currentSpeed.y = Mathf.Max(0f, currentSpeed.y - step);
        }

        void AdvanceOffset(float deltaTime)
        {
            offset.x = Mathf.Repeat(offset.x + currentSpeed.x * deltaTime, LoopLength);
            offset.y = Mathf.Repeat(offset.y + currentSpeed.y * deltaTime, LoopLength);
        }
    }
}

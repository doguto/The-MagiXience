using System;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public class MovableButton : MonoBehaviour
    {
        const float MoveTime = .5f;

        Transform myTransform;
        Vector2 initialPosition;

        protected void Awake()
        {
            myTransform = transform;
            initialPosition = myTransform.localPosition;
        }

        public void Move(Vector2 moveDistance)
        {
            var targetPosition = initialPosition + moveDistance;
            myTransform.DOLocalMove(targetPosition, MoveTime).SetEase(Ease.InOutQuart);
        }
    }
}

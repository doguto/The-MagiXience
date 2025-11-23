using System;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public class ArchivedButtonBase : MonoBehaviour
    {
        const float MoveTime = .5f;
        const float ScaleRatio = 1.1f;
        
        Transform _transform;
        Vector3 _initialScale;
        
        Vector2 initialPosition;
        
        readonly Subject<Unit> onPressed = new();
        public IObservable<Unit> OnPressed => onPressed;
        
        public bool IsActive { get; private set; }


        protected void Awake()
        {
            _transform = transform;
            _initialScale = _transform.localScale;
            initialPosition = _transform.localPosition;
        }

        public void SetActive(bool active)
        {
            var endPosition = _initialScale * (active? ScaleRatio : 1);
            _transform.DOScale(endPosition, MoveTime).SetEase(Ease.InOutQuart);
            IsActive = active;
        }
        
        public void Press()
        {
            if (!IsActive) return;
            
            onPressed.OnNext(Unit.Default);
        }


        public void Move(Vector2 moveDistance)
        {
            var targetPosition = initialPosition + moveDistance;
            _transform.DOLocalMove(targetPosition, MoveTime).SetEase(Ease.InOutQuart);
        }
    }
}
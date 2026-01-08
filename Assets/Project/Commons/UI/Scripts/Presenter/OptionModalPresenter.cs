using System;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.Presenter
{
    public class OptionModalPresenter: MonoPresenter
    {
        [SerializeField] OptionModalView optionModalView;
        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;
        RuntimeModel runtimeModel;

        void Awake()
        {
            runtimeModel = RuntimeModelRepository.Get();
        }

        void Start()
        {
            optionModalView.InitStart();
            
            optionModalView.OnPressedCancel.Subscribe(_ =>
            {
                gameObject.SetActive(false);
                if (runtimeModel.IsInGame) onClosed.OnNext(Unit.Default);
            }).AddTo(this);
            optionModalView.OnPressedSave.Subscribe(_ =>
            {
                //TODO saveする
            }).AddTo(this);
        }

        public void Open()
        {
            if (gameObject == null)
            {
                Debug.Log("null");
            }
            gameObject.SetActive(true);
        }
        
    }
}

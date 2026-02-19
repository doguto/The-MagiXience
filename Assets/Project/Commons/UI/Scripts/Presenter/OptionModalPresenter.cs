using System;
using Cysharp.Threading.Tasks;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.Presenter
{
    public class OptionModalPresenter : MonoPresenter
    {
        [SerializeField] OptionModalView optionModalView;
        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;
        RuntimeModel runtimeModel;

        void Awake()
        {
            runtimeModel = RuntimeModelRepository.Get();
        }

        protected override void Start()
        {
            base.Start();

            optionModalView.OnPressedCancel.Subscribe(_ =>
            {
                soundManager.PlaySEAsync(SeType.Cancel).Forget();
                gameObject.SetActive(false);
                onClosed.OnNext(Unit.Default);
            }).AddTo(this);
            optionModalView.OnPressedSave.Subscribe(_ =>
            {
                soundManager.PlaySEAsync(SeType.Click).Forget();
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
            optionModalView.InitStart();
        }
    }
}

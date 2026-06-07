using System;
using Cysharp.Threading.Tasks;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
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
        public bool IsOpen => gameObject.activeSelf;
        RuntimeModel runtimeModel;
        UserModel userModel;

        void Awake()
        {
            runtimeModel = RuntimeModelRepository.Get();
            userModel = UserModelRepository.Instance.Get();
        }

        protected override void Start()
        {
            base.Start();

            optionModalView.OnPressedCancel.Subscribe(_ =>
            {
                Close();
            }).AddTo(this);
            optionModalView.OnPressedSave.Subscribe(_ =>
            {
                soundManager.PlaySEAsync(SeType.Click).Forget();
                //TODO saveする
            }).AddTo(this);

            // 音量が変化したら即座にサウンドへ反映し、メモリ上のUserDataにも保持する。
            // 永続化(UserData.jsonへの書き込み)はモーダルを閉じたタイミングで行う。
            optionModalView.OnBgmValueChanged.Subscribe(volume =>
            {
                soundManager.SetBGMVolume(volume);
                userModel.SetVolume(volume, userModel.SeVolume);
            }).AddTo(this);
            optionModalView.OnSeValueChanged.Subscribe(volume =>
            {
                soundManager.SetSEVolume(volume);
                userModel.SetVolume(userModel.BgmVolume, volume);
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

            // 現在のUserDataの音量をスライダーへ反映する(通知を発火させずに初期化)。
            optionModalView.SetBgmValueWithoutNotify(userModel.BgmVolume);
            optionModalView.SetSeValueWithoutNotify(userModel.SeVolume);
        }

        public void Close()
        {
            soundManager.PlaySEAsync(SeType.Cancel).Forget();
            gameObject.SetActive(false);

            // 設定した音量をUserData.jsonへ書き込む。
            userModel.Save();

            onClosed.OnNext(Unit.Default);
        }
    }
}

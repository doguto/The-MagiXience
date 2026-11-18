using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Project.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Runtime;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // 実行中の Phase ノードの終了までの残りを Image.fillAmount に表示する。進み具合が決まらないフェーズでは隠す
    public class BossPhaseProgressGauge : MonoPresenter
    {
        [SerializeField] Image fillImage;

        // ボスのツリーはシナリオ明けに付与されるため、見つかった時点でサービスを購読する
        BattleServices subscribedServices;

        readonly CompositeDisposable disposables = new();
        readonly SerialDisposable progressSubscription = new();

        protected override void Start()
        {
            base.Start();

            SetProgress(null);
            BattleActor.OnAttached.Subscribe(OnActorAttached).AddTo(disposables);
            progressSubscription.AddTo(disposables);

            // 生成と同じフレームでツリーが付与された場合は OnAttached を取り逃がすため、既に付いていればここで拾う
            var existing = GetComponentInParent<BattleActor>();
            if (existing != null && existing.World != null) OnActorAttached(existing);
        }

        void OnActorAttached(BattleActor attached)
        {
            if (attached == null || !transform.IsChildOf(attached.transform)) return;
            var services = attached.World?.Services;
            if (services == null || services == subscribedServices) return;

            subscribedServices = services;
            progressSubscription.Disposable = services.PhaseProgress.Subscribe(SetProgress);
        }

        void SetProgress(float? progress)
        {
            if (fillImage == null) return;
            fillImage.gameObject.SetActive(progress.HasValue);
            if (progress.HasValue) fillImage.fillAmount = 1f - progress.Value;
        }

        void OnDestroy()
        {
            disposables.Dispose();
        }
    }
}

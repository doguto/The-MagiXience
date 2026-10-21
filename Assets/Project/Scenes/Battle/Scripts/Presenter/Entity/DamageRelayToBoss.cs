using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>
    /// この敵に通ったダメージ（＝CurrentHpの減少分）を、同じ分だけ転送先のボスへ肩代わりさせるコンポーネント。
    /// この敵自身は十分なHPを持たせて実質倒れないようにしておき、ダメージだけをボスへ流す用途を想定。
    ///
    /// 独立コンポーネントとして成立させるため、この敵のダメージ判定（EnemyEntityPresenter等）には手を入れず、
    /// IEntityPresenter経由でModelのCurrentHpを購読し、その減少分だけボスのTakeDamageを呼ぶ。
    /// </summary>
    public class DamageRelayToBoss : MonoBehaviour
    {
        [Tooltip("空の場合はシーンからBossEntityPresenterを自動検索する")]
        [SerializeField] BossEntityPresenter bossPresenter;

        IDisposable subscription;

        // Awakeでの生成順に依存しないよう、全Presenterのmodel生成(Awake)が終わったStartで購読を開始する。
        void Start()
        {
            var selfPresenter = GetComponent<IEntityPresenter>();
            var selfModel = selfPresenter?.GetModel();
            if (selfModel == null)
            {
                Debug.LogError("[DamageRelayToBoss] Self model is not available.");
                return;
            }

            if (bossPresenter == null)
            {
                bossPresenter = FindFirstObjectByType<BossEntityPresenter>();
            }
            if (bossPresenter == null)
            {
                Debug.LogError("[DamageRelayToBoss] BossEntityPresenter not found in scene.");
                return;
            }

            var bossModel = bossPresenter.GetModel();
            if (bossModel == null)
            {
                Debug.LogError("[DamageRelayToBoss] Boss model is not available.");
                return;
            }

            int previousHp = selfModel.CurrentHp.Value;
            subscription = selfModel.CurrentHp
                .Skip(1)
                .Subscribe(hp =>
                {
                    int delta = previousHp - hp;
                    previousHp = hp;

                    // 減少時のみ、その差分をボスへ転送する（回復・リセットは無視）
                    if (delta > 0 && bossModel.IsAlive)
                    {
                        bossModel.TakeDamage(delta);
                    }
                });
        }

        void OnDestroy()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}

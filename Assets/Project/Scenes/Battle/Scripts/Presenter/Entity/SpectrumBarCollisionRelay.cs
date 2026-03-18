using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>
    /// 各スペクトラムバーに付くコンポーネント。
    /// IEntityPresenterを実装し、共有のSpectrumBarEntityModelを返す。
    /// プレイヤーとの接触時にpushメッセージを発行する。
    /// </summary>
    public class SpectrumBarCollisionRelay : MonoBehaviour, IEntityPresenter
    {
        SpectrumBarEntityModel sharedModel;
        float pushForce;

        public void Initialize(SpectrumBarEntityModel model, float force)
        {
            sharedModel = model;
            pushForce = force;
        }

        public EntityBase GetModel() => sharedModel;

        void OnTriggerStay2D(Collider2D other)
        {
            // PlayerレイヤーとSpectrumBarレイヤーの衝突のみ到達する（Physics2D設定済み）
            var playerPresenter = other.GetComponent<PlayerEntityPresenter>();
            if (playerPresenter == null) return;

            MessageBroker.Default.Publish(new SpectrumBarPushMessage
            {
                pushDirection = Vector2.up,
                pushForce = pushForce
            });
        }
    }
}

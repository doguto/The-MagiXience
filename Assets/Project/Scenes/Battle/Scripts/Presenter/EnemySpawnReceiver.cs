using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Project.Scenes.Battle.Scripts.Presenter.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// EnemySpawnSignalを受け取り、指定されたPrefabをスポーンする汎用レシーバー
    /// </summary>
    public class EnemySpawnReceiver : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] EnemyTracker enemyTracker;
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is not SignalEmitter emitter) return;

            if (emitter.asset is Model.EnemySpawnSignal signal)
            {
                SpawnEnemy(signal);
            }
        }

        void SpawnEnemy(Model.EnemySpawnSignal signal)
        {
            if (signal.Prefab == null)
            {
                Debug.LogWarning("[EnemySpawnReceiver] Prefab is null, skipping spawn.", this);
                return;
            }

            var instance = Instantiate(signal.Prefab, signal.SpawnPosition, Quaternion.identity);
            var presenter = instance.GetComponent<IEntityPresenter>();
            if (presenter == null)
            {
                Debug.LogWarning($"[EnemySpawnReceiver] Spawned prefab '{signal.Prefab.name}' has no IEntityPresenter.", this);
            }

            // EnemyEntityPresenterの場合のみトラッカーに登録
            if (presenter is EnemyEntityPresenter enemyPresenter && enemyTracker != null)
            {
                enemyTracker.RegisterEnemy(enemyPresenter);
            }

            Debug.Log($"[EnemySpawnReceiver] Spawned '{signal.Prefab.name}' at {signal.SpawnPosition}", this);
        }
    }
}

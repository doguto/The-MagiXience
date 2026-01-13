using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class Stage1SignalReceiver : MonoBehaviour, INotificationReceiver
    {
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is SignalEmitter emitter)
            {
                // カスタムSignalAssetの場合
                if (emitter.asset is Model.EnemySpawnSignal enemySpawnSignal)
                {
                    HandleEnemySpawn(enemySpawnSignal);
                }
                // 標準SignalAssetの場合
                else
                {
                    HandleSignal(emitter);
                }
            }
        }

        void HandleSignal(SignalEmitter emitter)
        {
            if (emitter.asset == null) return;

            string signalName = emitter.asset.name;
            Debug.Log($"[Stage1SignalReceiver] Received signal: {signalName}", this);

            // Signal名に基づいて処理を分岐
            switch (signalName)
            {
                case "Stage1_Sample":
                    SampleFunc();
                    break;

                default:
                    Debug.LogWarning($"[Stage1SignalReceiver] Unknown signal: {signalName}", this);
                    break;
            }
        }
        void SampleFunc()
        {
            Debug.Log("[Stage1SignalReceiver] Playing intro effect", this);
        }

        void HandleEnemySpawn(Model.EnemySpawnSignal signal)
        {
            Debug.Log($"[Stage1SignalReceiver] Enemy Spawned. MaxHp: {signal.MaxHp}, Position: {signal.SpawnPosition}", this);
        }
    }
}

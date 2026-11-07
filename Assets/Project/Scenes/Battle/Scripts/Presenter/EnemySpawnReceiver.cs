using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.PhaseAction;
using Project.Scenes.Battle.Scripts.Presenter.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// フェーズの EnemySpawnAction に従って Prefab をスポーンする
    /// </summary>
    public class EnemySpawnReceiver : MonoBehaviour
    {
        [SerializeField] EnemyTracker enemyTracker;
        public void Spawn(EnemySpawnAction action)
        {
            if (action.Prefab == null)
            {
                Debug.LogWarning("[EnemySpawnReceiver] Prefab is null, skipping spawn.", this);
                return;
            }

            var instance = Instantiate(action.Prefab, action.SpawnPosition, Quaternion.identity);
            var presenter = instance.GetComponent<IEntityPresenter>();
            if (presenter == null)
            {
                Debug.LogWarning($"[EnemySpawnReceiver] Spawned prefab '{action.Prefab.name}' has no IEntityPresenter.", this);
            }

            // EnemyEntityPresenterの場合のみ動き・攻撃の上書き、トラッカー登録を行う
            if (presenter is EnemyEntityPresenter enemyPresenter)
            {
                enemyPresenter.OverrideBulletMovement(action.BulletMovementOverride);
                if (action.MovementOverride != null) enemyPresenter.OverrideMovement(action.MovementOverride);
                if (action.AttackOverride != null) enemyPresenter.OverrideAttack(action.AttackOverride);
                enemyTracker?.RegisterEnemy(enemyPresenter);
            }

            Debug.Log($"[EnemySpawnReceiver] Spawned '{action.Prefab.name}' at {action.SpawnPosition}", this);
        }
    }
}

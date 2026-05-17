using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Project.Scenes.Battle.Scripts.Presenter.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// BulletClearSignalを受け取り、シーン上の全BulletEntityPresenterをプールに返却し、
    /// 全EnemyEntityPresenterを破壊するレシーバー
    /// </summary>
    public class BulletClearReceiver : MonoBehaviour, INotificationReceiver
    {
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is not SignalEmitter emitter) return;
            if (emitter.asset is not Model.BulletClearSignal) return;

            ClearAllBullets();
            ClearAllEnemies();
        }

        public void ClearAllBullets()
        {
            var bullets = FindObjectsByType<BulletEntityPresenter>(FindObjectsSortMode.None);
            foreach (var bullet in bullets)
            {
                bullet.ReturnToPool();
            }

            Debug.Log($"[BulletClearReceiver] Cleared {bullets.Length} bullets.", this);
        }

        public void ClearAllEnemies()
        {
            var enemies = FindObjectsByType<EnemyEntityPresenter>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                Destroy(enemy.gameObject);
            }

            Debug.Log($"[BulletClearReceiver] Cleared {enemies.Length} enemies.", this);
        }
    }
}

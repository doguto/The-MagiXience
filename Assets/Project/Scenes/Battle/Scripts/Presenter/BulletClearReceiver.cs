using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// BulletClearSignalを受け取り、シーン上の全BulletEntityPresenterをプールに返却し、
    /// 全EnemyEntityPresenterを破壊するレシーバー。
    /// ビームの予告線・本体は敵として数えないためEnemyEntityPresenterを持たないので、弾と一緒に個別に破壊する
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

            ClearAllBeams();
        }

        void ClearAllBeams()
        {
            var warningLines = FindObjectsByType<WarningLineView>(FindObjectsSortMode.None);
            foreach (var warningLine in warningLines)
            {
                Destroy(warningLine.gameObject);
            }

            var beams = FindObjectsByType<SpriteBeamPresenter>(FindObjectsSortMode.None);
            foreach (var beam in beams)
            {
                Destroy(beam.gameObject);
            }

            Debug.Log($"[BulletClearReceiver] Cleared {warningLines.Length} warning lines and {beams.Length} beams.", this);
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

using UnityEngine;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.View;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    // 弾・警告線・ビーム、または敵を一括で消す。
    // ビームの予告線・本体は敵として数えないため EnemyEntityPresenter を持たず、ここで個別に破壊する
    public class BulletClearReceiver : MonoBehaviour
    {
        BulletSystem bullets;

        // 弾は消えた理由を「一括消去」にするため BulletSystem を通して消す。BattleFlow がバトル開始時に渡す
        public void BindBulletSystem(BulletSystem system) => bullets = system;

        public void ClearAllBullets()
        {
            bullets?.Clear();
            ClearAllBeams();
        }

        public void ClearAllBeams()
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
                Runtime.EnemyDespawn.Despawn(enemy.gameObject);
            }

            Debug.Log($"[BulletClearReceiver] Cleared {enemies.Length} enemies.", this);
        }
    }
}

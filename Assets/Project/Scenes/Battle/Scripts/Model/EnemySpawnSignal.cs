using UnityEngine;
using UnityEngine.Timeline;
namespace Project.Scenes.Battle.Scripts.Model
{
    /// <summary>
    /// バトルイベント用のカスタムSignalAsset
    /// イベント名、パラメータ、継続時間などのプロパティを持つ
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnSignal", menuName = "Battle/Signals/Enemy Spawn Signal")]
    public class EnemySpawnSignal : SignalAsset
    {
        [SerializeField] Vector3 spawnPosition;
        [SerializeField] int maxHp;

        public Vector3 SpawnPosition => spawnPosition;
        public int MaxHp => maxHp;

        /// <summary>
        /// ランタイムでプロパティを設定するためのメソッド
        /// BattleTimelineBuilderAssetから動的生成時に使用
        /// </summary>
        public void SetProperties(Vector3 position, int hp)
        {
            spawnPosition = position;
            maxHp = hp;
        }
    }
}

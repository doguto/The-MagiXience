using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
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
        [SerializeField] GameObject prefab;
        [SerializeField] MovementPreset movementOverride;
        [SerializeField] AttackPreset attackOverride;
        [SerializeField] MovementPreset bulletMovementOverride;

        public Vector3 SpawnPosition => spawnPosition;
        public GameObject Prefab => prefab;
        public MovementPreset MovementOverride => movementOverride;
        public AttackPreset AttackOverride => attackOverride;
        public MovementPreset BulletMovementOverride => bulletMovementOverride;

        /// <summary>
        /// ランタイムでプロパティを設定するためのメソッド
        /// BattleTimelineBuilderAssetから動的生成時に使用
        /// </summary>
        public void SetProperties(Vector3 position, GameObject prefab, MovementPreset movementOverride = null, AttackPreset attackOverride = null, MovementPreset bulletMovementOverride = null)
        {
            spawnPosition = position;
            this.prefab = prefab;
            this.movementOverride = movementOverride;
            this.attackOverride = attackOverride;
            this.bulletMovementOverride = bulletMovementOverride;
        }
    }
}

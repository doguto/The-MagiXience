using System;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseAction
{
    [Serializable]
    public class EnemySpawnAction : ITimedPhaseAction
    {
        [SerializeField, Min(0f)] float time;
        [SerializeField] Vector3 spawnPosition;
        [SerializeField] GameObject prefab;
        [SerializeField] MovementPreset movementOverride;
        [SerializeField] AttackPreset attackOverride;
        [SerializeField, Tooltip("この出現の敵が撃つ弾のMovement Stepsを置き換える。攻撃Entryにも指定があればEntryを優先。Noneなら弾Prefabの設定。")]
        MovementPreset bulletMovementOverride;

        public EnemySpawnAction() { }

        public EnemySpawnAction(float time, Vector3 spawnPosition, GameObject prefab,
            MovementPreset movementOverride, AttackPreset attackOverride, MovementPreset bulletMovementOverride)
        {
            this.time = time;
            this.spawnPosition = spawnPosition;
            this.prefab = prefab;
            this.movementOverride = movementOverride;
            this.attackOverride = attackOverride;
            this.bulletMovementOverride = bulletMovementOverride;
        }

        public float Time => time;
        public Vector3 SpawnPosition => spawnPosition;
        public GameObject Prefab => prefab;
        public MovementPreset MovementOverride => movementOverride;
        public AttackPreset AttackOverride => attackOverride;
        public MovementPreset BulletMovementOverride => bulletMovementOverride;
    }
}

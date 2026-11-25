using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 NWayEnemySpawnAttackSignal。扇状に並べ、各体を扇の角度だけ回す
    [Serializable]
    public sealed class FanLayout : SpawnLayout
    {
        [SerializeField, Min(1)] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;
        [SerializeField] Vector2 offset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots)
        {
            for (var i = 0; i < wayCount; i++)
            {
                var delta = wayCount == 1 ? 0f
                    : wayCount % 2 == 1 ? -spreadAngle * (wayCount - 1) / 2f + spreadAngle * i
                    : -spreadAngle / 2f - spreadAngle * (wayCount / 2 - 1) + spreadAngle * i;
                var rot = Quaternion.Euler(0f, 0f, delta);
                slots.Add(((Vector2)(rot * offset), rot));
            }
        }

        public override string Summary => $"{wayCount}体 扇";
    }
}

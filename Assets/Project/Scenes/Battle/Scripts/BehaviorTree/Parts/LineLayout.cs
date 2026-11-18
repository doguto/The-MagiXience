using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 LineEnemySpawnAttackSignal。中心から等間隔に並べる
    [Serializable]
    public sealed class LineLayout : SpawnLayout
    {
        [SerializeField, Min(1)] int count = 5;
        [SerializeField] float spacing = 1f;
        [SerializeField] Vector2 lineAxis = Vector2.up;
        [SerializeField] Vector2 centerOffset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots)
        {
            var axis = lineAxis.sqrMagnitude > 0f ? lineAxis.normalized : Vector2.up;
            for (var i = 0; i < count; i++) slots.Add((centerOffset + axis * ((i - (count - 1) / 2f) * spacing), Quaternion.identity));
        }

        public override string Summary => $"{count}体 一列";
    }
}

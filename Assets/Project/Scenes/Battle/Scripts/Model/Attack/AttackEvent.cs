using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    // memo: このenumは外に出して1ファイルにする方がいいのか？
    public enum AttackEventType
    {
        Bullet,
        EnemySpawn,
    }

    public readonly struct AttackEvent
    {
        public readonly AttackEventType Type;
        public readonly int SourceIndex;
        public readonly SeType SeType;
        public readonly IReadOnlyList<Vector2> Directions;
        public readonly Vector2 SpawnOffset;

        public AttackEvent(AttackEventType type, IReadOnlyList<Vector2> directions = null, int sourceIndex = 0, Vector2 spawnOffset = default, SeType seType = SeType.None)
        {
            Type = type;
            SourceIndex = sourceIndex;
            SeType = seType;
            Directions = directions;
            SpawnOffset = spawnOffset;
        }

        public static AttackEvent Single(Vector2 direction, int sourceIndex = 0, SeType seType = SeType.None) => new(AttackEventType.Bullet, new[] { direction }, sourceIndex, seType: seType);

        public static AttackEvent Spawn(Vector2 direction, int sourceIndex, Vector2 spawnOffset, SeType seType = SeType.None) => new(AttackEventType.EnemySpawn, new[] { direction }, sourceIndex, spawnOffset, seType);
    }
}

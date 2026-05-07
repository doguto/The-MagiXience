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
        public readonly Quaternion Rotation;

        public AttackEvent(AttackEventType type, IReadOnlyList<Vector2> directions = null, int sourceIndex = 0, Vector2 spawnOffset = default, SeType seType = SeType.None, Quaternion rotation = default)
        {
            Type = type;
            SourceIndex = sourceIndex;
            SeType = seType;
            Directions = directions;
            SpawnOffset = spawnOffset;
            // default(Quaternion) は (0,0,0,0) で不正なので identity に補正
            Rotation = rotation.x == 0f && rotation.y == 0f && rotation.z == 0f && rotation.w == 0f ? Quaternion.identity : rotation;
        }

        public static AttackEvent Single(Vector2 direction, Quaternion rotation, int sourceIndex = 0, SeType seType = SeType.None) => new(AttackEventType.Bullet, new[] { direction }, sourceIndex, seType: seType, rotation: rotation);

        public static AttackEvent Spawn(Vector2 direction, Quaternion rotation, int sourceIndex, Vector2 spawnOffset, SeType seType = SeType.None) => new(AttackEventType.EnemySpawn, new[] { direction }, sourceIndex, spawnOffset, seType, rotation);
    }
}

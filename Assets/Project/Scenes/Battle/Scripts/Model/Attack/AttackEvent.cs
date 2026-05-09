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
        public readonly IReadOnlyList<Vector2> SpawnOffsets;
        public readonly IReadOnlyList<Quaternion> Rotations;

        public AttackEvent(AttackEventType type, IReadOnlyList<Vector2> directions = null, int sourceIndex = 0, IReadOnlyList<Vector2> spawnOffsets = null, SeType seType = SeType.None, IReadOnlyList<Quaternion> rotations = null)
        {
            Type = type;
            SourceIndex = sourceIndex;
            SeType = seType;
            Directions = directions;
            SpawnOffsets = spawnOffsets;
            Rotations = rotations;
        }

        public static AttackEvent Single(Vector2 direction, Quaternion rotation, int sourceIndex = 0, SeType seType = SeType.None) => new(AttackEventType.Bullet, new[] { direction }, sourceIndex, seType: seType, rotations: new[] { Normalize(rotation) });

        public static AttackEvent Spawn(Vector2 direction, Quaternion rotation, int sourceIndex, Vector2 spawnOffset, SeType seType = SeType.None) => new(AttackEventType.EnemySpawn, new[] { direction }, sourceIndex, new[] { spawnOffset }, seType, new[] { Normalize(rotation) });

        public static AttackEvent SpawnMulti(IReadOnlyList<Vector2> directions, IReadOnlyList<Quaternion> rotations, int sourceIndex, IReadOnlyList<Vector2> spawnOffsets, SeType seType = SeType.None) => new(AttackEventType.EnemySpawn, directions, sourceIndex, spawnOffsets, seType, rotations);

        // default(Quaternion) は (0,0,0,0) で不正なので identity に補正
        static Quaternion Normalize(Quaternion q) => q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f ? Quaternion.identity : q;
    }
}

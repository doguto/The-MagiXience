using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public readonly struct AttackEvent
    {
        public readonly IReadOnlyList<Vector2> Directions;
        public readonly int BulletPoolIndex;
        public readonly SeType SeType;

        public AttackEvent(IReadOnlyList<Vector2> directions, int bulletPoolIndex = 0, SeType seType = SeType.None)
        {
            Directions = directions;
            BulletPoolIndex = bulletPoolIndex;
            SeType = seType;
        }

        public static AttackEvent Single(Vector2 direction, int bulletPoolIndex = 0, SeType seType = SeType.None) => new(new[] { direction }, bulletPoolIndex, seType);
    }
}

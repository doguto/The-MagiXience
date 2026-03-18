using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public readonly struct AttackEvent
    {
        public readonly IReadOnlyList<Vector2> Directions;
        public readonly int BulletPoolIndex;

        public AttackEvent(IReadOnlyList<Vector2> directions, int bulletPoolIndex = 0)
        {
            Directions = directions;
            BulletPoolIndex = bulletPoolIndex;
        }

        public static AttackEvent Single(Vector2 direction, int bulletPoolIndex = 0) => new(new[] { direction }, bulletPoolIndex);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public readonly struct AttackEvent
    {
        public readonly IReadOnlyList<Vector2> Directions;

        public AttackEvent(IReadOnlyList<Vector2> directions)
        {
            Directions = directions;
        }

        public static AttackEvent Single(Vector2 direction) => new(new[] { direction });
    }
}

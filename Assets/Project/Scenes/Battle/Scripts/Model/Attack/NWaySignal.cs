using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class NWaySignal : IAttackSignal
    {
        [SerializeField] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;

        public AttackEvent CreateEvent(IDirectionProvider directionProvider)
        {
            var baseDirection = directionProvider.GetDirection();
            var directions = new List<Vector2>(wayCount);
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            if (wayCount == 1)
            {
                directions.Add(baseDirection);
            }
            else
            {
                float halfSpread = spreadAngle / 2f;
                float step = spreadAngle / (wayCount - 1);
                for (int i = 0; i < wayCount; i++)
                {
                    float angle = baseAngle - halfSpread + step * i;
                    float rad = angle * Mathf.Deg2Rad;
                    directions.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
                }
            }

            return new AttackEvent(directions);
        }
    }
}

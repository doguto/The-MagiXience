using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class NWayEnemySpawnAttackSignal : IAttackSignal
    {
        [SerializeField] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;
        [SerializeField] Vector2 offset;

        public IAttackSignal Clone() => new NWayEnemySpawnAttackSignal { wayCount = wayCount, spreadAngle = spreadAngle, offset = offset };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var baseDirection = directionProvider.GetDirection();
            var baseRotation = rotationProvider.GetRotation();
            var directions = new List<Vector2>(wayCount);
            var spawnOffsets = new List<Vector2>(wayCount);
            var rotations = new List<Quaternion>(wayCount);
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            // offset を baseDirection 基準のローカル座標とみなして、各way方向に回転させる
            float offsetBaseAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            float offsetMagnitude = offset.magnitude;

            void AddWay(float angleDeg)
            {
                float rad = angleDeg * Mathf.Deg2Rad;
                directions.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));

                // baseDirection からの角度差ぶんだけ offset と rotation も回転させる
                float deltaDeg = angleDeg - baseAngle;
                float offsetRad = (offsetBaseAngle + deltaDeg) * Mathf.Deg2Rad;
                spawnOffsets.Add(new Vector2(Mathf.Cos(offsetRad), Mathf.Sin(offsetRad)) * offsetMagnitude);
                rotations.Add(Quaternion.Euler(0f, 0f, deltaDeg) * baseRotation);
            }

            if (wayCount == 1)
            {
                AddWay(baseAngle);
            }
            else if (wayCount % 2 == 1)
            {
                // 奇数: 中央にbaseDirectionが来る対称配置
                float halfSpread = spreadAngle * (wayCount - 1) / 2f;
                for (int i = 0; i < wayCount; i++)
                {
                    AddWay(baseAngle - halfSpread + spreadAngle * i);
                }
            }
            else
            {
                // 偶数: 中央を空けてspreadAngleの半分ずらす
                float halfStep = spreadAngle / 2f;
                for (int i = 0; i < wayCount; i++)
                {
                    AddWay(baseAngle - halfStep - spreadAngle * (wayCount / 2 - 1) + spreadAngle * i);
                }
            }

            return AttackEvent.SpawnMulti(directions, rotations, sourceIndex, spawnOffsets, seType);
        }
    }
}

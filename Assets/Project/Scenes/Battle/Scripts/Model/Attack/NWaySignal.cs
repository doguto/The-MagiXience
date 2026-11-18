using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class NWaySignal : IAttackSignal
    {
        [SerializeField] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;
        [SerializeField] float radius = 0f;
        [SerializeField, Tooltip("弾を生成してから移動を開始するまでの待機秒数。0で即座に移動開始")]
        float startDelay = 0f;
        [SerializeField, Tooltip("全体の回転オフセット（度数法）。正の値で反時計回りに回転する")]
        float rotationOffset = 0f;

        public NWaySignal() { }
        public NWaySignal(int count, float angleBetweenBullets)
        {
            wayCount = count;
            spreadAngle = angleBetweenBullets;
        }

        public IAttackSignal Clone() => new NWaySignal { wayCount = wayCount, spreadAngle = spreadAngle, radius = radius, startDelay = startDelay, rotationOffset = rotationOffset };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var baseDirection = directionProvider.GetDirection();
            var directions = new List<Vector2>(wayCount);
            // rotationOffset を基準角に加算し、全方向をまとめて回転させる
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg + rotationOffset;

            if (wayCount == 1)
            {
                float singleRad = baseAngle * Mathf.Deg2Rad;
                directions.Add(new Vector2(Mathf.Cos(singleRad), Mathf.Sin(singleRad)));
            }
            else if (wayCount % 2 == 1)
            {
                // 奇数: 中央にbaseDirectionが来る対称配置
                float halfSpread = spreadAngle * (wayCount - 1) / 2f;
                for (int i = 0; i < wayCount; i++)
                {
                    float angle = baseAngle - halfSpread + spreadAngle * i;
                    float rad = angle * Mathf.Deg2Rad;
                    directions.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
                }
            }
            else
            {
                // 偶数: 中央を空けてspreadAngleの半分ずらす
                float halfStep = spreadAngle / 2f;
                for (int i = 0; i < wayCount; i++)
                {
                    float angle = baseAngle - halfStep - spreadAngle * (wayCount / 2 - 1) + spreadAngle * i;
                    float rad = angle * Mathf.Deg2Rad;
                    directions.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
                }
            }

            List<Vector2> spawnOffsets = null;
            if (radius != 0f)
            {
                spawnOffsets = new List<Vector2>(directions.Count);
                foreach (var direction in directions)
                {
                    spawnOffsets.Add(direction * radius);
                }
            }

            return new AttackEvent(AttackEventType.Bullet, directions, sourceIndex, spawnOffsets, seType, new[] { rotationProvider.GetRotation() }, startDelay: startDelay);
        }
    }
}

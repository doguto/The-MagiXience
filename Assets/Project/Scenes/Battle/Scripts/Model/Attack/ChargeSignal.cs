using System;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 発射元(Enemy/Boss)を中心に半径radiusの円周上へwayCount発を均等配置し、
    /// 各弾を螺旋を描きながらtargetRadiusまで収束させる攻撃シグナル。
    /// 収束後は弾自身のライフタイムで消える想定のため、gatherDuration前後の値をBullet Prefabのlifetimeに設定しておくこと。
    /// </summary>
    [Serializable]
    public class ChargeSignal : IAttackSignal
    {
        [SerializeField, Min(1)] int wayCount = 16;
        [SerializeField, Min(0f)] float radius = 3.5f;
        [SerializeField, Min(0f)] float targetRadius = 0f;
        [SerializeField, Min(0.01f)] float gatherDuration = 1.2f;
        [SerializeField, Tooltip("度/秒。負値で時計回り")] float rotationSpeed = 480f;
        [SerializeField, Tooltip("弾を配置してから螺旋移動を開始するまでの待機秒数")]
        float startDelay = 0f;

        public IAttackSignal Clone() => new ChargeSignal
        {
            wayCount = wayCount,
            radius = radius,
            targetRadius = targetRadius,
            gatherDuration = gatherDuration,
            rotationSpeed = rotationSpeed,
            startDelay = startDelay,
        };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var baseDirection = directionProvider.GetDirection();
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            float spreadAngle = 360f / wayCount;

            var directions = new Vector2[wayCount];
            var spawnOffsets = new Vector2[wayCount];
            for (int i = 0; i < wayCount; i++)
            {
                float rad = (baseAngle + spreadAngle * i) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                directions[i] = dir;
                spawnOffsets[i] = dir * radius;
            }

            var spiral = new SpiralMovementConfig(radius, targetRadius, gatherDuration, rotationSpeed);

            return new AttackEvent(
                AttackEventType.Bullet,
                directions,
                sourceIndex,
                spawnOffsets,
                seType,
                new[] { rotationProvider.GetRotation() },
                startDelay: startDelay,
                bulletMovement: spiral);
        }
    }
}

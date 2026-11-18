using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// wayCount発の弾を、Enemy中心の半径radius上に spawnInterval 間隔で順番に配置してから、
    /// 全弾が同じ絶対時刻(配置完了 + releaseDelay)にぴったり同時に動き出す攻撃シグナル。
    /// 発射数・配置間隔・発射までの余白はすべてこのシグナルのフィールド(数値)だけで完結する。
    /// </summary>
    [Serializable]
    public class SequentialCircleBulletSignal : IAttackSignal
    {
        [SerializeField, Min(1)] int wayCount = 12;
        [SerializeField] float radius = 1.5f;
        [SerializeField, Tooltip("隣り合う弾を配置する間隔(秒)")]
        float spawnInterval = 0.08f;
        [SerializeField, Tooltip("全弾の配置が完了してから、一斉に動き出すまでの余白(秒)")]
        float releaseDelay = 0.2f;

        public IAttackSignal Clone() => new SequentialCircleBulletSignal
        {
            wayCount = wayCount,
            radius = radius,
            spawnInterval = spawnInterval,
            releaseDelay = releaseDelay,
        };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var baseDirection = directionProvider.GetDirection();
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            float angleStep = 360f / wayCount;

            var directions = new Vector2[wayCount];
            var spawnOffsets = new Vector2[wayCount];
            var spawnDelays = new float[wayCount];
            var startDelays = new float[wayCount];

            float totalSpawnTime = (wayCount - 1) * spawnInterval;

            for (int i = 0; i < wayCount; i++)
            {
                float angle = baseAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                directions[i] = direction;
                spawnOffsets[i] = direction * radius;

                float spawnDelay = i * spawnInterval;
                spawnDelays[i] = spawnDelay;
                startDelays[i] = (totalSpawnTime - spawnDelay) + releaseDelay;
            }

            return new AttackEvent(
                AttackEventType.Bullet,
                directions,
                sourceIndex,
                spawnOffsets,
                seType,
                new[] { rotationProvider.GetRotation() },
                startDelays: startDelays,
                spawnDelays: spawnDelays);
        }
    }
}

using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// wayCount発の弾を、Enemyの現在位置(オフセット0)に spawnInterval 間隔で順番に配置してから、
    /// 全弾が同じ絶対時刻(配置完了 + releaseDelay)にぴったり同時に、directionProviderを中心とした
    /// 扇状(NWaySignalと同じ角度計算)の各方向へ動き出す攻撃シグナル。
    /// 配置中にEnemy自体が移動していれば、その軌道上に弾が置かれていくトレイルになる。
    /// spreadAngleは隣り合う弾同士の角度差(NWaySignalと同じ意味)。180/(wayCount-1)にすると半円全体に等間隔で広がる。
    /// </summary>
    [Serializable]
    public class SequentialFanBulletSignal : IAttackSignal
    {
        [SerializeField, Min(1)] int wayCount = 13;
        [SerializeField] float spreadAngle = 15f;
        [SerializeField, Tooltip("隣り合う弾を配置する間隔(秒)")]
        float spawnInterval = 0.1f;
        [SerializeField, Tooltip("全弾の配置が完了してから、一斉に発射するまでの余白(秒)")]
        float releaseDelay = 0.3f;

        public IAttackSignal Clone() => new SequentialFanBulletSignal
        {
            wayCount = wayCount,
            spreadAngle = spreadAngle,
            spawnInterval = spawnInterval,
            releaseDelay = releaseDelay,
        };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var baseDirection = directionProvider.GetDirection();
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            var directions = new Vector2[wayCount];

            if (wayCount == 1)
            {
                directions[0] = baseDirection;
            }
            else if (wayCount % 2 == 1)
            {
                // 奇数: 中央にbaseDirectionが来る対称配置
                float halfSpread = spreadAngle * (wayCount - 1) / 2f;
                for (int i = 0; i < wayCount; i++)
                {
                    float angle = baseAngle - halfSpread + spreadAngle * i;
                    float rad = angle * Mathf.Deg2Rad;
                    directions[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
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
                    directions[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                }
            }

            var spawnDelays = new float[wayCount];
            var startDelays = new float[wayCount];
            float totalSpawnTime = (wayCount - 1) * spawnInterval;

            for (int i = 0; i < wayCount; i++)
            {
                float spawnDelay = i * spawnInterval;
                spawnDelays[i] = spawnDelay;
                startDelays[i] = (totalSpawnTime - spawnDelay) + releaseDelay;
            }

            return new AttackEvent(
                AttackEventType.Bullet,
                directions,
                sourceIndex,
                spawnOffsets: null,
                seType,
                new[] { rotationProvider.GetRotation() },
                startDelays: startDelays,
                spawnDelays: spawnDelays);
        }
    }
}

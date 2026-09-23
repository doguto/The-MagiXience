using System;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 発射元(Enemy/Boss本体)を targetOffset へ moveDuration かけて移動させながら、
    /// その軌道上(オフセット0、配置時点のEnemy位置)に wayCount 発の弾を順番に配置し、
    /// 全弾の配置完了(＝移動完了と同時) + releaseDelay 秒後に、
    /// directionProviderを中心とした扇状(NWaySignalと同じ角度計算)の各方向へ一斉に動き出す攻撃シグナル。
    /// SequentialFanBulletSignal に「発射元自身の移動」を組み込んだもの。
    /// wayCount:1 なら全弾同一方向、2以上なら spreadAngle で扇状に開く。
    /// 弾を配置する間隔(spawnInterval)は moveDuration / (wayCount - 1) で自動算出する。
    /// 移動時間と配置完了タイミングが常に一致するため、手動で揃える必要はない。
    /// </summary>
    [Serializable]
    public class MoveTrailFanBulletSignal : IAttackSignal
    {
        [SerializeField, Tooltip("移動先。isRelative=trueなら現在位置からの相対オフセット、falseならワールド座標の絶対位置")]
        Vector3 targetOffset = new Vector3(-3f, 0f, 0f);
        [SerializeField] bool isRelative = true;
        [SerializeField, Min(0f)] float moveDuration = 1f;
        [SerializeField] int moveEaseValue = (int)Ease.Linear;

        [SerializeField, Min(1)] int wayCount = 13;
        [SerializeField] float spreadAngle = 15f;
        [SerializeField, Tooltip("全弾の配置が完了してから、一斉に発射するまでの余白(秒)")]
        float releaseDelay = 0.3f;
        [SerializeField, Tooltip("falseなら弾を一切出さず、発射元の移動だけを行う(ただの経由地点への移動として使う)")]
        bool emitBullets = true;

        public IAttackSignal Clone() => new MoveTrailFanBulletSignal
        {
            targetOffset = targetOffset,
            isRelative = isRelative,
            moveDuration = moveDuration,
            moveEaseValue = moveEaseValue,
            wayCount = wayCount,
            spreadAngle = spreadAngle,
            releaseDelay = releaseDelay,
            emitBullets = emitBullets,
        };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var movementStep = new TweenMovementConfig(targetOffset, moveDuration, moveEaseValue, isRelative);

            if (!emitBullets)
            {
                return new AttackEvent(
                    AttackEventType.Bullet,
                    Array.Empty<Vector2>(),
                    sourceIndex,
                    spawnOffsets: null,
                    seType,
                    movementStep: movementStep);
            }

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

            // 配置間隔は moveDuration から自動算出し、全弾配置完了が移動完了と必ず一致するようにする
            float spawnInterval = wayCount > 1 ? moveDuration / (wayCount - 1) : 0f;

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
                spawnDelays: spawnDelays,
                movementStep: movementStep);
        }
    }
}

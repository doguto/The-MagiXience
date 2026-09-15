using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 敵を直線状に整列させて生成するSignal。
    /// NWayEnemySpawnAttackSignalの円弧(放射状)配置とは異なり、lineAxis方向に等間隔で並べる。
    /// 各敵の進行方向はdirectionProviderに従い、transform.rotationは常にidentity(無回転)にする。
    /// </summary>
    [Serializable]
    public class LineEnemySpawnAttackSignal : IAttackSignal
    {
        [SerializeField] int count = 5;
        [SerializeField] float spacing = 1f;
        // 整列軸。既定は(0,1)で画面縦一直線。正規化して使うので長さは問わない。
        [SerializeField] Vector2 lineAxis = new(0f, 1f);
        // 列全体の中心位置オフセット(発射元基準)。
        [SerializeField] Vector2 centerOffset;
        [SerializeField] MovementPreset movementOverride;

        public IAttackSignal Clone() => new LineEnemySpawnAttackSignal
        {
            count = count,
            spacing = spacing,
            lineAxis = lineAxis,
            centerOffset = centerOffset,
            movementOverride = movementOverride,
        };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            var direction = directionProvider.GetDirection();
            var axis = lineAxis.sqrMagnitude > 0f ? lineAxis.normalized : Vector2.up;

            var directions = new List<Vector2>(count);
            var spawnOffsets = new List<Vector2>(count);
            var rotations = new List<Quaternion>(count);

            for (int i = 0; i < count; i++)
            {
                // 列の中心を基準に対称配置する
                float step = i - (count - 1) / 2f;
                directions.Add(direction);
                spawnOffsets.Add(centerOffset + axis * (step * spacing));
                rotations.Add(Quaternion.identity);
            }

            return AttackEvent.SpawnMulti(directions, rotations, sourceIndex, spawnOffsets, seType, movementOverride);
        }
    }
}

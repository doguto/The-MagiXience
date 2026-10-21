using System;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class EnemySpawnAttackSignal : IAttackSignal
    {
        [SerializeField] Vector2 offset;
        [SerializeField] MovementPreset movementOverride;

        [SerializeField, Min(0f), Tooltip("生成物が IBeamVisualReceiver(予告線など)を持つ場合の太さ上書き(ワールド単位)。0以下ならPrefab側の設定を使う")]
        float width;

        public IAttackSignal Clone() => new EnemySpawnAttackSignal { offset = offset, movementOverride = movementOverride, width = width };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.Spawn(directionProvider.GetDirection(), rotationProvider.GetRotation(), sourceIndex, offset, seType, movementOverride, width);
        }
    }
}

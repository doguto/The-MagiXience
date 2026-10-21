using System;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackTimelineEntry
    {
        [SerializeField] public float time;
        [SerializeReference, SubclassSelector] public IAttackSignal signal;
        [SerializeReference, SubclassSelector] public IDirectionProvider directionProvider = new FixedDirectionConfig();
        [SerializeReference, SubclassSelector] public IRotationProvider rotationProvider = new InheritParentRotationConfig();
        [SerializeReference, SubclassSelector] public ISourceIndexProvider sourceIndexProvider = new ConstantSourceIndex();
        [SerializeField] public SeType seType = SeType.None;
        [SerializeField, Tooltip("この攻撃の弾のMovement Stepsを置き換える。敵の出現Clipの指定より優先。Noneなら出現Clip、次に弾Prefabの設定を使う。")]
        public MovementPreset bulletMovementOverride;

        public AttackEvent CreateEvent(MovementPreset inheritedBulletMovement = null)
        {
            var ev = signal.CreateEvent(directionProvider, rotationProvider, sourceIndexProvider?.Get() ?? 0, seType);
            return ev.WithBulletMovementOverride(bulletMovementOverride != null ? bulletMovementOverride : inheritedBulletMovement);
        }

        public AttackTimelineEntry DeepCopy()
        {
            return new AttackTimelineEntry
            {
                time = time,
                signal = signal?.Clone(),
                directionProvider = directionProvider?.Clone(),
                rotationProvider = rotationProvider?.Clone(),
                sourceIndexProvider = sourceIndexProvider?.Clone(),
                seType = seType,
                bulletMovementOverride = bulletMovementOverride
            };
        }
    }
}

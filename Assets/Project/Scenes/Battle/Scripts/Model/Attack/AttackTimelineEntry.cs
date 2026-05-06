using System;
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
        [SerializeField] public int sourceIndex;
        [SerializeField] public SeType seType = SeType.None;

        public AttackTimelineEntry DeepCopy()
        {
            return new AttackTimelineEntry
            {
                time = time,
                signal = signal?.Clone(),
                directionProvider = directionProvider?.Clone(),
                sourceIndex = sourceIndex,
                seType = seType
            };
        }
    }
}

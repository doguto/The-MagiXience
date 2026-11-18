using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackPhase
    {
        [SerializeField] public bool loop;
        [SerializeField] public float loopStart;
        [SerializeField] public float loopEnd = 5f;
        [SerializeField, Min(0.01f)] public float cycleDuration = 2f;
        [SerializeField] public List<AttackTimelineEntry> entries = new();

        [SerializeReference, SubclassSelector]
        public IAttackPhaseTriggerConfig nextPhaseTrigger;

        public AttackPhase DeepCopy()
        {
            var copy = new AttackPhase
            {
                loop = loop,
                loopStart = loopStart,
                loopEnd = loopEnd,
                cycleDuration = cycleDuration,
                entries = new List<AttackTimelineEntry>(entries.Count),
                nextPhaseTrigger = nextPhaseTrigger?.Clone()
            };
            foreach (var entry in entries)
            {
                copy.entries.Add(entry.DeepCopy());
            }
            return copy;
        }
    }
}

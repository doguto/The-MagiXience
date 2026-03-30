using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class PresetAttackSignal : IAttackSignal
    {
        [SerializeField] AttackPreset preset;
        [SerializeField] bool loop;
        [SerializeField] int loopCount = 1; // 0 = ignore (no infinite loop allowed)
        [SerializeField, Min(0.01f)] float cycleDuration = 2f;

        public AttackPreset Preset => preset;
        public bool Loop => loop;
        public int LoopCount => loopCount;
        public float CycleDuration => cycleDuration;

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, int bulletPoolIndex = 0, SeType seType = SeType.None)
        {
            // PresetSignalは直接CreateEventされない。AttackTimeline側で展開される。
            Debug.LogWarning("[PresetAttackSignal] CreateEvent should not be called directly. Use AttackTimeline expansion.");
            return default;
        }

        public IAttackSignal Clone() => new PresetAttackSignal
        {
            preset = preset,
            loop = loop,
            loopCount = loopCount,
            cycleDuration = cycleDuration
        };
    }
}

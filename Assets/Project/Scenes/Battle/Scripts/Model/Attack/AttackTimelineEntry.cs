using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackTimelineEntry
    {
        [SerializeField] public float time;
        [SerializeReference, SubclassSelector] public IAttackSignal signal;
        [SerializeReference, SubclassSelector] public IDirectionProvider directionProvider = new FixedDirectionConfig();
    }
}

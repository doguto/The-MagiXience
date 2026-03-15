using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [CreateAssetMenu(fileName = "AttackPreset", menuName = "Battle/Attack Preset")]
    public class AttackPreset : ScriptableObject
    {
        [SerializeField] AttackTimeline attackTimeline;

        public AttackTimeline CreateTimeline() => attackTimeline?.DeepCopy();
    }
}

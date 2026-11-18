using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [CreateAssetMenu(fileName = "MovementPreset", menuName = "Battle/Movement Preset")]
    public class MovementPreset : ScriptableObject
    {
        [SerializeReference, SubclassSelector]
        List<IMovementStep> steps = new();

        public IReadOnlyList<IMovementStep> Steps => steps;

        public IMovementStep SampleForAttackTime(float attackTime)
        {
            if (!steps.Exists(step => step is IAttackTimeMovementStep)) return null;
            var sampled = steps.ConvertAll(step => step is IAttackTimeMovementStep timed
                ? timed.SampleForAttackTime(attackTime) : step);
            return sampled.Count == 1 ? sampled[0] : new SampledSequence(sampled);
        }

        sealed class SampledSequence : IMovementStep
        {
            readonly List<IMovementStep> steps;
            public SampledSequence(List<IMovementStep> steps) => this.steps = steps;
            public Tween Play(Transform target, Vector2 direction, Animator animator)
            {
                var sequence = DOTween.Sequence();
                foreach (var step in steps)
                {
                    var tween = step?.Play(target, direction, animator);
                    if (tween != null) sequence.Append(tween);
                }
                return sequence;
            }
        }
    }
}

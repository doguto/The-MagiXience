using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [CreateAssetMenu(fileName = "MovementPreset", menuName = "Battle/Movement Preset")]
    public class MovementPreset : ScriptableObject
    {
        [SerializeReference, SubclassSelector]
        List<IMovementStep> steps = new();

        public IReadOnlyList<IMovementStep> Steps => steps;
    }
}

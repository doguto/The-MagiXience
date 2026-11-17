using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    [CreateAssetMenu(fileName = "Motion", menuName = "Battle/Motion Preset")]
    public class MotionPreset : ScriptableObject
    {
        [SerializeReference, SubclassSelector] List<MotionStep> steps = new();

        public List<MotionStep> Steps => steps;
    }
}

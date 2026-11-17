using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class WindInterval
    {
        public WindDirection direction;
        [Min(0.1f)] public float seconds = 6f;
    }
}

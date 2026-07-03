using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class RandomSourceIndex : ISourceIndexProvider
    {
        [SerializeField] int min;
        [SerializeField] int max;

        public int Get() => UnityEngine.Random.Range(min, max + 1);
        public ISourceIndexProvider Clone() => new RandomSourceIndex { min = min, max = max };
    }
}

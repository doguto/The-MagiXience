using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class ConstantSourceIndex : ISourceIndexProvider
    {
        [SerializeField] int index;

        public ConstantSourceIndex() { }
        public ConstantSourceIndex(int index) => this.index = index;

        public int Get() => index;
        public ISourceIndexProvider Clone() => new ConstantSourceIndex { index = index };
    }
}

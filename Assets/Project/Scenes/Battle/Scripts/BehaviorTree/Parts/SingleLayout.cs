using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class SingleLayout : SpawnLayout
    {
        [SerializeField] Vector2 offset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots) => slots.Add((offset, Quaternion.identity));
        public override string Summary => offset == Vector2.zero ? "1体" : $"1体 {offset}";
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class ParallelPattern : ShotPattern
    {
        [SerializeField] BtInt count = 3;
        [SerializeField, Tooltip("隣り合う弾の間隔")] BtFloat spacing = 1f;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var n = Mathf.Max(1, count.value);
            var side = new Vector2(-baseDirection.y, baseDirection.x).normalized;
            for (var i = 0; i < n; i++)
            {
                shots.Add(new Shot
                {
                    Direction = baseDirection,
                    Offset = side * ((i - (n - 1) / 2f) * spacing.value),
                });
            }
        }

        public override string Summary => $"{count} 本平行 間隔 {spacing}";

        public override ShotPattern Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(count.param) && string.IsNullOrEmpty(spacing.param)) return this;
            var copy = (ParallelPattern)MemberwiseClone();
            copy.count = count.Resolve(scope);
            copy.spacing = spacing.Resolve(scope);
            return copy;
        }
    }
}

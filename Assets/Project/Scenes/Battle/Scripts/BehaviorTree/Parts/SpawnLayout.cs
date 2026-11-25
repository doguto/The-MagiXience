using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 敵などを出現させる位置の並べ方。撃つ敵がいればその位置から、いなければワールド座標
    [Serializable]
    public abstract class SpawnLayout
    {
        public abstract void Build(Vector2 baseDirection, List<(Vector2 offset, Quaternion rotation)> slots);
        public virtual string Summary => GetType().Name.Replace("Layout", "");
    }
}

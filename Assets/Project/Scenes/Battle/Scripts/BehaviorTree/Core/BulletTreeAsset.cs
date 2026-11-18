using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 弾を動かすツリー。Self は BulletAgent。撃つ側から渡された移動は MoveSelf で実行する
    [CreateAssetMenu(fileName = "BulletTree", menuName = "Battle/Behavior Tree/Bullet")]
    public sealed class BulletTreeAsset : BehaviorTreeAsset
    {
        public override bool Accepts(Type nodeType) => !RequiresEnemy(nodeType);
    }
}

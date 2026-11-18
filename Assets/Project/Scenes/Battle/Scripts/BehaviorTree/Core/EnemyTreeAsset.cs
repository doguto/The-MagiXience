using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 敵・ボスを動かすツリー。Self は EnemyAgent
    [CreateAssetMenu(fileName = "EnemyTree", menuName = "Battle/Behavior Tree/Enemy")]
    public sealed class EnemyTreeAsset : BehaviorTreeAsset
    {
        public override bool Accepts(Type nodeType) => true;
    }
}

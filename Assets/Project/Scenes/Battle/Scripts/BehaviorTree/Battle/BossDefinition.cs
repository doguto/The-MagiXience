using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ボスを 1 体出すのに必要なもの。SpawnBoss / BossBattle ノードから参照する
    [CreateAssetMenu(fileName = "BossDefinition", menuName = "Battle/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("BossEntityPresenter を持つ Prefab")] GameObject prefab;
        [SerializeField] Vector3 spawnPosition;
        [SerializeReference, SubclassSelector, Tooltip("登場の移動。戦闘開始時に終わっていなければ終点まで進める")] List<MotionStep> entrance = new();
        [SerializeField, Tooltip("ボス自身が実行するツリー。BossBattle で別のツリーを指定しなければこれを使う")] BehaviorTreeAsset tree;

        public GameObject Prefab => prefab;
        public Vector3 SpawnPosition => spawnPosition;
        public IReadOnlyList<MotionStep> Entrance => entrance;
        public BehaviorTreeAsset Tree => tree;
    }
}
